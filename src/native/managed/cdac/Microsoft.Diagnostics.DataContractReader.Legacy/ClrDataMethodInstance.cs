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
    public ClrDataMethodInstance(
        Target target,
        MethodDescHandle methodDesc,
        TargetPointer appDomain)
    {
        _target = target;
        _methodDesc = methodDesc;
        _appDomain = appDomain;
    }

    int IXCLRDataMethodInstance.GetTypeInstance(DacComNullableByRef<IXCLRDataTypeInstance> typeInstance)
        => HResults.E_NOTIMPL;

    int IXCLRDataMethodInstance.GetDefinition(DacComNullableByRef<IXCLRDataMethodDefinition> methodDefinition)
        => HResults.E_NOTIMPL;

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

                TargetPointer mtAddr = rts.GetMethodTable(_methodDesc);
                ITypeHandle mainMT = rts.GetTypeHandle(mtAddr);
                TargetPointer module = rts.GetModule(mainMT);
                mod.Interface = new ClrDataModule(module, _target);
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


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


        return hr;
    }

    int IXCLRDataMethodInstance.EndEnumExtents(ulong handle)
    {
        int hr = HResults.S_OK;
        try
        {
            if (handle != 0)
            {
                GCHandle gcHandle = GCHandle.FromIntPtr((IntPtr)handle);
                if (gcHandle.Target is not EnumMethodExtents extents)
                    throw new ArgumentException("Invalid extent handle.", nameof(handle));

                ((IEnum<ClrDataAddressRange>)extents).Dispose();
                gcHandle.Free();
            }
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

    int IXCLRDataMethodInstance.Request(uint reqCode, uint inBufferSize, byte* inBuffer, uint outBufferSize, byte* outBuffer)
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

            *(uint*)outBuffer = 1;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }


        return hr;
    }

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


        return hr;
    }
}
