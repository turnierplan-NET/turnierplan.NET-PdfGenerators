using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Turnierplan.Adapter;
using Turnierplan.Adapter.Models;
using Turnierplan.PdfGenerators.Common;
using Turnierplan.PdfGenerators.Common.Extensions;

namespace Turnierplan.PdfGenerators.ChangingRoomSigns;

public sealed class ChangingRoomSignsGenerator : GeneratorBase<ChangingRoomSignsOptions>
{
    private const string TitleFormatPlaceholder = "{{NR}}";
    private const string TeamCountFormatPlaceholder = "{{COUNT}}";

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

        if (string.IsNullOrWhiteSpace(options.TitleFormat) || !options.TitleFormat.Contains(TitleFormatPlaceholder))
        {
            Logger.LogError($"The title format is not specified or does not contain the placeholder '{TitleFormatPlaceholder}'!");
            return;
        }

        if (string.IsNullOrWhiteSpace(options.TeamCountFormat) || !options.TeamCountFormat.Contains(TeamCountFormatPlaceholder))
        {
            Logger.LogError($"The team count format is not specified or does not contain the placeholder '{TeamCountFormatPlaceholder}'!");
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

        var data = PrepareChangingRoomData(tournaments, options.NumberOfChangingRooms.Value, pattern);

        CreateDocument(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(16));
                page.Content().Column(column =>
                {
                    for (var z = 0; z < options.NumberOfChangingRooms.Value; z++)
                    {
                        var changingRoomIndex = z;
                        column.Item().AlignCenter().PaddingBottom(1, Unit.Centimetre).Text(options.TitleFormat.Replace(TitleFormatPlaceholder, (changingRoomIndex + 1).ToString())).Bold();

                        for (var i = 0; i < tournaments.Count; i += 4)
                        {
                            var iCopy = i;

                            column.Item().Height(8, Unit.Centimetre).Row(row =>
                            {
                                for (var j = iCopy; j < iCopy + 4; j++)
                                {
                                    var jCopy = j;

                                    row.RelativeItem().Border(1).PaddingTop(5, Unit.Millimetre).Column(cell =>
                                    {
                                        if (jCopy < data.Count)
                                        {
                                            var dataEntry = data[jCopy];

                                            cell.Item().AlignCenter().Text(dataEntry.TournamentName).FontSize(18).Bold();
                                            cell.Item().AlignCenter().Text(dataEntry.TournamentDate).FontSize(11);

                                            var changingRoom = dataEntry.ChangingRooms[changingRoomIndex];

                                            foreach ((string Team, int Count) changingRoomTeam in changingRoom.Teams)
                                            {
                                                cell.Item().PaddingTop(4, Unit.Millimetre).Text(changingRoomTeam.Team).AlignCenter();

                                                if (changingRoomTeam.Count > 1)
                                                {
                                                    cell.Item().AlignCenter().Text(options.TeamCountFormat.Replace(TeamCountFormatPlaceholder, changingRoomTeam.Count.ToString())).FontSize(12);
                                                }
                                            }
                                        }
                                    });
                                }
                            });
                        }

                        if (changingRoomIndex != options.NumberOfChangingRooms - 1)
                        {
                            column.Item().PageBreak();
                        }
                    }
                });
            });
        });
    }

    private static List<Entry> PrepareChangingRoomData(List<Tournament> tournaments, int numberOfChangingRooms, Regex? homeTeamPattern)
    {
        var result = new List<Entry>();

        foreach (var tournament in tournaments)
        {
            var dataEntry = new Entry
            {
                TournamentName = tournament.Name,
                TournamentDate = $"{tournament.Matches.Select(x => x.Kickoff!.Value).Min().ToLocalTime():dd.MM.yyyy HH:mm}"
            };

            result.Add(dataEntry);

            for (var i = 0; i < numberOfChangingRooms; i++)
            {
                dataEntry.ChangingRooms.Add(new ChangingRoom { Id = i });
            }

            string NameWithoutNumber(Team team)
            {
                var name = team.Name.Trim();
                return char.IsDigit(name.Last()) ? name[..^1].Trim() : name;
            }

            var teamsGrouped = tournament.Teams
                .GroupBy(NameWithoutNumber)
                .OrderByDescending(x => x.Count())
                .ThenByDescending(x => homeTeamPattern is not null && homeTeamPattern.IsMatch(x.Key))
                .ThenBy(x => x.Key);

            foreach (var grouping in teamsGrouped)
            {
                var changingRoom = dataEntry.ChangingRooms.OrderBy(x => x.Utilization).ThenBy(x => x.Id).First();
                changingRoom.Teams.Add((Team: grouping.Key, Count: grouping.Count()));
            }
        }

        return result;
    }

    private sealed record Entry
    {
        public required string TournamentName { get; init; }

        public required string TournamentDate { get; init; }

        public List<ChangingRoom> ChangingRooms { get; } = [];
    }

    private sealed record ChangingRoom
    {
        public required int Id { get; init; }

        public List<(string Team, int Count)> Teams { get; } = [];

        public int Utilization => Teams.Sum(x => x.Count);
    }
}
