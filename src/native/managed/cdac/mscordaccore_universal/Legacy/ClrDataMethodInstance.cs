// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
internal sealed unsafe partial class ClrDataMethodInstance : IXCLRDataMethodInstance
{
    private readonly Target _target;
    private readonly MethodDescHandle _methodDesc;
    private readonly TargetPointer _appDomain;
    private readonly IXCLRDataMethodInstance? _legacyImpl;
    public ClrDataMethodInstance(
        Target target,
        MethodDescHandle methodDesc,
        TargetPointer appDomain,
        IXCLRDataMethodInstance? legacyImpl)
    {
        _target = target;
        _methodDesc = methodDesc;
        _appDomain = appDomain;
        _legacyImpl = legacyImpl;
    }

    int IXCLRDataMethodInstance.GetTypeInstance(void** typeInstance)
        => _legacyImpl is not null ? _legacyImpl.GetTypeInstance(typeInstance) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetDefinition(void** methodDefinition)
        => _legacyImpl is not null ? _legacyImpl.GetDefinition(methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTokenAndScope(uint* token, void** /*IXCLRDataModule*/ mod)
    {
        int hr = HResults.S_OK;
        StrategyBasedComWrappers cw = new();

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            if (token is not null)
            {
                *token = rts.GetMethodToken(_methodDesc);
            }
            if (mod is not null)
            {
                IXCLRDataModule? legacyMod = null;
                if (_legacyImpl is not null)
                {
                    void* legacyModPtr = null;
                    int hrLegacy = _legacyImpl.GetTokenAndScope(token, &legacyModPtr);
                    if (hrLegacy < 0)
                        return hrLegacy;
                    object obj = cw.GetOrCreateObjectForComInstance((nint)legacyModPtr, CreateObjectFlags.None);
                    if (obj is not IXCLRDataModule)
                    {
                        throw new ArgumentException("Invalid module object", nameof(mod));
                    }
                    legacyMod = obj as IXCLRDataModule;
                }

                TargetPointer mtAddr = rts.GetMethodTable(_methodDesc);
                TypeHandle mainMT = rts.GetTypeHandle(mtAddr);
                TargetPointer module = rts.GetModule(mainMT);
                IXCLRDataModule modImpl = new ClrDataModule(module, _target, legacyMod);
                nint modImplPtr = cw.GetOrCreateComInterfaceForObject(modImpl, CreateComInterfaceFlags.None);
                Marshal.QueryInterface(modImplPtr, typeof(IXCLRDataModule).GUID, out nint ptrToMod);
                Marshal.Release(modImplPtr);
                *mod = (void*)ptrToMod;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            bool validateToken = token is not null;
            bool validateMod = mod is not null;

            uint tokenLocal = 0;
            void* legacyModPtr = null;
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, validateMod ? &legacyModPtr : null);

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");

            if (validateToken)
            {
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
            }

            if (validateMod && hr == HResults.S_OK)
            {
                Marshal.Release((nint)legacyModPtr); // release the legacy module
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.GetName(uint flags, uint bufLen, uint* nameLen, char* nameBuf)
    {
        int hr = HResults.S_OK;

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            if (flags != 0)
                throw new ArgumentException();

            bool fallbackToUnknown = false;
            StringBuilder sb = new();
            try
            {
                TypeNameBuilder.AppendMethodInternal(
                    _target,
                    sb,
                    _methodDesc,
                    TypeNameFormat.FormatSignature |
                    TypeNameFormat.FormatNamespace |
                    TypeNameFormat.FormatFullInst);
            }
            catch
            {
                string? fallbackName = _target.Contracts.DacStreams.StringFromEEAddress(_methodDesc.Address);
                if (fallbackName != null)
                {
                    sb.Clear();
                    sb.Append(fallbackName);
                }
                else
                {
                    sb.Clear();
                    sb.Append("Unknown");
                    fallbackToUnknown = true;
                }
            }

            OutputBufferHelpers.CopyStringToBuffer(nameBuf, bufLen, nameLen, sb.ToString());

            if (!fallbackToUnknown && nameBuf != null && bufLen < sb.Length + 1)
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint nameLenLocal = 0;
            char[] nameBufLocal = new char[bufLen > 0 ? bufLen : 1];
            int hrLocal;
            fixed (char* pNameBufLocal = nameBufLocal)
            {
                hrLocal = _legacyImpl.GetName(flags, bufLen, &nameLenLocal, nameBuf is null ? null : pNameBufLocal);
            }

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            if (nameLen is not null)
                Debug.Assert(nameLenLocal == *nameLen, $"cDAC: {*nameLen:x}, DAC: {nameLenLocal:x}");

            if (nameBuf is not null)
            {
                string dacName = new string(nameBufLocal, 0, (int)nameLenLocal - 1);
                string cdacName = new string(nameBuf);
                Debug.Assert(dacName == cdacName, $"cDAC: {cdacName}, DAC: {dacName}");
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.GetFlags(uint* flags)
        => _legacyImpl is not null ? _legacyImpl.GetFlags(flags) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.IsSameObject(IXCLRDataMethodInstance* method)
        => _legacyImpl is not null ? _legacyImpl.IsSameObject(method) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetEnCVersion(uint* version)
        => _legacyImpl is not null ? _legacyImpl.GetEnCVersion(version) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetNumTypeArguments(uint* numTypeArgs)
        => _legacyImpl is not null ? _legacyImpl.GetNumTypeArguments(numTypeArgs) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTypeArgumentByIndex(uint index, void** typeArg)
        => _legacyImpl is not null ? _legacyImpl.GetTypeArgumentByIndex(index, typeArg) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILOffsetsByAddress(ClrDataAddress address, uint offsetsLen, uint* offsetsNeeded, uint* ilOffsets)
    {
        int hr = HResults.S_OK;

        try
        {
            TargetCodePointer pCode = address.ToTargetCodePointer(_target);
            List<OffsetMapping> map = _target.Contracts.DebugInfo.GetMethodNativeMap(
                pCode,
                preferUninstrumented: false,
                out uint codeOffset).ToList();

            uint hits = 0;
            for (int i = 0; i < map.Count; i++)
            {
                bool isEpilog = map[i].ILOffset == unchecked((uint)-3); // -3 is used to indicate an epilog
                bool lastValue = i == map.Count - 1;
                uint nativeEndOffset = lastValue ? 0 : map[i + 1].NativeOffset;
                if (codeOffset >= map[i].NativeOffset && (((isEpilog || lastValue) && nativeEndOffset == 0) || codeOffset < nativeEndOffset))
                {
                    if (hits < offsetsLen && ilOffsets is not null)
                    {
                        ilOffsets[hits] = map[i].ILOffset;
                    }

                    hits++;
                }
            }

            if (offsetsNeeded is not null)
            {
                *offsetsNeeded = hits;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            int hrLocal;

            bool validateOffsetsNeeded = offsetsNeeded is not null;
            uint localOffsetsNeeded = 0;

            bool validateIlOffsets = ilOffsets is not null;
            uint[] localIlOffsets = new uint[offsetsLen];

            fixed (uint* localIlOffsetsPtr = localIlOffsets)
            {
                hrLocal = _legacyImpl.GetILOffsetsByAddress(
                    address,
                    offsetsLen,
                    validateOffsetsNeeded ? &localOffsetsNeeded : null,
                    validateIlOffsets ? localIlOffsetsPtr : null);
            }

            // DAC function returns odd failure codes it doesn't make sense to match directly
            Debug.Assert(hrLocal == hr || (hrLocal < 0 && hr < 0), $"cDAC: {hr:x}, DAC: {hrLocal:x}");

            if (hr == HResults.S_OK)
            {
                if (validateOffsetsNeeded)
                {
                    Debug.Assert(localOffsetsNeeded == *offsetsNeeded, $"cDAC: {*offsetsNeeded:x}, DAC: {localOffsetsNeeded:x}");
                }

                if (validateIlOffsets)
                {
                    for (int i = 0; i < localIlOffsets.Length; i++)
                    {
                        Debug.Assert(localIlOffsets[i] == ilOffsets[i], $"cDAC: {localIlOffsets[i]:x}, DAC: {ilOffsets[i]:x}");
                    }
                }
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.GetAddressRangesByILOffset(uint ilOffset, uint rangesLen, uint* rangesNeeded, void* addressRanges)
        => _legacyImpl is not null ? _legacyImpl.GetAddressRangesByILOffset(ilOffset, rangesLen, rangesNeeded, addressRanges) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILAddressMap(uint mapLen, uint* mapNeeded, [In, Out, MarshalUsing(CountElementName = "mapLen")] ClrDataILAddressMap[]? maps)
    {
        int hr = HResults.S_OK;

        try
        {
            TargetCodePointer pCode = _target.Contracts.RuntimeTypeSystem.GetNativeCode(_methodDesc);
            TargetPointer codeStart = pCode.ToAddress(_target);
            List<OffsetMapping> map = _target.Contracts.DebugInfo.GetMethodNativeMap(
                pCode,
                preferUninstrumented: false,
                out uint _).ToList();

            if (maps is not null)
            {
                int outputMapIndex = 0;
                for (int i = 0; i < map.Count; i++)
                {
                    OffsetMapping entry = map[i];

                    bool lastValue = i == map.Count - 1;
                    uint nativeEndOffset = lastValue ? 0 : map[i + 1].NativeOffset;

                    if (outputMapIndex < maps.Length)
                    {
                        maps[outputMapIndex].ilOffset = entry.ILOffset;
                        maps[outputMapIndex].startAddress = new TargetPointer(codeStart + entry.NativeOffset).ToClrDataAddress(_target);
                        maps[outputMapIndex].endAddress = new TargetPointer(codeStart + nativeEndOffset).ToClrDataAddress(_target);
                        maps[outputMapIndex].type = ClrDataSourceType.CLRDATA_SOURCE_TYPE_INVALID;

                        outputMapIndex++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (mapNeeded is not null)
            {
                *mapNeeded = (uint)map.Count;
            }

            hr = map.Count > 0 ? HResults.S_OK : HResults.COR_E_INVALIDCAST /*E_NOINTERFACE*/;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            uint mapNeededLocal;
            ClrDataILAddressMap[]? mapsLocal = mapLen > 0 ? new ClrDataILAddressMap[mapLen] : null;
            int hrLocal = _legacyImpl.GetILAddressMap(mapLen, &mapNeededLocal, mapsLocal);
            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");

            if (hr == HResults.S_OK)
            {
                Debug.Assert(mapNeeded == null || *mapNeeded == mapNeededLocal);
                if (mapsLocal is not null)
                {
                    for (int i = 0; i < mapsLocal.Length; i++)
                    {
                        Debug.Assert(mapsLocal[i].ilOffset == maps![i].ilOffset, $"cDAC: {maps[i].ilOffset:x}, DAC: {mapsLocal[i].ilOffset:x}");
                        Debug.Assert(mapsLocal[i].startAddress == maps[i].startAddress, $"cDAC: {maps[i].startAddress:x}, DAC: {mapsLocal[i].startAddress:x}");
                        Debug.Assert(mapsLocal[i].endAddress == maps[i].endAddress, $"cDAC: {maps[i].endAddress:x}, DAC: {mapsLocal[i].endAddress:x}");
                        Debug.Assert(mapsLocal[i].type == maps[i].type, $"cDAC: {maps[i].type:x}, DAC: {mapsLocal[i].type:x}");
                    }
                }
            }
        }

#endif

        return hr;
    }

    int IXCLRDataMethodInstance.StartEnumExtents(ulong* handle)
        => _legacyImpl is not null ? _legacyImpl.StartEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.EnumExtent(ulong* handle, void* extent)
        => _legacyImpl is not null ? _legacyImpl.EnumExtent(handle, extent) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.EndEnumExtents(ulong handle)
        => _legacyImpl is not null ? _legacyImpl.EndEnumExtents(handle) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetRepresentativeEntryAddress(ClrDataAddress* addr)
    {
        int hr = HResults.S_OK;

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

            TargetCodePointer addrCode = rts.GetNativeCode(_methodDesc);

            if (addrCode.Value != 0)
            {
                *addr = addrCode.Value;
            }
            else
            {
                hr = unchecked((int)0x8000FFFF); // E_UNEXPECTED
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ClrDataAddress addrLocal;
            int hrLocal = _legacyImpl.GetRepresentativeEntryAddress(&addrLocal);

            Debug.Assert(hrLocal == hr, $"cDAC: {hr:x}, DAC: {hrLocal:x}");
            Debug.Assert(addrLocal == *addr, $"cDAC: {*addr:x}, DAC: {addrLocal:x}");
        }
#endif

        return hr;
    }
}
