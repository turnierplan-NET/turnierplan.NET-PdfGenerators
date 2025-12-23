using Turnierplan.PdfGenerators.Common;

namespace Turnierplan.PdfGenerators.ChangingRoomSigns;

public sealed record ChangingRoomSignsOptions : GeneratorOptionsBase
{
    public string? FolderId { get; set; }

    public string[]? SkipTournamentIds { get; set; }

    public string? HomeTeamNamePattern { get; set; }

    public int? NumberOfChangingRooms { get; set; }
}
