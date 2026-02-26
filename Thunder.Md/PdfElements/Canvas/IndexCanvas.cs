namespace Thunder.Md.PdfElements.Canvas;

using System.Diagnostics.CodeAnalysis;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Canvas.Indexes;

public abstract class IndexCanvas: ICanvasElement{
    public virtual void Draw(ThunderConfig config, IThunderBuildState state, IContainer container){
        container = IsTableOfContent ? container.SemanticTableOfContents() : container.SemanticIndex();

        container.Column(list => {
            IReadOnlyCollection<ThunderIndexItem> indexItems = GetItems(config, state, container);
            foreach(ThunderIndexItem indexItem in indexItems){
                ThunderIndexItem item = indexItem;
                IContainer itemElement = list.Item();
                if(IsTableOfContent){
                    itemElement = itemElement.SemanticTableOfContentsItem();
                }

                itemElement.SectionLink(item.SectionId).Row(row => {
                    row.AutoItem().Text($"{indexItem.ReferenceText} {indexItem.Name}");
                    row.RelativeItem().AlignRight().Text(new string('.', 200)).LetterSpacing(0.4f).ClampLines(1, "");
                    row.ConstantItem(config.Project!.FontSize * 2).AlignRight()
                       .Text(text => text.BeginPageNumberOfSection(item.SectionId));
                });
            }
        });
    }

    public void Prebuild(ThunderConfig config, IThunderBuildState state){ }

    protected virtual bool IsTableOfContent => false;

    protected abstract IReadOnlyCollection<ThunderIndexItem> GetItems(ThunderConfig config, IThunderBuildState state,
                                                                      IContainer container);

    public static bool Create(ExtensionArgs args, string url, ITextElement? altText, string? __label,
                              Dictionary<string, string?> __parameters,
                              [NotNullWhen(true)] out ICanvasElement? canvasElement){
        var simplified = url.Trim().ToLower();
        switch(simplified){
            case "content" or "sections" or "toc":
                canvasElement = new SectionIndex();
            break;
            case "graphics" or "figures":
                canvasElement = new GraphicsIndex();
            break;
            case "math" or "equations" or "formulas":
                canvasElement = new MathIndex();
            break;
            case "tables":
                canvasElement = new TableIndex();
            break;
            case "references" or "citations":
                canvasElement = new ReferenceIndex(altText?.Text);
            break;
            default:
                canvasElement = null;
                return false;
        }

        return true;
    }
}