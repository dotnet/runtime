// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
using System;
using System.Reflection;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class PrecodeStubsTests
{
    // high level outline of a precode machine descriptor
    public class PrecodeTestDescriptor {
        public string Name { get; }
        public required MockTarget.Architecture Arch { get; init; }
        public bool IsThumb { get; init; }
        public required int ReadWidthOfPrecodeType { get; init; }
        public required int OffsetOfPrecodeType { get; init; }
        public required int ShiftOfPrecodeType { get; init; }
        // #if defined(TARGET_ARM64) && defined(TARGET_UNIX)
        //    return max(16*1024u, GetOsPageSize());
        // #elif defined(TARGET_ARM)
        //    return 4096; // ARM is special as the 32bit instruction set does not easily permit a 16KB offset
        // #else
        //     return 16*1024;
        // #endif
        public required uint StubCodePageSize { get; init; }

        // #if defined(TARGET_AMD64)
        //     static const BYTE Type = 0x4C;
        //     static const SIZE_T CodeSize = 24;
        // #elif defined(TARGET_X86)
        //     static const BYTE Type = 0xA1;
        //     static const SIZE_T CodeSize = 24;
        // #elif defined(TARGET_ARM64)
        //     static const int Type = 0x4A;
        //     static const SIZE_T CodeSize = 24;
        // #elif defined(TARGET_ARM)
        //     static const int Type = 0xFF;
        //     static const SIZE_T CodeSize = 12;
        // #elif defined(TARGET_LOONGARCH64)
        //     static const int Type = 0x4;
        //     static const SIZE_T CodeSize = 24;
        // #elif defined(TARGET_RISCV64)
        //     static const int Type = 0x17;
        //     static const SIZE_T CodeSize = 24;
        // #endif // TARGET_AMD64
        public required byte StubPrecode { get; init; }
        public required int StubPrecodeSize { get; init; }
        public PrecodeTestDescriptor(string name) {
            Name = name;
        }

        internal void WritePrecodeType(int precodeType,TargetTestHelpers targetTestHelpers, Span<byte> dest)
        {
            if (ReadWidthOfPrecodeType == 1)
            {
                byte value = (byte)(((byte)precodeType & 0xff) << ShiftOfPrecodeType);
                // TODO: fill in the other bits with something
                targetTestHelpers.Write(dest.Slice(OffsetOfPrecodeType, 1), value);
            }
            else if (ReadWidthOfPrecodeType == 2)
            {
                ushort value = (ushort)(((ushort)precodeType & 0xff) << ShiftOfPrecodeType);
                // TODO: fill in the other bits with something
                targetTestHelpers.Write(dest.Slice(OffsetOfPrecodeType, 2), value);
            }
            else
            {
                throw new InvalidOperationException("Don't know how to write a precode type of width {ReadWidthOfPrecodeType}");
            }
        }
    }

    internal static PrecodeTestDescriptor X64TestDescriptor = new PrecodeTestDescriptor("X64") {
        Arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true },
        ReadWidthOfPrecodeType = 1,
        ShiftOfPrecodeType = 0,
        OffsetOfPrecodeType = 0,
        StubCodePageSize = 0x4000u, // 16KiB
        StubPrecode = 0x4c,
        StubPrecodeSize = 24,
    };
    internal static PrecodeTestDescriptor Arm64TestDescriptor = new PrecodeTestDescriptor("Arm64") {
        Arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true },
        ReadWidthOfPrecodeType = 1,
        ShiftOfPrecodeType = 0,
        OffsetOfPrecodeType = 0,
        StubCodePageSize = 0x4000u, // 16KiB
        StubPrecode = 0x4a,
        StubPrecodeSize = 24,

    };
    internal static PrecodeTestDescriptor LoongArch64TestDescriptor = new PrecodeTestDescriptor("LoongArch64") {
        Arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = true },
        ReadWidthOfPrecodeType = 2,
        ShiftOfPrecodeType = 5,
        OffsetOfPrecodeType = 0,
        StubCodePageSize = 0x4000u, // 16KiB
        StubPrecode = 0x4,
        StubPrecodeSize = 24,
    };

    internal static PrecodeTestDescriptor Arm32Thumb = new PrecodeTestDescriptor("Arm32Thumb") {
        Arch = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false },
        IsThumb = true,
        ReadWidthOfPrecodeType = 1,
        ShiftOfPrecodeType = 0,
        OffsetOfPrecodeType = 7,
        StubCodePageSize = 0x1000u, // 4KiB
        StubPrecode = 0xff,
        StubPrecodeSize = 12,
    };

    public static IEnumerable<object[]> PrecodeTestDescriptorData()
    {
        var arch32le = new MockTarget.Architecture { IsLittleEndian = true, Is64Bit = false };
        var arch32be = new MockTarget.Architecture { IsLittleEndian = false, Is64Bit = false };
        var arch64be = new MockTarget.Architecture { IsLittleEndian = false, Is64Bit = true };

        yield return new object[] { X64TestDescriptor };
        yield return new object[] { Arm64TestDescriptor };
        yield return new object[] { LoongArch64TestDescriptor };
        yield return new object[] { Arm32Thumb };
        // FIXME: maybe make these a little more exotic
        yield return new object[] { new PrecodeTestDescriptor("Fake 32-bit LE") {
            Arch = arch32le,
            ReadWidthOfPrecodeType = 1,
            ShiftOfPrecodeType = 0,
            OffsetOfPrecodeType = 0,
            StubCodePageSize = 0x4000u, // 16KiB
            StubPrecode = 0xa1,
            StubPrecodeSize = 24,
        }};
        yield return new object[] { new PrecodeTestDescriptor("Fake 32-bit BE") {
            Arch = arch32be,
            ReadWidthOfPrecodeType = 1,
            ShiftOfPrecodeType = 0,
            OffsetOfPrecodeType = 0,
            StubCodePageSize = 0x4000u, // 16KiB
            StubPrecode = 0xa1,
            StubPrecodeSize = 24,
        }};
        yield return new object[] { new PrecodeTestDescriptor("Fake 64-bit BE") {
            Arch = arch64be,
            ReadWidthOfPrecodeType = 1,
            ShiftOfPrecodeType = 0,
            OffsetOfPrecodeType = 0,
            StubCodePageSize = 0x4000u, // 16KiB
            StubPrecode = 0xa1,
            StubPrecodeSize = 24,
        }};
    }

    internal struct AllocationRange
    {
        public ulong PrecodeDescriptorStart;
        public ulong PrecodeDescriptorEnd;
        // This address range will behave a little unusually.
        // For testing, we will use a bump allocator to allocate the stub data, and then
        // subtract the code page size to get the code address for the stub and explicitly allocate
        // the code fragment
        public ulong StubDataPageStart;
        public ulong StubDataPageEnd;
    }

    internal readonly static AllocationRange DefaultAllocationRange = new AllocationRange {
        PrecodeDescriptorStart = 0x3333_1000u,
        PrecodeDescriptorEnd = 0x3333_2000u,
        StubDataPageStart = 0x11ee_0000u,
        StubDataPageEnd = 0x11ee_4000u,
    };

    internal class PrecodeBuilder {
        public readonly MockMemorySpace.Builder Builder;
        public readonly MockMemorySpace.BumpAllocator PrecodeAllocator;
        public readonly MockMemorySpace.BumpAllocator StubDataPageAllocator;
        public readonly Dictionary<DataType, Target.TypeInfo>? TypeInfoCache;

        public TargetPointer MachineDescriptorAddress;
        public CodePointerFlags CodePointerFlags {get; private set;}
        public PrecodeBuilder(MockTarget.Architecture arch) : this(DefaultAllocationRange, new MockMemorySpace.Builder(new TargetTestHelpers(arch))) {
        }
        public PrecodeBuilder(AllocationRange allocationRange, MockMemorySpace.Builder builder, Dictionary<DataType, Target.TypeInfo>? typeInfoCache = null) {
            Builder = builder;
            PrecodeAllocator = new MockMemorySpace.BumpAllocator(allocationRange.PrecodeDescriptorStart, allocationRange.PrecodeDescriptorEnd);
            StubDataPageAllocator = new MockMemorySpace.BumpAllocator(allocationRange.StubDataPageStart, allocationRange.StubDataPageEnd);
            TypeInfoCache = typeInfoCache ?? CreateTypeInfoCache(Builder.TargetTestHelpers);
        }

        public Dictionary<DataType, Target.TypeInfo> CreateTypeInfoCache(TargetTestHelpers targetTestHelpers) {
            var typeInfo = new Dictionary<DataType, Target.TypeInfo>();
            AddToTypeInfoCache(typeInfo, targetTestHelpers);
            return typeInfo;
        }

        public void AddToTypeInfoCache(Dictionary<DataType, Target.TypeInfo> typeInfoCache, TargetTestHelpers targetTestHelpers) {
            var layout = targetTestHelpers.LayoutFields([
                (nameof(Data.PrecodeMachineDescriptor.StubCodePageSize), DataType.uint32),
                (nameof(Data.PrecodeMachineDescriptor.OffsetOfPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.ReadWidthOfPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.ShiftOfPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.InvalidPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.StubPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.PInvokeImportPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.FixupPrecodeType), DataType.uint8),
                (nameof(Data.PrecodeMachineDescriptor.ThisPointerRetBufPrecodeType), DataType.uint8),
            ]);
            typeInfoCache[DataType.PrecodeMachineDescriptor] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
            };
            layout = targetTestHelpers.LayoutFields([
                (nameof(Data.StubPrecodeData.Type), DataType.uint8),
                (nameof(Data.StubPrecodeData.MethodDesc), DataType.pointer),
            ]);
            typeInfoCache[DataType.StubPrecodeData] = new Target.TypeInfo() {
                Fields = layout.Fields,
                Size = layout.Stride,
            };
        }

        private void SetCodePointerFlags(PrecodeTestDescriptor test)
        {
            CodePointerFlags = default;
            if (test.IsThumb) {
                CodePointerFlags |= CodePointerFlags.HasArm32ThumbBit;
            }
        }

        public void AddCDacMetadata(PrecodeTestDescriptor descriptor) {
            SetCodePointerFlags(descriptor);
            var typeInfo = TypeInfoCache[DataType.PrecodeMachineDescriptor];
            var fragment = PrecodeAllocator.Allocate((ulong)typeInfo.Size, $"{descriptor.Name} Precode Machine Descriptor");
            Builder.AddHeapFragment(fragment);
            MachineDescriptorAddress = fragment.Address;
            Span<byte> desc = Builder.BorrowAddressRange(fragment.Address, (int)typeInfo.Size);
            Builder.TargetTestHelpers.Write(desc.Slice(typeInfo.Fields[nameof(Data.PrecodeMachineDescriptor.ReadWidthOfPrecodeType)].Offset, sizeof(byte)), (byte)descriptor.ReadWidthOfPrecodeType);
            Builder.TargetTestHelpers.Write(desc.Slice(typeInfo.Fields[nameof(Data.PrecodeMachineDescriptor.OffsetOfPrecodeType)].Offset, sizeof(byte)), (byte)descriptor.OffsetOfPrecodeType);
            Builder.TargetTestHelpers.Write(desc.Slice(typeInfo.Fields[nameof(Data.PrecodeMachineDescriptor.ShiftOfPrecodeType)].Offset, sizeof(byte)), (byte)descriptor.ShiftOfPrecodeType);
            Builder.TargetTestHelpers.Write(desc.Slice(typeInfo.Fields[nameof(Data.PrecodeMachineDescriptor.StubCodePageSize)].Offset, sizeof(uint)), descriptor.StubCodePageSize);
            Builder.TargetTestHelpers.Write(desc.Slice(typeInfo.Fields[nameof(Data.PrecodeMachineDescriptor.StubPrecodeType)].Offset, sizeof(byte)), descriptor.StubPrecode);
            // FIXME: set the other fields
        }

        public TargetCodePointer AddStubPrecodeEntry(string name, PrecodeTestDescriptor test, TargetPointer methodDesc) {
            // TODO[cdac]: allow writing other kinds of stub precode subtypes
            ulong stubCodeSize = (ulong)test.StubPrecodeSize;
            var stubDataTypeInfo  = TypeInfoCache[DataType.StubPrecodeData];
            MockMemorySpace.HeapFragment stubDataFragment = StubDataPageAllocator.Allocate((ulong)stubDataTypeInfo.Size, $"Stub data for {name} on {test.Name}");
            Builder.AddHeapFragment(stubDataFragment);
            // allocate the code one page before the stub data
            ulong stubCodeStart = stubDataFragment.Address - test.StubCodePageSize;
            MockMemorySpace.HeapFragment stubCodeFragment = new MockMemorySpace.HeapFragment {
                Address = stubCodeStart,
                Data = new byte[stubCodeSize],
                Name = $"Stub code for {name} on {test.Name} with data at 0x{stubDataFragment.Address:x}",
            };
            test.WritePrecodeType(test.StubPrecode, Builder.TargetTestHelpers, stubCodeFragment.Data);
            Builder.AddHeapFragment(stubCodeFragment);

            Span<byte> stubData = Builder.BorrowAddressRange(stubDataFragment.Address, (int)stubDataTypeInfo.Size);
            Builder.TargetTestHelpers.Write(stubData.Slice(stubDataTypeInfo.Fields[nameof(Data.StubPrecodeData.Type)].Offset, sizeof(byte)), test.StubPrecode);
            Builder.TargetTestHelpers.WritePointer(stubData.Slice(stubDataTypeInfo.Fields[nameof(Data.StubPrecodeData.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDesc);
            TargetCodePointer address = stubCodeFragment.Address;
            if (test.IsThumb) {
                address = new TargetCodePointer(address.Value | 1);
            }
            return address;
        }

        public void MarkCreated() => Builder.MarkCreated();
    }

    internal class PrecodeTestTarget : TestPlaceholderTarget
    {
        private class TestPlatformMetadata : IPlatformMetadata
        {
            private readonly CodePointerFlags _codePointerFlags;
            private readonly TargetPointer _precodeMachineDescriptorAddress;
            public TestPlatformMetadata(CodePointerFlags codePointerFlags, TargetPointer precodeMachineDescriptorAddress) {
                _codePointerFlags = codePointerFlags;
                _precodeMachineDescriptorAddress = precodeMachineDescriptorAddress;
            }
            TargetPointer IPlatformMetadata.GetPrecodeMachineDescriptor() => _precodeMachineDescriptorAddress;
            CodePointerFlags IPlatformMetadata.GetCodePointerFlags() => _codePointerFlags;
        }
        internal readonly TargetPointer PrecodeMachineDescriptorAddress;
        // hack for this test put the precode machine descriptor at the same address as the PlatformMetadata
        internal TargetPointer PlatformMetadataAddress => PrecodeMachineDescriptorAddress;
        public static PrecodeTestTarget FromBuilder(PrecodeBuilder precodeBuilder)
        {
            precodeBuilder.MarkCreated();
            var arch = precodeBuilder.Builder.TargetTestHelpers.Arch;
            ReadFromTargetDelegate reader = precodeBuilder.Builder.GetReadContext().ReadFromTarget;
            var typeInfo = precodeBuilder.TypeInfoCache;
            return new PrecodeTestTarget(arch, reader, precodeBuilder.CodePointerFlags, precodeBuilder.MachineDescriptorAddress, typeInfo);
        }
        public PrecodeTestTarget(MockTarget.Architecture arch, ReadFromTargetDelegate reader, CodePointerFlags codePointerFlags, TargetPointer platformMetadataAddress, Dictionary<DataType, TypeInfo> typeInfoCache) : base(arch) {
            PrecodeMachineDescriptorAddress = platformMetadataAddress;
            SetTypeInfoCache(typeInfoCache);
            SetDataCache(new DefaultDataCache(this));
            SetDataReader(reader);
            IContractFactory<IPrecodeStubs> precodeFactory = new PrecodeStubsFactory();
            SetContracts(new TestRegistry() {
                CDacMetadataContract = new (() => new TestPlatformMetadata(codePointerFlags, PrecodeMachineDescriptorAddress)),
                PrecodeStubsContract = new (() => precodeFactory.CreateContract(this, 1)),

            });
        }

        public override TargetPointer ReadGlobalPointer (string name)
        {
            if (name == Constants.Globals.PlatformMetadata) {
                return PlatformMetadataAddress;
            }
            return base.ReadGlobalPointer(name);
        }
    }

    [Theory]
    [MemberData(nameof(PrecodeTestDescriptorData))]
    public void TestPrecodeStubPrecodeExpectedMethodDesc(PrecodeTestDescriptor test)
    {
        var builder = new PrecodeBuilder(test.Arch);
        builder.AddCDacMetadata(test);

        TargetPointer expectedMethodDesc = new TargetPointer(0xeeee_eee0u); // arbitrary
        TargetCodePointer stub1 = builder.AddStubPrecodeEntry("Stub 1", test, expectedMethodDesc);

        var target = PrecodeTestTarget.FromBuilder(builder);
        Assert.NotNull(target);

        var precodeContract = target.Contracts.PrecodeStubs;

        Assert.NotNull(precodeContract);

        var actualMethodDesc = precodeContract.GetMethodDescFromStubAddress(stub1);
        Assert.Equal(expectedMethodDesc, actualMethodDesc);


    }
}
