// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

public sealed unsafe partial class DacDbiImpl
{
    // Gets the number of fixed arguments (i.e., the explicit args and the "this" pointer) from the method signature.
    // This does not include other implicit arguments or varargs.
    private uint GetArgCount(ulong vmMethodDesc)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdh = rts.GetMethodDescHandle(new TargetPointer(vmMethodDesc));

        if (!rts.TryGetMethodSignature(mdh, out ReadOnlySpan<byte> signature))
            throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

        MethodSignatureHelpers.GetSignatureInfo(signature, out _, out uint numArgs);
        return numArgs;
    }

    internal static NativeVarInfo ConvertToNativeVarInfo(DebugVarInfo varInfo)
    {
        NativeVarInfo nvi = default;
        nvi.startOffset = varInfo.StartOffset;
        nvi.endOffset = varInfo.EndOffset;
        nvi.callReturnValueILOffset = varInfo.CallReturnValueILOffset;
        nvi.varNumber = varInfo.VarNumber;
        nvi.loc = ConvertToVarLoc(varInfo);
        return nvi;
    }

    internal static DbiOffsetMapping ConvertToDbiOffsetMapping(Contracts.OffsetMapping mapping)
    {
        DbiOffsetMapping nativeMapping = default;
        nativeMapping.nativeOffset = mapping.NativeOffset;
        nativeMapping.ilOffset = mapping.ILOffset;
        nativeMapping.source = ConvertSourceTypesToNative(mapping.SourceType);
        return nativeMapping;
    }

    internal static VarLoc ConvertToVarLoc(DebugVarInfo varInfo)
    {
        VarLoc loc = default;
        loc.vlType = (varInfo.Kind, varInfo.IsByRef, varInfo.IsFloatingPoint) switch
        {
            (DebugVarLocKind.Register, false, false) => VarLocType.VLT_REG,
            (DebugVarLocKind.Register, false, true) => VarLocType.VLT_REG_FP,
            (DebugVarLocKind.Register, true, _) => VarLocType.VLT_REG_BYREF,
            (DebugVarLocKind.Stack, false, _) => VarLocType.VLT_STK,
            (DebugVarLocKind.Stack, true, _) => VarLocType.VLT_STK_BYREF,
            (DebugVarLocKind.RegisterRegister, _, _) => VarLocType.VLT_REG_REG,
            (DebugVarLocKind.RegisterStack, _, _) => VarLocType.VLT_REG_STK,
            (DebugVarLocKind.StackRegister, _, _) => VarLocType.VLT_STK_REG,
            (DebugVarLocKind.DoubleStack, _, _) => VarLocType.VLT_STK2,
            _ => VarLocType.VLT_INVALID,
        };

        switch (varInfo.Kind)
        {
            case DebugVarLocKind.Register:
                loc.vlrReg = varInfo.Register;
                break;
            case DebugVarLocKind.Stack:
                loc.vlsBaseReg = varInfo.BaseRegister;
                loc.vlsOffset = varInfo.StackOffset;
                break;
            case DebugVarLocKind.RegisterRegister:
                loc.vlrrReg1 = varInfo.Register;
                loc.vlrrReg2 = varInfo.Register2;
                break;
            case DebugVarLocKind.RegisterStack:
                loc.vlrsReg = varInfo.Register;
                loc.vlrssBaseReg = varInfo.BaseRegister2;
                loc.vlrssOffset = varInfo.StackOffset2;
                break;
            case DebugVarLocKind.StackRegister:
                loc.vlsrsBaseReg = varInfo.BaseRegister;
                loc.vlsrsOffset = varInfo.StackOffset;
                loc.vlsrReg = varInfo.Register;
                break;
            case DebugVarLocKind.DoubleStack:
                loc.vlsBaseReg = varInfo.BaseRegister;
                loc.vlsOffset = varInfo.StackOffset;
                break;
        }

        return loc;
    }

    // Converts cDAC Contracts.SourceTypes to native ICorDebugInfo::SourceTypes values.
    // The cDAC uses compact bit positions while the native enum uses different bit values.
    internal static DbiSourceTypes ConvertSourceTypesToNative(Contracts.SourceTypes source)
    {
        DbiSourceTypes result = DbiSourceTypes.SourceTypeInvalid;
        if ((source & Contracts.SourceTypes.StackEmpty) != 0)
            result |= DbiSourceTypes.StackEmpty;
        if ((source & Contracts.SourceTypes.CallInstruction) != 0)
            result |= DbiSourceTypes.CallInstruction;
        if ((source & Contracts.SourceTypes.Async) != 0)
            result |= DbiSourceTypes.Async;

        return result;
    }

