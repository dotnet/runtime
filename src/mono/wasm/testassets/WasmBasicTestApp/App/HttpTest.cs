// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

public partial class HttpTest
{
    private static bool FeatureEnableStreamingResponse { get; } = AppContext.TryGetSwitch("System.Net.Http.WasmEnableStreamingResponse", out bool value) ? value : true;

    [JSExport]
    public static async Task GoodUpload()
    {
        var uri = GetOriginUrl() + "/upload/good.log";
        using var client = new HttpClient();
        using var response = await client.PostAsync(uri, new StringContent("This is a test log file."));
        Console.WriteLine($"TestOutput -> GoodUpload to returned status code {response.StatusCode}");
    }

    [JSExport]
    public static async Task BadUpload()
    {
        var uri = GetOriginUrl() + "/upload/evil.exe";
        using var client = new HttpClient();
        using var response = await client.PostAsync(uri, new StringContent("This is a dummy exe file."));
        Console.WriteLine($"TestOutput -> BadUpload to returned status code {response.StatusCode}");
    }

    [JSExport]
    public static async Task<int> HttpNoStreamingTest()
    {
        Console.WriteLine($"TestOutput -> AppContext FeatureEnableStreamingResponse={FeatureEnableStreamingResponse}");
        if(FeatureEnableStreamingResponse)
        {
            Console.WriteLine("FeatureEnableStreamingResponse is true, this test is not valid.");
            return -1;
        }

        var uri = GetOriginUrl() + "/main.js";
        using var client = new HttpClient();
        using var response = await client.GetAsync(uri);

        var contentType = response.Content.GetType();
        Console.WriteLine("TestOutput -> response.Content is " + contentType.FullName);
        if (contentType == typeof(StreamContent))
        {
            Console.WriteLine($"response.Content is {contentType.FullName}");
            return -2;
        }

        return 42;
    }

    public static string GetOriginUrl()
    {
        using var globalThis = JSHost.GlobalThis;
        using var document = globalThis.GetPropertyAsJSObject("document");
        using var location = globalThis.GetPropertyAsJSObject("location");
        return location.GetPropertyAsString("origin");
    }
}
