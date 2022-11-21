namespace NumbersServerCode.Models;

/// <summary>
/// Socket service options model.
/// </summary>
public class SocketServiceOptions
{
    /// <summary>
    /// TCP Port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Server host name.
    /// </summary>
    public string? Hostname { get; set; }
}