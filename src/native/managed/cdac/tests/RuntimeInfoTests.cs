// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
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
            .AddContract<IRuntimeInfo>(version: 1)
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

    private static readonly MockTarget.Architecture DefaultArch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true };

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
