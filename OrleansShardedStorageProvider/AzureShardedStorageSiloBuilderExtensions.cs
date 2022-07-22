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
    public static class AzureShardedStorageSiloBuilderExtensions
    {
        /// <summary>
        /// Configure silo to use azure ShardedTable storage as the default grain storage.
        /// </summary>
        public static ISiloHostBuilder AddAzureShardedGrainStorageAsDefault(this ISiloHostBuilder builder, Action<AzureShardedStorageOptions> configureOptions)
        {
            return builder.AddAzureShardedGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use azure ShardedTable storage for grain storage.
        /// </summary>
        public static ISiloHostBuilder AddAzureShardedGrainStorage(this ISiloHostBuilder builder, string name, Action<AzureShardedStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAzureShardedGrainStorage(name, ob => ob.Configure(configureOptions)));
        }


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


        ///// <summary>
        ///// Configure silo to use azure ShardedTable storage for grain storage.
        ///// </summary>
        //public static ISiloBuilder AddAzureShardedTableGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureFileStorageOptions>> configureOptions = null)
        //{
        //    return builder.ConfigureServices(services => services.AddAzureShardedTableGrainStorage(name, configureOptions));
        //}

        internal static IServiceCollection AddAzureShardedGrainStorage(this IServiceCollection services, string name,
            Action<OptionsBuilder<AzureShardedStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<AzureShardedStorageOptions>(name));
            services.ConfigureNamedOptionForLogging<AzureShardedStorageOptions>(name);
            services.TryAddSingleton<IGrainStorage>(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
            return services.AddSingletonNamedService<IGrainStorage>(name, AzureShardedGrainStorageFactory.Create)
                           .AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }
    }
}
