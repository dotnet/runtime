// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Pgo;
using SuperPmi;
using SuperPmi.Shim;

namespace Internal.JitInterface;

// The recorder of the collector: records the JIT's calls of the EE, with what the EE answered, into the method context.
// The native RecordingJitInfo makes the calls, and passes their arguments and results (or the codes of the exceptions
// that the EE threw) to the methods here, which don't call the EE.
internal sealed unsafe partial class CorInfoImpl
{
    private readonly CollectorJitCompiler m_compiler;
    // The EE's ICorJitInfo, for the few queries that the recording needs besides the JIT's calls.
    private readonly IntPtr _comp;
    private readonly MethodContext mc;
    private bool m_savedCollectionEarly;

    public CorInfoImpl(CollectorJitCompiler compiler, IntPtr comp, MethodContext mc)
    {
        m_compiler = compiler;
        _comp = comp;
        this.mc = mc;
    }

    private CompileResult cr => mc.cr;

    public bool SavedCollectionEarly => m_savedCollectionEarly;

    public MethodContext MethodContext => mc;

    // The handle that RecordingJitInfo calls back with, for the duration of the compilation.
    private static CorInfoImpl GetThis(IntPtr thisHandle) => (CorInfoImpl)GCHandle.FromIntPtr(thisHandle).Target;

    public static IntPtr RecorderCallbacks => GetRecorderCallbacks();

    private IntPtr AllocException(Exception ex) => ShimInterop.ToNativeException(ex);

    private void AddCall(string name) => cr.AddCall(name);

    private void getMethodInfo(CORINFO_METHOD_STRUCT_* ftn, CORINFO_METHOD_INFO* info, CORINFO_CONTEXT_STRUCT* context, bool temp, uint exceptionCode)
    {
        if (exceptionCode != 0)
            temp = false;
        mc.recGetMethodInfo(ftn, info, context, temp, exceptionCode);
    }

    private void canInline(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, CorInfoInline temp, uint exceptionCode)
    {
        if (exceptionCode != 0)
            temp = CorInfoInline.INLINE_NEVER;
        mc.recCanInline(callerHnd, calleeHnd, temp, exceptionCode);
    }

