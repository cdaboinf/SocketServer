using System.Net.Sockets;

namespace NumbersServerCode.Services;

/// <summary>
/// Socket service.
/// </summary>
public interface ISocketService
{
    /// <summary>
    /// Accept new client connection.
    /// </summary>
    /// <returns>Socket service</returns>
    Task<ISocketService> AcceptAsync();

    /// <summary>
    /// Receives data from connected client.
    /// </summary>
    /// <param name="buffer">Message buffer</param>
    /// <param name="socketFlags">Socket receive behavior</param>
    /// <returns>string value of the data sent by a client</returns>
    Task<string> ReceiveAsync(byte[] buffer, SocketFlags socketFlags);

    /// <summary>
    /// Shutdown open socket.
    /// </summary>
    /// <param name="socketShutdown"></param>
    void Shutdown(SocketShutdown socketShutdown);

    /// <summary>
    /// Close open socket.
    /// </summary>
    void Close();
    
    /// <summary>
    /// Open socket connections.
    /// </summary>
    int OpenConnections { get; }
}