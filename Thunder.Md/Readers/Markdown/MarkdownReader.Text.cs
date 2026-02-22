namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{

    private record struct EndChar(char Char, int RequiredCount);

    private enum EndLineManagement{
        Ignore,
        ToSpace,
        Insert,
        Error
    }
    
    private bool TryReadTextNotFormatted(EndChar[] endChars, EndLineManagement endLineManagement, bool reduceSpaces,
                                         char? firstChar, [NotNullWhen(true)] out string? value){
        if(endChars.Length == 0){
            throw new ArgumentException("endChars must not be empty", nameof(endChars));
        }

        int endCharCount = 0;
        char currentEndChar = endChars[0].Char;
        StringBuilder text = new();
        bool isInSpace = false;
        char c = '\0';
        while(endCharCount < endChars.Where(x => x.Char == currentEndChar).Min(x => x.RequiredCount)
           && (firstChar.HasValue || _fileReader.TryGetNext(out c))
           && _shouldRun
             ){
            if(firstChar.HasValue){
                c = firstChar.Value;
                firstChar = null;
            }

            if(c == '\0'){
                _logger.LogError(_fileReader, "Invalid '\0' character in file");
                value = null;
                return false;
            }

            if(endChars.Any(x => x.Char == c)){
                if(endCharCount == 0){
                    _fileReader.Save(-1);
                }

                if(currentEndChar != c){
                    currentEndChar = c;
                    endCharCount = 0;
                }

                endCharCount++;
                continue;
            }

            if(char.IsWhiteSpace(c) && endCharCount > 0){
                continue;
            }

            if(endCharCount > 0){
                endCharCount = 0;
                RecallOrErrorPop();
                _fileReader.TryGetNext(out c);
            }

            if(c == '\n'){
                switch(endLineManagement){
                    case EndLineManagement.Ignore:
                        // Ignore
                    break;
                    case EndLineManagement.Insert:
                        text.Append('\n');
                    break;
                    case EndLineManagement.ToSpace:
                        if(!isInSpace || !reduceSpaces){
                            text.Append(' ');
                        }

                        isInSpace = true;
                    break;
                    case EndLineManagement.Error:
                        value = null;
                        return false;
                    default:
                        throw new UnreachableException();
                }

                continue;
            }

            if(char.IsWhiteSpace(c)){
                if(!isInSpace || !reduceSpaces){
                    text.Append(' ');
                }

                isInSpace = true;
                continue;
            }

            isInSpace = false;

            text.Append(c);
        }

        value = text.ToString();

        _fileReader.DeleteLastSave();

        return true;
    }

    private bool TryReadText(EndChar[] endChars, EndLineManagement endLineManagement, bool reduceSpaces,
                             char? firstChar,
                             [NotNullWhen(true)] out TextWrapper? result){
        if(endChars.Length == 0){
            throw new ArgumentException("endChars must not be empty", nameof(endChars));
        }

        int endCharCount = 0;
        char currentEndChar = endChars[0].Char;
        StringBuilder text = new();
        result = new TextWrapper(new FontStyle());
        bool isInSpace = false;
        char c = '\0';
        while(endCharCount < endChars.Where(x => x.Char == currentEndChar).Min(x => x.RequiredCount)
           && (firstChar.HasValue || _fileReader.TryGetNext(out c))
           && _shouldRun
             ){
            if(firstChar.HasValue){
                c = firstChar.Value;
                firstChar = null;
            }

            if(c == '\0'){
                _logger.LogError(_fileReader, "Invalid '\0' character in file");
                return false;
            }

            if(endChars.Any(x => x.Char == c)){
                if(endCharCount == 0){
                    _fileReader.Save(-1);
                }

                if(currentEndChar != c){
                    currentEndChar = c;
                    endCharCount = 0;
                }

                endCharCount++;
                continue;
            }

            if(char.IsWhiteSpace(c) && endCharCount > 0){
                continue;
            }

            if(endCharCount > 0){
                endCharCount = 0;
                RecallOrErrorPop();
                _fileReader.TryGetNext(out c);
            }

            if(c == '\n'){
                switch(endLineManagement){
                    case EndLineManagement.Ignore:
                        // Ignore
                    break;
                    case EndLineManagement.Insert:
                        text.Append('\n');
                    break;
                    case EndLineManagement.ToSpace:
                        if(!isInSpace || !reduceSpaces){
                            text.Append(' ');
                        }

                        isInSpace = true;
                    break;
                    case EndLineManagement.Error:
                        return false;
                    default:
                        throw new UnreachableException();
                }

                continue;
            }

            if(char.IsWhiteSpace(c)){
                if(!isInSpace || !reduceSpaces){
                    text.Append(' ');
                }

                isInSpace = true;
                continue;
            }

            isInSpace = false;

            if(c == '*'){
                PureTextElement textElement = new(text.ToString());
                result.Add(textElement);
                text.Clear();
                _fileReader.Save();
                bool bold = false;
                if(!_fileReader.TryGetNext(out char tempC)){
                    text.Append(c);
                    continue;
                }

                bold = tempC == '*';
                if(!bold){
                    RecallOrErrorPop();
                }

                EndChar[] newEndChars = [..endChars, new('*', bold ? 2 : 1)];
                if(!TryReadText(newEndChars, endLineManagement, reduceSpaces, null, out TextWrapper? innerTextWrapper)){
                    result = null;
                    return false;
                }

                if(bold){
                    innerTextWrapper.FontStyle = innerTextWrapper.FontStyle with{ Bold = true };
                } else{
                    innerTextWrapper.FontStyle = innerTextWrapper.FontStyle with{ Italic = true };
                }

                result.Add(innerTextWrapper);
                continue;
            }

            if(c == '_'){
                PureTextElement textElement = new(text.ToString());
                result.Add(textElement);
                text.Clear();

                _fileReader.Save();
                if(!_fileReader.TryGetNext(out char tempC) || tempC != '_'){
                    RecallOrErrorPop();
                    text.Append(c);
                    continue;
                }


                EndChar[] newEndChars = [..endChars, new('_', 2)];
                if(!TryReadText(newEndChars, endLineManagement, reduceSpaces, null, out TextWrapper? innerTextWrapper)){
                    result = null;
                    return false;
                }

                innerTextWrapper.FontStyle = innerTextWrapper.FontStyle with{ Underline = true };

                result.Add(innerTextWrapper);
                continue;
            }

            if(c == '-'){
                PureTextElement textElement = new(text.ToString());
                result.Add(textElement);
                text.Clear();

                _fileReader.Save();
                if(!_fileReader.TryGetNext(out char tempC) || tempC != '-'){
                    RecallOrErrorPop();
                    text.Append(c);
                    continue;
                }


                EndChar[] newEndChars = [..endChars, new('-', 2)];
                if(!TryReadText(newEndChars, endLineManagement, reduceSpaces, null, out TextWrapper? innerTextWrapper)){
                    result = null;
                    return false;
                }

                innerTextWrapper.FontStyle = innerTextWrapper.FontStyle with{ Striketrough = true };

                result.Add(innerTextWrapper);
                continue;
            }

            if(c == '$'){
                PureTextElement textElement = new(text.ToString());
                result.Add(textElement);
                text.Clear();

                _fileReader.Save();
                if(!_fileReader.TryGetNext(out char tempC) || tempC != '$'){
                    RecallOrErrorPop();
                    text.Append(c);
                    continue;
                }


                EndChar[] newEndChars = [..endChars, new('$', 2)];
                if(!TryReadTextNotFormatted(newEndChars, endLineManagement, reduceSpaces, null, out string? mathStr)){
                    result = null;
                    return false;
                }


                result.Add(new MathInline(mathStr));
                continue;
            }

            if(c == '@'){
                PureTextElement textElement = new(text.ToString());
                result.Add(textElement);
                text.Clear();

                if(TryReadCitation(out CitationElement? citation)){
                    result.Add(citation);
                    continue;
                }

                RecallOrErrorPop();
                text.Append(c);
            }


            text.Append(c);
        }

        string remainderText = text.ToString();
        if(!string.IsNullOrWhiteSpace(remainderText)){
            PureTextElement remainderTextElement = new(remainderText);
            result.Add(remainderTextElement);
        }

        _fileReader.DeleteLastSave();

        return true;
    }
}