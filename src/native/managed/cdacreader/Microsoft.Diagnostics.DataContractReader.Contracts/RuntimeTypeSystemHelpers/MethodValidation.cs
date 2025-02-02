// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

// GC Heap corruption may create situations where a pointer value may point to garbage or even
// to an unmapped memory region.
// All types here have not been validated as actually representing a MethodTable, EEClass, etc.
// All checks are unsafe and may throw if we access an invalid address in target memory.
internal sealed class MethodValidation
{
    internal interface IMethodTableQueries
    {
        TargetPointer GetAddressOfMethodTableSlot(TargetPointer methodTablePointer, uint slot);
        bool SlotIsVtableSlot(TargetPointer methodTablePointer, uint slot);
    }

    private sealed class NIEMethodTableQueries : IMethodTableQueries
    {
        public TargetPointer GetAddressOfMethodTableSlot(TargetPointer methodTablePointer, uint slot) =>  throw new NotImplementedException();

        public bool SlotIsVtableSlot(TargetPointer methodTablePointer, uint slot) => throw new NotImplementedException();

        internal static NIEMethodTableQueries s_Instance = new NIEMethodTableQueries();
    }

    private readonly Target _target;

    private readonly ulong _methodDescAlignment;

    private IMethodTableQueries _methodTableQueries;

    internal MethodValidation(Target target, ulong methodDescAlignment)
    {
        _target = target;
        _methodDescAlignment = methodDescAlignment;
        _methodTableQueries = NIEMethodTableQueries.s_Instance;
    }

    internal void SetMethodTableQueries(IMethodTableQueries methodTableQueries)
    {
        _methodTableQueries = methodTableQueries;
    }

    internal struct NonValidatedMethodDesc
    {
        private readonly Target _target;
        private readonly Data.MethodDesc _desc;
        private readonly Data.MethodDescChunk _chunk;

        internal TargetPointer Address { get; init; }

        internal NonValidatedMethodDesc(Target target, TargetPointer methodDescAddr, Data.MethodDesc desc, Data.MethodDescChunk chunk)
        {
            _target = target;
            _desc = desc;
            _chunk = chunk;

            Address = methodDescAddr;
        }

        private bool HasFlags(MethodDescFlags_1.MethodDescFlags flag) => (_desc.Flags & (ushort)flag) != 0;
        private bool HasFlags(MethodDescFlags_1.MethodDescEntryPointFlags flag) => (_desc.EntryPointFlags & (byte)flag) != 0;

        private bool HasFlags(MethodDescFlags_1.MethodDescFlags3 flag) => (_desc.Flags3AndTokenRemainder & (ushort)flag) != 0;

        internal byte ChunkIndex => _desc.ChunkIndex;
        internal TargetPointer MethodTable => _chunk.MethodTable;
        internal ushort Slot => _desc.Slot;
        internal bool HasNonVtableSlot => MethodDescOptionalSlots.HasNonVtableSlot(_desc.Flags);
        internal bool HasNativeCodeSlot => MethodDescOptionalSlots.HasNativeCodeSlot(_desc.Flags);

        internal bool TemporaryEntryPointAssigned => HasFlags(MethodDescFlags_1.MethodDescEntryPointFlags.TemporaryEntryPointAssigned);

        internal TargetPointer CodeData => _desc.CodeData;

        internal MethodClassification Classification => (MethodClassification)(_desc.Flags & (ushort)MethodDescFlags_1.MethodDescFlags.ClassificationMask);
        internal bool IsFCall => Classification == MethodClassification.FCall;

        internal TargetPointer GetAddressOfNonVtableSlot() => MethodDescOptionalSlots.GetAddressOfNonVtableSlot(Address, Classification, _desc.Flags, _target);
        internal TargetPointer GetAddressOfNativeCodeSlot() => MethodDescOptionalSlots.GetAddressOfNativeCodeSlot(Address, Classification, _desc.Flags, _target);

        internal bool HasStableEntryPoint => HasFlags(MethodDescFlags_1.MethodDescFlags3.HasStableEntryPoint);
        internal bool HasPrecode => HasFlags(MethodDescFlags_1.MethodDescFlags3.HasPrecode);
    }

    internal TargetPointer GetMethodDescChunkPointerThrowing(TargetPointer methodDescPointer, Data.MethodDesc umd)
    {
        ulong? methodDescChunkSize = _target.GetTypeInfo(DataType.MethodDescChunk).Size;
        if (!methodDescChunkSize.HasValue)
        {
            throw new InvalidOperationException("Target has no definite MethodDescChunk size");
        }
        // The runtime allocates a contiguous block of memory for a MethodDescChunk followed by MethodDescAlignment * Size bytes of space
        // that is filled with MethodDesc (or its subclasses) instances.  Each MethodDesc has a ChunkIndex that indicates its
        // offset from the end of the MethodDescChunk.
        ulong chunkAddress = (ulong)methodDescPointer - methodDescChunkSize.Value - umd.ChunkIndex * _methodDescAlignment;
        return new TargetPointer(chunkAddress);
    }

