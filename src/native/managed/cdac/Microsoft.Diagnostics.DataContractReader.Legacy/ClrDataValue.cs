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
    private readonly uint _flags;
    private readonly ulong _totalSize;
    private readonly NativeVarLocation[] _locations;

    public ClrDataValue(
        Target target,
        uint flags,
        NativeVarLocation[] locations)
    {
        _target = target;
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
    {
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


        return hr;
    }

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


        return hr;
    }
}
