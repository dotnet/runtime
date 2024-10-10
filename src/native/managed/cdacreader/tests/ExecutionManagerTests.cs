// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System.Collections.Generic;
using System;
namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class ExecutionManagerTests
{
    private const ulong ExecutionManagerCodeRangeMapAddress = 0x000a_fff0;

    const int RangeSectionFragmentSize = 0x20; // must be big enough for the offsets of RangeSectionFragment size in ExecutionManagerTestTarget, below
    const int RangeSectionSize = 0x38; // must be big enough for the offsets of RangeSection size in ExecutionManagerTestTarget, below

    const int CodeHeapListNodeSize = 0x40; // must be big enough for the offsets of CodeHeapListNode size in ExecutionManagerTestTarget, below

    const int RealCodeHeaderSize = 0x08; // must be big enough for the offsets of RealCodeHeader size in ExecutionManagerTestTarget, below

    internal class ExecutionManagerTestBuilder
    {
        public MockMemorySpace.Builder Builder { get; }
        private readonly RangeSectionMapTests.Builder _rsmBuilder;

        public ExecutionManagerTestBuilder(MockTarget.Architecture arch) : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)))
        {}

        public ExecutionManagerTestBuilder(MockMemorySpace.Builder builder)
        {
            Builder = builder;
            _rsmBuilder = new RangeSectionMapTests.Builder(ExecutionManagerCodeRangeMapAddress, builder);
        }

        public NibbleMapTests.NibbleMapTestBuilder AddNibbleMap(ulong codeRangeStart, uint codeRangeSize, ulong nibbleMapStart)
        {
            NibbleMapTests.NibbleMapTestBuilder nibBuilder = new NibbleMapTests.NibbleMapTestBuilder(codeRangeStart, codeRangeSize, nibbleMapStart, Builder.TargetTestHelpers.Arch);
            Builder.AddHeapFragment(nibBuilder.NibbleMapFragment);
            return nibBuilder;
        }

        public void InsertRangeSection(ulong codeRangeStart, uint codeRangeSize, TargetPointer jitManagerAddress, TargetPointer rangeSectionAddress, TargetPointer codeHeapListNodeAddress)
        {
            MockMemorySpace.HeapFragment rangeSection = new MockMemorySpace.HeapFragment
            {
                Address = rangeSectionAddress,
                Data = new byte[RangeSectionSize],
                Name = "RangeSection"
            };
            Builder.AddHeapFragment(rangeSection);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rs = Builder.BorrowAddressRange(rangeSectionAddress, RangeSectionSize);
            /* RangeBegin */
            Builder.TargetTestHelpers.WritePointer(rs.Slice(0, pointerSize), codeRangeStart);
            /* RangeEndOpen */
            Builder.TargetTestHelpers.WritePointer(rs.Slice(8, pointerSize), codeRangeStart + codeRangeSize);
            /* Flags */
            Builder.TargetTestHelpers.Write(rs.Slice(32, sizeof(uint)), (uint)0x02); // Flags, 0x02 -= RangeSectionFlags.CodeHeap
            /* HeapList */
            Builder.TargetTestHelpers.WritePointer(rs.Slice(40, pointerSize), codeHeapListNodeAddress);
            /* JitManager */
            Builder.TargetTestHelpers.WritePointer(rs.Slice(24, pointerSize), jitManagerAddress);
            // FIXME: other fields
        }

        public void InsertAddressRange(ulong codeRangeStart, uint codeRangeSize, TargetPointer rangeSectionFragmentAddress, TargetPointer rangeSectionAddress)
        {
            // FIXME: this shouldn't really be called InsertAddressRange, but maybe InsertRangeSectionFragment?
            _rsmBuilder.InsertAddressRange(codeRangeStart, codeRangeSize, rangeSectionFragmentAddress);
            MockMemorySpace.HeapFragment rangeSectionFragment = new MockMemorySpace.HeapFragment
            {
                Address = rangeSectionFragmentAddress,
                Data = new byte[RangeSectionFragmentSize],
                Name = "RangeSectionFragment"
            };
            Builder.AddHeapFragment(rangeSectionFragment);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rsf = Builder.BorrowAddressRange(rangeSectionFragmentAddress, RangeSectionFragmentSize);
            /* RangeStart */
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(0, pointerSize), codeRangeStart);
            /* RangeEndOpen */
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(8, pointerSize), codeRangeStart + codeRangeSize);
            /* RangeSection */
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(16, pointerSize), rangeSectionAddress);
            /* Next = nullptr */
            // nothing
        }

        public void InsertCodeHeapListNode(ulong codeHeapListNodeAddress, TargetPointer next, TargetPointer startAddress, TargetPointer endAddress, TargetPointer mapBase, TargetPointer headerMap)
        {
            MockMemorySpace.HeapFragment codeHeapListNode = new MockMemorySpace.HeapFragment
            {
                Address = codeHeapListNodeAddress,
                Data = new byte[CodeHeapListNodeSize],
                Name = "CodeHeapListNode"
            };
            Builder.AddHeapFragment(codeHeapListNode);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> chln = Builder.BorrowAddressRange(codeHeapListNodeAddress, CodeHeapListNodeSize);
            /* Next */
            Builder.TargetTestHelpers.WritePointer(chln.Slice(0, pointerSize), next);
            /* StartAddress */
            Builder.TargetTestHelpers.WritePointer(chln.Slice(8, pointerSize), startAddress);
            /* EndAddress */
            Builder.TargetTestHelpers.WritePointer(chln.Slice(16, pointerSize), endAddress);
            /* MapBase */
            Builder.TargetTestHelpers.WritePointer(chln.Slice(24, pointerSize), mapBase);
            /* HeaderMap */
            Builder.TargetTestHelpers.WritePointer(chln.Slice(32, pointerSize), headerMap);
        }

        public void AllocateMethod(TargetCodePointer codeStart, int codeSize, TargetPointer codeHeaderAddress, TargetPointer methodDescAddress)
        {
            int codeHeaderOffset = Builder.TargetTestHelpers.PointerSize;

            TargetPointer methodFragmentStart = codeStart.AsTargetPointer - (ulong)codeHeaderOffset;
            MockMemorySpace.HeapFragment methodFragment = new MockMemorySpace.HeapFragment
            {
                Address = methodFragmentStart,
                Data = new byte[codeSize + codeHeaderOffset],
                Name = "Method Header & Code"
            };
            Builder.AddHeapFragment(methodFragment);
            Span<byte> mfPtr = Builder.BorrowAddressRange(methodFragmentStart, codeHeaderOffset);
            Builder.TargetTestHelpers.WritePointer(mfPtr.Slice(0, Builder.TargetTestHelpers.PointerSize), codeHeaderAddress);
            MockMemorySpace.HeapFragment codeHeaderFragment = new MockMemorySpace.HeapFragment
            {
                Address = codeHeaderAddress,
                Data = new byte[RealCodeHeaderSize],
                Name = "RealCodeHeader"
            };
            Builder.AddHeapFragment(codeHeaderFragment);
            Span<byte> chf = Builder.BorrowAddressRange(codeHeaderAddress, RealCodeHeaderSize);
            Builder.TargetTestHelpers.WritePointer(chf.Slice(0, Builder.TargetTestHelpers.PointerSize), methodDescAddress);
        }

        public void MarkCreated() => Builder.MarkCreated();

    }

    internal class ExecutionManagerTestTarget : TestPlaceholderTarget
    {
        public ExecutionManagerTestTarget(MockTarget.Architecture arch, ReadFromTargetDelegate dataReader) : base(arch)
        {
            SetDataReader(dataReader);
            typeInfoCache = new Dictionary<DataType, TypeInfo>() {
                [DataType.RangeSectionMap] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.RangeSectionMap.TopLevelData)] = new () {Offset = 0},
                    },
                },
                [DataType.RangeSectionFragment] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.RangeSectionFragment.RangeBegin)] = new () {Offset = 0},
                        [nameof(Data.RangeSectionFragment.RangeEndOpen)] = new () {Offset = 8},
                        [nameof(Data.RangeSectionFragment.RangeSection)] = new () {Offset = 16},
                        [nameof(Data.RangeSectionFragment.Next)] = new () {Offset = 24},
                    },
                },
                [DataType.RangeSection] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.RangeSection.RangeBegin)] = new () {Offset = 0},
                        [nameof(Data.RangeSection.RangeEndOpen)] = new () {Offset = 8},
                        [nameof(Data.RangeSection.NextForDelete)] = new () {Offset = 16},
                        [nameof(Data.RangeSection.JitManager)] = new () {Offset = 24},
                        [nameof(Data.RangeSection.Flags)] = new () {Offset = 32},
                        [nameof(Data.RangeSection.HeapList)] = new () {Offset = 40},
                        [nameof(Data.RangeSection.R2RModule)] = new () {Offset = 48},
                    },
                },
                [DataType.CodeHeapListNode] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.CodeHeapListNode.Next)] = new () {Offset = 0},
                        [nameof(Data.CodeHeapListNode.StartAddress)] = new () {Offset = 8},
                        [nameof(Data.CodeHeapListNode.EndAddress)] = new () {Offset = 16},
                        [nameof(Data.CodeHeapListNode.MapBase)] = new () {Offset = 24},
                        [nameof(Data.CodeHeapListNode.HeaderMap)] = new () {Offset = 32},
                    },
                },
                [DataType.RealCodeHeader] = new TypeInfo() {
                    Fields = new Dictionary<string, FieldInfo>() {
                        [nameof(Data.RealCodeHeader.MethodDesc)] = new () {Offset = 0},
                    },
                },
            };
            SetDataCache(new DefaultDataCache(this));
            IContractFactory<IExecutionManager> emfactory = new ExecutionManagerFactory();
            SetContracts(new TestRegistry() {
                ExecutionManagerContract = new (() => emfactory.CreateContract(this, 1)),
            });
        }

        public override TargetPointer ReadGlobalPointer(string global)
        {
            switch (global)
            {
            case Constants.Globals.ExecutionManagerCodeRangeMapAddress:
                return new TargetPointer(ExecutionManagerCodeRangeMapAddress);
            default:
                return base.ReadGlobalPointer(global);
            }
        }

        public override T ReadGlobal<T>(string name)
        {
            switch (name)
            {
            case Constants.Globals.StubCodeBlockLast:
                if (typeof(T) == typeof(byte))
                    return (T)(object)(byte)0x0Fu;
                break;
            default:
                break;
            }
            return base.ReadGlobal<T>(name);

        }

    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNull(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new (arch);
        MockMemorySpace.Builder builder = new (targetTestHelpers);
        RangeSectionMapTests.Builder rsmBuilder = new (ExecutionManagerCodeRangeMapAddress, builder);
        builder.MarkCreated();
        ExecutionManagerTestTarget target = new(arch, builder.GetReadContext().ReadFromTarget);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(TargetCodePointer.Null);
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullMissing(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new (arch);
        MockMemorySpace.Builder builder = new (targetTestHelpers);
        RangeSectionMapTests.Builder rsmBuilder = new (ExecutionManagerCodeRangeMapAddress, builder);
        builder.MarkCreated();
        ExecutionManagerTestTarget target = new(arch, builder.GetReadContext().ReadFromTarget);
        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(new TargetCodePointer(0x0a0a_0000));
        Assert.Null(eeInfo);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void LookupNonNullOneRangeOneMethod(MockTarget.Architecture arch)
    {
        const ulong codeRangeStart = 0x0a0a_0000u; // arbitrary
        const uint codeRangeSize = 0xc000u; // arbitrary
        TargetCodePointer methodStart = new (0x0a0a_0040); // arbitrary, inside [codeRangeStart,codeRangeStart+codeRangeSize]
        int methodSize = 0x100; // arbitrary
        ulong nibbleMapStart = 0x00ee_0000; // arbitrary

        TargetPointer jitManagerAddress = new (0x000b_ff00); // arbitrary

        TargetPointer methodCodeHeaderAddress = new (0x0033_4000);
        TargetPointer expectedMethodDescAddress = new TargetPointer(0x0101_aaa0);

        TargetPointer rangeSectionAddress = new (0x00dd_1000);
        TargetPointer rangeSectionFragmentAddress = new (0x00dd_2000);
        TargetPointer codeHeapListNodeAddress = new (0x00dd_3000);

        ExecutionManagerTestBuilder emBuilder = new (arch);

        emBuilder.InsertRangeSection(codeRangeStart, codeRangeSize, jitManagerAddress: jitManagerAddress, rangeSectionAddress: rangeSectionAddress, codeHeapListNodeAddress: codeHeapListNodeAddress);
        emBuilder.InsertCodeHeapListNode(codeHeapListNodeAddress, TargetPointer.Null, codeRangeStart, codeRangeStart + codeRangeSize, codeRangeStart, nibbleMapStart);
        emBuilder.InsertAddressRange(codeRangeStart, codeRangeSize, rangeSectionFragmentAddress, rangeSectionAddress);

        emBuilder.AddNibbleMap(codeRangeStart, codeRangeSize, nibbleMapStart).AllocateCodeChunk(methodStart, methodSize);

        emBuilder.AllocateMethod(methodStart, methodSize, methodCodeHeaderAddress, expectedMethodDescAddress);

        emBuilder.MarkCreated();

        ExecutionManagerTestTarget target = new(arch, emBuilder.Builder.GetReadContext().ReadFromTarget);

        // test

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(methodStart);
        Assert.NotNull(eeInfo);
        TargetPointer actualMethodDesc = em.GetMethodDesc(eeInfo.Value);
        Assert.Equal(expectedMethodDescAddress, actualMethodDesc);
    }
}
