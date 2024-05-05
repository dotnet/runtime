// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Web;

namespace Shared;

public class SignalRTest
{
    private TaskCompletionSource<int>? tcs;
    private HubConnection? _hubConnection;
    private string transport = string.Empty;
    private string message = string.Empty;
    private string wrongQueryError = "Query string with parameters 'message' and 'transport' is required";

    public async Task<int> Run(string origin, string fullUrl)
    {
        tcs = new TaskCompletionSource<int>();
        GetQueryParameters(fullUrl);
        await Connect(origin);
        await SignalRPassMessages();

        int delayInMin = 2;
        await Task.WhenAny(
            tcs!.Task,
            Task.Delay(TimeSpan.FromMinutes(delayInMin)));

        if (!tcs!.Task.IsCompleted)
            throw new TimeoutException($"Test timed out after waiting {delayInMin} minutes for process to exit.");
        return tcs.Task.Result;

    }

    private void SetResult(int value) => tcs?.SetResult(value);

    private void GetQueryParameters(string url)
    {
        var uri = new Uri(url);
        if (string.IsNullOrEmpty(uri.Query))
        {
            throw new Exception(wrongQueryError);
        }
        var parameters = HttpUtility.ParseQueryString(uri.Query);
        if (parameters == null)
        {
            throw new Exception(wrongQueryError);
        }
        transport = QueryParser.GetValue(parameters, "transport");
        message = $"{transport} {QueryParser.GetValue(parameters, "message")}" ;
        TestOutput.WriteLine($"Finished GetQueryParameters on CurrentManagedThreadId={Environment.CurrentManagedThreadId}.");
    }

    private async Task Connect(string baseUri)
    {
        string hubUrl = new Uri(new Uri(baseUri), "chathub").ToString();
        Console.WriteLine($"hubUrl: {hubUrl}");
        HttpTransportType httpTransportType = StringToTransportType(transport);
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
                {
                    options.Transports = httpTransportType;
                })
            .Build();

        _hubConnection.On<string>("ReceiveMessage", async (message) =>
        {
            TestOutput.WriteLine($"Message = [{message}]. ReceiveMessage from server on CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
            await DisposeHubConnection();
            SetResult(0);
        });
        TestOutput.WriteLine($"Subscribed to ReceiveMessage by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");

        await _hubConnection.StartAsync();
        TestOutput.WriteLine($"SignalR connected by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
    }

    private static HttpTransportType StringToTransportType(string transport)
    {
        switch (transport.ToLowerInvariant())
        {
            case "longpolling":
                return HttpTransportType.LongPolling;
            case "websockets":
                return HttpTransportType.WebSockets;
            default:
                throw new Exception($"{transport} is invalid transport type");
        }
    }

    private async Task SignalRPassMessages() =>
        await Task.Run(async () =>
            {
                if (_hubConnection == null)
                    throw new Exception("Cannot send messages before establishing hub connection");
                await _hubConnection!.SendAsync("SendMessage", message, Environment.CurrentManagedThreadId);
                TestOutput.WriteLine($"SignalRPassMessages was sent by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
            });

    private async Task DisposeHubConnection()
    {
        if (_hubConnection != null)
        {
            _hubConnection.Remove("ReceiveMessage");
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
        TestOutput.WriteLine($"SignalR disconnected by CurrentManagedThreadId={Environment.CurrentManagedThreadId}");
    }
}
