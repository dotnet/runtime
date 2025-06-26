// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers;
using Microsoft.Diagnostics.DataContractReader.Tests.ExecutionManager;

using InteriorMapValue = Microsoft.Diagnostics.DataContractReader.ExecutionManagerHelpers.RangeSectionMap.InteriorMapValue;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    internal class ExecutionManager
    {
        public const ulong ExecutionManagerCodeRangeMapAddress = 0x000a_fff0;

        const int RealCodeHeaderSize = 0x20; // must be big enough for the offsets of RealCodeHeader size in ExecutionManagerTestTarget, below

        public struct AllocationRange
        {
            // elements of the range section map are allocated in this range
            public ulong RangeSectionMapStart;
            public ulong RangeSectionMapEnd;
            // nibble maps for various range section fragments are allocated in this range
            public ulong NibbleMapStart;
            public ulong NibbleMapEnd;
            // "RealCodeHeader" objects for jitted methods and the module, info, runtime functions
            // and hot/cold map for R2R are allocated in this range
            public ulong ExecutionManagerStart;
            public ulong ExecutionManagerEnd;
        }

        public static readonly AllocationRange DefaultAllocationRange = new AllocationRange
        {
            RangeSectionMapStart = 0x00dd_0000,
            RangeSectionMapEnd = 0x00de_0000,
            NibbleMapStart = 0x00ee_0000,
            NibbleMapEnd = 0x00ef_0000,
            ExecutionManagerStart = 0x0033_4000,
            ExecutionManagerEnd = 0x0033_5000,
        };
        internal class RangeSectionMapTestBuilder
        {
            const ulong DefaultTopLevelAddress = 0x0000_1000u; // arbitrary
            const int EntriesPerMapLevel = 256; // for now its fixed at 256, see codeman.h RangeSectionMap::entriesPerMapLevel
            const int BitsPerLevel = 8;

            private readonly TargetPointer _topLevelAddress;
            private readonly MockMemorySpace.Builder _builder;
            private readonly TargetTestHelpers _targetTestHelpers;
            private readonly int _levels;
            private readonly int _maxSetBit;
            private ulong _nextMapAddress;
            public RangeSectionMapTestBuilder(MockTarget.Architecture arch) : this(DefaultTopLevelAddress, new MockMemorySpace.Builder(new TargetTestHelpers(arch)))
            {
            }

            public RangeSectionMapTestBuilder(TargetPointer topLevelAddress, MockMemorySpace.Builder builder)
            {
                _topLevelAddress = topLevelAddress;
                _builder = builder;
                _targetTestHelpers = builder.TargetTestHelpers;
                var arch = _targetTestHelpers.Arch;
                _levels = arch.Is64Bit ? 5 : 2;
                _maxSetBit = arch.Is64Bit ? 56 : 31; // 0 indexed
                MockMemorySpace.HeapFragment top = new MockMemorySpace.HeapFragment
                {
                    Address = topLevelAddress,
                    Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                    Name = $"Map Level {_levels}"
                };
                _nextMapAddress = topLevelAddress + (ulong)top.Data.Length;
                _builder.AddHeapFragment(top);
            }

            public TargetPointer TopLevel => _topLevelAddress;

            private int EffectiveBitsForLevel(ulong address, int level)
            {
                ulong addressBitsUsedInMap = address >> _maxSetBit + 1 - _levels * BitsPerLevel;
                ulong addressBitsShifted = addressBitsUsedInMap >> (level - 1) * BitsPerLevel;
                int addressBitsUsedInLevel = checked((int)(EntriesPerMapLevel - 1 & addressBitsShifted));
                return addressBitsUsedInLevel;
            }

            // This is how much of the address space is covered by each entry in the last level of the map
            private int BytesAtLastLevel => checked(1 << BitsAtLastLevel);
            private int BitsAtLastLevel => _maxSetBit - BitsPerLevel * _levels + 1;

            private TargetPointer CursorAddress(RangeSectionMap.Cursor cursor)
            {
                return cursor.LevelMap + (ulong)(cursor.Index * _targetTestHelpers.PointerSize);
            }

            private void WritePointer(RangeSectionMap.Cursor cursor, InteriorMapValue value)
            {
                TargetPointer address = CursorAddress(cursor);
                Span<byte> dest = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
                _targetTestHelpers.WritePointer(dest, value.RawValue);
            }

            private InteriorMapValue LoadCursorValue(RangeSectionMap.Cursor cursor)
            {
                TargetPointer address = CursorAddress(cursor);
                ReadOnlySpan<byte> src = _builder.BorrowAddressRange(address, _targetTestHelpers.PointerSize);
                return new InteriorMapValue(_targetTestHelpers.ReadPointer(src));
            }

            private MockMemorySpace.HeapFragment AllocateMapLevel(int level)
            {
                MockMemorySpace.HeapFragment mapLevel = new MockMemorySpace.HeapFragment
                {
                    Address = new TargetPointer(_nextMapAddress),
                    Data = new byte[EntriesPerMapLevel * _targetTestHelpers.PointerSize],
                    Name = $"Map Level {level}"
                };
                _nextMapAddress += (ulong)mapLevel.Data.Length;
                _builder.AddHeapFragment(mapLevel);
                return mapLevel;
            }


            // computes the cursor for the next level down from the given cursor
            // if the slot for the next level does not exist, it is created
            private RangeSectionMap.Cursor GetOrAddLevelSlot(TargetCodePointer address, RangeSectionMap.Cursor cursor, bool collectible = false)
            {
                int nextLevel = cursor.Level - 1;
                int nextIndex = EffectiveBitsForLevel(address, nextLevel);
                InteriorMapValue nextLevelMap = LoadCursorValue(cursor);
                if (nextLevelMap.IsNull)
                {
                    nextLevelMap = new(AllocateMapLevel(nextLevel).Address);
                    if (collectible)
                    {
                        nextLevelMap = new(nextLevelMap.RawValue | 1);
                    }
                    WritePointer(cursor, nextLevelMap);
                }
                return new RangeSectionMap.Cursor(nextLevelMap.Address, nextLevel, nextIndex);
            }

            // ensures that the maps for all the levels for the given address are allocated.
            // returns the address of the slot in the last level that corresponds to the given address
            RangeSectionMap.Cursor EnsureLevelsForAddress(TargetCodePointer address, bool collectible = false)
            {
                int topIndex = EffectiveBitsForLevel(address, _levels);
                RangeSectionMap.Cursor cursor = new RangeSectionMap.Cursor(TopLevel, _levels, topIndex);
                while (!cursor.IsLeaf)
                {
                    cursor = GetOrAddLevelSlot(address, cursor, collectible);
                }
                return cursor;
            }
            public void InsertAddressRange(TargetCodePointer start, uint length, ulong value, bool collectible = false)
            {
                TargetCodePointer cur = start;
                ulong end = start.Value + length;
                do
                {
                    RangeSectionMap.Cursor lastCursor = EnsureLevelsForAddress(cur, collectible);
                    WritePointer(lastCursor, new InteriorMapValue(value));
                    cur = new TargetCodePointer(cur.Value + (ulong)BytesAtLastLevel); // FIXME: round ?
                } while (cur.Value < end);
            }

            public MockMemorySpace.ReadContext GetReadContext()
            {
                return _builder.GetReadContext();
            }
        }

        public static RangeSectionMapTestBuilder CreateRangeSection(MockTarget.Architecture arch)
        {
            return new RangeSectionMapTestBuilder(arch);
        }

        private static readonly MockDescriptors.TypeFields RangeSectionMapFields = new()
        {
            DataType = DataType.RangeSectionMap,
            Fields =
            [
                new(nameof(Data.RangeSectionMap.TopLevelData), DataType.pointer),
            ]
        };

        private static readonly MockDescriptors.TypeFields RangeSectionFragmentFields = new()
        {
            DataType = DataType.RangeSectionFragment,
            Fields =
            [
                new(nameof(Data.RangeSectionFragment.RangeBegin), DataType.pointer),
                new(nameof(Data.RangeSectionFragment.RangeEndOpen), DataType.pointer),
                new(nameof(Data.RangeSectionFragment.RangeSection), DataType.pointer),
                new(nameof(Data.RangeSectionFragment.Next), DataType.pointer)
            ]
        };

        private static readonly MockDescriptors.TypeFields RangeSectionFields = new()
        {
            DataType = DataType.RangeSection,
            Fields =
            [
                new(nameof(Data.RangeSection.RangeBegin), DataType.pointer),
                new(nameof(Data.RangeSection.RangeEndOpen), DataType.pointer),
                new(nameof(Data.RangeSection.NextForDelete), DataType.pointer),
                new(nameof(Data.RangeSection.JitManager), DataType.pointer),
                new(nameof(Data.RangeSection.Flags), DataType.int32),
                new(nameof(Data.RangeSection.HeapList), DataType.pointer),
                new(nameof(Data.RangeSection.R2RModule), DataType.pointer),
            ]
        };

        private static readonly MockDescriptors.TypeFields CodeHeapListNodeFields = new()
        {
            DataType = DataType.CodeHeapListNode,
            Fields =
            [
                new(nameof(Data.CodeHeapListNode.Next), DataType.pointer),
                new(nameof(Data.CodeHeapListNode.StartAddress), DataType.pointer),
                new(nameof(Data.CodeHeapListNode.EndAddress), DataType.pointer),
                new(nameof(Data.CodeHeapListNode.MapBase), DataType.pointer),
                new(nameof(Data.CodeHeapListNode.HeaderMap), DataType.pointer),
            ]
        };

        private static readonly MockDescriptors.TypeFields RealCodeHeaderFields = new()
        {
            DataType = DataType.RealCodeHeader,
            Fields =
            [
                new(nameof(Data.RealCodeHeader.MethodDesc), DataType.pointer),
                new(nameof(Data.RealCodeHeader.GCInfo), DataType.pointer),
                new(nameof(Data.RealCodeHeader.NumUnwindInfos), DataType.uint32),
                new(nameof(Data.RealCodeHeader.UnwindInfos), DataType.pointer),
            ]
        };

        private static MockDescriptors.TypeFields ReadyToRunInfoFields(TargetTestHelpers helpers) => new()
        {
            DataType = DataType.ReadyToRunInfo,
            Fields =
            [
                new(nameof(Data.ReadyToRunInfo.ReadyToRunHeader), DataType.pointer),
                new(nameof(Data.ReadyToRunInfo.CompositeInfo), DataType.pointer),
                new(nameof(Data.ReadyToRunInfo.NumRuntimeFunctions), DataType.uint32),
                new(nameof(Data.ReadyToRunInfo.RuntimeFunctions), DataType.pointer),
                new(nameof(Data.ReadyToRunInfo.NumHotColdMap), DataType.uint32),
                new(nameof(Data.ReadyToRunInfo.HotColdMap), DataType.pointer),
                new(nameof(Data.ReadyToRunInfo.DelayLoadMethodCallThunks), DataType.pointer),
                new(nameof(Data.ReadyToRunInfo.EntryPointToMethodDescMap), DataType.Unknown, helpers.LayoutFields(MockDescriptors.HashMap.HashMapFields.Fields).Stride),
            ]
        };

        internal int Version { get; }

        internal MockMemorySpace.Builder Builder { get; }
        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        private readonly RangeSectionMapTestBuilder _rsmBuilder;
        private readonly RuntimeFunctions _rfBuilder;

        private readonly MockMemorySpace.BumpAllocator _rangeSectionMapAllocator;
        private readonly MockMemorySpace.BumpAllocator _nibbleMapAllocator;
        private readonly MockMemorySpace.BumpAllocator _allocator;

        internal ExecutionManager(int version, MockTarget.Architecture arch, AllocationRange allocationRange)
            : this(version, new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
        { }

        internal ExecutionManager(int version, MockMemorySpace.Builder builder, AllocationRange allocationRange)
        {
            Version = version;
            Builder = builder;
            _rsmBuilder = new RangeSectionMapTestBuilder(ExecutionManagerCodeRangeMapAddress, builder);
            _rfBuilder = new RuntimeFunctions(builder);
            _rangeSectionMapAllocator = Builder.CreateAllocator(allocationRange.RangeSectionMapStart, allocationRange.RangeSectionMapEnd);
            _nibbleMapAllocator = Builder.CreateAllocator(allocationRange.NibbleMapStart, allocationRange.NibbleMapEnd);
            _allocator = Builder.CreateAllocator(allocationRange.ExecutionManagerStart, allocationRange.ExecutionManagerEnd);
            Types = MockDescriptors.GetTypesForTypeFields(
                Builder.TargetTestHelpers,
                [
                    RangeSectionMapFields,
                    RangeSectionFragmentFields,
                    RangeSectionFields,
                    CodeHeapListNodeFields,
                    RealCodeHeaderFields,
                    ReadyToRunInfoFields(Builder.TargetTestHelpers),
                    MockDescriptors.ModuleFields,
                ]).Concat(MockDescriptors.HashMap.GetTypes(Builder.TargetTestHelpers))
                .Concat(_rfBuilder.Types)
                .ToDictionary();

            Globals =
            [
                (nameof(Constants.Globals.ExecutionManagerCodeRangeMapAddress), ExecutionManagerCodeRangeMapAddress),
                (nameof(Constants.Globals.StubCodeBlockLast), 0x0Fu),
            ];
            Globals = Globals
                .Concat(MockDescriptors.HashMap.GetGlobals(Builder.TargetTestHelpers))
                .ToArray();
        }

        internal NibbleMapTestBuilderBase CreateNibbleMap(ulong codeRangeStart, uint codeRangeSize)
        {
            NibbleMapTestBuilderBase nibBuilder = Version switch
            {
                1 => new NibbleMapTestBuilder_1(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch),

                // The nibblemap algorithm was changed in version 2
                2 => new NibbleMapTestBuilder_2(codeRangeStart, codeRangeSize, _nibbleMapAllocator, Builder.TargetTestHelpers.Arch),
                _ => throw new InvalidOperationException("Unknown version"),
            };

            Builder.AddHeapFragment(nibBuilder.NibbleMapFragment);
            return nibBuilder;
        }

        internal readonly struct JittedCodeRange
        {
            public MockMemorySpace.BumpAllocator Allocator { get; init; }
            public ulong RangeStart => Allocator.RangeStart;
            public ulong RangeEnd => Allocator.RangeEnd;
            public ulong RangeSize => RangeEnd - RangeStart;
        }

        public JittedCodeRange AllocateJittedCodeRange(ulong codeRangeStart, uint codeRangeSize)
        {
            MockMemorySpace.BumpAllocator allocator = Builder.CreateAllocator(codeRangeStart, codeRangeStart + codeRangeSize, minAlign: 1);
            return new JittedCodeRange { Allocator = allocator };
        }

        public TargetPointer AddRangeSection(JittedCodeRange jittedCodeRange, TargetPointer jitManagerAddress, TargetPointer codeHeapListNodeAddress)
        {
            var tyInfo = Types[DataType.RangeSection];
            uint rangeSectionSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment rangeSection = _rangeSectionMapAllocator.Allocate(rangeSectionSize, "RangeSection");
            Builder.AddHeapFragment(rangeSection);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rs = Builder.BorrowAddressRange(rangeSection.Address, (int)rangeSectionSize);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeBegin)].Offset, pointerSize), jittedCodeRange.RangeStart);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeEndOpen)].Offset, pointerSize), jittedCodeRange.RangeEnd);
            // 0x02 = RangeSectionFlags.CodeHeap
            Builder.TargetTestHelpers.Write(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.Flags)].Offset, sizeof(uint)), (uint)0x02);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.HeapList)].Offset, pointerSize), codeHeapListNodeAddress);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.JitManager)].Offset, pointerSize), jitManagerAddress);
            // FIXME: other fields

            return rangeSection.Address;
        }

        public TargetPointer AddReadyToRunRangeSection(JittedCodeRange jittedCodeRange, TargetPointer jitManagerAddress, TargetPointer r2rModule)
        {
            var tyInfo = Types[DataType.RangeSection];
            uint rangeSectionSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment rangeSection = _rangeSectionMapAllocator.Allocate(rangeSectionSize, "RangeSection");
            Builder.AddHeapFragment(rangeSection);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rs = Builder.BorrowAddressRange(rangeSection.Address, (int)rangeSectionSize);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeBegin)].Offset, pointerSize), jittedCodeRange.RangeStart);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.RangeEndOpen)].Offset, pointerSize), jittedCodeRange.RangeEnd);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.R2RModule)].Offset, pointerSize), r2rModule);
            Builder.TargetTestHelpers.WritePointer(rs.Slice(tyInfo.Fields[nameof(Data.RangeSection.JitManager)].Offset, pointerSize), jitManagerAddress);
            return rangeSection.Address;
        }

        public TargetPointer AddRangeSectionFragment(JittedCodeRange jittedCodeRange, TargetPointer rangeSectionAddress)
        {
            var tyInfo = Types[DataType.RangeSectionFragment];
            uint rangeSectionFragmentSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment rangeSectionFragment = _rangeSectionMapAllocator.Allocate(rangeSectionFragmentSize, "RangeSectionFragment");
            // FIXME: this shouldn't really be called InsertAddressRange, but maybe InsertRangeSectionFragment?
            _rsmBuilder.InsertAddressRange(jittedCodeRange.RangeStart, (uint)jittedCodeRange.RangeSize, rangeSectionFragment.Address);
            Builder.AddHeapFragment(rangeSectionFragment);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> rsf = Builder.BorrowAddressRange(rangeSectionFragment.Address, (int)rangeSectionFragmentSize);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeBegin)].Offset, pointerSize), jittedCodeRange.RangeStart);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeEndOpen)].Offset, pointerSize), jittedCodeRange.RangeEnd);
            Builder.TargetTestHelpers.WritePointer(rsf.Slice(tyInfo.Fields[nameof(Data.RangeSectionFragment.RangeSection)].Offset, pointerSize), rangeSectionAddress);
            /* Next = nullptr */
            // nothing
            return rangeSectionFragment.Address;
        }

        public TargetPointer AddCodeHeapListNode(TargetPointer next, TargetPointer startAddress, TargetPointer endAddress, TargetPointer mapBase, TargetPointer headerMap)
        {
            var tyInfo = Types[DataType.CodeHeapListNode];
            uint codeHeapListNodeSize = tyInfo.Size.Value;
            MockMemorySpace.HeapFragment codeHeapListNode = _rangeSectionMapAllocator.Allocate(codeHeapListNodeSize, "CodeHeapListNode");
            Builder.AddHeapFragment(codeHeapListNode);
            int pointerSize = Builder.TargetTestHelpers.PointerSize;
            Span<byte> chln = Builder.BorrowAddressRange(codeHeapListNode.Address, (int)codeHeapListNodeSize);
            Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.Next)].Offset, pointerSize), next);
            Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.StartAddress)].Offset, pointerSize), startAddress);
            Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.EndAddress)].Offset, pointerSize), endAddress);
            Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.MapBase)].Offset, pointerSize), mapBase);
            Builder.TargetTestHelpers.WritePointer(chln.Slice(tyInfo.Fields[nameof(Data.CodeHeapListNode.HeaderMap)].Offset, pointerSize), headerMap);
            return codeHeapListNode.Address;
        }

        private uint CodeHeaderSize => (uint)Builder.TargetTestHelpers.PointerSize;

        // offset from the start of the code
        private uint CodeHeaderOffset => CodeHeaderSize;

        private (MockMemorySpace.HeapFragment fragment, TargetCodePointer codeStart) AllocateJittedMethod(JittedCodeRange jittedCodeRange, uint codeSize, string name = "Method Header & Code")
        {
            ulong size = codeSize + CodeHeaderOffset;
            MockMemorySpace.HeapFragment methodFragment = jittedCodeRange.Allocator.Allocate(size, name);
            Builder.AddHeapFragment(methodFragment);
            TargetCodePointer codeStart = methodFragment.Address + CodeHeaderOffset;
            return (methodFragment, codeStart);
        }

        public TargetCodePointer AddJittedMethod(JittedCodeRange jittedCodeRange, uint codeSize, TargetPointer methodDescAddress)
        {
            (MockMemorySpace.HeapFragment methodFragment, TargetCodePointer codeStart) = AllocateJittedMethod(jittedCodeRange, codeSize);

            MockMemorySpace.HeapFragment codeHeaderFragment = _allocator.Allocate(RealCodeHeaderSize, "RealCodeHeader");
            Builder.AddHeapFragment(codeHeaderFragment);

            Span<byte> mfPtr = Builder.BorrowAddressRange(methodFragment.Address, (int)CodeHeaderSize);
            Builder.TargetTestHelpers.WritePointer(mfPtr.Slice(0, Builder.TargetTestHelpers.PointerSize), codeHeaderFragment.Address);

            Span<byte> chf = Builder.BorrowAddressRange(codeHeaderFragment.Address, RealCodeHeaderSize);
            var tyInfo = Types[DataType.RealCodeHeader];
            Builder.TargetTestHelpers.WritePointer(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDescAddress);

            // fields are not used in the test, but we still need to write them
            Builder.TargetTestHelpers.WritePointer(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.GCInfo)].Offset, Builder.TargetTestHelpers.PointerSize), TargetPointer.Null);
            Builder.TargetTestHelpers.Write(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.NumUnwindInfos)].Offset, sizeof(uint)), 0u);
            Builder.TargetTestHelpers.WritePointer(chf.Slice(tyInfo.Fields[nameof(Data.RealCodeHeader.UnwindInfos)].Offset, Builder.TargetTestHelpers.PointerSize), TargetPointer.Null);

            return codeStart;
        }

        public TargetPointer AddReadyToRunInfo(uint[] runtimeFunctions, uint[] hotColdMap)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            // Add the array of runtime functions
            uint numRuntimeFunctions = (uint)runtimeFunctions.Length;
            TargetPointer runtimeFunctionsAddr = _rfBuilder.AddRuntimeFunctions(runtimeFunctions);

            // Add the hot/cold map
            TargetPointer hotColdMapAddr = TargetPointer.Null;
            if (hotColdMap.Length > 0)
            {
                MockMemorySpace.HeapFragment hotColdMapFragment = _allocator.Allocate((ulong)hotColdMap.Length * sizeof(uint), $"HotColdMap[{hotColdMap.Length}]");
                Builder.AddHeapFragment(hotColdMapFragment);
                hotColdMapAddr = hotColdMapFragment.Address;
                for (uint i = 0; i < hotColdMap.Length; i++)
                {
                    Span<byte> span = Builder.BorrowAddressRange(hotColdMapFragment.Address + i * sizeof(uint), sizeof(uint));
                    helpers.Write(span, hotColdMap[i]);
                }
            }

            // Add ReadyToRunInfo
            Target.TypeInfo r2rInfoType = Types[DataType.ReadyToRunInfo];
            MockMemorySpace.HeapFragment r2rInfo = _allocator.Allocate(r2rInfoType.Size.Value, "ReadyToRunInfo");
            Builder.AddHeapFragment(r2rInfo);
            Span<byte> data = r2rInfo.Data;

            // Point composite info at itself
            helpers.WritePointer(data.Slice(r2rInfoType.Fields[nameof(Data.ReadyToRunInfo.CompositeInfo)].Offset, helpers.PointerSize), r2rInfo.Address);

            // Point at the runtime functions
            helpers.Write(data.Slice(r2rInfoType.Fields[nameof(Data.ReadyToRunInfo.NumRuntimeFunctions)].Offset, sizeof(uint)), numRuntimeFunctions);
            helpers.WritePointer(data.Slice(r2rInfoType.Fields[nameof(Data.ReadyToRunInfo.RuntimeFunctions)].Offset, helpers.PointerSize), runtimeFunctionsAddr);

            // Point at the hot/cold map
            helpers.Write(data.Slice(r2rInfoType.Fields[nameof(Data.ReadyToRunInfo.NumHotColdMap)].Offset, sizeof(uint)), hotColdMap.Length);
            helpers.WritePointer(data.Slice(r2rInfoType.Fields[nameof(Data.ReadyToRunInfo.HotColdMap)].Offset, helpers.PointerSize), hotColdMapAddr);

            return r2rInfo.Address;
        }

        public TargetPointer AddReadyToRunModule(TargetPointer r2rInfo)
        {
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            Target.TypeInfo moduleType = Types[DataType.Module];
            MockMemorySpace.HeapFragment r2rModule = _allocator.Allocate(moduleType.Size.Value, "R2R Module");
            Builder.AddHeapFragment(r2rModule);
            helpers.WritePointer(r2rModule.Data.AsSpan().Slice(moduleType.Fields[nameof(Data.Module.ReadyToRunInfo)].Offset, helpers.PointerSize), r2rInfo);

            return r2rModule.Address;
        }
    }
}
