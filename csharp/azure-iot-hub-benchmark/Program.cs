// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Based on the work of https://www.konabos.com/blog/load-testing-the-azure-iot-hub
 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using Microsoft.Azure.Devices;
using CommandLine;
using System.IO;
using Newtonsoft.Json;

namespace azure_iot_hub_benchmark
{
    class Options
    {
        [Option(
            Required = true,
            HelpText = "The iothubowner ConnectionString. Example \"HostName=myhub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=0U1REMOVEDvrfUDo=\"")]
        public string IotHubConnectionString { get; set; }
        [Option(
            Required = false,
            Default = "benchmarkfile",
            HelpText = "The full file path of the file were benchmark results are stored")]
        public string BenchmarkFileNamePath { get; set; }
        [Option(
            Required = false,
            Default = "MQTT",
            HelpText = "The Transport Type to use. MQTT,AMQP,HTTP")]
        public string TransportType { get; set; }
        [Option(
            Default = 100,
            HelpText = "how many devices we will create and clients we will launch")]
        public short DeviceCount { get; set; }
        [Option(
            Default = 20,
            HelpText = "once this count of messages are sent, the client shuts down")]
        public int MaxMessages { get; set; }
        [Option(
            Default = 20,
            HelpText = "Size of a single message in byte")]
        public int MessageSize { get; set; }
    }

