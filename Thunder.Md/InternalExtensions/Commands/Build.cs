namespace Thunder.Md.InternalExtensions.Commands;

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Thunder.Md.Extensions;
using Thunder.Md.Extensions.Config;
using Thunder.Md.Readers;
using Thunder.Md.Readers.Markdown;
using Thunder.Md.Writers;

public class Build: ICommand{
    public string Name => "build";

    public string Help =>
        $"build project to a pdf file\n" +
        $"\tthundermd {Name}\n" +
        $"\tthundermd {Name} [path to project]\n";
    public void Execute(string[] args, ExtensionArgs extensionArgs){
        var startTime = Environment.TickCount64;
        if(!Directory.Exists(ThunderPaths.Build)){
            Directory.CreateDirectory(ThunderPaths.Build);
        }
        
        var projectConfig = extensionArgs.Config.Project;
        if(projectConfig is null){
            extensionArgs.Logger.LogError("Project config not found in current project");
            return;
        }

        if(projectConfig.RootFile is null){
            extensionArgs.Logger.LogError("Root file not found in current project config file. Please add 'root-file' attribute");
            return;
        }
        
        var rootFilePath = Path.Combine(ThunderPaths.Source, projectConfig.RootFile);
        
        MarkdownReader markdownReader = new(rootFilePath, extensionArgs.Config, Program.ExtensionLoader!, Program.CreateLogger<MarkdownReader>());

        var pdfElements = markdownReader.Read().ToImmutableArray();


        var buildFileName = $"{Path.GetFileNameWithoutExtension(projectConfig.RootFile)}.pdf";
        var buildFilePath = Path.Combine(ThunderPaths.Build, buildFileName);
        var pdfBuilder = new PdfBuilder(Program.CreateLogger<PdfBuilder>(), extensionArgs.Config, buildFilePath);
        pdfBuilder.Write(pdfElements);
        var buildTime = Environment.TickCount64 - startTime;
        var timeSpan  = TimeSpan.FromMilliseconds(buildTime);
        extensionArgs.Logger.LogInformation($"Built to '{buildFilePath}' in {timeSpan.TotalSeconds:0.00}s");
    }
}