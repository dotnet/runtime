// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

// DispatchMap uses the MSB-first integer encoding from decodemd.cpp, not GCRefMap's bitstream.
internal struct DispatchMapDecoder(Target target, TargetPointer address)
{
    private TargetPointer _address = address;
    private int _remainingBits;
    private byte _currentByte;

    private uint ReadBit()
    {
        if (_remainingBits == 0)
        {
            _currentByte = target.Read<byte>(_address);
            _address = new(checked(_address.Value + 1));
            _remainingBits = 8;
        }
        return (uint)(_currentByte >> --_remainingBits) & 1;
    }

    internal uint Read()
    {
        ReadOnlySpan<uint> bases = [0, 1, 3, 11, 139, 32907];
        int ones = 0;
        while (ReadBit() != 0)
        {
            if (++ones == bases.Length)
                throw new InvalidOperationException("Invalid DispatchMap integer prefix.");
        }
        uint payload = 0;
        for (int bits = (1 << ones) - 1; bits > 0; bits--)
            payload = (payload << 1) | ReadBit();
        return checked(bases[ones] + payload);
    }
}

internal sealed class MethodGCRefMapLookup(Target target, FrameHelpers frames)
{
    private const uint HasDispatchMapSlot = 0x4;
    private readonly IRuntimeTypeSystem _rts = target.Contracts.RuntimeTypeSystem;

    internal TargetPointer Find(MethodDescHandle method, TargetPointer receiver, bool dispatch)
    {
        TargetPointer map = dispatch ? GetShareableGCRefMap(method.Address) : frames.GetGCRefMapFromMethodDesc(method.Address);
        if (map != TargetPointer.Null)
            return map;

        if (_rts.GetGenericMethodInstantiation(method).Length != 0 ||
            (!dispatch && _rts.GetGenericContextLoc(method) is GenericContextLoc.InstArgMethodDesc or GenericContextLoc.InstArgMethodTable))
            return TargetPointer.Null;

        Data.MethodDesc data = target.ProcessedData.GetOrAdd<Data.MethodDesc>(method.Address);
        if (((MethodDescFlags_1.MethodDescFlags)data.Flags & MethodDescFlags_1.MethodDescFlags.Static) != 0)
            return TargetPointer.Null;

        TargetPointer owner = _rts.GetMethodTable(method);
        ushort slot = _rts.GetSlotNumber(method);
        map = FindGCRefMapForSlot(owner, slot);
        if (map != TargetPointer.Null)
            return map;
        if (receiver == TargetPointer.Null)
            receiver = owner;
        if (IsInterface(GetMethodTable(receiver)))
            return TargetPointer.Null;
        MethodDescHandle implementation = method;
        if (IsInterface(GetMethodTable(owner)))
        {
            TargetPointer resolved = ResolveInterfaceTarget(owner, slot, receiver);
            if (resolved == TargetPointer.Null)
                return TargetPointer.Null;
            implementation = _rts.GetMethodDescHandle(resolved);
            map = FindGCRefMapForSlot(_rts.GetMethodTable(implementation), _rts.GetSlotNumber(implementation));
            if (map != TargetPointer.Null)
                return map;
        }

        for (TargetPointer current = receiver; current != TargetPointer.Null; current = GetMethodTable(current).ParentMethodTable)
        {
            Data.MethodTable mt = GetMethodTable(current);
            TargetPointer dispatchMap = GetDispatchMap(mt);
            if (dispatchMap == TargetPointer.Null)
                continue;

            DispatchMapDecoder decoder = new(target, dispatchMap);
            uint header = decoder.Read();
            if ((header & 1) != 0)
            {
                uint count = decoder.Read();
                for (uint i = 0; i < count; i++)
                {
                    ushort source = checked((ushort)decoder.Read());
                    ushort destination = checked((ushort)decoder.Read());
                    if (!Matches(receiver, source, implementation) && !Matches(receiver, destination, implementation))
                        continue;
                    map = FindGCRefMapForSlot(current, source);
                    if (map == TargetPointer.Null)
                        map = FindGCRefMapForSlot(current, destination);
                    if (map != TargetPointer.Null)
                        return map;
                }
            }

            foreach ((uint typeID, ushort source, ushort destination) in GetInterfaceEntries(mt))
            {
                if (!Matches(receiver, destination, implementation))
                    continue;
                map = FindGCRefMapForSlot(GetInterface(mt, typeID), source);
                if (map != TargetPointer.Null)
                    return map;
            }
        }
        return TargetPointer.Null;
    }

    private TargetPointer GetInterface(Data.MethodTable mt, uint typeID)
    {
        TargetPointer interfaceMap = mt.InterfaceMap ??
            throw new InvalidOperationException("MethodTable.InterfaceMap is required for virtual GC reference maps.");
        return target.ReadPointer(interfaceMap + checked((ulong)(typeID - 1) * (ulong)target.PointerSize));
    }

    private TargetPointer ResolveInterfaceTarget(TargetPointer owner, ushort slot, TargetPointer receiver)
    {
        for (TargetPointer current = receiver; current != TargetPointer.Null; current = GetMethodTable(current).ParentMethodTable)
        {
            Data.MethodTable mt = GetMethodTable(current);
            foreach ((uint typeID, ushort source, ushort destination) in GetInterfaceEntries(mt))
            {
                if (source != slot || GetInterface(mt, typeID) != owner)
                    continue;
                TargetPointer implementation = _rts.GetMethodDescForSlot(_rts.GetTypeHandle(receiver), destination);
                if (implementation != TargetPointer.Null)
                    return implementation;
            }
        }
        return TargetPointer.Null;
    }

