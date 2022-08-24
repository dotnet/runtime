// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    const string ServerUrl = "https://localhost:5001";
    const int NumRequests = 5000;

    static void Main(
        string python, 
        string spmiScriptPath, 
        string mchOutputPath, 
        string workspacePath, 
        string coreRunPath, 
        string dotnetCliPath, 
        string tfm, 
        string rid)
    {
        const string config = "Release";
        const string outputDir = "bin";

        Process.Start(
            new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "clone --quiet --depth 1 https://github.com/aspnet/benchmarks aspnet-benchmarks",
                WorkingDirectory = workspacePath
            })!.WaitForExit();

        string benchmarkDir = Path.Combine(workspacePath, "aspnet-benchmarks", "src", "Benchmarks");
        string binDir = Path.Combine(benchmarkDir, outputDir, config, tfm, rid);

        BuildCommand($"publish -r {rid} -c {config} -f {tfm} -o {outputDir} --self-contained", benchmarkDir, dotnetCliPath, tfm);
        RunSpmi(python, spmiScriptPath, mchOutputPath, binDir, coreRunPath);
        Directory.Delete(Path.Combine(benchmarkDir, outputDir), true);
    }

    static void BuildCommand(string cmd, string srcDir, string dotnet, string tfm)
    {
        var psi = new ProcessStartInfo
        {
            FileName = dotnet,
            Arguments = cmd,
            WorkingDirectory = srcDir,
            Environment =
            {
                ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["UseSharedCompilation"] = "false",
                ["BenchmarksTargetFramework"] = tfm
            }
        };

        Console.WriteLine($"Executing command: {psi.FileName} {psi.Arguments}");
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new InvalidOperationException("BuildCommand failed with " + p.ExitCode);
    }

    static void RunSpmi(string pythonExe, string spmiScriptPath, string outputMchPath, string binDir, string coreRun)
    {
        // NOTE: we need to make sure output is properly captured because we're going to wait for a specific message
        // to start the load simulation, hence, --dont_redirect_stdout is passed to SuperPMI
        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"{spmiScriptPath} collect {coreRun} \"Benchmarks.dll scenarios=plaintext,MvcJson,DbFortunesEf\" --dont_redirect_stdout -output_mch_path {outputMchPath}",
            WorkingDirectory = binDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            Environment =
            {
                ["ASPNETCORE_KestrelTransport"] = "Sockets",
                ["ASPNETCORE_nonInteractive"] = "true",
                ["connectionString"] = "Data Source=benchmarks.db;Cache=Shared",
                ["database"] = "Sqlite",
                ["ASPNETCORE_server"] = "Kestrel",
                ["ASPNETCORE_protocol"] = "https",
                ["ASPNETCORE_urls"] = ServerUrl,
                ["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1",
                ["DOTNET_MULTILEVEL_LOOKUP"] = "0",
                ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1",
                ["DOTNET_TieredPGO"] = "1"
            }
        };

        Console.WriteLine($"Executing command in directory {psi.WorkingDirectory}\n: {psi.FileName} {psi.Arguments}");

        using var srvProc = new Process();
        srvProc.StartInfo = psi;
        srvProc.EnableRaisingEvents = true;
        srvProc.OutputDataReceived += async (_, e) =>
        {
            Console.WriteLine(e.Data);

            // When the application reports that it has started, start sending requests.
            if ((e.Data != null) && e.Data.Contains("Application started. Press Ctrl+C to shut down."))
                await SendTestRequests().ConfigureAwait(false);
        };
        srvProc.ErrorDataReceived += (_, e) => Console.WriteLine(e.Data);
        srvProc.Start();
        srvProc.BeginOutputReadLine();
        srvProc.BeginErrorReadLine();
        srvProc.WaitForExit();
    }

    static async Task SendRequestsAsync(HttpClient client, Func<HttpRequestMessage> requestGenerator)
    {
        for (int i = 0; i < NumRequests; i++)
        {
            HttpRequestMessage request = requestGenerator();
            if (i % 1000 == 0 || i == (NumRequests - 1))
                Console.WriteLine($"Sending request number {i + 1} to {request.RequestUri}");

            HttpResponseMessage responseMessage = await client.SendAsync(request).ConfigureAwait(false);
            if (!responseMessage.IsSuccessStatusCode)
                throw new Exception($"Unexpected status code for {request.RequestUri}: {responseMessage.StatusCode}");
        }
    }

    static async Task SendTestRequests()
    {
        Console.WriteLine("Sending requests:");
        Console.WriteLine("-------------------");

        var clientHandler = new HttpClientHandler();
        clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
        using var client = new HttpClient(clientHandler);

        // Plaintext (doesn't allocate)
        string requestUrl = ServerUrl + "/plaintext";
        await SendRequestsAsync(client, () => CreatePlainTextHttpMessage("GET", requestUrl)).ConfigureAwait(false);

        // Json-MVC (allocats)
        requestUrl = ServerUrl + "/mvc/json";
        await SendRequestsAsync(client, () => CreateJsonHttpMessage("GET", requestUrl)).ConfigureAwait(false);

        // FortunesEF (db)
        requestUrl = ServerUrl + "/fortunes/ef";
        await SendRequestsAsync(client, () => CreatePlainTextHttpMessage("GET", requestUrl)).ConfigureAwait(false);

        Console.WriteLine("Stopping the server:");
        Console.WriteLine("----------------------");
        string shutdownUrl = ServerUrl + "/shutdown";
        Console.WriteLine($"Sending request to {shutdownUrl}");
        HttpRequestMessage shutdownRequest = CreatePlainTextHttpMessage("GET", shutdownUrl);
        HttpResponseMessage shutdownResponseMessage = await client.SendAsync(shutdownRequest).ConfigureAwait(false);
        if (!shutdownResponseMessage.IsSuccessStatusCode)
            throw new Exception($"Unexpected status code for {shutdownRequest.RequestUri}: {shutdownResponseMessage.StatusCode}");
    }

    static HttpRequestMessage CreatePlainTextHttpMessage(string method, string url)
    {
        var msg = new HttpRequestMessage(new HttpMethod(method), url);
        msg.Headers.Add("Accept", "text/plain,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7");
        msg.Headers.Add("Connection", "keep-alive");
        return msg;
    }

    static HttpRequestMessage CreateJsonHttpMessage(string method, string url)
    {
        var msg = new HttpRequestMessage(new HttpMethod(method), url);
        msg.Headers.Add("Accept", "application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7");
        msg.Headers.Add("Connection", "keep-alive");
        return msg;
    }
}
