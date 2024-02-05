using Orleans.Runtime;
using OrleansShardedStorageProvider.Storage;

namespace OrleansShardedStorageProvider.Providers
{

	internal class AzureShardedStorageOptionsValidator : IConfigurationValidator
	{
		private readonly AzureShardedStorageOptions _options;
		private readonly string _name;

		public AzureShardedStorageOptionsValidator(AzureShardedStorageOptions options, string name)
		{
			_options = options;
			_name = name;
		}

		public void ValidateConfiguration()
		{
			if (_options.ConnectionStrings == null)
			{
				throw new OrleansConfigurationException($"Invalid configuration for {nameof(AzureShardedGrainStorage)} with name {_name}. {nameof(AzureShardedStorageOptions)}.{nameof(_options.ConnectionStrings)} are required.");
			}
		}
	}

}
