// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Internal.JitInterface;

namespace SuperPmi;

// Copies data between the JIT-EE interface types and their recorded (agnostic) forms. 'Store' records
// new buffers, 'Restore' finds the existing ones (for lookup keys) or turns recorded data back into
// JIT-EE types.
public static unsafe class SpmiRecordsHelper
{
    // Unsafe.IsNullRef for a byref to a pointer, which can't be a type argument. The JIT passes no location for
    // some of these.
    public static bool IsNullRef(ref void* value)
    {
        fixed (void** p = &value)
            return p == null;
    }

    private const int CORINFO_MAXINDIRECTIONS = 4;
    private const int CORINFO_ACCESS_ALLOWED_MAX_ARGS = 4;

    public static ulong CastHandle(void* h) => (ulong)h;

    public static Agnostic_CORINFO_RESOLVED_TOKENin CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        var tokenIn = new Agnostic_CORINFO_RESOLVED_TOKENin();
        if (!Unsafe.IsNullRef(ref pResolvedToken))
        {
            tokenIn.tokenContext = CastHandle(pResolvedToken.tokenContext);
            tokenIn.tokenScope = CastHandle(pResolvedToken.tokenScope);
            tokenIn.token = (uint)pResolvedToken.token;
            tokenIn.tokenType = (uint)pResolvedToken.tokenType;
        }
        return tokenIn;
    }

    public static Agnostic_CORINFO_RESOLVED_TOKENout CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        var tokenOut = new Agnostic_CORINFO_RESOLVED_TOKENout();
        if (!Unsafe.IsNullRef(ref pResolvedToken))
        {
            tokenOut.hClass = CastHandle(pResolvedToken.hClass);
            tokenOut.hMethod = CastHandle(pResolvedToken.hMethod);
            tokenOut.hField = CastHandle(pResolvedToken.hField);
            tokenOut.cbTypeSpec = pResolvedToken.cbTypeSpec;
            tokenOut.cbMethodSpec = pResolvedToken.cbMethodSpec;
        }
        tokenOut.pTypeSpec_Index = uint.MaxValue;
        tokenOut.pMethodSpec_Index = uint.MaxValue;
        return tokenOut;
    }

    public static Agnostic_CORINFO_RESOLVED_TOKENout StoreAgnostic_CORINFO_RESOLVED_TOKENout(ref CORINFO_RESOLVED_TOKEN pResolvedToken, LightWeightMapBuffer buffers)
    {
        Agnostic_CORINFO_RESOLVED_TOKENout tokenOut = CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(ref pResolvedToken);
        if (!Unsafe.IsNullRef(ref pResolvedToken))
        {
            tokenOut.pTypeSpec_Index = buffers.AddBuffer(Bytes(pResolvedToken.pTypeSpec, pResolvedToken.cbTypeSpec));
            tokenOut.pMethodSpec_Index = buffers.AddBuffer(Bytes(pResolvedToken.pMethodSpec, pResolvedToken.cbMethodSpec));
        }
        return tokenOut;
    }

    public static Agnostic_CORINFO_RESOLVED_TOKENout RestoreAgnostic_CORINFO_RESOLVED_TOKENout(ref CORINFO_RESOLVED_TOKEN pResolvedToken, LightWeightMapBuffer buffers)
    {
        Agnostic_CORINFO_RESOLVED_TOKENout tokenOut = CreateAgnostic_CORINFO_RESOLVED_TOKENout_without_buffers(ref pResolvedToken);
        if (!Unsafe.IsNullRef(ref pResolvedToken))
        {
            tokenOut.pTypeSpec_Index = (uint)buffers.Contains(Bytes(pResolvedToken.pTypeSpec, pResolvedToken.cbTypeSpec));
            tokenOut.pMethodSpec_Index = (uint)buffers.Contains(Bytes(pResolvedToken.pMethodSpec, pResolvedToken.cbMethodSpec));
        }
        return tokenOut;
    }

    public static Agnostic_CORINFO_RESOLVED_TOKEN StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref CORINFO_RESOLVED_TOKEN pResolvedToken, LightWeightMapBuffer buffers)
        => new() { inValue = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken), outValue = StoreAgnostic_CORINFO_RESOLVED_TOKENout(ref pResolvedToken, buffers) };

    public static Agnostic_CORINFO_RESOLVED_TOKEN RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref CORINFO_RESOLVED_TOKEN pResolvedToken, LightWeightMapBuffer buffers)
        => new() { inValue = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken), outValue = RestoreAgnostic_CORINFO_RESOLVED_TOKENout(ref pResolvedToken, buffers) };

    public static CORINFO_RESOLVED_TOKEN Restore_CORINFO_RESOLVED_TOKEN(in Agnostic_CORINFO_RESOLVED_TOKEN pResolvedTokenAgnostic, LightWeightMapBuffer buffers)
    {
        var token = new CORINFO_RESOLVED_TOKEN
        {
            tokenContext = (CORINFO_CONTEXT_STRUCT*)pResolvedTokenAgnostic.inValue.tokenContext,
            tokenScope = (CORINFO_MODULE_STRUCT_*)pResolvedTokenAgnostic.inValue.tokenScope,
            token = (mdToken)pResolvedTokenAgnostic.inValue.token,
            tokenType = (CorInfoTokenKind)pResolvedTokenAgnostic.inValue.tokenType,
        };
        Restore_CORINFO_RESOLVED_TOKENout(ref token, pResolvedTokenAgnostic.outValue, buffers);
        return token;
    }

    // Restores the out values of the token; the in values are left as they are.
    public static void Restore_CORINFO_RESOLVED_TOKENout(ref CORINFO_RESOLVED_TOKEN pResolvedToken, in Agnostic_CORINFO_RESOLVED_TOKENout tokenOut, LightWeightMapBuffer buffers)
    {
        if (Unsafe.IsNullRef(ref pResolvedToken))
            return;

        pResolvedToken.hClass = (CORINFO_CLASS_STRUCT_*)tokenOut.hClass;
        pResolvedToken.hMethod = (CORINFO_METHOD_STRUCT_*)tokenOut.hMethod;
        pResolvedToken.hField = (CORINFO_FIELD_STRUCT_*)tokenOut.hField;
        pResolvedToken.pTypeSpec = buffers.GetBufferPointer(tokenOut.pTypeSpec_Index);
        pResolvedToken.cbTypeSpec = tokenOut.cbTypeSpec;
        pResolvedToken.pMethodSpec = buffers.GetBufferPointer(tokenOut.pMethodSpec_Index);
        pResolvedToken.cbMethodSpec = tokenOut.cbMethodSpec;
    }

    public static Agnostic_CORINFO_SIG_INFO CreateAgnostic_CORINFO_SIG_INFO_without_buffers(in CORINFO_SIG_INFO sigInfo) => new()
    {
        callConv = (uint)sigInfo.callConv,
        retTypeClass = CastHandle(sigInfo.retTypeClass),
        retTypeSigClass = CastHandle(sigInfo.retTypeSigClass),
        retType = (uint)sigInfo.retType,
        flags = (uint)sigInfo.flags,
        numArgs = sigInfo.numArgs,
        sigInst_classInstCount = sigInfo.sigInst.classInstCount,
        sigInst_methInstCount = sigInfo.sigInst.methInstCount,
        args = CastHandle(sigInfo.args),
        cbSig = sigInfo.cbSig,
        methodSignature = CastHandle(sigInfo.methodSignature),
        scope = CastHandle(sigInfo.scope),
        token = (uint)sigInfo.token,
    };

    // Appends the handles to 'handleMap' (creating it if needed), unless the same sequence is already there.
    public static void StoreAgnostic_CORINFO_SIG_INST_HandleArray(uint handleInstCount, CORINFO_CLASS_STRUCT_** handleInstArray,
        ref DenseLightWeightMap<ulong> handleMap, out uint handleInstCountOut, out uint handleInstIndexOut)
    {
        uint handleInstIndex;

        // Checking handleInstArray shouldn't be needed, but crossgen2 often sets (leaves?)
        // handleInstCount > 0 with handleInstArray == null.
        if (handleInstCount > 0 && handleInstArray != null)
        {
            handleMap ??= new DenseLightWeightMap<ulong>();

            // Reuse an existing copy of the array, so that replay finds the same index. This also keeps the
            // handle map small, since there are typically few unique handle arrays.
            handleInstIndex = ContainsHandleMap(handleInstCount, handleInstArray, handleMap);
            if (handleInstIndex == uint.MaxValue)
            {
                handleInstIndex = handleMap.GetCount();
                for (uint i = 0; i < handleInstCount; i++)
                    handleMap.Append(CastHandle(handleInstArray[i]));
                ErrorHandling.AssertCodeMsg(handleInstIndex + handleInstCount == handleMap.GetCount(), ExceptionCodes.MC,
                    "handleInstIndex + handleInstCount == handleMap->GetCount()", "Unexpected size of handleMap");
            }
        }
        else
        {
            // A count without an array is a VM/crossgen2 bug (or a by-design crossgen2 feature to avoid
            // creating an array the JIT doesn't look at).
            handleInstCount = 0;
            handleInstIndex = uint.MaxValue;
        }

        handleInstCountOut = handleInstCount;
        handleInstIndexOut = handleInstIndex;
    }

    public static Agnostic_CORINFO_SIG_INFO StoreAgnostic_CORINFO_SIG_INFO(in CORINFO_SIG_INFO sigInfo, LightWeightMapBuffer buffers, ref DenseLightWeightMap<ulong> handleMap)
    {
        Agnostic_CORINFO_SIG_INFO sig = CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo);
        StoreAgnostic_CORINFO_SIG_INST_HandleArray(sigInfo.sigInst.classInstCount, sigInfo.sigInst.classInst, ref handleMap,
            out sig.sigInst_classInstCount, out sig.sigInst_classInst_Index);
        StoreAgnostic_CORINFO_SIG_INST_HandleArray(sigInfo.sigInst.methInstCount, sigInfo.sigInst.methInst, ref handleMap,
            out sig.sigInst_methInstCount, out sig.sigInst_methInst_Index);
        sig.pSig_Index = buffers.AddBuffer(Bytes(sigInfo.pSig, sigInfo.cbSig));
        return sig;
    }

    // Returns the index at which the handle map holds all the handles in order, or uint.MaxValue.
    public static uint ContainsHandleMap(uint handleCount, CORINFO_CLASS_STRUCT_** handleArray, DenseLightWeightMap<ulong> handleMap)
    {
        if (handleCount == 0)
            return uint.MaxValue;

        uint mapCount = handleMap.GetCount();
        for (uint mapIndex = 0; mapIndex < mapCount; mapIndex++)
        {
            if (mapIndex + handleCount > mapCount)
                return uint.MaxValue;

            bool found = true;
            for (uint handleIndex = 0; handleIndex < handleCount; handleIndex++)
            {
                if (handleMap.Get(mapIndex + handleIndex) != CastHandle(handleArray[handleIndex]))
                {
                    found = false;
                    break;
                }
            }
            if (found)
                return mapIndex;
        }
        return uint.MaxValue;
    }

    // Builds the agnostic form of 'sigInfo' from already recorded data, for use as a lookup key.
    public static Agnostic_CORINFO_SIG_INFO RestoreAgnostic_CORINFO_SIG_INFO(in CORINFO_SIG_INFO sigInfo, LightWeightMapBuffer buffers, DenseLightWeightMap<ulong> handleMap)
    {
        Agnostic_CORINFO_SIG_INFO sig = CreateAgnostic_CORINFO_SIG_INFO_without_buffers(sigInfo);
        sig.sigInst_classInst_Index = ContainsHandleMap(sigInfo.sigInst.classInstCount, sigInfo.sigInst.classInst, handleMap);
        sig.sigInst_methInst_Index = ContainsHandleMap(sigInfo.sigInst.methInstCount, sigInfo.sigInst.methInst, handleMap);
        sig.pSig_Index = (uint)buffers.Contains(Bytes(sigInfo.pSig, sigInfo.cbSig));
        return sig;
    }

    public static CORINFO_CLASS_STRUCT_** DeserializeCORINFO_SIG_INST_HandleArray(uint handleInstCount, uint handleInstIndex,
        DenseLightWeightMap<ulong> handleMap, PinnedArena memoryTracker)
    {
        if (handleInstCount == 0)
            return null;

        var handleInstArray = memoryTracker.Allocate<nint>(handleInstCount);
        for (uint i = 0; i < handleInstCount; i++)
            handleInstArray[i] = (nint)handleMap.Get(handleInstIndex + i);
        return (CORINFO_CLASS_STRUCT_**)handleInstArray;
    }

    public static CORINFO_SIG_INFO Restore_CORINFO_SIG_INFO(in Agnostic_CORINFO_SIG_INFO sigInfo, LightWeightMapBuffer buffers,
        DenseLightWeightMap<ulong> handleMap, PinnedArena memoryTracker) => new()
    {
        callConv = (CorInfoCallConv)sigInfo.callConv,
        retTypeClass = (CORINFO_CLASS_STRUCT_*)sigInfo.retTypeClass,
        retTypeSigClass = (CORINFO_CLASS_STRUCT_*)sigInfo.retTypeSigClass,
        retType = (CorInfoType)sigInfo.retType,
        flags = (CorInfoSigInfoFlags)sigInfo.flags,
        numArgs = (ushort)sigInfo.numArgs,
        args = (CORINFO_ARG_LIST_STRUCT_*)sigInfo.args,
        cbSig = sigInfo.cbSig,
        pSig = buffers.GetBufferPointer(sigInfo.pSig_Index),
        methodSignature = (MethodSignatureInfo*)sigInfo.methodSignature,
        scope = (CORINFO_MODULE_STRUCT_*)sigInfo.scope,
        token = (mdToken)sigInfo.token,
        sigInst = new CORINFO_SIG_INST
        {
            classInstCount = sigInfo.sigInst_classInstCount,
            classInst = DeserializeCORINFO_SIG_INST_HandleArray(sigInfo.sigInst_classInstCount, sigInfo.sigInst_classInst_Index, handleMap, memoryTracker),
            methInstCount = sigInfo.sigInst_methInstCount,
            methInst = DeserializeCORINFO_SIG_INST_HandleArray(sigInfo.sigInst_methInstCount, sigInfo.sigInst_methInst_Index, handleMap, memoryTracker),
        },
    };

    public static Agnostic_CORINFO_LOOKUP_KIND CreateAgnostic_CORINFO_LOOKUP_KIND(ref CORINFO_LOOKUP_KIND pGenericLookupKind)
        => Unsafe.IsNullRef(ref pGenericLookupKind) ? default : new()
        {
            needsRuntimeLookup = pGenericLookupKind.needsRuntimeLookup ? 1u : 0u,
            runtimeLookupKind = (uint)pGenericLookupKind.runtimeLookupKind,
        };

    public static CORINFO_LOOKUP_KIND RestoreCORINFO_LOOKUP_KIND(in Agnostic_CORINFO_LOOKUP_KIND lookupKind) => new()
    {
        needsRuntimeLookup = lookupKind.needsRuntimeLookup != 0,
        runtimeLookupKind = (CORINFO_RUNTIME_LOOKUP_KIND)lookupKind.runtimeLookupKind,
    };

    public static Agnostic_CORINFO_CONST_LOOKUP StoreAgnostic_CORINFO_CONST_LOOKUP(ref CORINFO_CONST_LOOKUP pLookup)
        => new() { accessType = (uint)pLookup.accessType, handle = CastHandle(pLookup.handle) };

    public static CORINFO_CONST_LOOKUP RestoreCORINFO_CONST_LOOKUP(in Agnostic_CORINFO_CONST_LOOKUP lookup)
        => new() { accessType = (InfoAccessType)lookup.accessType, handle = (CORINFO_GENERIC_STRUCT_*)lookup.handle };

    public static Agnostic_CORINFO_RUNTIME_LOOKUP StoreAgnostic_CORINFO_RUNTIME_LOOKUP(ref CORINFO_RUNTIME_LOOKUP pLookup)
    {
        var runtimeLookup = new Agnostic_CORINFO_RUNTIME_LOOKUP
        {
            signature = CastHandle(pLookup.signature),
            helper = (uint)pLookup.helper,
            indirections = pLookup.indirections,
            testForNull = pLookup._testForNull,
            sizeOffset = pLookup.sizeOffset,
            indirectFirstOffset = pLookup._indirectFirstOffset,
            indirectSecondOffset = pLookup._indirectSecondOffset,
            helperEntryPoint = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pLookup.helperEntryPoint),
        };
        for (int i = 0; i < CORINFO_MAXINDIRECTIONS; i++)
            runtimeLookup.offsets[i] = (nuint)Unsafe.Add(ref pLookup.offset0, i);
        return runtimeLookup;
    }

    public static CORINFO_RUNTIME_LOOKUP RestoreCORINFO_RUNTIME_LOOKUP(in Agnostic_CORINFO_RUNTIME_LOOKUP lookup)
    {
        var runtimeLookup = new CORINFO_RUNTIME_LOOKUP
        {
            signature = (void*)lookup.signature,
            helper = (CorInfoHelpFunc)lookup.helper,
            indirections = (ushort)lookup.indirections,
            testForNull = lookup.testForNull != 0,
            sizeOffset = lookup.sizeOffset,
            indirectFirstOffset = lookup.indirectFirstOffset != 0,
            indirectSecondOffset = lookup.indirectSecondOffset != 0,
            helperEntryPoint = RestoreCORINFO_CONST_LOOKUP(lookup.helperEntryPoint),
        };
        for (int i = 0; i < CORINFO_MAXINDIRECTIONS; i++)
            Unsafe.Add(ref runtimeLookup.offset0, i) = (nint)lookup.offsets[i];
        return runtimeLookup;
    }

    public static Agnostic_CORINFO_LOOKUP StoreAgnostic_CORINFO_LOOKUP(ref CORINFO_LOOKUP pLookup)
    {
        var lookup = new Agnostic_CORINFO_LOOKUP { lookupKind = CreateAgnostic_CORINFO_LOOKUP_KIND(ref pLookup.lookupKind) };
        if (pLookup.lookupKind.needsRuntimeLookup)
            lookup.runtimeLookup = StoreAgnostic_CORINFO_RUNTIME_LOOKUP(ref pLookup.runtimeLookup);
        else
            lookup.constLookup = StoreAgnostic_CORINFO_CONST_LOOKUP(ref Unsafe.As<CORINFO_RUNTIME_LOOKUP, CORINFO_CONST_LOOKUP>(ref pLookup.runtimeLookup));
        return lookup;
    }

    public static CORINFO_LOOKUP RestoreCORINFO_LOOKUP(in Agnostic_CORINFO_LOOKUP agnosticLookup)
    {
        var lookup = new CORINFO_LOOKUP { lookupKind = RestoreCORINFO_LOOKUP_KIND(agnosticLookup.lookupKind) };
        if (lookup.lookupKind.needsRuntimeLookup)
            lookup.runtimeLookup = RestoreCORINFO_RUNTIME_LOOKUP(agnosticLookup.runtimeLookup);
        else
            lookup.constLookup = RestoreCORINFO_CONST_LOOKUP(agnosticLookup.constLookup);
        return lookup;
    }

    public static Agnostic_CORINFO_TYPE_LAYOUT_NODE StoreAgnostic_CORINFO_TYPE_LAYOUT_NODE(in CORINFO_TYPE_LAYOUT_NODE node) => new()
    {
        simdTypeHnd = CastHandle(node.simdTypeHnd),
        diagFieldHnd = CastHandle(node.diagFieldHnd),
        parent = node.parent,
        offset = node.offset,
        size = node.size,
        numFields = node.numFields,
        type = (byte)node.type,
        hasSignificantPadding = node.hasSignificantPadding ? (byte)1 : (byte)0,
    };

    public static CORINFO_TYPE_LAYOUT_NODE RestoreCORINFO_TYPE_LAYOUT_NODE(in Agnostic_CORINFO_TYPE_LAYOUT_NODE node) => new()
    {
        simdTypeHnd = (CORINFO_CLASS_STRUCT_*)node.simdTypeHnd,
        diagFieldHnd = (CORINFO_FIELD_STRUCT_*)node.diagFieldHnd,
        parent = node.parent,
        offset = node.offset,
        size = node.size,
        numFields = node.numFields,
        type = (CorInfoType)node.type,
        hasSignificantPadding = node.hasSignificantPadding != 0,
    };

    public static Agnostic_CORINFO_HELPER_DESC StoreAgnostic_CORINFO_HELPER_DESC(in CORINFO_HELPER_DESC desc)
    {
        var result = new Agnostic_CORINFO_HELPER_DESC { helperNum = (uint)desc.helperNum, numArgs = desc.numArgs };
        for (int i = 0; i < CORINFO_ACCESS_ALLOWED_MAX_ARGS; i++)
        {
            CORINFO_HELPER_ARG arg = Unsafe.Add(ref Unsafe.AsRef(in desc.args0), i);
            result.args[i] = new Agnostic_CORINFO_HELPER_ARG { constant = (nuint)arg.argHandle, argType = (uint)arg.argType };
        }
        return result;
    }

    public static CORINFO_HELPER_DESC RestoreCORINFO_HELPER_DESC(in Agnostic_CORINFO_HELPER_DESC desc)
    {
        var result = new CORINFO_HELPER_DESC { helperNum = (CorInfoHelpFunc)desc.helperNum, numArgs = desc.numArgs };
        for (int i = 0; i < CORINFO_ACCESS_ALLOWED_MAX_ARGS; i++)
        {
            Unsafe.Add(ref result.args0, i) = new CORINFO_HELPER_ARG
            {
                argHandle = (nint)desc.args[i].constant,
                argType = (CorInfoAccessAllowedHelperArgType)desc.args[i].argType,
            };
        }
        return result;
    }

    // PatchpointInfo: a fixed header followed by an int32 per local; the local count follows two pointer-sized fields.
    private static readonly int s_patchpointInfoHeaderSize = (8 + sizeof(nint) + 10 * sizeof(int) + 7) & ~7;

    public static nuint PatchpointInfoSize(PatchpointInfo* patchpointInfo)
        => (nuint)s_patchpointInfoHeaderSize + *(uint*)((byte*)patchpointInfo + 8 + sizeof(nint)) * sizeof(int);

    private static ReadOnlySpan<byte> Bytes(byte* data, uint length) => data == null ? default : new ReadOnlySpan<byte>(data, (int)length);
}
