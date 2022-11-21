namespace NumbersServerCode;

/// <summary>
/// Number server to process client connections.
/// </summary>
public interface INumbersServer
{
    /// <summary>
    /// Total count of unique numbers that has been processed.
    /// </summary>
    int TotalUniqueNumbersCount { get; }

    /// <summary>
    /// Listen to client connections to process digits requests.
    /// </summary>
    /// <param name="concurrentConnections">Maximum number of concurrent connections</param>
    /// <param name="reportOutputDelay">Report delay interval</param>
    /// <param name="tokenSource">Cancellation token source</param>
    /// <returns>Task</returns>
    Task ListenToConnectionsAsync(int concurrentConnections, int reportOutputDelay, CancellationTokenSource tokenSource);
}