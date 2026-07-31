// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class RuntimeInfoTests
{
    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        (string Name, string Value)[] globalStrings,
        (string Name, ulong Value)[]? globals = null)
    {
        var builder = new TestPlaceholderTarget.Builder(arch);
        if (globals is not null)
            builder.AddGlobals(globals);
        return builder
            .AddGlobalStrings(globalStrings)
            .AddContract<IRuntimeInfo>(version: "c1")
            .Build();
    }

    public static IEnumerable<object[]> StdArchAllTargetArchitectures()
    {
        foreach(object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];

            foreach(RuntimeInfoArchitecture targetArch in (RuntimeInfoArchitecture[])Enum.GetValues(typeof(RuntimeInfoArchitecture)))
            {
                // Skip Unknown architecture
                if (targetArch == RuntimeInfoArchitecture.Unknown)
                    continue;

                yield return new object[] { arch, targetArch.ToString().ToLowerInvariant(), targetArch };
            }

            yield return new object[] { arch, "notATargetArch", RuntimeInfoArchitecture.Unknown };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllTargetArchitectures))]
    public void GetTargetArchitectureTest(
        MockTarget.Architecture arch,
        string architecture,
        RuntimeInfoArchitecture expectedArchitecture)
    {
        Target target = CreateTarget(arch, [(Constants.Globals.Architecture, architecture)]);

        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        var actualArchitecture = runtimeInfo.GetTargetArchitecture();
        Assert.Equal(expectedArchitecture, actualArchitecture);
    }

    public static IEnumerable<object[]> StdArchAllTargetOS()
    {
        foreach(object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];

            foreach(RuntimeInfoOperatingSystem targetArch in (RuntimeInfoOperatingSystem[])Enum.GetValues(typeof(RuntimeInfoOperatingSystem)))
            {
                // Skip Unknown architecture
                if (targetArch == RuntimeInfoOperatingSystem.Unknown)
                    continue;

                yield return new object[] { arch, targetArch.ToString().ToLowerInvariant(), targetArch };
            }

            yield return new object[] { arch, "notAnOperatingSystem", RuntimeInfoOperatingSystem.Unknown };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllTargetOS))]
    public void GetTargetOperatingSystemTest(
        MockTarget.Architecture arch,
        string os,
        RuntimeInfoOperatingSystem expectedOS)
    {
        Target target = CreateTarget(arch, [(Constants.Globals.OperatingSystem, os)]);

        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        var actualArchitecture = runtimeInfo.GetTargetOperatingSystem();
        Assert.Equal(expectedOS, actualArchitecture);
    }

    public static IEnumerable<object[]> StdArchAllRuntimeFlavors()
    {
        foreach(object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];

            foreach(RuntimeInfoRuntimeFlavor flavor in (RuntimeInfoRuntimeFlavor[])Enum.GetValues(typeof(RuntimeInfoRuntimeFlavor)))
            {
                yield return new object[] { arch, flavor.ToString().ToLowerInvariant(), flavor };
            }

            yield return new object[] { arch, "notARuntimeFlavor", RuntimeInfoRuntimeFlavor.Unknown };
        }
    }

    [Theory]
    [MemberData(nameof(StdArchAllRuntimeFlavors))]
    public void GetRuntimeFlavorTest(
        MockTarget.Architecture arch,
        string flavor,
        RuntimeInfoRuntimeFlavor expectedFlavor)
    {
        Target target = CreateTarget(arch, [(Constants.Globals.RuntimeFlavor, flavor)]);

        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;
        var actualFlavor = runtimeInfo.GetRuntimeFlavor();
        Assert.Equal(expectedFlavor, actualFlavor);
    }

    [Fact]
    public void GetRuntimeFlavor_GlobalAbsent_ReturnsUnknown()
    {
        Target target = CreateTarget(DefaultArch, []);
        Assert.Equal(RuntimeInfoRuntimeFlavor.Unknown, target.Contracts.RuntimeInfo.GetRuntimeFlavor());
    }

    private static readonly MockTarget.Architecture DefaultArch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };

    [Fact]
    public void Values_AreCached_AndClearedOnFlush()
    {
        var target = new CountingTarget(DefaultArch);
        IRuntimeInfo runtimeInfo = target.Contracts.RuntimeInfo;

        // First access reads each global once.
        Assert.Equal(RuntimeInfoArchitecture.X64, runtimeInfo.GetTargetArchitecture());
        Assert.Equal(RuntimeInfoOperatingSystem.Windows, runtimeInfo.GetTargetOperatingSystem());
        Assert.Equal(RuntimeInfoRuntimeFlavor.Coreclr, runtimeInfo.GetRuntimeFlavor());
        Assert.Equal((uint)42, runtimeInfo.GetRecommendedReaderVersion());

        int baselineStringReads = target.GlobalStringReadCount;
        int baselineUintReads = target.GlobalReadCount;

        // Subsequent accesses must not re-read.
        for (int i = 0; i < 3; i++)
        {
            _ = runtimeInfo.GetTargetArchitecture();
            _ = runtimeInfo.GetTargetOperatingSystem();
            _ = runtimeInfo.GetRuntimeFlavor();
            _ = runtimeInfo.GetRecommendedReaderVersion();
        }
        Assert.Equal(baselineStringReads, target.GlobalStringReadCount);
        Assert.Equal(baselineUintReads, target.GlobalReadCount);

        // Flush clears the cache: next accesses must re-read.
        runtimeInfo.Flush(FlushScope.All);
        _ = runtimeInfo.GetTargetArchitecture();
        _ = runtimeInfo.GetTargetOperatingSystem();
        _ = runtimeInfo.GetRuntimeFlavor();
        _ = runtimeInfo.GetRecommendedReaderVersion();
        Assert.True(target.GlobalStringReadCount > baselineStringReads);
        Assert.True(target.GlobalReadCount > baselineUintReads);
    }

    private sealed class CountingTarget : TestPlaceholderTarget
    {
        public int GlobalStringReadCount { get; private set; }
        public int GlobalReadCount { get; private set; }

        public CountingTarget(MockTarget.Architecture arch)
            : base(
                arch,
                static (ulong _, Span<byte> _) => 0,
                globals: [(Constants.Globals.RecommendedReaderVersion, 42UL)],
                globalStrings:
                [
                    (Constants.Globals.Architecture, "x64"),
                    (Constants.Globals.OperatingSystem, "windows"),
                    (Constants.Globals.RuntimeFlavor, "coreclr"),
                ])
        {
            SetupContractRegistry(registry => registry.Register<IRuntimeInfo>("c1", t => new RuntimeInfo_1(t)))
                .SetVersion<IRuntimeInfo>("c1");
        }

        public override bool TryReadGlobalString(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
        {
            GlobalStringReadCount++;
            return base.TryReadGlobalString(name, out value);
        }

        public override bool TryReadGlobal<T>(string name, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out T? value)
        {
            GlobalReadCount++;
            return base.TryReadGlobal<T>(name, out value);
        }
    }

    [Fact]
    public void RecommendedReaderVersion_GlobalPresent_ReturnsValue()
    {
        Target target = CreateTarget(
            DefaultArch,
            [],
            [(Constants.Globals.RecommendedReaderVersion, (ulong)2)]);
        Assert.Equal((uint)2, target.Contracts.RuntimeInfo.GetRecommendedReaderVersion());
    }

    [Fact]
    public void RecommendedReaderVersion_GlobalAbsent_ReturnsZero()
    {
        Target target = CreateTarget(DefaultArch, []);
        Assert.Equal((uint)0, target.Contracts.RuntimeInfo.GetRecommendedReaderVersion());
    }
}
