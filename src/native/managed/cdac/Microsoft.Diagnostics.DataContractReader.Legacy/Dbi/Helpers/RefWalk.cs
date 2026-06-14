// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// cDAC port of the native DacRefWalker.
/// </summary>
internal sealed class RefWalk : IEnum<DacGcReference>
{
    private const uint CDAC_DEFERRED_FRAME = 0x40000000;
    private readonly Target _target;
    private readonly IGC _gc;
    private readonly bool _walkStacks;
    private readonly CorGCReferenceType _handleWalkMask;

    public IEnumerator<DacGcReference> Enumerator { get; }
    public nuint LegacyHandle { get; set; } = 0;

    public RefWalk(Target target, bool walkStacks, CorGCReferenceType handleWalkMask)
    {
        _target = target;
        _gc = target.Contracts.GC;
        _walkStacks = walkStacks;
        _handleWalkMask = handleWalkMask;
        Enumerator = Walk().GetEnumerator();
    }

    private IEnumerable<DacGcReference> Walk()
    {
        // The single AppDomain pointer; used to fill vmDomain for both handle and stack references.
        TargetPointer appDomain = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.AppDomain));

        if (_handleWalkMask != 0)
        {
            foreach (DacGcReference reference in WalkHandles(appDomain))
                yield return reference;
        }

        if (_walkStacks)
        {
            foreach (DacGcReference reference in WalkStacks(appDomain))
                yield return reference;
        }
    }

    private IEnumerable<DacGcReference> WalkHandles(TargetPointer appDomain)
    {
        HandleType[] requestedTypes = GetRequestedHandleTypes();
        if (requestedTypes.Length == 0)
            yield break;

        foreach (HandleData handle in _gc.GetHandles(requestedTypes))
        {
            if (!TryMapHandle(handle, out CorGCReferenceType dwType, out ulong extraData))
                continue;
            yield return new DacGcReference
            {
                vmDomain = appDomain.Value,
                objHnd = handle.Handle.Value,
                dwType = dwType,
                i64ExtraData = extraData,
            };
        }
    }

    private HandleType[] GetRequestedHandleTypes()
    {
        // Mirror native DacRefWalker::GetHandleWalkerMask: translate the CorGCReferenceType bits
        // in the mask into the handle types consumed by IGC.GetHandles.
        List<HandleType> types = new();
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleStrong))
            types.Add(HandleType.Strong);
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleStrongPinning))
            types.Add(HandleType.Pinned);
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleWeakShort))
            types.Add(HandleType.WeakShort);
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleWeakLong))
            types.Add(HandleType.WeakLong);
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleWeakRefCount) || _handleWalkMask.HasFlag(CorGCReferenceType.CorHandleStrongRefCount))
            types.Add(HandleType.RefCounted);
        if (_handleWalkMask.HasFlag(CorGCReferenceType.CorHandleStrongDependent))
            types.Add(HandleType.Dependent);

        if (types.Count == 0)
            return [];

        // Only request types the target actually supports
        HashSet<HandleType> supported = new(_gc.GetSupportedHandleTypes());
        types.RemoveAll(t => !supported.Contains(t));
        return types.ToArray();
    }

    private bool TryMapHandle(HandleData handle, out CorGCReferenceType dwType, out ulong extraData)
    {
        extraData = 0;
        switch (_gc.GetHandleTypes([handle.Type])[0])
        {
            case HandleType.Strong:
                dwType = CorGCReferenceType.CorHandleStrong;
                return true;
            case HandleType.Pinned:
                dwType = CorGCReferenceType.CorHandleStrongPinning;
                return true;
            case HandleType.WeakShort:
                dwType = CorGCReferenceType.CorHandleWeakShort;
                return true;
            case HandleType.WeakLong:
                dwType = CorGCReferenceType.CorHandleWeakLong;
                return true;
            case HandleType.RefCounted:
                extraData = handle.RefCount;
                dwType = handle.RefCount != 0
                    ? CorGCReferenceType.CorHandleStrongRefCount
                    : CorGCReferenceType.CorHandleWeakRefCount;
                return true;
            case HandleType.Dependent:
                dwType = CorGCReferenceType.CorHandleStrongDependent;
                extraData = handle.Secondary.Value;
                return true;
            default:
                dwType = 0;
                return false;
        }
    }

    private IEnumerable<DacGcReference> WalkStacks(TargetPointer appDomain)
    {
        IThread threadContract = _target.Contracts.Thread;
        IStackWalk stackWalkContract = _target.Contracts.StackWalk;

        ThreadStoreData threadStore = threadContract.GetThreadStoreData();
        TargetPointer threadAddr = threadStore.FirstThread;
        while (threadAddr != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(threadAddr);

            foreach (StackReferenceData stackRef in stackWalkContract.WalkStackReferences(threadData))
            {
                // Skip cDAC-private deferred-frame markers; they are not real GC references.
                if ((stackRef.Flags & CDAC_DEFERRED_FRAME) != 0)
                    continue;

                DacGcReference reference = new()
                {
                    vmDomain = appDomain.Value,
                    dwType = CorGCReferenceType.CorReferenceStack,
                    i64ExtraData = 0,
                };

                // Interior pointers, Frame refs, and enregistered vars are reported as a direct object pointer with the low bit set;
                // everything else is reported by the address of the stack slot holding the object.
                if (stackRef.IsInteriorPointer || stackRef.Address == TargetPointer.Null)
                    reference.pObject = stackRef.Object.Value | 1;
                else
                    reference.objHnd = stackRef.Address.Value;

                yield return reference;
            }

            threadAddr = threadData.NextThread;
        }
    }
}
