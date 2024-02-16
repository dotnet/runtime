// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace BlazorHosted.Server.Hubs;
public class ChatHub : Hub
{
    public async Task SendMessage(string message, int sendingThreadId)
    {
        TestOutputWriteLine($"Server: receives message={message} sent by treadID = {sendingThreadId} and sends it back.");
        await Clients.All.SendAsync("ReceiveMessage", message).ConfigureAwait(false);
    }

    public void ConfirmClientReceivedMessageAndExitWithSuccess(string message)
    {
        TestOutputWriteLine($"Server receives confirmation with message = {message}");
        Exit(0);
    }

    public void Exit(int code)
    {
        TestOutputWriteLine($"Received exit code {code} from client. Exiting.");
        Environment.Exit(code);
    }

    public static void TestOutputWriteLine(string message)
    {
        Console.WriteLine("TestOutput -> " + message);
    }
}
