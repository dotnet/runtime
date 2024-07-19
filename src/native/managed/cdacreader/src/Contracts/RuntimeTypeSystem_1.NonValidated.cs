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

            internal byte ChunkIndex => _desc.ChunkIndex;
            internal TargetPointer MethodTable => _chunk.MethodTable;
            internal ushort Slot => _desc.Slot;
            internal bool HasNonVtableSlot => HasFlag(MethodDescFlags.HasNonVtableSlot);
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
        // The runtime allocates a contiguous block of memory for a MethodDescChunk followedd by MethodDescAlignment * Size bytes of space
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

    private bool ValidateMethodDescPointer(TargetPointer methodDescPointer, [NotNullWhen(true)] out TargetPointer methodDescChunkPointer)
    {
        methodDescChunkPointer = TargetPointer.Null;
        try
        {
            NonValidated.MethodDesc umd = GetMethodDescThrowing(methodDescPointer, out methodDescChunkPointer);
            TargetPointer methodTablePointer = umd.MethodTable;
            if (methodTablePointer == TargetPointer.Null
                || methodTablePointer == new TargetPointer(0xffffffff_fffffffful)
                || methodTablePointer == new TargetPointer(0x00000000_fffffffful))
            {
                return false;
            }
            TypeHandle typeHandle = GetTypeHandle(methodTablePointer);

            if (umd.Slot >= GetNumVtableSlots(typeHandle) && !umd.HasNonVtableSlot)
            {
                return false;
            }
            // TODO: request.cpp
            // TODO[cdac]: this needs a Precode lookup
            // see MethodDescChunk::GetTemporaryEntryPoint
#if false
            MethodDesc *pMDCheck = MethodDesc::GetMethodDescFromStubAddr(pMD->GetTemporaryEntryPoint(), TRUE);

            if (PTR_HOST_TO_TADDR(pMD) != PTR_HOST_TO_TADDR(pMDCheck))
            {
                retval = FALSE;
            }
#endif

            // TODO: request.cpp
            // TODO[cdac]: needs MethodDesc::GetNativeCode and MethodDesc::GetMethodEntryPoint()
#if false
        if (retval && pMD->HasNativeCode() && !pMD->IsFCall())
        {
            PCODE jitCodeAddr = pMD->GetNativeCode();

            MethodDesc *pMDCheck = ExecutionManager::GetCodeMethodDesc(jitCodeAddr);
            if (pMDCheck)
            {
                // Check that the given MethodDesc matches the MethodDesc from
                // the CodeHeader
                if (PTR_HOST_TO_TADDR(pMD) != PTR_HOST_TO_TADDR(pMDCheck))
                {
                    retval = FALSE;
                }
            }
            else
            {
                retval = FALSE;
            }
        }
#endif

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
