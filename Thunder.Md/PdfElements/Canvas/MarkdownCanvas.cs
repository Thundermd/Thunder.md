namespace Thunder.Md.PdfElements.Canvas;

using System.Diagnostics.CodeAnalysis;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.Readers;
using Thunder.Md.Readers.Markdown;

public class MarkdownCanvas: ICanvasElement{
    public string FilePath{ get; }
    public MarkdownCanvas(string filePath){
        FilePath = filePath;
    }
    public void Draw(ThunderConfig config, ThunderBuildState state, IContainer container){
        MarkdownReader markdownReader = new(FilePath, config, Program.ExtensionLoader!, Program.CreateLogger<MarkdownReader>());

        List<IPdfElement> elements = markdownReader.Read().ToList();
        
        ThunderBuildState localState = state.Clone();
        foreach(IPdfElement element in elements){
            MockContainer mockContainer = new();
            element.Draw(config, state, mockContainer);
        }
        
        container.Column(columnsHandler => {
            columnsHandler.Spacing(20);
            foreach(IPdfElement element in elements){
                element.Draw(config, localState, columnsHandler.Item());
            }
        });
    }

    public static bool Create(ExtensionArgs args, string url, ITextElement? __altText, string? __label, Dictionary<string, string?> __parameters, [NotNullWhen(true)] out ICanvasElement? canvasElement){
        string fullPath = Path.Combine(ThunderPaths.Source, url);

        canvasElement = new MarkdownCanvas(fullPath);
        return true;
    }
}