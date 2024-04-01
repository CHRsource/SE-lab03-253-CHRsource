using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Collections;


string absoluteDataDir = "";
string dataDir = "\\server\\data\\";
string currentDirectory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
absoluteDataDir = currentDirectory + dataDir;

if (!Directory.Exists(absoluteDataDir))
{
    Directory.CreateDirectory(absoluteDataDir);
}
if (!File.Exists("indexes.txt"))
{
    File.Create("indexes.txt"); 
}

IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, 8888);
using Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
server.Bind(ipPoint);
server.Listen();
Console.WriteLine("Server started!");
Hashtable filesTable = new Hashtable();
int voidId = 1;
putHashTable(ref filesTable, ref voidId);
List<Socket> clients = new List<Socket>();
try
{
    while (true) // Цикл для ожидания новых подключений
    {
        try
        {
            Socket client = await server.AcceptAsync();
            clients.Add(client);
            Thread t = new Thread(async () => await ProcessClientAsync(client));
            t.Start();
        }
        catch
        {
            break;
        }
    }
}
finally
{
    getHashTable(filesTable);
    server.Close();
    Console.WriteLine("Server stopped.");
}


async Task ProcessClientAsync(Socket client)
{
    Console.WriteLine($"Client's address: {client.RemoteEndPoint}");
    byte[] b = new byte[512];
    var files = new List<(int, string)>();
    try
    {
        while (true)
        {
        
            client.Receive(b);
            string msg = Encoding.Default.GetString(b);
            var zpr = msg.Split("`");
            if (zpr[0].Contains("exit"))
            {
                client.Close();
                break;
            }
            else if (zpr[0] == "1")
            {
                byte[] resp = new byte[Convert.ToInt32(zpr[3])]; client.Receive(resp);
                PUT(client, zpr[2], filesTable, resp, zpr[4]);
            }
            else if (zpr[0] == "2")
            {
                if (zpr[1] == "1")
                {
                    GET(zpr[2], client);
                }
                else if (zpr[1] == "2")
                {
                    GETbyId(zpr[2], filesTable, client);
                }
            }
            else
            {
                if (zpr[1] == "1")
                {
                    var res = DELETE(zpr[2], ref filesTable);
                    await client.SendAsync(Encoding.UTF8.GetBytes(res), SocketFlags.None);
                }
                else
                {
                    var res = DELETEbyID(zpr[2], ref filesTable);
                    await client.SendAsync(Encoding.UTF8.GetBytes(res), SocketFlags.None);
                }
            }
        }
    }
    catch (Exception)
    {
        Console.WriteLine("Client disconnected");
        clients.Remove(client);
        if (clients.Count == 0)
        {
            getHashTable(filesTable);
            server.Close();
        }
    }
    finally
    {
        client.Close();
    }
}

async void PUT(Socket client, string fileName, Hashtable a, byte[] responseBytes, string FileFormat)
{
    if (fileName != "")
    {
        if (a.ContainsValue(fileName))
        {
            await client.SendAsync(Encoding.UTF8.GetBytes("403"), SocketFlags.None);
        }
        else
        {
            await client.SendAsync(Encoding.UTF8.GetBytes("202`" + voidId.ToString()), SocketFlags.None);
            using (var file = File.Open(absoluteDataDir + fileName, FileMode.CreateNew, FileAccess.Write))
            {
                file.Write(responseBytes);
            }
            a.Add(voidId.ToString(), fileName);
            voidId += 1;
        }
    }
    else
    {

        await client.SendAsync(Encoding.UTF8.GetBytes("202`" + voidId.ToString()), SocketFlags.None);
        await using (var file = File.Open(absoluteDataDir + voidId + FileFormat.Split(".")[1], FileMode.CreateNew, FileAccess.Write))
        {
            file.Write(responseBytes);
        }
        a.Add(voidId.ToString(), voidId + FileFormat.Split(".")[1]);
        voidId += 1;
    }
}

void putHashTable(ref Hashtable a, ref int voidId)
{
    using (StreamReader reader = new StreamReader("indexes.txt"))
    {
        string? line;
        while ((line = reader.ReadLine()) != null && line.Trim() != "")
        {
            a.Add(line.Split(" ")[0], line.Split(" ")[1]);
            if (voidId <= Convert.ToInt16(line.Split(" ")[0]))
            {
                voidId = Convert.ToInt16(line.Split(" ")[0]) + 1;
            }
        }
    }
}


void getHashTable(Hashtable a)
{
    using (StreamWriter writer = new StreamWriter("indexes.txt", false))
    {
        ICollection keys = a.Keys.Cast<string>().OrderBy(c => c).ToArray();
        foreach (string s in keys)
        {
            writer.WriteLine(s + " " + a[s]);
        }
    }
}
async void GET(string fileName, Socket client)
{
    if (!File.Exists(absoluteDataDir + fileName))
    {
        await client.SendAsync(Encoding.UTF8.GetBytes("404"), SocketFlags.None);
    }
    else
    {
        var e = new FileInfo(absoluteDataDir + fileName).Length;
        var temp = File.ReadAllBytes(absoluteDataDir + fileName);
        await client.SendAsync(Encoding.UTF8.GetBytes("200`" + e), SocketFlags.None);
        await client.SendAsync(temp, SocketFlags.None);
    }
}

async void GETbyId(string fileID, Hashtable a, Socket client)
{
    if (!a.ContainsKey(fileID))
    {
        await client.SendAsync(Encoding.UTF8.GetBytes("404"), SocketFlags.None);
    }
    else
    {
        var e = new FileInfo(absoluteDataDir + a[fileID]).Length;
        var temp = File.ReadAllBytes(absoluteDataDir + a[fileID]);
        await client.SendAsync(Encoding.UTF8.GetBytes("200`" + e), SocketFlags.None);
        await client.SendAsync(temp, SocketFlags.None);
    }
}
string DELETE(string fileName, ref Hashtable a)
{
    if (!File.Exists(absoluteDataDir + fileName))
    {
        return "404";
    }
    else
    {
        File.Delete(absoluteDataDir + fileName);
        foreach (var key in a.Keys)
        {
            if (a[key] as string == fileName)
            {
                a.Remove(key);
                break;
            }
        }
        return "200";
    }
}

string DELETEbyID(string fileID, ref Hashtable a)
{
    if (!a.ContainsKey(fileID))
    {
        return "404";
    }
    else
    {
        File.Delete(absoluteDataDir + a[fileID]);
        a.Remove(fileID);
        voidId = Convert.ToInt16(fileID);
        return "200";
    }
}