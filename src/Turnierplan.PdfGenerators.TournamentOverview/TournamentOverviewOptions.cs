using Turnierplan.PdfGenerators.Common;

namespace Turnierplan.PdfGenerators.TournamentOverview;

public sealed record TournamentOverviewOptions : GeneratorOptionsBase
{
    public string? FolderId { get; set; }
}
