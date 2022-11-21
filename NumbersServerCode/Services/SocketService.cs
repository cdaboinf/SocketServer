using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using NumbersServerCode.Models;

namespace NumbersServerCode.Services;

/// <summary>
/// Socket service.
/// </summary>
public class SocketService : ISocketService
{
    private readonly Socket _socket;
    private static int _openSockets;
    private readonly object _countsLock = new();

    /// <summary>
    /// Creates a new instance of the socket service.
    /// </summary>
    /// <param name="options">Socket service options</param>
    public SocketService(IOptions<SocketServiceOptions> options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Value?.Hostname == null)
        {
            throw new ArgumentException(nameof(options.Value.Hostname));
        }

        if (options.Value?.Port == null)
        {
            throw new ArgumentException(nameof(options.Value.Port));
        }

        var host = options.Value.Hostname;
        var ipHost = Dns.GetHostEntry(host);
        var ipAddr = ipHost.AddressList[0];
        var endPoint = new IPEndPoint(ipAddr, options.Value.Port);

        _socket = new Socket(
            endPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

        _socket.Bind(endPoint);
        _socket.Listen();

        _openSockets = 0;
    }

    private SocketService(Socket socket)
    {
        _socket = socket;
    }

    /// <inheritdoc cref="ISocketService.AcceptAsync"/>
    public async Task<ISocketService> AcceptAsync()
    {
        var socket = await _socket.AcceptAsync();
        OpenConnections++;
        return new SocketService(socket);
    }

    /// <inheritdoc cref="ISocketService.ReceiveAsync"/>
    public async Task<string> ReceiveAsync(byte[] buffer, SocketFlags socketFlags)
    {
        var received = await _socket.ReceiveAsync(buffer, SocketFlags.None);
        var receivedValue = Encoding.UTF8.GetString(buffer, 0, received);
        return receivedValue;
    }

    /// <inheritdoc cref="ISocketService.Close"/>
    public void Close()
    {
        this.Shutdown(SocketShutdown.Both);
    }

    /// <inheritdoc cref="ISocketService.Shutdown"/>
    public void Shutdown(SocketShutdown socketShutdown)
    {
        _socket.Close((int)socketShutdown);
        OpenConnections--;
    }

    /// <inheritdoc cref="ISocketService.OpenConnections"/>
    public int OpenConnections
    {
        get
        {
            lock (_countsLock)
            {
                return _openSockets;
            }
        }
        set
        {
            lock (_countsLock)
            {
                _openSockets = value;
            }
        }
    }
}