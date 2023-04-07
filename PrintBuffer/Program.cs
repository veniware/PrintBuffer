using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

internal class Program {

    static readonly string CONFIG_FILE = "printbuffer.cfg";

    static void Main(string[] args) {

        if (!File.Exists(CONFIG_FILE)) {
            Console.WriteLine("Configuration file not found");
            Thread.Sleep(3000);
            return;
        }

        StreamReader fileReader = new StreamReader(CONFIG_FILE);
        while (!fileReader.EndOfStream) {
            string line = fileReader.ReadLine();
            if (line.StartsWith("#")) continue;
            line = line.Trim();

            string[] split = line.Split(' ');
            
            if (split.Length == 2) {
                int port = int.Parse(split[0]);
                string target = split[1];

                new Thread(() => {
                    Listen(port, new IPEndPoint(IPAddress.Parse(target), 9100));
                }).Start();
            }
        }

        fileReader.Close();

        while (true) {
            Console.ReadLine();
        }

        //Listen(9100, new IPEndPoint(IPAddress.Parse("192.168.115.58"), 9100));
    }

    static void Listen(int port, IPEndPoint endPoint) {
        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();

        Console.WriteLine($"Listening on port {port}");

        while (true) {
            TcpClient client = server.AcceptTcpClient();
            new Thread(() => {
                Serve(client, endPoint);
            }).Start();
        }
    }

    static void Serve(TcpClient client, IPEndPoint endPoint) {
        Console.WriteLine($"New client: {client.Client.RemoteEndPoint}");

        NetworkStream downStream = client.GetStream();

        byte[] bytes = new byte[1024];
        List<byte[]> list = new List<byte[]>();
        int length;

        while ((length = downStream.Read(bytes, 0, bytes.Length)) != 0) {
            byte[] crop = new byte[length];
            Array.Copy(bytes, crop, length);
            list.Add(crop);
        }

        client.Close();

        Repeat(list, endPoint);
    }

    static void Repeat(List<byte[]> list, IPEndPoint endPoint) {
        int total = 0;
        for (int i = 0; i < list.Count; i++) {
            total += list[i].Length;
        }
        
        TcpClient repeater = new TcpClient();
        repeater.Connect(endPoint);

        Stream upStream = repeater.GetStream();

        Console.WriteLine($"Sending {total} bytes to {endPoint}");

        for (int i=0; i< list.Count; i++) {
            upStream.Write(list[i], 0, list[i].Length);
        }

        repeater.Close();

        Console.WriteLine("Done!");
    }

}
