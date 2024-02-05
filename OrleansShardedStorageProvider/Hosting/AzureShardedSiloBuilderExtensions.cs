using Microsoft.Extensions.Options;
using Orleans.Providers;
using OrleansShardedStorageProvider.Providers;

namespace OrleansShardedStorageProvider.Hosting
{

	/// <summary>
	/// <see cref="ISiloBuilder"/> extensions.
	/// </summary>
	public static class AzureShardedSiloBuilderExtensions
	{
		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static ISiloBuilder AddShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<AzureShardedStorageOptions> configureOptions)
		{
			return builder.AddShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static ISiloBuilder AddShardedGrainStorage(this ISiloBuilder builder, string name, Action<AzureShardedStorageOptions> configureOptions)
		{
			return builder.ConfigureServices(services => services.AddShardedGrainStorage(name, configureOptions));
		}

		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static ISiloBuilder AddShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			return builder.AddShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static ISiloBuilder AddShardedGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			return builder.ConfigureServices(services => services.AddShardedGrainStorage(name, configureOptions));
		}
	}


}
