namespace Thunder.Md.Logging;

using System.Text;
using Microsoft.Extensions.Logging;

public class CustomProvider: ILoggerProvider{
    public void Dispose(){
        // Nothing to do
    }

    public ILogger CreateLogger(string categoryName){
        return new CustomLogger(categoryName);
    }

    private class CustomLogger: ILogger{
        private readonly string _category;
        public CustomLogger(string category){
            _category = category;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter){
            StringBuilder message = new();
            if(logLevel >= LogLevel.Warning){
                message.Append('[');
                message.Append(logLevel);
                message.Append("] ");
            }
            message.Append(_category);
            message.Append(": ");
            message.Append(formatter(state, exception));
            ConsoleColor beforeColor = Console.ForegroundColor;
            Console.ForegroundColor = logLevel switch{
                                          LogLevel.Error or LogLevel.Critical => ConsoleColor.Red,
                                          LogLevel.Warning                    => ConsoleColor.Yellow,
                                          _                                   => Console.ForegroundColor
                                      };
            Console.WriteLine(message.ToString());
            Console.ForegroundColor = beforeColor;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState: notnull{
            return null;
        }
    }
}