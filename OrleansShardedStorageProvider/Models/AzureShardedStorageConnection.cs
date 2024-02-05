using Azure;

namespace OrleansShardedStorageProvider.Models
{

	public class AzureShardedStorageConnection
	{
		private static string TableStorageNameDefault = "OrleansGrainStateSharded";
		private static string BlobStorageNameDefault = "grainstatesharded";

		public AzureShardedStorageConnection()
		{

		}

		public AzureShardedStorageConnection(string accountName, string sasToken,
			StorageType storageType = StorageType.TableStorage)
			: this(accountName, sasToken, null, storageType)
		{
		}

		public AzureShardedStorageConnection(string accountName,
			string sasToken,
			string tableOrContainerName,
			StorageType storageType = StorageType.TableStorage)
		{
			if (String.IsNullOrWhiteSpace(tableOrContainerName))
			{
				if (storageType == StorageType.BlobStorage)
				{
					tableOrContainerName = BlobStorageNameDefault;
				}
				else
				{
					tableOrContainerName = TableStorageNameDefault;
				}
			}

			AccountName = accountName;
			BaseTableUri = new Uri($"https://{accountName}.table.core.windows.net/");
			BaseBlobUri = new Uri($"https://{accountName}.blob.core.windows.net/");
			SasToken = sasToken;
			TableOrContainerName = tableOrContainerName;
			TableStorageUri = new Uri($"https://{accountName}.table.core.windows.net/{tableOrContainerName}");
			StorageType = storageType;
		}

		public Uri GetBaseUri()
		{
			return this.StorageType == StorageType.TableStorage ?
				this.BaseTableUri : this.BaseBlobUri;
		}

		/// <summary>
		/// The base table storage URI (Does not include table name)
		/// </summary>
		public Uri BaseTableUri { get; set; }

		/// <summary>
		/// The base blob location (does not include container)
		/// </summary>
		public Uri BaseBlobUri { get; set; }

		/// <summary>
		/// The storage account name e.g. 'storage1'
		/// </summary>
		public string AccountName { get; set; }

		/// <summary>
		/// The SaS token used to access table or blob storage
		/// </summary>
		public string SasToken { get; set; }

		/// <summary>
		/// AzureSasCredential generated from SaSToken
		/// </summary>
		public AzureSasCredential SasCredential
		{
			get
			{
				return new AzureSasCredential(SasToken);
			}
		}

		/// <summary>
		/// For table storage, the table name
		/// For blob storage, the container name
		/// </summary>
		public string TableOrContainerName { get; set; }

		/// <summary>
		/// The full table storage URI (including table name)
		/// </summary>
		public Uri TableStorageUri { get; set; }


		/// <summary>
		/// The type of storage
		/// (this class can handle connections to table or blob storage)
		/// </summary>
		public StorageType StorageType { get; set; } = StorageType.TableStorage;
	}


	public enum StorageType
	{
		TableStorage = 0,
		BlobStorage = 1,
	}
}
