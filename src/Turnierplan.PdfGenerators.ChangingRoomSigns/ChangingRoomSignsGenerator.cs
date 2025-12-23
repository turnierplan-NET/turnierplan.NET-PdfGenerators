using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Turnierplan.Adapter;
using Turnierplan.PdfGenerators.Common;
using Turnierplan.PdfGenerators.Common.Extensions;

namespace Turnierplan.PdfGenerators.ChangingRoomSigns;

public sealed class ChangingRoomSignsGenerator : GeneratorBase<ChangingRoomSignsOptions>
{
    protected override async Task RunAsync(TurnierplanClient client, ChangingRoomSignsOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FolderId))
        {
            Logger.LogError("The folder ID is not specified!");
            return;
        }

        if (options.NumberOfChangingRooms is null or <= 0)
        {
            Logger.LogError("The number of changing rooms is not specified or less than 0!");
            return;
        }

        Regex? pattern = null;

        if (options.HomeTeamNamePattern is not null)
        {
            try
            {
                pattern = new Regex(options.HomeTeamNamePattern);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to compile home team name pattern!");
                return;
            }
        }

        var tournaments = await client.GetAllTournamentsWithDetailsAsync(options.FolderId, options.SkipTournamentIds ?? []);
        Logger.LogTrace("Successfully loaded {TournamentCount} tournaments for changing room signs", tournaments.Count);
    }
}