#if DEBUG
    private void ValidateNativeCodeInfoAgainstLegacy(
        ulong vmMethodDesc,
        ulong startAddress,
        Interop.BOOL fCodeAvailable,
        uint* pFixedArgCount,
        List<NativeVarInfo> cdacVarInfos,
        List<DbiOffsetMapping> cdacSeqPoints,
        int hr,
        bool varInfoRequested,
        bool seqPointsRequested)
    {
        uint dacFixedArgCount = 0;
        var dacData = new DebugNativeCodeData();
        GCHandle dacHandle = GCHandle.Alloc(dacData);
        int hrLocal;
        try
        {
            hrLocal = _legacy!.GetNativeCodeSequencePointsAndVarInfo(
                vmMethodDesc, startAddress, fCodeAvailable, &dacFixedArgCount,
                (delegate* unmanaged<NativeVarInfo*, void*, void>)&CollectNativeVarInfoCallback,
                (delegate* unmanaged<DbiOffsetMapping*, void*, void>)&CollectOffsetMappingCallback,
                GCHandle.ToIntPtr(dacHandle));
        }
        finally
        {
            dacHandle.Free();
        }

        Debug.ValidateHResult(hr, hrLocal);
        if (hr == HResults.S_OK)
        {
            if (pFixedArgCount != null)
            {
                Debug.Assert(*pFixedArgCount == dacFixedArgCount,
                    $"fixedArgCount mismatch - cDAC: {*pFixedArgCount}, DAC: {dacFixedArgCount}");
            }

            // Only compare lists whose callback was supplied.
            if (seqPointsRequested)
                AssertSeqPointsEqual(cdacSeqPoints, dacData.SeqPoints);
            if (varInfoRequested)
                AssertVarInfosEqual(cdacVarInfos, dacData.VarInfos);
        }
    }

    private static void AssertSeqPointsEqual(List<DbiOffsetMapping> cdac, List<DbiOffsetMapping> dac)
    {
        Debug.Assert(cdac.Count == dac.Count,
            $"SeqPoint count mismatch - cDAC: {cdac.Count}, DAC: {dac.Count}");
        int n = Math.Min(cdac.Count, dac.Count);
        for (int i = 0; i < n; i++)
        {
            DbiOffsetMapping c = cdac[i];
            DbiOffsetMapping d = dac[i];
            Debug.Assert(c.nativeOffset == d.nativeOffset,
                $"SeqPoint[{i}] nativeOffset mismatch - cDAC: {c.nativeOffset}, DAC: {d.nativeOffset}");
            Debug.Assert(c.ilOffset == d.ilOffset,
                $"SeqPoint[{i}] ilOffset mismatch - cDAC: {c.ilOffset}, DAC: {d.ilOffset}");
            Debug.Assert(c.source == d.source,
                $"SeqPoint[{i}] source mismatch - cDAC: 0x{c.source:X}, DAC: 0x{d.source:X}");
        }
    }

    private static void AssertVarInfosEqual(List<NativeVarInfo> cdac, List<NativeVarInfo> dac)
    {
        Debug.Assert(cdac.Count == dac.Count,
            $"VarInfo count mismatch - cDAC: {cdac.Count}, DAC: {dac.Count}");
        int n = Math.Min(cdac.Count, dac.Count);
        for (int i = 0; i < n; i++)
        {
            NativeVarInfo c = cdac[i];
            NativeVarInfo d = dac[i];
            Debug.Assert(c.startOffset == d.startOffset,
                $"VarInfo[{i}] startOffset mismatch - cDAC: {c.startOffset}, DAC: {d.startOffset}");
            Debug.Assert(c.endOffset == d.endOffset,
                $"VarInfo[{i}] endOffset mismatch - cDAC: {c.endOffset}, DAC: {d.endOffset}");
            Debug.Assert(c.callReturnValueILOffset == d.callReturnValueILOffset,
                $"VarInfo[{i}] callReturnValueILOffset mismatch - cDAC: {c.callReturnValueILOffset}, DAC: {d.callReturnValueILOffset}");
            Debug.Assert(c.varNumber == d.varNumber,
                $"VarInfo[{i}] varNumber mismatch - cDAC: {c.varNumber}, DAC: {d.varNumber}");
            Debug.Assert(c.loc.vlType == d.loc.vlType,
                $"VarInfo[{i}] vlType mismatch - cDAC: {c.loc.vlType}, DAC: {d.loc.vlType}");

            switch (c.loc.vlType)
            {
                case VarLocType.VLT_REG:
                case VarLocType.VLT_REG_FP:
                case VarLocType.VLT_REG_BYREF:
                    Debug.Assert(c.loc.vlrReg == d.loc.vlrReg,
                        $"VarInfo[{i}] vlrReg mismatch - cDAC: {c.loc.vlrReg}, DAC: {d.loc.vlrReg}");
                    break;
                case VarLocType.VLT_STK:
                case VarLocType.VLT_STK_BYREF:
                case VarLocType.VLT_STK2:
                    Debug.Assert(c.loc.vlsBaseReg == d.loc.vlsBaseReg,
                        $"VarInfo[{i}] vlsBaseReg mismatch - cDAC: {c.loc.vlsBaseReg}, DAC: {d.loc.vlsBaseReg}");
                    Debug.Assert(c.loc.vlsOffset == d.loc.vlsOffset,
                        $"VarInfo[{i}] vlsOffset mismatch - cDAC: {c.loc.vlsOffset}, DAC: {d.loc.vlsOffset}");
                    break;
                case VarLocType.VLT_REG_REG:
                    Debug.Assert(c.loc.vlrrReg1 == d.loc.vlrrReg1,
                        $"VarInfo[{i}] vlrrReg1 mismatch - cDAC: {c.loc.vlrrReg1}, DAC: {d.loc.vlrrReg1}");
                    Debug.Assert(c.loc.vlrrReg2 == d.loc.vlrrReg2,
                        $"VarInfo[{i}] vlrrReg2 mismatch - cDAC: {c.loc.vlrrReg2}, DAC: {d.loc.vlrrReg2}");
                    break;
                case VarLocType.VLT_REG_STK:
                    Debug.Assert(c.loc.vlrsReg == d.loc.vlrsReg,
                        $"VarInfo[{i}] vlrsReg mismatch - cDAC: {c.loc.vlrsReg}, DAC: {d.loc.vlrsReg}");
                    Debug.Assert(c.loc.vlrssBaseReg == d.loc.vlrssBaseReg,
                        $"VarInfo[{i}] vlrssBaseReg mismatch - cDAC: {c.loc.vlrssBaseReg}, DAC: {d.loc.vlrssBaseReg}");
                    Debug.Assert(c.loc.vlrssOffset == d.loc.vlrssOffset,
                        $"VarInfo[{i}] vlrssOffset mismatch - cDAC: {c.loc.vlrssOffset}, DAC: {d.loc.vlrssOffset}");
                    break;
                case VarLocType.VLT_STK_REG:
                    Debug.Assert(c.loc.vlsrsBaseReg == d.loc.vlsrsBaseReg,
                        $"VarInfo[{i}] vlsrsBaseReg mismatch - cDAC: {c.loc.vlsrsBaseReg}, DAC: {d.loc.vlsrsBaseReg}");
                    Debug.Assert(c.loc.vlsrsOffset == d.loc.vlsrsOffset,
                        $"VarInfo[{i}] vlsrsOffset mismatch - cDAC: {c.loc.vlsrsOffset}, DAC: {d.loc.vlsrsOffset}");
                    Debug.Assert(c.loc.vlsrReg == d.loc.vlsrReg,
                        $"VarInfo[{i}] vlsrReg mismatch - cDAC: {c.loc.vlsrReg}, DAC: {d.loc.vlsrReg}");
                    break;
            }
        }
    }

    private sealed class DebugNativeCodeData
    {
        public List<NativeVarInfo> VarInfos { get; } = new();
        public List<DbiOffsetMapping> SeqPoints { get; } = new();
    }

    [UnmanagedCallersOnly]
    private static void CollectNativeVarInfoCallback(NativeVarInfo* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((DebugNativeCodeData)handle.Target!).VarInfos.Add(*data);
    }

    [UnmanagedCallersOnly]
    private static void CollectOffsetMappingCallback(DbiOffsetMapping* data, void* pUserData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)pUserData);
        ((DebugNativeCodeData)handle.Target!).SeqPoints.Add(*data);
    }
#endif
}