    private void resolveToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken, uint exceptionCode)
    {
        mc.recResolveToken(ref pResolvedToken, exceptionCode);
    }

    private void canCast(CORINFO_CLASS_STRUCT_* child, CORINFO_CLASS_STRUCT_* parent, bool temp)
    {
        mc.recCanCast(child, parent, temp);
    }

    private void getClassGClayout(CORINFO_CLASS_STRUCT_* cls, byte* gcPtrs, uint temp)
    {
        // The length of the GC layout buffer, in the target's pointer size (not the host's), so that cross-target
        // collections (e.g., wasm32 on an x64 host) record all of it.
        uint len = ShimInterop.Shim_GetClassGCLayoutLength(_comp, cls);
        mc.recGetClassGClayout(cls, gcPtrs, len, temp);
    }

    // RecordingJitInfo passes the out parameters of getNewHelper, isEnum and getHelperFtn even where the JIT doesn't.
    private void getNewHelper(CORINFO_CLASS_STRUCT_* classHandle, ref bool pHasSideEffects, CorInfoHelpFunc result, uint exceptionCode)
    {
        if (exceptionCode != 0)
            result = CorInfoHelpFunc.CORINFO_HELP_UNDEF;
        mc.recGetNewHelper(classHandle, pHasSideEffects, result, exceptionCode);
    }

    private void isEnum(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_** underlyingType, TypeCompareState temp)
    {
        mc.recIsEnum(cls, *underlyingType, temp);
    }

    // RecordingJitInfo also records getArgClass for it, for a "fat" method context.
    private void getArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_** vcTypeRet, CorInfoTypeWithMod temp, uint exceptionCode)
    {
        if (exceptionCode != 0)
            temp = (CorInfoTypeWithMod)CorInfoType.CORINFO_TYPE_UNDEF;
        mc.recGetArgType(sig, args, vcTypeRet, temp, exceptionCode);
    }

    private void getExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet, int result)
    {
        mc.recGetExactClasses(baseType, maxExactClasses, exactClsRet, result);
    }

    private void getArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_* temp, uint exceptionCode)
    {
        if (exceptionCode != 0)
            temp = null;
        mc.recGetArgClass(sig, args, temp, exceptionCode);
    }

    private void getHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP* pNativeEntrypoint, CORINFO_METHOD_STRUCT_** pMethod)
    {
        mc.recGetHelperFtn(ftnNum, *pNativeEntrypoint, *pMethod);
    }

    private void getCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle,
        CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult, uint exceptionCode)
    {
        mc.recGetCallInfo(ref pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult, exceptionCode);
    }

    private void getTailCallHelpers(ref CORINFO_RESOLVED_TOKEN callToken, CORINFO_SIG_INFO* sig, CORINFO_GET_TAILCALL_HELPERS_FLAGS flags, ref CORINFO_TAILCALL_HELPERS pResult, bool result)
    {
        mc.recGetTailCallHelpers(ref callToken, sig, flags, result, ref pResult);
    }

    // The callback that RecordingJitInfo records updateEntryPointForTailCall with, which also gets the entry point
    // from before the call.
    [UnmanagedCallersOnly]
    public static void RecordUpdateEntryPointForTailCallCallback(IntPtr thisHandle, IntPtr* ppException, CORINFO_CONST_LOOKUP* origEntryPoint,
        CORINFO_CONST_LOOKUP* entryPoint, uint exceptionCode)
    {
        CorInfoImpl _this = GetThis(thisHandle);
        try
        {
            _this.AddCall("updateEntryPointForTailCall");
            if (exceptionCode == 0)
                _this.recordUpdateEntryPointForTailCall(*origEntryPoint, *entryPoint);
        }
        catch (Exception ex)
        {
            *ppException = _this.AllocException(ex);
        }
    }

    private void recordUpdateEntryPointForTailCall(CORINFO_CONST_LOOKUP origEntryPoint, CORINFO_CONST_LOOKUP entryPoint)
    {
        mc.recUpdateEntryPointForTailCall(origEntryPoint, entryPoint);
    }

    // Allocates memory that is handed to the EE; not recorded.
    private void allocateArray(nuint cBytes, void* result)
    {
    }

    private void freeArray(void* array)
    {
    }

    private void recordWasmManagedCallSig(CORINFO_SIG_INFO* callSig)
    {
        // No-op in the VM, nothing to record for replay.
    }

    private void recordWasmVirtualFunctionPointerLookup(CORINFO_METHOD_STRUCT_* method)
    {
        // No-op in the VM, nothing to record for replay.
    }

    // The results of the error traps aren't recorded: when the call is replayed, its result depends on whether
    // 'function' calls something that throws at replay time rather than at recording time.
    private void runWithErrorTrap(void* function, void* parameter, bool result)
    {
    }

    // Recorded before the EE gets the call, which may not return.
    private void doAssert(byte* szFile, int iLine, byte* szExpr)
    {
        m_compiler.finalizeAndCommitCollection(mc, CorJitResult.CORJIT_INTERNALERROR, null, 0);
        // The following assert may not always fail fast, so make sure we do not
        // save the collection twice if it throws an unwindable exception.
        m_savedCollectionEarly = true;
    }

    private void getPgoInstrumentationResults(CORINFO_METHOD_STRUCT_* ftnHnd, ref PgoInstrumentationSchema* pSchema, ref uint pCountSchemaItems,
        byte** pInstrumentationData, ref PgoSource pPgoSource, ref bool pDynamicPgo, HRESULT temp)
    {
        mc.recGetPgoInstrumentationResults(ftnHnd, ref pSchema, ref pCountSchemaItems, pInstrumentationData, ref pPgoSource, ref pDynamicPgo, (int)temp);
    }

    private void isIntrinsic(CORINFO_METHOD_STRUCT_* ftn, bool temp)
    {
        mc.recIsIntrinsic(ftn, temp);
    }

    private void canValueClassInstancePointerEscape(CORINFO_METHOD_STRUCT_* ftn, bool temp)
    {
        mc.recCanValueClassInstancePointerEscape(ftn, temp);
    }

    private void notifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn, bool temp)
    {
        mc.recNotifyMethodInfoUsage(ftn, temp);
    }

    private void getMethodAttribs(CORINFO_METHOD_STRUCT_* ftn, uint temp)
    {
        mc.recGetMethodAttribs(ftn, temp);
    }

    private void setMethodAttribs(CORINFO_METHOD_STRUCT_* ftn, CorInfoMethodRuntimeFlags attribs)
    {
        cr.recSetMethodAttribs(ftn, attribs);
    }

    private void getMethodSig(CORINFO_METHOD_STRUCT_* ftn, CORINFO_SIG_INFO* sig, CORINFO_CLASS_STRUCT_* memberParent)
    {
        mc.recGetMethodSig(ftn, sig, memberParent);
    }

    private void haveSameMethodDefinition(CORINFO_METHOD_STRUCT_* methHnd1, CORINFO_METHOD_STRUCT_* methHnd2, bool result)
    {
        mc.recHaveSameMethodDefinition(methHnd1, methHnd2, result);
    }

    private void getTypeDefinition(CORINFO_CLASS_STRUCT_* type, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetTypeDefinition(type, result);
    }

    private void beginInlining(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd)
    {
    }

    private void reportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
    {
        cr.recReportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
    }

    private void canTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix, bool temp)
    {
        mc.recCanTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix, temp);
    }

    private void reportTailCallDecision(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, bool fIsTailPrefix, CorInfoTailCall tailCallResult, byte* reason)
    {
        cr.recReportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
    }

    private void getEHinfo(CORINFO_METHOD_STRUCT_* ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        mc.recGetEHinfo(ftn, EHnumber, ref clause);
    }

    private void getMethodClass(CORINFO_METHOD_STRUCT_* method, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetMethodClass(method, temp);
    }

    private void getMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
    {
        mc.recGetMethodVTableOffset(method, ref offsetOfIndirection, ref offsetAfterIndirection, ref isRelative);
    }

    private void resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO* info, bool result)
    {
        mc.recResolveVirtualMethod(info, result);
    }

    private void getAsyncOtherVariant(CORINFO_METHOD_STRUCT_* ftn, ref bool variantIsThunk, CORINFO_METHOD_STRUCT_* result)
    {
        mc.recGetAsyncOtherVariant(ftn, variantIsThunk, result);
    }

    private void getDefaultComparerClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetDefaultComparerClass(cls, result);
    }

    private void getDefaultEqualityComparerClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetDefaultEqualityComparerClass(cls, result);
    }

    private void getSZArrayHelperEnumeratorClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetSZArrayHelperEnumeratorClass(cls, result);
    }

    private void expandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        mc.recExpandRawHandleIntrinsic(ref pResolvedToken, callerHandle, ref pResult);
    }

    private void isIntrinsicType(CORINFO_CLASS_STRUCT_* classHnd, bool temp)
    {
        mc.recIsIntrinsicType(classHnd, temp);
    }

    private void getUnmanagedCallConv(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, ref bool pSuppressGCTransition, CorInfoCallConvExtension temp)
    {
        mc.recGetUnmanagedCallConv(method, callSiteSig, temp, pSuppressGCTransition);
    }

    private void pInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, bool temp)
    {
        mc.recPInvokeMarshalingRequired(method, callSiteSig, temp);
    }

    private void satisfiesMethodConstraints(CORINFO_CLASS_STRUCT_* parent, CORINFO_METHOD_STRUCT_* method, bool temp)
    {
        mc.recSatisfiesMethodConstraints(parent, method, temp);
    }

    private void getGSCookie(IntPtr* pCookieVal, IntPtr** ppCookieVal)
    {
        mc.recGetGSCookie(pCookieVal, ppCookieVal);
    }

    private void setPatchpointInfo(PatchpointInfo* patchpointInfo)
    {
        cr.recSetPatchpointInfo(patchpointInfo);
    }

    private void getOSRInfo(ref uint ilOffset, PatchpointInfo* patchpointInfo)
    {
        mc.recGetOSRInfo(patchpointInfo, ref ilOffset);
    }

    private void findSig(CORINFO_MODULE_STRUCT_* module, uint sigTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        mc.recFindSig(module, sigTOK, context, sig);
    }

    private void findCallSiteSig(CORINFO_MODULE_STRUCT_* module, uint methTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        mc.recFindCallSiteSig(module, methTOK, context, sig);
    }

    private void getTokenTypeAsHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetTokenTypeAsHandle(ref pResolvedToken, temp);
    }

    private void getStringLiteral(CORINFO_MODULE_STRUCT_* module, uint metaTOK, char* buffer, int bufferSize, int startIndex, int temp)
    {
        mc.recGetStringLiteral(module, metaTOK, buffer, bufferSize, startIndex, temp);
    }

    private void printObjectDescription(CORINFO_OBJECT_STRUCT_* handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint temp)
    {
        mc.recPrintObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize, temp);
    }

    private void asCorInfoType(CORINFO_CLASS_STRUCT_* cls, CorInfoType temp)
    {
        mc.recAsCorInfoType(cls, temp);
    }

    private void getClassNameFromMetadata(CORINFO_CLASS_STRUCT_* cls, byte** namespaceName, byte* temp)
    {
        mc.recGetClassNameFromMetadata(cls, temp, namespaceName);
    }

    private void getTypeInstantiationArgument(CORINFO_CLASS_STRUCT_* cls, uint index, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetTypeInstantiationArgument(cls, index, result);
    }

    private void getMethodInstantiationArgument(CORINFO_METHOD_STRUCT_* ftn, uint index, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetMethodInstantiationArgument(ftn, index, result);
    }

    private void printClassName(CORINFO_CLASS_STRUCT_* cls, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        mc.recPrintClassName(cls, buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    }

    private void isValueClass(CORINFO_CLASS_STRUCT_* cls, bool temp)
    {
        mc.recIsValueClass(cls, temp);
    }

    private void isByRefLike(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        mc.recIsByRefLike(cls, result);
    }

    private void isSharedInst(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        mc.recIsSharedInst(cls, result);
    }

    private void getClassAttribs(CORINFO_CLASS_STRUCT_* cls, uint temp)
    {
        mc.recGetClassAttribs(cls, temp);
    }

    private void getClassAssemblyName(CORINFO_CLASS_STRUCT_* cls, byte* temp)
    {
        mc.recGetClassAssemblyName(cls, temp);
    }

    private void LongLifetimeMalloc(nuint sz, void* result)
    {
    }

    private void LongLifetimeFree(void* obj)
    {
    }

    private void getIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset, bool temp)
    {
        mc.recGetIsClassInitedFlagAddress(cls, ref addr, ref offset, temp);
    }

    private void getClassThreadStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls, void* temp)
    {
        mc.recGetClassThreadStaticDynamicInfo(cls, temp);
    }

    private void getClassStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls, void* temp)
    {
        mc.recGetClassStaticDynamicInfo(cls, temp);
    }

    private void getStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr, bool temp)
    {
        mc.recGetStaticBaseAddress(cls, isGc, ref addr, temp);
    }

    private void getClassSize(CORINFO_CLASS_STRUCT_* cls, uint temp)
    {
        mc.recGetClassSize(cls, temp);
    }

    private void getHeapClassSize(CORINFO_CLASS_STRUCT_* cls, uint temp)
    {
        mc.recGetHeapClassSize(cls, temp);
    }

    private void canAllocateOnStack(CORINFO_CLASS_STRUCT_* cls, bool temp)
    {
        mc.recCanAllocateOnStack(cls, temp);
    }

    private void getClassAlignmentRequirement(CORINFO_CLASS_STRUCT_* cls, bool fDoubleAlignHint, uint temp)
    {
        mc.recGetClassAlignmentRequirement(cls, fDoubleAlignHint, temp);
    }

    private void getClassNumInstanceFields(CORINFO_CLASS_STRUCT_* cls, uint temp)
    {
        mc.recGetClassNumInstanceFields(cls, temp);
    }

    private void getFieldInClass(CORINFO_CLASS_STRUCT_* clsHnd, int num, CORINFO_FIELD_STRUCT_* temp)
    {
        mc.recGetFieldInClass(clsHnd, num, temp);
    }

    private void getTypeLayout(CORINFO_CLASS_STRUCT_* typeHnd, CORINFO_TYPE_LAYOUT_NODE* nodes, nuint* numNodes, GetTypeLayoutResult result)
    {
        mc.recGetTypeLayout(result, typeHnd, nodes, *numNodes);
    }

    private void checkMethodModifier(CORINFO_METHOD_STRUCT_* hMethod, byte* modifier, bool fOptional, bool result)
    {
        mc.recCheckMethodModifier(hMethod, modifier, fOptional, result);
    }

    private void getNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls, CorInfoHelpFunc temp)
    {
        mc.recGetNewArrHelper(arrayCls, temp);
    }

    private void getCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing, CorInfoHelpFunc temp)
    {
        mc.recGetCastingHelper(ref pResolvedToken, fThrowing, temp);
    }

    private void getSharedCCtorHelper(CORINFO_CLASS_STRUCT_* clsHnd, CorInfoHelpFunc temp)
    {
        mc.recGetSharedCCtorHelper(clsHnd, temp);
    }

    private void getTypeForBox(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetTypeForBox(cls, temp);
    }

    private void getBoxHelper(CORINFO_CLASS_STRUCT_* cls, CorInfoHelpFunc temp)
    {
        mc.recGetBoxHelper(cls, temp);
    }

    private void getUnBoxHelper(CORINFO_CLASS_STRUCT_* cls, CorInfoHelpFunc temp)
    {
        mc.recGetUnBoxHelper(cls, temp);
    }

    private void getRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls, CORINFO_OBJECT_STRUCT_* temp)
    {
        mc.recGetRuntimeTypePointer(cls, temp);
    }

    private void isObjectImmutable(CORINFO_OBJECT_STRUCT_* typeObj, bool temp)
    {
        mc.recIsObjectImmutable(typeObj, temp);
    }

    private void getStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* value, bool temp)
    {
        mc.recGetStringChar(strObj, index, value, temp);
    }

    private void getObjectType(CORINFO_OBJECT_STRUCT_* typeObj, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetObjectType(typeObj, temp);
    }

    private void getReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CorInfoHelpFunc id, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_CONST_LOOKUP pLookup, bool result)
    {
        mc.recGetReadyToRunHelper(ref pResolvedToken, id, callerHandle, ref pLookup, result);
    }

    private void getReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_LOOKUP pLookup)
    {
        mc.recGetReadyToRunDelegateCtorHelper(ref pTargetMethod, (uint)targetConstraint, delegateType, callerHandle, ref pLookup);
    }

    private void initClass(CORINFO_FIELD_STRUCT_* field, CORINFO_METHOD_STRUCT_* method, CORINFO_CONTEXT_STRUCT* context, CorInfoInitClassResult temp)
    {
        mc.recInitClass(field, method, context, temp);
    }

    private void getBuiltinClass(CorInfoClassId classId, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetBuiltinClass(classId, temp);
    }

    private void getTypeForPrimitiveValueClass(CORINFO_CLASS_STRUCT_* cls, CorInfoType temp)
    {
        mc.recGetTypeForPrimitiveValueClass(cls, temp);
    }

    private void getTypeForPrimitiveNumericClass(CORINFO_CLASS_STRUCT_* cls, CorInfoType temp)
    {
        mc.recGetTypeForPrimitiveNumericClass(cls, temp);
    }

    private void compareTypesForCast(CORINFO_CLASS_STRUCT_* fromClass, CORINFO_CLASS_STRUCT_* toClass, TypeCompareState temp)
    {
        mc.recCompareTypesForCast(fromClass, toClass, temp);
    }

    private void compareTypesForEquality(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2, TypeCompareState temp)
    {
        mc.recCompareTypesForEquality(cls1, cls2, temp);
    }

    private void isMoreSpecificType(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2, bool temp)
    {
        mc.recIsMoreSpecificType(cls1, cls2, temp);
    }

    private void isExactType(CORINFO_CLASS_STRUCT_* cls, bool temp)
    {
        mc.recIsExactType(cls, temp);
    }

    private void isGenericType(CORINFO_CLASS_STRUCT_* cls, TypeCompareState temp)
    {
        mc.recIsGenericType(cls, temp);
    }

    private void isNullableType(CORINFO_CLASS_STRUCT_* cls, TypeCompareState temp)
    {
        mc.recIsNullableType(cls, temp);
    }

    private void getParentType(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetParentType(cls, temp);
    }

    private void getChildType(CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_CLASS_STRUCT_** clsRet, CorInfoType temp)
    {
        mc.recGetChildType(clsHnd, clsRet, temp);
    }

    private void isSDArray(CORINFO_CLASS_STRUCT_* cls, bool temp)
    {
        mc.recIsSDArray(cls, temp);
    }

    private void getArrayRank(CORINFO_CLASS_STRUCT_* cls, uint result)
    {
        mc.recGetArrayRank(cls, result);
    }

    private void getArrayIntrinsicID(CORINFO_METHOD_STRUCT_* ftn, CorInfoArrayIntrinsic result)
    {
        mc.recGetArrayIntrinsicID(ftn, result);
    }

    private void getArrayInitializationData(CORINFO_FIELD_STRUCT_* field, uint size, void* temp)
    {
        mc.recGetArrayInitializationData(field, size, temp);
    }

    private void canAccessClass(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_HELPER_DESC pAccessHelper, CorInfoIsAccessAllowedResult temp)
    {
        mc.recCanAccessClass(ref pResolvedToken, callerHandle, ref pAccessHelper, temp);
    }

    private void printFieldName(CORINFO_FIELD_STRUCT_* ftn, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint temp)
    {
        mc.recPrintFieldName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
    }

    private void getFieldClass(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recGetFieldClass(field, temp);
    }

    private void getFieldType(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_** structType, CORINFO_CLASS_STRUCT_* fieldOwnerHint, CorInfoType temp)
    {
        mc.recGetFieldType(field, structType, fieldOwnerHint, temp);
    }

    private void getFieldOffset(CORINFO_FIELD_STRUCT_* field, uint temp)
    {
        mc.recGetFieldOffset(field, temp);
    }

    private void getFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
    {
        mc.recGetFieldInfo(ref pResolvedToken, callerHandle, flags, pResult);
    }

    private void getThreadLocalFieldInfo(CORINFO_FIELD_STRUCT_* field, bool isGCType, uint result)
    {
        mc.recGetThreadLocalFieldInfo(field, isGCType, result);
    }

    private void getThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
    {
        mc.recGetThreadLocalStaticBlocksInfo(pInfo);
    }

    private void getThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
    {
        mc.recGetThreadLocalStaticInfo_NativeAOT(pInfo);
    }

    private void isFieldStatic(CORINFO_FIELD_STRUCT_* fldHnd, bool result)
    {
        mc.recIsFieldStatic(fldHnd, result);
    }

    private void getArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHnd, int result)
    {
        mc.recGetArrayOrStringLength(objHnd, result);
    }

    private void getBoundaries(CORINFO_METHOD_STRUCT_* ftn, ref uint cILOffsets, ref uint* pILOffsets, BoundaryTypes* implicitBoundaries)
    {
        mc.recGetBoundaries(ftn, ref cILOffsets, ref pILOffsets, implicitBoundaries);
    }

    private void setBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
    {
        cr.recSetBoundaries(ftn, cMap, pMap);
    }

    private void getVars(CORINFO_METHOD_STRUCT_* ftn, ref uint cVars, ILVarInfo** vars, ref bool extendOthers)
    {
        mc.recGetVars(ftn, ref cVars, vars, ref extendOthers);
    }

    private void setVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
    {
        cr.recSetVars(ftn, cVars, vars);
    }

    private void reportRichMappings(InlineTreeNode* inlineTreeNodes, uint numInlineTreeNodes, RichOffsetMapping* mappings, uint numMappings)
    {
    }

    private void reportAsyncDebugInfo(AsyncInfo* asyncInfo, AsyncSuspensionPoint* suspensionPoints, AsyncContinuationVarInfo* vars, uint numVars)
    {
    }

    private void reportMetadata(byte* key, void* value, nuint length)
    {
    }

    private void getArgNext(CORINFO_ARG_LIST_STRUCT_* args, CORINFO_ARG_LIST_STRUCT_* temp)
    {
        mc.recGetArgNext(args, temp);
    }

    private void getHFAType(CORINFO_CLASS_STRUCT_* hClass, CorInfoHFAElemType temp)
    {
        mc.recGetHFAType(hClass, temp);
    }

    private void getEEInfo(ref CORINFO_EE_INFO pEEInfoOut)
    {
        mc.recGetEEInfo(ref pEEInfoOut);
    }

    private void getAsyncInfo(ref CORINFO_ASYNC_INFO pAsyncInfo)
    {
        mc.recGetAsyncInfo(ref pAsyncInfo);
    }

    private void getAwaitReturnCall(CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg, CORINFO_METHOD_STRUCT_* result)
    {
        mc.recGetAwaitReturnCall(callerHandle, contextHandle, ref instArg, result);
    }

    private void getAwaitAwaiterInContinuationCall(CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool isUnsafe, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg, CORINFO_METHOD_STRUCT_* result)
    {
        mc.recGetAwaitAwaiterInContinuationCall(callerHandle, ref pResolvedToken, isUnsafe, contextHandle, ref instArg, result);
    }

    private void getMethodDefFromMethod(CORINFO_METHOD_STRUCT_* hMethod, mdToken result)
    {
        mc.recGetMethodDefFromMethod(hMethod, (uint)result);
    }

    private void printMethodName(CORINFO_METHOD_STRUCT_* ftn, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint temp)
    {
        mc.recPrintMethodName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
    }

    private void getMethodNameFromMetadata(CORINFO_METHOD_STRUCT_* ftn, byte** className, byte** namespaceName, byte** enclosingClassNames, nuint maxEnclosingClassNames, byte* temp)
    {
        mc.recGetMethodNameFromMetadata(ftn, temp, className, namespaceName, enclosingClassNames, maxEnclosingClassNames);
    }

    private void getMethodHash(CORINFO_METHOD_STRUCT_* ftn, uint temp)
    {
        mc.recGetMethodHash(ftn, temp);
    }

    private void getSystemVAmd64PassStructInRegisterDescriptor(CORINFO_CLASS_STRUCT_* structHnd, SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr, bool result)
    {
        mc.recGetSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr, result);
    }

    private void getSwiftLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_SWIFT_LOWERING pLowering)
    {
        mc.recGetSwiftLowering(structHnd, ref pLowering);
    }

    private void getFpStructLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_FPSTRUCT_LOWERING pLowering)
    {
        mc.recGetFpStructLowering(structHnd, ref pLowering);
    }

    private void getWasmLowering(CORINFO_CLASS_STRUCT_* structHnd, CorInfoWasmType result)
    {
        mc.recGetWasmLowering(structHnd, result);
    }

    private void getAddressAlignment(void* address, uint result)
    {
        mc.recGetAddressAlignment(address, result);
    }

    private void getWasmWellKnownGlobals(ref CORINFO_WASM_WELLKNOWN_GLOBALS pWellKnownGlobalsOut)
    {
        mc.recGetWasmWellKnownGlobals(ref pWellKnownGlobalsOut);
    }

    private void getThreadTLSIndex(ref void* ppIndirection, uint temp)
    {
        mc.recGetThreadTLSIndex(ref ppIndirection, temp);
    }

    private void getAddrOfCaptureThreadGlobal(ref void* ppIndirection, int* temp)
    {
        mc.recGetAddrOfCaptureThreadGlobal(ref ppIndirection, temp);
    }

    private void getFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
    {
        mc.recGetFunctionEntryPoint(ftn, ref pResult, accessFlags);
    }

    private void getFunctionFixedEntryPoint(CORINFO_METHOD_STRUCT_* ftn, bool isUnsafeFunctionPointer, ref CORINFO_CONST_LOOKUP pResult)
    {
        mc.recGetFunctionFixedEntryPoint(ftn, ref pResult);
    }

    private void embedModuleHandle(CORINFO_MODULE_STRUCT_* handle, ref void* ppIndirection, CORINFO_MODULE_STRUCT_* temp)
    {
        mc.recEmbedModuleHandle(handle, ref ppIndirection, temp);
    }

    private void embedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection, CORINFO_CLASS_STRUCT_* temp)
    {
        mc.recEmbedClassHandle(handle, ref ppIndirection, temp);
    }

    private void embedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection, CORINFO_METHOD_STRUCT_* temp)
    {
        mc.recEmbedMethodHandle(handle, ref ppIndirection, temp);
    }

    private void embedFieldHandle(CORINFO_FIELD_STRUCT_* handle, ref void* ppIndirection, CORINFO_FIELD_STRUCT_* temp)
    {
        mc.recEmbedFieldHandle(handle, ref ppIndirection, temp);
    }

    private void embedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        mc.recEmbedGenericHandle(ref pResolvedToken, fEmbedParent, callerHandle, ref pResult);
    }

    private void getLocationOfThisType(CORINFO_METHOD_STRUCT_* context, ref CORINFO_LOOKUP_KIND pLookupKind)
    {
        mc.recGetLocationOfThisType(context, ref pLookupKind);
    }

    private void getAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
    {
        mc.recGetAddressOfPInvokeTarget(method, ref pLookup);
    }

    private void GetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig, void* temp)
    {
        mc.recGetCookieForInterpreterCalliSig(szMetaSig, temp);
    }

    private void getJustMyCodeHandle(CORINFO_METHOD_STRUCT_* method, ref CORINFO_JUST_MY_CODE_HANDLE_* ppIndirection, CORINFO_JUST_MY_CODE_HANDLE_* temp)
    {
        mc.recGetJustMyCodeHandle(method, ref ppIndirection, temp);
    }

    private void GetProfilingHandle(ref bool pbHookFunction, ref void* pProfilerHandle, ref bool pbIndirectedHandles)
    {
        mc.recGetProfilingHandle(ref pbHookFunction, ref pProfilerHandle, ref pbIndirectedHandles);
    }

    private void getStaticFieldContent(CORINFO_FIELD_STRUCT_* field, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects, bool result)
    {
        mc.recGetStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects, result);
    }

    private void getObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset, bool result)
    {
        mc.recGetObjectContent(obj, buffer, bufferSize, valueOffset, result);
    }

    private void getStaticFieldCurrentClass(CORINFO_FIELD_STRUCT_* field, byte* pIsSpeculative, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetStaticFieldCurrentClass(field, (bool*)pIsSpeculative, result);
    }

    private void getVarArgsHandle(CORINFO_SIG_INFO* pSig, CORINFO_METHOD_STRUCT_* methHnd, ref void* ppIndirection, IntPtr temp)
    {
        mc.recGetVarArgsHandle(pSig, methHnd, ref ppIndirection, (void*)temp);
    }

    private void constructStringLiteral(CORINFO_MODULE_STRUCT_* module, mdToken metaTok, ref void* ppValue, InfoAccessType temp)
    {
        mc.recConstructStringLiteral(module, (uint)metaTok, ppValue, temp);
    }

    private void emptyStringLiteral(ref void* ppValue, InfoAccessType temp)
    {
        mc.recEmptyStringLiteral(ref ppValue, temp);
    }

    private void getFieldThreadLocalStoreID(CORINFO_FIELD_STRUCT_* field, ref void* ppIndirection, uint temp)
    {
        mc.recGetFieldThreadLocalStoreID(field, ref ppIndirection, temp);
    }

    private void GetDelegateCtor(CORINFO_METHOD_STRUCT_* methHnd, CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_METHOD_STRUCT_* targetMethodHnd, ref DelegateCtorArgs pCtorData, CORINFO_METHOD_STRUCT_* temp)
    {
        mc.recGetDelegateCtor(methHnd, clsHnd, targetMethodHnd, ref pCtorData, temp);
    }

    private void MethodCompileComplete(CORINFO_METHOD_STRUCT_* methHnd)
    {
    }

    private void getContinuationType(nuint dataSize, ref bool objRefs, nuint objRefsSize, CORINFO_CLASS_STRUCT_* result)
    {
        mc.recGetContinuationType(dataSize, ref objRefs, objRefsSize, result);
    }

    private void getAsyncResumptionStub(ref void* entryPoint, CORINFO_METHOD_STRUCT_* stub)
    {
        mc.recGetAsyncResumptionStub(stub, entryPoint);
    }

    private void convertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fMustConvert, bool result)
    {
        mc.recConvertPInvokeCalliToCall(ref pResolvedToken, fMustConvert, result);
    }

    private void notifyInstructionSetUsage(InstructionSet instructionSet, bool supported, bool preserveNegativeDependency, bool result)
    {
        mc.recNotifyInstructionSetUsage(instructionSet, supported, preserveNegativeDependency, result);
    }

    private void allocMem(ref AllocMemArgs pArgs)
    {
    }

    private void reserveUnwindInfo(bool isFunclet, bool isColdCode, uint unwindSize)
    {
        cr.recReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    }

    private void allocUnwindInfo(byte* pHotCode, byte* pColdCode, uint startOffset, uint endOffset, uint unwindSize, byte* pUnwindBlock, CorJitFuncKind funcKind)
    {
        cr.recAllocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
    }

    private void allocGCInfo(nuint size, void* temp)
    {
        cr.recAllocGCInfo(size, temp);
    }

    private void setEHcount(uint cEH)
    {
        cr.recSetEHcount(cEH);
    }

    private void setEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        cr.recSetEHinfo(EHnumber, ref clause);
    }

    private void logMsg(uint level, byte* fmt, IntPtr args, bool result)
    {
    }

    private void reportFatalError(CorJitResult result)
    {
        cr.recReportFatalError(result);
    }

    private void allocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData, HRESULT result)
    {
        mc.recAllocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData, (int)result);
    }

    private void recordCallSite(uint instrOffset, CORINFO_SIG_INFO* callSig, CORINFO_METHOD_STRUCT_* methodHandle)
    {
        cr.recRecordCallSite(instrOffset, callSig, methodHandle);
    }

    private void recordRelocation(void* location, void* locationRW, void* target, CorInfoReloc fRelocType, int addlDelta)
    {
        cr.recRecordRelocation(location, target, fRelocType, addlDelta);
    }

    private void getRelocTypeHint(void* target, CorInfoReloc result)
    {
        mc.recGetRelocTypeHint(target, result);
    }

    private void getExpectedTargetArchitecture(uint result)
    {
        mc.recGetExpectedTargetArchitecture(result);
    }

    private void getJitFlags(ref CORJIT_FLAGS jitFlags, uint sizeInBytes, uint result)
    {
        mc.recGetJitFlags(ref jitFlags, sizeInBytes, result);
    }

    private void getWasmTypeSymbol(CorInfoWasmType* types, nuint typesSize, CORINFO_WASM_TYPE_SYMBOL_STRUCT_* temp)
    {
        mc.recGetWasmTypeSymbol(types, typesSize, temp);
    }

    private void getSpecialCopyHelper(CORINFO_CLASS_STRUCT_* type, CORINFO_METHOD_STRUCT_* temp)
    {
        mc.recGetSpecialCopyHelper(type, temp);
    }
}
