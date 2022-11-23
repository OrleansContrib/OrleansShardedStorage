$subscriptionName = "<your subscription>"
$resourceGroupName = "<your resource group>"   # The resource group so we keep resources separate
$location = "uksouth"

#storage account name must be between 3 and 24 characters and use numbers and lower-case letters only
$storAccMainNameBase="<basename e.g. orleansstorage>"
$storAcctStartNum = 1 # IMPORTANT: The storage number to start at
$storAcctNumEndNum = 6 # IMPORTANT: The storage number to end at

# NOTE: PREMIUM STORAGE WOULD PERFORM BETTER! _LRS is not redundant! https://docs.microsoft.com/en-us/rest/api/storagerp/srp_sku_types
#single quotation is used as these are azure specific parameters
$redundancyType= 'Standard_LRS' #'Standard_RAGRS' 
$storageType= 'StorageV2'
$accessTier = 'Hot'


# Standard script from here -----------------------

# 1. Test connecting to your account
Connect-AzAccount
Get-AzSubscription -SubscriptionName $subscriptionName | Select-AzSubscription


$config = ""

# Create resrouce group if it doesn't exist
$rg = Get-AzResourceGroup -Name $resourceGroupName -Location $location -ErrorAction Ignore
if(!$rg){
    New-AzResourceGroup -Name $resourceGroupName -Location $location
}

# Create a list of storage accounts
For($i = $storAcctStartNum; $i -le $storAcctNumEndNum; $i++)
{
    $storAcctNum = $i
    $storAccMainName="$storAccMainNameBase$storAcctNum"

    # Create storage account
    $storAccMainAccount = Get-AzStorageAccount -ResourceGroupName $resourceGroupName -Name  $storAccMainName -ErrorAction Ignore
    if(!$storAccMainAccount){
        $storAccMainAccount = New-AzStorageAccount -ResourceGroupName $resourceGroupName `
                                               -Name  $storAccMainName `
                                               -Location $location `
                                               -SkuName $redundancyType `
                                               -Kind $storageType `
                                               -AccessTier $accessTier `
    }

    # Get the Sas token
    $context = (Get-AzStorageAccount -ResourceGroupName $resourceGroupName -AccountName $storAccMainName).context
    $storageSasToken = New-AzStorageAccountSASToken -Context $context -Service Blob,File,Table,Queue -ResourceType Service,Container,Object -Permission "racwdlup"

    # Add to to the config string
    $config = $config + "{
        `"Name`": `"$storAccMainName`",
        `"SasToken`": `"$storageSasToken`"
      },`r`n"
}


Write-Verbose  "Copy this config to appsettings/secrets.json (in TableStorageAccounts or BlobStorageAccounts fields as required)" -Verbose
Write-Verbose "Beware the final comma. Remove if needed!" -Verbose
""
$config 

Write-Verbose "Copy the config above!" -Verbose