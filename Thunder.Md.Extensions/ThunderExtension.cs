namespace Thunder.Md.Extensions;

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Thunder.Md.Extensions.PdfElements;

public abstract class ThunderExtension{
    private readonly List<string> _tempFiles = [];
    
    protected string SrcPath{ get; }
    protected ILogger Logger{ get; }
    public abstract string Id{ get; }
    
    
    protected ThunderExtension(string srcPath, ILogger logger){
        SrcPath = srcPath;
        Logger = logger;
    }

    public virtual IEnumerable<ICommand> GetCommands(){
        yield break;
    }

    public virtual IEnumerable<CanvasCreator> GetCanvasCreators(){
        yield break;
    }
    public virtual IEnumerable<InlineCanvasCreator> GetInlineCanvasCreators(){
        yield break;
    }
    public virtual void PreCompile(ExtensionArgs args){}
    public virtual void PostCompile(ExtensionArgs args){}
    
    
    
    public IReadOnlyList<string> GetTempFiles() => _tempFiles;

    protected bool TryCreateTempFile(string name, [NotNullWhen(true)] out Stream? fileStream){
        string path = Path.Combine(SrcPath, name);
        try{
            fileStream = File.Create(path);
            _tempFiles.Add(path);
        } catch{
            fileStream = null;
            return false;
        }

        return true;
    }

    protected bool TryWriteTempFile(string name, string text){
        if(!TryCreateTempFile(name, out Stream? fileStream)){
            return false;
        }
        
        using StreamWriter writer = new(fileStream);
        writer.Write(text);
        return true;
    }
}