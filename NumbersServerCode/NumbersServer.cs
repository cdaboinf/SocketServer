using System.Net.Sockets;
using System.Text.RegularExpressions;
using NumbersServerCode.Services;

namespace NumbersServerCode;

/// <summary>
/// Numbers server to process client connections.
/// </summary>
public class NumbersServer : INumbersServer
{
    private readonly ISocketService _listener;
    private readonly ILoggerService _loggerService;

    private readonly object _connectionsLock = new object();
    private readonly object _updateCountsLock = new object();

    private int _openConnections;
    private readonly HashSet<string> _uniqueNumbers;
    private readonly List<string> _currentUniqueNumbers;
    private int _currentDuplicateCount;
    private readonly Regex _digitsReg;

    private const string TerminateCommand = "terminate";
    private const string DigitsPattern = @"^\d{9}$";

    /// <summary>
    /// Creates a new instance of NumbersServer.
    /// </summary>
    /// <param name="listener">Socket listener</param>
    /// <param name="logger">Logger service</param>
    public NumbersServer(ISocketService listener, ILoggerService logger)
    {
        _listener = listener ?? throw new ArgumentNullException(nameof(listener));
        _loggerService = logger ?? throw new ArgumentNullException(nameof(logger));

        _uniqueNumbers = new HashSet<string>(); // insert O(1), read O(1), memory search
        _currentUniqueNumbers = new List<string>(); // temp list of uniques numbers per output round
        _digitsReg = new Regex(DigitsPattern);
    }

    /// <inheritdoc cref="INumbersServer.TotalUniqueNumbersCount"/>
    public int TotalUniqueNumbersCount => _uniqueNumbers.Count; // get current count of unique numbers, test

    /// <inheritdoc cref="INumbersServer.ListenToConnectionsAsync"/>
    public async Task ListenToConnectionsAsync(
        int concurrentConnections,
        int reportOutputDelay,
        CancellationTokenSource tokenSource)
    {
        // cancellation mechanism
        var token = tokenSource.Token;
        
        // background task to report number counts in the console window, requirement #7
        ReportToConsole(reportOutputDelay, token);

        Console.WriteLine("*** Waiting for connections ***");
        while (!token.IsCancellationRequested)
        {
            // validates max concurrent connections, requirement #1
            if (_listener.OpenConnections < concurrentConnections)
            {
                var socket = await _listener.AcceptAsync();
                Console.WriteLine($"process new client, on socket count => {_listener.OpenConnections}");
                _ = Task.Run( () => HandleConnection(socket, tokenSource), token);
            }
            else
            {
                Console.WriteLine($"Reached maximum concurrent connections {_listener.OpenConnections}");
            }
        }
    }

    private async Task HandleConnection(ISocketService socket, CancellationTokenSource cancellationToken)
    {
        try
        {
            var buffer = new byte[1_024];
            var receivedValue = await socket.ReceiveAsync(buffer, SocketFlags.None);

            await Task.Delay(5000);

            var clientNumbers = receivedValue.Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);

            if (clientNumbers.Any(n => n.Equals(TerminateCommand, StringComparison.OrdinalIgnoreCase)))
            {
                // shouts down server, requirement #8
                Console.WriteLine($"Shutting down server...");
                cancellationToken.Cancel();
            }
            // only valid data will be processed, requirement #6
            else if (clientNumbers.All(n => _digitsReg.IsMatch(n)))
            {
                // track of unique numbers and duplicate numbers, requirement #2
                ProcessClientRequest(clientNumbers);
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error: {ex}");
        }
        finally
        {
            socket.Shutdown(SocketShutdown.Receive);
        }
    }

    private void ReportToConsole(int reportDelay, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(reportDelay, cancellationToken);

                if (_currentUniqueNumbers.Any())
                {
                    // log unique numbers, requirement #4, #5
                    await _loggerService.LogToFileAsync(_currentUniqueNumbers, cancellationToken);
                }
                // report number counts in the console window, requirement #7
                Console.WriteLine(
                    $"{_currentUniqueNumbers.Count} unique numbers, " +
                    $"{_currentDuplicateCount} duplicates. " +
                    $"Unique total: {_uniqueNumbers.Count} " +
                    $"Socket count: {_listener.OpenConnections}");

                // reset last set of values
                _currentUniqueNumbers.Clear();
                _currentDuplicateCount = 0;
            }
        }, cancellationToken);
    }

    private void ProcessClientRequest(IEnumerable<string> clientNumbers)
    {
        lock (_updateCountsLock)
        {
            foreach (var number in clientNumbers)
            {
                if (!_uniqueNumbers.Contains(number))
                {
                    _uniqueNumbers.Add(number);
                    _currentUniqueNumbers.Add(number);
                }
                else
                {
                    _currentDuplicateCount++;
                }
            }
        }
    }

    // lock read and writes, interlock
    private int OpenConnections
    {
        get
        {
            lock (_connectionsLock)
            {
                return _openConnections;
            }
        }
        set
        {
            lock (_connectionsLock)
            {
                _openConnections = value;
            }
        }
    }
}