﻿using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Runtime;
using OrleansShardedStorageProvider.Providers;

namespace OrleansShardedStorageProvider.Storage
{
	public class AzureShardedGrainStorageBase
	{
		protected readonly string _serviceId;
		protected readonly AzureShardedStorageOptions _options;

		public AzureShardedGrainStorageBase(IOptions<ClusterOptions> clusterOptions, AzureShardedStorageOptions options)
		{
			_serviceId = clusterOptions.Value.ServiceId;
			_options = options;
		}

		public int GetShardNumberFromKey(string pk)
		{
			var hash = GetStableHashCode(pk);
			var storageNum = Math.Abs(hash % this._options.ConnectionStrings.Count());

			return storageNum;
		}

		/// <summary>
		/// Take from https://stackoverflow.com/a/36845864/852806
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		protected int GetStableHashCode(string str)
		{
			unchecked
			{
				int hash1 = 5381;
				int hash2 = hash1;

				for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1 || str[i + 1] == '\0')
						break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + (hash2 * 1566083941);
			}
		}


		protected const string KeyStringSeparator = "__";

		public string GetKeyStringSeparator()
		{
			return KeyStringSeparator;
		}

		protected string GetKeyString(GrainId grainId)
		{
			var key = $"{this._serviceId}{KeyStringSeparator}{grainId.ToString()}";

			return SanitizeTableProperty(key);
		}

		protected string SanitizeTableProperty(string key)
		{
			// Remove any characters that can't be used in Azure PartitionKey or RowKey values
			// http://www.jamestharpe.com/web-development/azure-table-service-character-combinations-disallowed-in-partitionkey-rowkey/
			key = key
				.Replace('/', '_')        // Forward slash
				.Replace('\\', '_')       // Backslash
				.Replace('#', '_')        // Pound sign
				.Replace('?', '_');       // Question mark

			if (key.Length >= 1024)
				throw new ArgumentException(string.Format("Key length {0} is too long to be an Azure table key. Key={1}", key.Length, key));

			return key;
		}
	}

}
