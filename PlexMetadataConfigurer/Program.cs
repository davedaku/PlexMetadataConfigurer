using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlexMetadataConfigurer;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

#if DEBUG
builder.Configuration.AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: false);
#endif

builder.Configuration
	.AddEnvironmentVariables(prefix: "PlexConfigurer_") // todo: have not tested this, might need to lose the prefix
	.AddCommandLine(args);

builder.Services.AddHostedService<PlexConfigurerService>();

using IHost host = builder.Build();
await host.RunAsync();