// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Legacy;
using Moq;
using Xunit;
using ModuleHandle = Microsoft.Diagnostics.DataContractReader.Contracts.ModuleHandle;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public partial class MetadataMemoryTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    public void MissingMetadataIsReadableThroughTarget(int locator, bool mapped)
    {
        using MetadataFixture fixture = new(locator, mapped);

        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(BinaryPrimitives.ReadUInt64LittleEndian(fixture.Metadata), fixture.Target.ReadPointer(fixture.MetadataAddress).Value);
        Assert.Equal("VarArgMethod", fixture.Target.ReadUtf8String(fixture.MethodNameAddress));
        byte[] buffer = new byte[fixture.Metadata.Length];
        fixture.Target.ReadBuffer(fixture.MetadataAddress, buffer);
        Assert.Equal(fixture.Metadata, buffer);

        MetadataReader reader = Assert.IsType<MetadataReader>(fixture.Target.Contracts.EcmaMetadata.GetMetadata(fixture.Module));
        MethodDefinition method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
        Assert.Equal("VarArgMethod", reader.GetString(method.Name));
        Assert.Equal(SignatureCallingConvention.VarArgs, reader.GetBlobReader(method.Signature).ReadSignatureHeader().CallingConvention);
        Assert.Equal(1, fixture.Enumerations);
        Assert.Equal(1, fixture.CLRCalls);
        Assert.Equal(locator == 0 ? 0 : 1, fixture.IXCLRCalls);
        Assert.Equal(locator == 2 ? 2 : 0, fixture.PathCalls);
    }

    [Fact]
    public void ReadableTargetMemoryTakesPrecedence()
    {
        using MetadataFixture fixture = new(readable: true);
        fixture.Metadata[0] = 0;

        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(0, fixture.Enumerations);
        Assert.Equal(0, fixture.CLRCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MetadataLocatorsWorkThroughNativeCom(int locator)
    {
        using MetadataFixture fixture = new(locator, nativeLocator: true);
        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.CLRCalls);
        Assert.Equal(locator == 0 ? 0 : 1, fixture.IXCLRCalls);
        Assert.Equal(locator == 2 ? 2 : 0, fixture.PathCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(64)]
    [InlineData(127)]
    public void ImageRangeLookupFindsTheOwningModule(int selectedImage)
    {
        using MetadataFixture fixture = new(imageCount: 128, selectedImage: selectedImage);
        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.Enumerations);
        Assert.Equal(1, fixture.CLRCalls);
        fixture.Loader.Verify(l => l.TryGetLoadedImageContents(It.IsAny<ModuleHandle>(), out It.Ref<TargetPointer>.IsAny,
            out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny), Times.Exactly(129));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void MetadataRecoverySupportsTargetArchitectures(bool is64Bit, bool isLittleEndian)
    {
        using MetadataFixture fixture = new(is64Bit: is64Bit, isLittleEndian: isLittleEndian);
        Assert.Equal(0x424a5342u, fixture.Target.ReadLittleEndian<uint>(fixture.MetadataAddress));
        Assert.Equal("VarArgMethod", fixture.Target.ReadUtf8String(fixture.MethodNameAddress));
        Assert.Equal(1, fixture.CLRCalls);
    }

    [Fact]
    public void UnreadablePathUsesTheModuleFileName()
    {
        using MetadataFixture fixture = new();
        fixture.Loader.Setup(l => l.GetPath(fixture.Module)).Throws<VirtualReadException>();
        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.CLRCalls);
    }

    [Fact]
    public void MissingOtherImageInformationDoesNotPreventRecovery()
    {
        using MetadataFixture fixture = new(imageCount: 128);
        fixture.Loader.Setup(l => l.TryGetLoadedImageContents(new ModuleHandle(new TargetPointer(0x1800)),
            out It.Ref<TargetPointer>.IsAny, out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny)).Throws<VirtualReadException>();

        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.CLRCalls);
        Assert.Equal(1, fixture.Enumerations);
    }

    [Fact]
    public void CorruptImageHeadersPreserveTheReadFailure()
    {
        using MetadataFixture fixture = new();
        fixture.Image.AsSpan(0, 4).Clear();

        Assert.Throws<VirtualReadException>(() => fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(0, fixture.CLRCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void ReadsCrossingMetadataBoundariesFail(int boundary)
    {
        using MetadataFixture fixture = new();
        ulong address = boundary switch
        {
            -1 => fixture.MetadataAddress - 1,
            0 => fixture.MetadataAddress + (ulong)fixture.Metadata.Length - 1,
            _ => ulong.MaxValue,
        };

        Assert.Throws<VirtualReadException>(() => fixture.Target.ReadBuffer(address, new byte[2]));
        Assert.Equal(0, fixture.CLRCalls);
        Assert.Equal(0, fixture.IXCLRCalls);
        Assert.Equal(0, fixture.PathCalls);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void DifferentMetadataSizeIsNotMapped(int sizeDifference)
    {
        using MetadataFixture fixture = new();
        fixture.SizeDifference = sizeDifference;

        Assert.Throws<VirtualReadException>(() => fixture.Target.Read<uint>(fixture.MetadataAddress));
        fixture.SizeDifference = 0;
        Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(2, fixture.CLRCalls);
        Assert.Equal(1, fixture.Enumerations);
    }

    [Fact]
    public void UnrelatedFailuresDoNotInvokeLocatorsOrRescanModules()
    {
        using MetadataFixture fixture = new();
        for (ulong address = 0x800000; address < 0x800010; address++)
            Assert.Throws<VirtualReadException>(() => fixture.Target.Read<byte>(address));

        Assert.Equal(1, fixture.Enumerations);
        Assert.Equal(0, fixture.CLRCalls);
        fixture.Loader.Verify(l => l.GetFileHeadersInfo(It.IsAny<ModuleHandle>(), out It.Ref<uint>.IsAny, out It.Ref<uint>.IsAny), Times.Never);
    }

    [Fact]
    public void FailedLocatorIsRetried()
    {
        using MetadataFixture fixture = new(locator: -1);
        Assert.Throws<VirtualReadException>(() => fixture.Target.Read<byte>(fixture.MetadataAddress));

        fixture.Locator = 0;
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(2, fixture.CLRCalls);
        Assert.Equal(1, fixture.Enumerations);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void UnavailableLocatedMetadataPreservesTheReadFailure(int failure)
    {
        using MetadataFixture fixture = new(locator: 2);
        switch (failure)
        {
            case 0:
                File.WriteAllBytes(fixture.LocatedFilePath, [1, 2, 3]);
                break;
            case 1:
                File.Delete(fixture.LocatedFilePath);
                break;
            case 2:
                fixture.LocatedFilePath = Path.GetDirectoryName(fixture.LocatedFilePath)!;
                break;
            case 3:
                byte[] image = File.ReadAllBytes(fixture.LocatedFilePath);
                using (PEReader pe = new(new MemoryStream(image)))
                    image.AsSpan(pe.PEHeaders.CorHeaderStartOffset + 8, 8).Clear();
                File.WriteAllBytes(fixture.LocatedFilePath, image);
                break;
        }

        Assert.Throws<VirtualReadException>(() => fixture.Target.Read<uint>(fixture.MetadataAddress));
        Assert.Equal(2, fixture.PathCalls);
    }

    [Fact]
    public void RecoveryDoesNotRecursivelyEnumerateModules()
    {
        using MetadataFixture fixture = new();
        fixture.FailEnumerationRead = true;
        Assert.Throws<VirtualReadException>(() => fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.Enumerations);
        Assert.Equal(0, fixture.CLRCalls);

        fixture.FailEnumerationRead = false;
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(2, fixture.Enumerations);
    }

    [Fact]
    public void FullFlushInvalidatesRecoveredBytesAndImageRanges()
    {
        using MetadataFixture fixture = new();
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));
        fixture.Metadata[0] = 0x43;
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));

        fixture.Target.Flush(FlushScope.All);

        Assert.Equal((byte)0x43, fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(2, fixture.CLRCalls);
        Assert.Equal(2, fixture.Enumerations);
    }

    [Fact]
    public void UpdatedMetadataDoesNotUseTheImageFallback()
    {
        using MetadataFixture fixture = new(generation: 1);
        Assert.Throws<VirtualReadException>(() => fixture.Target.Contracts.EcmaMetadata.GetMetadata(fixture.Module));
        Assert.Equal(0, fixture.CLRCalls);
    }

    [Fact]
    public void ReadReportingIncludesRecoveredReads()
    {
        using MetadataFixture fixture = new();
        bool reported = false;
        using (fixture.Target.RegisterReadCallback((address, size) =>
        {
            if (address == fixture.MetadataAddress && size == sizeof(uint))
                reported = true;
        }))
        {
            Assert.Equal(0x424a5342u, fixture.Target.Read<uint>(fixture.MetadataAddress));
        }

        Assert.True(reported);
        Assert.Equal((byte)0x42, fixture.Target.Read<byte>(fixture.MetadataAddress));
        Assert.Equal(1, fixture.CLRCalls);
    }

    [GeneratedComClass]
    private sealed unsafe partial class MetadataFixture : IDisposable, ICLRMetadataLocator, IXCLRDataTarget3, ICorDebugMetaDataLocator
    {
        private readonly ulong _imageBase;
        private readonly byte[] _image;
        private readonly string _filePath;
        private readonly uint _timestamp;
        private readonly uint _imageSize;
        private readonly ComObject? _locatorComObject;

        public ContractDescriptorTarget Target { get; }
        public Mock<ILoader> Loader { get; } = new(MockBehavior.Strict);
        public ModuleHandle Module { get; }
        public byte[] Metadata { get; }
        public byte[] Image => _image;
        public ulong MetadataAddress { get; }
        public ulong MethodNameAddress { get; }
        public int CLRCalls { get; private set; }
        public int IXCLRCalls { get; private set; }
        public int PathCalls { get; private set; }
        public int Enumerations { get; private set; }
        public int Locator { get; set; }
        public int SizeDifference { get; set; }
        public bool FailEnumerationRead { get; set; }
        public string LocatedFilePath { get; set; }

        public MetadataFixture(int locator = 0, bool mapped = false, bool readable = false, uint generation = 0,
            bool nativeLocator = false, int imageCount = 1, int selectedImage = 0, bool is64Bit = true, bool isLittleEndian = true)
        {
            Locator = locator;
            _imageBase = 0x10000 + (uint)selectedImage * 0x10000ul;
            Module = new(new TargetPointer(0x1000 + (uint)selectedImage * 0x20ul));
            MetadataBuilder metadata = new();
            metadata.AddModule(0, metadata.GetOrAddString("TestModule"), default, default, default);
            metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"),
                default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
            BlobHandle signature = metadata.GetOrAddBlob(new byte[] { 0x05, 0x00, 0x01 });
            metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static, MethodImplAttributes.IL,
                metadata.GetOrAddString("VarArgMethod"), signature, 0, MetadataTokens.ParameterHandle(1));
            BlobBuilder image = new();
            new ManagedPEBuilder(new PEHeaderBuilder(), new MetadataRootBuilder(metadata), new BlobBuilder()).Serialize(image);
            byte[] flatImage = image.ToArray();

            using PEReader pe = new(new MemoryStream(flatImage));
            Metadata = pe.GetMetadata().GetContent().AsSpan().ToArray();
            int metadataOffset = mapped ? pe.PEHeaders.CorHeader!.MetadataDirectory.RelativeVirtualAddress : pe.PEHeaders.MetadataStartOffset;
            MetadataAddress = _imageBase + (uint)metadataOffset;
            MetadataReader reader = pe.GetMetadataReader();
            MethodDefinition method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            MethodNameAddress = MetadataAddress + (uint)(reader.GetHeapMetadataOffset(HeapIndex.String) + MetadataTokens.GetHeapOffset(method.Name));
            _timestamp = unchecked((uint)pe.PEHeaders.CoffHeader.TimeDateStamp);
            _imageSize = (uint)pe.PEHeaders.PEHeader!.SizeOfImage;
            if (mapped)
            {
                _image = new byte[_imageSize];
                flatImage.AsSpan(0, pe.PEHeaders.PEHeader.SizeOfHeaders).CopyTo(_image);
                foreach (SectionHeader section in pe.PEHeaders.SectionHeaders)
                    flatImage.AsSpan(section.PointerToRawData, section.SizeOfRawData).CopyTo(_image.AsSpan(section.VirtualAddress));
            }
            else
            {
                _image = flatImage;
            }

            _filePath = Path.GetTempFileName();
            File.WriteAllBytes(_filePath, flatImage);
            LocatedFilePath = _filePath;
            uint size = (uint)_image.Length;
            uint flags = mapped ? 1u : 0u;
            uint timestamp = _timestamp;
            uint imageSize = _imageSize;
            Loader.Setup(l => l.GetAppDomain()).Returns(new TargetPointer(0x800));
            Loader.Setup(l => l.GetModuleHandles(It.IsAny<TargetPointer>(), It.IsAny<AssemblyIterationFlags>())).Returns(() =>
            {
                Enumerations++;
                if (Enumerations > 10)
                    throw new InvalidOperationException("Metadata recovery recursively enumerated modules.");
                if (FailEnumerationRead)
                    Target.Read<byte>(0x800000);
                ModuleHandle[] modules = new ModuleHandle[imageCount];
                for (int i = 0; i < modules.Length; i++)
                    modules[i] = new(new TargetPointer(0x1000 + (uint)(imageCount - 1 - i) * 0x20ul));
                return modules;
            });
            for (int i = 0; i < imageCount; i++)
            {
                ModuleHandle module = new(new TargetPointer(0x1000 + (uint)i * 0x20ul));
                TargetPointer imageBase = new(0x10000 + (uint)i * 0x10000ul);
                Loader.Setup(l => l.TryGetLoadedImageContents(module, out imageBase, out size, out flags)).Returns(true);
            }
            Loader.Setup(l => l.GetFileHeadersInfo(Module, out timestamp, out imageSize)).Returns(true);
            Loader.Setup(l => l.GetPath(Module)).Returns("TestModule.dll");
            Loader.Setup(l => l.GetFileName(Module)).Returns("TestModule.dll");
            Loader.Setup(l => l.GetPEAssembly(Module)).Throws<VirtualReadException>();
            Loader.Setup(l => l.Flush(It.IsAny<FlushScope>()));

            ContractDescriptorParser.ContractDescriptor descriptor = ContractDescriptorParser.ParseCompact("""
                {
                  "version": 2,
                  "contracts": { "EcmaMetadata": "c1", "Loader": "c1" },
                  "types": {
                    "Module": { "DynamicMetadata": [0, "pointer"], "MetadataGeneration": [8, "uint32"] }
                  }
                }
                """u8)!;
            Target = ContractDescriptorTarget.Create(descriptor, [],
                (address, buffer) =>
                {
                    if (address >= Module.Address.Value && address - Module.Address.Value < 16
                        && (ulong)buffer.Length <= 16 - (address - Module.Address.Value))
                    {
                        buffer.Clear();
                        if (address == Module.Address.Value + 8)
                        {
                            if (isLittleEndian)
                                BinaryPrimitives.WriteUInt32LittleEndian(buffer, generation);
                            else
                                BinaryPrimitives.WriteUInt32BigEndian(buffer, generation);
                        }
                        return HResults.S_OK;
                    }
                    if (address >= _imageBase && address - _imageBase < (ulong)_image.Length
                        && (ulong)buffer.Length <= (ulong)_image.Length - (address - _imageBase))
                    {
                        if (!readable && address < MetadataAddress + (ulong)Metadata.Length
                            && address + (ulong)buffer.Length > MetadataAddress)
                        {
                            return HResults.E_FAIL;
                        }
                        _image.AsSpan((int)(address - _imageBase), buffer.Length).CopyTo(buffer);
                        return HResults.S_OK;
                    }
                    return HResults.E_FAIL;
                },
                (_, _) => throw new NotImplementedException(),
                (_, _, _) => throw new NotImplementedException(),
                (_, _) => throw new NotImplementedException(),
                (ulong _, out ulong address) => throw new NotImplementedException(),
                isLittleEndian, pointerSize: is64Bit ? 8 : 4,
                contractRegistrations: [registry =>
                {
                    CoreCLRContracts.Register(registry);
                    registry.Register<ILoader>("c1", _ => Loader.Object);
                }]);
            if (nativeLocator)
            {
                void* ccw = ComInterfaceMarshaller<IXCLRDataTarget3>.ConvertToUnmanaged(this);
                try
                {
                    _locatorComObject = Assert.IsType<ComObject>(UniqueComInterfaceMarshaller<IXCLRDataTarget3>.ConvertToManaged(ccw));
                    EntrypointHelpers.EnableMetadataRecovery(Target, _locatorComObject);
                }
                finally
                {
                    ComInterfaceMarshaller<IXCLRDataTarget3>.Free(ccw);
                }
            }
            else
            {
                EntrypointHelpers.EnableMetadataRecovery(Target, this);
            }
        }

        public int GetMetadata(char* path, uint timestamp, uint imageSize, Guid* mvid, uint rva, uint flags,
            uint bufferSize, byte* buffer, uint* dataSize)
        {
            CLRCalls++;
            return GetMetadata(0, new string(path), timestamp, imageSize, rva, new Span<byte>(buffer, checked((int)bufferSize)), out *dataSize);
        }

        public int GetMetaData(char* path, uint timestamp, uint imageSize, Guid* mvid, uint rva, uint flags,
            uint bufferSize, byte* buffer, uint* dataSize)
        {
            IXCLRCalls++;
            return GetMetadata(1, new string(path), timestamp, imageSize, rva, new Span<byte>(buffer, checked((int)bufferSize)), out *dataSize);
        }

        private int GetMetadata(int locator, string path, uint timestamp, uint imageSize, uint rva, Span<byte> buffer, out uint dataSize)
        {
            Assert.Equal("TestModule.dll", path);
            Assert.Equal(_timestamp, timestamp);
            Assert.Equal(_imageSize, imageSize);
            Assert.Equal(0u, rva);
            dataSize = 0;
            if (Locator != locator)
                return HResults.E_NOTIMPL;

            dataSize = checked((uint)(Metadata.Length + SizeDifference));
            Metadata.CopyTo(buffer);
            return HResults.S_OK;
        }

        public int GetMetaData(char* path, uint timestamp, uint imageSize, uint bufferSize, uint* pathLength, char* buffer)
        {
            PathCalls++;
            Assert.Equal("TestModule.dll", new string(path));
            Assert.Equal(_timestamp, timestamp);
            Assert.Equal(_imageSize, imageSize);
            *pathLength = 0;
            if (Locator != 2)
                return HResults.E_NOTIMPL;

            *pathLength = 512;
            if (bufferSize <= *pathLength)
                return CorDbgHResults.ERROR_INSUFFICIENT_BUFFER;

            LocatedFilePath.AsSpan().CopyTo(new Span<char>(buffer, checked((int)bufferSize)));
            buffer[LocatedFilePath.Length] = '\0';
            *pathLength = (uint)LocatedFilePath.Length;
            return HResults.S_OK;
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

        public void Dispose()
        {
            _locatorComObject?.FinalRelease();
            File.Delete(_filePath);
        }
    }
}
