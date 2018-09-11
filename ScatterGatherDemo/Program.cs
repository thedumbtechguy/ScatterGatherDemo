using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScatterGatherDemo.Options;
using Serilog;
using StackExchange.Redis;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ScatterGatherDemo
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).RunConsoleAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return HostingHostBuilderExtensions.ConfigureLogging(new HostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureHostConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("hostsettings.json", optional: true);
                    config.AddEnvironmentVariables(prefix: "NETCORE_");

                    if(args != null)
                    {
                        config.AddCommandLine(args);
                    }
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                 {
                     var env = hostingContext.HostingEnvironment;

                     config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: false, reloadOnChange: true);

                     config.AddEnvironmentVariables();

                     if(args != null)
                     {
                         config.AddCommandLine(args);
                     }

                     // Extra configuration specified via env. Can be used to specify docker secrets path.
                     var extraConfig = Environment.GetEnvironmentVariable("EXTRA_CONFIG_FILE");
                     if(!string.IsNullOrWhiteSpace(extraConfig))
                     {
                         config.AddJsonFile(extraConfig, optional: false);
                     }
                 }), (hostingContext, logging) =>
                 {
                     // Configure serilog pipelines
                     Log.Logger = new LoggerConfiguration()
                                  .MinimumLevel.Debug()
                                  .Enrich.FromLogContext()
                                  .ReadFrom.Configuration(hostingContext.Configuration)
                                  .CreateLogger();

                     logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                     logging.AddConsole();
                     logging.AddDebug();
                     logging.AddSerilog(dispose: true);
                 })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddOptions()
                        .Configure<RedisOptions>(context.Configuration.GetSection("Redis"));

                    services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
                    {
                        var opts = serviceProvider.GetRequiredService<IOptions<RedisOptions>>();
                        return ConnectionMultiplexer.Connect(opts.Value.ConnectionString);
                    });
                })
                .ConfigureContainer<ContainerBuilder>((context, builder) =>
                {
                    builder
                        .RegisterModule<Module>();

                    builder
                        .RegisterAssemblyTypes(typeof(Program).Assembly)
                        .Where(type => !type.IsAssignableTo<IHostedService>())
                        .AsImplementedInterfaces();

                    builder
                        .RegisterType<App>()
                        .As<IHostedService>()
                        .SingleInstance();
                });
        }
    }
}