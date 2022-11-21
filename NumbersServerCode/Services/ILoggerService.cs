namespace NumbersServerCode.Services;

/// <summary>
/// File log service.
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Log collection of string digits.
    /// </summary>
    /// <param name="logs">list of digit logs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task LogToFileAsync(IEnumerable<string> logs, CancellationToken cancellationToken);
}