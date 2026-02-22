namespace Thunder.Md.CodeExtensions;

using Microsoft.Extensions.Logging;
using Thunder.Md.Readers;

public static class Logging{
    extension(ILogger logger){
        public void LogInformation(FileReader fileReader, string message, params object?[] args) =>
            logger.LogInformation($"{fileReader.Path}:{fileReader.Line}:{fileReader.Column} | {message}", args);
        public void LogWarning(FileReader fileReader, string message, params object?[] args) =>
            logger.LogWarning($"{fileReader.Path}:{fileReader.Line}:{fileReader.Column} | {message}", args);
        public void LogError(FileReader fileReader, string message, params object?[] args) =>
            logger.LogError($"{fileReader.Path}: {fileReader.Line}:{fileReader.Column} | {message}", args);
    }
}