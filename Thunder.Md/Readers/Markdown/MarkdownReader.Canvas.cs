namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private bool TryReadCanvas([NotNullWhen(true)] out ICanvasElement? canvasElement){
        if(!TryReadCanvasContent(out ITextElement? altTextElement, out string? path, out string? label, out Dictionary<string, string?> parameters)){
            canvasElement = null;
            return false;
        }

        if(!_extensionLoader.TryGetCanvasElement(path, altTextElement, label, parameters, _config, out canvasElement)){
            _logger.LogWarning("Cannot generate canvas for file '{file}'. Are you missing to import an extension?",
                               path);
            canvasElement = null;
            return false;
        }

        return true;
    }
    
    private bool TryReadInlineCanvas([NotNullWhen(true)]out IInlineCanvasElement? inlineCanvasElement){
        if(!TryReadCanvasContent(out ITextElement? altTextElement, out string? path, out string? label, out Dictionary<string, string?> parameters)){
            inlineCanvasElement = null;
            return false;
        }

        if(!_extensionLoader.TryGetInlineCanvas(path, altTextElement, label, parameters, _config, out inlineCanvasElement)){
            _logger.LogWarning("Cannot generate canvas for file '{file}'. Are you missing to import an extension?",
                               path);
            inlineCanvasElement = null;
            return false;
        }

        return true;
    }
    

    private bool TryReadCanvasContent(out ITextElement? altTextElement,
                                      [NotNullWhen(true)]out string? path, out string? referenceId, out Dictionary<string, string?> parameters){
        parameters = new Dictionary<string, string?>();
        if(!_fileReader.TryGetNext(out char c) || c != '['){
            altTextElement = null;
            path = null;
            referenceId = null;
            return false;
        }

        bool isDirect = false;
        if(!_fileReader.TryGetNext(out c)){
            altTextElement = null;
            path = null;
            referenceId = null;
            return false;
        }

        altTextElement = null;
        isDirect = c == '[';
        if(!isDirect){
            if(!TryReadText([new EndChar(']', 1)], EndLineManagement.Error, true, c,
                            out TextWrapper? innerTextWrapper, out _)){
                path = null;
                referenceId = null;
                return false;
            }

            altTextElement = innerTextWrapper;


            if(!_fileReader.TryGetNext(out c) || c != '('){
                path = null;
                referenceId = null;
                return false;
            }
        }

        if(!TryReadTextNotFormatted([new EndChar(isDirect ? ']' : ')', isDirect ? 2 : 1)], EndLineManagement.Error,
                                    true, null,
                                    out path, out _)){
            referenceId = null;
            return false;
        }

        TryReadReference(out referenceId);

        var atCount = path.Count(x => x == '@');
        if(atCount > 1){
            _logger.LogWarning(_fileReader, "The canvas path '{path}' contains more that one '@', that can lead to unexpected behavior", path!);
        }

        if(atCount > 0){
            string[] splittedPathString = path.Split('@');
            path = string.Join('@', splittedPathString[..^1]);
            string parameterString = splittedPathString[^1];
            string[] parameterStrings = parameterString.Split('&');
            IEnumerable<string[]> splittedParameterStrings = parameterStrings.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Split('='));
            foreach(string[] parameterParts in splittedParameterStrings){
                if(parameterParts.Length > 2){
                    _logger.LogWarning(_fileReader, "The parameter containes more that one '=' symbol. That is invalid syntax and only the first two parts will be used");
                }
                parameters.Add(parameterParts[0], parameterParts.Length > 1 ? parameterParts[1] : null);
            }
        }

        return true;
    }


}