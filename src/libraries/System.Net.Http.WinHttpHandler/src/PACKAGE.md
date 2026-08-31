## About

This package provides an [`HttpMessageHandler`](https://learn.microsoft.com/dotnet/api/system.net.http.httpmessagehandler) implementation backed by [Windows HTTP Services (WinHTTP)](https://learn.microsoft.com/windows/win32/winhttp/winhttp-start-page).
While the use of the default `HttpClientHandler` is highly recommended for applications targeting modern .NET, `WinHttpHandler` might help migration scenarios by providing an alternative HTTP backend for Windows that works consistently accross .NET Framework and modern .NET.

## Key Features

* Enables sending *asynchronous* HTTP requests with `HttpClient` on Windows.
* Handles authentication and credentials.
* Exposes a subset of WinHTTP options as C# properties on `WinHttpHandler`.
* Use custom proxy.
* Handle cookies.

## How to Use

```C#
using System.Net;

using WinHttpHandler handler = new()
{
    ServerCredentials = new NetworkCredential("usr", "pwd")
};

using HttpClient client = new(handler);
using HttpRequestMessage request = new(HttpMethod.Get, "https://httpbin.org/basic-auth/usr/pwd");
using HttpResponseMessage response = await client.SendAsync(request);

Console.WriteLine($"Status: {response.StatusCode}");
if (response.IsSuccessStatusCode)
{
    string content = await response.Content.ReadAsStringAsync();
    Console.WriteLine(content);
}
```

## Main Types

The main types provided by this library are:

* `System.Net.Http.WinHttpHandler`

## Additional Documentation

* [API documentation](https://learn.microsoft.com/dotnet/api/system.net.http.winhttphandler)

## Feedback & Contributing

System.Net.Http.WinHttpHandler is released as open source under the [MIT license](https://licenses.nuget.org/MIT). Bug reports and contributions are welcome at [the GitHub repository](https://github.com/dotnet/runtime).
