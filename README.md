# Azure IoTHub Benchmark

WIP Benchmark the troughput of the Azure IoTHub and figure out which variables affect it

## Usage

### Prerequisites

#### Linux

1. Install Powershell Core: https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-linux?view=powershell-6

#### All

1. If not already done install dotnet core SDK: https://dotnet.microsoft.com/download
1. If not already done install the Azure Powershell Module

```powershell
Install-Module -Name Az -AllowClobber
```

### Clone and Build

```powershell
git clone https://github.com/eifinger/azure-iot-hub-benchmark.git
cd azure-iot-hub-benchmark
cd csharp
dotnet publish
cd ..
```

### (Optional)

If not already done install the Azure Powershell Module

```powershell
Install-Module -Name Az -AllowClobber
```

### Configure and Run

1. Edit the file **run-benchmark.ps1** to include/exclude the combination you want to benchmark
1. Run the Powershell Script

```powershell
.\run-all-benchmarks.ps1
```

1. The Results are stored under the directory **benchmark_results**
1. The files are named in the following pattern *benchmark_\<location\>\_\<sku\>\_\<partitionCount\>\_\<deviceCount\>\_\<maxMessages\>\_\<messageSize\>\_\<Timestamp\>.json*