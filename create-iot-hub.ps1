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
# Confirm-Create - confirms that the user wants to continue with the 
# creation of the Hub
# 
Function Confirm-Create() {
    Write-Host @"
    
You are about to create an Azure IoT Hub:
    - Subscription $SubscriptionId ($subName)
    - Resource group $ResourceGroupName
    - Location '$Location'
    - Sku $Sku
    - PartitionCount $PartitionCount

Are you sure you want to continue?
"@

    while ($True) {
        $answer = Read-Host @"
    [Y] Yes [N] No (default is "Y")
"@

        switch ($Answer) {
            "Y" { return }
            "" { return }
            "N" { exit }
        }
    }
}

###########################################################################
#
# Invoke-ArmDeployment - Uses the .\iothubparams.json template to 
# create an IoT hub.  Returns the name of the hub
# 
Function Invoke-ArmDeployment($resourceGroup, $deploymentName) {
    # Submit the ARM template deployment
    if ($Sku.StartsWith("B")){
        $SkuTier = "Basic"
    } else {
        $SkuTier = "Standard"
    }
    $params = @{
        "location" = $Location
        "sku" = $Sku
        "partitioncount" = $PartitionCount
        "skutier" = $SkuTier
        "hubname" = $deploymentName
    }

    Write-Host @"
`nStarting deployment of the Azure IoT Hub which may take a while.
Progress can be monitored from the Azure Portal (http://portal.azure.com).
    1. Find the resource group $ResourceGroupName in $SubscriptionId ($subName) subscription.
    2. In the Deployments page open deployment $deploymentName.
"@

    $deployment = New-AzResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroup.ResourceGroupName -TemplateFile '.\iothubparams.json' -TemplateParameterObject $params

    Write-Host @"
`nThe hub is ready

    Subscription      :  $SubscriptionId ($subName)
    Resource group    :  $ResourceGroupName
    IoT Hub name      :  $($deployment.Outputs.hubName.value)

"@
return $deployment.Outputs.hubName.value
}

###########################################################################
#
# Get-ResourceGroup - Finds or creates the resource group to be used by the
# deployment.
# 
Function Get-ResourceGroup() {
    # Get or create resource group
    $rg = Get-AzResourceGroup $ResourceGroupName -ErrorAction Ignore
    if (!$rg) {
        $rg = New-AzResourceGroup $ResourceGroupName -Location $Location
    }
    return $rg
}

###########################################################################
#
# Create-Unique-Name - Create a name until the iothub does not already exist
# 
Function Get-UniqueName() {

    $randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_})
    $deploymentName = "BenchmarktestIoTHub-$randomSuffix"
    try{
    $iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $deploymentName
    } catch{}
    while($iotHub){
        $randomSuffix = -join ((65..90) + (97..122) | Get-Random -Count 5 | ForEach-Object {[char]$_})
        $deploymentName = "BenchmarktestIoTHub-$randomSuffix"
        $iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $deploymentName
    }
    return $deploymentName
}


###########################################################################
#
# Main 
# 

Connect-AzureSubscription
$subName = $(Get-AzContext).Subscription.Name

$SubscriptionId = Get-AzSubscription

Confirm-Create

$resourceGroup = Get-ResourceGroup

$deploymentName = Get-UniqueName

$iotHubName = Invoke-ArmDeployment $resourceGroup $deploymentName
return $iotHubName