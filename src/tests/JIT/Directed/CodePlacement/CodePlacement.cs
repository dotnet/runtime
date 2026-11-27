// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static unsafe class CodePlacement
{
    private static delegate* managed<int> s_target;
    private static int Target() => 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProbeFar<T, U>() => s_target();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ProbeNear<T>() => s_target();

    private static int RunBatch()
    {
        s_target = &Target;
        Type[] types = { typeof(int), typeof(long), typeof(short), typeof(byte), typeof(uint), typeof(ulong), typeof(ushort), typeof(sbyte) };
        var calls = new List<Func<int>>();
        var far = typeof(CodePlacement).GetMethod(nameof(ProbeFar), BindingFlags.NonPublic | BindingFlags.Static);
        foreach (Type t in types)
        {
            for (int i = 0; i < 4; i++)
            {
                calls.Add(far.MakeGenericMethod(t, types[i]).CreateDelegate<Func<int>>());
            }
        }

        // Resolve the indirect call target before placement starts changing.
        int sum = ProbeNear<int>();
        foreach (var call in calls)
            sum += call();

        // The first near allocation clears the estimate used during its import.
        sum += ProbeNear<byte>();
        sum += ProbeNear<long>();
        return sum == 35 ? 100 : 1;
    }

    private static int Main(string[] args)
    {
        if (args is ["child"])
            return RunBatch();

        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, UseShellExecute = false };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("child");
        start.Environment["DOTNET_JitStressCodePlacement"] = nameof(ProbeFar);
        start.Environment["DOTNET_TieredCompilation"] = "0";
        start.Environment["DOTNET_ReadyToRun"] = "0";
        start.Environment["DOTNET_JitEnableOptionalRelocs"] = "1";
        start.Environment["DOTNET_JitForcePageRelocOverflow"] = "0";
        start.Environment["DOTNET_ForceRelocs"] = "0";
        start.Environment["DOTNET_JitDisasm"] = "CodePlacement:Probe*";
        start.Environment.Remove("DOTNET_JitStdOutFile");

        using Process child = Process.Start(start)!;
        string output = child.StandardOutput.ReadToEnd();
        child.WaitForExit();
        if (child.ExitCode != 100)
            return child.ExitCode == 0 ? 1 : child.ExitCode;

        var farMethods = new HashSet<string>();
        var farAttempts = new List<bool>();
        string firstFar = null;
        bool retriedFirst = false;
        var nearAttempts = new List<bool>();
        foreach (string listing in output.Split("; Assembly listing for method ", StringSplitOptions.RemoveEmptyEntries))
        {
            using var reader = new StringReader(listing);
            string name = reader.ReadLine()!;
            bool usesPage = listing.Contains("[HIGH RELOC");
            if (name.StartsWith("CodePlacement:ProbeFar[", StringComparison.Ordinal))
            {
                if (farAttempts.Count == 0)
                    firstFar = name;
                else if (farAttempts.Count == 1)
                    retriedFirst = name == firstFar;
                farMethods.Add(name);
                farAttempts.Add(usesPage);
            }
            else if (name.StartsWith("CodePlacement:ProbeNear[", StringComparison.Ordinal))
            {
                nearAttempts.Add(usesPage);
            }
        }

        // Only the first of the 32 far methods should try ADRP. Its second listing
        // is the absolute retry. Later far methods should compile once each.
        if (farMethods.Count == 32 && farAttempts.Count == 33 && retriedFirst && farAttempts[0] &&
            farAttempts.LastIndexOf(true) == 0 && nearAttempts.Count == 3 &&
            nearAttempts[0] && !nearAttempts[1] && nearAttempts[2])
            return 100;

        Console.WriteLine($"Far methods: {farMethods.Count}, far attempts: {farAttempts.Count} (expected 33), " +
                          $"far ADRP attempts: {farAttempts.FindAll(page => page).Count}, near ADRP: {string.Join(", ", nearAttempts)}");
        return 1;
    }
}
