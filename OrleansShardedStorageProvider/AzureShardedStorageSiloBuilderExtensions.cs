using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Runtime;
using Orleans.Storage;

namespace OrleansShardedStorageProvider
{
    /// <remarks>
    /// Origin: https://github.com/JsAndDotNet/OrleansShardedStorage
    /// Ref: Oreleans:src\Azure\Orleans.Persistence.AzureStorage\Hosting\AzureTableSiloBuilderExtensions.cs
    /// </remarks>
    public static class AzureShardedStorageSiloBuilderExtensions
    {
        /// <summary>
        /// Configure silo to use azure ShardedTable storage as the default grain storage.
        /// </summary>
        public static ISiloBuilder AddAzureShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<AzureShardedStorageOptions> configureOptions)
        {         
            return builder.AddAzureShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use azure ShardedTable storage for grain storage.
        /// </summary>
        public static ISiloBuilder AddAzureShardedGrainStorage(this ISiloBuilder builder, string name, Action<AzureShardedStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAzureShardedGrainStorage(name, ob => ob.Configure(configureOptions)));
        }


        /// <summary>
        /// Configure silo to use azure ShardedTable storage as the default grain storage.
        /// </summary>
        /// 
        public static ISiloBuilder AddAzureShardedGrainStorageAsDefault(this ISiloBuilder builder, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
        {
            return builder.AddAzureShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }


        /// <summary>
        /// Configure silo to use azure ShardedTable storage for grain storage.
        /// </summary>
        public static ISiloBuilder AddAzureShardedGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
        {
            return builder.ConfigureServices(services => services.AddAzureShardedGrainStorage(name, configureOptions));
        }


        internal static IServiceCollection AddAzureShardedGrainStorage(
            this IServiceCollection services,
            string name,
            Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<AzureShardedStorageOptions>(name));
           
            services.ConfigureNamedOptionForLogging<AzureShardedStorageOptions>(name);
            if (string.Equals(name, ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, StringComparison.Ordinal))
            {
                services.TryAddSingleton(sp => sp.GetKeyedServices<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
            }
            return services.AddSingletonNamedService<IGrainStorage>(name, AzureShardedGrainStorageFactory.Create)
                           .AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }

    }
}
