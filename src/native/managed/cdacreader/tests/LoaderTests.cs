// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

using MockLoader = MockDescriptors.Loader;

public unsafe class LoaderTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetPath(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        builder = builder
            .SetContracts([nameof(Contracts.Loader)])
            .SetTypes(MockLoader.Types(helpers));

        string expected = $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}TestModule.dll";

        // Add the modules
        MockLoader loader = new(builder);
        TargetPointer moduleAddr = loader.AddModule(helpers, path: expected);
        TargetPointer moduleAddrEmptyPath = loader.AddModule(helpers);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);

        // Validate the expected module data
        Contracts.ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddr);
            string actual = contract.GetPath(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddrEmptyPath);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetFileName(MockTarget.Architecture arch)
    {
        // Set up the target
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        builder = builder
            .SetContracts([nameof(Contracts.Loader)])
            .SetTypes(MockLoader.Types(helpers));

        string expected = $"TestModule.dll";

        // Add the modules
        MockLoader loader = new(builder);
        TargetPointer moduleAddr = loader.AddModule(helpers, fileName: expected);
        TargetPointer moduleAddrEmptyName = loader.AddModule(helpers);

        bool success = builder.TryCreateTarget(out ContractDescriptorTarget? target);
        Assert.True(success);

        // Validate the expected module data
        Contracts.ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddr);
            string actual = contract.GetFileName(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddrEmptyName);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }
}
