using Microsoft.Extensions.Options;
using Orleans.Providers;
using OrleansShardedStorageProvider.Providers;

namespace OrleansShardedStorageProvider.Hosting
{
	/// <remarks>
	/// Origin: https://github.com/JsAndDotNet/OrleansShardedStorage
	/// Ref: Oreleans:src\Azure\Orleans.Persistence.AzureStorage\Hosting\AzureTableSiloBuilderExtensions.cs
	/// </remarks>
	/// <summary>
	/// <see cref="ISiloBuilder"/> extensions.
	/// </summary>
	public static class AzureShardedSiloBuilderExtensions
	{
		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static ISiloBuilder AddAzureShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<AzureShardedStorageOptions> configureOptions)
		{
			return builder.AddAzureShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static ISiloBuilder AddAzureShardedGrainStorage(this ISiloBuilder builder, string name, Action<AzureShardedStorageOptions> configureOptions)
		{
			return builder.ConfigureServices(services => services.AddAzureShardedGrainStorage(name, configureOptions));
		}

		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static ISiloBuilder AddAzureShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			return builder.AddAzureShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static ISiloBuilder AddAzureShardedGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			return builder.ConfigureServices(services => services.AddAzureShardedGrainStorage(name, configureOptions));
		}
	}


}
