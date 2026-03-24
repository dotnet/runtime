// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
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
    private readonly Target _target;
    private readonly IXCLRDataValue? _legacyImpl;
    private readonly uint _flags;
    private readonly ulong _totalSize;
    private readonly NativeVarLocation[] _locations;

    public ClrDataValue(
        Target target,
        uint flags,
        NativeVarLocation[] locations,
        IXCLRDataValue? legacyImpl)
    {
        _target = target;
        _legacyImpl = legacyImpl;
        _flags = flags;
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

    int IXCLRDataValue.SetBytes(uint bufLen, uint* dataSize, byte* buffer) => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetType(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance) => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetNumFields(uint* numFields) => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetFieldByIndex(
        uint index,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetNumFields2(uint flags, IXCLRDataTypeInstance? fromType, uint* numFields)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.StartEnumFields(uint flags, IXCLRDataTypeInstance? fromType, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EnumField(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EndEnumFields(ulong handle) => HResults.E_NOTIMPL;

    int IXCLRDataValue.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTypeInstance? fromType, ulong* handle)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> field, uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EndEnumFieldsByName(ulong handle) => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetFieldByToken(
        uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetAssociatedValue(DacComNullableByRef<IXCLRDataValue> assocValue)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetAssociatedType(DacComNullableByRef<IXCLRDataTypeInstance> assocType)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetString(uint bufLen, uint* strLen, char* str) => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetArrayProperties(uint* rank, uint* totalElements, uint numDim, uint* dims, uint numBases, int* bases)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetArrayElement(uint numInd, int* indices, DacComNullableByRef<IXCLRDataValue> value)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EnumField2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.EnumFieldByName2(
        ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetFieldByToken2(
        IXCLRDataModule? tokenScope,
        uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
        => HResults.E_NOTIMPL;

    int IXCLRDataValue.GetNumLocations(uint* numLocs)
    {
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
