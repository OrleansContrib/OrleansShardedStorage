using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Providers;
using Orleans.Runtime.Hosting;
using Orleans.Storage;
using OrleansShardedStorageProvider.Providers;
using OrleansShardedStorageProvider.Storage;

namespace OrleansShardedStorageProvider.Hosting
{


	/// <summary>
	/// <see cref="IServiceCollection"/> extensions.
	/// </summary>
	public static class AzureShardedGrainStorageServiceCollectionExtensions
	{
		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static IServiceCollection AddShardedGrainStorageAsDefault(this IServiceCollection services, Action<AzureShardedStorageOptions> configureOptions)
		{
			return services.AddShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, ob => ob.Configure(configureOptions));
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static IServiceCollection AddShardedGrainStorage(this IServiceCollection services, string name, Action<AzureShardedStorageOptions> configureOptions)
		{
			return services.AddShardedGrainStorage(name, ob => ob.Configure(configureOptions));
		}

		/// <summary>
		/// Configures Sharded as the default grain storage provider.
		/// </summary>
		public static IServiceCollection AddShardedGrainStorageAsDefault(this IServiceCollection services, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			return services.AddShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
		}

		/// <summary>
		/// Configures Sharded as a grain storage provider.
		/// </summary>
		public static IServiceCollection AddShardedGrainStorage(this IServiceCollection services, string name,
			Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
		{
			configureOptions?.Invoke(services.AddOptions<AzureShardedStorageOptions>(name));
			services.AddTransient<IConfigurationValidator>(sp => new AzureShardedStorageOptionsValidator(sp.GetRequiredService<IOptionsMonitor<AzureShardedStorageOptions>>().Get(name), name));
			services.AddTransient<IPostConfigureOptions<AzureShardedStorageOptions>, DefaultStorageProviderSerializerOptionsConfigurator<AzureShardedStorageOptions>>();
			services.ConfigureNamedOptionForLogging<AzureShardedStorageOptions>(name);
			return services.AddGrainStorage(name, AzureShardedGrainStorageFactory.Create);
		}
	}

}
