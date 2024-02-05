using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OrleansShardedStorageProvider.Providers;

namespace OrleansShardedStorageProvider.Storage
{

	/// <summary>
	/// Factory used to create instances of AzureShardedStorage
	/// </summary>
	public static class AzureShardedGrainStorageFactory
	{
		/// <summary>
		/// Creates a grain storage instance.
		/// </summary>
		public static AzureShardedGrainStorage Create(IServiceProvider services, string name)
		{
			var optionsMonitor = services.GetRequiredService<IOptionsMonitor<AzureShardedStorageOptions>>();
			var grainStorage = ActivatorUtilities.CreateInstance<AzureShardedGrainStorage>(services, name, optionsMonitor.Get(name));
			return grainStorage;
		}
	}

}
