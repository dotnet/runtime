// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Pgo;
using SuperPmi;
using SuperPmi.Replay;

namespace Internal.JitInterface;

// The JIT-EE interface for replay: answers the JIT's queries from the method context being replayed, and
// records what the JIT reports into its compile result.
internal sealed unsafe partial class CorInfoImpl
{
    private readonly JitInstance jitInstance;
    private readonly List<(IntPtr Native, Exception Managed)> _exceptions = [];
    private int m_metricSearchStart;

    public CorInfoImpl(JitInstance jitInstance) => this.jitInstance = jitInstance;

    private MethodContext mc => jitInstance.mc;

    private CompileResult cr => jitInstance.mc.cr;

    private static CorInfoImpl GetThis(IntPtr thisHandle) => Unsafe.Read<CorInfoImpl>((void*)thisHandle);

    // Rethrown through the JIT as a C++ exception, whose kind decides the error traps that catch it.
    private IntPtr AllocException(Exception ex)
    {
        SpmiJitInterface.ExceptionKind kind = ex switch
        {
            SpmiException { Code: ExceptionCodes.RECORDED_EXCEPTION } => SpmiJitInterface.ExceptionKind.EEException,
            SpmiException => SpmiJitInterface.ExceptionKind.SuperPmiException,
            _ => SpmiJitInterface.ExceptionKind.HostFailure,
        };
        IntPtr nativeException = SpmiJitInterface.AllocException(kind);
        _exceptions.Add((nativeException, ex));
        return nativeException;
    }

    // Compiles the method, rethrowing any exception that escaped the JIT.
    public CorJitResult CompileMethod(IntPtr jit, CORINFO_METHOD_INFO* info, uint flags, out byte* nativeEntry, out uint nativeSizeOfCode)
    {
        CorInfoImpl _this = this;
        IntPtr exception;
        uint exceptionCode;
        byte* entry;
        uint size;
        CorJitResult result = SpmiJitInterface.JitCompileMethod(&exception, &exceptionCode, jit, (IntPtr)Unsafe.AsPointer(ref _this),
            GetUnmanagedCallbacks(), info, flags, &entry, &size);

        Exception escaped = null;
        foreach ((IntPtr native, Exception managed) in _exceptions)
        {
            if (native == exception)
                escaped = managed;
            SpmiJitInterface.FreeException(native);
        }
        _exceptions.Clear();

        if (escaped != null)
            ExceptionDispatchInfo.Throw(escaped);
        if (exceptionCode != 0)
            throw new NativeException(exceptionCode);

        nativeEntry = entry;
        nativeSizeOfCode = size;
        return result;
    }

    private void AddCall(string name) => cr.AddCall(name);

    // Rethrows an exception that the EE threw during recording. The JIT doesn't look closely at the exceptions
    // that the EE throws, so changing their code is fine.
    private static void ThrowRecordedException(uint innerExceptionCode)
    {
        if (SpmiUtil.BreakOnException)
            System.Diagnostics.Debugger.Break();
        throw new SpmiException(ExceptionCodes.RECORDED_EXCEPTION, $"Recorded exception 0x{innerExceptionCode:x}");
    }

    private static nuint AlignUp(nuint value, nuint alignment) => (value + (alignment - 1)) & ~(alignment - 1);

    private bool isIntrinsic(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("isIntrinsic");
        return mc.repIsIntrinsic(ftn);
    }

    private bool canValueClassInstancePointerEscape(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("canValueClassInstancePointerEscape");
        return mc.repCanValueClassInstancePointerEscape(ftn);
    }