    class BenchmarkEntry
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public short DeviceCount { get; set; }
        public int MaxMessages { get; set; }
        public int MessageSize { get; set; }
        public double MessagePerSecond { get; set; }
        public List<DeviceBenchmarkEntry> DeviceBenchmarks { get; set; }
        public string TransportType {get; set;}
    }
    class DeviceBenchmarkEntry
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime StartTimeWithoutConnect { get; set; }
        public DateTime EndTimeWithoutConnect { get; set; }
        public string DeviceName { get; set; }
        public int MaxMessages { get; set; }
        public int MessageSize { get; set; }
        public double MessagePerSecond { get; set; }
        public double MessagePerSecondWithoutConnect { get; set; }
    }
    class Program
    {
        static readonly string IoTDevicePrefix = "loadTest";
        static readonly string CommonKey = "PICHP911sX4rh9OIw3ucDzkBYYb7jwxVet8vcqySYH8="; // random base64 string
        const int MAXMESSAGESIZE = 262143;

        static int MessageSize;

        static Microsoft.Azure.Devices.Client.TransportType TransportType;
 
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts =>
            {
                string IoTHub = opts.IotHubConnectionString.Split(';')[0].Split('=')[1];
                Console.WriteLine("Creating Devices");
                CreateDevices(opts.DeviceCount, opts.IotHubConnectionString).Wait();
                // Messagesize
                if(opts.MessageSize > 262143){
                    Console.WriteLine($"Setting MessageSize to maximum of {MAXMESSAGESIZE}");
                    MessageSize = MAXMESSAGESIZE;
                } else {
                    MessageSize = opts.MessageSize;
                }
                // Transporttype
                switch(opts.TransportType){
                    case "MQTT":
                        TransportType = Microsoft.Azure.Devices.Client.TransportType.Mqtt;
                        break;
                    case "AMQP":
                        TransportType = Microsoft.Azure.Devices.Client.TransportType.Amqp;
                        break;
                    case "HTTP":
                        TransportType = Microsoft.Azure.Devices.Client.TransportType.Http1;
                        break;
                    default:
                        TransportType = Microsoft.Azure.Devices.Client.TransportType.Mqtt;
                        break;
                }
                Console.WriteLine($"Starting Devices: DeviceCount: {opts.DeviceCount} MaxMessages:{opts.MaxMessages} MessageSize:{MessageSize} TransportType:{TransportType}");
                DateTime startTime = DateTime.Now;
                BenchmarkEntry benchmarkEntry = new BenchmarkEntry()
                {
                    StartTime = startTime,
                    DeviceCount = opts.DeviceCount,
                    MaxMessages = opts.MaxMessages,
                    MessageSize = MessageSize,
                    DeviceBenchmarks = new List<DeviceBenchmarkEntry>(),
                    TransportType = opts.TransportType
                };
                Task[] tasks = new Task[opts.DeviceCount];
                for (int deviceNumber = 1; deviceNumber <= opts.DeviceCount; deviceNumber++)
                {
                    tasks[deviceNumber-1] = StartClient(IoTHub, IoTDevicePrefix, deviceNumber, CommonKey, opts.MaxMessages, MessageSize, TransportType).ContinueWith(task =>
                    {
                        benchmarkEntry.DeviceBenchmarks.Add(task.Result);
                    });
                }
                Task.WaitAll(tasks);
                DateTime endTime = DateTime.Now;
                double messagesPerSecond = opts.DeviceCount * opts.MaxMessages / (endTime - startTime).TotalSeconds;
                benchmarkEntry.EndTime = endTime;
                benchmarkEntry.MessagePerSecond = messagesPerSecond;
                WriteResultsToFile(benchmarkEntry, opts.BenchmarkFileNamePath).Wait();

                Console.WriteLine("All Messages are sent");
                Console.WriteLine($"Total Clients: {opts.DeviceCount}");
                Console.WriteLine($"Message Size: {opts.MessageSize}");
                Console.WriteLine($"Total Messages Sent: {opts.DeviceCount * opts.MaxMessages}");
                Console.WriteLine($"Total Execution Time: {(endTime - startTime).TotalSeconds} seconds");
                Console.WriteLine($"Messages Per Second: {messagesPerSecond}");
                Thread.Sleep(7000); // Wait before starting to delete devices
                DeleteDevices(opts.DeviceCount, opts.IotHubConnectionString).Wait();
            });
        }
 
        static async Task<DeviceBenchmarkEntry> StartClient(string IoTHub, string IoTDevicePrefix, int deviceNumber, string commonKey, int maxMessages, int messageSize, Microsoft.Azure.Devices.Client.TransportType transportType)
        {
            string connectionString = "HostName=" + IoTHub + ";DeviceId=" + IoTDevicePrefix + deviceNumber + ";SharedAccessKey=" + commonKey;
            string deviceName = IoTDevicePrefix + deviceNumber;
            DateTime startTime = DateTime.Now;
            try
            {
                DeviceClient device = DeviceClient.CreateFromConnectionString(connectionString, transportType);
                await device.OpenAsync();
                int mycounter = 1;
                Console.WriteLine($"Device {deviceName} started");

                DateTime startTimeWithoutConnect = DateTime.Now;
                while (mycounter <= maxMessages)
                {
                    var IoTMessage = new Microsoft.Azure.Devices.Client.Message(new byte[messageSize]);
                    await device.SendEventAsync(IoTMessage);
                    mycounter++;
                }
                DateTime endTimeWithoutConnect = DateTime.Now;
                double messagesPerSecondWithoutConnect = maxMessages / (endTimeWithoutConnect - startTimeWithoutConnect).TotalSeconds;
                Console.WriteLine($"Device {deviceName}: Messages Per Second Without Connect: { messagesPerSecondWithoutConnect}");

                await device.CloseAsync();
                DateTime endTime = DateTime.Now;
                double messagesPerSecond = maxMessages / (endTime - startTime).TotalSeconds;
                Console.WriteLine($"Device {deviceName}: Messages Per Second: { messagesPerSecond}" );
                Console.WriteLine($"Device {deviceName} ended");

                DeviceBenchmarkEntry benchmarkEntry = new DeviceBenchmarkEntry()
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    StartTimeWithoutConnect = startTimeWithoutConnect,
                    EndTimeWithoutConnect = endTimeWithoutConnect,
                    DeviceName = deviceName,
                    MaxMessages = maxMessages,
                    MessageSize = messageSize,
                    MessagePerSecond = messagesPerSecond,
                    MessagePerSecondWithoutConnect = messagesPerSecondWithoutConnect
                };
                return benchmarkEntry;
            } 
            catch (Exception er)
            {
                Console.WriteLine($"Error starting device: {deviceName} error: {er.InnerException.Message}");
            }
            return null;
        }
 
        static async Task CreateDevices(int number, string iotHubConnectionString)
        {
            for (int i = 1; i <= number; i++)
            {
                var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
                Device mydevice = new Device(IoTDevicePrefix + i.ToString());
                mydevice.Authentication = new AuthenticationMechanism();
                mydevice.Authentication.SymmetricKey.PrimaryKey = CommonKey;
                mydevice.Authentication.SymmetricKey.SecondaryKey = CommonKey;
                try
                {
                    await registryManager.AddDeviceAsync(mydevice);
                    Console.WriteLine($"Adding device: {IoTDevicePrefix + i.ToString()}");
                }
                catch (Exception er)
                {
                    Console.WriteLine($"Error adding device: {IoTDevicePrefix + i.ToString()} error: {er.InnerException.Message}");
                }
            }
 
        }
        static async Task DeleteDevices(int number, string iotHubConnectionString)
        {
            for (int i = 1; i <= number; i++)
            {
                var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
 
                try
                {
                    Device mydevice = await registryManager.GetDeviceAsync(IoTDevicePrefix + i.ToString());
                    await registryManager.RemoveDeviceAsync(mydevice);
                    Console.WriteLine($"Deleting device: {IoTDevicePrefix + i.ToString()}");
                }
                catch (Exception er) {
                    Console.WriteLine($"Error deleting device: {IoTDevicePrefix + i.ToString()} error: {er.InnerException.Message}");
                }
            } 
        }

        static async Task WriteResultsToFile(BenchmarkEntry benchmarkEntry, string filename)
        {
            Console.WriteLine($"Writing Bencmark Results to file {Path.GetFullPath(filename)}");
            using (StreamWriter file = File.CreateText(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                //serialize object directly into file stream
                serializer.Serialize(file, benchmarkEntry);
                file.Close();
            }
        }
    }
}
