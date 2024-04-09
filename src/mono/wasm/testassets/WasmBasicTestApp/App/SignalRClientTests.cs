// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;

public partial class SignalRClientTests
{
    private static HubConnection? _hubConnection;

    [JSExport]
    public static async Task Connect(string baseUrl, string transport)
    {
        string hubUrl = Path.Combine(baseUrl, "chathub");
        HttpTransportType httpTransportType = StringToTransportType(transport);
        _hubConnection = new HubConnectionBuilder().WithUrl(hubUrl, options =>
            {
                options.Transports = httpTransportType;
            })
            .Build();
        _hubConnection.On<string>("ReceiveMessage", async (message) =>
        {
            TestOutput.WriteLine($"Message = [{message}]. ReceiveMessage from server on CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
            await DisposeHubConnection();
            ExitWithSuccess();
        });
        await _hubConnection.StartAsync();
        TestOutput.WriteLine($"SignalR connected by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
    }

    [JSExport]
    private static async Task SignalRPassMessages(string message) =>
        await Task.Run(async () =>
        {
            if (_hubConnection == null)
            {
                TestOutput.WriteLine("SignalR connection is not established.");
                return;
            }
            await _hubConnection.SendAsync("SendMessage", message, Environment.CurrentManagedThreadId);
            TestOutput.WriteLine($"SignalRPassMessages was sent by CurrentManagedThreadId={Environment.CurrentManagedThreadId}, message={message}");
        });

    private static async Task DisposeHubConnection()
    {
        if (_hubConnection != null)
        {
            _hubConnection.Remove("ReceiveMessage");
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        TestOutput.WriteLine($"SignalR disconnected by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
    }

    [JSImport("exitWithSuccess", "main.js")]
    public static partial void ExitWithSuccess();

    public static HttpTransportType StringToTransportType(string? transport)
    {
        switch (transport?.ToLowerInvariant())
        {
            case "longpolling":
                return HttpTransportType.LongPolling;
            case "websockets":
                return HttpTransportType.WebSockets;
            default:
                throw new Exception($"{transport} is invalid transport type");
        }
    }
}
