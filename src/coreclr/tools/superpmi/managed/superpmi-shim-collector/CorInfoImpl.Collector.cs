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

// The JIT-EE interface for collection: forwards the JIT's calls to the EE, and records them (with what the EE
// answered) into the method context.
internal sealed unsafe partial class CorInfoImpl
{
    private readonly CollectorJitCompiler m_compiler;
    private readonly OriginalJitInfo _original;
    private readonly MethodContext mc;
    private bool m_savedCollectionEarly;

    public CorInfoImpl(CollectorJitCompiler compiler, OriginalJitInfo original, MethodContext mc)
    {
        m_compiler = compiler;
        _original = original;
        this.mc = mc;
    }

    private CompileResult cr => mc.cr;

    public bool SavedCollectionEarly => m_savedCollectionEarly;

    public MethodContext MethodContext => mc;

    // The handle that RecordingJitInfo calls back with, for the duration of the compilation.
    private static CorInfoImpl GetThis(IntPtr thisHandle) => (CorInfoImpl)GCHandle.FromIntPtr(thisHandle).Target;

    public static IntPtr UnmanagedCallbacks => GetUnmanagedCallbacks();

    private IntPtr AllocException(Exception ex) => ShimInterop.ToNativeException(ex);

    private void AddCall(string name) => cr.AddCall(name);

    // An exception filter that records the code of the exception and lets it continue, so that the call is
    // recorded with it (and replayed by throwing).
    private static bool CaptureExceptionCode(Exception e, ref uint exceptionCode)
    {
        exceptionCode = e switch
        {
            EEException ee => ee.Code,
            SpmiException se => se.Code,
            _ => 1,
        };
        return false;
    }

    // Forces some extra data into the method context ("fat" method contexts), which ildump needs.
    public void RecordExtraData(CORINFO_METHOD_INFO* info)
    {
        getBuiltinClass(CorInfoClassId.CLASSID_SYSTEM_OBJECT);
        getBuiltinClass(CorInfoClassId.CLASSID_TYPED_BYREF);
        getBuiltinClass(CorInfoClassId.CLASSID_TYPE_HANDLE);
        getBuiltinClass(CorInfoClassId.CLASSID_FIELD_HANDLE);
        getBuiltinClass(CorInfoClassId.CLASSID_METHOD_HANDLE);
        getBuiltinClass(CorInfoClassId.CLASSID_STRING);
        getBuiltinClass(CorInfoClassId.CLASSID_RUNTIME_TYPE);

        CORINFO_CLASS_STRUCT_* ourClass = getMethodClass(info->ftn);
        getClassAttribs(ourClass);
        isValueClass(ourClass);
        asCorInfoType(ourClass);

        PrintInFull((buffer, bufferSize, requiredBufferSize) => printClassName(ourClass, buffer, bufferSize, requiredBufferSize));
        PrintInFull((buffer, bufferSize, requiredBufferSize) => printMethodName(info->ftn, buffer, bufferSize, requiredBufferSize));
    }

    private delegate nuint Printer(byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize);

    private static void PrintInFull(Printer print)
    {
        byte* buffer = stackalloc byte[256];
        nuint requiredSize;
        print(buffer, 256, &requiredSize);

        if (requiredSize > 256)
        {
            fixed (byte* fullBuffer = new byte[requiredSize])
                print(fullBuffer, requiredSize, null);
        }
    }

    private bool getMethodInfo(CORINFO_METHOD_STRUCT_* ftn, CORINFO_METHOD_INFO* info, CORINFO_CONTEXT_STRUCT* context)
    {
        bool temp = false;
        uint exceptionCode = 0;
        try
        {
            AddCall("getMethodInfo");
            temp = _original.getMethodInfo(ftn, info, context);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recGetMethodInfo(ftn, info, context, temp, exceptionCode);
        }
        return temp;
    }

    private CorInfoInline canInline(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd)
    {
        CorInfoInline temp = CorInfoInline.INLINE_NEVER;
        uint exceptionCode = 0;
        try
        {
            AddCall("canInline");
            temp = _original.canInline(callerHnd, calleeHnd);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recCanInline(callerHnd, calleeHnd, temp, exceptionCode);
        }
        return temp;
    }

