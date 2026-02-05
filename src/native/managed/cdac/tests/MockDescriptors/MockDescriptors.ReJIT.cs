// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class ReJIT
    {
        private const ulong DefaultAllocationRangeStart = 0x0010_1000;
        private const ulong DefaultAllocationRangeEnd = 0x00011_0000;

        // see src/coreclr/vm/codeversion.h
        [Flags]
        public enum RejitFlags : uint
        {
            kStateRequested = 0x00000000,

            kStateGettingReJITParameters = 0x00000001,

            kStateActive = 0x00000002,

            kStateMask = 0x0000000F,

            kSuppressParams = 0x80000000
        }

        private static readonly TypeFields ProfControlBlockFields = new TypeFields()
        {
            DataType = DataType.ProfControlBlock,
            Fields =
            [
                new(nameof(Data.ProfControlBlock.GlobalEventMask), DataType.uint64)
            ]
        };

        internal readonly MockMemorySpace.Builder Builder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        private CodeVersions _codeVersions { get; }

        private readonly MockMemorySpace.BumpAllocator _rejitAllocator;

        public ReJIT(MockTarget.Architecture arch)
            : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public ReJIT(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            _rejitAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            _codeVersions = new CodeVersions(Builder);

            Types = GetTypes(builder.TargetTestHelpers);

            Globals =
            [
                (nameof(Constants.Globals.ProfilerControlBlock), AddProfControlBlock()),
            ];
        }

        public ILCodeVersionHandle AddExplicitILCodeVersion(TargetNUInt rejitId, RejitFlags rejitFlags)
        {
            TargetPointer codeVersionNode = _codeVersions.AddILCodeVersionNode(TargetPointer.Null, rejitId, (uint)rejitFlags);

            return ILCodeVersionHandle.CreateExplicit(codeVersionNode);
        }

        internal static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            Dictionary<DataType, Target.TypeInfo> cvTypes = CodeVersions.GetTypes(helpers);
            Dictionary<DataType, Target.TypeInfo> types = GetTypesForTypeFields(
                helpers,
                [
                    ProfControlBlockFields
                ]);
            foreach(var (dataType, typeInfo) in cvTypes)
            {
                types.Add(dataType, typeInfo);
            }
            return types;
        }

        private ulong AddProfControlBlock()
        {
            Target.TypeInfo info = Types[DataType.ProfControlBlock];
            MockMemorySpace.HeapFragment fragment = _rejitAllocator.Allocate((ulong)Types[DataType.ProfControlBlock].Size, "ProfControlBlock");
            Builder.AddHeapFragment(fragment);
            Span<byte> pcb = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.Write(pcb.Slice(info.Fields[nameof(Data.ProfControlBlock.GlobalEventMask)].Offset, sizeof(ulong)), 0ul);
            return fragment.Address;
        }
    }
}
