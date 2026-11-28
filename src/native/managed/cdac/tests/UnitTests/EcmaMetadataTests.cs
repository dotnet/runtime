// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class EcmaMetadataTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    public void GetMetadata_RecoversMissingMetadata(int locator, bool readable)
    {
        using MetadataFixture fixture = new(locator, readable);

        MetadataReader reader = Assert.IsType<MetadataReader>(fixture.Contract.GetMetadata(fixture.Module));
        MethodDefinition method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
        Assert.Equal("VarArgMethod", reader.GetString(method.Name));
        Assert.Equal(SignatureCallingConvention.VarArgs, reader.GetBlobReader(method.Signature).ReadSignatureHeader().CallingConvention);
        Assert.Same(reader, fixture.Contract.GetMetadata(fixture.Module));

        Assert.Equal(readable ? 0 : 1, fixture.CLRCalls);
        Assert.Equal(readable || locator == 0 ? 0 : 1, fixture.IXCLRCalls);
        Assert.Equal(!readable && locator == 2 ? 2 : 0, fixture.PathCalls);
    }

    [Fact]
    public void GetMetadata_DoesNotReplaceUpdatedMetadata()
    {
        using MetadataFixture fixture = new(locator: 0, readable: false, generation: 1);

        Assert.Throws<VirtualReadException>(() => fixture.Contract.GetMetadata(fixture.Module));
        Assert.Equal(0, fixture.CLRCalls);
        Assert.Equal(0, fixture.IXCLRCalls);
        Assert.Equal(0, fixture.PathCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-2)]
    public void GetMetadata_NoLocatorReturnsNull(int locator)
    {
        using MetadataFixture fixture = new(locator, readable: false);
        Assert.Null(fixture.Contract.GetMetadata(fixture.Module));
    }

    [Fact]
    public void CorDebugMetadataLocator_RejectsInsufficientBuffer()
    {
        using MetadataFixture fixture = new(locator: 2, readable: false);
        int metadataSize = checked((int)fixture.Contract.GetReadOnlyMetadataAddress(fixture.Module).Size);
        byte[] buffer = new byte[metadataSize - 1];
        ContractDescriptorTarget.GetMetadataDelegate callback =
            Assert.IsType<ContractDescriptorTarget.GetMetadataDelegate>(EntrypointHelpers.CreateMetadataCallback(fixture));

        int result = callback("TestModule.dll", 0, 0, 0, buffer, out uint dataSize);
        Assert.Equal(CorDbgHResults.ERROR_INSUFFICIENT_BUFFER, result);
        Assert.Equal((uint)metadataSize, dataSize);
    }

    [Fact]
    public void GetMetadata_DoesNotFallbackForMalformedMetadata()
    {
        using MetadataFixture fixture = new(locator: 0, readable: true, corruptMetadata: true);
        Assert.Throws<BadImageFormatException>(() => fixture.Contract.GetMetadata(fixture.Module));
        Assert.Equal(0, fixture.CLRCalls);
        Assert.Equal(0, fixture.IXCLRCalls);
        Assert.Equal(0, fixture.PathCalls);
    }

    private sealed unsafe class MetadataFixture : IDisposable, ICLRMetadataLocator, IXCLRDataTarget3, ICorDebugMetaDataLocator
    {
        private const ulong ImageBase = 0x10000;
        private readonly byte[] _image;
        private readonly byte[] _metadata;
        private readonly string _filePath;
        private readonly int _locator;

        public IEcmaMetadata Contract { get; }
        public ModuleHandle Module { get; } = new(new TargetPointer(0x1000));
        public int CLRCalls { get; private set; }
        public int IXCLRCalls { get; private set; }
        public int PathCalls { get; private set; }

        public MetadataFixture(int locator, bool readable, uint generation = 0, bool corruptMetadata = false)
        {
            _locator = locator;
            MetadataBuilder metadata = new();
            metadata.AddModule(0, metadata.GetOrAddString("TestModule"), default, default, default);
            metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
                default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            BlobHandle signature = metadata.GetOrAddBlob(new byte[] { 0x05, 0x00, 0x01 });
            metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
                metadata.GetOrAddString("VarArgMethod"), signature, 0, MetadataTokens.ParameterHandle(1));
            BlobBuilder image = new();
            new ManagedPEBuilder(new PEHeaderBuilder(), new MetadataRootBuilder(metadata), new BlobBuilder()).Serialize(image);
            _image = image.ToArray();

            using PEReader pe = new(new MemoryStream(_image));
            _metadata = pe.GetMetadata().GetContent().AsSpan().ToArray();
            int metadataOffset = pe.PEHeaders.MetadataStartOffset;
            if (corruptMetadata)
                _image[metadataOffset] = 0;
            Mock<ILoader> loader = new(MockBehavior.Strict);
            TargetPointer imageBase = new(ImageBase);
            uint size = (uint)_image.Length;
            uint flags = 0;
            uint timestamp = unchecked((uint)pe.PEHeaders.CoffHeader.TimeDateStamp);
            uint imageSize = (uint)pe.PEHeaders.PEHeader!.SizeOfImage;
            loader.Setup(l => l.TryGetLoadedImageContents(Module, out imageBase, out size, out flags)).Returns(true);
            loader.Setup(l => l.GetFileHeadersInfo(Module, out timestamp, out imageSize)).Returns(true);
            loader.Setup(l => l.GetPath(Module)).Returns("TestModule.dll");
            loader.Setup(l => l.GetPEAssembly(Module)).Throws<VirtualReadException>();

            ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact("""
                {
                  "version": 2,
                  "contracts": { "EcmaMetadata": "c1", "Loader": "c1" },
                  "types": {
                    "Module": { "DynamicMetadata": [0, "pointer"], "MetadataGeneration": [8, "uint32"] }
                  }
                }
                """u8)!;
            ContractDescriptorTarget target = ContractDescriptorTarget.Create(descriptor, [],
                (address, buffer) =>
                {
                    if (address >= Module.Address.Value && address + (ulong)buffer.Length <= Module.Address.Value + 16)
                    {
                        buffer.Clear();
                        if (address == Module.Address.Value + 8)
                            System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(buffer, generation);
                        return 0;
                    }
                    if (address >= ImageBase
                        && address + (ulong)buffer.Length <= ImageBase + (ulong)(readable ? _image.Length : metadataOffset))
                    {
                        _image.AsSpan((int)(address - ImageBase), buffer.Length).CopyTo(buffer);
                        return 0;
                    }
                    return unchecked((int)0x80004005);
                },
                (_, _) => throw new NotImplementedException(),
                (_, _, _) => throw new NotImplementedException(),
                (_, _) => throw new NotImplementedException(),
                (ulong _, out ulong address) => throw new NotImplementedException(),
                isLittleEndian: true, pointerSize: 8,
                contractRegistrations: [registry =>
                {
                    CoreCLRContracts.Register(registry);
                    registry.Register<ILoader>("c1", _ => loader.Object);
                }],
                getMetadata: locator == -2 ? null : EntrypointHelpers.CreateMetadataCallback(this));
            Contract = target.Contracts.EcmaMetadata;
            _filePath = Path.GetTempFileName();
            File.WriteAllBytes(_filePath, _image);
        }

        public int GetMetadata(char* path, uint timestamp, uint imageSize, Guid* mvid, uint rva, uint flags,
            uint bufferSize, byte* buffer, uint* dataSize)
        {
            CLRCalls++;
            return GetMetadata(0, new string(path), new Span<byte>(buffer, checked((int)bufferSize)), out *dataSize);
        }

        public int GetMetaData(char* path, uint timestamp, uint imageSize, Guid* mvid, uint rva, uint flags,
            uint bufferSize, byte* buffer, uint* dataSize)
        {
            IXCLRCalls++;
            return GetMetadata(1, new string(path), new Span<byte>(buffer, checked((int)bufferSize)), out *dataSize);
        }

        private int GetMetadata(int locator, string path, Span<byte> buffer, out uint dataSize)
        {
            Assert.Equal("TestModule.dll", path);
            dataSize = 0;
            if (_locator != locator)
                return unchecked((int)0x80004002);

            dataSize = (uint)_metadata.Length;
            if (buffer.Length < dataSize)
                return unchecked((int)0x8007007A);

            _metadata.CopyTo(buffer);
            return 0;
        }

        public int GetMetaData(char* path, uint timestamp, uint imageSize, uint bufferSize, uint* pathLength, char* buffer)
        {
            PathCalls++;
            Assert.Equal("TestModule.dll", new string(path));
            *pathLength = 0;
            if (_locator != 2)
                return unchecked((int)0x80004002);

            *pathLength = 512;
            if (bufferSize < *pathLength)
                return unchecked((int)0x8007007A);

            _filePath.AsSpan().CopyTo(new Span<char>(buffer, checked((int)bufferSize)));
            buffer[_filePath.Length] = '\0';
            *pathLength = (uint)_filePath.Length;
            return 0;
        }

        public int GetMachineType(uint* machineType) => throw new NotImplementedException();
        public int GetPointerSize(uint* pointerSize) => throw new NotImplementedException();
        public int GetImageBase(string imagePath, ulong* baseAddress) => throw new NotImplementedException();
        public int ReadVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesRead) => throw new NotImplementedException();
        public int WriteVirtual(ulong address, byte* buffer, uint bytesRequested, uint* bytesWritten) => throw new NotImplementedException();
        public int GetTLSValue(uint threadID, uint index, ulong* value) => throw new NotImplementedException();
        public int SetTLSValue(uint threadID, uint index, ulong value) => throw new NotImplementedException();
        public int GetCurrentThreadID(uint* threadID) => throw new NotImplementedException();
        public int GetThreadContext(uint threadID, uint contextFlags, uint contextSize, byte* context) => throw new NotImplementedException();
        public int SetThreadContext(uint threadID, uint contextSize, byte* context) => throw new NotImplementedException();
        public int Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer) => throw new NotImplementedException();
        public int AllocVirtual(ClrDataAddress addr, uint size, uint typeFlags, uint protectFlags, ClrDataAddress* virt) => throw new NotImplementedException();
        public int FreeVirtual(ClrDataAddress addr, uint size, uint typeFlags) => throw new NotImplementedException();

        public void Dispose() => File.Delete(_filePath);
    }
}
