// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

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
                (nameof(Data.MethodDescVersioningState.NativeCodeVersionNode), DataType.pointer),
                (nameof(Data.MethodDescVersioningState.Flags), DataType.uint8),
            ]
        };

        private static readonly TypeFields NativeCodeVersionNodeFields = new TypeFields()
        {
            DataType = DataType.NativeCodeVersionNode,
            Fields =
            [
                (nameof(Data.NativeCodeVersionNode.Next), DataType.pointer),
                (nameof(Data.NativeCodeVersionNode.MethodDesc), DataType.pointer),
                (nameof(Data.NativeCodeVersionNode.NativeCode), DataType.pointer),
                (nameof(Data.NativeCodeVersionNode.Flags), DataType.uint32),
                (nameof(Data.NativeCodeVersionNode.ILVersionId), DataType.nuint),
            ]
        };

        private static readonly TypeFields ILCodeVersioningStateFields = new TypeFields()
        {
            DataType = DataType.ILCodeVersioningState,
            Fields =
            [
                (nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef), DataType.uint32),
                (nameof(Data.ILCodeVersioningState.ActiveVersionModule), DataType.pointer),
                (nameof(Data.ILCodeVersioningState.ActiveVersionKind), DataType.uint32),
                (nameof(Data.ILCodeVersioningState.ActiveVersionNode), DataType.pointer),
            ]
        };

        private static readonly TypeFields ILCodeVersionNodeFields = new TypeFields()
        {
            DataType = DataType.ILCodeVersionNode,
            Fields =
            [
                (nameof(Data.ILCodeVersionNode.VersionId), DataType.nuint),
            ]
        };

        internal readonly MockMemorySpace.Builder Builder;
        internal Dictionary<DataType, Target.TypeInfo> Types { get; }

        private readonly MockMemorySpace.BumpAllocator _codeVersionsAllocator;

        public CodeVersions(MockTarget.Architecture arch)
            : this(new MockMemorySpace.Builder(new TargetTestHelpers(arch)), (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
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
                ]);
        }

        public void MarkCreated() => Builder.MarkCreated();

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

        public void FillNativeCodeVersionNode(TargetPointer dest, TargetPointer methodDesc, TargetCodePointer nativeCode, TargetPointer next, bool isActive, TargetNUInt ilVersionId)
        {
            Target.TypeInfo info = Types[DataType.NativeCodeVersionNode];
            Span<byte> ncvn = Builder.BorrowAddressRange(dest, (int)info.Size!);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.Next)].Offset, Builder.TargetTestHelpers.PointerSize), next);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.MethodDesc)].Offset, Builder.TargetTestHelpers.PointerSize), methodDesc);
            Builder.TargetTestHelpers.WritePointer(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.NativeCode)].Offset, Builder.TargetTestHelpers.PointerSize), nativeCode);
            Builder.TargetTestHelpers.Write(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.Flags)].Offset, sizeof(uint)), isActive ? (uint)CodeVersions_1.NativeCodeVersionNodeFlags.IsActiveChild : 0u);
            Builder.TargetTestHelpers.WriteNUInt(ncvn.Slice(info.Fields[nameof(Data.NativeCodeVersionNode.ILVersionId)].Offset, Builder.TargetTestHelpers.PointerSize), ilVersionId);
        }

        public (TargetPointer First, TargetPointer Active) AddNativeCodeVersionNodesForMethod(TargetPointer methodDesc, int count, int activeIndex, TargetCodePointer activeNativeCode, TargetNUInt explicitILVersion)
        {
            TargetPointer activeVersionNode = TargetPointer.Null;
            TargetPointer next = TargetPointer.Null;
            for (int i = count - 1; i >= 0; i--)
            {
                TargetPointer node = AddNativeCodeVersionNode();
                bool isActive = i == activeIndex;
                TargetCodePointer nativeCode = isActive ? activeNativeCode : 0;
                TargetNUInt ilVersionId = isActive ? explicitILVersion : default;
                FillNativeCodeVersionNode(node, methodDesc, nativeCode, next, isActive, ilVersionId);
                next = node;
                if (isActive)
                    activeVersionNode = node;
            }

            return (next, activeVersionNode);
        }

        public TargetPointer AddILCodeVersioningState(uint activeVersionKind, TargetPointer activeVersionNode, TargetPointer activeVersionModule, uint activeVersionMethodDef)
        {
            Target.TypeInfo info = Types[DataType.ILCodeVersioningState];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.ILCodeVersioningState].Size, "ILCodeVersioningState");
            Builder.AddHeapFragment(fragment);
            Span<byte> ilcvs = Builder.BorrowAddressRange(fragment.Address, fragment.Data.Length);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionModule)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionModule);
            Builder.TargetTestHelpers.WritePointer(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionNode)].Offset, Builder.TargetTestHelpers.PointerSize), activeVersionNode);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionMethodDef)].Offset, sizeof(uint)), activeVersionMethodDef);
            Builder.TargetTestHelpers.Write(ilcvs.Slice(info.Fields[nameof(Data.ILCodeVersioningState.ActiveVersionKind)].Offset, sizeof(uint)), activeVersionKind);
            return fragment.Address;
        }

        public TargetPointer AddILCodeVersionNode(TargetNUInt versionId)
        {
            Target.TypeInfo info = Types[DataType.ILCodeVersionNode];
            MockMemorySpace.HeapFragment fragment = _codeVersionsAllocator.Allocate((ulong)Types[DataType.ILCodeVersionNode].Size, "NativeCodeVersionNode");
            Builder.AddHeapFragment(fragment);
            Builder.TargetTestHelpers.WriteNUInt(fragment.Data.AsSpan().Slice(info.Fields[nameof(Data.ILCodeVersionNode.VersionId)].Offset), versionId);
            return fragment.Address;
        }
    }
}
