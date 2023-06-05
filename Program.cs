using System;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class PortForwarder
{
    private readonly string comPortName;
    private readonly IPAddress tcpAddress;
    private readonly int tcpPort;

    public PortForwarder(string comPortName, string tcpAddress, int tcpPort)
    {
        this.comPortName = comPortName;
        this.tcpAddress = IPAddress.Parse(tcpAddress);
        this.tcpPort = tcpPort;
    }

    public async Task StartAsync(CancellationToken mainCancellationToken)
    {
        SerialPort? comPort = null;
        TcpListener? tcpListener = null;

        while (!mainCancellationToken.IsCancellationRequested)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(mainCancellationToken);
            try
            {
                comPort = new SerialPort(comPortName);
                comPort.Open();

                tcpListener = new TcpListener(tcpAddress, tcpPort);
                tcpListener.Start();

                Console.WriteLine($"Ready! forwarding Port:{comPortName} <-> TCP:{tcpAddress.ToString()}:{tcpPort.ToString()}");
                Console.Write("Waiting for connection ... ");
                using (var tcpClient = await tcpListener.AcceptTcpClientAsync())
                {
                    var tcpStream = tcpClient.GetStream();
                    Console.WriteLine("Connected!");
                    await Task.WhenAll(
                        ForwardComToTcpAsync(comPort, tcpStream, cts),
                        ForwardTcpToComAsync(tcpStream, comPort, cts.Token)
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}, retrying...");
                await Task.Delay(1000); // wait 1 sec before retry
            }
            finally
            {
                comPort?.Close();
                comPort?.Dispose();
                tcpListener?.Stop();
            }
        }
    }

    private async Task ForwardComToTcpAsync(SerialPort comPort, NetworkStream tcpStream, CancellationTokenSource cts)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                int bytesRead = await comPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                if (bytesRead > 0)
                {
#if EXTRA_DEBUG
                    Console.WriteLine("Com->Tcp");
#endif
                    await tcpStream.WriteAsync(buffer, 0, bytesRead, cts.Token);
                }
            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"COM->TCP: Failed ({ex.Message}), restarting...");
            cts.Cancel();
        }
    }

    private async Task ForwardTcpToComAsync(NetworkStream tcpStream, SerialPort comPort, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Tcp2Com Cancelled");
                }
                int bytesRead = await tcpStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead > 0)
                {
#if EXTRA_DEBUG
                    Console.WriteLine("Tcp->Com");
#endif
                    await comPort.BaseStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP->COM: Failed ({ex.Message}), restarting...");
            throw;
        }
    }

    public static string GetWSLIpAddress()
    {
        var name = "vEthernet (WSL)";
        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        foreach (var ni in networkInterfaces)
        {
            if (ni.Name == name && ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
            {
                var ipProps = ni.GetIPProperties();
                foreach (var ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        throw new Exception("Could not find IP address for 'vEthernet (WSL)'");
    }

    public static async Task Main(string[] args)
    {
        if (args.Length != 3)
        {
            Console.WriteLine("Usage: com2tcp.exe COMPort TCPAddress TCPPort");
            return;
        }

        var comPortName = args[0];
        var tcpAddress = args[1];
        if (tcpAddress.ToLower() == "wsl")
        {
            tcpAddress = GetWSLIpAddress();
        }
        else if (!IPAddress.TryParse(tcpAddress, out _))
        {
            Console.WriteLine("The TCPAddress must be an IP address or 'wsl'.");
            return;
        }
        if (!int.TryParse(args[2], out var tcpPort))
        {
            Console.WriteLine("The TCPPort must be an integer.");
            return;
        }

        var portForwarder = new PortForwarder(comPortName, tcpAddress, tcpPort);
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            if (!cts.IsCancellationRequested)
            {
                Console.WriteLine("Ctrl+C received, shutting down gracefully. Press Ctrl+C again to force shutdown.");
                e.Cancel = true;
                cts.Cancel();
            }
            else
            {
                Environment.Exit(0);
            }
        };

        await portForwarder.StartAsync(cts.Token);
    }
}
