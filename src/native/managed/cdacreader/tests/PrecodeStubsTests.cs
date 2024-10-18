// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
using System;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class PrecodeStubsTests
{
    // high level outline of a precode machine descriptor
    public class PrecodeTestDescriptor {
        public string Name { get; }
        public PrecodeTestDescriptor(string name) {
            Name = name;
        }
    }

    internal static PrecodeTestDescriptor X64TestDescriptor = new PrecodeTestDescriptor("X64");
    internal static PrecodeTestDescriptor Arm64TestDescriptor = new PrecodeTestDescriptor("Arm64");
    internal static PrecodeTestDescriptor LoongArch64TestDescriptor = new PrecodeTestDescriptor("LoongArch64");
    internal static PrecodeTestDescriptor GenericTestDescriptor = new PrecodeTestDescriptor("Generic");

    public static IEnumerable<object[]> PrecodeTestDescriptorData()
    {
        foreach (object[] inp in new MockTarget.StdArch())
        {
            MockTarget.Architecture arch = (MockTarget.Architecture)inp[0];
            if (arch.Is64Bit && arch.IsLittleEndian)
            {
                yield return new object[] { arch, X64TestDescriptor };
                yield return new object[] { arch, Arm64TestDescriptor };
                yield return new object[] { arch, LoongArch64TestDescriptor };
            }
            yield return new object[] { arch, GenericTestDescriptor};
        }
    }

    [Theory]
    [MemberData(nameof(PrecodeTestDescriptorData))]
    public void TestPrecodeStubs(MockTarget.Architecture arch, PrecodeTestDescriptor precodeTestDescriptor)
    {
        // TODO: make a PrecodeMachineDescriptor based on the precodeTestDescriptor and then make some stubs
        // and ask them for their MethodDesc
        // TODO: finish me
        var target = new TestPlaceholderTarget(arch);
        Assert.NotNull(target);
        Assert.NotNull(precodeTestDescriptor);
    }
}
