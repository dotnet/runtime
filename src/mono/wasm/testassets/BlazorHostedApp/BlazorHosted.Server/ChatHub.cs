// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace BlazorHosted.Server.Hubs;
public class ChatHub : Hub
{
    public async Task SendMessage(string message, int sendingThreadId)
    {
        TestOutputWriteLine($"Server: receives Message=[{message}] sent by treadID = {sendingThreadId} and sends it back.");
        string changedMessage = $"{message}-pong";
        await Clients.All.SendAsync("ReceiveMessage", changedMessage).ConfigureAwait(false);
    }

    public void Exit(int code)
    {
        TestOutputWriteLine($"Received exit code {code} from client.");
        Environment.Exit(code);
    }

    public static void TestOutputWriteLine(string message)
    {
        Console.WriteLine("TestOutput -> " + message);
    }
}