    private bool notifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("notifyMethodInfoUsage");
        return mc.repNotifyMethodInfoUsage(ftn);
    }

    private uint getMethodAttribs(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getMethodAttribs");
        return mc.repGetMethodAttribs(ftn);
    }

    private void setMethodAttribs(CORINFO_METHOD_STRUCT_* ftn, CorInfoMethodRuntimeFlags attribs)
    {
        AddCall("setMethodAttribs");
        cr.recSetMethodAttribs(ftn, attribs);
    }

    private void getMethodSig(CORINFO_METHOD_STRUCT_* ftn, CORINFO_SIG_INFO* sig, CORINFO_CLASS_STRUCT_* memberParent)
    {
        AddCall("getMethodSig");
        mc.repGetMethodSig(ftn, sig, memberParent);
    }

    private bool getMethodInfo(CORINFO_METHOD_STRUCT_* ftn, CORINFO_METHOD_INFO* info, CORINFO_CONTEXT_STRUCT* context)
    {
        AddCall("getMethodInfo");
        bool value = mc.repGetMethodInfo(ftn, info, context, out uint exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
        return value;
    }

    private bool haveSameMethodDefinition(CORINFO_METHOD_STRUCT_* meth1Hnd, CORINFO_METHOD_STRUCT_* meth2Hnd)
    {
        AddCall("haveSameMethodDefinition");
        return mc.repHaveSameMethodDefinition(meth1Hnd, meth2Hnd);
    }

    private CORINFO_CLASS_STRUCT_* getTypeDefinition(CORINFO_CLASS_STRUCT_* type)
    {
        AddCall("getTypeDefinition");
        return mc.repGetTypeDefinition(type);
    }

    private CorInfoInline canInline(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd)
    {
        AddCall("canInline");
        uint exceptionCode = 0;
        CorInfoInline result = mc.repCanInline(callerHnd, calleeHnd, ref exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
        return result;
    }

    private void beginInlining(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd)
    {
    }

    private void reportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
    {
        AddCall("reportInliningDecision");
        cr.recReportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
    }

    private bool canTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix)
    {
        AddCall("canTailCall");
        return mc.repCanTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
    }

    private void reportTailCallDecision(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, bool fIsTailPrefix, CorInfoTailCall tailCallResult, byte* reason)
    {
        AddCall("reportTailCallDecision");
        cr.recReportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
    }

    private void getEHinfo(CORINFO_METHOD_STRUCT_* ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        AddCall("getEHinfo");
        mc.repGetEHinfo(ftn, EHnumber, ref clause);
    }

    private CORINFO_CLASS_STRUCT_* getMethodClass(CORINFO_METHOD_STRUCT_* method)
    {
        AddCall("getMethodClass");
        return mc.repGetMethodClass(method);
    }

    private void getMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
    {
        AddCall("getMethodVTableOffset");
        mc.repGetMethodVTableOffset(method, ref offsetOfIndirection, ref offsetAfterIndirection, ref isRelative);
    }

    private bool resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO* info)
    {
        AddCall("resolveVirtualMethod");
        return mc.repResolveVirtualMethod(info);
    }

    private CORINFO_METHOD_STRUCT_* getAsyncOtherVariant(CORINFO_METHOD_STRUCT_* ftn, ref bool variantIsThunk)
    {
        AddCall("getAsyncOtherVariant");
        return mc.repGetAsyncOtherVariant(ftn, ref variantIsThunk);
    }

    private CORINFO_CLASS_STRUCT_* getDefaultComparerClass(CORINFO_CLASS_STRUCT_* elemType)
    {
        AddCall("getDefaultComparerClass");
        return mc.repGetDefaultComparerClass(elemType);
    }

    private CORINFO_CLASS_STRUCT_* getDefaultEqualityComparerClass(CORINFO_CLASS_STRUCT_* elemType)
    {
        AddCall("getDefaultEqualityComparerClass");
        return mc.repGetDefaultEqualityComparerClass(elemType);
    }

    private CORINFO_CLASS_STRUCT_* getSZArrayHelperEnumeratorClass(CORINFO_CLASS_STRUCT_* elemType)
    {
        AddCall("getSZArrayHelperEnumeratorClass");
        return mc.repGetSZArrayHelperEnumeratorClass(elemType);
    }

    private void expandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        AddCall("expandRawHandleIntrinsic");
        mc.repExpandRawHandleIntrinsic(ref pResolvedToken, callerHandle, ref pResult);
    }

    private bool isIntrinsicType(CORINFO_CLASS_STRUCT_* classHnd)
    {
        AddCall("isIntrinsicType");
        return mc.repIsIntrinsicType(classHnd);
    }

    private CorInfoCallConvExtension getUnmanagedCallConv(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, ref bool pSuppressGCTransition)
    {
        AddCall("getUnmanagedCallConv");
        return mc.repGetUnmanagedCallConv(method, callSiteSig, ref pSuppressGCTransition);
    }

    private bool pInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig)
    {
        AddCall("pInvokeMarshalingRequired");
        return mc.repPInvokeMarshalingRequired(method, callSiteSig);
    }

    private bool satisfiesMethodConstraints(CORINFO_CLASS_STRUCT_* parent, CORINFO_METHOD_STRUCT_* method)
    {
        AddCall("satisfiesMethodConstraints");
        return mc.repSatisfiesMethodConstraints(parent, method);
    }

    private void getGSCookie(IntPtr* pCookieVal, IntPtr** ppCookieVal)
    {
        AddCall("getGSCookie");
        mc.repGetGSCookie(pCookieVal, (nint**)ppCookieVal);
    }

    private void setPatchpointInfo(PatchpointInfo* patchpointInfo)
    {
        AddCall("setPatchpointInfo");
        cr.recSetPatchpointInfo(patchpointInfo);
        freeArray(patchpointInfo); // Ownership of the array is ours.
    }

    private PatchpointInfo* getOSRInfo(ref uint ilOffset)
    {
        AddCall("getOSRInfo");
        return mc.repGetOSRInfo(ref ilOffset);
    }

    private void resolveToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        AddCall("resolveToken");
        uint exceptionCode;
        mc.repResolveToken(ref pResolvedToken, out exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
    }

    private void findSig(CORINFO_MODULE_STRUCT_* module, uint sigTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        AddCall("findSig");
        mc.repFindSig(module, sigTOK, context, sig);
    }

    private void findCallSiteSig(CORINFO_MODULE_STRUCT_* module, uint methTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        AddCall("findCallSiteSig");
        mc.repFindCallSiteSig(module, methTOK, context, sig);
    }

    private CORINFO_CLASS_STRUCT_* getTokenTypeAsHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        AddCall("getTokenTypeAsHandle");
        return mc.repGetTokenTypeAsHandle(ref pResolvedToken);
    }

    private int getStringLiteral(CORINFO_MODULE_STRUCT_* module, uint metaTOK, char* buffer, int bufferSize, int startIndex)
    {
        AddCall("getStringLiteral");
        return mc.repGetStringLiteral(module, metaTOK, buffer, bufferSize, startIndex);
    }

    private nuint printObjectDescription(CORINFO_OBJECT_STRUCT_* handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printObjectDescription");
        return mc.repPrintObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize);
    }

    private CorInfoType asCorInfoType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("asCorInfoType");
        return mc.repAsCorInfoType(cls);
    }

    private byte* getClassNameFromMetadata(CORINFO_CLASS_STRUCT_* cls, byte** namespaceName)
    {
        AddCall("getClassNameFromMetadata");
        return mc.repGetClassNameFromMetadata(cls, namespaceName);
    }

    private CORINFO_CLASS_STRUCT_* getTypeInstantiationArgument(CORINFO_CLASS_STRUCT_* cls, uint index)
    {
        AddCall("getTypeInstantiationArgument");
        return mc.repGetTypeInstantiationArgument(cls, index);
    }

    private CORINFO_CLASS_STRUCT_* getMethodInstantiationArgument(CORINFO_METHOD_STRUCT_* ftn, uint index)
    {
        AddCall("getMethodInstantiationArgument");
        return mc.repGetMethodInstantiationArgument(ftn, index);
    }

    private nuint printClassName(CORINFO_CLASS_STRUCT_* cls, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printClassName");
        return mc.repPrintClassName(cls, buffer, bufferSize, pRequiredBufferSize);
    }

    private bool isValueClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isValueClass");
        return mc.repIsValueClass(cls);
    }

    private bool isByRefLike(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isByRefLike");
        return mc.repIsByRefLike(cls);
    }

    private bool isSharedInst(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isSharedInst");
        return mc.repIsSharedInst(cls);
    }

    private uint getClassAttribs(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassAttribs");
        return mc.repGetClassAttribs(cls);
    }

    private byte* getClassAssemblyName(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassAssemblyName");
        return mc.repGetClassAssemblyName(cls);
    }

    private void* LongLifetimeMalloc(nuint sz)
    {
        AddCall("LongLifetimeMalloc");
        Logger.LogError("Hit unimplemented LongLifetimeMalloc");
        SpmiUtil.DebugBreakorAV(32);
        return null;
    }

    private void LongLifetimeFree(void* obj)
    {
        AddCall("LongLifetimeFree");
        Logger.LogError("Hit unimplemented LongLifetimeFree");
        SpmiUtil.DebugBreakorAV(33);
    }

    private bool getIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset)
    {
        AddCall("getIsClassInitedFlagAddress");
        return mc.repGetIsClassInitedFlagAddress(cls, ref addr, ref offset);
    }

    private void* getClassThreadStaticDynamicInfo(CORINFO_CLASS_STRUCT_* clr)
    {
        AddCall("getClassThreadStaticDynamicInfo");
        return mc.repGetClassThreadStaticDynamicInfo(clr);
    }

    private void* getClassStaticDynamicInfo(CORINFO_CLASS_STRUCT_* clr)
    {
        AddCall("getClassStaticDynamicInfo");
        return mc.repGetClassStaticDynamicInfo(clr);
    }

    private bool getStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr)
    {
        AddCall("getStaticBaseAddress");
        return mc.repGetStaticBaseAddress(cls, isGc, ref addr);
    }

    private uint getClassSize(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassSize");
        return mc.repGetClassSize(cls);
    }

    private uint getHeapClassSize(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getHeapClassSize");
        return mc.repGetHeapClassSize(cls);
    }

    private bool canAllocateOnStack(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("canAllocateOnStack");
        return mc.repCanAllocateOnStack(cls);
    }

    private uint getClassAlignmentRequirement(CORINFO_CLASS_STRUCT_* cls, bool fDoubleAlignHint)
    {
        AddCall("getClassAlignmentRequirement");
        return mc.repGetClassAlignmentRequirement(cls, fDoubleAlignHint);
    }

    private uint getClassGClayout(CORINFO_CLASS_STRUCT_* cls, byte* gcPtrs)
    {
        AddCall("getClassGClayout");
        return mc.repGetClassGClayout(cls, gcPtrs);
    }

    private uint getClassNumInstanceFields(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassNumInstanceFields");
        return mc.repGetClassNumInstanceFields(cls);
    }

    private CORINFO_FIELD_STRUCT_* getFieldInClass(CORINFO_CLASS_STRUCT_* clsHnd, int num)
    {
        AddCall("getFieldInClass");
        return mc.repGetFieldInClass(clsHnd, num);
    }

    private GetTypeLayoutResult getTypeLayout(CORINFO_CLASS_STRUCT_* typeHnd, CORINFO_TYPE_LAYOUT_NODE* treeNodes, nuint* numTreeNodes)
    {
        AddCall("getTypeLayout");
        return mc.repGetTypeLayout(typeHnd, treeNodes, numTreeNodes);
    }

    private bool checkMethodModifier(CORINFO_METHOD_STRUCT_* hMethod, byte* modifier, bool fOptional)
    {
        AddCall("checkMethodModifier");
        return mc.repCheckMethodModifier(hMethod, modifier, fOptional);
    }

    private CorInfoHelpFunc getNewHelper(CORINFO_CLASS_STRUCT_* classHandle, ref bool pHasSideEffects)
    {
        AddCall("getNewHelper");
        CorInfoHelpFunc result;
        uint exceptionCode;
        result = mc.repGetNewHelper(classHandle, ref pHasSideEffects, out exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
        return result;
    }

    private CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls)
    {
        AddCall("getNewArrHelper");
        return mc.repGetNewArrHelper(arrayCls);
    }

    private CorInfoHelpFunc getCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing)
    {
        AddCall("getCastingHelper");
        return mc.repGetCastingHelper(ref pResolvedToken, fThrowing);
    }

    private CorInfoHelpFunc getSharedCCtorHelper(CORINFO_CLASS_STRUCT_* clsHnd)
    {
        AddCall("getSharedCCtorHelper");
        return mc.repGetSharedCCtorHelper(clsHnd);
    }

    private CORINFO_CLASS_STRUCT_* getTypeForBox(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForBox");
        return mc.repGetTypeForBox(cls);
    }

    private CorInfoHelpFunc getBoxHelper(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getBoxHelper");
        return mc.repGetBoxHelper(cls);
    }

    private CorInfoHelpFunc getUnBoxHelper(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getUnBoxHelper");
        return mc.repGetUnBoxHelper(cls);
    }

    private CORINFO_OBJECT_STRUCT_* getRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getRuntimeTypePointer");
        return mc.repGetRuntimeTypePointer(cls);
    }

    private bool isObjectImmutable(CORINFO_OBJECT_STRUCT_* objPtr)
    {
        AddCall("isObjectImmutable");
        return mc.repIsObjectImmutable(objPtr);
    }

    private bool getStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* value)
    {
        AddCall("getStringChar");
        return mc.repGetStringChar(strObj, index, value);
    }

    private CORINFO_CLASS_STRUCT_* getObjectType(CORINFO_OBJECT_STRUCT_* objPtr)
    {
        AddCall("getObjectType");
        return mc.repGetObjectType(objPtr);
    }

    private bool getReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CorInfoHelpFunc id, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_CONST_LOOKUP pLookup)
    {
        AddCall("getReadyToRunHelper");
        return mc.repGetReadyToRunHelper(ref pResolvedToken, id, callerHandle, ref pLookup);
    }

    private void getReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType,
        CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_LOOKUP pLookup)
    {
        AddCall("getReadyToRunDelegateCtorHelper");
        mc.repGetReadyToRunDelegateCtorHelper(ref pTargetMethod, targetConstraint, delegateType, callerHandle, ref pLookup);
    }

    private CorInfoInitClassResult initClass(CORINFO_FIELD_STRUCT_* field, CORINFO_METHOD_STRUCT_* method, CORINFO_CONTEXT_STRUCT* context)
    {
        AddCall("initClass");
        return mc.repInitClass(field, method, context);
    }

    private CORINFO_CLASS_STRUCT_* getBuiltinClass(CorInfoClassId classId)
    {
        AddCall("getBuiltinClass");
        return mc.repGetBuiltinClass(classId);
    }

    private CorInfoType getTypeForPrimitiveValueClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForPrimitiveValueClass");
        return mc.repGetTypeForPrimitiveValueClass(cls);
    }

    private CorInfoType getTypeForPrimitiveNumericClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForPrimitiveNumericClass");
        return mc.repGetTypeForPrimitiveNumericClass(cls);
    }

    private bool canCast(CORINFO_CLASS_STRUCT_* child, CORINFO_CLASS_STRUCT_* parent)
    {
        AddCall("canCast");
        return mc.repCanCast(child, parent);
    }

    private TypeCompareState compareTypesForCast(CORINFO_CLASS_STRUCT_* fromClass, CORINFO_CLASS_STRUCT_* toClass)
    {
        AddCall("compareTypesForCast");
        return mc.repCompareTypesForCast(fromClass, toClass);
    }

    private TypeCompareState compareTypesForEquality(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        AddCall("compareTypesForEquality");
        return mc.repCompareTypesForEquality(cls1, cls2);
    }

    private bool isMoreSpecificType(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        AddCall("isMoreSpecificType");
        return mc.repIsMoreSpecificType(cls1, cls2);
    }

    private bool isExactType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isExactType");
        return mc.repIsExactType(cls);
    }

    private TypeCompareState isGenericType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isGenericType");
        return mc.repIsGenericType(cls);
    }

    private TypeCompareState isNullableType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isNullableType");
        return mc.repIsNullableType(cls);
    }

    private TypeCompareState isEnum(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_** underlyingType)
    {
        AddCall("isEnum");
        return mc.repIsEnum(cls, underlyingType);
    }

    private CORINFO_CLASS_STRUCT_* getParentType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getParentType");
        return mc.repGetParentType(cls);
    }

    private CorInfoType getChildType(CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_CLASS_STRUCT_** clsRet)
    {
        AddCall("getChildType");
        return mc.repGetChildType(clsHnd, clsRet);
    }

    private bool isSDArray(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isSDArray");
        return mc.repIsSDArray(cls);
    }

    private uint getArrayRank(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getArrayRank");
        return mc.repGetArrayRank(cls);
    }

    private CorInfoArrayIntrinsic getArrayIntrinsicID(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getArrayIntrinsicID");
        return mc.repGetArrayIntrinsicID(ftn);
    }

    private void* getArrayInitializationData(CORINFO_FIELD_STRUCT_* field, uint size)
    {
        AddCall("getArrayInitializationData");
        return mc.repGetArrayInitializationData(field, size);
    }

    private CorInfoIsAccessAllowedResult canAccessClass(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_HELPER_DESC pAccessHelper)
    {
        AddCall("canAccessClass");
        return mc.repCanAccessClass(ref pResolvedToken, callerHandle, ref pAccessHelper);
    }

    private nuint printFieldName(CORINFO_FIELD_STRUCT_* field, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printFieldName");
        return mc.repPrintFieldName(field, buffer, bufferSize, pRequiredBufferSize);
    }

    private CORINFO_CLASS_STRUCT_* getFieldClass(CORINFO_FIELD_STRUCT_* field)
    {
        AddCall("getFieldClass");
        return mc.repGetFieldClass(field);
    }

    private CorInfoType getFieldType(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_** structType, CORINFO_CLASS_STRUCT_* fieldOwnerHint)
    {
        AddCall("getFieldType");
        return mc.repGetFieldType(field, structType, fieldOwnerHint);
    }

    private uint getFieldOffset(CORINFO_FIELD_STRUCT_* field)
    {
        AddCall("getFieldOffset");
        return mc.repGetFieldOffset(field);
    }

    private void getFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
    {
        AddCall("getFieldInfo");
        mc.repGetFieldInfo(ref pResolvedToken, callerHandle, flags, pResult);
    }

    private uint getThreadLocalFieldInfo(CORINFO_FIELD_STRUCT_* field, bool isGCtype)
    {
        AddCall("getThreadLocalFieldInfo");
        return mc.repGetThreadLocalFieldInfo(field, isGCtype);
    }

    private void getThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
    {
        AddCall("getThreadLocalStaticBlocksInfo");
        mc.repGetThreadLocalStaticBlocksInfo(pInfo);
    }

    private void getThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
    {
        AddCall("getThreadLocalStaticInfo_NativeAOT");
        mc.repGetThreadLocalStaticInfo_NativeAOT(pInfo);
    }

    private bool isFieldStatic(CORINFO_FIELD_STRUCT_* fldHnd)
    {
        AddCall("isFieldStatic");
        return mc.repIsFieldStatic(fldHnd);
    }

    private int getArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHnd)
    {
        AddCall("getArrayOrStringLength");
        return mc.repGetArrayOrStringLength(objHnd);
    }

    private void getBoundaries(CORINFO_METHOD_STRUCT_* ftn, ref uint cILOffsets, ref uint* pILOffsets, BoundaryTypes* implicitBoundaries)
    {
        AddCall("getBoundaries");
        mc.repGetBoundaries(ftn, ref cILOffsets, ref pILOffsets, implicitBoundaries);

        // The JIT frees the array with freeArray, so move the data to memory that complies with that.
        if (cILOffsets > 0)
        {
            nuint size = cILOffsets * sizeof(uint);
            var realOffsets = (uint*)allocateArray(size);
            Buffer.MemoryCopy(pILOffsets, realOffsets, size, size);
            pILOffsets = realOffsets;
        }
        else
        {
            pILOffsets = null;
        }
    }

    private void setBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
    {
        AddCall("setBoundaries");
        cr.recSetBoundaries(ftn, cMap, pMap);
        freeArray(pMap); // Ownership of the array is ours.
    }

    private void getVars(CORINFO_METHOD_STRUCT_* ftn, ref uint cVars, ILVarInfo** vars, ref bool extendOthers)
    {
        AddCall("getVars");
        mc.repGetVars(ftn, ref cVars, vars, ref extendOthers);

        // The JIT frees the array with freeArray, so move the data to memory that complies with that.
        if (cVars > 0)
        {
            nuint size = cVars * (nuint)sizeof(ILVarInfo);
            var realVars = (ILVarInfo*)allocateArray(size);
            Buffer.MemoryCopy(*vars, realVars, size, size);
            *vars = realVars;
        }
        else
        {
            *vars = null;
        }
    }

    private void setVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
    {
        AddCall("setVars");
        cr.recSetVars(ftn, cVars, vars);
        freeArray(vars); // Ownership of the array is ours.
    }

    // These compile outputs aren't saved.
    private void reportRichMappings(InlineTreeNode* inlineTreeNodes, uint numInlineTreeNodes, RichOffsetMapping* mappings, uint numMappings)
    {
        AddCall("reportRichMappings");
        freeArray(inlineTreeNodes);
        freeArray(mappings);
    }

    private void reportAsyncDebugInfo(AsyncInfo* asyncInfo, AsyncSuspensionPoint* suspensionPoints, AsyncContinuationVarInfo* vars, uint numVars)
    {
        AddCall("reportAsyncDebugInfo");
        freeArray(suspensionPoints);
        freeArray(vars);
    }

    private void reportMetadata(byte* key, void* value, nuint length)
    {
        AddCall("reportMetadata");

        ReadOnlySpan<byte> name = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(key);
        if (name.SequenceEqual("MethodFullName"u8))
        {
            cr.MethodFullName = Encoding.UTF8.GetString((byte*)value, (int)length);
        }
        else if (name.SequenceEqual("TieringName"u8))
        {
            cr.TieringName = Encoding.UTF8.GetString((byte*)value, (int)length);
        }
        else
        {
            int index = JitMetrics.FindIndex(name, ref m_metricSearchStart);
            if (index != -1)
                cr.Metrics.Store(index, value, length);
        }
    }

    private void* allocateArray(nuint cBytes) => jitInstance.allocateArray(cBytes);

    private void freeArray(void* array) => jitInstance.freeArray(array);

    private CORINFO_ARG_LIST_STRUCT_* getArgNext(CORINFO_ARG_LIST_STRUCT_* args)
    {
        AddCall("getArgNext");
        return mc.repGetArgNext(args);
    }

    private CorInfoTypeWithMod getArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_** vcTypeRet)
    {
        AddCall("getArgType");
        CorInfoTypeWithMod value = mc.repGetArgType(sig, args, vcTypeRet, out uint exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
        return value;
    }

    private int getExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet)
    {
        AddCall("getExactClasses");
        return mc.repGetExactClasses(baseType, maxExactClasses, exactClsRet);
    }

    private CORINFO_CLASS_STRUCT_* getArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args)
    {
        AddCall("getArgClass");
        CORINFO_CLASS_STRUCT_* value = mc.repGetArgClass(sig, args, out uint exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
        return value;
    }

    private CorInfoHFAElemType getHFAType(CORINFO_CLASS_STRUCT_* hClass)
    {
        AddCall("getHFAType");
        return mc.repGetHFAType(hClass);
    }

    // The error traps are implemented natively, by the JIT-EE interface wrapper.
    private bool runWithErrorTrap(void* function, void* parameter) => throw new NotSupportedException();

    private bool runWithSPMIErrorTrap(void* function, void* parameter) => throw new NotSupportedException();

    private void getEEInfo(ref CORINFO_EE_INFO pEEInfoOut)
    {
        AddCall("getEEInfo");
        mc.repGetEEInfo(ref pEEInfoOut);
    }

    private void getAsyncInfo(ref CORINFO_ASYNC_INFO pAsyncInfoOut)
    {
        AddCall("getAsyncInfo");
        mc.repGetAsyncInfo(ref pAsyncInfoOut);
    }

    private CORINFO_METHOD_STRUCT_* getAwaitReturnCall(CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        AddCall("getAwaitReturnCall");
        return mc.repGetAwaitReturnCall(callerHandle, contextHandle, ref instArg);
    }

    private CORINFO_METHOD_STRUCT_* getAwaitAwaiterInContinuationCall(CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool isUnsafe,
        CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        AddCall("getAwaitAwaiterInContinuationCall");
        return mc.repGetAwaitAwaiterInContinuationCall(callerHandle, ref pResolvedToken, isUnsafe, contextHandle, ref instArg);
    }

    private mdToken getMethodDefFromMethod(CORINFO_METHOD_STRUCT_* hMethod)
    {
        AddCall("getMethodDefFromMethod");
        return mc.repGetMethodDefFromMethod(hMethod);
    }

    private nuint printMethodName(CORINFO_METHOD_STRUCT_* ftn, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printMethodName");
        return mc.repPrintMethodName(ftn, buffer, bufferSize, pRequiredBufferSize);
    }

    private byte* getMethodNameFromMetadata(CORINFO_METHOD_STRUCT_* ftn, byte** className, byte** namespaceName, byte** enclosingClassNames, nuint maxEnclosingClassNames)
    {
        AddCall("getMethodNameFromMetadata");
        return mc.repGetMethodNameFromMetadata(ftn, className, namespaceName, enclosingClassNames, maxEnclosingClassNames);
    }

    private uint getMethodHash(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getMethodHash");
        return mc.repGetMethodHash(ftn);
    }

    private bool getSystemVAmd64PassStructInRegisterDescriptor(CORINFO_CLASS_STRUCT_* structHnd, SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
    {
        AddCall("getSystemVAmd64PassStructInRegisterDescriptor");
        return mc.repGetSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
    }

    private void getSwiftLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_SWIFT_LOWERING pLowering)
    {
        AddCall("getSwiftLowering");
        mc.repGetSwiftLowering(structHnd, ref pLowering);
    }

    private void getFpStructLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_FPSTRUCT_LOWERING pLowering)
    {
        AddCall("getFpStructLowering");
        mc.repGetFpStructLowering(structHnd, ref pLowering);
    }

    private CorInfoWasmType getWasmLowering(CORINFO_CLASS_STRUCT_* structHnd)
    {
        AddCall("getWasmLowering");
        return mc.repGetWasmLowering(structHnd);
    }

    private uint getAddressAlignment(void* address)
    {
        AddCall("getAddressAlignment");
        return mc.repGetAddressAlignment(address);
    }

    private void getWasmWellKnownGlobals(ref CORINFO_WASM_WELLKNOWN_GLOBALS pWellKnownGlobalsOut)
    {
        AddCall("getWasmWellKnownGlobals");
        mc.repGetWasmWellKnownGlobals(ref pWellKnownGlobalsOut);
    }

    private uint getThreadTLSIndex(ref void* ppIndirection)
    {
        AddCall("getThreadTLSIndex");
        return mc.repGetThreadTLSIndex(ref ppIndirection);
    }

    private int* getAddrOfCaptureThreadGlobal(ref void* ppIndirection)
    {
        AddCall("getAddrOfCaptureThreadGlobal");
        return mc.repGetAddrOfCaptureThreadGlobal(ref ppIndirection);
    }

    private void getHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP* pNativeEntrypoint, CORINFO_METHOD_STRUCT_** pMethod)
    {
        AddCall("getHelperFtn");
        mc.repGetHelperFtn(ftnNum, pNativeEntrypoint, pMethod);
    }

    private void getFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
    {
        AddCall("getFunctionEntryPoint");
        mc.repGetFunctionEntryPoint(ftn, ref pResult, accessFlags);
    }

    private void getFunctionFixedEntryPoint(CORINFO_METHOD_STRUCT_* ftn, bool isUnsafeFunctionPointer, ref CORINFO_CONST_LOOKUP pResult)
    {
        AddCall("getFunctionFixedEntryPoint");
        mc.repGetFunctionFixedEntryPoint(ftn, isUnsafeFunctionPointer, ref pResult);
    }

    private CORINFO_MODULE_STRUCT_* embedModuleHandle(CORINFO_MODULE_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedModuleHandle");
        return mc.repEmbedModuleHandle(handle, ref ppIndirection);
    }

    private CORINFO_CLASS_STRUCT_* embedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedClassHandle");
        return mc.repEmbedClassHandle(handle, ref ppIndirection);
    }

    private CORINFO_METHOD_STRUCT_* embedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedMethodHandle");
        return mc.repEmbedMethodHandle(handle, ref ppIndirection);
    }

    private CORINFO_FIELD_STRUCT_* embedFieldHandle(CORINFO_FIELD_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedFieldHandle");
        return mc.repEmbedFieldHandle(handle, ref ppIndirection);
    }

    private void embedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        AddCall("embedGenericHandle");
        mc.repEmbedGenericHandle(ref pResolvedToken, fEmbedParent, callerHandle, ref pResult);
    }

    private void getLocationOfThisType(CORINFO_METHOD_STRUCT_* context, ref CORINFO_LOOKUP_KIND pLookupKind)
    {
        AddCall("getLocationOfThisType");
        mc.repGetLocationOfThisType(context, ref pLookupKind);
    }

    private void getAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
    {
        AddCall("getAddressOfPInvokeTarget");
        mc.repGetAddressOfPInvokeTarget(method, ref pLookup);
    }

    private void* GetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig)
    {
        AddCall("GetCookieForInterpreterCalliSig");
        return mc.repGetCookieForInterpreterCalliSig(szMetaSig);
    }

    private CORINFO_JUST_MY_CODE_HANDLE_* getJustMyCodeHandle(CORINFO_METHOD_STRUCT_* method, ref CORINFO_JUST_MY_CODE_HANDLE_* ppIndirection)
    {
        AddCall("getJustMyCodeHandle");
        return mc.repGetJustMyCodeHandle(method, ref ppIndirection);
    }

    private void GetProfilingHandle(ref bool pbHookFunction, ref void* pProfilerHandle, ref bool pbIndirectedHandles)
    {
        AddCall("GetProfilingHandle");
        mc.repGetProfilingHandle(ref pbHookFunction, ref pProfilerHandle, ref pbIndirectedHandles);
    }

    private void getCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle,
        CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult)
    {
        AddCall("getCallInfo");
        uint exceptionCode;
        mc.repGetCallInfo(ref pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult, out exceptionCode);
        if (exceptionCode != 0)
            ThrowRecordedException(exceptionCode);
    }

    private bool getStaticFieldContent(CORINFO_FIELD_STRUCT_* field, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
    {
        AddCall("getStaticFieldContent");
        return mc.repGetStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects);
    }

    private bool getObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset)
    {
        AddCall("getObjectContent");
        return mc.repGetObjectContent(obj, buffer, bufferSize, valueOffset);
    }

    private CORINFO_CLASS_STRUCT_* getStaticFieldCurrentClass(CORINFO_FIELD_STRUCT_* field, byte* pIsSpeculative)
    {
        AddCall("getStaticFieldCurrentClass");
        return mc.repGetStaticFieldCurrentClass(field, pIsSpeculative);
    }

    private IntPtr getVarArgsHandle(CORINFO_SIG_INFO* pSig, CORINFO_METHOD_STRUCT_* methHnd, ref void* ppIndirection)
    {
        AddCall("getVarArgsHandle");
        return mc.repGetVarArgsHandle(pSig, methHnd, ref ppIndirection);
    }

    private InfoAccessType constructStringLiteral(CORINFO_MODULE_STRUCT_* module, mdToken metaTok, ref void* ppValue)
    {
        AddCall("constructStringLiteral");
        return mc.repConstructStringLiteral(module, metaTok, ref ppValue);
    }

    private InfoAccessType emptyStringLiteral(ref void* ppValue)
    {
        AddCall("emptyStringLiteral");
        return mc.repEmptyStringLiteral(ref ppValue);
    }

    private uint getFieldThreadLocalStoreID(CORINFO_FIELD_STRUCT_* field, ref void* ppIndirection)
    {
        AddCall("getFieldThreadLocalStoreID");
        return mc.repGetFieldThreadLocalStoreID(field, ref ppIndirection);
    }

    private CORINFO_METHOD_STRUCT_* GetDelegateCtor(CORINFO_METHOD_STRUCT_* methHnd, CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_METHOD_STRUCT_* targetMethodHnd, ref DelegateCtorArgs pCtorData)
    {
        AddCall("GetDelegateCtor");
        return mc.repGetDelegateCtor(methHnd, clsHnd, targetMethodHnd, ref pCtorData);
    }

    private void MethodCompileComplete(CORINFO_METHOD_STRUCT_* methHnd)
    {
        AddCall("MethodCompileComplete");
        Logger.LogError("Hit unimplemented MethodCompileComplete");
        SpmiUtil.DebugBreakorAV(118);
    }

    private bool getTailCallHelpers(ref CORINFO_RESOLVED_TOKEN callToken, CORINFO_SIG_INFO* sig, CORINFO_GET_TAILCALL_HELPERS_FLAGS flags, ref CORINFO_TAILCALL_HELPERS pResult)
    {
        AddCall("getTailCallHelpers");
        return mc.repGetTailCallHelpers(ref callToken, sig, flags, ref pResult);
    }

    private CORINFO_CLASS_STRUCT_* getContinuationType(nuint dataSize, ref bool objRefs, nuint objRefsSize)
    {
        AddCall("getContinuationType");
        return mc.repGetContinuationType(dataSize, ref objRefs, objRefsSize);
    }

    private CORINFO_METHOD_STRUCT_* getAsyncResumptionStub(ref void* entryPoint)
    {
        AddCall("getAsyncResumptionStub");
        return mc.repGetAsyncResumptionStub(ref entryPoint);
    }

    private bool convertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool mustConvert)
    {
        AddCall("convertPInvokeCalliToCall");
        return mc.repConvertPInvokeCalliToCall(ref pResolvedToken, mustConvert);
    }

    private bool notifyInstructionSetUsage(InstructionSet instructionSet, bool supportEnabled, bool preserveNegativeDependency)
    {
        AddCall("notifyInstructionSetUsage");
        return mc.repNotifyInstructionSetUsage(instructionSet, supportEnabled, preserveNegativeDependency);
    }

    private void updateEntryPointForTailCall(ref CORINFO_CONST_LOOKUP entryPoint)
    {
        AddCall("updateEntryPointForTailCall");
        mc.repUpdateEntryPointForTailCall(ref entryPoint);
    }

    // Allocates the code and data blocks: the hot code aligned as requested, and all the RO data in one block.
    private void allocMem(ref AllocMemArgs pArgs)
    {
        AddCall("allocMem");

        byte* hotCodeBlock = null, coldCodeBlock = null, roDataBlock = null;
        nuint hotCodeSize = 0, coldCodeSize = 0, roDataSize = 0;
        uint roDataAlignment = 0;
        const CorJitAllocMemFlag CodeFlags = CorJitAllocMemFlag.CORJIT_ALLOCMEM_HOT_CODE | CorJitAllocMemFlag.CORJIT_ALLOCMEM_COLD_CODE;

        for (uint i = 0; i < pArgs.chunksCount; i++)
        {
            ref AllocMemChunk chunk = ref pArgs.chunks[i];
            if ((chunk.flags & CorJitAllocMemFlag.CORJIT_ALLOCMEM_HOT_CODE) != 0)
            {
                hotCodeSize = chunk.size;
                uint codeAlignment = Math.Max((uint)sizeof(void*), chunk.alignment);
                hotCodeBlock = (byte*)AlignUp((nuint)cr.allocateMemory(chunk.size + codeAlignment - 1), codeAlignment);
                chunk.block = chunk.blockRW = hotCodeBlock;
            }
            else if ((chunk.flags & CorJitAllocMemFlag.CORJIT_ALLOCMEM_COLD_CODE) != 0)
            {
                coldCodeSize = chunk.size;
                coldCodeBlock = (byte*)cr.allocateMemory(coldCodeSize);
                chunk.block = chunk.blockRW = coldCodeBlock;
            }
            else
            {
                roDataAlignment = Math.Max(roDataAlignment, chunk.alignment);
                roDataSize = AlignUp(roDataSize, chunk.alignment) + chunk.size;
            }
        }

        if (roDataSize > 0)
        {
            // The block is zeroed, so all the padding compares equal later.
            roDataBlock = (byte*)AlignUp((nuint)cr.allocateMemory(roDataSize + roDataAlignment - 1), roDataAlignment);
            nuint offset = 0;
            for (uint i = 0; i < pArgs.chunksCount; i++)
            {
                ref AllocMemChunk chunk = ref pArgs.chunks[i];
                if ((chunk.flags & CodeFlags) != 0)
                    continue;

                offset = AlignUp(offset, chunk.alignment);
                chunk.block = chunk.blockRW = roDataBlock + offset;
                offset += chunk.size;
            }
        }

        cr.recAllocMem(hotCodeSize, coldCodeSize, roDataSize, pArgs.xcptnsCount, hotCodeBlock, coldCodeBlock, roDataBlock);
    }

    private void reserveUnwindInfo(bool isFunclet, bool isColdCode, uint unwindSize)
    {
        AddCall("reserveUnwindInfo");
        cr.recReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    }

    private void allocUnwindInfo(byte* pHotCode, byte* pColdCode, uint startOffset, uint endOffset, uint unwindSize, byte* pUnwindBlock, CorJitFuncKind funcKind)
    {
        AddCall("allocUnwindInfo");
        cr.recAllocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
    }

    private void* allocGCInfo(nuint size)
    {
        AddCall("allocGCInfo");
        void* temp = cr.allocateMemory(size);
        cr.recAllocGCInfo(size, temp);
        return temp;
    }

    private void setEHcount(uint cEH)
    {
        AddCall("setEHcount");
        cr.recSetEHcount(cEH);
    }

    private void setEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        AddCall("setEHinfo");
        cr.recSetEHinfo(EHnumber, ref clause);
    }

    private bool logMsg(uint level, byte* fmt, IntPtr args)
    {
        AddCall("logMsg");
        return false;
    }

    // Returns whether the code should retry (DebugBreak), or ignore the assert.
    private int doAssert(byte* szFile, int iLine, byte* szExpr)
    {
        AddCall("doAssert");
        string file = Marshal.PtrToStringUTF8((IntPtr)szFile);
        string expr = Marshal.PtrToStringUTF8((IntPtr)szExpr);
        string buff = $"{file} ({iLine}) - {expr}";

        Logger.LogIssue($"<ASSERT> #{mc.index} {buff}");
        cr.recMessageLog(buff);

        // Under "-boa", the assert is reported as a failure, without launching the JIT debugger.
        if (SpmiUtil.BreakOnDebugBreakorAV)
        {
            Logger.LogError($"SuperPMI: Assert Failure (PID {Environment.ProcessId}, Thread {Environment.CurrentManagedThreadId}/{Environment.CurrentManagedThreadId:x})\n" +
                $"{expr}\n\n{file}, Line: {iLine}\n");
        }

        SpmiUtil.DebugBreakorAV(0x7b);
        return 0;
    }

    private void reportFatalError(CorJitResult result)
    {
        AddCall("reportFatalError");
        cr.recReportFatalError(result);
    }

    private HRESULT getPgoInstrumentationResults(CORINFO_METHOD_STRUCT_* ftnHnd, ref PgoInstrumentationSchema* pSchema, ref uint pCountSchemaItems,
        byte** pInstrumentationData, ref PgoSource pPgoSource, ref bool pDynamicPgo)
    {
        AddCall("getPgoInstrumentationResults");
        return mc.repGetPgoInstrumentationResults(ftnHnd, ref pSchema, ref pCountSchemaItems, pInstrumentationData, ref pPgoSource, ref pDynamicPgo);
    }

    private HRESULT allocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData)
    {
        AddCall("allocPgoInstrumentationBySchema");
        return mc.repAllocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData);
    }

    private void recordCallSite(uint instrOffset, CORINFO_SIG_INFO* callSig, CORINFO_METHOD_STRUCT_* methodHandle)
    {
        AddCall("recordCallSite");
        cr.repRecordCallSite(instrOffset, callSig, methodHandle);
    }

    // Only meaningful for ReadyToRun Wasm compilation.
    private void recordWasmManagedCallSig(CORINFO_SIG_INFO* callSig) => AddCall("recordWasmManagedCallSig");

    // Only meaningful for ReadyToRun Wasm compilation.
    private void recordWasmVirtualFunctionPointerLookup(CORINFO_METHOD_STRUCT_* method) => AddCall("recordWasmVirtualFunctionPointerLookup");

    private void recordRelocation(void* location, void* locationRW, void* target, CorInfoReloc fRelocType, int addlDelta)
    {
        AddCall("recordRelocation");
        cr.repRecordRelocation(location, target, fRelocType, addlDelta);
    }

    private CorInfoReloc getRelocTypeHint(void* target)
    {
        AddCall("getRelocTypeHint");
        return mc.repGetRelocTypeHint(target);
    }

    private uint getExpectedTargetArchitecture()
    {
        AddCall("getExpectedTargetArchitecture");
        return mc.repGetExpectedTargetArchitecture();
    }

    private uint getJitFlags(ref CORJIT_FLAGS flags, uint sizeInBytes)
    {
        AddCall("getJitFlags");
        return jitInstance.getJitFlags(ref flags, sizeInBytes);
    }

    private CORINFO_WASM_TYPE_SYMBOL_STRUCT_* getWasmTypeSymbol(CorInfoWasmType* types, nuint typesSize)
    {
        AddCall("getWasmTypeSymbol");
        return mc.repGetWasmTypeSymbol(types, typesSize);
    }

    private CORINFO_METHOD_STRUCT_* getSpecialCopyHelper(CORINFO_CLASS_STRUCT_* type)
    {
        AddCall("getSpecialCopyHelper");
        return mc.repGetSpecialCopyHelper(type);
    }
}
