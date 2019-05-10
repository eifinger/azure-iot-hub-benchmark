#*********************************************************
#
# This code is licensed under the MIT License (MIT).
# THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
# ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
# IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
# PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
# Author: k.eifinger@googlemail.com
#
#*********************************************************

param
(

    [Parameter(Mandatory = $True)]
    [string] $ResourceGroupName,

    [Parameter(Mandatory = $True)]
    [ValidateSet("Australia East", "East US", "East US 2", "South Central US", "Southeast Asia", "West Europe", "West Central US", "West US 2")]
    [string] $Location,

    [Parameter(Mandatory = $True)]
    [ValidateSet("B1", "B2", "B3", "S1", "S2", "S3")]
    [string] $Sku,

    [Parameter(Mandatory = $True)]
    [int16] $PartitionCount
)

$ErrorActionPreference = "Stop"

###########################################################################
#
# Connect-AzureSubscription - gets current Azure context or triggers a 
# user log in to Azure. Selects the Azure subscription for creation of 
# the IoT Hub
# 
Function Connect-AzureSubscription() {
    # Ensure the user is logged in
    try {
        $subscription = Get-AzSubscription
        $azureContext = Set-AzContext -SubscriptionId $subscription
    }
    catch {
    }

    if (!$azureContext -or !$azureContext.Account) {
        Write-Host "Please login to Azure..."
        Connect-AzAccount
        $subscription = Get-AzSubscription
        Set-AzContext -SubscriptionId $subscription
    }
}

###########################################################################
#
# Main 
# 

Write-Host "Starting Benchmark"

Connect-AzureSubscription

Write-Host "Creating IoT Hub"
$iotHubName = Invoke-Expression "$PSScriptRoot\create-iot-hub.ps1 -ResourceGroupName '$ResourceGroupName' -Location '$Location' -Sku $Sku -PartitionCount $PartitionCount"

$connectionString = $(Get-AzIotHubConnectionString -ResourceGroupName $ResourceGroupName -Name $iotHubName -KeyName "iothubowner").PrimaryKey

Write-Host "Starting CSharp Benchmark"

Start-Process -FilePath "dotnet" -WorkingDirectory "$PSScriptRoot" -ArgumentList "$PSScriptRoot\csharp\azure-iot-hub-benchmark\bin\Debug\netcoreapp2.2\azure-iot-hub-benchmark.dll --iothubconnectionstring $connectionString" -NoNewWindow

Invoke-Expression "$PSScriptRoot\delete-resource-group.ps1 -ResourceGroupName '$ResourceGroupName'"