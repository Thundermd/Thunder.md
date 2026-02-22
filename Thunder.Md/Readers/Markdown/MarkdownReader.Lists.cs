namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.PdfElements.Container;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private static readonly char[] _unorderedListChars = ['-', '*'];

    private bool TryReadList(char firstChar, [NotNullWhen(true)] out ListElement? listElement) =>
        TryReadList(firstChar, 0, out listElement);

    private bool TryReadList(char firstChar, int whitespaces, [NotNullWhen(true)] out ListElement? listElement){
        if(firstChar is '-' or '*'){
            return TryReadUnorderedList(whitespaces, firstChar, out listElement);
        } else{
            return TryReadOrderedList(whitespaces, firstChar, out listElement);
        }
    }

    private bool TryReadOrderedList(int whitespaces, char firstChar, [NotNullWhen(true)] out ListElement? listElement){
        return TryReadAnyList(whitespaces, firstChar, true, out listElement);
    }


    private bool TryReadUnorderedList(int whitespaces, char firstChar,
                                      [NotNullWhen(true)] out ListElement? listElement){
        return TryReadAnyList(whitespaces, firstChar, false, out listElement);
    }

    private bool TryReadAnyList(int whitespaces, char firstChar, bool isOrdered, out ListElement? listElement){
        List<ListItem> listItems = [];
        NumberingStyle numberingStyle = NumberingStyle.Numeric;
        while(true){
            _fileReader.Save();
            int lineWhiteSpaces = 0;
            char c;
            while(true){
                if(listItems.Count == 0){
                    c = firstChar;
                } else if(!_fileReader.TryGetNext(out c)){
                    listElement = null;
                    return false;
                }

                if(!char.IsWhiteSpace(c) || c == '\n'){
                    break;
                }

                lineWhiteSpaces++;
            }

            if(listItems.Count > 0 && lineWhiteSpaces < whitespaces){
                break;
            }

            if(lineWhiteSpaces > whitespaces){
                if(!TryReadList(c, lineWhiteSpaces, out ListElement? innerList)){
                    break;
                }

                listItems.Add(new ListItem(innerList));
                continue;
            }

            if(!TryReadListLabel(c, isOrdered, out string? lineLabel)){
                break;
            }

            if(isOrdered){
                NumberingStyle lineNumberingStyle;
                if(lineLabel.All(x => x is 'i' or 'v')){
                    lineNumberingStyle = NumberingStyle.RomanLowercase;
                } else if(lineLabel.All(x => x is 'I' or 'V')){
                    lineNumberingStyle = NumberingStyle.RomanUppercase;
                } else if(lineLabel.All(x => char.IsLetter(x) && char.IsLower(x))){
                    lineNumberingStyle = NumberingStyle.AlphabeticLowercase;
                } else if(lineLabel.All(x => char.IsLetter(x) && char.IsUpper(x))){
                    lineNumberingStyle = NumberingStyle.AlphabeticUppercase;
                } else{
                    lineNumberingStyle = NumberingStyle.Numeric;
                }

                if(listItems.Count == 0){
                    numberingStyle = lineNumberingStyle;
                } else if(lineNumberingStyle != numberingStyle){
                    _logger.LogWarning(_fileReader, "Unexpected list format. Will use the '{format}' format of the first item instead", numberingStyle);
                }
            }

            if(!TryReadText([new EndChar('\n', 1)], EndLineManagement.Ignore, true, null, out TextWrapper? text)){
                break;
            }

            listItems.Add(new ListItem(text));

            _fileReader.DeleteLastSave();
        }

        RecallOrErrorPop();


        if(listItems.Count == 0){
            listElement = null;
            return false;
        }

        listElement = new ListElement(isOrdered, numberingStyle, listItems);
        return true;
    }

    private bool TryReadListLabel(char firstChar, bool isOrdered, [NotNullWhen(true)] out string? label){
        if(!isOrdered){
            label = new string(firstChar, 1);
            return _unorderedListChars.Contains(firstChar) && _fileReader.TryGetNext(out char whitespace) &&
                   char.IsWhiteSpace(whitespace) && whitespace != '\n';
        }

        StringBuilder labelBuilder = new();
        labelBuilder.Append(firstChar);
        int counter = 1;
        char c;
        while(true){
            if(!_fileReader.TryGetNext(out c)){
                label = null;
                return false;
            }

            if(!char.IsLetterOrDigit(c)){
                break;
            }

            counter++;
            if(counter > 6){
                label = null;
                return false;
            }
        }

        if(c != '.' || !_fileReader.TryGetNext(out c) || !char.IsWhiteSpace(c) || c == '\n'){
            label = null;
            return false;
        }

        label = labelBuilder.ToString();
        return true;
    }
}