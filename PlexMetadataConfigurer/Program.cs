using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlexMetadataConfigurer;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
	.AddEnvironmentVariables(prefix: "PlexConfigurer_");

builder.Services.AddHostedService<PlexConfigurerService>();

using IHost host = builder.Build();
await host.RunAsync();