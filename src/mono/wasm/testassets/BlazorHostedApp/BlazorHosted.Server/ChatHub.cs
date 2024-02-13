// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR;

namespace BlazorHosted.Server.Hubs;
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public async Task SendMessage(string transport, string message)
    {
        _logger.LogInformation($"[{transport}] Server receives message={message}");
        string changedMessage = $"{message} from server";
        await Clients.All.SendAsync("ReceiveMessage", transport, changedMessage).ConfigureAwait(false);
    }

    public void ConfirmClientReceivedMessageAndExit(string transport, string message)
    {
        _logger.LogInformation($"[{transport}] {message}");
        Environment.Exit(0);
    }
}
