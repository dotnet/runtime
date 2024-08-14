// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial struct RuntimeTypeSystem_1 : IRuntimeTypeSystem
{
    // GC Heap corruption may create situations where a pointer value may point to garbage or even
    // to an unmapped memory region.
    // All types here have not been validated as actually representing a MethodTable, EEClass, etc.
    // All checks are unsafe and may throw if we access an invalid address in target memory.
    internal static class NonValidated
    {

        // This doesn't need as many properties as MethodTable because we don't want to be operating on
        // a NonValidatedMethodTable for too long
        internal struct MethodTable
        {
            private readonly Target _target;
            private readonly Target.TypeInfo _type;
            internal TargetPointer Address { get; init; }

            private MethodTableFlags? _methodTableFlags;

            internal MethodTable(Target target, TargetPointer methodTablePointer)
            {
                _target = target;
                _type = target.GetTypeInfo(DataType.MethodTable);
                Address = methodTablePointer;
                _methodTableFlags = null;
            }

            private MethodTableFlags GetOrCreateFlags()
            {
                if (_methodTableFlags == null)
                {
                    // note: may throw if the method table Address is corrupted
                    MethodTableFlags flags = new MethodTableFlags
                    {
                        MTFlags = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags.MTFlags)].Offset),
                        MTFlags2 = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags.MTFlags2)].Offset),
                        BaseSize = _target.Read<uint>(Address + (ulong)_type.Fields[nameof(MethodTableFlags.BaseSize)].Offset),
                    };
                    _methodTableFlags = flags;
                }
                return _methodTableFlags.Value;
            }

            internal MethodTableFlags Flags => GetOrCreateFlags();

            internal TargetPointer EEClassOrCanonMT => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(EEClassOrCanonMT)].Offset);
            internal TargetPointer EEClass => GetEEClassOrCanonMTBits(EEClassOrCanonMT) == EEClassOrCanonMTBits.EEClass ? EEClassOrCanonMT : throw new InvalidOperationException("not an EEClass");
            internal TargetPointer CanonMT
            {
                get
                {
                    if (GetEEClassOrCanonMTBits(EEClassOrCanonMT) == EEClassOrCanonMTBits.CanonMT)
                    {
                        return new TargetPointer((ulong)EEClassOrCanonMT & ~(ulong)EEClassOrCanonMTBits.Mask);
                    }
                    else
                    {
                        throw new InvalidOperationException("not a canonical method table");
                    }
                }
            }
        }

        internal struct EEClass
        {
            public readonly Target _target;
            private readonly Target.TypeInfo _type;

            internal TargetPointer Address { get; init; }

            internal EEClass(Target target, TargetPointer eeClassPointer)
            {
                _target = target;
                Address = eeClassPointer;
                _type = target.GetTypeInfo(DataType.EEClass);
            }

            internal TargetPointer MethodTable => _target.ReadPointer(Address + (ulong)_type.Fields[nameof(MethodTable)].Offset);
        }

        internal struct MethodDesc
        {
            private readonly Target _target;
            private readonly Data.MethodDesc _desc;
            private readonly Data.MethodDescChunk _chunk;
            internal MethodDesc(Target target, Data.MethodDesc desc, Data.MethodDescChunk chunk)
            {
                _target = target;
                _desc = desc;
                _chunk = chunk;
            }

            private bool HasFlag(MethodDescFlags flag) => (_desc.Flags & (ushort)flag) != 0;
            private bool HasFlag(MethodDescEntryPointFlags flag) => (_desc.EntryPointFlags & (byte)flag) != 0;

            private bool HasFlag(MethodDescFlags3 flag) => (_desc.Flags3AndTokenRemainder & (ushort)flag) != 0;

            internal byte ChunkIndex => _desc.ChunkIndex;
            internal TargetPointer MethodTable => _chunk.MethodTable;
            internal ushort Slot => _desc.Slot;
            internal bool HasNonVtableSlot => HasFlag(MethodDescFlags.HasNonVtableSlot);
            internal bool HasMethodImpl => HasFlag(MethodDescFlags.HasMethodImpl);
            internal bool HasNativeCodeSlot => HasFlag(MethodDescFlags.HasNativeCodeSlot);

            internal bool TemporaryEntryPointAssigned => HasFlag(MethodDescEntryPointFlags.TemporaryEntryPointAssigned);

            internal TargetPointer CodeData => _desc.CodeData;

            internal MethodClassification Classification => (MethodClassification)(_desc.Flags & (ushort)MethodDescFlags.ClassificationMask);
            internal bool IsFCall => Classification == MethodClassification.FCall;

            #region Additional Pointers
            private int AdditionalPointersHelper(MethodDescFlags extraFlags)
                => int.PopCount(_desc.Flags & (ushort)extraFlags);

            // non-vtable slot, native code slot and MethodImpl slots are stored after the MethodDesc itself, packed tightly
            // in the order: [non-vtable; methhod impl; native code].
            internal int NonVtableSlotIndex => HasNonVtableSlot ? 0 : throw new InvalidOperationException("no non-vtable slot");
            internal int MethodImplIndex
            {
                get
                {
                    if (!HasMethodImpl)
                    {
                        throw new InvalidOperationException("no method impl slot");
                    }
                    return AdditionalPointersHelper(MethodDescFlags.HasNonVtableSlot);
                }
            }
            internal int NativeCodeSlotIndex
            {
                get
                {
                    if (!HasNativeCodeSlot)
                    {
                        throw new InvalidOperationException("no native code slot");
                    }
                    return AdditionalPointersHelper(MethodDescFlags.HasNonVtableSlot | MethodDescFlags.HasMethodImpl);
                }
            }

            internal int AdditionalPointersCount => AdditionalPointersHelper(MethodDescFlags.MethodDescAdditionalPointersMask);
            #endregion Additional Pointers

            internal bool HasStableEntryPoint => HasFlag(MethodDescFlags3.HasStableEntryPoint);
            internal bool HasPrecode => HasFlag(MethodDescFlags3.HasPrecode);

        }


        internal static MethodTable GetMethodTableData(Target target, TargetPointer methodTablePointer)
        {
            return new MethodTable(target, methodTablePointer);
        }

        internal static EEClass GetEEClassData(Target target, TargetPointer eeClassPointer)
        {
            return new EEClass(target, eeClassPointer);
        }

    }

    /// <summary>
    /// Validates that the given address is a valid MethodTable.
    /// </summary>
    ///  <remarks>
    ///  If the target process has memory corruption, we may see pointers that are not valid method tables.
    ///  We validate by looking at the MethodTable -> EEClass -> MethodTable relationship (which may throw if we access invalid memory).
    ///  And then we do some ad-hoc checks on the method table flags.
    private bool ValidateMethodTablePointer(NonValidated.MethodTable umt)
    {
        try
        {
            if (!ValidateThrowing(umt))
            {
                return false;
            }
            if (!ValidateMethodTableAdHoc(umt))
            {
                return false;
            }
        }
        catch (System.Exception)
        {
            // TODO(cdac): maybe don't swallow all exceptions? We could consider a richer contract that
            // helps to track down what sort of memory corruption caused the validation to fail.
            // TODO(cdac): we could also consider a more fine-grained exception type so we don't mask
            // programmer mistakes in cdacreader.
            return false;
        }
        return true;
    }

    // This portion of validation may throw if we are trying to read an invalid address in the target process
    private bool ValidateThrowing(NonValidated.MethodTable methodTable)
    {
        // For non-generic classes, we can rely on comparing
        //    object->methodtable->class->methodtable
        // to
        //    object->methodtable
        //
        //  However, for generic instantiation this does not work. There we must
        //  compare
        //
        //    object->methodtable->class->methodtable->class
        // to
        //    object->methodtable->class
        TargetPointer eeClassPtr = GetClassThrowing(methodTable);
        if (eeClassPtr != TargetPointer.Null)
        {
            NonValidated.EEClass eeClass = NonValidated.GetEEClassData(_target, eeClassPtr);
            TargetPointer methodTablePtrFromClass = eeClass.MethodTable;
            if (methodTable.Address == methodTablePtrFromClass)
            {
                return true;
            }
            if (methodTable.Flags.HasInstantiation || methodTable.Flags.IsArray)
            {
                NonValidated.MethodTable methodTableFromClass = NonValidated.GetMethodTableData(_target, methodTablePtrFromClass);
                TargetPointer classFromMethodTable = GetClassThrowing(methodTableFromClass);
                return classFromMethodTable == eeClassPtr;
            }
        }
        return false;
    }

    private bool ValidateMethodTableAdHoc(NonValidated.MethodTable methodTable)
    {
        // ad-hoc checks; add more here as needed
        if (!methodTable.Flags.IsInterface && !methodTable.Flags.IsString)
        {
            if (methodTable.Flags.BaseSize == 0 || !_target.IsAlignedToPointerSize(methodTable.Flags.BaseSize))
            {
                return false;
            }
        }
        return true;
    }

    internal static EEClassOrCanonMTBits GetEEClassOrCanonMTBits(TargetPointer eeClassOrCanonMTPtr)
    {
        return (EEClassOrCanonMTBits)(eeClassOrCanonMTPtr & (ulong)EEClassOrCanonMTBits.Mask);
    }
    private TargetPointer GetClassThrowing(NonValidated.MethodTable methodTable)
    {
        TargetPointer eeClassOrCanonMT = methodTable.EEClassOrCanonMT;

        if (GetEEClassOrCanonMTBits(eeClassOrCanonMT) == EEClassOrCanonMTBits.EEClass)
        {
            return methodTable.EEClass;
        }
        else
        {
            TargetPointer canonicalMethodTablePtr = methodTable.CanonMT;
            NonValidated.MethodTable umt = NonValidated.GetMethodTableData(_target, canonicalMethodTablePtr);
            return umt.EEClass;
        }
    }

    private TargetPointer GetMethodDescChunkPointerThrowing(TargetPointer methodDescPointer, Data.MethodDesc umd)
    {
        ulong? methodDescChunkSize = _target.GetTypeInfo(DataType.MethodDescChunk).Size;
        if (!methodDescChunkSize.HasValue)
        {
            throw new InvalidOperationException("Target has no definite MethodDescChunk size");
        }
        // The runtime allocates a contiguous block of memory for a MethodDescChunk followed by MethodDescAlignment * Size bytes of space
        // that is filled with MethodDesc (or its subclasses) instances.  Each MethodDesc has a ChunkIndex that indicates its
        // offset from the end of the MethodDescChunk.
        ulong chunkAddress = (ulong)methodDescPointer - methodDescChunkSize.Value - umd.ChunkIndex * MethodDescAlignment;
        return new TargetPointer(chunkAddress);
    }

    private Data.MethodDescChunk GetMethodDescChunkThrowing(TargetPointer methodDescPointer, Data.MethodDesc md, out TargetPointer methodDescChunkPointer)
    {
        methodDescChunkPointer = GetMethodDescChunkPointerThrowing(methodDescPointer, md);
        return new Data.MethodDescChunk(_target, methodDescChunkPointer);
    }

    private NonValidated.MethodDesc GetMethodDescThrowing(TargetPointer methodDescPointer, out TargetPointer methodDescChunkPointer)
    {
        // may throw if the method desc at methodDescPointer is corrupted
        // we bypass the target data cache here because we don't want to cache non-validated data
        Data.MethodDesc desc = new Data.MethodDesc(_target, methodDescPointer);
        Data.MethodDescChunk chunk = GetMethodDescChunkThrowing(methodDescPointer, desc, out methodDescChunkPointer);
        return new NonValidated.MethodDesc(_target, desc, chunk);
    }

    private TargetCodePointer GetTemporaryEntryPointIfExists(NonValidated.MethodDesc umd)
    {
        if (!umd.TemporaryEntryPointAssigned || umd.CodeData == TargetPointer.Null)
        {
            return TargetCodePointer.Null;
        }
        Data.MethodDescCodeData codeData = _target.ProcessedData.GetOrAdd<Data.MethodDescCodeData>(umd.CodeData);
        return codeData.TemporaryEntryPoint;
    }

    private TargetPointer GetAddrOfNativeCodeSlot(TargetPointer methodDescPointer, NonValidated.MethodDesc umd)
    {
        uint offset = MethodDescAdditioinalPointersOffset(umd);
        offset += (uint)(_target.PointerSize * umd.NativeCodeSlotIndex);
        return methodDescPointer.Value + offset;
    }

    private TargetPointer GetAddresOfNonVtableSlot(TargetPointer methodDescPointer, NonValidated.MethodDesc umd)
    {
        uint offset = MethodDescAdditioinalPointersOffset(umd);
        offset += (uint)(_target.PointerSize * umd.NonVtableSlotIndex);
        return methodDescPointer.Value + offset;
    }

    private TargetCodePointer GetCodePointer(TargetPointer methodDescPointer, NonValidated.MethodDesc umd)
    {
        // TODO(cdac): _ASSERTE(!IsDefaultInterfaceMethod() || HasNativeCodeSlot());
        if (umd.HasNativeCodeSlot)
        {
            // When profiler is enabled, profiler may ask to rejit a code even though we
            // we have ngen code for this MethodDesc.  (See MethodDesc::DoPrestub).
            // This means that *ppCode is not stable. It can turn from non-zero to zero.
            TargetPointer ppCode = GetAddrOfNativeCodeSlot(methodDescPointer, umd);
            TargetCodePointer pCode = _target.ReadCodePointer(ppCode);

            // if arm32, set the thumb bit
            Data.PrecodeMachineDescriptor precodeMachineDescriptor = _target.ProcessedData.GetOrAdd<Data.PrecodeMachineDescriptor>(_target.ReadGlobalPointer(Constants.Globals.PrecodeMachineDescriptor));
            pCode = (TargetCodePointer)(pCode.Value | ~precodeMachineDescriptor.CodePointerToInstrPointerMask.Value);

            return pCode;
        }

        if (!umd.HasStableEntryPoint || umd.HasPrecode)
            return TargetCodePointer.Null;

        return GetStableEntryPoint(methodDescPointer, umd);
    }

    private TargetCodePointer GetStableEntryPoint(TargetPointer methodDescPointer, NonValidated.MethodDesc umd)
    {
        // TODO(cdac): _ASSERTE(HasStableEntryPoint());
        // TODO(cdac): _ASSERTE(!IsVersionableWithVtableSlotBackpatch());

        return GetMethodEntryPointIfExists(methodDescPointer, umd);
    }

    private TargetCodePointer GetMethodEntryPointIfExists(TargetPointer methodDescAddress, NonValidated.MethodDesc umd)
    {
        if (umd.HasNonVtableSlot)
        {
            TargetPointer pSlot = GetAddresOfNonVtableSlot(methodDescAddress, umd);

            return _target.ReadCodePointer(pSlot);
        }

        TargetPointer methodTablePointer = umd.MethodTable;
        TypeHandle typeHandle = GetTypeHandle(methodTablePointer);
        // TODO: cdac:  _ASSERTE(GetMethodTable()->IsCanonicalMethodTable());
        TargetPointer addrOfSlot = GetAddressOfSlot(typeHandle, umd.Slot);
        return _target.ReadCodePointer(addrOfSlot);
    }

    private uint MethodDescAdditioinalPointersOffset(NonValidated.MethodDesc umd)
    {
        MethodClassification cls = umd.Classification;
        switch (cls)
        {
            case MethodClassification.IL:
                return _target.GetTypeInfo(DataType.MethodDesc).Size ?? throw new InvalidOperationException("size of MethodDesc not known");
            case MethodClassification.FCall:
                throw new NotImplementedException();
            case MethodClassification.PInvoke:
                throw new NotImplementedException();
            case MethodClassification.EEImpl:
                throw new NotImplementedException();
            case MethodClassification.Array:
                throw new NotImplementedException();
            case MethodClassification.Instantiated:
                throw new NotImplementedException();
            case MethodClassification.ComInterop:
                throw new NotImplementedException();
            case MethodClassification.Dynamic:
                throw new NotImplementedException();
            default:
                throw new InvalidOperationException($"Unexpected method classification 0x{cls:x2} for MethodDesc");
        }
    }

    internal uint GetMethodDescBaseSize(NonValidated.MethodDesc umd)
    {
        uint baseSize = MethodDescAdditioinalPointersOffset(umd);
        baseSize += (uint)(_target.PointerSize * umd.AdditionalPointersCount);
        return baseSize;
    }

    private bool HasNativeCode(TargetPointer methodDescPointer, NonValidated.MethodDesc umd) => GetCodePointer(methodDescPointer, umd) != TargetCodePointer.Null;

    private bool ValidateMethodDescPointer(TargetPointer methodDescPointer, [NotNullWhen(true)] out TargetPointer methodDescChunkPointer)
    {
        methodDescChunkPointer = TargetPointer.Null;
        try
        {
            NonValidated.MethodDesc umd = GetMethodDescThrowing(methodDescPointer, out methodDescChunkPointer);
            TargetPointer methodTablePointer = umd.MethodTable;
            if (methodTablePointer == TargetPointer.Null
                || methodTablePointer == TargetPointer.Max64Bit
                || methodTablePointer == TargetPointer.Max32Bit)
            {
                return false;
            }
            TypeHandle typeHandle = GetTypeHandle(methodTablePointer);

            if (umd.Slot >= GetNumVtableSlots(typeHandle) && !umd.HasNonVtableSlot)
            {
                return false;
            }

            TargetCodePointer temporaryEntryPoint = GetTemporaryEntryPointIfExists(umd);
            if (temporaryEntryPoint != TargetCodePointer.Null)
            {
                Contracts.INativeCodePointers codePointers = _target.Contracts.NativeCodePointers;
                TargetPointer methodDesc = codePointers.MethodDescFromStubAddress(temporaryEntryPoint);
                if (methodDesc != methodDescPointer)
                {
                    return false;
                }
            }

            if (HasNativeCode(methodDescPointer, umd) && !umd.IsFCall)
            {
                TargetCodePointer jitCodeAddr = GetCodePointer(methodDescPointer, umd);
                //FIXME: this is the wrong code.

                TargetPointer methodDesc = _target.Contracts.NativeCodePointers.ExecutionManagerGetCodeMethodDesc(jitCodeAddr);
                if (methodDesc == TargetPointer.Null)
                {
                    return false;
                }
                if (methodDesc != methodDescPointer)
                {
                    return false;
                }
            }
        }
        catch (System.Exception)
        {
            // TODO(cdac): maybe don't swallow all exceptions? We could consider a richer contract that
            // helps to track down what sort of memory corruption caused the validation to fail.
            // TODO(cdac): we could also consider a more fine-grained exception type so we don't mask
            // programmer mistakes in cdacreader.
            return false;
        }
        return true;
    }

}
