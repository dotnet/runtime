// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public unsafe class ClrDataModuleTests
{
    private static readonly MockTarget.Architecture s_arch = new() { IsLittleEndian = true, Is64Bit = true };
    private static readonly TargetPointer s_modulePointer = new(0x1000);
    private static readonly ModuleHandle s_moduleHandle = new(new TargetPointer(0x2000));

    [Fact]
    public void GetVersionId_ReturnsMetadataMvid()
    {
        Guid expected = Guid.NewGuid();
        using MetadataReaderProvider provider = CreateMetadata(expected);
        IXCLRDataModule module = CreateModule(provider.GetMetadataReader());

        Guid actual;
        Assert.Equal(HResults.S_OK, module.GetVersionId(&actual));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetVersionId_MissingMetadataReturnsEFail()
    {
        IXCLRDataModule module = CreateModule(metadata: null);

        Guid actual = default;
        Assert.Equal(HResults.E_FAIL, module.GetVersionId(&actual));
    }

    private static IXCLRDataModule CreateModule(MetadataReader? metadata)
    {
        var loader = new Mock<ILoader>();
        loader.Setup(l => l.GetModuleHandleFromModulePtr(s_modulePointer)).Returns(s_moduleHandle);
        var ecmaMetadata = new Mock<IEcmaMetadata>();
        ecmaMetadata.Setup(e => e.GetMetadata(s_moduleHandle)).Returns(metadata);

        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(s_arch)
            .UseReader((ulong _, Span<byte> _) => -1)
            .AddMockContract(loader)
            .AddMockContract(ecmaMetadata)
            .Build();
        return new ClrDataModule(s_modulePointer, target, legacyImpl: null);
    }

    private static MetadataReaderProvider CreateMetadata(Guid mvid)
    {
        MetadataBuilder builder = new();
        builder.AddModule(0, builder.GetOrAddString("TestModule"), builder.GetOrAddGuid(mvid), default, default);
        BlobBuilder blob = new();
        new MetadataRootBuilder(builder).Serialize(blob, 0, 0);
        return MetadataReaderProvider.FromMetadataImage(ImmutableArray.Create(blob.ToArray()));
    }
}
