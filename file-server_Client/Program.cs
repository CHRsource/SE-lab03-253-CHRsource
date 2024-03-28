using System.IO;
using System.Net.Sockets;
using System.Text;
using System;
using System.Threading.Tasks;

class Client
{
    public static async Task Main(string[] args)
    {
        var port = 12345;

        try
        {
            Console.InputEncoding = System.Text.Encoding.Unicode;

            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync("127.0.0.1", port);
            Console.WriteLine($"Connection to server {socket.RemoteEndPoint} established.");

            while (true)
            {
                Console.Write("Enter action (1 - get a file, 2 - create a file, 3 - delete a file): > ");

                string action = Console.ReadLine();

                //отправка запроса на сервер
                if (action.Trim().ToLower() == "exit")
                {
                    byte[] requestBytesWithData = Encoding.UTF8.GetBytes("exit");
                    await socket.SendAsync(requestBytesWithData, SocketFlags.None);
                    Console.WriteLine("The request was sent.");
                }
                else if (action == "2")
                {
                    //ввод имени файла и его содержимого
                    Console.Write("Enter filename: > ");
                    string filename = Console.ReadLine();

                    Console.Write("Enter file content (to complete your entry, press Enter on an empty line):\n> ");
                    string fileContent = Console.ReadLine(); // Переменная для хранения содержимого файла
                    string line;
                    while (true) // Проверяем, что введена не пустая строка
                    {
                        Console.Write("> ");
                        if (string.IsNullOrWhiteSpace(line = Console.ReadLine())) break;
                        fileContent += "\\n" + line; // Добавляем строку к содержимому файла с разделителем новой строки
                    }
                    //отправка имени и содержимого файла на сервер
                    string request = $"PUT {filename} {fileContent}";
                    byte[] requestBytesWithData = Encoding.UTF8.GetBytes(request);
                    await socket.SendAsync(requestBytesWithData, SocketFlags.None);
                    Console.WriteLine("The request was sent.");
                }
                else if (action == "1" || action == "3")
                {
                    //ввод имени файла
                    Console.Write("Enter filename: > ");
                    string filename = Console.ReadLine();

                    //отправка имени файла на сервер
                    string request = $"{(action == "1" ? "GET" : "DELETE")} {filename}";
                    byte[] filenameBytes = Encoding.UTF8.GetBytes(request);
                    await socket.SendAsync(filenameBytes, SocketFlags.None);
                    Console.WriteLine("The request was sent.");
                }

                if (action == "1" || action == "2" || action == "3")
                {
                    // Получение ответа от сервера
                    byte[] responseBytes = new byte[1024];
                    int bytesRead = await socket.ReceiveAsync(responseBytes, SocketFlags.None);
                    string response = Encoding.UTF8.GetString(responseBytes, 0, bytesRead);
                    switch (action)
                    {
                        case "1":
                            {
                                if (response == "404")
                                    Console.WriteLine("The response says that the file was not found!");
                                else
                                    Console.WriteLine($"The content of the file is:\n{response.Substring(4)}");
                                break;
                            };
                        case "2":
                            {
                                if (response == "403")
                                    Console.WriteLine("The response says that creating the file was forbidden!");
                                else if (response == "200")
                                    Console.WriteLine("The response says that the file was created!");
                                break;
                            }
                        case "3":
                            {
                                if (response == "404")
                                    Console.WriteLine("The response says that the file was not found!");
                                else if (response == "200")
                                    Console.WriteLine("The response says that the file was successfully deleted!");
                                break;
                            }
                    }
                }
            }
            
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Could not establish a connection to the server.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
        finally
        {
            // Закрываем соединение с сервером и завершаем работу
            Environment.Exit(0);
        }
    }
}
