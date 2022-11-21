using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NumbersServerCode.Services;
using NUnit.Framework;

namespace NumbersServerCode.Tests;

public class NumberServerTests
{
    [Test]
    public async Task ListenToConnectionsAsync_Only_Unique_Numbers_Received()
    {
        var socketServiceMock = new Mock<ISocketService>();
        socketServiceMock
            .SetupSequence(x => x.AcceptAsync())
            .ReturnsAsync(GetSocketServiceMock("123456789"))
            .ReturnsAsync(GetSocketServiceMock("000000789"))
            .ReturnsAsync(GetSocketServiceMock("terminate"));

        var numbersServer = new NumbersServer(socketServiceMock.Object, Mock.Of<ILoggerService>());
        var tokenSource = new CancellationTokenSource();
        await numbersServer.ListenToConnectionsAsync(1, 0, tokenSource);

        socketServiceMock.Verify(x => x.AcceptAsync(), Times.Exactly(3));
        Assert.AreEqual(2, numbersServer.TotalUniqueNumbersCount);
    }
    
    [Test]
    public async Task ListenToConnectionsAsync_Unique_And_Duplicate_Numbers_Received()
    {
        var socketServiceMock = new Mock<ISocketService>();
        socketServiceMock
            .SetupSequence(x => x.AcceptAsync())
            .ReturnsAsync(GetSocketServiceMock("123456789"))
            .ReturnsAsync(GetSocketServiceMock("000000789"))
            .ReturnsAsync(GetSocketServiceMock("000000789"))
            .ReturnsAsync(GetSocketServiceMock("000000789"))
            .ReturnsAsync(GetSocketServiceMock("terminate"));

        var numbersServer = new NumbersServer(socketServiceMock.Object, Mock.Of<ILoggerService>());
        var tokenSource = new CancellationTokenSource();
        await numbersServer.ListenToConnectionsAsync(1, 0, tokenSource);

        socketServiceMock.Verify(x => x.AcceptAsync(), Times.Exactly(5));
        Assert.AreEqual(2, numbersServer.TotalUniqueNumbersCount);
    }
    
    [Test]
    public async Task ListenToConnectionsAsync_Unique_Numbers_Invalid_Values_Received()
    {
        var socketServiceMock = new Mock<ISocketService>();
        socketServiceMock
            .SetupSequence(x => x.AcceptAsync())
            .ReturnsAsync(GetSocketServiceMock("123456789"))
            .ReturnsAsync(GetSocketServiceMock("000000789"))
            .ReturnsAsync(GetSocketServiceMock("-@3330-"))
            .ReturnsAsync(GetSocketServiceMock("abc-#*"))
            .ReturnsAsync(GetSocketServiceMock("terminate"));

        var numbersServer = new NumbersServer(socketServiceMock.Object, Mock.Of<ILoggerService>());
        var tokenSource = new CancellationTokenSource();
        await numbersServer.ListenToConnectionsAsync(1, 0, tokenSource);

        socketServiceMock.Verify(x => x.AcceptAsync(), Times.Exactly(5));
        Assert.AreEqual(2, numbersServer.TotalUniqueNumbersCount);
    }
    
    [Test]
    public async Task ListenToConnectionsAsync_Invalid_Values_Received()
    {
        var socketServiceMock = new Mock<ISocketService>();
        socketServiceMock
            .SetupSequence(x => x.AcceptAsync())
            .ReturnsAsync(GetSocketServiceMock("-@3330-"))
            .ReturnsAsync(GetSocketServiceMock("abc-#*"))
            .ReturnsAsync(GetSocketServiceMock("terminate"));

        var numbersServer = new NumbersServer(socketServiceMock.Object, Mock.Of<ILoggerService>());
        var tokenSource = new CancellationTokenSource();
        await numbersServer.ListenToConnectionsAsync(1, 0, tokenSource);

        socketServiceMock.Verify(x => x.AcceptAsync(), Times.Exactly(3));
        Assert.AreEqual(0, numbersServer.TotalUniqueNumbersCount);
    }
    
    [Test]
    public async Task ListenToConnectionsAsync_Terminate_Command_Received()
    {
        var socketServiceMock = new Mock<ISocketService>();
        socketServiceMock
            .Setup(x => x.AcceptAsync())
            .ReturnsAsync(GetSocketServiceMock("terminate"));

        var numbersServer = new NumbersServer(socketServiceMock.Object, Mock.Of<ILoggerService>());
        var tokenSource = new CancellationTokenSource();
        await numbersServer.ListenToConnectionsAsync(1, 0, tokenSource);

        socketServiceMock.Verify(x => x.AcceptAsync(), Times.Exactly(1));
        Assert.AreEqual(0, numbersServer.TotalUniqueNumbersCount);
    }

    /// <summary>
    /// Creat a socket service mock.
    /// </summary>
    /// <param name="receiveAsyncValue">Client received value</param>
    /// <returns>ISocketService</returns>
    private static ISocketService GetSocketServiceMock(string receiveAsyncValue)
    {
        var mock = new Mock<ISocketService>();
        mock .Setup(x => x.ReceiveAsync(It.IsAny<byte[]>(), It.IsAny<SocketFlags>()))
            .ReturnsAsync(receiveAsyncValue);

        return mock.Object;
    }
}