using Turnierplan.Adapter;
using Turnierplan.Adapter.Models;

namespace Turnierplan.PdfGenerators.Common.Extensions;

public static class TournamentClientExtensions
{
    public static async Task<List<Tournament>> GetAllTournamentsWithDetailsAsync(this TurnierplanClient client, string folderId, params string[] skipTournamentIds)
    {
        var tournamentHeaders = await client.GetTournaments(folderId);
        var tournaments = new List<Tournament>();

        foreach (var header in tournamentHeaders)
        {
            if (skipTournamentIds.Contains(header.Id))
            {
                continue;
            }

            var tournament = await client.GetTournament(header.Id);
            tournaments.Add(tournament);
        }

        return tournaments.OrderBy(x => x.Name).ToList();
    }
}
