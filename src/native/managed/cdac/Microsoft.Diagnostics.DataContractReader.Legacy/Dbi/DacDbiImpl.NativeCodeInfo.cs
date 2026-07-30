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
            (DebugVarLocKind.FloatingPointStack, _, _) => VarLocType.VLT_FPSTK,
            (DebugVarLocKind.FixedVarArg, _, _) => VarLocType.VLT_FIXED_VA,
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
            case DebugVarLocKind.FloatingPointStack:
                loc.vlfReg = varInfo.FloatingPointStackRegister;
                break;
            case DebugVarLocKind.FixedVarArg:
                loc.vlfvOffset = varInfo.FixedVarArgOffset;
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

}
