using Microsoft.Extensions.Options;
using NumbersServerCode.Models;

namespace NumbersServerCode.Services;

/// <summary>
/// File log service.
/// </summary>
public class LoggerService : ILoggerService
{
    private readonly string _file;
    private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Creates a new instance of LoggerService.
    /// </summary>
    /// <param name="options">Log service options</param>
    public LoggerService(IOptions<LoggerServiceOptions> options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _file = options.Value?.Filename ?? throw new ArgumentException(nameof(options.Value.Filename));

        // create or override log file, requirement #3
        CreateLogFile();
    }

    /// <inheritdoc cref="ILoggerService.LogToFileAsync"/>
    public async Task LogToFileAsync(IEnumerable<string> logs, CancellationToken cancellationToken)
    {
        try
        {
            await SemaphoreSlim.WaitAsync(cancellationToken);
            {
                await using StreamWriter file = new(_file, append: true);
                foreach (var log in logs)
                {
                    await file.WriteLineAsync(log);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        finally
        {
            SemaphoreSlim.Release();
        }
    }

    private void CreateLogFile()
    {
        try
        {
            if (File.Exists(_file))
            {
                File.Delete(_file);
            }

            using var fs = File.Create(_file);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}