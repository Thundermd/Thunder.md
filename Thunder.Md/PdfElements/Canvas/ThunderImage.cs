namespace Thunder.Md.PdfElements.Canvas;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Inline;

public class ThunderImage: ICanvasElement{
    private readonly ITextElement? _altText;
    private readonly string? _label;
    public string FilePath{ get; }
    private readonly float _width;
    public ThunderImage(string filePath, ITextElement? altText, string? label, float width){
        _altText = altText;
        _label = label;
        _width = width;
        FilePath = filePath;
    }
    public void Draw(ThunderConfig config, ThunderBuildState state, IContainer container){
        ThunderIndexItem? indexItem = _altText is null ? null: state.GetNextGraphicsName(_altText.Text, _label);
        container.Column(column => {
            float availableWidth = (config.Project!.PaperWidth - 2 * config.Project!.PageMargin);
            float availableHeight = (config.Project!.PaperHeight - 2 * config.Project!.PageMargin);

            float imageWidth = availableWidth * _width;
            float imageHeight = availableHeight * 1;
            
            if(indexItem is not null){
                column.Item().Section(indexItem.LabelId);
            }
            if(config.Project!.GraphicsNumbering.Alignment ==  Alignment.Top){
                AddAltText(config, column, indexItem, state);
                column.Item().Height(config.Project!.FontSize * 0.5f);
            }

            column.Item().AlignCenter()
                  .MaxHeight(imageHeight, Unit.Millimetre)
                  .Width(imageWidth, Unit.Millimetre)
                  .AlignCenter().AlignBottom()
                  .SemanticFigure(_altText?.Text ?? "Image").Image(Image.FromFile(FilePath))
                  .FitArea();
            if(config.Project!.GraphicsNumbering.Alignment ==  Alignment.Bottom){
                column.Item().Height(config.Project!.FontSize * 0.5f);
                AddAltText(config, column, indexItem, state);
            }
        });
    }

    private void AddAltText(ThunderConfig config, ColumnDescriptor column, ThunderIndexItem? indexItem,
                            ThunderBuildState state){
        if(indexItem is null || _altText is null){
            return;
        }
        TextWrapper nameWrapper = new(config.Project!.NumberingStyle);
        nameWrapper.Add(new PureTextElement(indexItem.Id + " "));
        TextWrapper textWrapper = new(new FontStyle());
        textWrapper.Add(nameWrapper);
        textWrapper.Add(_altText);
        column.Item().SemanticCaption().AlignCenter().Text(text => {
            textWrapper.Draw(text, new FontStyle(), state, config);
        });
    }

    public static bool Create(ExtensionArgs args, string url, ITextElement? altText, string? label, Dictionary<string, string?> parameters, [NotNullWhen(true)] out ICanvasElement? canvasElement){
        string fullPath = Path.Combine(ThunderPaths.Source, url);

        float width = 0.8f;

        float readWidth = 0;
        if((parameters.TryGetValue("width", out string? widthStr) && widthStr is not null && float.TryParse(widthStr, CultureInfo.InvariantCulture, out readWidth))
           || parameters.Count(x => x.Value is null && float.TryParse(x.Key, CultureInfo.InvariantCulture, out readWidth)) == 1){
            width = readWidth;
        }
        
        canvasElement = new ThunderImage(fullPath, altText, label, width);
        return true;
    }
}