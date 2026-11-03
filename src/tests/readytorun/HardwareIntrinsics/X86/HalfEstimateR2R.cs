// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

public static class HalfEstimateR2R
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Half TestReciprocal(Half value) => Half.ReciprocalEstimate(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Half TestReciprocalSqrt(Half value) => Half.ReciprocalSqrtEstimate(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Half TestSqrt(Half value) => Half.Sqrt(value);

    [Fact]
    public static void TestEntryPoint()
    {
        // ReadyToRun maps AVX10v1 to Avx512Bf16 in ReadyToRunInstructionSetHelper.
        const string Avx10v1Prerequisite = "Avx512Bf16-";

        string imagePath = Path.Combine(Environment.CurrentDirectory, "composite-r2r.dll");
        string coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT")
            ?? throw new InvalidOperationException("CORE_ROOT is required to inspect the ReadyToRun image.");

        Assert.True(File.Exists(imagePath), imagePath);

        (string methodName, string instruction)[] estimates =
        [
            (nameof(TestReciprocal), "vrcpsh"),
            (nameof(TestReciprocalSqrt), "vrsqrtsh"),
        ];
        foreach ((string methodName, string instruction) in estimates)
        {
            string disassembly = GetDisassembly(imagePath, coreRoot, methodName);
            Assert.Matches(@"\bcall\b", disassembly);
            Assert.DoesNotContain(instruction, disassembly, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("CHECK_InstructionSetSupport", disassembly);
            Assert.Contains(Avx10v1Prerequisite, disassembly);
        }

        string sqrt = GetDisassembly(imagePath, coreRoot, nameof(TestSqrt));
        Assert.DoesNotContain("CHECK_InstructionSetSupport", sqrt);
    }

    private static string GetDisassembly(string imagePath, string coreRoot, string methodName)
    {
        string? testDotNet = Environment.GetEnvironmentVariable("__TestDotNetCmd");
        ProcessStartInfo startInfo = new(testDotNet is null ? "dotnet" : testDotNet)
        {
            RedirectStandardOutput = true,
            InheritedHandles = [],
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(Path.Combine(coreRoot, "R2RDump", "R2RDump.dll"));
        startInfo.ArgumentList.Add("--in");
        startInfo.ArgumentList.Add(imagePath);
        startInfo.ArgumentList.Add("--query");
        startInfo.ArgumentList.Add(methodName);
        startInfo.ArgumentList.Add("--disasm");
        startInfo.ArgumentList.Add("--rp");
        startInfo.ArgumentList.Add(coreRoot);

        using Process process = Process.Start(startInfo)!;
        string output = process.StandardOutput.ReadToEnd();
        ProcessExitStatus status = process.WaitForExitStatus();

        Assert.True(status.ExitCode == 0, output);
        Assert.Contains($"1 result(s) for \"{methodName}\"", output);
        return output;
    }
}
