// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

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
        MockLoader loader = new(builder);

        string expected = $"{AppContext.BaseDirectory}{Path.DirectorySeparatorChar}TestModule.dll";

        // Add the modules
        TargetPointer moduleAddr = loader.AddModule(path: expected);
        TargetPointer moduleAddrEmptyPath = loader.AddModule();

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, loader.Types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)));

        // Validate the expected module data
        ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetPath(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyPath);
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
        MockLoader loader = new(builder);

        string expected = $"TestModule.dll";

        // Add the modules
        TargetPointer moduleAddr = loader.AddModule(fileName: expected);
        TargetPointer moduleAddrEmptyName = loader.AddModule();

        var target = new TestPlaceholderTarget(arch, builder.GetMemoryContext().ReadFromTarget, loader.Types);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.Loader == ((IContractFactory<ILoader>)new LoaderFactory()).CreateContract(target, 1)));

        // Validate the expected module data
        Contracts.ILoader contract = target.Contracts.Loader;
        Assert.NotNull(contract);
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddr);
            string actual = contract.GetFileName(handle);
            Assert.Equal(expected, actual);
        }
        {
            Contracts.ModuleHandle handle = contract.GetModuleHandleFromModulePtr(moduleAddrEmptyName);
            string actual = contract.GetFileName(handle);
            Assert.Equal(string.Empty, actual);
        }
    }
}
