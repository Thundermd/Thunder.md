namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Container;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{
    private bool TryReadHeadline([NotNullWhen(true)] out IPdfElement? pdfElement){
        int layer = 1;
        bool stayedInFile;
        char c;
        while((stayedInFile = _fileReader.TryGetNext(out c)) && _shouldRun && c == '#'){
            layer++;
        }

        if(!stayedInFile){
            _logger.LogWarning(_fileReader, "Reached unexpected end of file. With start of a headline");
            pdfElement = null;
            return false;
        }

        bool indexed = c != '*';
        if(!indexed && !_fileReader.TryGetNext(out c)){
            _logger.LogWarning(_fileReader, "Reached unexpected end of file. With start of a headline");
            pdfElement = null;
            return false;
        }

        if(!char.IsWhiteSpace(c) || !TryReadText([new EndChar('\n', 1)], EndLineManagement.Ignore, true, null,
                                                 out TextWrapper? textElement)){
            pdfElement = null;
            return false;
        }

        pdfElement = new HeadlineElement(layer, indexed, textElement, Program.CreateLogger<HeadlineElement>());
        return true;
    }

}