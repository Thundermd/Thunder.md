namespace Thunder.Md.PdfElements.Inline;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;

public class InlineCode: ITextElement{
    public string Text =>  _content;
    
    private readonly string _content;
    public InlineCode(string text){
        _content = text;
    }
    
    public void Prebuild(ThunderConfig config, IThunderBuildState state){ }
    
    public void Draw(TextDescriptor text, FontStyle fontStyle, IThunderBuildState state, ThunderConfig config){
        text.Element()
            .PaddingBottom(-2)
            .AddBorderRadius(config)
            .Background(config.Project!.TextColor.ToPdfColor())
            .PaddingHorizontal(1, Unit.Millimetre)
            .Text(_content)
            .FontFamily("Consolas", "Courier New", "Monaco", "Courier")
            .FontColor(config.Project!.BackgroundColor.ToPdfColor());
    }
}