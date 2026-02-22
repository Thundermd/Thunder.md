namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Container;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private readonly FileReader _fileReader;
    private readonly ThunderConfig _config;
    private readonly ExtensionLoader _extensionLoader;
    private readonly ILogger _logger;
    private bool _shouldRun = true;

    public MarkdownReader(string path, ThunderConfig config, ExtensionLoader extensionLoader, ILogger logger){
        _config = config;
        _extensionLoader = extensionLoader;
        _logger = logger;
        _fileReader = new FileReader(path);
    }

    public IEnumerable<IPdfElement> Read(){
        if(!_fileReader.TryInit()){
            _logger.LogError("Cannot read file '{FilePath}'", _fileReader.Path);
            yield break;
        }

        _shouldRun = true;
        while(_fileReader.TryGetNext(out char c) && _shouldRun){
            _fileReader.Save();
            if(char.IsWhiteSpace(c)){
                continue;
            }

            if(c == '#'){
                if(TryReadHeadline(out IPdfElement? headline)){
                    yield return headline;
                    continue;
                }

                RecallOrError();
            }

            if(c == '!'){
                if(TryReadCanvas(out ICanvasElement? canvas)){
                    yield return canvas;
                    continue;
                }

                RecallOrError();
            }

            if(c == '$'){
                if(_fileReader.TryGetNext(out char tempC)
                && tempC == '$'
                && TryReadTextNotFormatted([new('$', 2)], EndLineManagement.Ignore, false, null, out string? mathStr)
                  ){
                    yield return new MathContainer(mathStr);
                    continue;
                }

                RecallOrError();
            }

            if(c == '-' || c == '*' || char.IsLetterOrDigit(c)){
                if(TryReadList(c, out ListElement? listElement)){
                    yield return listElement;
                    continue;
                }

                RecallOrError();
            }

            if(!TryReadText([new EndChar('\n', 2)], EndLineManagement.ToSpace, true, c, out TextWrapper? paragraph)){
                RecallOrError();
                _logger.LogError(_fileReader, "Can not read paragraph");
                continue;
            }

            yield return new ParagraphElement(paragraph);
        }

        if(!_shouldRun){
            Program.Error = true;
        }
    }


    private void RecallOrError(){
        RecallOrErrorPop();
        _fileReader.Save();
    }

    private void RecallOrErrorPop(){
        bool value = _fileReader.TryRecall();
        _shouldRun = value;
        if(value){
            return;
        }

        _logger.LogError("Cannot recall file point '{FilePath}'", _fileReader.Path);
    }
}