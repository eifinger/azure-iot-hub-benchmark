// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Based on the work of https://www.konabos.com/blog/load-testing-the-azure-iot-hub
 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using Microsoft.Azure.Devices;
using CommandLine;
 
namespace azure_iot_hub_benchmark
{
    class Options
    {
        [Option(
            Required = true,
            HelpText = "The iothubowner ConnectionString. Example \"HostName=myhub.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=0U1REMOVEDvrfUDo=\"")]
        public String IotHubConnectionString { get; set; }

        [Option(
            Default = 100,
            HelpText = "how many devices we will create and clients we will launch")]
        public int DeviceCount { get; set; }

        [Option(
            Default = 20,
            HelpText = "once this count of messages are sent, the client shuts down")]
        public int MaxMessages { get; set; }

        [Option(
            Default = 20,
            HelpText = "Size of a single message in byte")]
        public int MessageSize { get; set; }
    }
    class Program
    {
        static string IoTDevicePrefix = "loadTest";
        static string commonKey = "PICHP911sX4rh9OIw3ucDzkBYYb7jwxVet8vcqySYH8="; // random base64 string
 
        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts =>
            {
                string IoTHub = opts.IotHubConnectionString.Split(';')[0].Split('=')[1];
                Console.WriteLine("Creating Devices");
                createDevices(opts.DeviceCount, opts.IotHubConnectionString);
                Console.WriteLine($"Starting Devices: DeviceCount: {opts.DeviceCount} MaxMessages:{opts.MaxMessages} MessageSize:{opts.MessageSize}");
                DateTime startTime = DateTime.Now;
                Task[] tasks = new Task[opts.DeviceCount];
                for (int deviceNumber = 1; deviceNumber <= opts.DeviceCount; deviceNumber++)
                {
                    tasks[deviceNumber-1] = startClient(IoTHub, IoTDevicePrefix, deviceNumber, commonKey, opts.MaxMessages, opts.MessageSize);
                }

                Task.WaitAll(tasks);
                DateTime endTime = DateTime.Now;
                Console.WriteLine("All Messages are sent");
                Console.WriteLine("Total Clients: " + opts.DeviceCount);
                Console.WriteLine("Total Messages Sent: " + opts.DeviceCount * opts.MaxMessages);
                Console.WriteLine("Total Execution Time: " + (endTime - startTime).TotalSeconds + " seconds");
                Console.WriteLine("Messages Per Second: " + opts.DeviceCount * opts.MaxMessages / (endTime - startTime).TotalSeconds);
                Thread.Sleep(7000);
                Task.WaitAny(
                    new Task[] { deleteDevices(opts.DeviceCount, opts.IotHubConnectionString) }
                    );
            });
        }
 
        static async Task startClient(string IoTHub, string IoTDevicePrefix, int deviceNumber, string commonKey, int maxMessages, int messageSize)
        {
            string connectionString = "HostName=" + IoTHub + ";DeviceId=" + IoTDevicePrefix + deviceNumber + ";SharedAccessKey=" + commonKey;
            DeviceClient device = DeviceClient.CreateFromConnectionString(connectionString, Microsoft.Azure.Devices.Client.TransportType.Mqtt);
            await device.OpenAsync();
            int mycounter = 1;
            Console.WriteLine("Device " + IoTDevicePrefix + deviceNumber + " started");
 
            while (mycounter <= maxMessages)
            {
                var IoTMessage = new Microsoft.Azure.Devices.Client.Message(new byte[messageSize]);
                await device.SendEventAsync(IoTMessage);
                mycounter++;
            }
            await device.CloseAsync();
            Console.WriteLine("Device " + IoTDevicePrefix + deviceNumber + " ended");
        }
 
        static void createDevices(int number, string iotHubConnectionString)
        {
            for (int i = 1; i <= number; i++)
            {
                var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
                Device mydevice = new Device(IoTDevicePrefix + i.ToString());
                mydevice.Authentication = new AuthenticationMechanism();
                mydevice.Authentication.SymmetricKey.PrimaryKey = commonKey;
                mydevice.Authentication.SymmetricKey.SecondaryKey = commonKey;
                try
                {
                    registryManager.AddDeviceAsync(mydevice).Wait();
                    Console.WriteLine("Adding device: " + IoTDevicePrefix + i.ToString());
                }
                catch (Exception er)
                {
                    Console.WriteLine("  Error adding device: " + IoTDevicePrefix + i.ToString() + " error: " + er.InnerException.Message);
                }
            }
 
        }
        static async Task deleteDevices(int number, string iotHubConnectionString)
        {
            for (int i = 1; i <= number; i++)
            {
                var registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
 
                try
                {
                    Device mydevice = await registryManager.GetDeviceAsync(IoTDevicePrefix + i.ToString());
                    registryManager.RemoveDeviceAsync(mydevice).Wait();
                    Console.WriteLine("Deleting device " + IoTDevicePrefix + i.ToString());
                }
                catch (Exception er) {
                    Console.WriteLine("  Error deleting device: " + IoTDevicePrefix + i.ToString() + " error: " + er.InnerException.Message);
                }
            } 
        }
    }
}
