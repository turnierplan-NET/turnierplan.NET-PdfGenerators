using Turnierplan.PdfGenerators.Common;

namespace Turnierplan.PdfGenerators.QrCodes;

public sealed record QrCodesOptions : GeneratorOptionsBase
{
    public string? FolderId { get; set; }

    public string? Text { get; set; }

    public string? LogoImageFile { get; set; }
}
