// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
public sealed unsafe partial class ClrDataMethodInstance : IXCLRDataMethodInstance
{
    private sealed class EnumMethodExtents : IEnum<ClrDataAddressRange>
    {
        public IEnumerator<ClrDataAddressRange> Enumerator { get; }
        public nuint LegacyHandle { get; set; }

        public EnumMethodExtents(ClrDataAddressRange extent)
        {
            Enumerator = Enumerable.Repeat(extent, 1).GetEnumerator();
        }
    }

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

    int IXCLRDataMethodInstance.GetTypeInstance(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetDefinition(DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.GetDefinition(methodDefinition) : HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTokenAndScope(uint* token, DacComNullableByRef<IXCLRDataModule> mod)
    {
        int hr = HResults.S_OK;

        try
        {
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            if (token is not null)
            {
                *token = rts.GetMethodToken(_methodDesc);
            }
            if (!mod.IsNullRef)
            {
                IXCLRDataModule? legacyMod = null;
                if (_legacyImpl is not null)
                {
                    DacComNullableByRef<IXCLRDataModule> legacyModOut = new(isNullRef: false);
                    int hrLegacy = _legacyImpl.GetTokenAndScope(token, legacyModOut);
                    if (hrLegacy < 0)
                        return hrLegacy;
                    legacyMod = legacyModOut.Interface;
                }

                TargetPointer mtAddr = rts.GetMethodTable(_methodDesc);
                TypeHandle mainMT = rts.GetTypeHandle(mtAddr);
                TargetPointer module = rts.GetModule(mainMT);
                mod.Interface = new ClrDataModule(module, _target, legacyMod);
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
            bool validateMod = !mod.IsNullRef;

            uint tokenLocal = 0;
            DacComNullableByRef<IXCLRDataModule> legacyModOutLocal = new(isNullRef: !validateMod);
            int hrLocal = _legacyImpl.GetTokenAndScope(validateToken ? &tokenLocal : null, legacyModOutLocal);

            Debug.ValidateHResult(hr, hrLocal);

            if (validateToken)
            {
                Debug.Assert(tokenLocal == *token, $"cDAC: {*token:x}, DAC: {tokenLocal:x}");
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

            Debug.ValidateHResult(hr, hrLocal);
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
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.IsSameObject(IXCLRDataMethodInstance* method)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetEnCVersion(uint* version)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetNumTypeArguments(uint* numTypeArgs)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetTypeArgumentByIndex(uint index, DacComNullableByRef<IXCLRDataTypeInstance> typeArg)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILOffsetsByAddress(ClrDataAddress address, uint offsetsLen, uint* offsetsNeeded, uint* ilOffsets)
    {
        int hr = HResults.S_OK;

        try
        {
            TargetCodePointer pCode = address.ToTargetCodePointer(_target);

            // No debug info exists at all (e.g. ILStubs).
            // This matches the DAC where GetBoundariesAndVars returns FALSE -> E_FAIL.
            if (!_target.Contracts.DebugInfo.HasDebugInfo(pCode))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            IEnumerable<OffsetMapping> mapEnumerable = _target.Contracts.DebugInfo.GetMethodNativeMap(
                pCode,
                preferUninstrumented: false,
                out uint codeOffset);

            List<OffsetMapping> map = [.. mapEnumerable];

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

            // AllowCdacSuccess: the DAC fails on interpreted code.
            Debug.ValidateHResult(hr, hrLocal, HResultValidationMode.AllowCdacSuccess);

            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
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
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetILAddressMap(uint mapLen, uint* mapNeeded, [In, Out, MarshalUsing(CountElementName = "mapLen")] ClrDataILAddressMap[]? maps)
    {
        int hr = HResults.S_OK;

        try
        {
            TargetCodePointer nativeCode = _target.Contracts.RuntimeTypeSystem.GetNativeCode(_methodDesc);
            TargetCodePointer pCode = _target.Contracts.PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
            TargetPointer codeStart = pCode.ToAddress(_target);

            // No debug info exists at all (e.g. ILStubs).
            // This matches the DAC where GetBoundariesAndVars returns FALSE -> E_FAIL.
            if (!_target.Contracts.DebugInfo.HasDebugInfo(pCode))
                throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

            IEnumerable<OffsetMapping> mapEnumerable = _target.Contracts.DebugInfo.GetMethodNativeMap(
                pCode,
                preferUninstrumented: false,
                out uint _);

            List<OffsetMapping> map = [.. mapEnumerable];

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
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK)
            {
                Debug.Assert(mapNeeded == null || *mapNeeded == mapNeededLocal);
                if (mapsLocal is not null)
                {
                    int countToCheck = Math.Min(mapsLocal.Length, (int)mapNeededLocal);
                    for (int i = 0; i < countToCheck; i++)
                    {
                        Debug.Assert(mapsLocal[i].ilOffset == maps![i].ilOffset, $"ILOffset - cDAC: {maps[i].ilOffset:x}, DAC: {mapsLocal[i].ilOffset:x}");
                        Debug.Assert(mapsLocal[i].startAddress == maps[i].startAddress, $"StartAddress - cDAC: {maps[i].startAddress:x}, DAC: {mapsLocal[i].startAddress:x}");
                        Debug.Assert(mapsLocal[i].endAddress == maps[i].endAddress, $"EndAddress - cDAC: {maps[i].endAddress:x}, DAC: {mapsLocal[i].endAddress:x}");
                        Debug.Assert(mapsLocal[i].type == maps[i].type, $"Type - cDAC: {maps[i].type:x}, DAC: {mapsLocal[i].type:x}");
                    }
                }
            }
        }

#endif

        return hr;
    }

    private ClrDataAddressRange GetMethodExtent()
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TargetCodePointer nativeCode = rts.GetNativeCode(_methodDesc);
        TargetCodePointer code = _target.Contracts.PrecodeStubs.GetInterpreterCodeFromInterpreterPrecodeIfPresent(nativeCode);
        if (code == TargetCodePointer.Null)
        {
            code = nativeCode;
        }

        if (code == TargetCodePointer.Null)
        {
            throw new InvalidCastException(); // E_NOINTERFACE
        }

        IExecutionManager executionManager = _target.Contracts.ExecutionManager;
        CodeBlockHandle? codeBlock = executionManager.GetCodeBlockHandle(code);
        if (codeBlock is null)
        {
            throw new InvalidOperationException($"No code block found for native code address {code.ToClrDataAddress(_target):x} (the address may be invalid or the corresponding module may not be loaded).");
        }

        executionManager.GetGCInfo(codeBlock.Value, out TargetPointer gcInfoAddress, out uint gcVersion);
        CodeKind codeKind = executionManager.GetCodeKind(code);
        IGCInfo gcInfo = _target.Contracts.GCInfo;
        IGCInfoHandle gcInfoHandle = codeKind == CodeKind.Interpreter
            ? gcInfo.DecodeInterpreterGCInfo(gcInfoAddress, gcVersion)
            : gcInfo.DecodePlatformSpecificGCInfo(gcInfoAddress, gcVersion);

        ClrDataAddress startAddress = code.ToClrDataAddress(_target);
        uint codeLength = gcInfo.GetCodeLength(gcInfoHandle);
        return new ClrDataAddressRange
        {
            startAddress = startAddress,
            endAddress = startAddress + codeLength,
        };
    }

    int IXCLRDataMethodInstance.StartEnumExtents(ulong* handle)
    {
        int hr = HResults.S_OK;
        try
        {
            if (handle is null)
                throw new ArgumentNullException(nameof(handle));

            EnumMethodExtents extents = new(GetMethodExtent());
            *handle = (ulong)((IEnum<ClrDataAddressRange>)extents).GetHandle();
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null)
        {
            ulong legacyHandle = 0;
            int hrLocal = _legacyImpl.StartEnumExtents(handle is null ? null : &legacyHandle);
            Debug.ValidateHResult(hr, hrLocal);

            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
                ((EnumMethodExtents)gcHandle.Target!).LegacyHandle = (nuint)legacyHandle;
            }
            else if (hrLocal == HResults.S_OK)
            {
                _legacyImpl.EndEnumExtents(legacyHandle);
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.EnumExtent(ulong* handle, ClrDataAddressRange* extent)
    {
        int hr = HResults.S_OK;
        EnumMethodExtents? extents = null;
        try
        {
            if (handle is null)
                throw new ArgumentNullException(nameof(handle));
            if (extent is null)
                throw new ArgumentNullException(nameof(extent));
            if (*handle == 0)
                throw new ArgumentException("Invalid extent handle.", nameof(handle));

            GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)(*handle));
            if (gcHandle.Target is not EnumMethodExtents methodExtents)
                throw new ArgumentException("Invalid extent handle.", nameof(handle));

            extents = methodExtents;
            if (extents.Enumerator.MoveNext())
            {
                *extent = extents.Enumerator.Current;
            }
            else
            {
                hr = HResults.S_FALSE;
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null && extents is { LegacyHandle: not 0 })
        {
            ulong legacyHandle = (ulong)extents.LegacyHandle;
            ClrDataAddressRange extentLocal = default;
            int hrLocal = _legacyImpl.EnumExtent(&legacyHandle, &extentLocal);
            extents.LegacyHandle = (nuint)legacyHandle;
            Debug.ValidateHResult(hr, hrLocal);
            if (hr == HResults.S_OK)
            {
                Debug.Assert(extent->startAddress == extentLocal.startAddress, $"StartAddress - cDAC: {extent->startAddress:x}, DAC: {extentLocal.startAddress:x}");
                Debug.Assert(extent->endAddress == extentLocal.endAddress, $"EndAddress - cDAC: {extent->endAddress:x}, DAC: {extentLocal.endAddress:x}");
            }
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.EndEnumExtents(ulong handle)
    {
        int hr = HResults.S_OK;
        nuint legacyHandle = 0;
        try
        {
            if (handle != 0)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
                if (gcHandle.Target is not EnumMethodExtents extents)
                    throw new ArgumentException("Invalid extent handle.", nameof(handle));

                legacyHandle = extents.LegacyHandle;
                ((IEnum<ClrDataAddressRange>)extents).Dispose();
                gcHandle.Free();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }

#if DEBUG
        if (_legacyImpl is not null && legacyHandle != 0)
        {
            int hrLocal = _legacyImpl.EndEnumExtents((ulong)legacyHandle);
            Debug.ValidateHResult(hr, hrLocal);
        }
#endif

        return hr;
    }

    int IXCLRDataMethodInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
        => LegacyFallbackHelper.CanFallback() && _legacyImpl is not null ? _legacyImpl.Request(reqCode, inBufferSize, inBuffer, outBufferSize, outBuffer) : HResults.E_NOTIMPL;

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

            Debug.ValidateHResult(hr, hrLocal);
            Debug.Assert(addrLocal == *addr, $"cDAC: {*addr:x}, DAC: {addrLocal:x}");
        }
#endif

        return hr;
    }
}
