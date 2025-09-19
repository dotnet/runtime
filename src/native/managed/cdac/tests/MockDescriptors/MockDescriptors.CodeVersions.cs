// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class CodeVersions
    {
        private const ulong DefaultAllocationRangeStart = 0x000f_c000;
        private const ulong DefaultAllocationRangeEnd = 0x00010_0000;

        private static readonly TypeFields MethodDescVersioningStateFields = new TypeFields()
        {
            DataType = DataType.MethodDescVersioningState,
            Fields =
            [
                new(nameof(Data.MethodDescVersioningState.NativeCodeVersionNode), DataType.pointer),
                new(nameof(Data.MethodDescVersioningState.Flags), DataType.uint8),
            ]
        };
        // note: we aren't testing this on wasm so we can go ahead and include both OptimizationTier and NativeId
        private static readonly TypeFields NativeCodeVersionNodeFields = new TypeFields()
        {
            DataType = DataType.NativeCodeVersionNode,
            Fields =
            [
                new(nameof(Data.NativeCodeVersionNode.Next), DataType.pointer),
                new(nameof(Data.NativeCodeVersionNode.MethodDesc), DataType.pointer),
                new(nameof(Data.NativeCodeVersionNode.NativeCode), DataType.pointer),
                new(nameof(Data.NativeCodeVersionNode.Flags), DataType.uint32),
                new(nameof(Data.NativeCodeVersionNode.ILVersionId), DataType.nuint),
                new(nameof(Data.NativeCodeVersionNode.OptimizationTier), DataType.uint32),
                new(nameof(Data.NativeCodeVersionNode.NativeId), DataType.uint32),
                new(nameof(Data.NativeCodeVersionNode.GCCoverageInfo), DataType.pointer),
            ]
        };

        private static readonly TypeFields ILCodeVersioningStateFields = new TypeFields()
        {
            DataType = DataType.ILCodeVersioningState,
            Fields =
            [
                new(nameof(Data.ILCodeVersioningState.FirstVersionNode), DataType.pointer),
                new(nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef), DataType.uint32),
                new(nameof(Data.ILCodeVersioningState.ActiveVersionModule), DataType.pointer),
                new(nameof(Data.ILCodeVersioningState.ActiveVersionKind), DataType.uint32),
                new(nameof(Data.ILCodeVersioningState.ActiveVersionNode), DataType.pointer),
            ]
        };

        private static readonly TypeFields ILCodeVersionNodeFields = new TypeFields()
        {
            DataType = DataType.ILCodeVersionNode,
            Fields =
            [
                new(nameof(Data.ILCodeVersionNode.VersionId), DataType.nuint),
                new(nameof(Data.ILCodeVersionNode.Next), DataType.pointer),
                new(nameof(Data.ILCodeVersionNode.RejitState), DataType.uint32),
                new(nameof(Data.ILCodeVersionNode.ILAddress), DataType.pointer),
            ]
        };

        internal readonly MockMemorySpace.Builder Builder;
        internal Dictionary<DataType, Target.TypeInfo> Types { get; }

        private readonly MockMemorySpace.BumpAllocator _codeVersionsAllocator;

        public CodeVersions(MockTarget.Architecture arch)
            : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public CodeVersions(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public CodeVersions(MockTarget.Architecture arch, (ulong Start, ulong End) allocationRange)
            : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), allocationRange)
        { }

        public CodeVersions(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            _codeVersionsAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
            Types = GetTypes(Builder.TargetTestHelpers);
        }

        internal static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            return GetTypesForTypeFields(
                helpers,
                [
                    MethodDescVersioningStateFields,
                    NativeCodeVersionNodeFields,
                    ILCodeVersioningStateFields,
                    ILCodeVersionNodeFields,
                    GCCoverageInfoFields,
                ]);
        }

        public TargetPointer AddMethodDescVersioningState(TargetPointer nativeCodeVersionNode, bool isDefaultVersionActive)
        {
            Target.TypeInfo info = Types[DataType.MethodDescVersioningState];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.MethodDescVersioningState].Size, "MethodDescVersioningState");
            Builder.AddHeapFragment(fragment);
            Span<byte> mdvs = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.WritePointer(mdvs.Slice(info.Fields[nameof(Data.MethodDescVersioningState.NativeCodeVersionNode)].Offset, Builder.TargetTestHelpers.PointerSize), nativeCodeVersionNode);
            Builder.TargetTestHelpers.Write(mdvs.Slice(info.Fields[nameof(Data.MethodDescVersioningState.Flags)].Offset, sizeof(byte)), (byte)(isDefaultVersionActive ? CodeVersions_1.MethodDescVersioningStateFlags.IsDefaultVersionActiveChildFlag : 0));
            return fragment.Address;
        }

        public TargetPointer AddNativeCodeVersionNode()
        {
            Target.TypeInfo info = Types[DataType.NativeCodeVersionNode];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.NativeCodeVersionNode].Size, "NativeCodeVersionNode");
            Builder.AddHeapFragment(fragment);
            return fragment.Address;
        }

        public void FillNativeCodeVersionNode(TargetPointer dest, TargetPointer methodDesc, TargetCodePointer nativeCode, TargetPointer next, bool isActive, TargetNUInt ilVersionId, TargetPointer? gcCoverageInfo = null)
        {
            Target.TypeInfo info = Types[DataType.NativeCodeVersionNode];
            Span<byte> ncvn = Builder.BorrowAddressRange(dest, (int)info.Size!);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.Next)].Offset, Builder.TargetTestHelpers.PointerSize), next);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDesc);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.NativeCode)].Offset, Builder.TargetTestHelpers.PointerSize), nativeCode);
            Builder.TargetTestHelpers.Write(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.Flags)].Offset, sizeof(uint)), isActive ? (uint)CodeVersions_1.NativeCodeVersionNodeFlags.IsActiveChild : 0u);
            Builder.TargetTestHelpers.WriteNUInt(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.ILVersionId)].Offset, Builder.TargetTestHelpers.PointerSize), ilVersionId);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.GCCoverageInfo)].Offset, Builder.TargetTestHelpers.PointerSize), gcCoverageInfo ?? TargetPointer.Null);
        }

        public (TargetPointer First, TargetPointer Active) AddNativeCodeVersionNodesForMethod(TargetPointer methodDesc, int count, int activeIndex, TargetCodePointer activeNativeCode, TargetNUInt ilVersion, TargetPointer? firstNode = null)
        {
            TargetPointer activeVersionNode = TargetPointer.Null;
            TargetPointer next = firstNode != null ? firstNode.Value : TargetPointer.Null;
            for (int i = count - 1; i >= 0; i--)
            {
                TargetPointer node = AddNativeCodeVersionNode();
                bool isActive = i == activeIndex;
                TargetCodePointer nativeCode = isActive ? activeNativeCode : 0;
                TargetNUInt ilVersionId = ilVersion;
                FillNativeCodeVersionNode(node, methodDesc, nativeCode, next, isActive, ilVersionId);
                next = node;
                if (isActive)
                    activeVersionNode = node;
            }

            return (next, activeVersionNode);
        }

        public TargetPointer AddILCodeVersioningState(uint activeVersionKind, TargetPointer activeVersionNode, TargetPointer activeVersionModule, uint activeVersionMethodDef, TargetPointer firstVersionNode)
        {
            Target.TypeInfo info = Types[DataType.ILCodeVersioningState];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.ILCodeVersioningState].Size, "ILCodeVersioningState");
            Builder.AddHeapFragment(fragment);
            Span<byte> ilcvs = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionModule)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionModule);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionNode)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionNode);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef)].Offset, sizeof(uint)), activeVersionMethodDef);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionKind)].Offset, sizeof(uint)), activeVersionKind);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.FirstVersionNode)].Offset), firstVersionNode);
            return fragment.Address;
        }

        public TargetPointer AddILCodeVersionNode(TargetPointer prevNodeAddress, TargetNUInt versionId, uint rejitFlags)
        {
            Target.TypeInfo info = Types[DataType.ILCodeVersionNode];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.ILCodeVersionNode].Size, "NativeCodeVersionNode");
            Builder.AddHeapFragment(fragment);
            Builder.TargetTestHelpers.WriteNUInt(fragment.Data.AsSpan().Slice(info.Fields[nameof(Data.ILCodeVersionNode.VersionId)].Offset), versionId);
            Builder.TargetTestHelpers.Write(fragment.Data.AsSpan().Slice(info.Fields[nameof(Data.ILCodeVersionNode.RejitState)].Offset), (uint)rejitFlags);

            // set new node next pointer to null
            Builder.TargetTestHelpers.WritePointer(fragment.Data.AsSpan().Slice(info.Fields[nameof(Data.ILCodeVersionNode.Next)].Offset), TargetPointer.Null);

            // set the previous node next pointer to the new node
            if(prevNodeAddress != TargetPointer.Null)
            {
                Span<byte> prevNode = Builder.BorrowAddressRange(prevNodeAddress, fragment.Data.Length);
                Builder.TargetTestHelpers.WritePointer(prevNode.Slice(info.Fields[nameof(Data.ILCodeVersionNode.Next)].Offset), fragment.Address);
            }

            return fragment.Address;
        }
    }
}
