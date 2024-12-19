// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class RuntimeTypeSystem
    {
        internal const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;

        private const ulong DefaultAllocationRangeStart = 0x00000000_4a000000;
        private const ulong DefaultAllocationRangeEnd = 0x00000000_4b000000;

        private static readonly TypeFields TypeDescFields = new TypeFields()
        {
            DataType = DataType.TypeDesc,
            Fields =
            [
                new(nameof(Data.TypeDesc.TypeAndFlags), DataType.uint32),
            ]
        };

        private static readonly TypeFields FnPtrTypeDescFields = new TypeFields()
        {
            DataType = DataType.FnPtrTypeDesc,
            Fields =
            [
                new(nameof(Data.FnPtrTypeDesc.NumArgs), DataType.uint32),
                new(nameof(Data.FnPtrTypeDesc.CallConv), DataType.uint32),
                new(nameof(Data.FnPtrTypeDesc.LoaderModule), DataType.pointer),
                new(nameof(Data.FnPtrTypeDesc.RetAndArgTypes), DataType.pointer),
            ],
            BaseTypeFields = TypeDescFields
        };

        private static readonly TypeFields ParamTypeDescFields = new TypeFields()
        {
            DataType = DataType.ParamTypeDesc,
            Fields =
            [
                new(nameof(Data.ParamTypeDesc.TypeArg), DataType.pointer),
            ],
            BaseTypeFields = TypeDescFields
        };

        private static readonly TypeFields TypeVarTypeDescFields = new TypeFields()
        {
            DataType = DataType.TypeVarTypeDesc,
            Fields =
            [
                new(nameof(Data.TypeVarTypeDesc.Module), DataType.pointer),
                new(nameof(Data.TypeVarTypeDesc.Token), DataType.uint32),
            ],
            BaseTypeFields = TypeDescFields
        };

        private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            return GetTypesForTypeFields(
                helpers,
                [
                    MethodTableFields,
                    EEClassFields,
                    ArrayClassFields,
                    MethodTableAuxiliaryDataFields,
                    TypeDescFields,
                    FnPtrTypeDescFields,
                    ParamTypeDescFields,
                    TypeVarTypeDescFields,
                    GCCoverageInfoFields,
                ]);
        }

        internal static uint GetMethodDescAlignment(TargetTestHelpers helpers) => helpers.Arch.Is64Bit ? 8u : 4u;

        internal readonly MockMemorySpace.Builder Builder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        internal MockMemorySpace.BumpAllocator TypeSystemAllocator { get; }

        internal TargetPointer FreeObjectMethodTableAddress { get; private set; }

        public RuntimeTypeSystem(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public RuntimeTypeSystem(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            Builder = builder;
            TypeSystemAllocator = builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            Types = GetTypes(Builder.TargetTestHelpers);

            AddGlobalPointers();
            Globals =
            [
                (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress),
                (nameof(Constants.Globals.MethodDescAlignment), GetMethodDescAlignment(Builder.TargetTestHelpers)),
            ];
        }

        private void AddGlobalPointers()
        {
            AddFreeObjectMethodTable();
        }

        private void AddFreeObjectMethodTable()
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            MockMemorySpace.HeapFragment freeObjectMethodTableFragment = TypeSystemAllocator.Allocate(methodTableTypeInfo.Size.Value, "Free Object Method Table");
            Builder.AddHeapFragment(freeObjectMethodTableFragment);
            FreeObjectMethodTableAddress = freeObjectMethodTableFragment.Address;

            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(globalAddr.Data, FreeObjectMethodTableAddress);
            Builder.AddHeapFragment(globalAddr);
        }

        // set the eeClass MethodTable pointer to the canonMT and the canonMT's EEClass pointer to the eeClass
        internal void SetEEClassAndCanonMTRefs(TargetPointer eeClass, TargetPointer canonMT)
        {
            // make eeClass point at the canonMT
            Target.TypeInfo eeClassTypeInfo = Types[DataType.EEClass];
            Span<byte> eeClassBytes = Builder.BorrowAddressRange(eeClass, (int)eeClassTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(eeClassBytes.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset, Builder.TargetTestHelpers.PointerSize), canonMT);

            // and make the canonMT point at the eeClass
            SetMethodTableEEClassOrCanonMTRaw(canonMT, eeClass);
        }

        // for cases when a methodTable needs to point at a canonical method table
        internal void SetMethodTableCanonMT(TargetPointer methodTable, TargetPointer canonMT) => SetMethodTableEEClassOrCanonMTRaw(methodTable, canonMT.Value | 1);

        // NOTE: don't use directly unless you want to write a bogus value into the canonMT field
        internal void SetMethodTableEEClassOrCanonMTRaw(TargetPointer methodTable, TargetPointer eeClassOrCanonMT)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            Span<byte> methodTableBytes = Builder.BorrowAddressRange(methodTable, (int)methodTableTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(methodTableBytes.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset, Builder.TargetTestHelpers.PointerSize), eeClassOrCanonMT);
        }

        // call SetEEClassAndCanonMTRefs after the EEClass and the MethodTable have been added
        internal TargetPointer AddEEClass(string name, uint attr, ushort numMethods, ushort numNonVirtualSlots)
        {
            Target.TypeInfo eeClassTypeInfo  = Types[DataType.EEClass];
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;

            MockMemorySpace.HeapFragment eeClassFragment = TypeSystemAllocator.Allocate(eeClassTypeInfo.Size.Value, $"EEClass '{name}'");
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            builder.AddHeapFragment(eeClassFragment);
            return eeClassFragment.Address;
        }

        internal TargetPointer AddArrayClass(string name, uint attr, ushort numMethods, ushort numNonVirtualSlots, byte rank)
        {
            Dictionary<DataType, Target.TypeInfo> types = Types;
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo eeClassTypeInfo = types[DataType.EEClass];
            Target.TypeInfo arrayClassTypeInfo = types[DataType.ArrayClass];
            MockMemorySpace.HeapFragment eeClassFragment = TypeSystemAllocator.Allocate (arrayClassTypeInfo.Size.Value, $"ArrayClass '{name}'");
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            targetTestHelpers.Write(dest.Slice(arrayClassTypeInfo.Fields[nameof(Data.ArrayClass.Rank)].Offset), rank);
            builder.AddHeapFragment(eeClassFragment);
            return eeClassFragment.Address;
        }

        internal TargetPointer AddMethodTable(string name, uint mtflags, uint mtflags2, uint baseSize,
                                                            TargetPointer module, TargetPointer parentMethodTable, ushort numInterfaces, ushort numVirtuals)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment methodTableFragment = TypeSystemAllocator.Allocate(methodTableTypeInfo.Size.Value,  $"MethodTable '{name}'");
            Span<byte> dest = methodTableFragment.Data;
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), mtflags);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), mtflags2);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), baseSize);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.Module)].Offset), module);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.ParentMethodTable)].Offset), parentMethodTable);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumInterfaces)].Offset), numInterfaces);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumVirtuals)].Offset), numVirtuals);

            // TODO fill in the rest of the fields
            builder.AddHeapFragment(methodTableFragment);
            return methodTableFragment.Address;
        }

        internal void SetMethodTableAuxData(TargetPointer methodTablePointer, TargetPointer loaderModule)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            Target.TypeInfo auxDataTypeInfo = Types[DataType.MethodTableAuxiliaryData];
            MockMemorySpace.HeapFragment auxDataFragment = TypeSystemAllocator.Allocate(auxDataTypeInfo.Size.Value, "MethodTableAuxiliaryData");
            Span<byte> dest = auxDataFragment.Data;
            Builder.TargetTestHelpers.WritePointer(dest.Slice(auxDataTypeInfo.Fields[nameof(Data.MethodTableAuxiliaryData.LoaderModule)].Offset), loaderModule);
            Builder.AddHeapFragment(auxDataFragment);

            Span<byte> methodTable = Builder.BorrowAddressRange(methodTablePointer, (int)methodTableTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(methodTable.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.AuxiliaryData)].Offset), auxDataFragment.Address);
        }

        internal TargetPointer AddFunctionPointerTypeDesc(uint callConv, TargetPointer[] retAndArgTypes, TargetPointer loaderModule)
        {
            Target.TypeInfo typeInfo = Types[DataType.FnPtrTypeDesc];
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            ulong size = typeInfo.Size.Value + (ulong)(retAndArgTypes.Length * helpers.PointerSize);
            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate(size, $"FnPtrTypeDesc");
            Builder.AddHeapFragment(fragment);
            Span<byte> dest = fragment.Data;
            helpers.Write(dest.Slice(Types[DataType.TypeDesc].Fields[nameof(Data.TypeDesc.TypeAndFlags)].Offset), (uint)CorElementType.FnPtr);
            helpers.Write(dest.Slice(typeInfo.Fields[nameof(Data.FnPtrTypeDesc.NumArgs)].Offset), retAndArgTypes.Length - 1);
            helpers.Write(dest.Slice(typeInfo.Fields[nameof(Data.FnPtrTypeDesc.CallConv)].Offset), callConv);
            helpers.WritePointer(dest.Slice(typeInfo.Fields[nameof(Data.FnPtrTypeDesc.LoaderModule)].Offset), loaderModule);
            for (int i = 0; i < retAndArgTypes.Length; i ++)
            {
                Span<byte> span = fragment.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.FnPtrTypeDesc.RetAndArgTypes)].Offset + i * helpers.PointerSize, helpers.PointerSize);
                helpers.WritePointer(span, retAndArgTypes[i]);
            }

            return fragment.Address;
        }

        internal TargetPointer AddParamTypeDesc(uint typeAndFlags, TargetPointer typeArg)
        {
            CorElementType type = (CorElementType)(typeAndFlags & 0xFF);
            if (type != CorElementType.ValueType && type != CorElementType.Byref && type != CorElementType.Ptr)
                throw new ArgumentOutOfRangeException(nameof(typeAndFlags));

            Target.TypeInfo typeInfo = Types[DataType.ParamTypeDesc];
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate(typeInfo.Size.Value, $"ParamTypeDesc");
            Builder.AddHeapFragment(fragment);
            Span<byte> dest = fragment.Data;
            helpers.Write(dest.Slice(Types[DataType.TypeDesc].Fields[nameof(Data.TypeDesc.TypeAndFlags)].Offset), typeAndFlags);
            helpers.WritePointer(dest.Slice(typeInfo.Fields[nameof(Data.ParamTypeDesc.TypeArg)].Offset), typeArg);
            return fragment.Address;
        }

        internal TargetPointer AddTypeVarTypeDesc(uint typeAndFlags, TargetPointer module, uint token)
        {
            CorElementType type = (CorElementType)(typeAndFlags & 0xFF);
            if (type != CorElementType.MVar && type != CorElementType.Var)
                throw new ArgumentOutOfRangeException(nameof(typeAndFlags));

            Target.TypeInfo typeInfo = Types[DataType.TypeVarTypeDesc];
            TargetTestHelpers helpers = Builder.TargetTestHelpers;

            MockMemorySpace.HeapFragment fragment = TypeSystemAllocator.Allocate(typeInfo.Size.Value, $"TypeVarTypeDesc");
            Builder.AddHeapFragment(fragment);
            Span<byte> dest = fragment.Data;
            helpers.Write(dest.Slice(Types[DataType.TypeDesc].Fields[nameof(Data.TypeDesc.TypeAndFlags)].Offset), typeAndFlags);
            helpers.WritePointer(dest.Slice(typeInfo.Fields[nameof(Data.TypeVarTypeDesc.Module)].Offset), module);
            helpers.Write(dest.Slice(typeInfo.Fields[nameof(Data.TypeVarTypeDesc.Token)].Offset), token);
            return fragment.Address;
        }
    }
}
