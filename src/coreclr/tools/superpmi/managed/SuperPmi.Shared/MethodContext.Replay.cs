// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.JitInterface;
using Internal.Pgo;

using static SuperPmi.SpmiRecordsHelper;

namespace SuperPmi;

// The replay side of the JIT-EE interface: answers the JIT's queries from the recorded data, and throws
// an SPMI exception (ExceptionCodes.MC) for data that wasn't recorded. Some queries fabricate plausible
// answers instead (marked "Sparse"), where a guess still generates good code.
public sealed unsafe partial class MethodContext
{
    public const ulong CONTINUATION_TYPE_PSEUDO_CLASS_HANDLE = 0x12345678beef0000;

    private PinnedArena tempAllocations;

    private void* AllocJitTempBuffer(nuint size) => (tempAllocations ??= new PinnedArena()).Allocate(size);

    public void Reset()
    {
        cr = new CompileResult();
        tempAllocations = null;
    }

    // 'keyFormat' describes the key for the failure message, as a composite format string of a, b, c and d. It is only
    // formatted on failure, so that the lookups that succeed don't format anything.
    private static TItem LookupByKeyOrMiss<TKey, TItem>(LightWeightMap<TKey, TItem> map, TKey key, string keyFormat = "",
        ulong a = 0, ulong b = 0, ulong c = 0, ulong d = 0,
        [CallerArgumentExpression(nameof(map))] string mapName = "", [CallerArgumentExpression(nameof(key))] string keyName = "")
        where TKey : unmanaged
        where TItem : unmanaged
    {
        if (map == null)
            ErrorHandling.LogException(ExceptionCodes.MC, "SuperPMI assertion failed (missing map " +
                $"{mapName}){string.Format(CultureInfo.InvariantCulture, keyFormat, a, b, c, d)}");

        int index = map.GetIndex(key);
        if (index == -1)
        {
            ErrorHandling.LogException(ExceptionCodes.MC,
                $"SuperPMI assertion failed (missing key \"{(keyName == "0u" ? "0" : keyName)}\" in map " +
                    $"{mapName}){string.Format(CultureInfo.InvariantCulture, keyFormat, a, b, c, d)}");
        }

        return map.GetItem(index);
    }

    private static TItem LookupByKeyOrMiss<TItem>(DenseLightWeightMap<TItem> map, uint key,
        [CallerArgumentExpression(nameof(map))] string mapName = "", [CallerArgumentExpression(nameof(key))] string keyName = "")
        where TItem : unmanaged
    {
        if (map == null)
            ErrorHandling.LogException(ExceptionCodes.MC, $"SuperPMI assertion failed (missing map {mapName})");

        int index = map.GetIndex(key);
        if (index == -1)
            ErrorHandling.LogException(ExceptionCodes.MC, $"SuperPMI assertion failed (missing key \"{(keyName == "0u" ? "0" : keyName)}\" in map {mapName})");

        return map.GetItem(index);
    }

    private static TItem LookupByHandle<TItem>(LightWeightMap<ulong, TItem> map, void* handle, [CallerArgumentExpression(nameof(map))] string mapName = "")
        where TItem : unmanaged
    {
        ulong key = CastHandle(handle);
        return LookupByKeyOrMiss(map, key, ": key {0:X16}", key, mapName: mapName);
    }

    private static void AssertMapExists(LightWeightMapBuffer map, string keyFormat = "", ulong a = 0, ulong b = 0,
        [CallerArgumentExpression(nameof(map))] string mapName = "")
    {
        if (map == null)
            ErrorHandling.LogException(ExceptionCodes.MC, "SuperPMI assertion failed (missing map " +
                $"{mapName}){string.Format(CultureInfo.InvariantCulture, keyFormat, a, b)}");
    }

    private static ReadOnlySpan<byte> Bytes(void* data, nuint length) => data == null ? default : new ReadOnlySpan<byte>(data, (int)length);

    // A null-terminated string, including the terminator.
    private static ReadOnlySpan<byte> CString(byte* s) => new(s, MemoryMarshal.CreateReadOnlySpanFromNullTerminated(s).Length + 1);

    private void Restore_CORINFO_METHOD_INFO(CORINFO_METHOD_INFO* info, in Agnostic_CORINFO_METHOD_INFO value, LightWeightMapBuffer buffers)
    {
        info->ftn = (CORINFO_METHOD_STRUCT_*)value.ftn;
        info->scope = (CORINFO_MODULE_STRUCT_*)value.scope;
        info->ILCode = buffers.GetBufferPointer(value.ILCode_offset);
        info->ILCodeSize = value.ILCodeSize;
        info->maxStack = value.maxStack;
        info->EHcount = value.EHcount;
        info->options = (CorInfoOptions)value.options;
        info->regionKind = (CorInfoRegionKind)value.regionKind;
        info->args = Restore_CORINFO_SIG_INFO(value.args, buffers, SigInstHandleMap, cr.MemoryTracker);
        info->locals = Restore_CORINFO_SIG_INFO(value.locals, buffers, SigInstHandleMap, cr.MemoryTracker);
    }

    private static void Restore_CORINFO_GENERICHANDLE_RESULT(ref CORINFO_GENERICHANDLE_RESULT pResult, in Agnostic_CORINFO_GENERICHANDLE_RESULT value)
    {
        pResult.lookup = RestoreCORINFO_LOOKUP(value.lookup);
        pResult.compileTimeHandle = (CORINFO_GENERIC_STRUCT_*)value.compileTimeHandle;
        pResult.handleType = (CorInfoGenericHandleType)value.handleType;
    }

    private static CORINFO_RESOLVED_TOKEN TokenWithInValues(in Agnostic_CORINFO_RESOLVED_TOKENin inValue) => new()
    {
        tokenContext = (CORINFO_CONTEXT_STRUCT*)inValue.tokenContext,
        tokenScope = (CORINFO_MODULE_STRUCT_*)inValue.tokenScope,
        token = (mdToken)inValue.token,
        tokenType = (CorInfoTokenKind)inValue.tokenType,
    };

    public void repCompileMethod(CORINFO_METHOD_INFO* info, uint* flags, CORINFO_OS* os)
    {
        // The only item in this map is the single group of inputs to compileMethod.
        Agnostic_CompileMethod value = LookupByKeyOrMiss(CompileMethod, 0u);
        Restore_CORINFO_METHOD_INFO(info, value.info, CompileMethod);
        methodSize = info->ILCodeSize;
        *flags = value.flags;
        *os = (CORINFO_OS)value.os;
    }

