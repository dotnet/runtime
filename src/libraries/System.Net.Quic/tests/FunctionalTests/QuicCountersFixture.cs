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

public class QuicCountersListener : IDisposable
{
    public unsafe void Dispose()
    {
        Type msQuicApiType = Type.GetType("System.Net.Quic.MsQuicApi, System.Net.Quic");
        object msQuicApiInstance = msQuicApiType.GetProperty("Api", BindingFlags.NonPublic | BindingFlags.Static).GetGetMethod(true).Invoke(null, Array.Empty<object?>());
        QUIC_API_TABLE* apiTable = (QUIC_API_TABLE*)(Pointer.Unbox(msQuicApiType.GetProperty("ApiTable").GetGetMethod().Invoke(msQuicApiInstance, Array.Empty<object?>())));

        long[] counters = new long[(int)QUIC_PERFORMANCE_COUNTERS.MAX];
        int countersAvailable;

        int status;
        fixed (long* pCounters = counters)
        {
            uint size = (uint)counters.Length * sizeof(long);
            status = apiTable->GetParam(null, QUIC_PARAM_GLOBAL_PERF_COUNTERS, &size, (byte*)pCounters);
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
}

[CollectionDefinition(nameof(QuicCountersListener), DisableParallelization = true)]
public class QuicCountersCollection : ICollectionFixture<QuicCountersListener>
{
}