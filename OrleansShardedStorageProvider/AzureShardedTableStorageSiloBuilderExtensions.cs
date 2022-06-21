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
    public static class AzureShardedTableStorageSiloBuilderExtensions
    {
        /// <summary>
        /// Configure silo to use azure ShardedTable storage as the default grain storage.
        /// </summary>
        public static ISiloHostBuilder AddAzureShardedTableGrainStorageAsDefault(this ISiloHostBuilder builder, Action<AzureShardedTableStorageOptions> configureOptions)
        {
            return builder.AddAzureShardedTableGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use azure ShardedTable storage for grain storage.
        /// </summary>
        public static ISiloHostBuilder AddAzureShardedTableGrainStorage(this ISiloHostBuilder builder, string name, Action<AzureShardedTableStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAzureShardedTableGrainStorage(name, ob => ob.Configure(configureOptions)));
        }


        /// <summary>
        /// Configure silo to use azure ShardedTable storage as the default grain storage.
        /// </summary>
        public static ISiloBuilder AddAzureShardedTableGrainStorageAsDefault(this ISiloBuilder builder, Action<AzureShardedTableStorageOptions> configureOptions)
        {
            return builder.AddAzureShardedTableGrainStorage(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME, configureOptions);
        }

        /// <summary>
        /// Configure silo to use azure ShardedTable storage for grain storage.
        /// </summary>
        public static ISiloBuilder AddAzureShardedTableGrainStorage(this ISiloBuilder builder, string name, Action<AzureShardedTableStorageOptions> configureOptions)
        {
            return builder.ConfigureServices(services => services.AddAzureShardedTableGrainStorage(name, ob => ob.Configure(configureOptions)));
        }


        ///// <summary>
        ///// Configure silo to use azure ShardedTable storage for grain storage.
        ///// </summary>
        //public static ISiloBuilder AddAzureShardedTableGrainStorage(this ISiloBuilder builder, string name, Action<OptionsBuilder<AzureFileStorageOptions>> configureOptions = null)
        //{
        //    return builder.ConfigureServices(services => services.AddAzureShardedTableGrainStorage(name, configureOptions));
        //}

        internal static IServiceCollection AddAzureShardedTableGrainStorage(this IServiceCollection services, string name,
            Action<OptionsBuilder<AzureShardedTableStorageOptions>> configureOptions = null)
        {
            configureOptions?.Invoke(services.AddOptions<AzureShardedTableStorageOptions>(name));
            services.ConfigureNamedOptionForLogging<AzureShardedTableStorageOptions>(name);
            services.TryAddSingleton<IGrainStorage>(sp => sp.GetServiceByName<IGrainStorage>(ProviderConstants.DEFAULT_STORAGE_PROVIDER_NAME));
            return services.AddSingletonNamedService<IGrainStorage>(name, AzureFileGrainStorageFactory.Create)
                           .AddSingletonNamedService<ILifecycleParticipant<ISiloLifecycle>>(name, (s, n) => (ILifecycleParticipant<ISiloLifecycle>)s.GetRequiredServiceByName<IGrainStorage>(n));
        }
    }
}
