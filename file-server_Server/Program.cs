using System.Net;
using System.Net.Sockets;
using System.Text;

class Server
{
    // Создаем список для хранения активных клиентских сокетов
    static List<Socket> clientSockets = new List<Socket>();
    static string absoluteDataDir = "";
    static bool flag = true;

    private static async Task HandleClient(Socket clientSocket)
    {

        // Добавляем клиентский сокет в список активных сокетов
        clientSockets.Add(clientSocket);

        Console.WriteLine("Client connected!");

        byte[] buffer = new byte[1024];

        while (true)
        {
            int bytesRead = clientSocket.Receive(buffer);
            string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Request received: {request}");

            string[] requestParts = request.Split();
            string action = requestParts[0];

            // Проверяем, если клиент отправил команду "exit", то завершаем работу сервера
            if (action == "exit")
            {
                // Закрываем все активные клиентские сокеты
                foreach (var client in clientSockets)
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                clientSockets.Clear();
                flag = false;
                Console.WriteLine("Server shutdown.");
                Environment.Exit(0); // Завершение работы приложения
                return; // Завершаем работу сервера
            }

            string filename = requestParts[1];

            if (action == "GET")
            {
                try
                {
                    string filePath = Path.Combine(absoluteDataDir, filename);
                    string fileContent = File.ReadAllText(filePath);

                    byte[] responseBytes = Encoding.UTF8.GetBytes($"200 {fileContent}");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine("File content sent.");
                }
                catch (FileNotFoundException)
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes("404");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine("File was not found.");
                }
            }
            else if (action == "PUT")
            {
                string fileData = string.Join(" ", requestParts.Skip(2)).Replace("\\n", "\n");
                Console.WriteLine(fileData);
                try
                {
                    string filePath = Path.Combine(absoluteDataDir, filename);
                    if (!File.Exists(filePath))
                        File.WriteAllText(filePath, fileData);
                    else throw new IOException("File already exists.");
                    byte[] responseBytes = Encoding.UTF8.GetBytes("200");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine("File created.");
                }
                catch (IOException e)
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes("403");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine($"{e.Message} Failed to create a file.");
                }
            }
            else if (action == "DELETE")
            {
                try
                {
                    string filePath = Path.Combine(absoluteDataDir, filename);
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                    else throw new FileNotFoundException();

                    byte[] responseBytes = Encoding.UTF8.GetBytes("200");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine("File deleted.");
                }
                catch (FileNotFoundException)
                {
                    byte[] responseBytes = Encoding.UTF8.GetBytes("404");
                    clientSocket.Send(responseBytes);
                    Console.WriteLine("File was not found.");
                }
            }
        }
    }
    public static async Task Main(string[] args)
    {
        int port = 12345;
        string dataDir = "\\server\\data\\";
        string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        absoluteDataDir = currentDirectory + dataDir;
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //привязка сокета к локальному IP адресу и порту
        serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
        //установка максимальной длины очереди ожидающих подключений
        serverSocket.Listen(10);
        Console.WriteLine("Server started!");


        while (flag)
        {
            Socket client = await serverSocket.AcceptAsync();
            _ = Task.Run(() => HandleClient(client));
        }

    }

}