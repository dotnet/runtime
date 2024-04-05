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
    public static string? Transport;
    public static string? Message;
    private static HubConnection? _hubConnection;

    [JSExport]
    public static void GetQueryParameters(string transport, string message)
    {
        Transport = transport;
        Message = message; // can be removed, should we remove both?
        TestOutput.WriteLine($"Finished GetQueryParameters on CurrentManagedThreadId={Environment.CurrentManagedThreadId}.");
    }

    [JSExport]
    public static async Task Connect(int serverPort)
    {
        // connect using aspnetcore server url
        var builder = new UriBuilder("http", "localhost", serverPort) { Path = "/chathub" };
        string hubUrl = builder.ToString();
        TestOutput.WriteLine($"hubUrl = {hubUrl}");
        HttpTransportType httpTransportType = StringToTransportType(Transport);
        _hubConnection = new HubConnectionBuilder().WithUrl(hubUrl, options =>
            {
                options.Transports = httpTransportType;
            })
            .Build();
        _hubConnection.On<string>("ReceiveMessage", (message) =>
        {
            TestOutput.WriteLine($"Message = [{message}]. ReceiveMessage from server on CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
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
            TestOutput.WriteLine($"SignalRPassMessages was sent by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
        });

    [JSExport]
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
