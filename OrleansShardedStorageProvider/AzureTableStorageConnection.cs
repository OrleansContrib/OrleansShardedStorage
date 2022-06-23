namespace OrleansShardedStorageProvider
{
    public class AzureTableStorageConnection
    {
        public AzureTableStorageConnection()
        {
        }

        public AzureTableStorageConnection(string accountName, string sasToken)
            :this(accountName, sasToken, "OrleansGrainStateSharded")
        {
        }

        public AzureTableStorageConnection(string accountName, string sasToken, string tableName)
        {
            AccountName = accountName;
            BaseTableUri = new Uri($"https://{accountName}.table.core.windows.net/");
            SasToken = sasToken;
            TableName = tableName;
            TableStorageUri = new Uri($"https://{accountName}.table.core.windows.net/{tableName}");
        }

        public Uri BaseTableUri { get; set; }
        public string AccountName { get; set; }
        public string SasToken { get; set; }
        public string TableName { get; set; }
        public Uri TableStorageUri { get; set; }
    }
}
