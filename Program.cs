using System;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;


class Options
{
    [Option("com-port", Required = true, HelpText = "Input files to be processed.")]
    public string ComPort { get; set; }

    [Option("tcp-port", Required = false, Default = 5001, HelpText = "Input files to be processed.")]
    public int TcpPort { get; set; }

    [Option("tcp-address", Required = false, Default = "0.0.0.0", HelpText = "Input files to be processed.")]
    public string TcpAddress { get; set; }

    [Option("wsl", Required = false, Default = false, HelpText = "Input files to be processed.")]
    public bool Wsl { get; set; }
}

public class PortForwarder
{
    private readonly string comPortName;
    private readonly IPAddress tcpAddress;
    private readonly int tcpPort;

    private List<TcpClient> ActiveConnection = new List<TcpClient>();

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
        TcpClient? tcpClient = null;
        TcpClient? ActivetcpClient = null;
        Task? ActiveTask = null;
        CancellationTokenSource ActiveTaskCancellationToken = new CancellationTokenSource();
        while (!mainCancellationToken.IsCancellationRequested)
        {
            //var cts = CancellationTokenSource.CreateLinkedTokenSource(mainCancellationToken);
            try
            {
                comPort = new SerialPort(comPortName);
                comPort.Open();

                tcpListener = new TcpListener(tcpAddress, tcpPort);
                tcpListener.Start();

                Console.WriteLine($"Ready! forwarding Port:{comPortName} <-> TCP:{tcpAddress.ToString()}:{tcpPort.ToString()}");
                Console.WriteLine("Waiting for connection ... ");
                while (true)
                {
                    tcpClient = await tcpListener.AcceptTcpClientAsync();
                    if (ActiveTask != null && !ActiveTask.IsCompleted)
                    {
                        Console.WriteLine("Connection already active, closing old connection");
                        //Use cancellation token to cancel the task
                        ActiveTaskCancellationToken.Cancel();
                        ActiveTask.Wait();
                        ActiveTaskCancellationToken.Dispose();
                        ActiveTaskCancellationToken = new CancellationTokenSource();
                    }
                    ActivetcpClient = tcpClient;
                    ActiveTaskCancellationToken.Token.Register(() =>
                    {
                        ActivetcpClient.GetStream().Close();
                        ActivetcpClient.Close();
                        if (comPort.IsOpen)
                        { comPort.DiscardInBuffer(); }
                    });
                    ActiveTask = HandleClientAsync(ActivetcpClient, comPort, ActiveTaskCancellationToken.Token);
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
    private async Task HandleClientAsync(TcpClient tcpClient, SerialPort comPort, CancellationToken cts)
    {
        Console.WriteLine($"Active Conn: Intiating...");
        var tcpStream = tcpClient.GetStream();
        while (!cts.IsCancellationRequested)
        {
            try
            {
                while (!comPort.IsOpen)
                {
                    comPort.Open();
                    Console.WriteLine($"Active Conn: Waiting for COM port to open");
                    await Task.Delay(1000);
                }
                Console.WriteLine($"Active Conn: Connected!");
                await Task.WhenAll(ForwardTcpToComAsync(tcpStream, comPort, cts),
                                    ForwardComToTcpAsync(comPort, tcpClient, cts));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Active Conn: Error occurred: {ex.Message}");
            }
            finally
            {
                if (!cts.IsCancellationRequested)
                {
                    Console.WriteLine($"Active Conn: Restarting....");
                }
                else
                {
                    Console.WriteLine($"Active Conn: is Cancelled ({cts.IsCancellationRequested})");
                }
            }
        }
    }

    private async Task ForwardComToTcpAsync(SerialPort comPort, TcpClient tcpClient, CancellationToken cts)
    {
        NetworkStream tcpStream = tcpClient.GetStream();
        byte[] buffer = new byte[4096];
        try
        {
            while (!cts.IsCancellationRequested)
            {
                int bytesRead = await comPort.BaseStream.ReadAsync(buffer, 0, buffer.Length, cts);
                if (bytesRead > 0)
                {
#if EXTRA_DEBUG
                    Console.WriteLine("Com->Tcp");
#endif
                    await tcpStream.WriteAsync(buffer, 0, bytesRead, cts);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"COM->TCP: Failed ({ex.Message})");
        }
        finally
        {
            Console.WriteLine($"COM->TCP: Cancelled={cts.IsCancellationRequested}");
        }
    }

    private async Task ForwardTcpToComAsync(NetworkStream tcpStream, SerialPort comPort, CancellationToken cts)
    {
        byte[] buffer = new byte[4096];
        try
        {
            while (!cts.IsCancellationRequested)
            {
                int bytesRead = await tcpStream.ReadAsync(buffer, 0, buffer.Length, cts).ConfigureAwait(false);
                if (bytesRead > 0)
                {
#if EXTRA_DEBUG
                    Console.WriteLine("Tcp->Com");
#endif
                    await comPort.BaseStream.WriteAsync(buffer, 0, bytesRead, cts);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TCP->COM: Failed ({ex.Message})");
        }
        finally
        {
            Console.WriteLine($"TCP->COM: Cancelled=({cts.IsCancellationRequested})");
        }
    }
}

public class Com2Tcp
{
    /// <summary>
    /// Get the IP address of the WSL interface
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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

    /// <summary>
    /// Parse command line options and Start the port forwarder
    /// </summary>
    /// <param name="opts"></param>
    /// <returns></returns>
    static async Task RunOptions(Options opts)
    {
        String tcpAddress;

        if (opts.Wsl)
        {
            tcpAddress = GetWSLIpAddress();
            Console.WriteLine($"Listening on WSL IP address: {tcpAddress}");
        }
        else
        {
            tcpAddress = opts.TcpAddress;
        }

        var portForwarder = new PortForwarder(opts.ComPort, tcpAddress, opts.TcpPort);
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

    /// <summary>
    /// Handle parse errors
    /// </summary>
    /// <param name="errs"></param>
    static void HandleParseError(IEnumerable<Error> errs)
    {
        //handle errors
    }

    /// <summary>
    /// Main entry point
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static async Task Main(string[] args)
    {
        try
        {
            await CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(RunOptions);
        }
        catch (System.Exception)
        {
            throw;
        }
    }
}