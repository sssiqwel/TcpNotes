using System.Net.Sockets;
using System.Text;

const string host = "127.0.0.1";
const int port = 5000;

try
{
    using var client = new TcpClient();
    await client.ConnectAsync(host, port);
    Console.WriteLine($"Подключено к серверу {host}:{port}");
    Console.WriteLine("Команды: ADD <текст>, LIST, EXIT");

    using NetworkStream stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
    using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
    {
        AutoFlush = true
    };

    while (true)
    {
        Console.Write("> ");
        string? command = Console.ReadLine();

        if (command is null || command.Equals("EXIT", StringComparison.OrdinalIgnoreCase))
            break;

        if (string.IsNullOrWhiteSpace(command))
            continue;

        await writer.WriteLineAsync(command);
        string? response = await reader.ReadLineAsync();

        if (response is null)
        {
            Console.WriteLine("Сервер закрыл соединение.");
            break;
        }

        Console.WriteLine(response);
    }
}
catch (SocketException)
{
    Console.WriteLine("Не удалось подключиться к серверу. Проверьте, что он запущен.");
}
catch (IOException)
{
    Console.WriteLine("Соединение с сервером прервано.");
}
catch (Exception exception)
{
    Console.WriteLine($"Непредвиденная ошибка: {exception.Message}");
}
