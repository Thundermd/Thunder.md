namespace Thunder.Md.PdfElements.Inline;

using QuestPDF.Fluent;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;

public class LineBreak: ITextElement{
    public string Text => "\n";
    public void Draw(TextDescriptor text, FontStyle fontStyle, IThunderBuildState state, ThunderConfig config){
        text.EmptyLine();
    }
    public void Prebuild(ThunderConfig config, IThunderBuildState state){}
}