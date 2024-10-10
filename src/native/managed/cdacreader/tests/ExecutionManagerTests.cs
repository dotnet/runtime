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

        public void InsertRangeSection(ulong codeRangeStart, uint codeRangeSize, TargetPointer rangeSectionAddress)
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
            Builder.TargetTestHelpers.Write(rs.Slice(32, 4), (int)0x02); // Flags, 0x02 -= RangeSectionFlags.CodeHeap
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

        TargetPointer rangeSectionAddress = new (0x00dd_1000);
        TargetPointer rangeSectionFragmentAddress = new (0x00dd_2000);

        ExecutionManagerTestBuilder emBuilder = new (arch);

        emBuilder.InsertRangeSection(codeRangeStart, codeRangeSize, rangeSectionAddress);
        emBuilder.InsertAddressRange(codeRangeStart, codeRangeSize, rangeSectionFragmentAddress, rangeSectionAddress);

        emBuilder.AddNibbleMap(codeRangeStart, codeRangeSize, nibbleMapStart).AllocateCodeChunk(methodStart, methodSize);

        emBuilder.MarkCreated();

        ExecutionManagerTestTarget target = new(arch, emBuilder.Builder.GetReadContext().ReadFromTarget);

        // test

        var em = target.Contracts.ExecutionManager;
        Assert.NotNull(em);
        var eeInfo = em.GetEECodeInfoHandle(methodStart);
        Assert.NotNull(eeInfo);
    }
}
