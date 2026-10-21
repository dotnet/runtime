// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using Lxr.Harness.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lxr.Harness.Worker.AspNet;

/// <summary>
/// The flagship scenario: a real Kestrel server under open-loop request load, with latency measured
/// end to end by the client from the request's <em>intended</em> arrival time.
///
/// <para>P0.2 established this as the acceptance signal. The paper's only comparative pause table
/// (Table 1, arXiv:2210.17175, PDF page 2) shows LXR's GC pauses longer than G1's at every percentile
/// while its query latency is far better, under the caption "Short GC pauses do not assure low
/// latency". A collector is therefore accepted or rejected on what the application observes, which is
/// what this scenario measures and no other scenario in the matrix measures as directly.</para>
///
/// <para>The server is deliberately allocation-shaped rather than trivial: each request builds a
/// short-lived object graph, promotes a fraction of it into a bounded session cache with realistic
/// survival, and serialises a response. A handler that allocated nothing would measure Kestrel, not
/// the collector.</para>
/// </summary>
public sealed class AspNetRequestLoadScenario : IScenario
{
    private WebApplication? _app;
    private HttpClient? _client;
    private HttpMessageInvoker? _invoker;
    private string _url = string.Empty;
    private long _requests;
    private long _failures;
    private long _bytes;
    private int _sessionSlots;
    private SessionEntry?[] _sessions = [];
    private int _payloadSize;

    private sealed class SessionEntry
    {
        public required byte[] Payload { get; init; }

        public required string Key { get; init; }

        public long Hits;
    }

    public ScenarioDescriptor Describe() => new()
    {
        Id = "aspnet-request-load",
        Rationale =
            "The flagship scenario. P0.2 established that application-observed latency is the acceptance " +
            "signal and pause distribution only a characterization: arXiv:2210.17175 Table 1 (PDF page 2) " +
            "reports LXR's GC pauses longer than G1's at every percentile while its query latency is far " +
            "better, captioned \"Short GC pauses do not assure low latency\". This scenario is the one that " +
            "measures the deciding number, under a real server rather than a simulation of one.",
        Primary = PrimaryMetric.Latency,
        RequiredCapabilities = HostCapabilities.AspNetCoreSharedFramework,
        DefaultTimeoutSeconds = 420,
        DefaultWorkerCount = 16,
        MaxWorkerCount = 256,
        DefaultArrivalRatePerSecond = 2000,
        MinimumOperations = 100,
        ProvisionalBaselineHeapBytes = 512L * 1024 * 1024,
        Axes = ["arrival rate", "session cache size", "payload size", "heap factor"],
    };

    public void Setup(ScenarioContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _sessionSlots = context.Parameters.GetInt32("sessionSlots", 4096);
        _payloadSize = context.Parameters.GetInt32("payloadBytes", 512);
        int port = context.Parameters.GetInt32("port", 0);
        _sessions = new SessionEntry?[_sessionSlots];

        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, port);
            kestrel.AllowSynchronousIO = false;
        });

        WebApplication app = builder.Build();
        app.MapGet("/work/{id:int}", (int id) => Handle(id));
        app.Start();

        _url = ResolveAddress(app);
        _app = app;

        var handler = new SocketsHttpHandler
        {
            // A generous pool with no idle timeout: connection churn would add its own latency tail and
            // this scenario exists to measure the collector's, not the connection pool's.
            MaxConnectionsPerServer = int.MaxValue,
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
            UseProxy = false,
            AllowAutoRedirect = false,
        };

        _invoker = new HttpMessageInvoker(handler);
        _client = new HttpClient(handler, disposeHandler: false) { Timeout = TimeSpan.FromSeconds(30) };

        // Prime the connection pool and JIT the request path before measurement begins.
        for (int i = 0; i < 32; i++)
        {
            _ = Issue(i);
        }

        Interlocked.Exchange(ref _requests, 0);
        Interlocked.Exchange(ref _failures, 0);
        Interlocked.Exchange(ref _bytes, 0);
    }

    private static string ResolveAddress(WebApplication app)
    {
        foreach (string address in app.Urls)
        {
            return address;
        }

        throw new InvalidOperationException("Kestrel did not report a bound address.");
    }

    private object Handle(int id)
    {
        // Per-request garbage, plus a bounded promotion into a longer-lived cache. This is the shape
        // that makes a request-serving workload interesting to a collector.
        byte[] payload = new byte[_payloadSize];
        payload[0] = (byte)id;
        payload[^1] = (byte)(id >> 8);

        int slot = (int)((uint)id % (uint)_sessionSlots);
        SessionEntry? existing = Volatile.Read(ref _sessions[slot]);
        if (existing is null || (id & 0x7) == 0)
        {
            var entry = new SessionEntry
            {
                Payload = payload,
                Key = string.Create(CultureInfo.InvariantCulture, $"session-{slot}-{id}"),
            };

            Volatile.Write(ref _sessions[slot], entry);
            existing = entry;
        }

        Interlocked.Increment(ref existing.Hits);

        int checksum = 0;
        for (int i = 0; i < payload.Length; i += 64)
        {
            checksum += payload[i];
        }

        return new { id, slot, key = existing.Key, hits = Interlocked.Read(ref existing.Hits), checksum };
    }

    private long Issue(int id)
    {
        HttpClient client = _client!;
        using HttpResponseMessage response = client.GetAsync(
            $"{_url}/work/{id}", HttpCompletionOption.ResponseContentRead).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Interlocked.Increment(ref _failures);
            return 0;
        }

        byte[] body = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
        Interlocked.Add(ref _bytes, body.Length);
        return body.Length;
    }

    public long RunOperation(int workerIndex)
    {
        long id = Interlocked.Increment(ref _requests);
        return Issue((int)(id & 0x7FFFFFF));
    }

    public ScenarioVerification Verify()
    {
        long requests = Interlocked.Read(ref _requests);
        long failures = Interlocked.Read(ref _failures);
        long bytes = Interlocked.Read(ref _bytes);

        var violations = new List<string>();
        if (failures > 0)
        {
            violations.Add($"{failures} HTTP requests did not succeed; a latency number taken over failed requests is meaningless.");
        }

        if (requests > 0 && bytes == 0)
        {
            violations.Add("Requests were issued but no response bytes were read, so the server did no observable work.");
        }

        return new ScenarioVerification
        {
            Success = requests > 0 && failures == 0 && bytes > 0,
            Marker = $"aspnet-request-load:requests={requests}",
            Detail = $"requests={requests}, responseBytes={bytes}, sessionSlots={_sessionSlots}, payloadBytes={_payloadSize}",
            Violations = violations,
        };
    }

    public void Teardown()
    {
        _client?.Dispose();
        _invoker?.Dispose();
        _app?.StopAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
        (_app as IDisposable)?.Dispose();
        _app = null;
    }
}
