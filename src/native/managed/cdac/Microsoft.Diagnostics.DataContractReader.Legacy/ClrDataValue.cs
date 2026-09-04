// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Describes a resolved physical location for a variable value.
/// A variable can span up to 2 locations (e.g., split across register and stack).
/// </summary>
public readonly struct NativeVarLocation
{
    public ulong AddressOrValue { get; init; }
    public ulong Size { get; init; }
    public bool IsRegisterValue { get; init; }
}

[GeneratedComClass]
public sealed unsafe partial class ClrDataValue : IXCLRDataValue
{
    private readonly Lock _apiLock;
    private readonly Target _target;
    private readonly TargetPointer _threadAddress;
    private readonly IXCLRDataValue? _legacyImpl;
    private readonly uint _flags;
    private readonly ITypeHandle? _typeHandle;
    private readonly TargetPointer _baseAddress;
    private readonly ulong _totalSize;
    private readonly NativeVarLocation[] _locations;

    public ClrDataValue(
        Target target,
        TargetPointer threadAddress,
        uint flags,
        ITypeHandle? typeHandle,
        TargetPointer baseAddress,
        NativeVarLocation[] locations,
        IXCLRDataValue? legacyImpl,
        Lock apiLock)
    {
        _apiLock = apiLock;
        _target = target;
        _threadAddress = threadAddress;
        _legacyImpl = legacyImpl;
        _flags = flags;
        _typeHandle = typeHandle;
        _baseAddress = baseAddress;
        _locations = locations;

        if (_locations.Length > 0 && (_flags & (uint)ClrDataValueFlag.IS_REFERENCE) != 0)
        {
            _totalSize = (ulong)_target.PointerSize;
        }
        else
        {
            _totalSize = 0;
            foreach (NativeVarLocation loc in _locations)
            {
                _totalSize += loc.Size;
            }
        }
    }

