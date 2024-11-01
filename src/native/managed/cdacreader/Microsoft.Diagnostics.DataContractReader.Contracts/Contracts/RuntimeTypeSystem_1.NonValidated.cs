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