    private void resolveToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        uint exceptionCode = 0;
        try
        {
            AddCall("resolveToken");
            _original.resolveToken(ref pResolvedToken);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recResolveToken(ref pResolvedToken, exceptionCode);
        }
    }

    private bool canCast(CORINFO_CLASS_STRUCT_* child, CORINFO_CLASS_STRUCT_* parent)
    {
        AddCall("canCast");
        bool temp = _original.canCast(child, parent);
        mc.recCanCast(child, parent, temp);
        return temp;
    }

    private uint getClassGClayout(CORINFO_CLASS_STRUCT_* cls, byte* gcPtrs)
    {
        AddCall("getClassGClayout");
        uint temp = _original.getClassGClayout(cls, gcPtrs);

        // Use the target's pointer size, not the host's, so cross-target collections
        // (e.g., wasm32 on an x64 host) record the full GC-layout buffer.
        uint targetPointerSize = (CorInfoArch)_original.getExpectedTargetArchitecture() switch
        {
            CorInfoArch.CORINFO_ARCH_X86 or CorInfoArch.CORINFO_ARCH_ARM or CorInfoArch.CORINFO_ARCH_WASM32 => 4,
            _ => 8,
        };

        uint len = isValueClass(cls)
            ? (getClassSize(cls) + targetPointerSize - 1) / targetPointerSize
            : (getHeapClassSize(cls) + targetPointerSize - 1) / targetPointerSize;
        mc.recGetClassGClayout(cls, gcPtrs, len, temp);
        return temp;
    }

    // RecordingJitInfo passes the out parameters of getNewHelper, isEnum and getHelperFtn even where the JIT doesn't.
    private CorInfoHelpFunc getNewHelper(CORINFO_CLASS_STRUCT_* classHandle, ref bool pHasSideEffects)
    {
        CorInfoHelpFunc result = CorInfoHelpFunc.CORINFO_HELP_UNDEF;
        uint exceptionCode = 0;
        try
        {
            AddCall("getNewHelper");
            result = _original.getNewHelper(classHandle, ref pHasSideEffects);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recGetNewHelper(classHandle, pHasSideEffects, result, exceptionCode);
        }
        return result;
    }

    private TypeCompareState isEnum(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_** underlyingType)
    {
        AddCall("isEnum");
        TypeCompareState temp = _original.isEnum(cls, underlyingType);
        mc.recIsEnum(cls, *underlyingType, temp);
        return temp;
    }

    private CorInfoTypeWithMod getArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_** vcTypeRet)
    {
        CorInfoTypeWithMod temp = (CorInfoTypeWithMod)CorInfoType.CORINFO_TYPE_UNDEF;
        uint exceptionCode = 0;
        try
        {
            AddCall("getArgType");
            temp = _original.getArgType(sig, args, vcTypeRet);

            // For a "fat" method context.
            getArgClass(sig, args);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recGetArgType(sig, args, vcTypeRet, temp, exceptionCode);
        }
        return temp;
    }

    private int getExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet)
    {
        AddCall("getExactClasses");
        int result = _original.getExactClasses(baseType, maxExactClasses, exactClsRet);
        mc.recGetExactClasses(baseType, maxExactClasses, exactClsRet, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args)
    {
        CORINFO_CLASS_STRUCT_* temp = null;
        uint exceptionCode = 0;
        try
        {
            AddCall("getArgClass");
            temp = _original.getArgClass(sig, args);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recGetArgClass(sig, args, temp, exceptionCode);
        }
        return temp;
    }

    private void getHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP* pNativeEntrypoint, CORINFO_METHOD_STRUCT_** pMethod)
    {
        AddCall("getHelperFtn");
        _original.getHelperFtn(ftnNum, pNativeEntrypoint, pMethod);
        mc.recGetHelperFtn(ftnNum, *pNativeEntrypoint, *pMethod);
    }

    private void getCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle,
        CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult)
    {
        uint exceptionCode = 0;
        try
        {
            AddCall("getCallInfo");
            _original.getCallInfo(ref pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult);
        }
        catch (Exception e) when (CaptureExceptionCode(e, ref exceptionCode))
        {
        }
        finally
        {
            mc.recGetCallInfo(ref pResolvedToken, pConstrainedResolvedToken, callerHandle, flags, pResult, exceptionCode);
        }
    }

    private bool getTailCallHelpers(ref CORINFO_RESOLVED_TOKEN callToken, CORINFO_SIG_INFO* sig, CORINFO_GET_TAILCALL_HELPERS_FLAGS flags, ref CORINFO_TAILCALL_HELPERS pResult)
    {
        AddCall("getTailCallHelpers");
        bool result = _original.getTailCallHelpers(ref callToken, sig, flags, ref pResult);
        mc.recGetTailCallHelpers(ref callToken, sig, flags, result, ref pResult);
        return result;
    }

    // RecordingJitInfo records updateEntryPointForTailCall with RecordUpdateEntryPointForTailCallCallback instead.
    private void updateEntryPointForTailCall(ref CORINFO_CONST_LOOKUP entryPoint) => throw new UnreachableException();

    // The callback that RecordingJitInfo records updateEntryPointForTailCall with, which also gets the entry point
    // from before the call.
    [UnmanagedCallersOnly]
    public static void RecordUpdateEntryPointForTailCallCallback(IntPtr thisHandle, IntPtr* ppException, CORINFO_CONST_LOOKUP* origEntryPoint,
        CORINFO_CONST_LOOKUP* entryPoint)
    {
        CorInfoImpl _this = GetThis(thisHandle);
        try
        {
            _this.recordUpdateEntryPointForTailCall(*origEntryPoint, ref *entryPoint);
        }
        catch (Exception ex)
        {
            *ppException = _this.AllocException(ex);
        }
    }

    private void recordUpdateEntryPointForTailCall(CORINFO_CONST_LOOKUP origEntryPoint, ref CORINFO_CONST_LOOKUP entryPoint)
    {
        AddCall("updateEntryPointForTailCall");
        // Takes the pending call, without calling the EE.
        _original.updateEntryPointForTailCall(ref entryPoint);
        mc.recUpdateEntryPointForTailCall(origEntryPoint, entryPoint);
    }

    // Allocates memory that is handed to the EE; not recorded.
    private void* allocateArray(nuint cBytes)
    {
        AddCall("allocateArray");
        return _original.allocateArray(cBytes);
    }

    private void freeArray(void* array)
    {
        AddCall("freeArray");
        _original.freeArray(array);
    }

    private void recordWasmManagedCallSig(CORINFO_SIG_INFO* callSig)
    {
        AddCall("recordWasmManagedCallSig");
        _original.recordWasmManagedCallSig(callSig);
        // No-op in the VM, nothing to record for replay.
    }

    private void recordWasmVirtualFunctionPointerLookup(CORINFO_METHOD_STRUCT_* method)
    {
        AddCall("recordWasmVirtualFunctionPointerLookup");
        _original.recordWasmVirtualFunctionPointerLookup(method);
        // No-op in the VM, nothing to record for replay.
    }

    // The results of the error traps aren't recorded: when the call is replayed, its result depends on whether
    // 'function' calls something that throws at replay time rather than at recording time.
    private bool runWithErrorTrap(void* function, void* parameter)
    {
        AddCall("runWithErrorTrap");
        return _original.runWithErrorTrap(function, parameter);
    }

    // The error trap for the method context errors of the shim itself. The JIT's calls don't get here: RecordingJitInfo
    // implements it natively.
    private bool runWithSPMIErrorTrap(void* function, void* parameter)
    {
        IntPtr exception = 0;
        bool result = ShimInterop.Shim_RunWithSPMIErrorTrap(&exception, function, parameter, ExceptionCodes.MC) != 0;
        if (exception != 0)
            throw new EEException(exception);
        return result;
    }

    private int doAssert(byte* szFile, int iLine, byte* szExpr)
    {
        AddCall("doAssert");

        m_compiler.finalizeAndCommitCollection(mc, CorJitResult.CORJIT_INTERNALERROR, null, 0);
        // The following assert may not always fail fast, so make sure we do not
        // save the collection twice if it throws an unwindable exception.
        m_savedCollectionEarly = true;

        return _original.doAssert(szFile, iLine, szExpr);
    }

    private HRESULT getPgoInstrumentationResults(CORINFO_METHOD_STRUCT_* ftnHnd, ref PgoInstrumentationSchema* pSchema, ref uint pCountSchemaItems,
        byte** pInstrumentationData, ref PgoSource pPgoSource, ref bool pDynamicPgo)
    {
        AddCall("getPgoInstrumentationResults");
        HRESULT temp = _original.getPgoInstrumentationResults(ftnHnd, ref pSchema, ref pCountSchemaItems, pInstrumentationData, ref pPgoSource, ref pDynamicPgo);
        mc.recGetPgoInstrumentationResults(ftnHnd, ref pSchema, ref pCountSchemaItems, pInstrumentationData, ref pPgoSource, ref pDynamicPgo, (int)temp);
        return temp;
    }

    private bool isIntrinsic(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("isIntrinsic");
        var temp = _original.isIntrinsic(ftn);
        mc.recIsIntrinsic(ftn, temp);
        return temp;
    }

    private bool canValueClassInstancePointerEscape(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("canValueClassInstancePointerEscape");
        var temp = _original.canValueClassInstancePointerEscape(ftn);
        mc.recCanValueClassInstancePointerEscape(ftn, temp);
        return temp;
    }

    private bool notifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("notifyMethodInfoUsage");
        var temp = _original.notifyMethodInfoUsage(ftn);
        mc.recNotifyMethodInfoUsage(ftn, temp);
        return temp;
    }

    private uint getMethodAttribs(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getMethodAttribs");
        var temp = _original.getMethodAttribs(ftn);
        mc.recGetMethodAttribs(ftn, temp);
        return temp;
    }

    private void setMethodAttribs(CORINFO_METHOD_STRUCT_* ftn, CorInfoMethodRuntimeFlags attribs)
    {
        AddCall("setMethodAttribs");
        _original.setMethodAttribs(ftn, attribs);
        cr.recSetMethodAttribs(ftn, attribs);
    }

    private void getMethodSig(CORINFO_METHOD_STRUCT_* ftn, CORINFO_SIG_INFO* sig, CORINFO_CLASS_STRUCT_* memberParent)
    {
        AddCall("getMethodSig");
        _original.getMethodSig(ftn, sig, memberParent);
        mc.recGetMethodSig(ftn, sig, memberParent);
    }

    private bool haveSameMethodDefinition(CORINFO_METHOD_STRUCT_* methHnd1, CORINFO_METHOD_STRUCT_* methHnd2)
    {
        var result = _original.haveSameMethodDefinition(methHnd1, methHnd2);
        mc.recHaveSameMethodDefinition(methHnd1, methHnd2, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getTypeDefinition(CORINFO_CLASS_STRUCT_* type)
    {
        var result = _original.getTypeDefinition(type);
        mc.recGetTypeDefinition(type, result);
        return result;
    }

    private void beginInlining(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd)
    {
        AddCall("beginInlining");
        _original.beginInlining(inlinerHnd, inlineeHnd);
    }

    private void reportInliningDecision(CORINFO_METHOD_STRUCT_* inlinerHnd, CORINFO_METHOD_STRUCT_* inlineeHnd, CorInfoInline inlineResult, byte* reason)
    {
        AddCall("reportInliningDecision");
        _original.reportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
        cr.recReportInliningDecision(inlinerHnd, inlineeHnd, inlineResult, reason);
    }

    private bool canTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix)
    {
        AddCall("canTailCall");
        var temp = _original.canTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix);
        mc.recCanTailCall(callerHnd, declaredCalleeHnd, exactCalleeHnd, fIsTailPrefix, temp);
        return temp;
    }

    private void reportTailCallDecision(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, bool fIsTailPrefix, CorInfoTailCall tailCallResult, byte* reason)
    {
        AddCall("reportTailCallDecision");
        _original.reportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
        cr.recReportTailCallDecision(callerHnd, calleeHnd, fIsTailPrefix, tailCallResult, reason);
    }

    private void getEHinfo(CORINFO_METHOD_STRUCT_* ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        AddCall("getEHinfo");
        _original.getEHinfo(ftn, EHnumber, ref clause);
        mc.recGetEHinfo(ftn, EHnumber, ref clause);
    }

    private CORINFO_CLASS_STRUCT_* getMethodClass(CORINFO_METHOD_STRUCT_* method)
    {
        AddCall("getMethodClass");
        var temp = _original.getMethodClass(method);
        mc.recGetMethodClass(method, temp);
        return temp;
    }

    private void getMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
    {
        AddCall("getMethodVTableOffset");
        _original.getMethodVTableOffset(method, ref offsetOfIndirection, ref offsetAfterIndirection, ref isRelative);
        mc.recGetMethodVTableOffset(method, ref offsetOfIndirection, ref offsetAfterIndirection, ref isRelative);
    }

    private bool resolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO* info)
    {
        AddCall("resolveVirtualMethod");
        var result = _original.resolveVirtualMethod(info);
        mc.recResolveVirtualMethod(info, result);
        return result;
    }

    private CORINFO_METHOD_STRUCT_* getAsyncOtherVariant(CORINFO_METHOD_STRUCT_* ftn, ref bool variantIsThunk)
    {
        AddCall("getAsyncOtherVariant");
        var result = _original.getAsyncOtherVariant(ftn, ref variantIsThunk);
        mc.recGetAsyncOtherVariant(ftn, variantIsThunk, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getDefaultComparerClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getDefaultComparerClass");
        var result = _original.getDefaultComparerClass(cls);
        mc.recGetDefaultComparerClass(cls, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getDefaultEqualityComparerClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getDefaultEqualityComparerClass");
        var result = _original.getDefaultEqualityComparerClass(cls);
        mc.recGetDefaultEqualityComparerClass(cls, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getSZArrayHelperEnumeratorClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getSZArrayHelperEnumeratorClass");
        var result = _original.getSZArrayHelperEnumeratorClass(cls);
        mc.recGetSZArrayHelperEnumeratorClass(cls, result);
        return result;
    }

    private void expandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        AddCall("expandRawHandleIntrinsic");
        _original.expandRawHandleIntrinsic(ref pResolvedToken, callerHandle, ref pResult);
        mc.recExpandRawHandleIntrinsic(ref pResolvedToken, callerHandle, ref pResult);
    }

    private bool isIntrinsicType(CORINFO_CLASS_STRUCT_* classHnd)
    {
        AddCall("isIntrinsicType");
        var temp = _original.isIntrinsicType(classHnd);
        mc.recIsIntrinsicType(classHnd, temp);
        return temp;
    }

    private CorInfoCallConvExtension getUnmanagedCallConv(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, ref bool pSuppressGCTransition)
    {
        AddCall("getUnmanagedCallConv");
        var temp = _original.getUnmanagedCallConv(method, callSiteSig, ref pSuppressGCTransition);
        mc.recGetUnmanagedCallConv(method, callSiteSig, temp, pSuppressGCTransition);
        return temp;
    }

    private bool pInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig)
    {
        AddCall("pInvokeMarshalingRequired");
        var temp = _original.pInvokeMarshalingRequired(method, callSiteSig);
        mc.recPInvokeMarshalingRequired(method, callSiteSig, temp);
        return temp;
    }

    private bool satisfiesMethodConstraints(CORINFO_CLASS_STRUCT_* parent, CORINFO_METHOD_STRUCT_* method)
    {
        AddCall("satisfiesMethodConstraints");
        var temp = _original.satisfiesMethodConstraints(parent, method);
        mc.recSatisfiesMethodConstraints(parent, method, temp);
        return temp;
    }

    private void getGSCookie(IntPtr* pCookieVal, IntPtr** ppCookieVal)
    {
        AddCall("getGSCookie");
        _original.getGSCookie(pCookieVal, ppCookieVal);
        mc.recGetGSCookie(pCookieVal, ppCookieVal);
    }

    private void setPatchpointInfo(PatchpointInfo* patchpointInfo)
    {
        AddCall("setPatchpointInfo");
        cr.recSetPatchpointInfo(patchpointInfo);
        _original.setPatchpointInfo(patchpointInfo);
    }

    private PatchpointInfo* getOSRInfo(ref uint ilOffset)
    {
        AddCall("getOSRInfo");
        var patchpointInfo = _original.getOSRInfo(ref ilOffset);
        mc.recGetOSRInfo(patchpointInfo, ref ilOffset);
        return patchpointInfo;
    }

    private void findSig(CORINFO_MODULE_STRUCT_* module, uint sigTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        AddCall("findSig");
        _original.findSig(module, sigTOK, context, sig);
        mc.recFindSig(module, sigTOK, context, sig);
    }

    private void findCallSiteSig(CORINFO_MODULE_STRUCT_* module, uint methTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        AddCall("findCallSiteSig");
        _original.findCallSiteSig(module, methTOK, context, sig);
        mc.recFindCallSiteSig(module, methTOK, context, sig);
    }

    private CORINFO_CLASS_STRUCT_* getTokenTypeAsHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken)
    {
        AddCall("getTokenTypeAsHandle");
        var temp = _original.getTokenTypeAsHandle(ref pResolvedToken);
        mc.recGetTokenTypeAsHandle(ref pResolvedToken, temp);
        return temp;
    }

    private int getStringLiteral(CORINFO_MODULE_STRUCT_* module, uint metaTOK, char* buffer, int bufferSize, int startIndex)
    {
        AddCall("getStringLiteral");
        var temp = _original.getStringLiteral(module, metaTOK, buffer, bufferSize, startIndex);
        mc.recGetStringLiteral(module, metaTOK, buffer, bufferSize, startIndex, temp);
        return temp;
    }

    private nuint printObjectDescription(CORINFO_OBJECT_STRUCT_* handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printObjectDescription");
        var temp = _original.printObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize);
        mc.recPrintObjectDescription(handle, buffer, bufferSize, pRequiredBufferSize, temp);
        return temp;
    }

    private CorInfoType asCorInfoType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("asCorInfoType");
        var temp = _original.asCorInfoType(cls);
        mc.recAsCorInfoType(cls, temp);
        return temp;
    }

    private byte* getClassNameFromMetadata(CORINFO_CLASS_STRUCT_* cls, byte** namespaceName)
    {
        AddCall("getClassNameFromMetadata");
        var temp = _original.getClassNameFromMetadata(cls, namespaceName);
        mc.recGetClassNameFromMetadata(cls, temp, namespaceName);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getTypeInstantiationArgument(CORINFO_CLASS_STRUCT_* cls, uint index)
    {
        AddCall("getTypeInstantiationArgument");
        var result = _original.getTypeInstantiationArgument(cls, index);
        mc.recGetTypeInstantiationArgument(cls, index, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getMethodInstantiationArgument(CORINFO_METHOD_STRUCT_* ftn, uint index)
    {
        AddCall("getMethodInstantiationArgument");
        var result = _original.getMethodInstantiationArgument(ftn, index);
        mc.recGetMethodInstantiationArgument(ftn, index, result);
        return result;
    }

    private nuint printClassName(CORINFO_CLASS_STRUCT_* cls, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printClassName");
        var bytesWritten = _original.printClassName(cls, buffer, bufferSize, pRequiredBufferSize);
        mc.recPrintClassName(cls, buffer, bufferSize, pRequiredBufferSize, bytesWritten);
        return bytesWritten;
    }

    private bool isValueClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isValueClass");
        var temp = _original.isValueClass(cls);
        mc.recIsValueClass(cls, temp);
        return temp;
    }

    private bool isByRefLike(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isByRefLike");
        var result = _original.isByRefLike(cls);
        mc.recIsByRefLike(cls, result);
        return result;
    }

    private bool isSharedInst(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isSharedInst");
        var result = _original.isSharedInst(cls);
        mc.recIsSharedInst(cls, result);
        return result;
    }

    private uint getClassAttribs(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassAttribs");
        var temp = _original.getClassAttribs(cls);
        mc.recGetClassAttribs(cls, temp);
        return temp;
    }

    private byte* getClassAssemblyName(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassAssemblyName");
        var temp = _original.getClassAssemblyName(cls);
        mc.recGetClassAssemblyName(cls, temp);
        return temp;
    }

    private void* LongLifetimeMalloc(nuint sz)
    {
        AddCall("LongLifetimeMalloc");
        return _original.LongLifetimeMalloc(sz);
    }

    private void LongLifetimeFree(void* obj)
    {
        AddCall("LongLifetimeFree");
        _original.LongLifetimeFree(obj);
    }

    private bool getIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset)
    {
        AddCall("getIsClassInitedFlagAddress");
        var temp = _original.getIsClassInitedFlagAddress(cls, ref addr, ref offset);
        mc.recGetIsClassInitedFlagAddress(cls, ref addr, ref offset, temp);
        return temp;
    }

    private void* getClassThreadStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassThreadStaticDynamicInfo");
        var temp = _original.getClassThreadStaticDynamicInfo(cls);
        mc.recGetClassThreadStaticDynamicInfo(cls, temp);
        return temp;
    }

    private void* getClassStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassStaticDynamicInfo");
        var temp = _original.getClassStaticDynamicInfo(cls);
        mc.recGetClassStaticDynamicInfo(cls, temp);
        return temp;
    }

    private bool getStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr)
    {
        AddCall("getStaticBaseAddress");
        var temp = _original.getStaticBaseAddress(cls, isGc, ref addr);
        mc.recGetStaticBaseAddress(cls, isGc, ref addr, temp);
        return temp;
    }

    private uint getClassSize(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassSize");
        var temp = _original.getClassSize(cls);
        mc.recGetClassSize(cls, temp);
        return temp;
    }

    private uint getHeapClassSize(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getHeapClassSize");
        var temp = _original.getHeapClassSize(cls);
        mc.recGetHeapClassSize(cls, temp);
        return temp;
    }

    private bool canAllocateOnStack(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("canAllocateOnStack");
        var temp = _original.canAllocateOnStack(cls);
        mc.recCanAllocateOnStack(cls, temp);
        return temp;
    }

    private uint getClassAlignmentRequirement(CORINFO_CLASS_STRUCT_* cls, bool fDoubleAlignHint)
    {
        AddCall("getClassAlignmentRequirement");
        var temp = _original.getClassAlignmentRequirement(cls, fDoubleAlignHint);
        mc.recGetClassAlignmentRequirement(cls, fDoubleAlignHint, temp);
        return temp;
    }

    private uint getClassNumInstanceFields(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getClassNumInstanceFields");
        var temp = _original.getClassNumInstanceFields(cls);
        mc.recGetClassNumInstanceFields(cls, temp);
        return temp;
    }

    private CORINFO_FIELD_STRUCT_* getFieldInClass(CORINFO_CLASS_STRUCT_* clsHnd, int num)
    {
        AddCall("getFieldInClass");
        var temp = _original.getFieldInClass(clsHnd, num);
        mc.recGetFieldInClass(clsHnd, num, temp);
        return temp;
    }

    private GetTypeLayoutResult getTypeLayout(CORINFO_CLASS_STRUCT_* typeHnd, CORINFO_TYPE_LAYOUT_NODE* nodes, nuint* numNodes)
    {
        AddCall("getTypeLayout");
        var result = _original.getTypeLayout(typeHnd, nodes, numNodes);
        mc.recGetTypeLayout(result, typeHnd, nodes, *numNodes);
        return result;
    }

    private bool checkMethodModifier(CORINFO_METHOD_STRUCT_* hMethod, byte* modifier, bool fOptional)
    {
        AddCall("checkMethodModifier");
        var result = _original.checkMethodModifier(hMethod, modifier, fOptional);
        mc.recCheckMethodModifier(hMethod, modifier, fOptional, result);
        return result;
    }

    private CorInfoHelpFunc getNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls)
    {
        AddCall("getNewArrHelper");
        var temp = _original.getNewArrHelper(arrayCls);
        mc.recGetNewArrHelper(arrayCls, temp);
        return temp;
    }

    private CorInfoHelpFunc getCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing)
    {
        AddCall("getCastingHelper");
        var temp = _original.getCastingHelper(ref pResolvedToken, fThrowing);
        mc.recGetCastingHelper(ref pResolvedToken, fThrowing, temp);
        return temp;
    }

    private CorInfoHelpFunc getSharedCCtorHelper(CORINFO_CLASS_STRUCT_* clsHnd)
    {
        AddCall("getSharedCCtorHelper");
        var temp = _original.getSharedCCtorHelper(clsHnd);
        mc.recGetSharedCCtorHelper(clsHnd, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getTypeForBox(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForBox");
        var temp = _original.getTypeForBox(cls);
        mc.recGetTypeForBox(cls, temp);
        return temp;
    }

    private CorInfoHelpFunc getBoxHelper(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getBoxHelper");
        var temp = _original.getBoxHelper(cls);
        mc.recGetBoxHelper(cls, temp);
        return temp;
    }

    private CorInfoHelpFunc getUnBoxHelper(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getUnBoxHelper");
        var temp = _original.getUnBoxHelper(cls);
        mc.recGetUnBoxHelper(cls, temp);
        return temp;
    }

    private CORINFO_OBJECT_STRUCT_* getRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getRuntimeTypePointer");
        var temp = _original.getRuntimeTypePointer(cls);
        mc.recGetRuntimeTypePointer(cls, temp);
        return temp;
    }

    private bool isObjectImmutable(CORINFO_OBJECT_STRUCT_* typeObj)
    {
        AddCall("isObjectImmutable");
        var temp = _original.isObjectImmutable(typeObj);
        mc.recIsObjectImmutable(typeObj, temp);
        return temp;
    }

    private bool getStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* value)
    {
        AddCall("getStringChar");
        var temp = _original.getStringChar(strObj, index, value);
        mc.recGetStringChar(strObj, index, value, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getObjectType(CORINFO_OBJECT_STRUCT_* typeObj)
    {
        AddCall("getObjectType");
        var temp = _original.getObjectType(typeObj);
        mc.recGetObjectType(typeObj, temp);
        return temp;
    }

    private bool getReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CorInfoHelpFunc id, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_CONST_LOOKUP pLookup)
    {
        AddCall("getReadyToRunHelper");
        var result = _original.getReadyToRunHelper(ref pResolvedToken, id, callerHandle, ref pLookup);
        mc.recGetReadyToRunHelper(ref pResolvedToken, id, callerHandle, ref pLookup, result);
        return result;
    }

    private void getReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, mdToken targetConstraint, CORINFO_CLASS_STRUCT_* delegateType, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_LOOKUP pLookup)
    {
        AddCall("getReadyToRunDelegateCtorHelper");
        _original.getReadyToRunDelegateCtorHelper(ref pTargetMethod, targetConstraint, delegateType, callerHandle, ref pLookup);
        mc.recGetReadyToRunDelegateCtorHelper(ref pTargetMethod, (uint)targetConstraint, delegateType, callerHandle, ref pLookup);
    }

    private CorInfoInitClassResult initClass(CORINFO_FIELD_STRUCT_* field, CORINFO_METHOD_STRUCT_* method, CORINFO_CONTEXT_STRUCT* context)
    {
        AddCall("initClass");
        var temp = _original.initClass(field, method, context);
        mc.recInitClass(field, method, context, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getBuiltinClass(CorInfoClassId classId)
    {
        AddCall("getBuiltinClass");
        var temp = _original.getBuiltinClass(classId);
        mc.recGetBuiltinClass(classId, temp);
        return temp;
    }

    private CorInfoType getTypeForPrimitiveValueClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForPrimitiveValueClass");
        var temp = _original.getTypeForPrimitiveValueClass(cls);
        mc.recGetTypeForPrimitiveValueClass(cls, temp);
        return temp;
    }

    private CorInfoType getTypeForPrimitiveNumericClass(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getTypeForPrimitiveNumericClass");
        var temp = _original.getTypeForPrimitiveNumericClass(cls);
        mc.recGetTypeForPrimitiveNumericClass(cls, temp);
        return temp;
    }

    private TypeCompareState compareTypesForCast(CORINFO_CLASS_STRUCT_* fromClass, CORINFO_CLASS_STRUCT_* toClass)
    {
        AddCall("compareTypesForCast");
        var temp = _original.compareTypesForCast(fromClass, toClass);
        mc.recCompareTypesForCast(fromClass, toClass, temp);
        return temp;
    }

    private TypeCompareState compareTypesForEquality(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        AddCall("compareTypesForEquality");
        var temp = _original.compareTypesForEquality(cls1, cls2);
        mc.recCompareTypesForEquality(cls1, cls2, temp);
        return temp;
    }

    private bool isMoreSpecificType(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2)
    {
        AddCall("isMoreSpecificType");
        var temp = _original.isMoreSpecificType(cls1, cls2);
        mc.recIsMoreSpecificType(cls1, cls2, temp);
        return temp;
    }

    private bool isExactType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isExactType");
        var temp = _original.isExactType(cls);
        mc.recIsExactType(cls, temp);
        return temp;
    }

    private TypeCompareState isGenericType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isGenericType");
        var temp = _original.isGenericType(cls);
        mc.recIsGenericType(cls, temp);
        return temp;
    }

    private TypeCompareState isNullableType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isNullableType");
        var temp = _original.isNullableType(cls);
        mc.recIsNullableType(cls, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getParentType(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getParentType");
        var temp = _original.getParentType(cls);
        mc.recGetParentType(cls, temp);
        return temp;
    }

    private CorInfoType getChildType(CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_CLASS_STRUCT_** clsRet)
    {
        AddCall("getChildType");
        var temp = _original.getChildType(clsHnd, clsRet);
        mc.recGetChildType(clsHnd, clsRet, temp);
        return temp;
    }

    private bool isSDArray(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("isSDArray");
        var temp = _original.isSDArray(cls);
        mc.recIsSDArray(cls, temp);
        return temp;
    }

    private uint getArrayRank(CORINFO_CLASS_STRUCT_* cls)
    {
        AddCall("getArrayRank");
        var result = _original.getArrayRank(cls);
        mc.recGetArrayRank(cls, result);
        return result;
    }

    private CorInfoArrayIntrinsic getArrayIntrinsicID(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getArrayIntrinsicID");
        var result = _original.getArrayIntrinsicID(ftn);
        mc.recGetArrayIntrinsicID(ftn, result);
        return result;
    }

    private void* getArrayInitializationData(CORINFO_FIELD_STRUCT_* field, uint size)
    {
        AddCall("getArrayInitializationData");
        var temp = _original.getArrayInitializationData(field, size);
        mc.recGetArrayInitializationData(field, size, temp);
        return temp;
    }

    private CorInfoIsAccessAllowedResult canAccessClass(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_HELPER_DESC pAccessHelper)
    {
        AddCall("canAccessClass");
        var temp = _original.canAccessClass(ref pResolvedToken, callerHandle, ref pAccessHelper);
        mc.recCanAccessClass(ref pResolvedToken, callerHandle, ref pAccessHelper, temp);
        return temp;
    }

    private nuint printFieldName(CORINFO_FIELD_STRUCT_* ftn, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printFieldName");
        var temp = _original.printFieldName(ftn, buffer, bufferSize, pRequiredBufferSize);
        mc.recPrintFieldName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* getFieldClass(CORINFO_FIELD_STRUCT_* field)
    {
        AddCall("getFieldClass");
        var temp = _original.getFieldClass(field);
        mc.recGetFieldClass(field, temp);
        return temp;
    }

    private CorInfoType getFieldType(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_** structType, CORINFO_CLASS_STRUCT_* fieldOwnerHint)
    {
        AddCall("getFieldType");
        var temp = _original.getFieldType(field, structType, fieldOwnerHint);
        mc.recGetFieldType(field, structType, fieldOwnerHint, temp);
        return temp;
    }

    private uint getFieldOffset(CORINFO_FIELD_STRUCT_* field)
    {
        AddCall("getFieldOffset");
        var temp = _original.getFieldOffset(field);
        mc.recGetFieldOffset(field, temp);
        return temp;
    }

    private void getFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
    {
        AddCall("getFieldInfo");
        _original.getFieldInfo(ref pResolvedToken, callerHandle, flags, pResult);
        mc.recGetFieldInfo(ref pResolvedToken, callerHandle, flags, pResult);
    }

    private uint getThreadLocalFieldInfo(CORINFO_FIELD_STRUCT_* field, bool isGCType)
    {
        AddCall("getThreadLocalFieldInfo");
        var result = _original.getThreadLocalFieldInfo(field, isGCType);
        mc.recGetThreadLocalFieldInfo(field, isGCType, result);
        return result;
    }

    private void getThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
    {
        AddCall("getThreadLocalStaticBlocksInfo");
        _original.getThreadLocalStaticBlocksInfo(pInfo);
        mc.recGetThreadLocalStaticBlocksInfo(pInfo);
    }

    private void getThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
    {
        AddCall("getThreadLocalStaticInfo_NativeAOT");
        _original.getThreadLocalStaticInfo_NativeAOT(pInfo);
        mc.recGetThreadLocalStaticInfo_NativeAOT(pInfo);
    }

    private bool isFieldStatic(CORINFO_FIELD_STRUCT_* fldHnd)
    {
        AddCall("isFieldStatic");
        var result = _original.isFieldStatic(fldHnd);
        mc.recIsFieldStatic(fldHnd, result);
        return result;
    }

    private int getArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHnd)
    {
        AddCall("getArrayOrStringLength");
        var result = _original.getArrayOrStringLength(objHnd);
        mc.recGetArrayOrStringLength(objHnd, result);
        return result;
    }

    private void getBoundaries(CORINFO_METHOD_STRUCT_* ftn, ref uint cILOffsets, ref uint* pILOffsets, BoundaryTypes* implicitBoundaries)
    {
        AddCall("getBoundaries");
        _original.getBoundaries(ftn, ref cILOffsets, ref pILOffsets, implicitBoundaries);
        mc.recGetBoundaries(ftn, ref cILOffsets, ref pILOffsets, implicitBoundaries);
    }

    private void setBoundaries(CORINFO_METHOD_STRUCT_* ftn, uint cMap, OffsetMapping* pMap)
    {
        AddCall("setBoundaries");
        cr.recSetBoundaries(ftn, cMap, pMap);
        _original.setBoundaries(ftn, cMap, pMap);
    }

    private void getVars(CORINFO_METHOD_STRUCT_* ftn, ref uint cVars, ILVarInfo** vars, ref bool extendOthers)
    {
        AddCall("getVars");
        _original.getVars(ftn, ref cVars, vars, ref extendOthers);
        mc.recGetVars(ftn, ref cVars, vars, ref extendOthers);
    }

    private void setVars(CORINFO_METHOD_STRUCT_* ftn, uint cVars, NativeVarInfo* vars)
    {
        AddCall("setVars");
        cr.recSetVars(ftn, cVars, vars);
        _original.setVars(ftn, cVars, vars);
    }

    private void reportRichMappings(InlineTreeNode* inlineTreeNodes, uint numInlineTreeNodes, RichOffsetMapping* mappings, uint numMappings)
    {
        AddCall("reportRichMappings");
        _original.reportRichMappings(inlineTreeNodes, numInlineTreeNodes, mappings, numMappings);
    }

    private void reportAsyncDebugInfo(AsyncInfo* asyncInfo, AsyncSuspensionPoint* suspensionPoints, AsyncContinuationVarInfo* vars, uint numVars)
    {
        AddCall("reportAsyncDebugInfo");
        _original.reportAsyncDebugInfo(asyncInfo, suspensionPoints, vars, numVars);
    }

    private void reportMetadata(byte* key, void* value, nuint length)
    {
        AddCall("reportMetadata");
        _original.reportMetadata(key, value, length);
    }

    private CORINFO_ARG_LIST_STRUCT_* getArgNext(CORINFO_ARG_LIST_STRUCT_* args)
    {
        AddCall("getArgNext");
        var temp = _original.getArgNext(args);
        mc.recGetArgNext(args, temp);
        return temp;
    }

    private CorInfoHFAElemType getHFAType(CORINFO_CLASS_STRUCT_* hClass)
    {
        AddCall("getHFAType");
        var temp = _original.getHFAType(hClass);
        mc.recGetHFAType(hClass, temp);
        return temp;
    }

    private void getEEInfo(ref CORINFO_EE_INFO pEEInfoOut)
    {
        AddCall("getEEInfo");
        _original.getEEInfo(ref pEEInfoOut);
        mc.recGetEEInfo(ref pEEInfoOut);
    }

    private void getAsyncInfo(ref CORINFO_ASYNC_INFO pAsyncInfo)
    {
        AddCall("getAsyncInfo");
        _original.getAsyncInfo(ref pAsyncInfo);
        mc.recGetAsyncInfo(ref pAsyncInfo);
    }

    private CORINFO_METHOD_STRUCT_* getAwaitReturnCall(CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        AddCall("getAwaitReturnCall");
        var result = _original.getAwaitReturnCall(callerHandle, contextHandle, ref instArg);
        mc.recGetAwaitReturnCall(callerHandle, contextHandle, ref instArg, result);
        return result;
    }

    private CORINFO_METHOD_STRUCT_* getAwaitAwaiterInContinuationCall(CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool isUnsafe, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg)
    {
        AddCall("getAwaitAwaiterInContinuationCall");
        var result = _original.getAwaitAwaiterInContinuationCall(callerHandle, ref pResolvedToken, isUnsafe, contextHandle, ref instArg);
        mc.recGetAwaitAwaiterInContinuationCall(callerHandle, ref pResolvedToken, isUnsafe, contextHandle, ref instArg, result);
        return result;
    }

    private mdToken getMethodDefFromMethod(CORINFO_METHOD_STRUCT_* hMethod)
    {
        AddCall("getMethodDefFromMethod");
        var result = _original.getMethodDefFromMethod(hMethod);
        mc.recGetMethodDefFromMethod(hMethod, (uint)result);
        return result;
    }

    private nuint printMethodName(CORINFO_METHOD_STRUCT_* ftn, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize)
    {
        AddCall("printMethodName");
        var temp = _original.printMethodName(ftn, buffer, bufferSize, pRequiredBufferSize);
        mc.recPrintMethodName(ftn, buffer, bufferSize, pRequiredBufferSize, temp);
        return temp;
    }

    private byte* getMethodNameFromMetadata(CORINFO_METHOD_STRUCT_* ftn, byte** className, byte** namespaceName, byte** enclosingClassNames, nuint maxEnclosingClassNames)
    {
        AddCall("getMethodNameFromMetadata");
        var temp = _original.getMethodNameFromMetadata(ftn, className, namespaceName, enclosingClassNames, maxEnclosingClassNames);
        mc.recGetMethodNameFromMetadata(ftn, temp, className, namespaceName, enclosingClassNames, maxEnclosingClassNames);
        return temp;
    }

    private uint getMethodHash(CORINFO_METHOD_STRUCT_* ftn)
    {
        AddCall("getMethodHash");
        var temp = _original.getMethodHash(ftn);
        mc.recGetMethodHash(ftn, temp);
        return temp;
    }

    private bool getSystemVAmd64PassStructInRegisterDescriptor(CORINFO_CLASS_STRUCT_* structHnd, SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr)
    {
        AddCall("getSystemVAmd64PassStructInRegisterDescriptor");
        var result = _original.getSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr);
        mc.recGetSystemVAmd64PassStructInRegisterDescriptor(structHnd, structPassInRegDescPtr, result);
        return result;
    }

    private void getSwiftLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_SWIFT_LOWERING pLowering)
    {
        AddCall("getSwiftLowering");
        _original.getSwiftLowering(structHnd, ref pLowering);
        mc.recGetSwiftLowering(structHnd, ref pLowering);
    }

    private void getFpStructLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_FPSTRUCT_LOWERING pLowering)
    {
        AddCall("getFpStructLowering");
        _original.getFpStructLowering(structHnd, ref pLowering);
        mc.recGetFpStructLowering(structHnd, ref pLowering);
    }

    private CorInfoWasmType getWasmLowering(CORINFO_CLASS_STRUCT_* structHnd)
    {
        AddCall("getWasmLowering");
        var result = _original.getWasmLowering(structHnd);
        mc.recGetWasmLowering(structHnd, result);
        return result;
    }

    private uint getAddressAlignment(void* address)
    {
        AddCall("getAddressAlignment");
        var result = _original.getAddressAlignment(address);
        mc.recGetAddressAlignment(address, result);
        return result;
    }

    private void getWasmWellKnownGlobals(ref CORINFO_WASM_WELLKNOWN_GLOBALS pWellKnownGlobalsOut)
    {
        AddCall("getWasmWellKnownGlobals");
        _original.getWasmWellKnownGlobals(ref pWellKnownGlobalsOut);
        mc.recGetWasmWellKnownGlobals(ref pWellKnownGlobalsOut);
    }

    private uint getThreadTLSIndex(ref void* ppIndirection)
    {
        AddCall("getThreadTLSIndex");
        var temp = _original.getThreadTLSIndex(ref ppIndirection);
        mc.recGetThreadTLSIndex(ref ppIndirection, temp);
        return temp;
    }

    private int* getAddrOfCaptureThreadGlobal(ref void* ppIndirection)
    {
        AddCall("getAddrOfCaptureThreadGlobal");
        var temp = _original.getAddrOfCaptureThreadGlobal(ref ppIndirection);
        mc.recGetAddrOfCaptureThreadGlobal(ref ppIndirection, temp);
        return temp;
    }

    private void getFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
    {
        AddCall("getFunctionEntryPoint");
        _original.getFunctionEntryPoint(ftn, ref pResult, accessFlags);
        mc.recGetFunctionEntryPoint(ftn, ref pResult, accessFlags);
    }

    private void getFunctionFixedEntryPoint(CORINFO_METHOD_STRUCT_* ftn, bool isUnsafeFunctionPointer, ref CORINFO_CONST_LOOKUP pResult)
    {
        AddCall("getFunctionFixedEntryPoint");
        _original.getFunctionFixedEntryPoint(ftn, isUnsafeFunctionPointer, ref pResult);
        mc.recGetFunctionFixedEntryPoint(ftn, ref pResult);
    }

    private CORINFO_MODULE_STRUCT_* embedModuleHandle(CORINFO_MODULE_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedModuleHandle");
        var temp = _original.embedModuleHandle(handle, ref ppIndirection);
        mc.recEmbedModuleHandle(handle, ref ppIndirection, temp);
        return temp;
    }

    private CORINFO_CLASS_STRUCT_* embedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedClassHandle");
        var temp = _original.embedClassHandle(handle, ref ppIndirection);
        mc.recEmbedClassHandle(handle, ref ppIndirection, temp);
        return temp;
    }

    private CORINFO_METHOD_STRUCT_* embedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedMethodHandle");
        var temp = _original.embedMethodHandle(handle, ref ppIndirection);
        mc.recEmbedMethodHandle(handle, ref ppIndirection, temp);
        return temp;
    }

    private CORINFO_FIELD_STRUCT_* embedFieldHandle(CORINFO_FIELD_STRUCT_* handle, ref void* ppIndirection)
    {
        AddCall("embedFieldHandle");
        var temp = _original.embedFieldHandle(handle, ref ppIndirection);
        mc.recEmbedFieldHandle(handle, ref ppIndirection, temp);
        return temp;
    }

    private void embedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        AddCall("embedGenericHandle");
        _original.embedGenericHandle(ref pResolvedToken, fEmbedParent, callerHandle, ref pResult);
        mc.recEmbedGenericHandle(ref pResolvedToken, fEmbedParent, callerHandle, ref pResult);
    }

    private void getLocationOfThisType(CORINFO_METHOD_STRUCT_* context, ref CORINFO_LOOKUP_KIND pLookupKind)
    {
        AddCall("getLocationOfThisType");
        _original.getLocationOfThisType(context, ref pLookupKind);
        mc.recGetLocationOfThisType(context, ref pLookupKind);
    }

    private void getAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
    {
        AddCall("getAddressOfPInvokeTarget");
        _original.getAddressOfPInvokeTarget(method, ref pLookup);
        mc.recGetAddressOfPInvokeTarget(method, ref pLookup);
    }

    private void* GetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig)
    {
        AddCall("GetCookieForInterpreterCalliSig");
        var temp = _original.GetCookieForInterpreterCalliSig(szMetaSig);
        mc.recGetCookieForInterpreterCalliSig(szMetaSig, temp);
        return temp;
    }

    private CORINFO_JUST_MY_CODE_HANDLE_* getJustMyCodeHandle(CORINFO_METHOD_STRUCT_* method, ref CORINFO_JUST_MY_CODE_HANDLE_* ppIndirection)
    {
        AddCall("getJustMyCodeHandle");
        var temp = _original.getJustMyCodeHandle(method, ref ppIndirection);
        mc.recGetJustMyCodeHandle(method, ref ppIndirection, temp);
        return temp;
    }

    private void GetProfilingHandle(ref bool pbHookFunction, ref void* pProfilerHandle, ref bool pbIndirectedHandles)
    {
        AddCall("GetProfilingHandle");
        _original.GetProfilingHandle(ref pbHookFunction, ref pProfilerHandle, ref pbIndirectedHandles);
        mc.recGetProfilingHandle(ref pbHookFunction, ref pProfilerHandle, ref pbIndirectedHandles);
    }

    private bool getStaticFieldContent(CORINFO_FIELD_STRUCT_* field, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects)
    {
        AddCall("getStaticFieldContent");
        var result = _original.getStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects);
        mc.recGetStaticFieldContent(field, buffer, bufferSize, valueOffset, ignoreMovableObjects, result);
        return result;
    }

    private bool getObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset)
    {
        AddCall("getObjectContent");
        var result = _original.getObjectContent(obj, buffer, bufferSize, valueOffset);
        mc.recGetObjectContent(obj, buffer, bufferSize, valueOffset, result);
        return result;
    }

    private CORINFO_CLASS_STRUCT_* getStaticFieldCurrentClass(CORINFO_FIELD_STRUCT_* field, byte* pIsSpeculative)
    {
        AddCall("getStaticFieldCurrentClass");
        var result = _original.getStaticFieldCurrentClass(field, pIsSpeculative);
        mc.recGetStaticFieldCurrentClass(field, (bool*)pIsSpeculative, result);
        return result;
    }

    private IntPtr getVarArgsHandle(CORINFO_SIG_INFO* pSig, CORINFO_METHOD_STRUCT_* methHnd, ref void* ppIndirection)
    {
        AddCall("getVarArgsHandle");
        var temp = _original.getVarArgsHandle(pSig, methHnd, ref ppIndirection);
        mc.recGetVarArgsHandle(pSig, methHnd, ref ppIndirection, (void*)temp);
        return temp;
    }

    private InfoAccessType constructStringLiteral(CORINFO_MODULE_STRUCT_* module, mdToken metaTok, ref void* ppValue)
    {
        AddCall("constructStringLiteral");
        var temp = _original.constructStringLiteral(module, metaTok, ref ppValue);
        mc.recConstructStringLiteral(module, (uint)metaTok, ppValue, temp);
        return temp;
    }

    private InfoAccessType emptyStringLiteral(ref void* ppValue)
    {
        AddCall("emptyStringLiteral");
        var temp = _original.emptyStringLiteral(ref ppValue);
        mc.recEmptyStringLiteral(ref ppValue, temp);
        return temp;
    }

    private uint getFieldThreadLocalStoreID(CORINFO_FIELD_STRUCT_* field, ref void* ppIndirection)
    {
        AddCall("getFieldThreadLocalStoreID");
        var temp = _original.getFieldThreadLocalStoreID(field, ref ppIndirection);
        mc.recGetFieldThreadLocalStoreID(field, ref ppIndirection, temp);
        return temp;
    }

    private CORINFO_METHOD_STRUCT_* GetDelegateCtor(CORINFO_METHOD_STRUCT_* methHnd, CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_METHOD_STRUCT_* targetMethodHnd, ref DelegateCtorArgs pCtorData)
    {
        AddCall("GetDelegateCtor");
        var temp = _original.GetDelegateCtor(methHnd, clsHnd, targetMethodHnd, ref pCtorData);
        mc.recGetDelegateCtor(methHnd, clsHnd, targetMethodHnd, ref pCtorData, temp);
        return temp;
    }

    private void MethodCompileComplete(CORINFO_METHOD_STRUCT_* methHnd)
    {
        AddCall("MethodCompileComplete");
        _original.MethodCompileComplete(methHnd);
    }

    private CORINFO_CLASS_STRUCT_* getContinuationType(nuint dataSize, ref bool objRefs, nuint objRefsSize)
    {
        AddCall("getContinuationType");
        var result = _original.getContinuationType(dataSize, ref objRefs, objRefsSize);
        mc.recGetContinuationType(dataSize, ref objRefs, objRefsSize, result);
        return result;
    }

    private CORINFO_METHOD_STRUCT_* getAsyncResumptionStub(ref void* entryPoint)
    {
        AddCall("getAsyncResumptionStub");
        var stub = _original.getAsyncResumptionStub(ref entryPoint);
        mc.recGetAsyncResumptionStub(stub, entryPoint);
        return stub;
    }

    private bool convertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fMustConvert)
    {
        AddCall("convertPInvokeCalliToCall");
        var result = _original.convertPInvokeCalliToCall(ref pResolvedToken, fMustConvert);
        mc.recConvertPInvokeCalliToCall(ref pResolvedToken, fMustConvert, result);
        return result;
    }

    private bool notifyInstructionSetUsage(InstructionSet instructionSet, bool supported, bool preserveNegativeDependency)
    {
        AddCall("notifyInstructionSetUsage");
        var result = _original.notifyInstructionSetUsage(instructionSet, supported, preserveNegativeDependency);
        mc.recNotifyInstructionSetUsage(instructionSet, supported, preserveNegativeDependency, result);
        return result;
    }

    private void allocMem(ref AllocMemArgs pArgs)
    {
        AddCall("allocMem");
        _original.allocMem(ref pArgs);
    }

    private void reserveUnwindInfo(bool isFunclet, bool isColdCode, uint unwindSize)
    {
        AddCall("reserveUnwindInfo");
        _original.reserveUnwindInfo(isFunclet, isColdCode, unwindSize);
        cr.recReserveUnwindInfo(isFunclet, isColdCode, unwindSize);
    }

    private void allocUnwindInfo(byte* pHotCode, byte* pColdCode, uint startOffset, uint endOffset, uint unwindSize, byte* pUnwindBlock, CorJitFuncKind funcKind)
    {
        AddCall("allocUnwindInfo");
        _original.allocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
        cr.recAllocUnwindInfo(pHotCode, pColdCode, startOffset, endOffset, unwindSize, pUnwindBlock, funcKind);
    }

    private void* allocGCInfo(nuint size)
    {
        AddCall("allocGCInfo");
        var temp = _original.allocGCInfo(size);
        cr.recAllocGCInfo(size, temp);
        return temp;
    }

    private void setEHcount(uint cEH)
    {
        AddCall("setEHcount");
        _original.setEHcount(cEH);
        cr.recSetEHcount(cEH);
    }

    private void setEHinfo(uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        AddCall("setEHinfo");
        _original.setEHinfo(EHnumber, ref clause);
        cr.recSetEHinfo(EHnumber, ref clause);
    }

    private bool logMsg(uint level, byte* fmt, IntPtr args)
    {
        AddCall("logMsg");
        return _original.logMsg(level, fmt, args);
    }

    private void reportFatalError(CorJitResult result)
    {
        AddCall("reportFatalError");
        _original.reportFatalError(result);
        cr.recReportFatalError(result);
    }

    private HRESULT allocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData)
    {
        AddCall("allocPgoInstrumentationBySchema");
        var result = _original.allocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData);
        mc.recAllocPgoInstrumentationBySchema(ftnHnd, pSchema, countSchemaItems, pInstrumentationData, (int)result);
        return result;
    }

    private void recordCallSite(uint instrOffset, CORINFO_SIG_INFO* callSig, CORINFO_METHOD_STRUCT_* methodHandle)
    {
        AddCall("recordCallSite");
        _original.recordCallSite(instrOffset, callSig, methodHandle);
        cr.recRecordCallSite(instrOffset, callSig, methodHandle);
    }

    private void recordRelocation(void* location, void* locationRW, void* target, CorInfoReloc fRelocType, int addlDelta)
    {
        AddCall("recordRelocation");
        _original.recordRelocation(location, locationRW, target, fRelocType, addlDelta);
        cr.recRecordRelocation(location, target, fRelocType, addlDelta);
    }

    private CorInfoReloc getRelocTypeHint(void* target)
    {
        AddCall("getRelocTypeHint");
        var result = _original.getRelocTypeHint(target);
        mc.recGetRelocTypeHint(target, result);
        return result;
    }

    private uint getExpectedTargetArchitecture()
    {
        AddCall("getExpectedTargetArchitecture");
        var result = _original.getExpectedTargetArchitecture();
        mc.recGetExpectedTargetArchitecture(result);
        return result;
    }

    private uint getJitFlags(ref CORJIT_FLAGS jitFlags, uint sizeInBytes)
    {
        AddCall("getJitFlags");
        var result = _original.getJitFlags(ref jitFlags, sizeInBytes);
        mc.recGetJitFlags(ref jitFlags, sizeInBytes, result);
        return result;
    }

    private CORINFO_WASM_TYPE_SYMBOL_STRUCT_* getWasmTypeSymbol(CorInfoWasmType* types, nuint typesSize)
    {
        AddCall("getWasmTypeSymbol");
        var temp = _original.getWasmTypeSymbol(types, typesSize);
        mc.recGetWasmTypeSymbol(types, typesSize, temp);
        return temp;
    }

    private CORINFO_METHOD_STRUCT_* getSpecialCopyHelper(CORINFO_CLASS_STRUCT_* type)
    {
        AddCall("getSpecialCopyHelper");
        var temp = _original.getSpecialCopyHelper(type);
        mc.recGetSpecialCopyHelper(type, temp);
        return temp;
    }
}
