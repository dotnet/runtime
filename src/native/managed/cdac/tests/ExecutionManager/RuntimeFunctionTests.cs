// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using System.Collections.Generic;
using System;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

public class RuntimeFunctionTests
{
    public static IEnumerable<object[]> StdArchFunctionLengthData()
    {
        foreach (object[] arr in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)arr[0];
            yield return new object[] { arch, true /*includeEndAddress*/, false /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, true /*includeEndAddress*/, true /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, false /*includeEndAddress*/, false /*unwindInfoIsFunctionLength*/};
            yield return new object[] { arch, false /*includeEndAddress*/, true /*unwindInfoIsFunctionLength*/};
        }
    }

    [Theory]
    [MemberData(nameof(StdArchFunctionLengthData))]
    public void GetFunctionLength(MockTarget.Architecture arch, bool includeEndAddress, bool unwindInfoIsFunctionLength)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.RuntimeFunctions runtimeFunctions = new(builder, includeEndAddress, unwindInfoIsFunctionLength);

        uint[] entries = [0x100, 0x1f0, 0x1000, 0x2000, 0xa000];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        Target target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, runtimeFunctions.Types);
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        for (uint i = 0; i < entries.Length; i++)
        {
            uint expectedFunctionLength = i < entries.Length - 1
                ? Math.Min(entries[i + 1] - entries[i], MockDescriptors.RuntimeFunctions.DefaultFunctionLength)
                : MockDescriptors.RuntimeFunctions.DefaultFunctionLength;

            Data.RuntimeFunction function = lookup.GetRuntimeFunction(addr, i);
            uint functionLength = lookup.GetFunctionLength(function);
            Assert.Equal(expectedFunctionLength, functionLength);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetRuntimeFunctionIndexForAddress(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.RuntimeFunctions runtimeFunctions = new(builder);

        uint[] entries = [0x100, 0x1f0, 0x1000, 0x2000, 0xa000];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        TestPlaceholderTarget target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, runtimeFunctions.Types);
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.PlatformMetadata == new Mock<Contracts.IPlatformMetadata>().Object);
        target.SetContracts(reg);
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        for (uint i = 0; i < entries.Length; i++)
        {
            TargetPointer relativeAddress = (TargetPointer)entries[i];
            bool res = lookup.TryGetRuntimeFunctionIndexForAddress(addr, (uint)entries.Length, relativeAddress, out uint index);
            Assert.True(res);
            Assert.Equal(i, index);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void TryGetRuntimeFunctionIndexForAddress_NoMatch(MockTarget.Architecture arch)
    {
        MockMemorySpace.Builder builder = new(new TargetTestHelpers(arch));
        MockDescriptors.RuntimeFunctions runtimeFunctions = new(builder);

        uint[] entries = [0x100, 0x1f0];
        TargetPointer addr = runtimeFunctions.AddRuntimeFunctions(entries);

        TestPlaceholderTarget target = new TestPlaceholderTarget(builder.TargetTestHelpers.Arch, builder.GetMemoryContext().ReadFromTarget, runtimeFunctions.Types);
        ContractRegistry reg = Mock.Of<ContractRegistry>(
            c => c.PlatformMetadata == new Mock<Contracts.IPlatformMetadata>().Object);
        target.SetContracts(reg);
        RuntimeFunctionLookup lookup = RuntimeFunctionLookup.Create(target);

        TargetPointer relativeAddress = 0x0ff;
        bool res = lookup.TryGetRuntimeFunctionIndexForAddress(addr, (uint)entries.Length, relativeAddress, out _);
        Assert.False(res);
    }
}
