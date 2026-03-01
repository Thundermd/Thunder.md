namespace Thunder.Md.PdfElements.Container;

using System.Collections.Immutable;
using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.InternalExtensions.CodeFormatters;

public class CodeContainer: IPdfElement{
    private readonly string _content;
    private readonly string _languageId;
    private ImmutableArray<ITextElement> _contentElements;

    public CodeContainer(string languageId, string content){
        _languageId = languageId;
        _content = content.Trim();
    }

    public void Prebuild(ThunderConfig config, IThunderBuildState state){
        ICodeFormatter codeFormatter = Program.ExtensionLoader is not null ? Program.ExtensionLoader.GetCodeFormatter(_languageId) : new DefaultCodeFormatter();

        _contentElements = [..codeFormatter.Format(_content)];
        foreach(ITextElement contentElement in _contentElements){
            contentElement.Prebuild(config, state);
        }
    }
    
    public void Draw(ThunderConfig config, IThunderBuildState state, IContainer container){
        container.AddBorderRadius(config)
                 .Background(config.Project!.TextColor.ToPdfColor())
                 .Padding(2, Unit.Millimetre)
                 .Text(text => {
                     text.DefaultTextStyle(style => style.FontFamily("Consolas", "Courier New", "Monaco", "Courier"));
                     foreach(ITextElement element in _contentElements){
                         element.Draw(text, new FontStyle(Color: config.Project!.BackgroundColor), state, config);
                     }
                 });
    }
}