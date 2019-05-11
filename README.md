# Azure IoTHub Benchmark

WIP Benchmark the troughput of the Azure IoTHub and figure out which variables affect it

## Usage

### Clone and Build

```powershell
git clone https://github.com/eifinger/azure-iot-hub-benchmark.git
cd azure-iot-hub-benchmark
cd csharp
dotnet publish
cd ..
```

### Configure and Run

1. Edit the file **run-benchmark.ps1** to include/exclude the combination you want to benchmark
1. Run the Powershell Script

```powershell
.\run-all-benchmarks.ps1
```

1. The Results are stored under the directory **benchmark_results**
1. The files are named in the following pattern *benchmark_\<location\>\_\<sku\>\_\<partitionCount\>\_\<deviceCount\>\_\<maxMessages\>\_\<messageSize\>\_\<Timestamp\>.json*