using System.Text;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using SkiaSharp.QrCode;
using Turnierplan.Adapter;
using Turnierplan.Adapter.Models;
using Turnierplan.PdfGenerators.Common;

namespace Turnierplan.PdfGenerators.QrCodes;

public sealed class QrCodesGenerator : GeneratorBase<QrCodesOptions>
{
    protected override async Task RunAsync(TurnierplanClient client, QrCodesOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FolderId))
        {
            Logger.LogError("The folder ID is not specified!");
            return;
        }

        var tournamentHeaders = await client.GetTournaments(options.FolderId);
        var tournaments = new List<Tournament>();

        foreach (var header in tournamentHeaders)
        {
            var tournament = await client.GetTournament(header.Id);
            tournaments.Add(tournament);
        }

        tournaments = tournaments.OrderBy(x => x.Name).ToList();

        Logger.LogTrace("Successfully loaded {TournamentCount} tournaments for QR codes", tournaments.Count);

        CreateDocument(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(15, Unit.Millimetre);
                page.MarginBottom(0, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(16));
                page.Content().Column(column =>
                {
                    if (options.LogoImageFile is not null)
                    {
                        const float logoImageSize = 2.75f;
                        column.Item().Unconstrained().Width(logoImageSize, Unit.Centimetre).Height(logoImageSize, Unit.Centimetre).Image(options.LogoImageFile);
                        column.Item().AlignRight().Unconstrained().TranslateX(-logoImageSize, Unit.Centimetre).Width(logoImageSize, Unit.Centimetre).Height(logoImageSize, Unit.Centimetre).Image(options.LogoImageFile);
                    }

                    if (options.Text is not null)
                    {
                        column.Item().AlignCenter().PaddingBottom(23, Unit.Millimetre).PaddingHorizontal(3, Unit.Centimetre).Text(options.Text).Bold().AlignCenter();
                    }

                    for (var i = 0; i < tournaments.Count; i += 4)
                    {
                        var iCopy = i;

                        column.Item().Height(7.5f, Unit.Centimetre).Row(row =>
                        {
                            for (var j = iCopy; j < iCopy + 4; j++)
                            {
                                var jCopy = j;

                                row.RelativeItem().Column(cell =>
                                {
                                    if (jCopy < tournaments.Count)
                                    {
                                        var tournament = tournaments[jCopy];
                                        var url = $"{InstanceUrl.TrimEnd('/')}/tournament?id={tournament.Id}";

                                        cell.Item().AlignCenter().Text(tournament.Name).FontSize(18).Bold();
                                        cell.Item().AlignCenter().PaddingBottom(3, Unit.Millimetre).Text($"{tournament.Matches.Select(x => x.Kickoff!.Value).Min().ToLocalTime():dd.MM.yyyy HH:mm}").FontSize(13);

                                        cell.Item().AlignCenter().Width(4.5f, Unit.Centimetre).Height(4.5f, Unit.Centimetre).SkiaSharpSvgCanvas((canvas, size) =>
                                        {
                                            var data = QRCodeGenerator.CreateQrCode(url, ECCLevel.M, quietZoneSize: 1);
                                            canvas.Render(data, (int)size.Width, (int)size.Height);
                                        });
                                    }
                                });
                            }
                        });
                    }
                });
            });
        });
    }
}

// Copied from turnierplan.NET code
// https://github.com/turnierplan-NET/turnierplan.NET/blob/1377956c894d6616d23c90c1c3d0c5f2156916c0/src/Turnierplan.PdfRendering/Extensions/QuestPdfContainerExtensions.cs
file static class SkiaSharpHelpers
{
    public static void SkiaSharpSvgCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
    {
        container.Svg(size =>
        {
            using var stream = new MemoryStream();

            using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream))
            {
                drawOnCanvas(canvas, size);
            }

            var svgData = stream.ToArray();
            return Encoding.UTF8.GetString(svgData);
        });
    }
}
