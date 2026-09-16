using System.Net;
using System.Net.Sockets;
using System.Text;

const int port = 5000;
var notes = new List<string>();
var notesLock = new object();

var listener = new TcpListener(IPAddress.Loopback, port);
listener.Start();
Console.WriteLine($"Сервер заметок запущен: 127.0.0.1:{port}");
Console.WriteLine("Для остановки нажмите Ctrl+C.");

try
{
    while (true)
    {
        TcpClient client = await listener.AcceptTcpClientAsync();
        Console.WriteLine($"Подключился клиент: {client.Client.RemoteEndPoint}");

        // Не ждём завершения клиента: следующий клиент может подключаться сразу.
        _ = Task.Run(() => HandleClientAsync(client));
    }
}
finally
{
    listener.Stop();
}

async Task HandleClientAsync(TcpClient client)
{
    EndPoint? remoteEndPoint = client.Client.RemoteEndPoint;

    try
    {
        using (client)
        {
            using NetworkStream stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true
            };

            while (await reader.ReadLineAsync() is { } command)
            {
                string response = ProcessCommand(command);
                await writer.WriteLineAsync(response);
            }
        }
    }
    catch (IOException)
    {
        // Клиент мог закрыть программу или сеть могла оборваться.
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Ошибка при работе с клиентом: {exception.Message}");
    }
    finally
    {
        Console.WriteLine($"Клиент отключился: {remoteEndPoint}");
    }
}

string ProcessCommand(string command)
{
    if (command.StartsWith("ADD ", StringComparison.OrdinalIgnoreCase))
    {
        string text = command[4..].Trim();
        if (string.IsNullOrWhiteSpace(text))
            return "ERROR Текст заметки не может быть пустым.";

        lock (notesLock)
        {
            notes.Add(text);
            return $"OK Заметка добавлена. Всего заметок: {notes.Count}.";
        }
    }

    if (command.Equals("LIST", StringComparison.OrdinalIgnoreCase))
    {
        lock (notesLock)
        {
            if (notes.Count == 0)
                return "NOTES (список пуст)";

            // Ответ всегда занимает одну строку: это удобно для простого протокола.
            return "NOTES " + string.Join(" | ", notes.Select((note, index) => $"{index + 1}. {note}"));
        }
    }

    return "ERROR Неизвестная команда. Используйте: ADD <текст> или LIST.";
}
