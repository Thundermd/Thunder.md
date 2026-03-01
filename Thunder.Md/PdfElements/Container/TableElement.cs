namespace Thunder.Md.PdfElements.Container;

using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.Building;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.InternalExtensions;
using Thunder.Md.PdfElements.Inline;

public record struct TableCell(ITextElement Text, bool IsHeader);

public class TableElement: IPdfElement{
    private TableCell[,] _table;
    private ThunderIndexItem? _indexItem;
    private readonly ITextElement? _caption;
    private readonly string? _referenceId;

    public TableElement(TableCell[,] table, ITextElement? caption, string? referenceId){
        _table = table;
        _caption = caption;
        _referenceId = referenceId;
    }

    public void Draw(ThunderConfig config, IThunderBuildState state, IContainer container){
        container.Column(container => {
            IContainer tableItem = container.Item();
            if(_indexItem is not null){
                tableItem = tableItem.Section(_indexItem.SectionId);
            }
            tableItem.SemanticTable().AddBorderRadius(config).Table(table => {
                table.ColumnsDefinition(columns => {
                    for(int x = 0; x < _table.GetLength(0); x++){
                        columns.RelativeColumn();
                    }
                });

                var headerBackgroundColor = config.Project!.Colors[0];


                var headerTextStyle
                    = new FontStyle(Bold: true,
                                    Color: config.Project!.ContrastColorTo(headerBackgroundColor));

                for(int y = 0; y < _table.GetLength(1); y++){
                    for(int x = 0; x < _table.GetLength(0); x++){
                        TableCell cell = _table[x, y];
                        IContainer cellElement = table.Cell();
                        if(cell.IsHeader){
                            cellElement = cellElement.Background(headerBackgroundColor.ToPdfColor());
                        }

                        cellElement = cellElement.Border(1, config.Project!.TextColor.ToPdfColor());

                        cellElement.Text(text => {
                            var fontStyle = cell.IsHeader ? headerTextStyle : new FontStyle();
                            cell.Text.Draw(text, fontStyle, state, config);
                        });
                    }
                }
            });

            if(_caption is not null){
                //TODO: match position to config
                if(_indexItem is null){
                    throw new UnreachableException();
                }

                TextWrapper wrapper = new TextWrapper(new FontStyle());

                TextWrapper indexElement = new TextWrapper(config.Project!.NumberingStyle);
                indexElement.Add(new PureTextElement(_indexItem.ReferenceText + " "));
                
                wrapper.Add(indexElement);
                wrapper.Add(_caption);
                
                container.Item().Height(config.Project!.FontSize * 0.5f);
                container.Item().SemanticCaption().AlignCenter().Text(text => {
                    wrapper.Draw(text, new FontStyle(), state, config);
                });
            }
        });
    }

    public void Prebuild(ThunderConfig config, IThunderBuildState state){
        if(_caption is null){
            return;
        }
        _indexItem = state.GetNextIndexItem(ThunderBuildState.TableGroup, _caption.Text, _referenceId);
    }
}