namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private bool TryReadCanvas([NotNullWhen(true)] out ICanvasElement? canvasElement){
        if(!_fileReader.TryGetNext(out char c) || c != '['){
            canvasElement = null;
            return false;
        }

        bool isDirect = false;
        if(!_fileReader.TryGetNext(out c)){
            canvasElement = null;
            return false;
        }

        ITextElement? altTextElement = null;
        isDirect = c == '[';
        if(!isDirect){
            if(!TryReadText([new EndChar(']', 1)], EndLineManagement.Error, true, c,
                            out TextWrapper? innerTextWrapper)){
                canvasElement = null;
                return false;
            }

            altTextElement = innerTextWrapper;


            if(!_fileReader.TryGetNext(out c) || c != '('){
                canvasElement = null;
                return false;
            }
        }

        if(!TryReadTextNotFormatted([new EndChar(isDirect ? ']' : ')', isDirect ? 2 : 1)], EndLineManagement.Error,
                                    true, null,
                                    out string? file)){
            canvasElement = null;
            return false;
        }

        if(!_extensionLoader.TryGetCanvasElement(file, altTextElement, _config, out canvasElement)){
            _logger.LogWarning("Cannot generate canvas for file '{file}'. Are you missing to import an extension?",
                               file);
            canvasElement = null;
            return false;
        }

        return true;
    }
}