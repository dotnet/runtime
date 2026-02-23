// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Wasm.Build.Tests.Blazor;

/// <summary>
/// Client for interacting with the request log API exposed by the BlazorWebWasm test server app.
/// </summary>
internal class BlazorWebWasmLogClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public BlazorWebWasmLogClient(string baseUrl)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    /// <summary>
    /// Retrieves logs for requests processed by the server since the last call to ClearRequestLogsAsync.
    /// </summary>
    public async Task<BlazorWebWasmRequestLog[]> GetRequestLogsAsync()
    {
        var response = await _httpClient.GetAsync("request-logs");
        response.EnsureSuccessStatusCode();
        var logs = await response.Content.ReadFromJsonAsync<BlazorWebWasmRequestLog[]>() ?? [];
        return logs;
    }

    public async Task ClearRequestLogsAsync()
    {
        var response = await _httpClient.DeleteAsync("request-logs");
        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
