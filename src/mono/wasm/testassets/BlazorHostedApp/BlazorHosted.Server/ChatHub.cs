// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace BlazorHosted.Server.Hubs;
public class ChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        TestOutputWriteLine($"Server receives message={message}");
        await Clients.All.SendAsync("ReceiveMessage", message).ConfigureAwait(false);
    }

    public async Task ConfirmClientReceivedMessageAndExitWithSuccess(string message)
    {
        TestOutputWriteLine($"Server receives confirmation with message = {message}");
        await Exit(0);
    }

    public async Task Exit(int code)
    {
        int delay = 3;
        TestOutputWriteLine($"Received exit code {code} from client. Exiting with {delay} sec delay to see if there are not errors/logs.");
        await Task.Delay(delay * 1000);
        Environment.Exit(code);
    }

    public static void TestOutputWriteLine(string message)
    {
        Console.WriteLine("TestOutput -> " + message);
    }
}
