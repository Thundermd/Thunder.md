namespace Thunder.Md.InternalExtensions;

using Microsoft.Extensions.Logging;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.PdfElements;
using Thunder.Md.InternalExtensions.Commands;
using Thunder.Md.PdfElements.Canvas;

public class InternalExtensionWrapper(string srcPath, ILogger logger): ThunderExtension(srcPath, logger){
    public override string Id => "internal";

    public override IEnumerable<ICommand> GetCommands(){
        yield return new Build();
    }

    public override IEnumerable<CanvasCreator> GetCanvasCreators(){
        yield return new CanvasCreator(ThunderImage.Create, null, "jpg", "jpeg", "png", "gif", "webp");
        yield return new CanvasCreator(MarkdownCanvas.Create, null, "md");
        yield return new CanvasCreator(IndexCanvas.Create, null, "toc", "sections", "content", "figures", "graphics", "tables", "references", "citations", "math", "equations", "formulas");
    }
    
    public override void PreCompile(ExtensionArgs args){
        
    }

    public override void PostCompile(ExtensionArgs args){
        
    }
}