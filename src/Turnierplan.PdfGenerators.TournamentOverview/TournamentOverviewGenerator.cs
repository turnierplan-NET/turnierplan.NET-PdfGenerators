using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Turnierplan.Adapter;
using Turnierplan.Adapter.Models;
using Turnierplan.PdfGenerators.Common;
using Turnierplan.PdfGenerators.Common.Extensions;

namespace Turnierplan.PdfGenerators.TournamentOverview;

public sealed class TournamentOverviewGenerator : GeneratorBase<TournamentOverviewOptions>
{
    protected override async Task RunAsync(TurnierplanClient client, TournamentOverviewOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FolderId))
        {
            Logger.LogError("The folder ID is not specified!");
            return;
        }

        var tournaments = await client.GetAllTournamentsWithDetailsAsync(options.FolderId);
        Logger.LogTrace("Successfully loaded {TournamentCount} tournaments for QR codes", tournaments.Count);

        CreateDocument(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(2);
                        cols.RelativeColumn(0.8f);
                        cols.RelativeColumn(0.8f);
                        cols.RelativeColumn(0.8f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1f).BorderBottom(2f).Padding(4f).Text("Name").Bold();
                        header.Cell().Border(1f).BorderBottom(2f).Padding(4f).Text("Anpfiff").Bold();
                        header.Cell().Border(1f).BorderBottom(2f).Padding(4f).Text("Mannschaften").Bold();
                        header.Cell().Border(1f).BorderBottom(2f).Padding(4f).Text("Gruppen").Bold();
                        header.Cell().Border(1f).BorderBottom(2f).Padding(4f).Text("Spiele").Bold();
                    });

                    List<Tournament> sorted =
                    [
                        ..tournaments.Where(x => x.Matches.Length == 0),
                        ..tournaments.Where(x => x.Matches.Length > 0)
                            .OrderBy(x => x.Matches.Min(y => y.Kickoff ?? DateTime.MinValue))
                    ];

                    foreach (var tournament in sorted)
                    {
                        table.Cell().Border(1f).Padding(4f).Text(tournament.Name);
                        table.Cell().Border(1f).Padding(4f).Text(tournament.Matches.Length == 0 ? string.Empty : $"{tournament.Matches.Min(x => x.Kickoff ?? DateTime.MinValue)}");
                        table.Cell().Border(1f).Padding(4f).Text($"{tournament.Teams.Length}");
                        table.Cell().Border(1f).Padding(4f).Text($"{tournament.Groups.Length}");
                        table.Cell().Border(1f).Padding(4f).Text($"{tournament.Matches.Length}");
                    }
                });
            });
        });
    }
}
