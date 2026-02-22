namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private bool TryReadCitation([NotNullWhen(true)] out CitationElement? citationElement){
        StringBuilder label = new();
        _fileReader.Save();
        while(_fileReader.TryGetNext(out char c) && (char.IsLetterOrDigit(c) || c == '-' || c == '_')){
            _fileReader.DeleteLastSave();
            _fileReader.Save();
            label.Append(c);
        }

        RecallOrErrorPop();

        if(label.Length == 0){
            citationElement = null;
            return false;
        }

        citationElement = new CitationElement(label.ToString());
        return true;
    }
}