    private IEnumerable<(uint TypeID, ushort Source, ushort Target)> GetInterfaceEntries(Data.MethodTable mt)
    {
        TargetPointer map = GetDispatchMap(mt);
        if (map == TargetPointer.Null)
            yield break;
        DispatchMapDecoder decoder = new(target, map);
        uint header = decoder.Read();
        uint numTypes = header >> 1;
        if ((header & 1) != 0)
        {
            uint aliases = decoder.Read();
            for (uint i = 0; i < aliases; i++)
            {
                decoder.Read();
                decoder.Read();
            }
        }
        long typeID = -1;
        for (uint type = 0; type < numTypes; type++)
        {
            typeID = checked(typeID + decoder.Read() + 1);
            uint count = decoder.Read();
            uint hasNegatives = decoder.Read();
            if (hasNegatives > 1 || typeID <= 0 || typeID > mt.NumInterfaces || count == 0)
                throw new InvalidOperationException("Invalid DispatchMap interface header.");
            long source = -1;
            long destination = -1;
            for (uint entry = 0; entry < count; entry++)
            {
                source = checked(source + decoder.Read() + 1);
                uint delta = decoder.Read();
                long signedDelta = hasNegatives == 0 ? delta : (delta & 1) != 0 ? (delta + 1L) / 2 : -(long)delta / 2;
                destination = checked(destination + signedDelta + 1);
                yield return (checked((uint)typeID), checked((ushort)source), checked((ushort)destination));
            }
        }
    }

    private Data.MethodTable GetMethodTable(TargetPointer address)
        => target.ProcessedData.GetOrAdd<Data.MethodTable>(address);

    private static bool IsInterface(Data.MethodTable mt)
        => new MethodTableFlags_1 { MTFlags = mt.MTFlags }.IsInterface;

    private bool Matches(TargetPointer receiver, ushort source, MethodDescHandle implementation)
    {
        TargetPointer candidate = _rts.GetMethodDescForSlot(_rts.GetTypeHandle(receiver), source);
        if (candidate == TargetPointer.Null)
            return false;
        if (candidate == implementation.Address)
            return true;

        MethodDescHandle method = _rts.GetMethodDescHandle(candidate);
        ITypeHandle owner = _rts.GetTypeHandle(_rts.GetMethodTable(method));
        ITypeHandle implementationOwner = _rts.GetTypeHandle(_rts.GetMethodTable(implementation));
        if (_rts.GetCanonicalMethodTable(owner) != _rts.GetCanonicalMethodTable(implementationOwner) ||
            _rts.GetModule(owner) != _rts.GetModule(implementationOwner) ||
            _rts.GetMethodToken(method) != _rts.GetMethodToken(implementation))
            return false;

        const AsyncMethodFlags VariantMask = AsyncMethodFlags.IsAsyncVariant | AsyncMethodFlags.ReturnDroppingThunk;
        return (_rts.GetAsyncMethodFlags(method) & VariantMask) == (_rts.GetAsyncMethodFlags(implementation) & VariantMask);
    }

    private TargetPointer GetDispatchMap(Data.MethodTable mt)
    {
        if ((mt.MTFlags2 & HasDispatchMapSlot) == 0)
        {
            TargetPointer canonical = _rts.GetCanonicalMethodTable(_rts.GetTypeHandle(mt.Address));
            if (canonical == mt.Address)
                return TargetPointer.Null;
            mt = GetMethodTable(canonical);
        }
        if ((mt.MTFlags2 & HasDispatchMapSlot) == 0)
            return TargetPointer.Null;
        uint size = target.GetTypeInfo(DataType.MethodTableAuxiliaryData).Size ??
            throw new InvalidOperationException("MethodTableAuxiliaryData size is required for dispatch map lookup.");
        return mt.AuxiliaryData + size;
    }

    private TargetPointer FindGCRefMapForSlot(TargetPointer owner, ushort slot)
    {
        Data.MethodTable mt = GetMethodTable(owner);
        while (true)
        {
            foreach (TargetPointer candidate in _rts.GetIntroducedMethodDescs(_rts.GetTypeHandle(mt.Address)))
            {
                MethodDescHandle method = _rts.GetMethodDescHandle(candidate);
                if (_rts.GetSlotNumber(method) != slot || _rts.IsUnboxingStub(method))
                    continue;
                TargetPointer map = GetShareableGCRefMap(candidate);
                if (map != TargetPointer.Null)
                    return map;
            }
            if (IsInterface(mt) || slot >= mt.NumVirtuals || mt.ParentMethodTable == TargetPointer.Null)
                return TargetPointer.Null;
            mt = GetMethodTable(mt.ParentMethodTable);
            if (slot >= mt.NumVirtuals)
                return TargetPointer.Null;
        }
    }

    private TargetPointer GetShareableGCRefMap(TargetPointer method)
    {
        MethodDescHandle md = _rts.GetMethodDescHandle(method);
        if (_rts.GetGenericMethodInstantiation(md).Length != 0 ||
            _rts.GetGenericContextLoc(md) is GenericContextLoc.InstArgMethodDesc or GenericContextLoc.InstArgMethodTable)
            return TargetPointer.Null;
        return frames.GetGCRefMapFromMethodDesc(method);
    }
}
