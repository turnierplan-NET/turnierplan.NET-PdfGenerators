using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
using Turnierplan.Adapter;
using Turnierplan.PdfGenerators.Common;
using Turnierplan.PdfGenerators.Console.Options;
using Turnierplan.PdfGenerators.QrCodes;

Console.WriteLine();
Console.WriteLine( "  __                                                     ___                                        __");
Console.WriteLine(@" /\ \__                        __                       /\_ \                                      /\ \__");
Console.WriteLine(@" \ \ ,_\  __  __  _ __    ___ /\_\     __   _ __   _____\//\ \      __      ___         ___      __\ \ ,_\");
Console.WriteLine(@"  \ \ \/ /\ \/\ \/\`'__\/' _ `\/\ \  /'__`\/\`'__\/\ '__`\\ \ \   /'__`\  /' _ `\     /' _ `\  /'__`\ \ \/");
Console.WriteLine(@"   \ \ \_\ \ \_\ \ \ \/ /\ \/\ \ \ \/\  __/\ \ \/ \ \ \L\ \\_\ \_/\ \L\.\_/\ \/\ \  __/\ \/\ \/\  __/\ \ \_");
Console.WriteLine(@"    \ \__\\ \____/\ \_\ \ \_\ \_\ \_\ \____\\ \_\  \ \ ,__//\____\ \__/.\_\ \_\ \_\/\_\ \_\ \_\ \____\\ \__\");
Console.WriteLine(@"     \/__/ \/___/  \/_/  \/_/\/_/\/_/\/____/ \/_/   \ \ \/ \/____/\/__/\/_/\/_/\/_/\/_/\/_/\/_/\/____/ \/__/");
Console.WriteLine(@"                                                     \ \_\");
Console.WriteLine(@"                                                      \/_/   turnierplan.NET PDF Generators");
Console.WriteLine();

QuestPDF.Settings.License = LicenseType.Community;

// Switch base path if we are running from 'bin/Release/net10.0' folder
var basePath = File.Exists(Path.Join(Directory.GetCurrentDirectory(), "../../../appsettings.json"))
    ? Path.Join(Directory.GetCurrentDirectory(), "../../../")
    : Directory.GetCurrentDirectory();

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(basePath)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables();

await using var serviceProvider = new ServiceCollection().AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Trace)).BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger<Program>();

var configuration = configurationBuilder.Build();

var adapterOptions = configuration.GetSection(nameof(TurnierplanAdapterOptions)).Get<TurnierplanAdapterOptions>();

if (adapterOptions is null || string.IsNullOrWhiteSpace(adapterOptions.InstanceUrl) || string.IsNullOrWhiteSpace(adapterOptions.ApiKey) || string.IsNullOrWhiteSpace(adapterOptions.ApiKeySecret))
{
    Console.WriteLine("Provide the 'InstanceUrl', 'ApiKey' and 'ApiKeySecret' settings via 'appsettings.json', environment variable or .NET user secrets");
    return;
}

var clientOptions = new TurnierplanClientOptions(adapterOptions.InstanceUrl, adapterOptions.ApiKey, adapterOptions.ApiKeySecret);
logger.LogInformation("Adapter configuration has been read successfully");

Type[] knownGenerators =
[
    typeof(QrCodesGenerator)
];

foreach (var generatorType in knownGenerators)
{
    var generatorName = generatorType.Name;
    var relevantSection = configuration.GetSection("Generators").GetSection(generatorName);
    var isEnabled = relevantSection.GetValue<bool>("IsEnabled");

    if (!isEnabled)
    {
        logger.LogInformation("The generator '{generatorName}' is not enabled by configuration and will be skipped", generatorName);
        continue;
    }

    if (Activator.CreateInstance(generatorType) is not IGenerator generatorInstance)
    {
        logger.LogError($"An instance of '{{generatorType}}' cannot be created or is not assignable to {nameof(IGenerator)}.", generatorType.FullName);
        continue;
    }

    logger.LogInformation("Running generator '{generatorName}'...", generatorName);

    await generatorInstance.RunAsync(clientOptions, relevantSection.GetSection("Options"), loggerFactory);
}
