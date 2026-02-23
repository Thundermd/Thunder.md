namespace Thunder.Md.Readers.Markdown;

using System.Diagnostics.CodeAnalysis;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.PdfElements.Container;
using Thunder.Md.PdfElements.Inline;

public partial class MarkdownReader{

    private bool TryReadTable([NotNullWhen(true)] out TableElement? tableElement){
        List<List<TableCell>> rows = [];
        
        char c;
        while(true){
            _fileReader.Save();
            if(rows.Count > 0){
                if(!_fileReader.TryGetNext(out c) || c != '|'){
                    break;
                }
            }
            
            List<TableCell> row = [];
            do{
                if(!TryReadText([new EndChar('|', 1), new EndChar('\n', 1), new EndChar(':', 1)], EndLineManagement.Ignore, true, null,
                                out TextWrapper? text, out c)){
                    tableElement = null;
                    return false;
                }

                if(c != '\n'){
                    row.Add(new TableCell(text, c == ':'));
                }
            } while(c != '\n');

            if(row.Count == 0){
                break;
            }

            if(row.All(cell => cell.Text.Text.All(x => char.IsWhiteSpace(x) || x == '-'))){
                if(rows.Count > 0){
                    rows[^1] = rows[^1].Select(x => x with { IsHeader = true }).ToList();
                }
            } else{
                rows.Add(row);
            }


            _fileReader.DeleteLastSave();
        }
        
        RecallOrErrorPop();
        _fileReader.Save();
        TextWrapper? caption = null;
        if(_fileReader.TryGetNext(out c) && c == '[' && TryReadText([new EndChar(']', 1)], EndLineManagement.Error, true, null, out TextWrapper? captionText, out _)){
            caption = captionText;
            _fileReader.DeleteLastSave();
        } else{
            RecallOrErrorPop();
        }

        if(rows.Count == 0){
            tableElement = null;
            return false;
        }

        int height = rows.Count;
        int minWidth = rows.Min(x => x.Count);
        if(rows.Any(x => x.Count != minWidth)){
            _logger.LogWarning(_fileReader, "Table has invalid format. Not all rows have the same amount of cells. Maybe some cells are missing in the final table.");
        }
        
        TableCell[,] table =  new TableCell[minWidth, height];

        for(int y = 0; y < height; y++){
            List<TableCell> row = rows[y];
            for(int x = 0; x < minWidth; x++){
                TableCell  cell = row[x];
                table[x, y] = cell;
            }
        }
        
        tableElement = new TableElement(table, caption);
        return true;
    }
}