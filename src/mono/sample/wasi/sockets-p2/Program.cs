// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;
using System.Text;

public static class WasiMainWrapper
{
    public static async Task<int> MainAsync(string[] args)
    {
        IPHostEntry ipHostInfo = await Dns.GetHostEntryAsync("example.com");
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        Console.WriteLine($"IP Address: {ipAddress}");

        IPEndPoint ipEndPoint = new(ipAddress, 80);
        using Socket client = new(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

        await client.ConnectAsync(ipEndPoint);

        // Send message.
        var message = @"GET / HTTP/1.1
Host: example.com
Accept: */*

";
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var start = 0;
        while (start < messageBytes.Length)
        {
            start += await client.SendAsync(messageBytes.AsMemory(start), SocketFlags.None);
        }

        // Receive ack.
        var buffer = new byte[2048];
        var received = await client.ReceiveAsync(buffer, SocketFlags.None);
        var response = Encoding.UTF8.GetString(buffer, 0, received);
        Console.WriteLine(response);

        client.Shutdown(SocketShutdown.Both);

        return 0;
    }

    public static int Main(string[] args)
    {
        return PollWasiEventLoopUntilResolved((Thread)null!, MainAsync(args));

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "PollWasiEventLoopUntilResolved")]
        static extern T PollWasiEventLoopUntilResolved<T>(Thread t, Task<T> mainTask);
    }

}