    int IXCLRDataValue.GetFlags(uint* flags)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            *flags = _flags;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint flagsLocal;
            int hrLocal = _legacyImpl.GetFlags(&flagsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
                Debug.Assert(*flags == flagsLocal, $"GetFlags cDAC: 0x{*flags:X}, DAC: 0x{flagsLocal:X}");
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetAddress(ClrDataAddress* address)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            *address = 0;
            if (_locations.Length != 1 || _locations[0].IsRegisterValue)
            {
                throw new InvalidCastException(); // E_NOINTERFACE
            }

            *address = _locations[0].AddressOrValue;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress addressLocal;
            int hrLocal = _legacyImpl.GetAddress(&addressLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
                Debug.Assert((ulong)*address == (ulong)addressLocal, $"GetAddress cDAC: 0x{(ulong)*address:X}, DAC: 0x{(ulong)addressLocal:X}");
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetSize(ulong* size)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            if (_totalSize == 0)
            {
                *size = 0;
                throw new InvalidCastException(); // E_NOINTERFACE
            }

            *size = _totalSize;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong sizeLocal;
            int hrLocal = _legacyImpl.GetSize(&sizeLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
                Debug.Assert(*size == sizeLocal, $"GetSize cDAC: {*size}, DAC: {sizeLocal}");
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetBytes(uint bufLen, uint* dataSize, byte* buffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            if (_totalSize == 0)
                throw new InvalidCastException(); // E_NOINTERFACE

            if (dataSize is not null)
                *dataSize = (uint)_totalSize;

            if (bufLen < _totalSize)
                throw Marshal.GetExceptionForHR(/*ERROR_BUFFER_OVERFLOW*/ CorDbgHResults.ERROR_BUFFER_OVERFLOW)!;

            byte* dst = buffer;
            foreach (NativeVarLocation loc in _locations)
            {
                if (loc.IsRegisterValue)
                {
                    int size = (int)loc.Size;
                    ulong value = loc.AddressOrValue;
                    for (int i = 0; i < size; i++)
                    {
                        dst[i] = (byte)(value & 0xFF);
                        value >>= 8;
                    }
                }
                else
                {
                    Span<byte> memBytes = new(dst, (int)loc.Size);
                    _target.ReadBuffer(loc.AddressOrValue, memBytes);
                }

                dst += loc.Size;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            byte[] legacyBuf = new byte[bufLen];
            uint legacyDataSize;
            int hrLocal;
            fixed (byte* pLegacy = legacyBuf)
            {
                hrLocal = _legacyImpl.GetBytes(bufLen, &legacyDataSize, pLegacy);
            }
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0 && hrLocal >= 0)
            {
                if (dataSize is not null)
                    Debug.Assert(*dataSize == legacyDataSize, $"GetBytes dataSize cDAC: {*dataSize}, DAC: {legacyDataSize}");

                int compareLen = (int)Math.Min(_totalSize, legacyDataSize);
                for (int i = 0; i < compareLen; i++)
                    Debug.Assert(buffer[i] == legacyBuf[i], $"GetBytes mismatch at byte {i}: cDAC: 0x{buffer[i]:X2}, DAC: 0x{legacyBuf[i]:X2}");
            }
        }
#endif

        return hr;
    }

    int IXCLRDataValue.SetBytes(uint bufLen, uint* dataSize, byte* buffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.GetType(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        IXCLRDataTypeInstance? legacyType = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataTypeInstance> legacyTypeOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetType(legacyTypeOut);
            if (hrLocal >= 0)
                legacyType = legacyTypeOut.Interface;
        }

        try
        {
            if ((_flags & (uint)ClrDataValueFlag.IS_REFERENCE) != 0)
            {
                typeInstance.Interface = null;
                hr = HResults.S_FALSE;
            }
            else if (_typeHandle is null)
            {
                hr = HResults.E_NOTIMPL;
            }
            else
            {
                typeInstance.Interface = new ClrDataTypeInstance(_target, _typeHandle, legacyType, _apiLock);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    int IXCLRDataValue.GetNumFields(uint* numFields)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.GetFieldByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;

        try
        {
            if (reqCode != (uint)CLRDataGeneralRequest.CLRDATA_REQUEST_REVISION
                || inBufferSize != 0
                || inBuffer is not null
                || outBufferSize != sizeof(uint))
            {
                throw new ArgumentException("Invalid request parameters.");
            }

            if (outBuffer is null)
                throw new NullReferenceException("The output buffer is null.");

            *(uint*)outBuffer = 3;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint revisionLocal = 0;
            int hrLocal = _legacyImpl.Request(
                reqCode,
                inBufferSize,
                inBuffer,
                outBufferSize,
                outBuffer is null ? null : (byte*)&revisionLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*(uint*)outBuffer == revisionLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetNumFields2(uint flags, IXCLRDataTypeInstance? fromType, uint* numFields)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            *numFields = 0;
            ValidateFieldFlags(flags);
            *numFields = (_flags & (uint)ClrDataValueFlag.IS_REFERENCE) != 0
                ? 0
                : checked((uint)GetFields(flags, fromType).Count);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numFieldsLocal = 0;
            int hrLocal = _legacyImpl.GetNumFields2(flags, GetLegacyType(fromType), &numFieldsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
                Debug.Assert(*numFields == numFieldsLocal, $"GetNumFields2 cDAC: {*numFields}, DAC: {numFieldsLocal}");
        }
#endif

        return hr;
    }

    int IXCLRDataValue.StartEnumFields(uint flags, IXCLRDataTypeInstance? fromType, ulong* handle)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return StartEnumFields(null, (uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_SENSITIVE, flags, fromType, handle);
    }

    int IXCLRDataValue.EnumField(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return EnumField(handle, field, nameBufLen, nameLen, nameBuf, token, byName: false);
    }

    int IXCLRDataValue.EndEnumFields(ulong handle)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return EndEnumFields(handle, byName: false);
    }

    int IXCLRDataValue.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTypeInstance? fromType, ulong* handle)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return StartEnumFields(name is null ? null : new string(name), nameFlags, fieldFlags, fromType, handle);
    }

    int IXCLRDataValue.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> field, uint* token)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return EnumField(handle, field, 0, null, null, token, byName: true);
    }

    int IXCLRDataValue.EndEnumFieldsByName(ulong handle)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return EndEnumFields(handle, byName: true);
    }

