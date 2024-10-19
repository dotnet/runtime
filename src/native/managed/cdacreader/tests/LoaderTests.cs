// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using MockLoader = MockDescriptors.Loader;

public unsafe class LoaderTests
{
    private static void RunLoaderContractTest(TargetTestHelpers helpers, Func<MockMemorySpace.Builder, Dictionary<DataType, Target.TypeInfo>, MockMemorySpace.Builder> configure, Action<Target> testCase)
    {
        var types = MockLoader.Types(helpers);

        MockMemorySpace.Builder builder = new(helpers);
        builder = builder
            .SetContracts([nameof(Contracts.Loader)])
            .SetTypes(types);

        if (configure != null)
        {
            builder = configure(builder, types);
        }

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);
        testCase(target);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPath(MockTarget.Architecture arch)
    {
        TargetPointer address = 0x1000;
        TargetPointer emptyPathAddress = 0x2000;
        string expected = $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}TestModule.dll";

        TargetTestHelpers helpers = new(arch);
        RunLoaderContractTest(
            helpers,
            (builder, types) =>
            {
                MockLoader.AddModule(helpers, builder, address);
                MockLoader.AddModule(helpers, builder, emptyPathAddress);

                Target.TypeInfo typeInfo = types[DataType.Module];
                ulong pathAddress = address + typeInfo.Size.Value;
                helpers.WritePointer(
                    builder.BorrowAddressRange(address + (ulong)typeInfo.Fields[nameof(Data.Module.Path)].Offset, helpers.PointerSize),
                    pathAddress);

                MockDescriptors.AddUtf16String(helpers, builder, pathAddress, expected);
                return builder;
            },
            (target) =>
            {
                Contracts.ILoader contract = target.Contracts.Loader;
                Assert.NotNull(contract);
                {
                    Contracts.ModuleHandle handle = contract.GetModuleHandle(address);
                    string actual = contract.GetPath(handle);
                    Assert.Equal(expected, actual);
                }
                {
                    Contracts.ModuleHandle handle = contract.GetModuleHandle(emptyPathAddress);
                    string actual = contract.GetFileName(handle);
                    Assert.Equal(string.Empty, actual);
                }
            });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFileName(MockTarget.Architecture arch)
    {
        TargetPointer address = 0x1000;
        TargetPointer emptyNameAddress = 0x2000;
        string expected = $"TestModule.dll";

        TargetTestHelpers helpers = new(arch);
        RunLoaderContractTest(
            helpers,
            (builder, types) =>
            {
                MockLoader.AddModule(helpers, builder, address);
                MockLoader.AddModule(helpers, builder, emptyNameAddress);

                Target.TypeInfo typeInfo = types[DataType.Module];
                ulong fileNameAddress = address + typeInfo.Size.Value;
                helpers.WritePointer(
                    builder.BorrowAddressRange(address + (ulong)typeInfo.Fields[nameof(Data.Module.FileName)].Offset, helpers.PointerSize),
                    fileNameAddress);

                MockDescriptors.AddUtf16String(helpers, builder, fileNameAddress, expected);
                return builder;
            },
            (target) =>
            {
                Contracts.ILoader contract = target.Contracts.Loader;
                Assert.NotNull(contract);
                {
                    Contracts.ModuleHandle handle = contract.GetModuleHandle(address);
                    string actual = contract.GetFileName(handle);
                    Assert.Equal(expected, actual);
                }
                {
                    Contracts.ModuleHandle handle = contract.GetModuleHandle(emptyNameAddress);
                    string actual = contract.GetFileName(handle);
                    Assert.Equal(string.Empty, actual);
                }
            });
    }
}
