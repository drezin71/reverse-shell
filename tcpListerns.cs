using System.Net;
using System.Net.Sockets;
using System.Text;

const string EndMarker = "###END###";

TcpListener server = new TcpListener(IPAddress.Loopback, 14000);
server.Start();

Console.WriteLine("Esperando conexão...");

using TcpClient client = server.AcceptTcpClient();
Console.WriteLine("Conectado!\n");

NetworkStream stream = client.GetStream();
byte[] buffer = new byte[65536];

while (true)
{
    Console.Write("shell> ");
    string? command = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(command))
        continue;

    // Envia o comando
    byte[] cmdBytes = Encoding.UTF8.GetBytes(command);
    stream.Write(cmdBytes, 0, cmdBytes.Length);

    // Lê a resposta até achar o marcador de fim
    StringBuilder output = new StringBuilder();
    while (true)
    {
        int bytes = stream.Read(buffer, 0, buffer.Length);

        if (bytes == 0) // shell fechou a conexão
        {
            Console.WriteLine("\nShell desconectou.");
            server.Stop();
            return;
        }

        output.Append(Encoding.UTF8.GetString(buffer, 0, bytes));
        string text = output.ToString();

        int endIdx = text.IndexOf(EndMarker, StringComparison.Ordinal);
        if (endIdx >= 0)
        {
            Console.Write(text[..endIdx]);
            break;
        }
    }
}