    private Data.MethodDescChunk GetMethodDescChunkThrowing(TargetPointer methodDescPointer, Data.MethodDesc md, out TargetPointer methodDescChunkPointer)
    {
        methodDescChunkPointer = GetMethodDescChunkPointerThrowing(methodDescPointer, md);
        return new Data.MethodDescChunk(_target, methodDescChunkPointer);
    }

    private NonValidatedMethodDesc GetMethodDescThrowing(TargetPointer methodDescPointer, out TargetPointer methodDescChunkPointer)
    {
        // may throw if the method desc at methodDescPointer is corrupted
        // we bypass the target data cache here because we don't want to cache non-validated data
        Data.MethodDesc desc = new Data.MethodDesc(_target, methodDescPointer);
        Data.MethodDescChunk chunk = GetMethodDescChunkThrowing(methodDescPointer, desc, out methodDescChunkPointer);
        return new NonValidatedMethodDesc(_target, methodDescPointer, desc, chunk);
    }

    private TargetCodePointer GetTemporaryEntryPointIfExists(NonValidatedMethodDesc umd)
    {
        if (!umd.TemporaryEntryPointAssigned || umd.CodeData == TargetPointer.Null)
        {
            return TargetCodePointer.Null;
        }
        Data.MethodDescCodeData codeData = _target.ProcessedData.GetOrAdd<Data.MethodDescCodeData>(umd.CodeData);
        return codeData.TemporaryEntryPoint;
    }

    private TargetCodePointer GetCodePointer(NonValidatedMethodDesc umd)
    {
        // TODO(cdac): _ASSERTE(!IsDefaultInterfaceMethod() || HasNativeCodeSlot());
        if (umd.HasNativeCodeSlot)
        {
            // When profiler is enabled, profiler may ask to rejit a code even though we
            // we have ngen code for this MethodDesc.  (See MethodDesc::DoPrestub).
            // This means that *ppCode is not stable. It can turn from non-zero to zero.
            TargetPointer ppCode = umd.GetAddressOfNativeCodeSlot();
            TargetCodePointer pCode = _target.ReadCodePointer(ppCode);

            return CodePointerUtils.CodePointerFromAddress(pCode.AsTargetPointer, _target);
        }

        if (!umd.HasStableEntryPoint || umd.HasPrecode)
            return TargetCodePointer.Null;

        return GetStableEntryPoint(umd);
    }

    private TargetCodePointer GetStableEntryPoint(NonValidatedMethodDesc umd)
    {
        Debug.Assert(umd.HasStableEntryPoint);
        // TODO(cdac): _ASSERTE(!IsVersionableWithVtableSlotBackpatch());

        return GetMethodEntryPointIfExists(umd);
    }


    private TargetCodePointer GetMethodEntryPointIfExists(NonValidatedMethodDesc umd)
    {
        if (umd.HasNonVtableSlot)
        {
            TargetPointer pSlot = umd.GetAddressOfNonVtableSlot();

            return _target.ReadCodePointer(pSlot);
        }

        TargetPointer methodTablePointer = umd.MethodTable;
        Debug.Assert(methodTablePointer != TargetPointer.Null);
        TargetPointer addrOfSlot = _methodTableQueries.GetAddressOfMethodTableSlot(methodTablePointer, umd.Slot);
        return _target.ReadCodePointer(addrOfSlot);
    }

    private bool HasNativeCode(NonValidatedMethodDesc umd) => GetCodePointer(umd) != TargetCodePointer.Null;

    internal bool ValidateMethodDescPointer(TargetPointer methodDescPointer, [NotNullWhen(true)] out TargetPointer methodDescChunkPointer)
    {
        methodDescChunkPointer = TargetPointer.Null;
        try
        {
            NonValidatedMethodDesc umd = GetMethodDescThrowing(methodDescPointer, out methodDescChunkPointer);
            TargetPointer methodTablePointer = umd.MethodTable;
            if (methodTablePointer == TargetPointer.Null
                || methodTablePointer == TargetPointer.Max64Bit
                || methodTablePointer == TargetPointer.Max32Bit)
            {
                return false;
            }

            if (!umd.HasNonVtableSlot && !_methodTableQueries.SlotIsVtableSlot(methodTablePointer, umd.Slot))
            {
                return false;
            }

            TargetCodePointer temporaryEntryPoint = GetTemporaryEntryPointIfExists(umd);
            if (temporaryEntryPoint != TargetCodePointer.Null)
            {
                Contracts.IPrecodeStubs precode = _target.Contracts.PrecodeStubs;
                TargetPointer methodDesc = precode.GetMethodDescFromStubAddress(temporaryEntryPoint);
                if (methodDesc != methodDescPointer)
                {
                    return false;
                }
            }

            if (HasNativeCode(umd) && !umd.IsFCall)
            {
                TargetCodePointer jitCodeAddr = GetCodePointer(umd);
                Contracts.IExecutionManager executionManager = _target.Contracts.ExecutionManager;
                CodeBlockHandle? codeInfo = executionManager.GetCodeBlockHandle(jitCodeAddr);
                if (!codeInfo.HasValue)
                {
                    return false;
                }
                TargetPointer methodDesc = executionManager.GetMethodDesc(codeInfo.Value);
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
