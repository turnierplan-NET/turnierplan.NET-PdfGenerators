namespace Turnierplan.PdfGenerators.Console.Options;

public sealed record TurnierplanAdapterOptions
{
    public required string InstanceUrl { get; set; }

    public required string ApiKey { get; set; }

    public required string ApiKeySecret { get; set; }
}
