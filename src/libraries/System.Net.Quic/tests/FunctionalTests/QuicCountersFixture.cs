// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.XUnitExtensions;
using System;
using System.Net.Quic;
using System.Reflection;
using System.Text;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

using Microsoft.Quic;
using static Microsoft.Quic.MsQuic;

[CollectionDefinition(nameof(QuicTestCollection))]
public unsafe class QuicTestCollection : ICollectionFixture<QuicTestCollection>, IDisposable
{
    public static bool IsSupported => QuicListener.IsSupported && QuicConnection.IsSupported;

    public QuicTestCollection()
    {
        string msQuicLibraryVersion = GetMsQuicLibraryVersion();
        // If any of the reflection bellow breaks due to changes in "System.Net.Quic.MsQuicApi", also check and fix HttpStress project as it uses the same hack.
        Console.WriteLine($"MsQuic {(IsSupported ? "supported" : "not supported")} and using '{msQuicLibraryVersion}'.");

        if (IsSupported)
        {
            QUIC_SETTINGS settings = default(QUIC_SETTINGS);
            settings.IsSet.MaxWorkerQueueDelayUs = 1;
            settings.MaxWorkerQueueDelayUs = 2_500_000u; // 2.5s, 10x the default
            if (MsQuic.StatusFailed(GetApiTable()->SetParam(null, MsQuic.QUIC_PARAM_GLOBAL_SETTINGS, (uint)sizeof(QUIC_SETTINGS), (byte*)&settings)))
            {
                Console.WriteLine($"Unable to set MsQuic MaxWorkerQueueDelayUs.");
            }
        }
    }

    public unsafe void Dispose()
    {
        if (!IsSupported)
        {
            return;
        }

        long[] counters = new long[(int)QUIC_PERFORMANCE_COUNTERS.MAX];
        int countersAvailable;

        int status;
        fixed (long* pCounters = counters)
        {
            uint size = (uint)counters.Length * sizeof(long);
            status = GetApiTable()->GetParam(null, QUIC_PARAM_GLOBAL_PERF_COUNTERS, &size, (byte*)pCounters);
            countersAvailable = (int)size / sizeof(long);
        }

        if (StatusFailed(status))
        {
            System.Console.WriteLine($"Failed to read MsQuic counters: {status}");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("MsQuic Counters:");

        int maxlen = Enum.GetNames(typeof(QUIC_PERFORMANCE_COUNTERS)).Max(s => s.Length);
        void DumpCounter(QUIC_PERFORMANCE_COUNTERS counter)
        {
            var name = $"{counter}:".PadRight(maxlen + 1);
            var index = (int)counter;
            var value = index < countersAvailable ? counters[(int)counter].ToString() : "N/A";
            sb.AppendLine($"    {counter} {value}");
        }

        DumpCounter(QUIC_PERFORMANCE_COUNTERS.CONN_CREATED);
        DumpCounter(QUIC_PERFORMANCE_COUNTERS.CONN_HANDSHAKE_FAIL);
        DumpCounter(QUIC_PERFORMANCE_COUNTERS.CONN_APP_REJECT);
        DumpCounter(QUIC_PERFORMANCE_COUNTERS.CONN_LOAD_REJECT);

        System.Console.WriteLine(sb.ToString());
    }

    private static string? GetMsQuicLibraryVersion()
    {
        Type msQuicApiType = Type.GetType("System.Net.Quic.MsQuicApi, System.Net.Quic");

        return (string)msQuicApiType.GetProperty("MsQuicLibraryVersion", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true).Invoke(null, Array.Empty<object?>());
    }

    private static QUIC_API_TABLE* GetApiTable()
    {
        Type msQuicApiType = Type.GetType("System.Net.Quic.MsQuicApi, System.Net.Quic");
        object msQuicApiInstance = msQuicApiType.GetProperty("Api", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true).Invoke(null, Array.Empty<object?>());
        return (QUIC_API_TABLE*)(Pointer.Unbox(msQuicApiType.GetProperty("ApiTable").GetGetMethod().Invoke(msQuicApiInstance, Array.Empty<object?>())));
    }
}
