using System;
using System.Net.Sockets;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;

if (!Directory.Exists("client\\data\\"))
{
    Directory.CreateDirectory("client\\data\\");
}
var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
try
{
    await client.ConnectAsync("127.0.0.1", 8888);
    var responseBytes = new byte[8192];
    int bytes = 0;
    Console.WriteLine($"Подключение к {client.RemoteEndPoint} установлено");
    string response = "";
    string temp2 = "";
    string temp;
    while (true)
    {
        temp = getOperation();
        if (temp == "exit")
        {
            client.Close();
            break;
        }
        else if (temp == "2")
        {
            Console.WriteLine("Do you want to get the file by name or by id (1 - name, 2 - id): >");
            temp2 = Console.ReadLine();
            if (temp2 == "1")
            {
                Console.WriteLine("Enter name: >");
                temp2 = Console.ReadLine();
                if (temp2.Contains("txt"))
                {
                    client.Send(Encoding.UTF8.GetBytes("2`1`" + temp2 + "`"));
                }
                else
                {
                    client.Send(Encoding.UTF8.GetBytes("2`3`" + temp2 + "`"));
                }
            }
            else
            {
                Console.WriteLine("Enter ID: >");
                string temp3 = Console.ReadLine();
                if (temp2.Contains("1"))
                {
                    client.Send(Encoding.UTF8.GetBytes("2`2`" + temp3 + "`"));
                }
                else
                {
                    client.Send(Encoding.UTF8.GetBytes("2`4`" + temp3 + "`"));
                }
            }
            bytes = await client.ReceiveAsync(responseBytes, SocketFlags.None);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            var items = response.Split("`");
            if (items[0] == "200")
            {
                if (temp2.Contains("txt"))
                {
                    using (StreamWriter writer = new StreamWriter("client\\data\\" + "temp.txt", false))
                    {
                        writer.Write(items[1]);
                        writer.Close();
                    }
                    Console.WriteLine("The File was downloaded! Specify a name for it: >");
                    string tempQ = Console.ReadLine();
                    while (true)
                    {
                        if (tempQ == "temp.txt")
                        {
                            Console.WriteLine("Wrong name!");
                            tempQ = Console.ReadLine();
                        }
                        else
                        {
                            break;
                        }
                    }
                    File.Move("client\\data\\" + "temp.txt", "client\\data\\" + tempQ);
                    File.Create("client\\data\\" + "temp.txt");
                }
                else
                {
                    byte[] imageBytes = new byte[Convert.ToInt32(items[1])];
                    bytes = await client.ReceiveAsync(imageBytes, SocketFlags.None);
                    Console.WriteLine("The File was downloaded! Specify a name for it: >");
                    string tempQ = Console.ReadLine();
                    using (var ms12 = new MemoryStream(imageBytes))
                    {
                        System.Drawing.Image img = System.Drawing.Image.FromStream(ms12);
                        img.Save("client\\data\\" + tempQ, img.RawFormat);
                        ms12.Close();
                    }
                }
            }
            else
            {
                Console.WriteLine("The response says that this file is not found!");
            }
        }
        else if (temp == "3")
        {
            Console.WriteLine("Do you want to delete the file by name or by id (1 - name, 2 - id): >");
            temp2 = Console.ReadLine();
            if (temp2 == "1")
            {
                Console.WriteLine("Enter name: >");
                temp2 = Console.ReadLine();
                Console.WriteLine("The request was sent.");
                client.Send(Encoding.UTF8.GetBytes("3`1`" + temp2));

            }
            else
            {
                Console.WriteLine("Enter ID: >");
                temp2 = Console.ReadLine();
                Console.WriteLine("The request was sent.");
                client.Send(Encoding.UTF8.GetBytes("3`2`" + temp2 + "`"));
            }
            bytes = await client.ReceiveAsync(responseBytes, SocketFlags.None);
            response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
            var items = response.Split("`");
            if (items[0] == "404")
            {
                Console.WriteLine("The response says that this file is not found!");
            }
            else
            {
                Console.WriteLine("The response says that this file was deleted successfully!");
            }
        }
        else
        {
            Console.WriteLine("Enter name of the file: >");
            temp2 = Console.ReadLine();
            Console.WriteLine("Enter name of the file to be saved on server: >");
            string temp3 = Console.ReadLine();
            Console.WriteLine("The request was sent.");
            saveFile(temp2, temp3, client, responseBytes, bytes, response);
        }
        break;
    }
}
catch
{
    Console.WriteLine($"Не удалось установить подключение");
}
finally
{
    client.Close();
}


string getOperation()
{
    string temp;
    Console.WriteLine("Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
    temp = Console.ReadLine();
    while (true)
    {
        if (temp == "1" || temp == "2" || temp == "3" || temp == "exit")
        {
            break;
        }
        else
        {
            Console.WriteLine("Введите корректное значение! \n Enter action (1-put in a file,2 - get a file,3 delete a file,exit-for exit):>");
            temp = Console.ReadLine();
        }
    }
    return temp;
}


async void saveFile(string fileName, string serverFile, Socket client, byte[] responseBytes, int bytes, string response)
{
    if (fileName.Contains("txt"))
    {
        var e = new FileInfo("client\\data\\" + fileName).Length;
        var msg = "1`1`" + serverFile + "`" + e + "`";
        var msg1 = File.ReadAllText("client\\data\\" + fileName);
        await client.SendAsync(Encoding.UTF8.GetBytes(msg), SocketFlags.None);
        await client.SendAsync(Encoding.UTF8.GetBytes(msg1), SocketFlags.None);
    }
    else
    {
        long e;
        byte[] ms1;
        using (var ms = new MemoryStream())
        {
            System.Drawing.Image image = System.Drawing.Image.FromFile("client\\data\\" + fileName);
            image.Save(ms, image.RawFormat);
            e = ms.Length;
            ms1 = ms.ToArray();
            ms.Close();
        }
        var msg = "1`2`" + serverFile + "`" + e + "`";
        await client.SendAsync(Encoding.UTF8.GetBytes(msg), SocketFlags.None);
        await client.SendAsync(ms1, SocketFlags.None);
    }
    bytes = client.Receive(responseBytes);
    response = Encoding.UTF8.GetString(responseBytes, 0, bytes);
    var items = response.Split("`");
    if (items[0].Contains("202"))
    {
        Console.WriteLine("Response says that file is saved! ID =" + items[1]);
    }
}