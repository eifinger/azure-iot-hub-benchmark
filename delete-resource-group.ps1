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
    [string] $ResourceGroupName
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
# Confirm-Delete - confirms that the user wants to continue with the 
# deletion of the RG
# 
Function Confirm-Delete() {
    Write-Host @"
    
You are about to delete the Resource Group:
    - Resource group $ResourceGroupName

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
# Main 
# 
Connect-AzureSubscription
Confirm-Delete
Write-Host "Removing Resource Group $ResourceGroupName and all Resources in it"
Remove-AzResourceGroup -Name $ResourceGroupName -Force
Write-Host "$ResourceGroupName successfully deleted"