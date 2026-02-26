namespace Thunder.Md.Readers;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.RegularExpressions;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using Thunder.Md.CodeExtensions;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Extensions.PdfElements;

public class ExtensionLoader{
   private readonly ILogger _logger;

    private readonly string _strPath;

    private readonly Dictionary<ICommand, ThunderExtension> _commandExtensionLookup = new();
    private readonly Dictionary<ThunderExtension, string> _extensionPathLookup = new();
    private readonly Dictionary<string, ICommand> _commands = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, ThunderExtension> _extensions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, CanvasCreator> _canvasCreatorsFileExtensions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, CanvasCreator> _canvasCreatorsProtocols = new(StringComparer.InvariantCultureIgnoreCase);
    
    
    private readonly Dictionary<string, InlineCanvasCreator> _inlineCanvasCreatorsFileExtensions = new(StringComparer.InvariantCultureIgnoreCase);
    private readonly Dictionary<string, InlineCanvasCreator> _inlineCanvasCreatorsProtocols = new(StringComparer.InvariantCultureIgnoreCase);
    
    private readonly Dictionary<CanvasCreator, ThunderExtension> _canvasExtensionLookup = new();
    private readonly Dictionary<InlineCanvasCreator, ThunderExtension> _inlineCanvasExtensionLookup = new();

    public ExtensionLoader(ILogger logger, string strPath){
        _logger = logger;
        _strPath = strPath;
    }

    public bool TryGetCommand(string name, [NotNullWhen(true)] out ICommand? command) =>
        _commands.TryGetValue(name, out command);


    public bool TryGetExtension(string name, [NotNullWhen(true)] out ThunderExtension? extension) =>
        _extensions.TryGetValue(name, out extension);

    public void LoadExtensions(){
        if(!Directory.Exists(ThunderPaths.ExtensionPath)){
            Directory.CreateDirectory(ThunderPaths.ExtensionPath);
            return;
        }

        // create plugin loaders
        IEnumerable<(string Path, string RootFile)> extensionDlls = Directory.GetDirectories(ThunderPaths.ExtensionPath)
                                                                             .Select(x => (x, RootFile: Path.Combine(x, Path.GetFileName(x) + ".dll")))
                                                                             .Where(x => File.Exists(x.RootFile));

        IEnumerable<(string Path, PluginLoader Loader)> extensionLoaders = extensionDlls.Select(x => (x.Path,
            PluginLoader.CreateFromAssemblyFile(
                                                x.RootFile,
                                                sharedTypes:[
                                                                typeof(ThunderExtension),
                                                                typeof(ICommand),
                                                                typeof(ILogger),
                                                                typeof(ThunderConfig),
                                                                typeof(ThunderProjectConfig),
                                                                typeof(ThunderGlobalConfig),
                                                                typeof(ExtensionArgs),
                                                            ])));

        IEnumerable<(string Path, Type Type)> extensionTypes = extensionLoaders.SelectMany(x => x.Loader
            .LoadDefaultAssembly()
            .GetTypes()
            .Where(t => typeof(ThunderExtension)
                            .IsAssignableFrom(t) &&
                        !t.IsAbstract).Select(t => (x.Path, t)));

        foreach((string path, Type pluginType) in extensionTypes){
            AddExtension(pluginType, path);
        }
    }

    public ExtensionArgs GetExtensionConfig(InlineCanvasCreator canvasCreator, ThunderConfig config){
        if(!_inlineCanvasExtensionLookup.TryGetValue(canvasCreator, out ThunderExtension? extension)){
            throw new UnreachableException($"There is no extension for command {canvasCreator.GetType().Name}");
        }

        return GetExtensionConfig(extension, config);
    }
    public ExtensionArgs GetExtensionConfig(CanvasCreator canvasCreator, ThunderConfig config){
        if(!_canvasExtensionLookup.TryGetValue(canvasCreator, out ThunderExtension? extension)){
            throw new UnreachableException($"There is no extension for command {canvasCreator.GetType().Name}");
        }

        return GetExtensionConfig(extension, config);
    }
    
    public ExtensionArgs GetExtensionConfig(ICommand command, ThunderConfig config){
        if(!_commandExtensionLookup.TryGetValue(command, out ThunderExtension? extension)){
            throw new UnreachableException($"There is no extension for command {command.Name}");
        }

        return GetExtensionConfig(extension, config);
    }

    public ExtensionArgs GetExtensionConfig(ThunderExtension extension, ThunderConfig config, ILogger? logger = null){
        if(!_extensionPathLookup.TryGetValue(extension, out string? path)){
            throw new UnreachableException("There is no extension path for the extension");
        }

        if(logger is null){
            logger = Program.CreateLogger(extension.GetType());
        }

        return new ExtensionArgs(config, path, logger);
    }

    public void AddExtension<T>() where T: ThunderExtension{
        AddExtension(typeof(T), ThunderPaths.ExtensionPath);
    }

