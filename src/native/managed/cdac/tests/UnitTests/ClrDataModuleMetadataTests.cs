// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataModuleMetadataTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };
    private static readonly TargetPointer s_modulePointer = new(0x1000);
    private static readonly ModuleHandle s_moduleHandle = new(new TargetPointer(0x2000));

    [Fact]
    public void StartEnumMethodDefinitionsByName_MissingMetadataReturnsEFail()
    {
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(s_modulePointer)).Returns(s_moduleHandle);
        var ecmaMetadata = new Mock<IEcmaMetadata>();
        ecmaMetadata.Setup(e => e.GetMetadata(s_moduleHandle)).Returns((MetadataReader?)null);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(loader)
            .AddMockContract(ecmaMetadata)
            .Build();
        IXCLRDataModule module = new ClrDataModule(s_modulePointer, target, legacyImpl: null);

        ulong handle;
        fixed (char* name = "Foo")
        {
            Assert.Equal(HResults.E_FAIL, module.StartEnumMethodDefinitionsByName(name, 0, &handle));
        }
    }
}
