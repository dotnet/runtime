// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Validation-shim proxy. Each method calls the production cDAC first (its result is always the one
// returned to the caller), then calls the legacy DAC and compares. The `#if DEBUG` comparison blocks
// are the pre-refactor cDAC blocks, recovered verbatim from the implementations that hosted the
// legacy DAC before the production decoupling; `hr` is the production cDAC result and the `_legacy*`
// fields are the legacy DAC's interfaces, exactly as they were in the original code.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Paired cDAC/DAC proxy for IXCLRDataValue.
/// </summary>
[GeneratedComClass]
internal sealed unsafe partial class ClrDataValueProxy
    : ShimProxy, ICustomQueryInterface, IXCLRDataValue
{
    private readonly IXCLRDataValue? _cdacImpl;
    private readonly IXCLRDataValue? _legacyImpl;

    internal ClrDataValueProxy(ValidationSession session, object? cdacObject, object? dacObject)
        : base(session, cdacObject, dacObject)
    {
        _cdacImpl = cdacObject as IXCLRDataValue;
        _legacyImpl = dacObject as IXCLRDataValue;
    }

    /// <summary>
    /// Mirrors the production cDAC object's QueryInterface surface exactly: an interface is only
    /// exposed to the caller when the object being proxied exposes it, so consumers cannot observe
    /// a capability the cDAC does not actually have.
    /// </summary>
    public CustomQueryInterfaceResult GetInterface(ref Guid iid, out nint ppv)
    {
        ppv = default;
        CustomQueryInterfaceResult? custom = null;
        GetCustomInterface(ref iid, ref ppv, ref custom);
        if (custom is not null)
            return custom.Value;

        if (iid == typeof(IXCLRDataValue).GUID)
            return Support(_cdacImpl, _legacyImpl);

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>Hook for proxies that hand out a paired object of a different type (see ClrDataModuleProxy).</summary>
    partial void GetCustomInterface(ref Guid iid, ref nint ppv, ref CustomQueryInterfaceResult? result);

    #region IXCLRDataValue
    int IXCLRDataValue.GetFlags(uint* flags)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFlags(flags) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAddress(address) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetSize(size) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetBytes(bufLen, dataSize, buffer) : HResults.E_NOTIMPL;
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

                // The pre-refactor cDAC bounded the comparison by its internal _totalSize, which on
                // success equals the value it wrote to *dataSize. The shim uses that reported size
                // when available, otherwise the DAC's size (the two are asserted equal above).
                ulong cdacTotal = dataSize is not null ? *dataSize : legacyDataSize;
                int compareLen = (int)Math.Min(cdacTotal, legacyDataSize);
                for (int i = 0; i < compareLen; i++)
                    Debug.Assert(buffer[i] == legacyBuf[i], $"GetBytes mismatch at byte {i}: cDAC: 0x{buffer[i]:X2}, DAC: 0x{legacyBuf[i]:X2}");
            }
        }
#endif
        return hr;
    }

    int IXCLRDataValue.SetBytes(uint bufLen, uint* dataSize, byte* buffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.SetBytes(bufLen, dataSize, buffer) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetType(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetType(typeInstance) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetNumFields(uint* numFields)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumFields(numFields) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetFieldByIndex(uint index,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldByIndex(index, field, bufLen, nameLen, nameBuf, token) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;
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
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumFields2(flags, ShimProxy.UnwrapCDac<IXCLRDataTypeInstance>(fromType), numFields) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.StartEnumFields(uint flags, IXCLRDataTypeInstance? fromType, ulong* handle)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumFields(flags, ShimProxy.UnwrapCDac<IXCLRDataTypeInstance>(fromType), handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EnumField(ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        uint* token)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumField(handle, field, nameBufLen, nameLen, nameBuf, token) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EndEnumFields(ulong handle)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumFields(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.StartEnumFieldsByName(char* name, uint nameFlags, uint fieldFlags, IXCLRDataTypeInstance? fromType, ulong* handle)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.StartEnumFieldsByName(name, nameFlags, fieldFlags, ShimProxy.UnwrapCDac<IXCLRDataTypeInstance>(fromType), handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EnumFieldByName(ulong* handle, DacComNullableByRef<IXCLRDataValue> field, uint* token)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumFieldByName(handle, field, token) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EndEnumFieldsByName(ulong handle)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EndEnumFieldsByName(handle) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetFieldByToken(uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldByToken(token, field, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetAssociatedValue(DacComNullableByRef<IXCLRDataValue> assocValue)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssociatedValue(assocValue) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetAssociatedType(DacComNullableByRef<IXCLRDataTypeInstance> assocType)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetAssociatedType(assocType) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetString(uint bufLen, uint* strLen, char* str)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetString(bufLen, strLen, str) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetArrayProperties(uint* rank, uint* totalElements, uint numDim, uint* dims, uint numBases, int* bases)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetArrayProperties(rank, totalElements, numDim, dims, numBases, bases) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetArrayElement(uint numInd, int* indices, DacComNullableByRef<IXCLRDataValue> value)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetArrayElement(numInd, indices, value) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EnumField2(ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        uint nameBufLen,
        uint* nameLen,
        char* nameBuf,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumField2(handle, field, nameBufLen, nameLen, nameBuf, tokenScope, token) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.EnumFieldByName2(ulong* handle,
        DacComNullableByRef<IXCLRDataValue> field,
        DacComNullableByRef<IXCLRDataModule> tokenScope,
        uint* token)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.EnumFieldByName2(handle, field, tokenScope, token) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetFieldByToken2(IXCLRDataModule? tokenScope,
        uint token,
        DacComNullableByRef<IXCLRDataValue> field,
        uint bufLen,
        uint* nameLen,
        char* nameBuf)
    {
        // Pre-refactor cDAC returned E_NOTIMPL with no legacy comparison.
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetFieldByToken2(ShimProxy.UnwrapCDac<IXCLRDataModule>(tokenScope), token, field, bufLen, nameLen, nameBuf) : HResults.E_NOTIMPL;
        return hr;
    }

    int IXCLRDataValue.GetNumLocations(uint* numLocs)
    {
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetNumLocations(numLocs) : HResults.E_NOTIMPL;
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
        using ShimCall shimCall = ShimCall.Enter();
        int hr = _cdacImpl is not null ? _cdacImpl.GetLocationByIndex(loc, flags, arg) : HResults.E_NOTIMPL;
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

    #endregion IXCLRDataValue

}