    public void AddExtension(Type type, string path){
        if(!TryCreateExtension(type, out ThunderExtension? extension)){
            return;
        }

        _extensionPathLookup.Add(extension, path);
        _extensions.Add(extension.Id, extension);
        foreach(ICommand command in extension.GetCommands()){
            _commandExtensionLookup.Add(command, extension);
            _commands.Add(command.Name, command);
        }
        
        foreach(var inlineCanvasCreator in extension.GetInlineCanvasCreators()){
            _inlineCanvasExtensionLookup.Add(inlineCanvasCreator, extension);
            
            foreach(string fileExtension in inlineCanvasCreator.FileExtension){
                _inlineCanvasCreatorsFileExtensions.Add(fileExtension, inlineCanvasCreator);
            }

            if(inlineCanvasCreator.Protocol is not null){
                _inlineCanvasCreatorsProtocols.Add(inlineCanvasCreator.Protocol, inlineCanvasCreator);
            }
        }
        
        foreach(var canvasCreator in extension.GetCanvasCreators()){
            _canvasExtensionLookup.Add(canvasCreator, extension);
            
            foreach(string fileExtension in canvasCreator.FileExtension){
                _canvasCreatorsFileExtensions.Add(fileExtension, canvasCreator);
            }

            if(canvasCreator.Protocol is not null){
                _canvasCreatorsProtocols.Add(canvasCreator.Protocol, canvasCreator);
            }
        }
    }


    private bool TryCreateExtension(Type type, [NotNullWhen(true)] out ThunderExtension? extension){
        ConstructorInfo? constructor = type.GetConstructors()
                                           .FirstOrDefault(x => FindConstructor(x, typeof(string), typeof(ILogger)));
        if(constructor is null){
            _logger.LogWarning("Unable to initiate Extension {Name}. Invalid Constructor", type.Name);
            extension = null;
            return false;
        }

        ILogger logger = Program.CreateLogger(type);
        object extensionObj;
        try{
            extensionObj = constructor.Invoke([_strPath, logger]);
        } catch(Exception e){
            _logger.LogWarning("Unable to initiate Extension {Name}, unknown error.", type.Name);
            _logger.LogTrace(e.ToString());
            extension = null;
            return false;
        }

        extension = extensionObj as ThunderExtension;
        if(extension is null){
            _logger.LogWarning("Unable to initiate Extension {Name}, unknown error.", type.Name);
        }

        return extension is not null;
    }

    private static bool FindConstructor(ConstructorInfo info, params Type[] args){
        ParameterInfo[] parameters = info.GetParameters();
        return parameters.Length == args.Length &&
               args.Select((x, index) => x == parameters[index].ParameterType).All(x => x);
    }

    public IEnumerable<string> GetCommandNames(){
        return _commands.Keys;
    }

    public bool TryGetCanvasElement(string path, ITextElement? altTextElement, string? label,
                                    Dictionary<string, string?> parameters, ThunderConfig config,
                                    [NotNullWhen(true)] out ICanvasElement? canvasElement){
        Regex urlRegex = new(@"(.*):\/\/", RegexOptions.Compiled);
        var match = urlRegex.Match(path);
        CanvasCreator? creator = null;
        if(match.Success){
            var protocol = match.Groups[1].Value;
            if(!_canvasCreatorsProtocols.TryGetValue(protocol, out creator)){
                canvasElement = null;
                return false;
            }
        } else{
            string fileExtension = path.Contains('.') ? Path.GetExtension(path).Trim('.') : path;
            if(!_canvasCreatorsFileExtensions.TryGetValue(fileExtension, out creator)){
                canvasElement = null;
                return false;
            }
        }

        return creator.Creator(GetExtensionConfig(creator, config), path, altTextElement, label, parameters, out canvasElement);
    }

    public bool TryGetInlineCanvas(string path, ITextElement? altTextElement, string? label,
                                   Dictionary<string, string?> parameters, ThunderConfig config,
                                   [NotNullWhen(true)] out IInlineCanvasElement? inlineCanvasElement){
        Regex urlRegex = new(@"(.*):\/\/", RegexOptions.Compiled);
        var match = urlRegex.Match(path);
        InlineCanvasCreator? creator = null;
        if(match.Success){
            var protocol = match.Groups[1].Value;
            if(!_inlineCanvasCreatorsProtocols.TryGetValue(protocol, out creator)){
                inlineCanvasElement = null;
                return false;
            }
        } else{
            string fileExtension = path.Contains('.') ? Path.GetExtension(path).Trim('.') : path;
            if(!_inlineCanvasCreatorsFileExtensions.TryGetValue(fileExtension, out creator)){
                inlineCanvasElement = null;
                return false;
            }
        }

        return creator.Creator(GetExtensionConfig(creator, config), path, altTextElement, label, parameters, out inlineCanvasElement);
    }
}