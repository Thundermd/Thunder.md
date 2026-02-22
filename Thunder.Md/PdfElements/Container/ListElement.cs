namespace Thunder.Md.PdfElements.Container;

using System.Diagnostics;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;

public class ListElement: IPdfElement{
    public bool Ordered{ get; }
    public NumberingStyle NumberingStyle { get; }
    private List<ListItem> _items = [];
    
    public ListElement(bool ordered, NumberingStyle numberingStyle, IReadOnlyList<ListItem> list){
        Ordered = ordered;
        NumberingStyle = numberingStyle;
        _items.AddRange(list);
    }
    
    public void Draw(ThunderConfig config, ThunderBuildState state, IContainer container){
        Draw(config, state, container, 0, this);
    }
    
    private static void Draw(ThunderConfig config, ThunderBuildState state, IContainer container, int padding, ListElement listElement){
        container.SemanticList().PaddingLeft(padding, Unit.Millimetre).Column(column => {
            int counter = 0;
            foreach(ListItem item in listElement._items){
                if(item is{ IsList: true, List: not null }){
                    Draw(config, state, column.Item().SemanticListItemBody(), padding + 5, item.List);
                    continue;
                }

                if(item.Text is null){
                    throw new UnreachableException();
                }

                counter++;
                int localCounter = counter;
                column.Item().SemanticListItem().Text(text => {
                    if(listElement.Ordered){
                        text.Span(NumberEncoder.Encode(localCounter, listElement.NumberingStyle));
                        text.Span(".");
                    } else{
                        text.Span("\u2022");
                    }

                    text.Span("  ");
                    item.Text.Draw(text, new FontStyle(), state, config);
                });
            }
        });
    }
}

public class ListItem{
    public ITextElement? Text { get; }
    public ListElement? List { get; }
    
    public bool IsList =>  List is not null;

    public ListItem(ITextElement text){
        Text = text;
        List = null;
    }

    public ListItem(ListElement list){
        Text = null;
        List = list;
    }
}