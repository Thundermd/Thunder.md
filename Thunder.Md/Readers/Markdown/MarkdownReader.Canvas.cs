namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private bool TryReadCanvas([NotNullWhen(true)] out ICanvasElement? canvasElement){
        if(!TryReadCanvasContent(out ITextElement? altTextElement, out string? path)){
            canvasElement = null;
            return false;
        }

        if(!_extensionLoader.TryGetCanvasElement(path, altTextElement, _config, out canvasElement)){
            _logger.LogWarning("Cannot generate canvas for file '{file}'. Are you missing to import an extension?",
                               path);
            canvasElement = null;
            return false;
        }

        return true;
    }
    
    private bool TryReadInlineCanvas([NotNullWhen(true)]out IInlineCanvasElement? inlineCanvasElement){
        if(!TryReadCanvasContent(out ITextElement? altTextElement, out string? path)){
            inlineCanvasElement = null;
            return false;
        }

        if(!_extensionLoader.TryGetInlineCanvas(path, altTextElement, _config, out inlineCanvasElement)){
            _logger.LogWarning("Cannot generate canvas for file '{file}'. Are you missing to import an extension?",
                               path);
            inlineCanvasElement = null;
            return false;
        }

        return true;
    }
    

    private bool TryReadCanvasContent(out ITextElement? altTextElement,
                                      [NotNullWhen(true)]out string? path){
        if(!_fileReader.TryGetNext(out char c) || c != '['){
            altTextElement = null;
            path = null;
            return false;
        }

        bool isDirect = false;
        if(!_fileReader.TryGetNext(out c)){
            altTextElement = null;
            path = null;
            return false;
        }

        altTextElement = null;
        isDirect = c == '[';
        if(!isDirect){
            if(!TryReadText([new EndChar(']', 1)], EndLineManagement.Error, true, c,
                            out TextWrapper? innerTextWrapper, out _)){
                path = null;
                return false;
            }

            altTextElement = innerTextWrapper;


            if(!_fileReader.TryGetNext(out c) || c != '('){
                path = null;
                return false;
            }
        }

        if(!TryReadTextNotFormatted([new EndChar(isDirect ? ']' : ')', isDirect ? 2 : 1)], EndLineManagement.Error,
                                    true, null,
                                    out path, out _)){
            return false;
        }

        return true;
    }


}