    int IXCLRDataValue.GetFieldByToken(
        uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.GetAssociatedValue(DacComNullableByRef<IXCLRDataValue> assocValue)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        IXCLRDataValue? legacyValue = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataValue> legacyValueOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetAssociatedValue(legacyValueOut);
            if (hrLocal >= 0)
                legacyValue = legacyValueOut.Interface;
        }

        try
        {
            if (_locations.Length == 0
                || (_flags & (uint)ClrDataValueFlag.IS_REFERENCE) == 0
                || _typeHandle is null)
            {
                throw new InvalidCastException();
            }

            NativeVarLocation currentLocation = _locations[0];
            ulong address = currentLocation.IsRegisterValue
                ? currentLocation.AddressOrValue
                : _target.ReadPointer(new TargetPointer(currentLocation.AddressOrValue)).Value;
            uint flags = GetTypeFieldValueFlags(_typeHandle, null, _flags & (uint)ClrDataValueFlag.ALL_LOCATIONS, isDeref: true);
            NativeVarLocation location = new()
            {
                AddressOrValue = address,
                Size = _target.Contracts.RuntimeTypeSystem.GetBaseSize(_typeHandle),
                IsRegisterValue = false,
            };

            assocValue.Interface = new ClrDataValue(_target, _threadAddress, flags, _typeHandle, address, [location], legacyValue, _apiLock);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    int IXCLRDataValue.GetAssociatedType(DacComNullableByRef<IXCLRDataTypeInstance> assocType)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        IXCLRDataTypeInstance? legacyType = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataTypeInstance> legacyTypeOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetAssociatedType(legacyTypeOut);
            if (hrLocal >= 0)
                legacyType = legacyTypeOut.Interface;
        }

        try
        {
            ITypeHandle? typeHandle = null;
            if ((_flags & (uint)ClrDataValueFlag.IS_REFERENCE) != 0)
            {
                typeHandle = _typeHandle;
            }
            else if ((_flags & (uint)ClrDataValueFlag.IS_ARRAY) != 0)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
                TargetPointer methodTable = _target.Contracts.Object.GetMethodTableAddress(_baseAddress);
                typeHandle = rts.GetTypeParam(rts.GetTypeHandle(methodTable));
            }

            if (typeHandle is null)
                throw new InvalidCastException();

            assocType.Interface = new ClrDataTypeInstance(_target, typeHandle, legacyType, _apiLock);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    int IXCLRDataValue.GetString(uint bufLen, uint* strLen, char* str)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            if ((_flags & (uint)ClrDataValueFlag.IS_STRING) == 0)
                throw new ArgumentException();