    public CORINFO_CLASS_STRUCT_* repGetMethodClass(CORINFO_METHOD_STRUCT_* methodHandle) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetMethodClass, methodHandle);

    public uint repGetClassAttribs(CORINFO_CLASS_STRUCT_* classHandle) => LookupByHandle(GetClassAttribs, classHandle);

    public bool repIsIntrinsic(CORINFO_METHOD_STRUCT_* ftn) => LookupByHandle(IsIntrinsic, ftn) != 0;

    public bool repNotifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn) => LookupByHandle(NotifyMethodInfoUsage, ftn) != 0;

    public bool repNotifyInstructionSetUsage(InstructionSet isa, bool supported, bool preserveNegativeDependency)
    {
        var key = new DD { A = (uint)isa, B = (supported ? 1u : 0u) | (preserveNegativeDependency ? 2u : 0u) };
        int index = NotifyInstructionSetUsage?.GetIndex(key) ?? -1;

        // Fall back to the most likely answer instead of a miss, since ISA usage changes are quite
        // common with JIT changes.
        return index == -1 ? supported : NotifyInstructionSetUsage.GetItem(index) != 0;
    }

    public uint repGetMethodAttribs(CORINFO_METHOD_STRUCT_* methodHandle)
    {
        uint value = LookupByHandle(GetMethodAttribs, methodHandle);
        if ((cr.repSetMethodAttribs(methodHandle) & CorInfoMethodRuntimeFlags.CORINFO_FLG_BAD_INLINEE) != 0)
            value |= (uint)CorInfoFlag.CORINFO_FLG_DONT_INLINE;
        return value;
    }

    public byte* repGetClassAssemblyName(CORINFO_CLASS_STRUCT_* cls) => GetClassAssemblyName.GetBufferPointer(LookupByHandle(GetClassAssemblyName, cls));

    public void repGetVars(CORINFO_METHOD_STRUCT_* ftn, ref uint cVars, ILVarInfo** vars_in, ref bool extendOthers)
    {
        if (GetVars == null)
        {
            cVars = 0;
            return;
        }

        Agnostic_GetVars value = GetVars.Get(CastHandle(ftn));
        cVars = value.cVars;
        if (cVars > 0)
            *vars_in = (ILVarInfo*)GetVars.GetBufferPointer(value.vars_offset);
        extendOthers = value.extendOthers != 0;
    }

    public void repGetBoundaries(CORINFO_METHOD_STRUCT_* ftn, ref uint cILOffsets, ref uint* pILOffsets, BoundaryTypes* implicitBoundaries)
    {
        Agnostic_GetBoundaries value = LookupByHandle(GetBoundaries, ftn);
        cILOffsets = value.cILOffsets;
        if (cILOffsets > 0)
            pILOffsets = (uint*)GetBoundaries.GetBufferPointer(value.pILOffset_offset);
        *implicitBoundaries = (BoundaryTypes)value.implicitBoundaries;
    }

    public CorInfoInitClassResult repInitClass(CORINFO_FIELD_STRUCT_* field, CORINFO_METHOD_STRUCT_* method, CORINFO_CONTEXT_STRUCT* context)
    {
        var key = new Agnostic_InitClass { field = CastHandle(field), method = CastHandle(method), context = CastHandle(context) };
        return (CorInfoInitClassResult)LookupByKeyOrMiss(InitClass, key, ": fld-{0:X16} meth-{1:X16} con-{2:X16}", key.field, key.method, key.context);
    }

    public byte* repGetMethodNameFromMetadata(CORINFO_METHOD_STRUCT_* ftn, byte** moduleName, byte** namespaceName, byte** enclosingClassNames,
        nuint maxEnclosingClassNames)
    {
        var key = new Agnostic_CORINFO_METHODNAME_TOKENin
        {
            ftn = CastHandle(ftn),
            className = moduleName != null ? 1u : 0u,
            namespaceName = namespaceName != null ? 1u : 0u,
            maxEnclosingClassNames = (uint)maxEnclosingClassNames,
        };

        Agnostic_CORINFO_METHODNAME_TOKENout value = LookupByKeyOrMiss(GetMethodNameFromMetadata, key,
            ": ftn-{0:X16} className-{1} namespaceName-{2} maxEnclosingClassNames-{3}", key.ftn, key.className, key.namespaceName, key.maxEnclosingClassNames);

        byte* result = GetMethodNameFromMetadata.GetBufferPointer(value.methodName);
        if (moduleName != null)
            *moduleName = GetMethodNameFromMetadata.GetBufferPointer(value.className);
        if (namespaceName != null)
            *namespaceName = GetMethodNameFromMetadata.GetBufferPointer(value.namespaceName);

        byte* enclosingClassNameIndices = GetMethodNameFromMetadata.GetBufferPointer(value.enclosingClassNames);
        for (nuint i = 0; i < maxEnclosingClassNames; i++)
            enclosingClassNames[i] = GetMethodNameFromMetadata.GetBufferPointer(Unsafe.ReadUnaligned<uint>(enclosingClassNameIndices + i * sizeof(uint)));

        return result;
    }

    public uint repGetJitFlags(ref CORJIT_FLAGS jitFlags, uint sizeInBytes)
    {
        DD value = LookupByKeyOrMiss(GetJitFlags, 0u);
        ErrorHandling.AssertCode(sizeInBytes >= value.B, ExceptionCodes.ASSERT, "sizeInBytes >= value.B");
        GetJitFlags.GetBuffer(value.A).Slice(0, (int)value.B).CopyTo(
            MemoryMarshal.CreateSpan(ref Unsafe.As<CORJIT_FLAGS, byte>(ref jitFlags), (int)sizeInBytes));
        return value.B;
    }

    public CorInfoInline repCanInline(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, ref uint exceptionCode)
    {
        var key = new DLDL { A = CastHandle(callerHnd), B = CastHandle(calleeHnd) };
        if (CanInline == null || CanInline.GetIndex(key) == -1)
        {
            Logger.LogDebug("Sparse - repCanInline saying INLINE_FAIL");
            return CorInfoInline.INLINE_FAIL; // if we have no info, it's pretty safe to say we can't inline it.
        }

        Agnostic_CanInline value = CanInline.Get(key);
        exceptionCode = value.exceptionCode;
        return (CorInfoInline)value.result;
    }

    public void repResolveToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken, out uint exceptionCode)
    {
        Agnostic_CORINFO_RESOLVED_TOKENin key = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken);
        ResolveTokenValue value = LookupByKeyOrMiss(ResolveToken, key, ": token {0:x}", (uint)pResolvedToken.token);
        Restore_CORINFO_RESOLVED_TOKENout(ref pResolvedToken, value.tokenOut, ResolveToken);
        exceptionCode = value.exceptionCode;
    }

    public void repGetCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle,
        CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult, out uint exceptionCode)
    {
        AssertMapExists(GetCallInfo);

        var key = new Agnostic_GetCallInfo
        {
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetCallInfo),
            callerHandle = CastHandle(callerHandle),
            flags = (uint)flags,
        };
        if (pConstrainedResolvedToken != null)
            key.ConstrainedResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref *pConstrainedResolvedToken, GetCallInfo);

        Agnostic_CORINFO_CALL_INFO value = LookupByKeyOrMiss(GetCallInfo, key, ": key {0:x8}, {1:x16}", key.ResolvedToken.inValue.token, key.ResolvedToken.outValue.hClass);

        if (value.exceptionCode != 0)
        {
            // If there was an exception, the stored data is ignored.
            *pResult = default;
        }
        else
        {
            pResult->hMethod = (CORINFO_METHOD_STRUCT_*)value.hMethod;
            pResult->methodFlags = value.methodFlags;

            // getCallInfo may have been recorded earlier in this context without CORINFO_FLG_DONT_INLINE.
            // If the JIT later realized the method is never inlined, it would call setMethodAttribs, but
            // a later getCallInfo wouldn't update the recorded entry. Mimic the runtime by computing this
            // dynamic flag here (as repGetMethodAttribs does). This doesn't handle all cases; for example,
            // parallelism could still record these flags without CORINFO_FLG_DONT_INLINE.
            if ((cr.repSetMethodAttribs(pResult->hMethod) & CorInfoMethodRuntimeFlags.CORINFO_FLG_BAD_INLINEE) != 0)
                pResult->methodFlags |= (uint)CorInfoFlag.CORINFO_FLG_DONT_INLINE;

            pResult->classFlags = value.classFlags;
            pResult->sig = Restore_CORINFO_SIG_INFO(value.sig, GetCallInfo, SigInstHandleMap, cr.MemoryTracker);
            pResult->accessAllowed = (CorInfoIsAccessAllowedResult)value.accessAllowed;
            pResult->callsiteCalloutHelper = RestoreCORINFO_HELPER_DESC(value.callsiteCalloutHelper);
            pResult->thisTransform = (CORINFO_THIS_TRANSFORM)value.thisTransform;
            pResult->kind = (CORINFO_CALL_KIND)value.kind;
            pResult->nullInstanceCheck = value.nullInstanceCheck != 0;
            pResult->contextHandle = (CORINFO_CONTEXT_STRUCT*)value.contextHandle;
            pResult->exactContextNeedsRuntimeLookup = value.exactContextNeedsRuntimeLookup != 0;
            pResult->codePointerOrStubLookup = RestoreCORINFO_LOOKUP(value.stubLookup);

            if (pResult->kind == CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB)
                cr.CallTargetTypes.Add(CastHandle(pResult->codePointerOrStubLookup.constLookup.addr), (uint)CORINFO_CALL_KIND.CORINFO_VIRTUALCALL_STUB);

            pResult->instParamLookup = RestoreCORINFO_CONST_LOOKUP(value.instParamLookup);
        }

        exceptionCode = value.exceptionCode;
    }

    public void repGetCallInfoFromMethodHandle(CORINFO_METHOD_STRUCT_* methodHandle, CORINFO_CALL_INFO* pResult)
    {
        if (GetCallInfo != null)
        {
            for (int i = 0; i < GetCallInfo.GetCount(); i++)
            {
                Agnostic_GetCallInfo key = GetCallInfo.GetKey(i);
                if ((CORINFO_METHOD_STRUCT_*)GetCallInfo.GetItem(i).hMethod != methodHandle)
                    continue;

                CORINFO_RESOLVED_TOKEN resolvedToken = TokenWithInValues(key.ResolvedToken.inValue);
                repResolveToken(ref resolvedToken, out _);

                // If the original call passed a null constrainedResolvedToken, it can only be replayed with
                // a null one too, not with the address of a local (but meaningless) token.
                CORINFO_RESOLVED_TOKEN constrainedResolvedToken;
                CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken = null;
                if (key.ConstrainedResolvedToken.inValue.tokenContext != 0 && key.ConstrainedResolvedToken.inValue.tokenScope != 0)
                {
                    constrainedResolvedToken = TokenWithInValues(key.ConstrainedResolvedToken.inValue);
                    pConstrainedResolvedToken = &constrainedResolvedToken;
                    repResolveToken(ref *pConstrainedResolvedToken, out _);
                }

                repGetCallInfo(ref resolvedToken, pConstrainedResolvedToken, (CORINFO_METHOD_STRUCT_*)key.callerHandle,
                    (CORINFO_CALLINFO_FLAGS)key.flags, pResult, out _);
                return;
            }
        }

        ErrorHandling.LogException(ExceptionCodes.MC, $"Didn't find key {CastHandle(methodHandle):X16}.");
    }

    public void repExpandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        var key = new Agnostic_ExpandRawHandleIntrinsic
        {
            ResolvedToken = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken),
            hCallerHandle = CastHandle(callerHandle),
        };
        Agnostic_CORINFO_GENERICHANDLE_RESULT value = LookupByKeyOrMiss(ExpandRawHandleIntrinsic, key, ": key {0:x}", (uint)pResolvedToken.token);
        Restore_CORINFO_GENERICHANDLE_RESULT(ref pResult, value);
    }

    public bool repIsIntrinsicType(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsIntrinsicType, cls) != 0;

    public CorInfoType repAsCorInfoType(CORINFO_CLASS_STRUCT_* cls) => (CorInfoType)LookupByHandle(AsCorInfoType, cls);

    public bool repIsValueClass(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsValueClass, cls) != 0;

    public bool repIsByRefLike(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsByRefLike, cls) != 0;

    public bool repIsSharedInst(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsSharedInst, cls) != 0;

    public uint repGetClassSize(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(GetClassSize, cls);

    public uint repGetHeapClassSize(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(GetHeapClassSize, cls);

    public bool repCanAllocateOnStack(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(CanAllocateOnStack, cls) != 0;

    public uint repGetClassNumInstanceFields(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(GetClassNumInstanceFields, cls);

    public CorInfoHelpFunc repGetNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls) => (CorInfoHelpFunc)LookupByHandle(GetNewArrHelper, arrayCls);

    public CorInfoHelpFunc repGetSharedCCtorHelper(CORINFO_CLASS_STRUCT_* clsHnd) => (CorInfoHelpFunc)LookupByHandle(GetSharedCCtorHelper, clsHnd);

    public CORINFO_CLASS_STRUCT_* repGetTypeForBox(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetTypeForBox, cls);

    public CorInfoHelpFunc repGetBoxHelper(CORINFO_CLASS_STRUCT_* cls) => (CorInfoHelpFunc)LookupByHandle(GetBoxHelper, cls);

    public CORINFO_CLASS_STRUCT_* repGetBuiltinClass(CorInfoClassId classId)
    {
        uint key = (uint)classId;
        return (CORINFO_CLASS_STRUCT_*)LookupByKeyOrMiss(GetBuiltinClass, key, ": key {0:X8}", key);
    }

    public CorInfoType repGetTypeForPrimitiveValueClass(CORINFO_CLASS_STRUCT_* cls) => (CorInfoType)LookupByHandle(GetTypeForPrimitiveValueClass, cls);

    public CorInfoType repGetTypeForPrimitiveNumericClass(CORINFO_CLASS_STRUCT_* cls) => (CorInfoType)LookupByHandle(GetTypeForPrimitiveNumericClass, cls);

    public CORINFO_CLASS_STRUCT_* repGetParentType(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetParentType, cls);

    public bool repIsSDArray(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsSDArray, cls) != 0;

    public CORINFO_CLASS_STRUCT_* repGetFieldClass(CORINFO_FIELD_STRUCT_* field) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetFieldClass, field);

    public uint repGetFieldOffset(CORINFO_FIELD_STRUCT_* field) => LookupByHandle(GetFieldOffset, field);

    public CorInfoHelpFunc repGetUnBoxHelper(CORINFO_CLASS_STRUCT_* cls) => (CorInfoHelpFunc)LookupByHandle(GetUnBoxHelper, cls);

    public CORINFO_OBJECT_STRUCT_* repGetRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_OBJECT_STRUCT_*)LookupByHandle(GetRuntimeTypePointer, cls);

    public bool repIsObjectImmutable(CORINFO_OBJECT_STRUCT_* objPtr) => LookupByHandle(IsObjectImmutable, objPtr) != 0;

    public bool repGetStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* charValue)
    {
        var key = new DLD { A = CastHandle(strObj), B = (uint)index };
        DD value = LookupByKeyOrMiss(GetStringChar, key, ": key {0:X16}", key.A);
        *charValue = (ushort)value.B;
        return value.A != 0;
    }

    public CORINFO_CLASS_STRUCT_* repGetObjectType(CORINFO_OBJECT_STRUCT_* objPtr) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetObjectType, objPtr);

    public bool repGetReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CorInfoHelpFunc id, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_CONST_LOOKUP pLookup)
    {
        AssertMapExists(GetReadyToRunHelper);

        var key = new GetReadyToRunHelper_TOKENin
        {
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetReadyToRunHelper),
            id = (uint)id,
            callerHandle = CastHandle(callerHandle),
        };
        GetReadyToRunHelper_TOKENout value = LookupByKeyOrMiss(GetReadyToRunHelper, key);
        pLookup = RestoreCORINFO_CONST_LOOKUP(value.Lookup);
        return value.result != 0;
    }

    public void repGetReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType,
        CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_LOOKUP pLookup)
    {
        AssertMapExists(GetReadyToRunDelegateCtorHelper);

        var key = new GetReadyToRunDelegateCtorHelper_TOKENIn
        {
            TargetMethod = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pTargetMethod, GetReadyToRunDelegateCtorHelper),
            targetConstraint = (uint)targetConstraint,
            delegateType = CastHandle(delegateType),
            callerHandle = CastHandle(callerHandle),
        };
        pLookup = RestoreCORINFO_LOOKUP(LookupByKeyOrMiss(GetReadyToRunDelegateCtorHelper, key));
    }

    public void repGetHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP* pNativeEntrypoint, CORINFO_METHOD_STRUCT_** pMethodHandle)
    {
        uint key = (uint)ftnNum;
        Agnostic_GetHelperFtn value = LookupByKeyOrMiss(GetHelperFtn, key, ": key {0}", key);
        if (pNativeEntrypoint != null)
            *pNativeEntrypoint = RestoreCORINFO_CONST_LOOKUP(value.helperLookup);
        if (pMethodHandle != null)
            *pMethodHandle = (CORINFO_METHOD_STRUCT_*)value.helperMethod;
    }

    public bool fndGetHelperFtn(void* functionAddress, out CorInfoHelpFunc result)
    {
        if (GetHelperFtn != null)
        {
            for (int i = 0; i < GetHelperFtn.GetCount(); i++)
            {
                // TODO-Cleanup: this only compares the function addresses, and doesn't account for
                // ppIndirection, which will break if the helper is a dynamic helper function.
                if (GetHelperFtn.GetItem(i).helperLookup.handle == CastHandle(functionAddress))
                {
                    result = (CorInfoHelpFunc)GetHelperFtn.GetKey(i);
                    return true;
                }
            }
        }

        Logger.LogDebug($"fndGetHelperFtn - didn't find value 0x{CastHandle(functionAddress):x}");
        result = default;
        return false;
    }

    public CORINFO_JUST_MY_CODE_HANDLE_* repGetJustMyCodeHandle(CORINFO_METHOD_STRUCT_* method, ref CORINFO_JUST_MY_CODE_HANDLE_* ppIndirection)
    {
        DLDL value = LookupByHandle(GetJustMyCodeHandle, method);
        ppIndirection = (CORINFO_JUST_MY_CODE_HANDLE_*)value.A;
        return (CORINFO_JUST_MY_CODE_HANDLE_*)value.B;
    }

    public void repGetFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
    {
        var key = new DLD { A = CastHandle(ftn), B = (uint)accessFlags };

        if (GetFunctionEntryPoint == null || GetFunctionEntryPoint.GetIndex(key) == -1)
        {
            key.B ^= (uint)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_NONNULL;
            if (GetFunctionEntryPoint != null && GetFunctionEntryPoint.GetIndex(key) != -1)
            {
                Logger.LogDebug("Sparse - repGetFunctionEntryPoint found result with inverted CORINFO_ACCESS_NONNULL");
            }
            else
            {
                Logger.LogDebug("Sparse - repGetFunctionEntryPoint fabricated result for request.");
                pResult.accessType = InfoAccessType.IAT_PVALUE;
                pResult.addr = (void*)(CastHandle(ftn) + 0x1c);
                return;
            }
        }

        DLD value = GetFunctionEntryPoint.Get(key);
        pResult.accessType = (InfoAccessType)value.B;
        pResult.addr = (void*)value.A;
    }

    public bool fndGetFunctionEntryPoint(DLD value, out CORINFO_METHOD_STRUCT_* result)
    {
        if (GetFunctionEntryPoint != null)
        {
            for (int i = 0; i < GetFunctionEntryPoint.GetCount(); i++)
            {
                // TODO-Cleanup: we should be more conscious of the rest of the information in CORINFO_CONST_LOOKUP
                if ((uint)GetFunctionEntryPoint.GetItem(i).A == (uint)value.A)
                {
                    result = (CORINFO_METHOD_STRUCT_*)GetFunctionEntryPoint.GetKey(i).A;
                    return true;
                }
            }
        }

        Logger.LogDebug($"fndGetFunctionEntryPoint - didn't find value {value.A:X16}");
        result = null;
        return false;
    }

    public InfoAccessType repConstructStringLiteral(CORINFO_MODULE_STRUCT_* moduleHandle, mdToken metaTok, ref void* ppValue)
    {
        var key = new DLD { A = CastHandle(moduleHandle), B = (uint)metaTok };
        DLD value = LookupByKeyOrMiss(ConstructStringLiteral, key, ": key {0:X16}", CastHandle(moduleHandle));
        ppValue = (void*)value.A;
        return (InfoAccessType)value.B;
    }

    public bool repConvertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fMustConvert)
    {
        AssertMapExists(ConvertPInvokeCalliToCall);

        var key = new DLD { A = CastHandle(pResolvedToken.tokenScope), B = (uint)pResolvedToken.token };
        DLDL value = LookupByKeyOrMiss(ConvertPInvokeCalliToCall, key);
        pResolvedToken.hClass = (CORINFO_CLASS_STRUCT_*)value.A;
        pResolvedToken.hMethod = (CORINFO_METHOD_STRUCT_*)value.B;
        return value.B != 0;
    }

    public InfoAccessType repEmptyStringLiteral(ref void* ppValue)
    {
        // TODO-Cleanup: sketchy if someone calls this twice
        DLD temp2 = LookupByKeyOrMiss(EmptyStringLiteral, 0u);
        ppValue = (void*)temp2.A;
        return (InfoAccessType)temp2.B;
    }

    public CorInfoTypeWithMod repGetArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_** vcTypeRet, out uint exceptionCode)
    {
        AssertMapExists(GetArgType, ": key {0:X16} {1:X16}", CastHandle(sig->scope), CastHandle(args));

        var key = new Agnostic_GetArgType_Key
        {
            flags = (uint)sig->flags,
            numArgs = sig->numArgs,
            sigInst_classInstCount = sig->sigInst.classInstCount,
            sigInst_methInstCount = sig->sigInst.methInstCount,
            methodSignature = CastHandle(sig->methodSignature),
            scope = CastHandle(sig->scope),
            args = CastHandle(args),
            sigInst_classInst_Index = ContainsHandleMap(sig->sigInst.classInstCount, sig->sigInst.classInst, SigInstHandleMap),
            sigInst_methInst_Index = ContainsHandleMap(sig->sigInst.methInstCount, sig->sigInst.methInst, SigInstHandleMap),
        };

        Agnostic_GetArgType_Value value = LookupByKeyOrMiss(GetArgType, key, ": key {0:X16} {1:X16}", key.scope, key.args);
        *vcTypeRet = (CORINFO_CLASS_STRUCT_*)value.vcTypeRet;
        exceptionCode = value.exceptionCode;
        return (CorInfoTypeWithMod)value.result;
    }

    public int repGetExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet)
    {
        var key = new DLD { A = CastHandle(baseType), B = (uint)maxExactClasses };
        Agnostic_GetExactClassesResult value = LookupByKeyOrMiss(GetExactClasses, key, ": key {0:X16} {1:X8}", key.A, key.B);
        ErrorHandling.AssertCode(maxExactClasses >= value.numClasses, ExceptionCodes.ASSERT, "maxExactClasses >= value.numClasses");

        byte* buffer = GetExactClasses.GetBufferPointer(value.classes);
        for (int i = 0; i < value.numClasses; i++)
            exactClsRet[i] = (CORINFO_CLASS_STRUCT_*)Unsafe.ReadUnaligned<ulong>(buffer + i * sizeof(ulong));
        return value.numClasses;
    }

    public CORINFO_ARG_LIST_STRUCT_* repGetArgNext(CORINFO_ARG_LIST_STRUCT_* args) => (CORINFO_ARG_LIST_STRUCT_*)LookupByHandle(GetArgNext, args);

    public void repGetMethodSig(CORINFO_METHOD_STRUCT_* ftn, CORINFO_SIG_INFO* sig, CORINFO_CLASS_STRUCT_* memberParent)
    {
        var key = new DLDL { A = CastHandle(ftn), B = CastHandle(memberParent) };
        Agnostic_CORINFO_SIG_INFO value = LookupByKeyOrMiss(GetMethodSig, key, ": key ftn-{0:X16} prt-{1:X16}", key.A, key.B);
        *sig = Restore_CORINFO_SIG_INFO(value, GetMethodSig, SigInstHandleMap, cr.MemoryTracker);
    }

    public CORINFO_CLASS_STRUCT_* repGetArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, out uint exceptionCode)
    {
        var key = new Agnostic_GetArgClass_Key
        {
            sigInst_classInstCount = sig->sigInst.classInstCount,
            sigInst_methInstCount = sig->sigInst.methInstCount,
            methodSignature = CastHandle(sig->methodSignature),
            scope = CastHandle(sig->scope),
            args = CastHandle(args),
            sigInst_classInst_Index = ContainsHandleMap(sig->sigInst.classInstCount, sig->sigInst.classInst, SigInstHandleMap),
            sigInst_methInst_Index = ContainsHandleMap(sig->sigInst.methInstCount, sig->sigInst.methInst, SigInstHandleMap),
        };

        Agnostic_GetArgClass_Value value = LookupByKeyOrMiss(GetArgClass, key, ": key {0:X16} {1:X16}", key.scope, key.args);
        exceptionCode = value.exceptionCode;
        return (CORINFO_CLASS_STRUCT_*)value.result;
    }

    public CorInfoHFAElemType repGetHFAType(CORINFO_CLASS_STRUCT_* clsHnd) => (CorInfoHFAElemType)LookupByHandle(GetHFAType, clsHnd);

    public bool repGetMethodInfo(CORINFO_METHOD_STRUCT_* ftn, CORINFO_METHOD_INFO* info, CORINFO_CONTEXT_STRUCT* context, out uint exceptionCode)
    {
        var key = new DLDL { A = CastHandle(ftn), B = CastHandle(context) };
        Agnostic_GetMethodInfo value = LookupByKeyOrMiss(GetMethodInfo, key, ": key {0:X16}", key.A);
        if (value.result != 0)
            Restore_CORINFO_METHOD_INFO(info, value.info, GetMethodInfo);
        exceptionCode = value.exceptionCode;
        return value.result != 0;
    }

    public bool repHaveSameMethodDefinition(CORINFO_METHOD_STRUCT_* methHnd1, CORINFO_METHOD_STRUCT_* methHnd2)
    {
        var key = new DLDL { A = CastHandle(methHnd1), B = CastHandle(methHnd2) };
        return LookupByKeyOrMiss(HaveSameMethodDefinition, key, "key {0:X16}, {1:X16}", key.A, key.B) != 0;
    }

    public CORINFO_CLASS_STRUCT_* repGetTypeDefinition(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetTypeDefinition, cls);

    public CorInfoHelpFunc repGetNewHelper(CORINFO_CLASS_STRUCT_* classHandle, ref bool pHasSideEffects, out uint exceptionCode)
    {
        DDD value = LookupByHandle(GetNewHelper, classHandle);
        if (!Unsafe.IsNullRef(ref pHasSideEffects))
            pHasSideEffects = value.A != 0;
        exceptionCode = value.C;
        return (CorInfoHelpFunc)value.B;
    }

    public void repEmbedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        AssertMapExists(EmbedGenericHandle);

        var key = new Agnostic_EmbedGenericHandle
        {
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, EmbedGenericHandle),
            fEmbedParent = fEmbedParent ? 1u : 0u,
            hCallerHandle = CastHandle(callerHandle),
        };
        Restore_CORINFO_GENERICHANDLE_RESULT(ref pResult, LookupByKeyOrMiss(EmbedGenericHandle, key));
    }

    public void repGetEHinfo(CORINFO_METHOD_STRUCT_* ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        AssertMapExists(GetEHinfo, ": key {0:X16}", CastHandle(ftn));

        var key = new DLD { A = CastHandle(ftn), B = EHnumber };
        Agnostic_CORINFO_EH_CLAUSE value = LookupByKeyOrMiss(GetEHinfo, key, ": key {0:X16}", CastHandle(ftn));
        clause.Flags = (CORINFO_EH_CLAUSE_FLAGS)value.Flags;
        clause.TryOffset = value.TryOffset;
        clause.TryLength = value.TryLength;
        clause.HandlerOffset = value.HandlerOffset;
        clause.HandlerLength = value.HandlerLength;
        clause.ClassTokenOrOffset = value.ClassToken;
    }

    public void repGetMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
    {
        DDD value = LookupByHandle(GetMethodVTableOffset, method);
        offsetOfIndirection = value.A;
        offsetAfterIndirection = value.B;
        isRelative = value.C != 0;
    }

    public bool repResolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO* info)
    {
        var key = new Agnostic_ResolveVirtualMethodKey
        {
            virtualMethod = CastHandle(info->virtualMethod),
            objClass = CastHandle(info->objClass),
            context = CastHandle(info->context),
            pResolvedTokenVirtualMethodNonNull = info->pResolvedTokenVirtualMethod != null ? 1u : 0u,
        };
        if (key.pResolvedTokenVirtualMethodNonNull != 0)
            key.pResolvedTokenVirtualMethod = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref *info->pResolvedTokenVirtualMethod, ResolveToken);

        Agnostic_ResolveVirtualMethodResult result = LookupByKeyOrMiss(ResolveVirtualMethod, key, ": {0:X16}-{1:X16}-{2:X16}-{3:X8}",
            key.virtualMethod, key.objClass, key.context, key.pResolvedTokenVirtualMethodNonNull);

        info->devirtualizedMethod = (CORINFO_METHOD_STRUCT_*)result.devirtualizedMethod;
        info->tokenLookupContext = (CORINFO_CONTEXT_STRUCT*)result.tokenLookupContext;
        info->detail = (CORINFO_DEVIRTUALIZATION_DETAIL)result.detail;
        info->instParamLookup = RestoreCORINFO_LOOKUP(result.instParamLookup);
        if (result.returnValue != 0)
        {
            info->resolvedTokenDevirtualizedMethod = Restore_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedMethod, ResolveToken);
            info->resolvedTokenDevirtualizedUnboxedMethod = Restore_CORINFO_RESOLVED_TOKEN(result.resolvedTokenDevirtualizedUnboxedMethod, ResolveToken);
        }
        return result.returnValue != 0;
    }

    public CORINFO_METHOD_STRUCT_* repGetAsyncOtherVariant(CORINFO_METHOD_STRUCT_* ftn, ref bool variantIsThunk)
    {
        DLD value = LookupByHandle(GetAsyncOtherVariant, ftn);
        variantIsThunk = value.B != 0;
        return (CORINFO_METHOD_STRUCT_*)value.A;
    }

    public CORINFO_CLASS_STRUCT_* repGetDefaultComparerClass(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetDefaultComparerClass, cls);

    public CORINFO_CLASS_STRUCT_* repGetDefaultEqualityComparerClass(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetDefaultEqualityComparerClass, cls);

    public CORINFO_CLASS_STRUCT_* repGetSZArrayHelperEnumeratorClass(CORINFO_CLASS_STRUCT_* cls) => (CORINFO_CLASS_STRUCT_*)LookupByHandle(GetSZArrayHelperEnumeratorClass, cls);

    public CORINFO_CLASS_STRUCT_* repGetTokenTypeAsHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        var key = new GetTokenTypeAsHandleValue { hMethod = CastHandle(pResolvedToken.hMethod), hField = CastHandle(pResolvedToken.hField) };
        return (CORINFO_CLASS_STRUCT_*)LookupByKeyOrMiss(GetTokenTypeAsHandle, key);
    }

    public void repGetFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
    {
        AssertMapExists(GetFieldInfo, ": key {0:x}", (uint)pResolvedToken.token);

        var key = new Agnostic_GetFieldInfo
        {
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetFieldInfo),
            callerHandle = CastHandle(callerHandle),
            flags = (uint)flags,
        };

        uint origFlag = key.flags;
        int keyIndex = GetFieldInfo.GetIndex(key);
        if (keyIndex == -1)
        {
            key.flags = origFlag ^ (uint)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_THIS;
            keyIndex = GetFieldInfo.GetIndex(key);
            if (keyIndex != -1)
            {
                Logger.LogDebug("Sparse - repGetFieldInfo found value with inverted CORINFO_ACCESS_THIS");
            }
            else
            {
                key.flags = origFlag ^ (uint)CORINFO_ACCESS_FLAGS.CORINFO_ACCESS_INLINECHECK;
                keyIndex = GetFieldInfo.GetIndex(key);
                if (keyIndex != -1)
                    Logger.LogDebug("Sparse - repGetFieldInfo found value with inverted CORINFO_ACCESS_INLINECHECK");
                else
                    ErrorHandling.LogException(ExceptionCodes.MC, $"repGetFieldInfo: didn't find {(uint)pResolvedToken.token:x}");
            }
        }

        Agnostic_CORINFO_FIELD_INFO value = GetFieldInfo.GetItem(keyIndex);
        pResult->fieldAccessor = (CORINFO_FIELD_ACCESSOR)value.fieldAccessor;
        pResult->fieldFlags = (CORINFO_FIELD_FLAGS)value.fieldFlags;
        pResult->helper = (CorInfoHelpFunc)value.helper;
        pResult->offset = value.offset;
        pResult->fieldType = (CorInfoType)value.fieldType;
        pResult->structType = (CORINFO_CLASS_STRUCT_*)value.structType;
        pResult->accessAllowed = (CorInfoIsAccessAllowedResult)value.accessAllowed;
        pResult->accessCalloutHelper = RestoreCORINFO_HELPER_DESC(value.accessCalloutHelper);
        pResult->fieldLookup = RestoreCORINFO_CONST_LOOKUP(value.fieldLookup);
    }

    public uint repGetThreadLocalFieldInfo(CORINFO_FIELD_STRUCT_* field, bool isGCType)
    {
        var key = new DLD { A = CastHandle(field), B = isGCType ? 0u : 1u };
        return LookupByKeyOrMiss(GetThreadLocalFieldInfo, key, ": key hnd-{0:X16}, gctype-{1}", key.A, key.B);
    }

    public void repGetThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
    {
        uint key = 0;
        Agnostic_GetThreadLocalStaticBlocksInfo value = LookupByKeyOrMiss(GetThreadLocalStaticBlocksInfo, key, ": key {0}", key);
        pInfo->tlsIndex = RestoreCORINFO_CONST_LOOKUP(value.tlsIndex);
        pInfo->tlsGetAddrFtnPtr = (nuint)value.tlsGetAddrFtnPtr;
        pInfo->tlsIndexObject = (nuint)value.tlsIndexObject;
        pInfo->threadVarsSection = (nuint)value.threadVarsSection;
        pInfo->offsetOfThreadLocalStoragePointer = value.offsetOfThreadLocalStoragePointer;
        pInfo->offsetOfMaxThreadStaticBlocks = value.offsetOfMaxThreadStaticBlocks;
        pInfo->offsetOfThreadStaticBlocks = value.offsetOfThreadStaticBlocks;
        pInfo->offsetOfBaseOfThreadLocalData = value.offsetOfBaseOfThreadLocalData;
    }

    public void repGetThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
    {
        ulong key = 1;
        Agnostic_GetThreadStaticInfo_NativeAOT value = LookupByKeyOrMiss(GetThreadLocalStaticInfo_NativeAOT, key, ": key {0:X16}", key);
        pInfo->tlsRootObject = RestoreCORINFO_CONST_LOOKUP(value.tlsRootObject);
        pInfo->tlsIndexObject = RestoreCORINFO_CONST_LOOKUP(value.tlsIndexObject);
        pInfo->offsetOfThreadLocalStoragePointer = value.offsetOfThreadLocalStoragePointer;
        pInfo->threadStaticBaseSlow = RestoreCORINFO_CONST_LOOKUP(value.threadStaticBaseSlow);
        pInfo->tlsGetAddrFtnPtr = RestoreCORINFO_CONST_LOOKUP(value.tlsGetAddrFtnPtr);
    }

    public CORINFO_METHOD_STRUCT_* repEmbedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection)
    {
        DLDL value = LookupByHandle(EmbedMethodHandle, handle);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return (CORINFO_METHOD_STRUCT_*)value.B;
    }

    public bool repGetStaticFieldContent(CORINFO_FIELD_STRUCT_* field, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
    {
        var key = new DLDDD { A = CastHandle(field), B = (uint)bufferSize, C = ignoreMovableObjects ? 1u : 0u, D = (uint)valueOffset };
        DD value = LookupByKeyOrMiss(GetStaticFieldContent, key, ": key {0:X16}", key.A);
        if (buffer != null && value.A != 0)
            CopyContent(GetStaticFieldContent.GetBufferPointer(value.B), buffer, bufferSize);
        return value.A != 0;
    }

    public bool repGetObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset)
    {
        var key = new DLDD { A = CastHandle(obj), B = (uint)bufferSize, C = (uint)valueOffset };
        DD value = LookupByKeyOrMiss(GetObjectContent, key, ": key {0:X16}", key.A);
        if (buffer != null && value.A != 0)
            CopyContent(GetObjectContent.GetBufferPointer(value.B), buffer, bufferSize);
        return value.A != 0;
    }

    private static void CopyContent(byte* srcBuffer, byte* buffer, int bufferSize)
    {
        ErrorHandling.AssertCode(srcBuffer != null, ExceptionCodes.ASSERT, "srcBuffer != nullptr");
        Buffer.MemoryCopy(srcBuffer, buffer, bufferSize, bufferSize);
    }

    public CORINFO_CLASS_STRUCT_* repGetStaticFieldCurrentClass(CORINFO_FIELD_STRUCT_* field, byte* pIsSpeculative)
    {
        var key = new DLD { A = CastHandle(field), B = pIsSpeculative != null ? 1u : 0u };
        Agnostic_GetStaticFieldCurrentClass value = LookupByKeyOrMiss(GetStaticFieldCurrentClass, key, ": key {0:X16}", key.A);
        if (pIsSpeculative != null)
            *pIsSpeculative = value.isSpeculative;
        return (CORINFO_CLASS_STRUCT_*)value.classHandle;
    }

    public uint repGetClassGClayout(CORINFO_CLASS_STRUCT_* cls, byte* gcPtrs)
    {
        Agnostic_GetClassGClayout value = LookupByHandle(GetClassGClayout, cls);
        if (value.gcPtrs_Index != uint.MaxValue)
            Buffer.MemoryCopy(GetClassGClayout.GetBufferPointer(value.gcPtrs_Index), gcPtrs, value.len, value.len);
        return value.valCount;
    }

    public uint repGetClassAlignmentRequirement(CORINFO_CLASS_STRUCT_* cls, bool fDoubleAlignHint)
    {
        var key = new DLD { A = CastHandle(cls), B = fDoubleAlignHint ? 1u : 0u };
        return LookupByKeyOrMiss(GetClassAlignmentRequirement, key, ": key {0:X16}", key.A);
    }

    public CorInfoIsAccessAllowedResult repCanAccessClass(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_HELPER_DESC pAccessHelper)
    {
        AssertMapExists(CanAccessClass, ": key {0:X16}", CastHandle(pResolvedToken.hClass));

        var key = new Agnostic_CanAccessClassIn
        {
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, CanAccessClass),
            callerHandle = CastHandle(callerHandle),
        };
        Agnostic_CanAccessClassOut value = LookupByKeyOrMiss(CanAccessClass, key, ": key {0:X16}", CastHandle(pResolvedToken.hClass));
        pAccessHelper = RestoreCORINFO_HELPER_DESC(value.AccessHelper);
        return (CorInfoIsAccessAllowedResult)value.result;
    }

    public CorInfoHelpFunc repGetCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing)
    {
        var key = new Agnostic_GetCastingHelper { hClass = CastHandle(pResolvedToken.hClass), fThrowing = fThrowing ? 1u : 0u };
        return (CorInfoHelpFunc)LookupByKeyOrMiss(GetCastingHelper, key, ": key {0:X16}", key.hClass);
    }

    public CORINFO_MODULE_STRUCT_* repEmbedModuleHandle(CORINFO_MODULE_STRUCT_* handle, ref void* ppIndirection)
    {
        DLDL value = LookupByHandle(EmbedModuleHandle, handle);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return (CORINFO_MODULE_STRUCT_*)value.B;
    }

    public CORINFO_CLASS_STRUCT_* repEmbedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection)
    {
        ulong key = CastHandle(handle);
        if (key == CONTINUATION_TYPE_PSEUDO_CLASS_HANDLE)
        {
            if (EmbedClassHandle.GetCount() > 0 && EmbedClassHandle.GetItem(0).A != 0)
            {
                // Some other embedded handle required an indirection, so do the same for the pseudo handle
                // (avoids spurious improvements in R2R scenarios).
                ppIndirection = (void*)CONTINUATION_TYPE_PSEUDO_CLASS_HANDLE;
                return null;
            }

            ppIndirection = null;
            return handle;
        }

        DLDL value = LookupByKeyOrMiss(EmbedClassHandle, key, ": key {0:X16}", key);
        ppIndirection = (void*)value.A;
        return (CORINFO_CLASS_STRUCT_*)value.B;
    }

    public bool repPInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig)
    {
        if (PInvokeMarshalingRequired == null) // so when we replay Checked on Release, we throw from lwm
            return true;                       // TODO-Cleanup: hackish...

        var key = new MethodOrSigInfoValue
        {
            method = CastHandle(method),
            pSig_Index = (uint)PInvokeMarshalingRequired.Contains(Bytes(callSiteSig->pSig, callSiteSig->cbSig)),
            cbSig = callSiteSig->cbSig,
            scope = CastHandle(callSiteSig->scope),
        };
        return LookupByKeyOrMiss(PInvokeMarshalingRequired, key) != 0;
    }

    public CorInfoCallConvExtension repGetUnmanagedCallConv(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, ref bool pSuppressGCTransition)
    {
        if (GetUnmanagedCallConv == null)
        {
            Logger.LogDebug("Sparse - repGetUnmanagedCallConv returning CorInfoCallConvExtension::Managed");
            return CorInfoCallConvExtension.Managed;
        }

        var key = new MethodOrSigInfoValue { method = CastHandle(method) };
        if (callSiteSig != null)
        {
            key.pSig_Index = (uint)GetUnmanagedCallConv.Contains(Bytes(callSiteSig->pSig, callSiteSig->cbSig));
            key.cbSig = callSiteSig->cbSig;
            key.scope = CastHandle(callSiteSig->scope);
        }

        DD value = LookupByKeyOrMiss(GetUnmanagedCallConv, key);
        pSuppressGCTransition = value.B != 0;
        return (CorInfoCallConvExtension)value.A;
    }

    public void repFindSig(CORINFO_MODULE_STRUCT_* moduleHandle, uint sigTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        var key = new Agnostic_FindSig { module = CastHandle(moduleHandle), sigTOK = sigTOK, context = CastHandle(context) };
        Agnostic_CORINFO_SIG_INFO value = LookupByKeyOrMiss(FindSig, key);
        *sig = Restore_CORINFO_SIG_INFO(value, FindSig, SigInstHandleMap, cr.MemoryTracker);
    }

    public void repGetEEInfo(ref CORINFO_EE_INFO pEEInfoOut)
    {
        Agnostic_CORINFO_EE_INFO value = LookupByKeyOrMiss(GetEEInfo, 0u);
        pEEInfoOut.inlinedCallFrameInfo.size = value.inlinedCallFrameInfo.size;
        pEEInfoOut.inlinedCallFrameInfo.sizeWithSecretStubArg = value.inlinedCallFrameInfo.sizeWithSecretStubArg;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfFrameLink = value.inlinedCallFrameInfo.offsetOfFrameLink;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfCallSiteSP = value.inlinedCallFrameInfo.offsetOfCallSiteSP;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfCalleeSavedFP = value.inlinedCallFrameInfo.offsetOfCalleeSavedFP;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfCallTarget = value.inlinedCallFrameInfo.offsetOfCallTarget;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfReturnAddress = value.inlinedCallFrameInfo.offsetOfReturnAddress;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfSecretStubArg = value.inlinedCallFrameInfo.offsetOfSecretStubArg;
        pEEInfoOut.inlinedCallFrameInfo.offsetOfSPAfterProlog = value.inlinedCallFrameInfo.offsetOfSPAfterProlog;
        pEEInfoOut.offsetOfThreadFrame = value.offsetOfThreadFrame;
        pEEInfoOut.offsetOfGCState = value.offsetOfGCState;
        pEEInfoOut.offsetOfDelegateInstance = value.offsetOfDelegateInstance;
        pEEInfoOut.offsetOfDelegateFirstTarget = value.offsetOfDelegateFirstTarget;
        pEEInfoOut.sizeOfReversePInvokeFrame = value.sizeOfReversePInvokeFrame;
        pEEInfoOut.osPageSize = value.osPageSize;
        pEEInfoOut.maxUncheckedOffsetForNullObject = value.maxUncheckedOffsetForNullObject;
        pEEInfoOut.targetAbi = (CORINFO_RUNTIME_ABI)value.targetAbi;
        pEEInfoOut.osType = (CORINFO_OS)value.osType;
    }

    public void repGetAsyncInfo(ref CORINFO_ASYNC_INFO pAsyncInfoOut)
    {
        Agnostic_CORINFO_ASYNC_INFO value = LookupByKeyOrMiss(GetAsyncInfo, 0u);
        pAsyncInfoOut.continuationClsHnd = (CORINFO_CLASS_STRUCT_*)value.continuationClsHnd;
        pAsyncInfoOut.continuationNextFldHnd = (CORINFO_FIELD_STRUCT_*)value.continuationNextFldHnd;
        pAsyncInfoOut.continuationResumeInfoFldHnd = (CORINFO_FIELD_STRUCT_*)value.continuationResumeInfoFldHnd;
        pAsyncInfoOut.continuationStateFldHnd = (CORINFO_FIELD_STRUCT_*)value.continuationStateFldHnd;
        pAsyncInfoOut.continuationFlagsFldHnd = (CORINFO_FIELD_STRUCT_*)value.continuationFlagsFldHnd;
        pAsyncInfoOut.captureExecutionContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureExecutionContextMethHnd;
        pAsyncInfoOut.captureContinuationContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureContinuationContextMethHnd;
        pAsyncInfoOut.captureContextsMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureContextsMethHnd;
        pAsyncInfoOut.restoreContextsMethHnd = (CORINFO_METHOD_STRUCT_*)value.restoreContextsMethHnd;
        pAsyncInfoOut.restoreContextsOnSuspensionMethHnd = (CORINFO_METHOD_STRUCT_*)value.restoreContextsOnSuspensionMethHnd;
        pAsyncInfoOut.restoreInlinedFrameContextsMethHnd = (CORINFO_METHOD_STRUCT_*)value.restoreInlinedFrameContextsMethHnd;
        pAsyncInfoOut.captureInlinedFrameTransitionWithContinuationContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureInlinedFrameTransitionWithContinuationContextMethHnd;
        pAsyncInfoOut.captureInlinedFrameTransitionNoContinuationContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureInlinedFrameTransitionNoContinuationContextMethHnd;
        pAsyncInfoOut.captureInlinedFrameTransitionContinueOnThreadPoolMethHnd = (CORINFO_METHOD_STRUCT_*)value.captureInlinedFrameTransitionContinueOnThreadPoolMethHnd;
        pAsyncInfoOut.finishSuspensionNoContinuationContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.finishSuspensionNoContinuationContextMethHnd;
        pAsyncInfoOut.finishSuspensionWithContinuationContextMethHnd = (CORINFO_METHOD_STRUCT_*)value.finishSuspensionWithContinuationContextMethHnd;
    }

    public void repGetWasmWellKnownGlobals(ref CORINFO_WASM_WELLKNOWN_GLOBALS pWellKnownGlobalsOut)
    {
        Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS value = LookupByKeyOrMiss(GetWasmWellKnownGlobals, 0u);
        pWellKnownGlobalsOut.stackPointer = (CORINFO_WASM_GLOBAL_SYMBOL_STRUCT_*)value.stackPointer;
        pWellKnownGlobalsOut.imageBase = (CORINFO_WASM_GLOBAL_SYMBOL_STRUCT_*)value.imageBase;
        pWellKnownGlobalsOut.tableBase = (CORINFO_WASM_GLOBAL_SYMBOL_STRUCT_*)value.tableBase;
        pWellKnownGlobalsOut.asyncContinuation = (CORINFO_WASM_GLOBAL_SYMBOL_STRUCT_*)value.asyncContinuation;
    }

    public CORINFO_METHOD_STRUCT_* repGetAwaitReturnCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        Agnostic_GetAwaitReturnCallResult result = LookupByKeyOrMiss(GetAwaitReturnCall, CastHandle(callerHnd));
        *contextHandle = (CORINFO_CONTEXT_STRUCT*)result.contextHandle;
        instArg = RestoreCORINFO_LOOKUP(result.instArg);
        return (CORINFO_METHOD_STRUCT_*)result.methodHnd;
    }

    public CORINFO_METHOD_STRUCT_* repGetAwaitAwaiterInContinuationCall(CORINFO_METHOD_STRUCT_* callerHnd, ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool isUnsafe,
        CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        var key = new Agnostic_GetAwaitAwaiterInContinuationCall
        {
            callerHnd = CastHandle(callerHnd),
            ResolvedToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetAwaitAwaiterInContinuationCall),
            isUnsafe = isUnsafe ? 1u : 0u,
        };
        Agnostic_GetAwaitReturnCallResult value = LookupByKeyOrMiss(GetAwaitAwaiterInContinuationCall, key);
        *contextHandle = (CORINFO_CONTEXT_STRUCT*)value.contextHandle;
        instArg = RestoreCORINFO_LOOKUP(value.instArg);
        return (CORINFO_METHOD_STRUCT_*)value.methodHnd;
    }

    public void repGetGSCookie(nint* pCookieVal, nint** ppCookieVal)
    {
        if (GetGSCookie == null)
        {
            // Fake the result, since for the codegen it is just a constant.
            if (pCookieVal != null)
                *pCookieVal = 0x06000000;
            if (ppCookieVal != null)
                *ppCookieVal = (nint*)0x06000001;
            return;
        }

        DLDL value = LookupByKeyOrMiss(GetGSCookie, 0u);
        if (pCookieVal != null)
            *pCookieVal = (nint)value.A;
        if (ppCookieVal != null)
            *ppCookieVal = (nint*)value.B;
    }

    public PatchpointInfo* repGetOSRInfo(ref uint ilOffset)
    {
        uint key = 0;
        Agnostic_GetOSRInfo value = LookupByKeyOrMiss(GetOSRInfo, key);
        ilOffset = value.ilOffset;
        return (PatchpointInfo*)GetOSRInfo.GetBufferPointer(value.index);
    }

    public bool repGetIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset)
    {
        Agnostic_GetIsClassInitedFlagAddress value = LookupByHandle(GetIsClassInitedFlagAddress, cls);
        offset = (int)value.offset;
        addr.accessType = (InfoAccessType)value.addr.accessType;
        addr.addr = (void*)value.addr.handle;
        return value.result != 0;
    }

    public bool repGetStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr)
    {
        var key = new DLD { A = CastHandle(cls), B = isGc ? 1u : 0u };
        Agnostic_GetStaticBaseAddress value = LookupByKeyOrMiss(GetStaticBaseAddress, key, ": key {0:X16}", key.A);
        addr.accessType = (InfoAccessType)value.addr.accessType;
        addr.addr = (void*)value.addr.handle;
        return value.result != 0;
    }

    public uint repGetThreadTLSIndex(ref void* ppIndirection)
    {
        DLD value = LookupByKeyOrMiss(GetThreadTLSIndex, 0u);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return value.B;
    }

    public int* repGetAddrOfCaptureThreadGlobal(ref void* ppIndirection)
    {
        if (GetAddrOfCaptureThreadGlobal == null || GetAddrOfCaptureThreadGlobal.GetIndex(0) == -1)
        {
            Logger.LogDebug("Sparse - repGetAddrOfCaptureThreadGlobal returning nullptr and 0xCAFE0001");
            if (!IsNullRef(ref ppIndirection))
                ppIndirection = null;
            return (int*)0xCAFE0001;
        }

        DLDL value = GetAddrOfCaptureThreadGlobal.Get(0);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return (int*)value.B;
    }

    public void* repGetClassStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls) => (void*)LookupByHandle(GetClassStaticDynamicInfo, cls).A;

    public void* repGetClassThreadStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls) => (void*)LookupByHandle(GetClassThreadStaticDynamicInfo, cls).A;

    public void repGetLocationOfThisType(CORINFO_METHOD_STRUCT_* context, ref CORINFO_LOOKUP_KIND pLookupKind)
        => pLookupKind = RestoreCORINFO_LOOKUP_KIND(LookupByHandle(GetLocationOfThisType, context));

    public CORINFO_METHOD_STRUCT_* repGetDelegateCtor(CORINFO_METHOD_STRUCT_* methHnd, CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_METHOD_STRUCT_* targetMethodHnd,
        ref DelegateCtorArgs pCtorData)
    {
        var key = new Agnostic_GetDelegateCtorIn { methHnd = CastHandle(methHnd), clsHnd = CastHandle(clsHnd), targetMethodHnd = CastHandle(targetMethodHnd) };
        Agnostic_GetDelegateCtorOut value = LookupByKeyOrMiss(GetDelegateCtor, key, ": key {0:X16}", key.methHnd);
        pCtorData.pMethod = (void*)value.CtorData.pMethod;
        pCtorData.pArg3 = (void*)value.CtorData.pArg3;
        pCtorData.pArg4 = (void*)value.CtorData.pArg4;
        pCtorData.pArg5 = (void*)value.CtorData.pArg5;
        return (CORINFO_METHOD_STRUCT_*)value.result;
    }

    // The isUnsafeFunctionPointer has no impact on the resulting value; it only produces side effects in the runtime.
    public void repGetFunctionFixedEntryPoint(CORINFO_METHOD_STRUCT_* ftn, bool isUnsafeFunctionPointer, ref CORINFO_CONST_LOOKUP pResult)
        => pResult = RestoreCORINFO_CONST_LOOKUP(LookupByHandle(GetFunctionFixedEntryPoint, ftn));

    public CORINFO_FIELD_STRUCT_* repGetFieldInClass(CORINFO_CLASS_STRUCT_* clsHnd, int num)
    {
        var key = new DLD { A = CastHandle(clsHnd), B = (uint)num };
        return (CORINFO_FIELD_STRUCT_*)LookupByKeyOrMiss(GetFieldInClass, key, ": key {0:X16}", key.A);
    }

    public GetTypeLayoutResult repGetTypeLayout(CORINFO_CLASS_STRUCT_* typeHnd, CORINFO_TYPE_LAYOUT_NODE* nodes, nuint* numNodes)
    {
        ulong key = CastHandle(typeHnd);
        Agnostic_GetTypeLayoutResult value = LookupByKeyOrMiss(GetTypeLayout, key, ": key type-{0:X16}", key);

        var result = (GetTypeLayoutResult)value.result;
        if (result == GetTypeLayoutResult.Failure)
            return result;

        byte* valueFields = GetTypeLayout.GetBufferPointer(value.nodesBuffer);
        nuint nodesToWrite = Math.Min(value.numNodes, *numNodes);
        for (nuint i = 0; i < nodesToWrite; i++)
            nodes[i] = RestoreCORINFO_TYPE_LAYOUT_NODE(Unsafe.ReadUnaligned<Agnostic_CORINFO_TYPE_LAYOUT_NODE>(valueFields + i * (nuint)sizeof(Agnostic_CORINFO_TYPE_LAYOUT_NODE)));

        *numNodes = nodesToWrite;
        return result == GetTypeLayoutResult.Partial || value.numNodes > *numNodes ? GetTypeLayoutResult.Partial : GetTypeLayoutResult.Success;
    }

    public CorInfoType repGetFieldType(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_** structType, CORINFO_CLASS_STRUCT_* fieldOwnerHint)
    {
        var key = new DLDL { A = CastHandle(field), B = CastHandle(fieldOwnerHint) };
        DLD value = LookupByKeyOrMiss(GetFieldType, key, ": key {0:X16}", key.A);
        if (structType != null)
            *structType = (CORINFO_CLASS_STRUCT_*)value.A;
        return (CorInfoType)value.B;
    }

    public bool repSatisfiesMethodConstraints(CORINFO_CLASS_STRUCT_* parent, CORINFO_METHOD_STRUCT_* method)
    {
        var key = new DLDL { A = CastHandle(parent), B = CastHandle(method) };
        return LookupByKeyOrMiss(SatisfiesMethodConstraints, key) != 0;
    }

    public int repGetStringLiteral(CORINFO_MODULE_STRUCT_* module, uint metaTOK, char* buffer, int bufferSize, int startIndex)
    {
        var key = new DLDDD { A = CastHandle(module), B = metaTOK, C = (uint)bufferSize, D = (uint)startIndex };
        DD value = LookupByKeyOrMiss(GetStringLiteral, key, ": key handle-{0:X16} token-{1:X} bufferSize-{2} startIndex-{3}", key.A, key.B, key.C, key.D);

        int srcBufferLength = (int)value.A;
        if (buffer != null && srcBufferLength > 0)
        {
            byte* srcBuffer = GetStringLiteral.GetBufferPointer(value.B);
            ErrorHandling.AssertCode(srcBuffer != null, ExceptionCodes.ASSERT, "srcBuffer != nullptr");
            int bytes = Math.Min(srcBufferLength, bufferSize) * sizeof(char);
            Buffer.MemoryCopy(srcBuffer, buffer, bytes, bytes);
        }
        return srcBufferLength;
    }

    public bool repCanCast(CORINFO_CLASS_STRUCT_* child, CORINFO_CLASS_STRUCT_* parent)
    {
        var key = new DLDL { A = CastHandle(child), B = CastHandle(parent) };
        return LookupByKeyOrMiss(CanCast, key, ": key {0:X16} {1:X16}", key.A, key.B) != 0;
    }

    public CorInfoType repGetChildType(CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_CLASS_STRUCT_** clsRet)
    {
        DLD value = LookupByHandle(GetChildType, clsHnd);
        *clsRet = (CORINFO_CLASS_STRUCT_*)value.A;
        return (CorInfoType)value.B;
    }

    public void* repGetArrayInitializationData(CORINFO_FIELD_STRUCT_* field, uint size)
    {
        var key = new DLD { A = CastHandle(field), B = size };
        return (void*)LookupByKeyOrMiss(GetArrayInitializationData, key);
    }

    public void repGetAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
    {
        DLD value = LookupByHandle(GetAddressOfPInvokeTarget, method);
        pLookup.addr = (void*)value.A;
        pLookup.accessType = (InfoAccessType)value.B;
    }

    public uint repGetMethodHash(CORINFO_METHOD_STRUCT_* ftn)
    {
        ulong key = CastHandle(ftn);
        return GetMethodHash != null && GetMethodHash.GetIndex(key) >= 0 ? GetMethodHash.Get(key) : 0x43;
    }

    public bool repCanTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix)
    {
        var key = new Agnostic_CanTailCall
        {
            callerHnd = CastHandle(callerHnd),
            declaredCalleeHnd = CastHandle(declaredCalleeHnd),
            exactCalleeHnd = CastHandle(exactCalleeHnd),
            fIsTailPrefix = fIsTailPrefix ? (ushort)1 : (ushort)0,
        };
        return LookupByKeyOrMiss(CanTailCall, key, ": key {0:X16}", key.callerHnd) != 0;
    }

    public void repFindCallSiteSig(CORINFO_MODULE_STRUCT_* module, uint methTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        var key = new Agnostic_FindCallSiteSig { module = CastHandle(module), methTok = methTOK, context = CastHandle(context) };
        Agnostic_CORINFO_SIG_INFO value = LookupByKeyOrMiss(FindCallSiteSig, key, ": key {0:X8}", key.methTok);
        *sig = Restore_CORINFO_SIG_INFO(value, FindCallSiteSig, SigInstHandleMap, cr.MemoryTracker);
    }

    public nint repGetVarArgsHandle(CORINFO_SIG_INFO* pSig, CORINFO_METHOD_STRUCT_* methHnd, ref void* ppIndirection)
    {
        var key = new GetVarArgsHandleValue
        {
            cbSig = pSig->cbSig,
            pSig_Index = (uint)GetVarArgsHandle.Contains(Bytes(pSig->pSig, pSig->cbSig)),
            scope = CastHandle(pSig->scope),
            token = (uint)pSig->token,
            methHnd = CastHandle(methHnd),
        };
        DLDL value = LookupByKeyOrMiss(GetVarArgsHandle, key);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return (nint)value.B;
    }

    public uint repGetFieldThreadLocalStoreID(CORINFO_FIELD_STRUCT_* field, ref void* ppIndirection)
    {
        DLD value = LookupByHandle(GetFieldThreadLocalStoreID, field);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return value.B;
    }

    private static uint InstrumentationKindToSize(PgoInstrumentationKind kind) => (kind & PgoInstrumentationKind.MarshalMask) switch
    {
        PgoInstrumentationKind.None => 0,
        PgoInstrumentationKind.FourByte => 4,
        PgoInstrumentationKind.EightByte => 8,
        PgoInstrumentationKind.TypeHandle or PgoInstrumentationKind.MethodHandle => (uint)sizeof(nint),
        _ => 0,
    };

    private static uint InstrumentationKindToAlignment(PgoInstrumentationKind kind) => (kind & PgoInstrumentationKind.AlignMask) switch
    {
        PgoInstrumentationKind.Align4Byte => 4,
        PgoInstrumentationKind.Align8Byte => 8,
        PgoInstrumentationKind.AlignPointer => (uint)sizeof(nint),
        _ => InstrumentationKindToSize(kind),
    };

    public HRESULT repAllocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData)
    {
        // Matching case: return the same layout as the recorded EE.
        if (repAllocPgoInstrumentationBySchemaRecorded(ftnHnd, pSchema, countSchemaItems, pInstrumentationData, out HRESULT result))
            return result;

        // Do our own layout.
        uint maxAlignment = 1;
        uint offset = 0;
        for (uint i = 0; i < countSchemaItems; i++)
        {
            uint size = InstrumentationKindToSize(pSchema[i].InstrumentationKind) * (uint)pSchema[i].Count;
            if (size != 0)
            {
                uint alignment = InstrumentationKindToAlignment(pSchema[i].InstrumentationKind);
                maxAlignment = Math.Max(maxAlignment, alignment);
                offset = (offset + alignment - 1) & ~(alignment - 1);
                pSchema[i].Offset = (nint)offset;
            }
            offset += size;
        }

        // We assume the JIT doesn't write or read this buffer, only generates code that uses it.
        *pInstrumentationData = (byte*)(0x12345678u & ~(maxAlignment - 1));
        return HRESULT.S_OK;
    }

    private bool repAllocPgoInstrumentationBySchemaRecorded(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems,
        byte** pInstrumentationData, out HRESULT result)
    {
        result = default;
        int index = AllocPgoInstrumentationBySchema?.GetIndex(CastHandle(ftnHnd)) ?? -1;
        if (index == -1)
            return false;

        Agnostic_AllocPgoInstrumentationBySchema value = AllocPgoInstrumentationBySchema.GetItem(index);
        if (value.countSchemaItems != countSchemaItems)
            return false;

        byte* pAgnosticSchema = AllocPgoInstrumentationBySchema.GetBufferPointer(value.schema_index);
        for (uint iSchema = 0; iSchema < value.countSchemaItems; iSchema++)
        {
            // Everything but Offset is an IN argument, so verify it against what was stored (since those
            // IN arguments weren't part of the key).
            var agnostic = Unsafe.ReadUnaligned<Agnostic_PgoInstrumentationSchema>(pAgnosticSchema + iSchema * (uint)sizeof(Agnostic_PgoInstrumentationSchema));
            if ((PgoInstrumentationKind)agnostic.InstrumentationKind != pSchema[iSchema].InstrumentationKind ||
                (int)agnostic.ILOffset != pSchema[iSchema].ILOffset ||
                (int)agnostic.Count != pSchema[iSchema].Count ||
                (int)agnostic.Other != pSchema[iSchema].Other)
            {
                return false;
            }

            pSchema[iSchema].Offset = (nint)agnostic.Offset;
        }

        // We assume the JIT doesn't write or read this buffer, only generates code that uses it.
        *pInstrumentationData = (byte*)value.instrumentationDataAddress;
        result = (HRESULT)value.result;
        return true;
    }

    public HRESULT repGetPgoInstrumentationResults(CORINFO_METHOD_STRUCT_* ftnHnd, ref PgoInstrumentationSchema* pSchema, ref uint pCountSchemaItems,
        byte** pInstrumentationData, ref PgoSource pPgoSource, ref bool pDynamicPgo)
    {
        Agnostic_GetPgoInstrumentationResults tempValue = LookupByHandle(GetPgoInstrumentationResults, ftnHnd);

        pCountSchemaItems = tempValue.countSchemaItems;
        *pInstrumentationData = GetPgoInstrumentationResults.GetBufferPointer(tempValue.data_index);
        pPgoSource = (PgoSource)tempValue.pgoSource;
        pDynamicPgo = tempValue.dynamicPgo != 0;

        var pOutSchema = (PgoInstrumentationSchema*)AllocJitTempBuffer(tempValue.countSchemaItems * (nuint)sizeof(PgoInstrumentationSchema));
        byte* pAgnosticSchema = GetPgoInstrumentationResults.GetBufferPointer(tempValue.schema_index);
        for (uint iSchema = 0; iSchema < tempValue.countSchemaItems; iSchema++)
        {
            var agnostic = Unsafe.ReadUnaligned<Agnostic_PgoInstrumentationSchema>(pAgnosticSchema + iSchema * (uint)sizeof(Agnostic_PgoInstrumentationSchema));
            pOutSchema[iSchema] = new PgoInstrumentationSchema
            {
                Offset = (nint)agnostic.Offset,
                InstrumentationKind = (PgoInstrumentationKind)agnostic.InstrumentationKind,
                ILOffset = (int)agnostic.ILOffset,
                Count = (int)agnostic.Count,
                Other = (int)agnostic.Other,
            };
        }

        pSchema = pOutSchema;
        return (HRESULT)tempValue.result;
    }

    public bool repIsMoreSpecificType(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        var key = new DLDL { A = CastHandle(cls1), B = CastHandle(cls2) };
        return LookupByKeyOrMiss(IsMoreSpecificType, key, ": key {0:X16} {1:X16}", key.A, key.B) != 0;
    }

    public bool repIsExactType(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(IsExactType, cls) != 0;

    public TypeCompareState repIsGenericType(CORINFO_CLASS_STRUCT_* cls) => (TypeCompareState)LookupByHandle(IsGenericType, cls);

    public TypeCompareState repIsNullableType(CORINFO_CLASS_STRUCT_* cls) => (TypeCompareState)LookupByHandle(IsNullableType, cls);

    public TypeCompareState repIsEnum(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_** underlyingType)
    {
        DLD value = LookupByHandle(IsEnum, cls);
        if (underlyingType != null)
            *underlyingType = (CORINFO_CLASS_STRUCT_*)value.A;
        return (TypeCompareState)value.B;
    }

    public void* repGetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig)
    {
        AssertMapExists(GetCookieForInterpreterCalliSig);

        var key = new GetCookieForInterpreterCalliSigValue
        {
            cbSig = szMetaSig->cbSig,
            pSig_Index = (uint)GetCookieForInterpreterCalliSig.Contains(Bytes(szMetaSig->pSig, szMetaSig->cbSig)),
            scope = CastHandle(szMetaSig->scope),
            token = (uint)szMetaSig->token,
        };
        return (void*)LookupByKeyOrMiss(GetCookieForInterpreterCalliSig, key).A;
    }

    public void repGetProfilingHandle(ref bool pbHookFunction, ref void* pProfilerHandle, ref bool pbIndirectedHandles)
    {
        Agnostic_GetProfilingHandle value = LookupByKeyOrMiss(GetProfilingHandle, 0u);
        pbHookFunction = value.bHookFunction != 0;
        pProfilerHandle = (void*)value.ProfilerHandle;
        pbIndirectedHandles = value.bIndirectedHandles != 0;
    }

    public CORINFO_FIELD_STRUCT_* repEmbedFieldHandle(CORINFO_FIELD_STRUCT_* handle, ref void* ppIndirection)
    {
        DLDL value = LookupByHandle(EmbedFieldHandle, handle);
        if (!IsNullRef(ref ppIndirection))
            ppIndirection = (void*)value.A;
        return (CORINFO_FIELD_STRUCT_*)value.B;
    }

    public TypeCompareState repCompareTypesForCast(CORINFO_CLASS_STRUCT_* fromClass, CORINFO_CLASS_STRUCT_* toClass)
    {
        var key = new DLDL { A = CastHandle(fromClass), B = CastHandle(toClass) };
        return (TypeCompareState)LookupByKeyOrMiss(CompareTypesForCast, key, ": key {0:X16} {1:X16}", key.A, key.B);
    }

    public TypeCompareState repCompareTypesForEquality(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        var key = new DLDL { A = CastHandle(cls1), B = CastHandle(cls2) };
        return (TypeCompareState)LookupByKeyOrMiss(CompareTypesForEquality, key, ": key {0:X16} {1:X16}", key.A, key.B);
    }

    public bool repGetSystemVAmd64PassStructInRegisterDescriptor(CORINFO_CLASS_STRUCT_* structHnd, SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
    {
        Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor value = LookupByHandle(GetSystemVAmd64PassStructInRegisterDescriptor, structHnd);
        structPassInRegDescPtr->passedInRegisters = value.passedInRegisters != 0;
        structPassInRegDescPtr->eightByteCount = (byte)value.eightByteCount;
        structPassInRegDescPtr->eightByteClassifications0 = (SystemVClassificationType)value.eightByteClassifications[0];
        structPassInRegDescPtr->eightByteClassifications1 = (SystemVClassificationType)value.eightByteClassifications[1];
        structPassInRegDescPtr->eightByteSizes0 = (byte)value.eightByteSizes[0];
        structPassInRegDescPtr->eightByteSizes1 = (byte)value.eightByteSizes[1];
        structPassInRegDescPtr->eightByteOffsets0 = (byte)value.eightByteOffsets[0];
        structPassInRegDescPtr->eightByteOffsets1 = (byte)value.eightByteOffsets[1];
        return value.result != 0;
    }

    public void repGetSwiftLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_SWIFT_LOWERING pLowering)
    {
        Agnostic_GetSwiftLowering value = LookupByHandle(GetSwiftLowering, structHnd);
        pLowering.byReference = value.byReference != 0;
        pLowering.numLoweredElements = (nint)value.numLoweredElements;
        for (int i = 0; i < pLowering.numLoweredElements; i++)
        {
            pLowering.LoweredElements[i] = (CorInfoType)value.loweredElements[i];
            pLowering.Offsets[i] = value.offsets[i];
        }
    }

    public void repGetFpStructLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_FPSTRUCT_LOWERING pLowering)
    {
        Agnostic_GetFpStructLowering value = LookupByHandle(GetFpStructLowering, structHnd);
        pLowering.byIntegerCallConv = value.byIntegerCallConv != 0;
        pLowering.numLoweredElements = (nint)value.numLoweredElements;
        for (int i = 0; i < pLowering.numLoweredElements; i++)
        {
            pLowering.LoweredElements[i] = (CorInfoType)value.loweredElements[i];
            pLowering.Offsets[i] = value.offsets[i];
        }
    }

    public CorInfoWasmType repGetWasmLowering(CORINFO_CLASS_STRUCT_* structHnd) => (CorInfoWasmType)LookupByHandle(GetWasmLowering, structHnd);

    public CorInfoReloc repGetRelocTypeHint(void* target)
    {
        ulong key = CastHandle(target);
        int index = GetRelocTypeHint?.GetIndex(key) ?? -1;
        if (index == -1)
        {
            Logger.LogDebug("Sparse - repGetRelocTypeHint yielding fake answer...");
            return CorInfoReloc.NONE;
        }
        return (CorInfoReloc)GetRelocTypeHint.GetItem(index);
    }

    public uint repGetAddressAlignment(void* address)
    {
        ulong key = CastHandle(address);
        int index = GetAddressAlignment?.GetIndex(key) ?? -1;
        if (index == -1)
        {
            Logger.LogDebug("Sparse - repGetAddressAlignment yielding fake answer...");
            return 1;
        }
        return GetAddressAlignment.GetItem(index);
    }

    public uint repGetExpectedTargetArchitecture()
    {
        uint key = 0;
        return LookupByKeyOrMiss(GetExpectedTargetArchitecture, key, ": key {0:X8}", key);
    }

    public byte* repGetClassNameFromMetadata(CORINFO_CLASS_STRUCT_* cls, byte** namespaceName)
    {
        var key = new DLD { A = CastHandle(cls), B = namespaceName != null ? 1u : 0u };
        DD value = LookupByKeyOrMiss(GetClassNameFromMetadata, key, " : key cls-{0:X16} hasNs-{1}", key.A, key.B);
        if (namespaceName != null)
            *namespaceName = GetClassNameFromMetadata.GetBufferPointer(value.B);
        return GetClassNameFromMetadata.GetBufferPointer(value.A);
    }

    public CORINFO_CLASS_STRUCT_* repGetTypeInstantiationArgument(CORINFO_CLASS_STRUCT_* cls, uint index)
    {
        var key = new DLD { A = CastHandle(cls), B = index };
        return (CORINFO_CLASS_STRUCT_*)LookupByKeyOrMiss(GetTypeInstantiationArgument, key);
    }

    public CORINFO_CLASS_STRUCT_* repGetMethodInstantiationArgument(CORINFO_METHOD_STRUCT_* ftn, uint index)
    {
        var key = new DLD { A = CastHandle(ftn), B = index };
        return (CORINFO_CLASS_STRUCT_*)LookupByKeyOrMiss(GetMethodInstantiationArgument, key);
    }

    // Copies the recorded name into 'buffer' (null-terminated, truncated to fit), and returns the number of
    // bytes written. Fails if the recorded data can't answer the query losslessly.
    public nuint repPrint(string name, LightWeightMap<ulong, Agnostic_PrintResult> map, ulong handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        int index = map?.GetIndex(handle) ?? -1;
        if (index == -1)
            LookupByKeyOrMiss(map, handle, ": map " + name + " key {0:x16}", handle, mapName: "map", keyName: "handle");
        Agnostic_PrintResult res = map.GetItem(index);

        if (pRequiredBufferSize != null)
        {
            if (res.requiredBufferSize == uint.MaxValue)
                ErrorHandling.LogException(ExceptionCodes.MC, $"SuperPMI assertion failed (missing requiredBufferSize for {name} key {handle:x16})");
            *pRequiredBufferSize = res.requiredBufferSize;
        }

        // requiredBufferSize includes the null terminator, but the buffer is stored without one. Determine
        // whether there's enough data to answer the query losslessly. Note that the recorder always sets
        // requiredBufferSize if it had enough information to determine it.
        bool haveFullBuffer = res.requiredBufferSize != uint.MaxValue && unchecked(res.stringBufferSize + 1) >= res.requiredBufferSize;
        if (!haveFullBuffer && bufferSize > (nuint)res.stringBufferSize + 1)
            ErrorHandling.LogException(ExceptionCodes.MC, $"SuperPMI assertion failed (not enough buffer data for {name} key {handle:x16})");

        nuint bytesWritten = 0;
        if (buffer != null && bufferSize > 0)
        {
            bytesWritten = Math.Min(bufferSize - 1, res.stringBufferSize);
            if (bytesWritten > 0)
            {
                // The "full buffer" check above ensures this, given that res.stringBuffer == UINT_MAX
                // implies res.stringBufferSize == 0.
                ErrorHandling.AssertCode(res.stringBuffer != uint.MaxValue, ExceptionCodes.ASSERT, "res.stringBuffer != UINT_MAX");
                Buffer.MemoryCopy(map.GetBufferPointer(res.stringBuffer), buffer, bytesWritten, bytesWritten);
            }
            buffer[bytesWritten] = 0;
        }

        return bytesWritten;
    }

    public nuint repPrintObjectDescription(CORINFO_OBJECT_STRUCT_* handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
        => repPrint("PrintObjectDescription", PrintObjectDescription, CastHandle(handle), buffer, bufferSize, pRequiredBufferSize);

    public nuint repPrintClassName(CORINFO_CLASS_STRUCT_* cls, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
        => repPrint("PrintClassName", PrintClassName, CastHandle(cls), buffer, bufferSize, pRequiredBufferSize);

    public nuint repPrintFieldName(CORINFO_FIELD_STRUCT_* fld, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
        => repPrint("PrintFieldName", PrintFieldName, CastHandle(fld), buffer, bufferSize, pRequiredBufferSize);

    public nuint repPrintMethodName(CORINFO_METHOD_STRUCT_* meth, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
        => repPrint("PrintMethodName", PrintMethodName, CastHandle(meth), buffer, bufferSize, pRequiredBufferSize);

    public bool repGetTailCallHelpers(ref CORINFO_RESOLVED_TOKEN callToken, CORINFO_SIG_INFO* sig, CORINFO_GET_TAILCALL_HELPERS_FLAGS flags, ref CORINFO_TAILCALL_HELPERS pResult)
    {
        AssertMapExists(GetTailCallHelpers);

        var key = new Agnostic_GetTailCallHelpers
        {
            callToken = RestoreAgnostic_CORINFO_RESOLVED_TOKEN(ref callToken, GetTailCallHelpers),
            sig = RestoreAgnostic_CORINFO_SIG_INFO(*sig, GetTailCallHelpers, SigInstHandleMap),
            flags = (uint)flags,
        };
        Agnostic_CORINFO_TAILCALL_HELPERS value = LookupByKeyOrMiss(GetTailCallHelpers, key);
        if (value.result == 0)
            return false;

        pResult.flags = (CORINFO_TAILCALL_HELPERS_FLAGS)value.flags;
        pResult.hStoreArgs = (CORINFO_METHOD_STRUCT_*)value.hStoreArgs;
        pResult.hCallTarget = (CORINFO_METHOD_STRUCT_*)value.hCallTarget;
        pResult.hDispatcher = (CORINFO_METHOD_STRUCT_*)value.hDispatcher;
        return true;
    }

    public CORINFO_METHOD_STRUCT_* repGetAsyncResumptionStub(ref void* entryPoint)
    {
        DLDL value = LookupByKeyOrMiss(GetAsyncResumptionStub, 0u);
        entryPoint = (void*)value.B;
        return (CORINFO_METHOD_STRUCT_*)value.A;
    }

    public CORINFO_CLASS_STRUCT_* repGetContinuationType(nuint dataSize, ref bool objRefs, nuint objRefsSize)
    {
        AssertMapExists(GetContinuationType);

        var key = new Agnostic_GetContinuationTypeIn
        {
            dataSize = dataSize,
            objRefs = (uint)GetContinuationType.Contains(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref objRefs, (int)objRefsSize))),
            objRefsSize = (uint)objRefsSize,
        };

        // This handle is more or less opaque, so tolerate a miss and fake up a handle. Otherwise SPMI
        // would be useless for any async change that results in changes in live state.
        int index = GetContinuationType.GetIndex(key);
        return (CORINFO_CLASS_STRUCT_*)(index == -1 ? CONTINUATION_TYPE_PSEUDO_CLASS_HANDLE : GetContinuationType.GetItem(index));
    }

    public void repUpdateEntryPointForTailCall(ref CORINFO_CONST_LOOKUP entryPoint)
    {
        AssertMapExists(UpdateEntryPointForTailCall);

        Agnostic_CORINFO_CONST_LOOKUP key = StoreAgnostic_CORINFO_CONST_LOOKUP(ref entryPoint);
        entryPoint = RestoreCORINFO_CONST_LOOKUP(LookupByKeyOrMiss(UpdateEntryPointForTailCall, key));
    }

    public mdToken repGetMethodDefFromMethod(CORINFO_METHOD_STRUCT_* hMethod)
    {
        // Since this is diagnostic, fake up a result if one wasn't recorded.
        if (GetMethodDefFromMethod == null)
            return (mdToken)0x06000000;

        int index = GetMethodDefFromMethod.GetIndex(CastHandle(hMethod));
        return index < 0 ? (mdToken)0x06000001 : (mdToken)GetMethodDefFromMethod.GetItem(index);
    }

    public bool repCheckMethodModifier(CORINFO_METHOD_STRUCT_* hMethod, byte* modifier, bool fOptional)
    {
        var key = new Agnostic_CheckMethodModifier
        {
            hMethod = CastHandle(hMethod),
            modifier = modifier != null ? (uint)CheckMethodModifier.Contains(CString(modifier)) : uint.MaxValue,
            fOptional = fOptional ? 1u : 0u,
        };
        return LookupByKeyOrMiss(CheckMethodModifier, key) != 0;
    }

    public uint repGetArrayRank(CORINFO_CLASS_STRUCT_* cls) => LookupByHandle(GetArrayRank, cls);

    public CorInfoArrayIntrinsic repGetArrayIntrinsicID(CORINFO_METHOD_STRUCT_* hMethod) => (CorInfoArrayIntrinsic)LookupByHandle(GetArrayIntrinsicID, hMethod);

    public bool repIsFieldStatic(CORINFO_FIELD_STRUCT_* fhld) => LookupByHandle(IsFieldStatic, fhld) != 0;

    public int repGetArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHandle) => (int)LookupByHandle(GetArrayOrStringLength, objHandle);

    public int repGetIntConfigValue(byte* name, int defaultValue)
    {
        if (ignoreStoredConfig || GetIntConfigValue == null)
            return defaultValue;

        ErrorHandling.AssertCodeMsg(name != null, ExceptionCodes.MC, "name != nullptr", "Name can not be nullptr");

        int nameIndex = GetIntConfigValue.Contains(CString(name));
        if (nameIndex == -1) // config name not in map
            return defaultValue;

        int index = GetIntConfigValue.GetIndex(new Agnostic_ConfigIntInfo { nameIndex = (uint)nameIndex, defaultValue = (uint)defaultValue });
        if (index == -1) // the default value has changed
            return defaultValue;

        return (int)GetIntConfigValue.GetItem(index);
    }

    public byte* repGetStringConfigValue(byte* name)
    {
        if (ignoreStoredConfig || GetStringConfigValue == null)
            return null;

        ErrorHandling.AssertCodeMsg(name != null, ExceptionCodes.MC, "name != nullptr", "Name can not be nullptr");

        int nameIndex = GetStringConfigValue.Contains(CString(name));
        if (nameIndex == -1) // config name not in map
            return null;

        uint resultIndex = LookupByKeyOrMiss(GetStringConfigValue, (uint)nameIndex, keyName: "nameIndex");
        return GetStringConfigValue.GetBufferPointer(resultIndex);
    }

    public CORINFO_METHOD_STRUCT_* repGetSpecialCopyHelper(CORINFO_CLASS_STRUCT_* type) => (CORINFO_METHOD_STRUCT_*)LookupByHandle(GetSpecialCopyHelper, type);

    public CORINFO_WASM_TYPE_SYMBOL_STRUCT_* repGetWasmTypeSymbol(CorInfoWasmType* types, nuint typesSize)
    {
        // Fake up a result so we can cross-replay onto wasm.
        if (GetWasmTypeSymbol == null)
            return (CORINFO_WASM_TYPE_SYMBOL_STRUCT_*)0xbadcab;

        var key = new Agnostic_GetWasmTypeSymbol
        {
            types = (uint)GetWasmTypeSymbol.Contains(Bytes(types, (nuint)sizeof(CorInfoWasmType) * typesSize)),
            typesSize = (uint)typesSize,
        };
        return (CORINFO_WASM_TYPE_SYMBOL_STRUCT_*)LookupByKeyOrMiss(GetWasmTypeSymbol, key, ": types {0}, typesSize {1}", key.types, key.typesSize);
    }

    public bool repCanValueClassInstancePointerEscape(CORINFO_METHOD_STRUCT_* ftn) => LookupByHandle(CanValueClassInstancePointerEscape, ftn) != 0;

    public sealed class ConfigEnvironment
    {
        public LightWeightMap<Agnostic_ConfigIntInfo, uint> getIntConfigValue;
        public LightWeightMap<uint, uint> getStringConfigValue;
    }

    public ConfigEnvironment cloneEnvironment() => new()
    {
        getIntConfigValue = GetIntConfigValue?.Clone(),
        getStringConfigValue = GetStringConfigValue?.Clone(),
    };

    public bool WasEnvironmentChanged(ConfigEnvironment prevEnv)
        => !AreLWMHeadersEqual(prevEnv.getIntConfigValue, GetIntConfigValue) || !AreLWMHeadersEqual(prevEnv.getStringConfigValue, GetStringConfigValue) ||
           !IsIntConfigContentEqual(prevEnv.getIntConfigValue, GetIntConfigValue) || !IsStringContentEqual(prevEnv.getStringConfigValue, GetStringConfigValue);

    private static bool AreLWMHeadersEqual(LightWeightMapBuffer prev, LightWeightMapBuffer curr)
        => (prev == null && curr == null) || (prev != null && curr != null && prev.GetCount() == curr.GetCount());

    private static bool IsIntConfigContentEqual(LightWeightMap<Agnostic_ConfigIntInfo, uint> prev, LightWeightMap<Agnostic_ConfigIntInfo, uint> curr)
    {
        if (prev == null || curr == null)
            return prev == curr;

        if (prev.GetCount() != curr.GetCount())
            return false;

        for (int i = 0; i < prev.GetCount(); i++)
        {
            Agnostic_ConfigIntInfo currKey = curr.GetKey(i);
            Agnostic_ConfigIntInfo prevKey = prev.GetKey(i);

            // The previous name is deliberately read at the current key's index: this decides when the JIT is reset.
            if (curr.GetItem(i) != prev.GetItem(i) || currKey.defaultValue != prevKey.defaultValue ||
                curr.GetString(currKey.nameIndex) != prev.GetString(currKey.nameIndex))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsStringContentEqual(LightWeightMap<uint, uint> prev, LightWeightMap<uint, uint> curr)
    {
        if (prev == null || curr == null)
            return prev == curr;

        if (prev.GetCount() != curr.GetCount())
            return false;

        for (int i = 0; i < curr.GetCount(); i++)
        {
            if (curr.GetString(curr.GetKey(i)) != prev.GetString(prev.GetKey(i)) ||
                curr.GetString(curr.GetItem(i)) != prev.GetString(prev.GetItem(i)))
            {
                return false;
            }
        }
        return true;
    }

    public bool hasPgoData(out bool hasEdgeProfile, out bool hasClassProfile, out bool hasMethodProfile, out bool hasLikelyClass,
        out bool hasLikelyMethod, out PgoSource pgoSource)
    {
        hasEdgeProfile = false;
        hasClassProfile = false;
        hasMethodProfile = false;
        hasLikelyClass = false;
        hasLikelyMethod = false;
        pgoSource = PgoSource.Unknown;

        CORINFO_METHOD_INFO info;
        uint flags;
        CORINFO_OS os;
        repCompileMethod(&info, &flags, &os);

        if (GetPgoInstrumentationResults == null || GetPgoInstrumentationResults.GetIndex(CastHandle(info.ftn)) == -1)
            return false;

        PgoInstrumentationSchema* schema = null;
        uint schemaCount = 0;
        byte* schemaData;
        PgoSource source = default;
        bool dynamicPgo = false;
        HRESULT pgoHR = repGetPgoInstrumentationResults(info.ftn, ref schema, ref schemaCount, &schemaData, ref source, ref dynamicPgo);
        pgoSource = source;
        if (pgoHR < 0)
            return false;

        for (uint i = 0; i < schemaCount; i++)
        {
            PgoInstrumentationKind kind = schema[i].InstrumentationKind;
            hasEdgeProfile |= kind is PgoInstrumentationKind.EdgeIntCount or PgoInstrumentationKind.EdgeLongCount;
            hasClassProfile |= kind == PgoInstrumentationKind.HandleHistogramTypes;
            hasMethodProfile |= kind == PgoInstrumentationKind.HandleHistogramMethods;
            hasLikelyClass |= kind == PgoInstrumentationKind.GetLikelyClass;
            hasLikelyMethod |= kind == PgoInstrumentationKind.GetLikelyMethod;

            // Deliberately doesn't wait for hasMethodProfile.
            if (hasEdgeProfile && hasClassProfile && hasLikelyClass && hasLikelyMethod)
                break;
        }

        return true;
    }
}
