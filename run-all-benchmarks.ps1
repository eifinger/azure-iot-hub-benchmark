# $Locations = "Australia East", "East US", "East US 2", "South Central US", "Southeast Asia", "West Europe", "West Central US", "West US 2"
$Locations = "West Europe"
#$Skus = "B1", "B2", "B3", "S1", "S2", "S3"
$Skus = "B2", "B3"
#$PartitionCounts = 4, 8, 16, 32
$PartitionCounts = 4, 32
#$DeviceCounts = 10, 20, 40, 80, 160, 320
$DeviceCounts = 10, 20, 40, 80
#$MaxMessages = 10, 20, 40, 80, 160, 320
$MaxMessages = 10, 20, 40, 80, 160
#$MessageSizes = 50, 100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600
$MessageSizes = 50, 100, 800, 1600, 6400
$TransportTypes = "MQTT", "AMQP", "HTTP"

$ResourceGroupName = "BenchmarkTest"

foreach($location in $Locations)
{
    foreach($sku in $Skus)
    {
        foreach($partitionCount in $PartitionCounts)
        {
            foreach($deviceCount in $DeviceCounts)
            {
                foreach($maxMessages in $MaxMessages)
                {
                    foreach($messageSize in $MessageSizes)
                    {
                        foreach($transportType in $TransportTypes)
                        {
                                                    Write-Host @"
`nStarting Benchmark for:

    Location      :  $location
    ResourceGroup :  $ResourceGroupName
    Sku           :  $sku
    PartitionCunt :  $partitionCount
    DeviceCount   :  $deviceCount
    MaxMessages   :  $maxMessages
    MessagesSize  :  $messageSize
    TransportType :  $transportType

"@
                        Invoke-Expression "$PSScriptRoot\run-benchmark.ps1 -ResourceGroupName '$ResourceGroupName' -Location '$location' -Sku $sku -PartitionCount $partitionCount -DeviceCount $deviceCount -MaxMessages $maxMessages -MessageSize $messageSize -TransportType $transportType -Force"
                        }
                   }
                }
            }
        }
    }
}