            string value = _target.Contracts.Object.GetStringValue(_baseAddress);
            OutputBufferHelpers.CopyStringToBuffer(str, bufLen, strLen, value);
            if (str is null || bufLen < value.Length + 1)
                hr = HResults.S_FALSE;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint strLenLocal = 0;
            char[] strLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* strLocalPtr = strLocal)
                hrLocal = _legacyImpl.GetString(bufLen, &strLenLocal, str is null ? null : strLocalPtr);
            Debug.ValidateHResult(hr, hrLocal);
            if (strLen is not null)
                Debug.Assert(*strLen == strLenLocal, $"GetString length cDAC: {*strLen}, DAC: {strLenLocal}");
            if (str is not null && hr >= 0)
            {
                fixed (char* strLocalPtr = strLocal)
                    Debug.Assert(new string(str) == new string(strLocalPtr));
            }
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetArrayProperties(uint* rank, uint* totalElements, uint numDim, [Out, MarshalUsing(CountElementName = nameof(numDim))] uint[] dims, uint numBases, [Out, MarshalUsing(CountElementName = nameof(numBases))] int[] bases)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            if ((_flags & (uint)ClrDataValueFlag.IS_ARRAY) == 0)
                throw new ArgumentException();

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            ITypeHandle arrayType = rts.GetTypeHandle(_target.Contracts.Object.GetMethodTableAddress(_baseAddress));
            rts.IsArray(arrayType, out uint arrayRank);
            _target.Contracts.Object.GetArrayData(_baseAddress, out uint count, out _, out _, out uint[] dimensionLengths, out int[] lowerBoundsValues);

            if (rank is not null)
                *rank = arrayRank;
            if (totalElements is not null)
                *totalElements = count;
            for (uint i = 0; i < Math.Min(numDim, arrayRank); i++)
                dims[i] = dimensionLengths[i];
            for (uint i = 0; i < Math.Min(numBases, arrayRank); i++)
                bases[i] = lowerBoundsValues[i];
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint rankLocal = 0;
            uint totalElementsLocal = 0;
            uint[] dimsLocal = new uint[numDim];
            int[] basesLocal = new int[numBases];
            int hrLocal = _legacyImpl.GetArrayProperties(rank is null ? null : &rankLocal, totalElements is null ? null : &totalElementsLocal, numDim, dimsLocal, numBases, basesLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(rank is null || *rank == rankLocal);
                Debug.Assert(totalElements is null || *totalElements == totalElementsLocal);
                Debug.Assert(dims.AsSpan().SequenceEqual(dimsLocal));
                Debug.Assert(bases.AsSpan().SequenceEqual(basesLocal));
            }
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetArrayElement(uint numInd, [In, MarshalUsing(CountElementName = nameof(numInd))] int[] indices, DacComNullableByRef<IXCLRDataValue> value)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        IXCLRDataValue? legacyValue = null;
        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null)
        {
            DacComNullableByRef<IXCLRDataValue> legacyValueOut = new(isNullRef: false);
            hrLocal = _legacyImpl.GetArrayElement(numInd, indices, legacyValueOut);
            if (hrLocal >= 0)
                legacyValue = legacyValueOut.Interface;
        }

        try
        {
            if ((_flags & (uint)ClrDataValueFlag.IS_ARRAY) == 0)
                throw new ArgumentException();

            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            ITypeHandle arrayType = rts.GetTypeHandle(_target.Contracts.Object.GetMethodTableAddress(_baseAddress));
            if (!rts.IsArray(arrayType, out uint rank) || numInd != rank)
                throw new ArgumentException();

            ITypeHandle elementType = rts.GetTypeParam(arrayType);
            TargetPointer data = _target.Contracts.Object.GetArrayData(_baseAddress, out _, out _, out _, out uint[] dimensionLengths, out int[] lowerBoundsValues);
            ulong offset = data.Value;
            ulong dimensionSize = rts.GetComponentSize(arrayType);

            for (uint dimension = rank; dimension-- > 0;)
            {
                int lowerBound = lowerBoundsValues[dimension];
                uint dimensionLength = dimensionLengths[dimension];
                if (indices[dimension] < lowerBound)
                    throw new ArgumentException();

                uint index = checked((uint)(indices[dimension] - lowerBound));
                if (index >= dimensionLength)
                    throw new ArgumentException();

                offset = checked(offset + (dimensionSize * index));
                dimensionSize = checked(dimensionSize * dimensionLength);
            }

            NativeVarLocation location = new()
            {
                AddressOrValue = offset,
                Size = GetTypeSize(elementType),
                IsRegisterValue = false,
            };
            uint flags = GetTypeFieldValueFlags(elementType, null, 0, isDeref: false);
            value.Interface = new ClrDataValue(_target, _threadAddress, flags, elementType, offset, [location], legacyValue, _apiLock);
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    private sealed class FieldEnumeration : IEnum<FieldEntry>
    {
        public IEnumerator<FieldEntry> Enumerator { get; }
        public nuint LegacyHandle { get; set; }

        public FieldEnumeration(IEnumerable<FieldEntry> fields, nuint legacyHandle)
        {
            Enumerator = fields.GetEnumerator();
            LegacyHandle = legacyHandle;
        }
    }

    private readonly record struct FieldEntry(TargetPointer FieldDesc, bool IsInherited);

    private int StartEnumFields(string? name, uint nameFlags, uint fieldFlags, IXCLRDataTypeInstance? fromType, ulong* handle)
    {
        int hr = HResults.S_OK;
        int hrLocal = HResults.S_OK;
        ulong legacyHandle = 0;
        if (_legacyImpl is not null)
        {
            IXCLRDataTypeInstance? legacyFromType = GetLegacyType(fromType);
            if (name is null)
            {
                hrLocal = _legacyImpl.StartEnumFields(fieldFlags, legacyFromType, &legacyHandle);
            }
            else
            {
                fixed (char* namePtr = name)
                    hrLocal = _legacyImpl.StartEnumFieldsByName(namePtr, nameFlags, fieldFlags, legacyFromType, &legacyHandle);
            }
        }

        try
        {
            if (handle is null)
                throw new ArgumentNullException(nameof(handle));
            *handle = 0;
            if (nameFlags > (uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_INSENSITIVE)
                throw new ArgumentException(nameof(nameFlags));

            ValidateFieldFlags(fieldFlags);
            List<FieldEntry> fields = GetFields(fieldFlags, fromType);
            if (name is not null)
            {
                int separatorIndex = name.LastIndexOf('.');
                string memberName = separatorIndex >= 0 ? name[(separatorIndex + 1)..] : name;
                StringComparison comparison = nameFlags == (uint)CLRDataByNameFlag.CLRDATA_BYNAME_CASE_INSENSITIVE
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
                fields = fields.Where(entry => string.Equals(GetFieldMetadata(entry.FieldDesc).Name, memberName, comparison)).ToList();
            }

            FieldEnumeration enumeration = new(fields, (nuint)legacyHandle);
            *handle = (ulong)((IEnum<FieldEntry>)enumeration).GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
            if (_legacyImpl is not null && hrLocal == HResults.S_OK)
            {
                if (name is null)
                    _legacyImpl.EndEnumFields(legacyHandle);
                else
                    _legacyImpl.EndEnumFieldsByName(legacyHandle);
            }
        }

#if DEBUG
        if (_legacyImpl is not null)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    private int EnumField(ulong* handle, DacComNullableByRef<IXCLRDataValue> field, uint nameBufLen, uint* nameLen, char* nameBuf, uint* token, bool byName)
    {
        int hr = HResults.S_OK;
        FieldEnumeration? enumeration = null;
        int hrLocal = HResults.S_OK;
        IXCLRDataValue? legacyField = null;
        uint nameLenLocal = 0;
        uint tokenLocal = 0;
        char[] nameBufLocal = new char[nameBufLen > 0 ? nameBufLen : 1];

        try
        {
            if (handle is null || *handle == 0)
                throw new ArgumentException("Invalid field handle.", nameof(handle));

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
            if (gcHandle.Target is not FieldEnumeration fields)
                throw new ArgumentException("Invalid field handle.", nameof(handle));
            enumeration = fields;

            if (_legacyImpl is not null && enumeration.LegacyHandle != 0)
            {
                ulong legacyHandle = (ulong)enumeration.LegacyHandle;
                DacComNullableByRef<IXCLRDataValue> legacyFieldOut = new(isNullRef: false);
                fixed (char* nameBufLocalPtr = nameBufLocal)
                {
                    hrLocal = byName
                        ? _legacyImpl.EnumFieldByName(&legacyHandle, legacyFieldOut, &tokenLocal)
                        : _legacyImpl.EnumField(&legacyHandle, legacyFieldOut, nameBufLen, &nameLenLocal, nameBuf is null ? null : nameBufLocalPtr, &tokenLocal);
                }
                enumeration.LegacyHandle = (nuint)legacyHandle;
                if (hrLocal >= 0)
                    legacyField = legacyFieldOut.Interface;
            }

            if (!enumeration.Enumerator.MoveNext())
            {
                hr = HResults.S_FALSE;
            }
            else
            {
                FieldEntry entry = enumeration.Enumerator.Current;
                (string fieldName, uint fieldToken, FieldDefinition fieldDefinition, ITypeHandle enclosingType) = GetFieldMetadata(entry.FieldDesc);
                OutputBufferHelpers.CopyStringToBuffer(nameBuf, nameBufLen, nameLen, fieldName);
                if (nameBuf is not null && nameBufLen != 0 && nameBufLen < fieldName.Length + 1)
                {
                    hr = unchecked((int)0x8007007A);
                }
                else
                {
                    if (token is not null)
                        *token = fieldToken;
                    if (!field.IsNullRef)
                        field.Interface = CreateFieldValue(entry, fieldDefinition, enclosingType, legacyField);
                }
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null && enumeration is not null)
        {
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);
            if (hr == HResults.S_OK && hrLocal >= 0)
            {
                Debug.Assert(token is null || *token == tokenLocal);
                Debug.Assert(nameLen is null || *nameLen == nameLenLocal);
                Debug.Assert(nameBuf is null || new ReadOnlySpan<char>(nameBuf, checked((int)nameLenLocal)).SequenceEqual(nameBufLocal.AsSpan(0, checked((int)nameLenLocal))));
            }
        }
#endif

        return hr;
    }

    private int EndEnumFields(ulong handle, bool byName)
    {
        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            if (handle != 0)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
                if (gcHandle.Target is not FieldEnumeration enumeration)
                    throw new ArgumentException("Invalid field handle.", nameof(handle));

                legacyHandle = enumeration.LegacyHandle;
                ((IEnum<FieldEntry>)enumeration).Dispose();
                gcHandle.Free();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

        int hrLocal = HResults.S_OK;
        if (_legacyImpl is not null && legacyHandle != 0)
        {
            hrLocal = byName
                ? _legacyImpl.EndEnumFieldsByName((ulong)legacyHandle)
                : _legacyImpl.EndEnumFields((ulong)legacyHandle);
        }

#if DEBUG
        if (_legacyImpl is not null && legacyHandle != 0)
            Debug.ValidateHResult(hr, hrLocal);
#endif

        return hr;
    }

    private static void ValidateFieldFlags(uint flags)
    {
        if ((flags & ~(uint)ClrDataValueFlag.ALL_FIELDS) != 0
            || (flags & (uint)ClrDataValueFlag.ALL_KINDS) != (uint)ClrDataValueFlag.ALL_KINDS
            || (flags & (uint)ClrDataValueFlag.ALL_LOCATIONS) == 0)
        {
            throw new ArgumentException(nameof(flags));
        }
    }

    private List<FieldEntry> GetFields(uint flags, IXCLRDataTypeInstance? fromType)
    {
        bool includeParents = (flags & (uint)ClrDataValueFlag.IS_INHERITED) != 0;
        if (includeParents && fromType is not null)
            throw new ArgumentException(nameof(fromType));

        ITypeHandle? typeHandle = !includeParents && fromType is ClrDataTypeInstance fromTypeInstance
            ? fromTypeInstance.TypeHandle
            : _typeHandle;
        if (typeHandle is null)
            throw new ArgumentException(nameof(flags));

        bool includeInstanceFields = (flags & (uint)ClrDataValueFlag.FROM_INSTANCE) != 0;
        bool includeStaticFields = (flags & (uint)ClrDataValueFlag.FROM_STATIC) != 0;
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        List<ITypeHandle> types = [];
        ITypeHandle current = typeHandle;
        do
        {
            types.Add(current);
            TargetPointer parent = includeParents ? rts.GetParentMethodTable(current) : TargetPointer.Null;
            if (parent == TargetPointer.Null)
                break;
            current = rts.GetTypeHandle(parent);
        }
        while (true);

        types.Reverse();
        List<FieldEntry> fields = [];
        for (int i = 0; i < types.Count; i++)
        {
            bool inherited = i != types.Count - 1;
            foreach (TargetPointer fieldDesc in rts.GetFieldDescList(types[i]))
            {
                bool isStatic = rts.IsFieldDescStatic(fieldDesc) || rts.IsFieldDescThreadStatic(fieldDesc);
                if ((isStatic && includeStaticFields) || (!isStatic && includeInstanceFields))
                    fields.Add(new FieldEntry(fieldDesc, inherited));
            }
        }
        return fields;
    }

    private (string Name, uint Token, FieldDefinition Definition, ITypeHandle EnclosingType) GetFieldMetadata(TargetPointer fieldDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        ITypeHandle enclosingType = rts.GetTypeHandle(rts.GetMTOfEnclosingClass(fieldDesc));
        TargetPointer module = rts.GetModule(enclosingType);
        Contracts.ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandleFromModulePtr(module);
        MetadataReader metadata = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle) ?? throw new NotImplementedException();
        uint token = rts.GetFieldDescMemberDef(fieldDesc);
        FieldDefinition definition = metadata.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle((int)EcmaMetadataUtils.GetRowId(token)));
        return (metadata.GetString(definition.Name), token, definition, enclosingType);
    }

    private ClrDataValue CreateFieldValue(
        FieldEntry entry,
        FieldDefinition fieldDefinition,
        ITypeHandle enclosingType,
        IXCLRDataValue? legacyValue)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TargetPointer fieldDesc = entry.FieldDesc;
        ITypeHandle? fieldType = rts.GetFieldDescApproxTypeHandle(fieldDesc);
        CorElementType fieldElementType = rts.GetFieldDescType(fieldDesc);
        if (fieldType is null && !rts.IsCorElementTypeObjRef(fieldElementType))
            throw new ArgumentException();

        NativeVarLocation[] locations;
        ulong baseAddress;
        if (rts.ContainsGenericVariables(enclosingType))
        {
            locations = [];
            baseAddress = 0;
        }
        else
        {
            TargetPointer address;
            if (rts.IsFieldDescThreadStatic(fieldDesc))
            {
                if (_threadAddress == TargetPointer.Null)
                    throw new ArgumentException();

                address = rts.GetFieldDescThreadStaticAddress(fieldDesc, _threadAddress, unboxValueTypes: false);
            }
            else if (rts.IsFieldDescStatic(fieldDesc))
            {
                address = rts.GetFieldDescStaticAddress(fieldDesc, unboxValueTypes: false);
            }
            else
            {
                uint offset = rts.GetFieldDescOffset(fieldDesc, fieldDefinition);
                ulong objectOffset = rts.IsValueType(enclosingType) ? 0 : (ulong)_target.PointerSize;
                address = new TargetPointer(checked(_baseAddress + objectOffset + offset));
            }

            baseAddress = address.Value;
            locations =
            [
                new NativeVarLocation
                {
                    AddressOrValue = address.Value,
                    Size = fieldType is null
                        ? (ulong)_target.PointerSize
                        : GetTypeSize(fieldType),
                    IsRegisterValue = false,
                },
            ];
        }

        uint flags = entry.IsInherited ? (uint)ClrDataValueFlag.IS_INHERITED : 0;
        flags = GetTypeFieldValueFlags(fieldType, fieldDesc, flags, isDeref: false, fieldDefinition);
        return new ClrDataValue(_target, _threadAddress, flags, fieldType, baseAddress, locations, legacyValue, _apiLock);
    }

    private uint GetTypeFieldValueFlags(
        ITypeHandle? typeHandle,
        TargetPointer? fieldDesc,
        uint otherFlags,
        bool isDeref,
        FieldDefinition fieldDefinition = default)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        otherFlags &= ~(uint)ClrDataValueFlag.ALL_KINDS;

        CorElementType elementType = fieldDesc is TargetPointer field
            ? rts.GetFieldDescType(field)
            : rts.GetInternalCorElementType(typeHandle!);

        if (!isDeref && rts.IsCorElementTypeObjRef(elementType))
            otherFlags |= (uint)ClrDataValueFlag.IS_REFERENCE;
        else if (typeHandle is not null && rts.IsEnum(typeHandle))
            otherFlags |= (uint)ClrDataValueFlag.IS_ENUM;
        else if (elementType == CorElementType.String)
            otherFlags |= (uint)ClrDataValueFlag.IS_STRING;
        else if (elementType == CorElementType.Ptr)
            otherFlags |= (uint)ClrDataValueFlag.IS_POINTER;
        else if (IsPrimitive(elementType))
            otherFlags |= (uint)ClrDataValueFlag.IS_PRIMITIVE;
        else if (typeHandle is not null && rts.IsArray(typeHandle, out _))
            otherFlags |= (uint)ClrDataValueFlag.IS_ARRAY;
        else if (typeHandle is not null && rts.IsValueType(typeHandle))
            otherFlags |= (uint)ClrDataValueFlag.IS_VALUE_TYPE;
        else if (elementType == CorElementType.Class
            && typeHandle is not null
            && typeHandle.Address == rts.GetWellKnownMethodTable(WellKnownMethodTable.String))
        {
            otherFlags |= (uint)ClrDataValueFlag.IS_STRING;
        }

        if (fieldDesc is TargetPointer fieldPointer)
        {
            otherFlags &= ~((uint)ClrDataValueFlag.IS_LITERAL
                | (uint)ClrDataValueFlag.FROM_INSTANCE
                | (uint)ClrDataValueFlag.FROM_TASK_LOCAL
                | (uint)ClrDataValueFlag.FROM_STATIC);

            if ((isDeref || (otherFlags & (uint)ClrDataValueFlag.IS_REFERENCE) == 0)
                && (fieldDefinition.Attributes & FieldAttributes.Literal) != 0)
            {
                otherFlags |= (uint)ClrDataValueFlag.IS_LITERAL;
            }

            if (rts.IsFieldDescStatic(fieldPointer))
                otherFlags |= (uint)ClrDataValueFlag.FROM_STATIC;
            else if (rts.IsFieldDescThreadStatic(fieldPointer))
                otherFlags |= (uint)ClrDataValueFlag.FROM_TASK_LOCAL;
            else
                otherFlags |= (uint)ClrDataValueFlag.FROM_INSTANCE;
        }

        return otherFlags;
    }

    private ulong GetTypeSize(ITypeHandle typeHandle)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        CorElementType elementType = rts.GetInternalCorElementType(typeHandle);
        return elementType switch
        {
            CorElementType.ValueType => rts.GetNumInstanceFieldBytes(typeHandle),
            _ when elementType != CorElementType.Void && IsPrimitive(elementType)
                => rts.GetNumInstanceFieldBytes(typeHandle),
            _ => (ulong)_target.PointerSize,
        };
    }

    private static bool IsPrimitive(CorElementType elementType) =>
        elementType is >= CorElementType.Void and <= CorElementType.R8
            or CorElementType.I
            or CorElementType.U;

    private static IXCLRDataTypeInstance? GetLegacyType(IXCLRDataTypeInstance? typeInstance) =>
        typeInstance is ClrDataTypeInstance managedType ? managedType.LegacyImpl : typeInstance;

    int IXCLRDataValue.EnumField2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.EnumFieldByName2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.GetFieldByToken2(
        IXCLRDataModule? tokenScope,
        uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
    {
        using Lock.Scope scope = _apiLock.EnterScope();

        return HResults.E_NOTIMPL;
    }

    int IXCLRDataValue.GetNumLocations(uint* numLocs)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            *numLocs = (uint)_locations.Length;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint numLocsLocal;
            int hrLocal = _legacyImpl.GetNumLocations(&numLocsLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
                Debug.Assert(*numLocs == numLocsLocal, $"GetNumLocations cDAC: {*numLocs}, DAC: {numLocsLocal}");
        }
#endif

        return hr;
    }

    int IXCLRDataValue.GetLocationByIndex(uint loc, uint* flags, ClrDataAddress* arg)
    {
        using Lock.Scope scope = _apiLock.EnterScope();
        int hr = HResults.S_OK;
        try
        {
            *flags = 0;
            *arg = 0;

            if (loc >= (uint)_locations.Length)
                throw new ArgumentException();

            NativeVarLocation location = _locations[loc];
            *flags = location.IsRegisterValue ? ClrDataVLocFlag.CLRDATA_VLOC_REGISTER : ClrDataVLocFlag.CLRDATA_VLOC_MEMORY;
            *arg = location.IsRegisterValue ? 0 : location.AddressOrValue;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint flagsLocal;
            ClrDataAddress argLocal;
            int hrLocal = _legacyImpl.GetLocationByIndex(loc, &flagsLocal, &argLocal);
            Debug.ValidateHResult(hr, hrLocal);
            if (hr >= 0)
            {
                Debug.Assert(*flags == flagsLocal, $"GetLocationByIndex[{loc}] flags cDAC: {*flags}, DAC: {flagsLocal}");
                // Address comparison is best-effort: the native DAC does not handle REGNUM_AMBIENT_SP
                // on AMD64 (returns garbage from GetRegOffsInCONTEXT's default case), so addresses may
                // legitimately differ for variables stored relative to the ambient stack pointer.
                if ((ulong)*arg != (ulong)argLocal)
                {
                    Debug.WriteLine($"GetLocationByIndex[{loc}] addr divergence - cDAC: 0x{(ulong)*arg:X}, DAC: 0x{(ulong)argLocal:X}");
                }
            }
        }
#endif

        return hr;
    }
}
