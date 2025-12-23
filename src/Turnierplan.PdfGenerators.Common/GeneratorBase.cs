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

public abstract class GeneratorBase : IGenerator
{
    /// <remarks>
    /// Static date time in non-generic base class so that all derived generators use the same timestamp when multiple generators are run at once.
    /// </remarks>
    private protected DateTime DateTime = DateTime.UtcNow;

    private protected GeneratorBase()
    {
    }

    public abstract Task RunAsync(TurnierplanClientOptions clientOptions, IConfigurationSection configuration, ILoggerFactory loggerFactory);
}

public abstract class GeneratorBase<TOptions> : GeneratorBase
    where TOptions : GeneratorOptionsBase
{
    private const string OutputDirectoryName = "output";

    private bool _runCalled;
    private int _documentIndex = 1;

    protected GeneratorBase()
    {
    }

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

    public override async Task RunAsync(TurnierplanClientOptions clientOptions, IConfigurationSection configuration, ILoggerFactory loggerFactory)
    {
        if (_runCalled)
        {
            throw new InvalidOperationException($"'{nameof(RunAsync)}()' may only be called once");
        }

        _runCalled = true;

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

        var fileSafeTimestamp = $"{DateTime:yyyy-MM-dd_HH-mm-ss}";
        var folder = Path.Join(Directory.GetCurrentDirectory(), OutputDirectoryName, fileSafeTimestamp);
        Directory.CreateDirectory(folder);

        var fileName = $"{GetType().Name}-{_documentIndex++}.pdf";
        var filePath = Path.Join(folder, fileName);
        using var stream = File.OpenWrite(filePath);

        document.GeneratePdf(stream);

        Logger.LogInformation("Wrote PDF to {pdfPath}", Path.GetFullPath(filePath));
    }
}
