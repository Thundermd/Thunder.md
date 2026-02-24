namespace Thunder.Md.Extensions.PdfElements;

using System.Diagnostics.CodeAnalysis;

public delegate bool CanvasCreatorMethod(ExtensionArgs args, string url, ITextElement? altText, string? label, Dictionary<string, string?> parameters, [NotNullWhen(true)] out ICanvasElement? canvasElement);
public delegate bool InlineCanvasCreatorMethod(ExtensionArgs args, string url, ITextElement? altText, string? label, Dictionary<string, string?> parameters, [NotNullWhen(true)] out IInlineCanvasElement? inlineCanvas);
public record CanvasCreator(CanvasCreatorMethod Creator, string? Protocol = null, params string[] FileExtension);
public record InlineCanvasCreator(InlineCanvasCreatorMethod Creator, string? Protocol = null, params string[] FileExtension);