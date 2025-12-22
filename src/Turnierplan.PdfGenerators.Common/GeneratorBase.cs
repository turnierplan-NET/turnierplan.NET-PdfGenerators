using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Turnierplan.Adapter;

namespace Turnierplan.PdfGenerators.Common;

public interface IGenerator
{
    Task RunAsync(TurnierplanClientOptions clientOptions, IConfigurationSection configuration, ILoggerFactory loggerFactory);
}

public abstract class GeneratorBase<TOptions> : IGenerator
    where TOptions : GeneratorOptionsBase
{
    protected string InstanceUrl
    {
        get => field ?? throw new InvalidOperationException();
        private set;
    }

    protected ILogger<GeneratorBase<TOptions>> Logger
    {
        get => field ?? throw new InvalidOperationException();
        private set;
    }

    public async Task RunAsync(TurnierplanClientOptions clientOptions, IConfigurationSection configuration, ILoggerFactory loggerFactory)
    {
        InstanceUrl = clientOptions.ApplicationUri.ToString();
        Logger = loggerFactory.CreateLogger<GeneratorBase<TOptions>>();

        var client = new TurnierplanClient(clientOptions);

        var generatorOptions = configuration.Get<TOptions>();

        if (generatorOptions is null)
        {
            Logger.LogError("Failed to get configuration as '{configurationType}'", typeof(TOptions).FullName);
            return;
        }

        await RunAsync(client, generatorOptions);
    }

    protected abstract Task RunAsync(TurnierplanClient client, TOptions options);

    protected void CreateDocument(Action<IDocumentContainer> handler)
    {
        var document = Document.Create(handler);
    }
}
