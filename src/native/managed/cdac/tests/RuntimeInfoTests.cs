// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class RuntimeInfoTests
{
    internal static Target CreateTarget(
        MockTarget.Architecture arch,
        (string Name, string Value)[] globalStrings)
    {
        MockMemorySpace.Builder builder = new MockMemorySpace.Builder(new TargetTestHelpers(arch));
        TestPlaceholderTarget target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, [], [], globalStrings);

        IContractFactory<IRuntimeInfo> runtimeInfoFactory = new RuntimeInfoFactory();

        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.RuntimeInfo == runtimeInfoFactory.CreateContract(target, 1));
        target.SetContracts(reg);
        return target;
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
        var target = CreateTarget(arch, [(Constants.Globals.Architecture, architecture)]);

        // TEST

        var runtimeInfo = target.Contracts.RuntimeInfo;
        Assert.NotNull(runtimeInfo);

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
        var target = CreateTarget(arch, [(Constants.Globals.OperatingSystem, os)]);

        // TEST

        var runtimeInfo = target.Contracts.RuntimeInfo;
        Assert.NotNull(runtimeInfo);

        var actualArchitecture = runtimeInfo.GetTargetOperatingSystem();
        Assert.Equal(expectedOS, actualArchitecture);
    }
}
