// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static unsafe class CodePlacement
{
    private static delegate* managed<int> s_target;
    private static int Target() => 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PlacementProbeA<T, U>() => s_target();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int PlacementProbeB<T, U>() => s_target();

    private const int Batch = 8;

    private static bool ExpectFar(int index) =>
        index < 4 * Batch ? (index / Batch) % 2 == 0 : index % 2 == 0;

    private static int RunProbes()
    {
        s_target = &Target;
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Static;
        var a = typeof(CodePlacement).GetMethod(nameof(PlacementProbeA), flags)!;
        var b = typeof(CodePlacement).GetMethod(nameof(PlacementProbeB), flags)!;
        // ForceRelocs requests far placement for odd method RIDs.
        var (far, near) = (a.MetadataToken & 1) != 0 ? (a, b) : (b, a);
        Type[] types = { typeof(int), typeof(long), typeof(short), typeof(byte), typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte) };
        var calls = new Func<int>[6 * Batch];
        for (int i = 0; i < calls.Length; i++)
        {
            var method = ExpectFar(i) ? far : near;
            calls[i] = method.MakeGenericMethod(types[i / types.Length], types[i % types.Length]).CreateDelegate<Func<int>>();
        }

        // Finish reflection and delegate setup before changing placement.
        int sum = 0;
        foreach (var call in calls)
            sum += call();
        return sum == calls.Length ? 100 : 1;
    }

    private sealed class Probe
    {
        public bool Near, Far, Applied;
        public int Retries;
    }

    private static int Main(string[] args)
    {
        if (args is ["child"])
            return RunProbes();

        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, RedirectStandardOutput = true };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("child");
        start.Environment["DOTNET_ForceRelocs"] = "1";
        start.Environment["DOTNET_TieredCompilation"] = "0";
        start.Environment["DOTNET_ReadyToRun"] = "0";
        start.Environment["DOTNET_JitEnableOptionalRelocs"] = "1";
        start.Environment["DOTNET_LogEnable"] = "1";
        start.Environment["DOTNET_LogToConsole"] = "1";
        start.Environment["DOTNET_LogFacility"] = "8"; // LF_JIT
        start.Environment["DOTNET_LogLevel"] = "7"; // LL_INFO10000

        using var child = Process.Start(start)!;
        var probes = new List<Probe>();
        var byMethod = new Dictionary<string, Probe>();
        while (child.StandardOutput.ReadLine() is string line)
        {
            if (!line.Contains("PlacementProbe", StringComparison.Ordinal))
                continue;
            string[] fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 5 || fields[^4] is not ("placement" or "relocation"))
                continue;
            if (!byMethod.TryGetValue(fields[^1], out var probe))
            {
                byMethod.Add(fields[^1], probe = new Probe());
                probes.Add(probe);
            }
            switch (fields[^3])
            {
                case "far": probe.Far = true; break;
                case "near": probe.Near = true; break;
                case "applied": probe.Applied = true; break;
                case "overflow": probe.Retries++; break;
            }
        }
        child.WaitForExit();
        if (child.ExitCode != 100 || probes.Count != 6 * Batch)
        {
            Console.WriteLine($"Child exit code {child.ExitCode}, observed {probes.Count} of {6 * Batch} probes.");
            return 1;
        }

        for (int i = 0; i < probes.Count; i++)
        {
            if (probes[i].Far != ExpectFar(i) || probes[i].Near == ExpectFar(i))
            {
                Console.WriteLine($"Skipping retry counts: ForceRelocs could not obtain the requested placement for probe {i}.");
                return 100;
            }
        }

        int[] farRetries = { probes.Take(Batch).Sum(p => p.Retries), probes.Skip(2 * Batch).Take(Batch).Sum(p => p.Retries) };
        int alternatingRetries = probes.Skip(4 * Batch).Sum(p => p.Retries);
        bool recovered = probes[2 * Batch - 1].Applied && probes[4 * Batch - 1].Applied;
        Console.WriteLine($"Page retries in stable far batches: {string.Join(", ", farRetries)}. " +
                          $"ADRP restored after both: {recovered}. Page retries during alternation: {alternatingRetries}.");
        // Startup may have already established far placement. Each near batch must
        // restore ADRP, so the next stable far batch should miss exactly once, on entry.
        return farRetries[0] == probes[0].Retries && farRetries[0] <= 1 &&
               farRetries[1] == 1 && probes[2 * Batch].Retries == 1 && recovered ? 100 : 1;
    }
}
