// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.JitInterface;
using Internal.Pgo;

using static SuperPmi.SpmiRecordsHelper;

namespace SuperPmi;

// The recording side of the JIT-EE interface: stores the EE's answers to the JIT's queries, keyed the way
// the replay side looks them up.
public sealed unsafe partial class MethodContext
{
    private static nuint strlen(byte* s) => (nuint)System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpanFromNullTerminated(s).Length;

    private static ulong CastPointer(void* p) => (ulong)p;

    private static ulong CastPointer(nuint p) => p;

    // The signature instantiation handles are stored in their own map, created on first use.
    private Agnostic_CORINFO_SIG_INFO StoreSig(in CORINFO_SIG_INFO sig, LightWeightMapBuffer buffers)
    {
        DenseLightWeightMap<ulong> handleMap = SigInstHandleMap;
        Agnostic_CORINFO_SIG_INFO result = StoreAgnostic_CORINFO_SIG_INFO(sig, buffers, ref handleMap);
        SigInstHandleMap = handleMap;
        return result;
    }

    private void StoreSigInstHandles(uint count, CORINFO_CLASS_STRUCT_** handles, out uint countOut, out uint indexOut)
    {
        DenseLightWeightMap<ulong> handleMap = SigInstHandleMap;
        StoreAgnostic_CORINFO_SIG_INST_HandleArray(count, handles, ref handleMap, out countOut, out indexOut);
        SigInstHandleMap = handleMap;
    }

    public void recGlobalContext(MethodContext other)
    {
        ErrorHandling.AssertMsg(GetIntConfigValue == null, "GetIntConfigValue == null", "");
        ErrorHandling.AssertMsg(GetStringConfigValue == null, "GetStringConfigValue == null", "");
    
        if (other.GetIntConfigValue != null)
        {
            GetIntConfigValue = other.GetIntConfigValue.Clone();
        }
    
        if (other.GetStringConfigValue != null)
        {
            GetStringConfigValue = other.GetStringConfigValue.Clone();
        }
    }

    public void recCompileMethod(CORINFO_METHOD_INFO* info, uint flags, CORINFO_OS os)
    {
        CompileMethod ??= new();
    
        Agnostic_CompileMethod value = default;
    
        value.info.ftn           = CastHandle(info->ftn);
        value.info.scope         = CastHandle(info->scope);
        value.info.ILCode_offset = (uint)CompileMethod.AddBuffer(Bytes(info->ILCode, info->ILCodeSize));
        value.info.ILCodeSize    = (uint)info->ILCodeSize;
        value.info.maxStack      = (uint)info->maxStack;
        value.info.EHcount       = (uint)info->EHcount;
        value.info.options       = (uint)info->options;
        value.info.regionKind    = (uint)info->regionKind;
    
        value.info.args   = StoreSig(info->args, CompileMethod);
        value.info.locals = StoreSig(info->locals, CompileMethod);
    
        value.os = (uint)os;
    
        value.flags = (uint)flags;
    
        CompileMethod.Add(0, value);
    }

    public void recGetMethodClass(CORINFO_METHOD_STRUCT_* methodHandle, CORINFO_CLASS_STRUCT_* classHandle)
    {
        GetMethodClass ??= new();
    
        ulong key = CastHandle(methodHandle);
        ulong value = CastHandle(classHandle);
        GetMethodClass.Add(key, value);
    }

    public void recGetClassAttribs(CORINFO_CLASS_STRUCT_* classHandle, uint attribs)
    {
        GetClassAttribs ??= new();
    
        ulong key = CastHandle(classHandle);
        GetClassAttribs.Add(key, attribs);
    }

    public void recIsIntrinsic(CORINFO_METHOD_STRUCT_* ftn, bool result)
    {
        IsIntrinsic ??= new();
    
        ulong key = CastHandle(ftn);
        uint value = result ? 1u : 0u;
        IsIntrinsic.Add(key, value);
    }

    public void recNotifyMethodInfoUsage(CORINFO_METHOD_STRUCT_* ftn, bool result)
    {
        NotifyMethodInfoUsage ??= new();
    
        ulong key = CastHandle(ftn);
        uint value = result ? 1u : 0u;
        NotifyMethodInfoUsage.Add(key, value);
    }

    public void recNotifyInstructionSetUsage(InstructionSet isa, bool supported, bool preserveNegativeDependency, bool result)
    {
        NotifyInstructionSetUsage ??= new();
    
        DD key = default;
        key.A = (uint)isa;
        key.B = (supported ? 1u : 0u) | (preserveNegativeDependency ? 2u : 0u);
        NotifyInstructionSetUsage.Add(key, result ? 1u : 0u);
    }

    public void recGetMethodAttribs(CORINFO_METHOD_STRUCT_* methodHandle, uint attribs)
    {
        GetMethodAttribs ??= new();
    
        ulong key = CastHandle(methodHandle);
        GetMethodAttribs.Add(key, attribs);
    }

    public void recGetClassAssemblyName(CORINFO_CLASS_STRUCT_* cls, byte* assemblyName)
    {
        GetClassAssemblyName ??= new();
    
        uint value;
        if (assemblyName != null)
        {
            value = GetClassAssemblyName.AddBuffer(Bytes(assemblyName, strlen(assemblyName) + 1));
        }
        else
        {
            value = uint.MaxValue;
        }
    
        ulong key = CastHandle(cls);
        GetClassAssemblyName.Add(key, value);
    }

    public void recGetVars(CORINFO_METHOD_STRUCT_* ftn, ref uint cVars, ILVarInfo** vars_in, ref bool extendOthers)
    {
        GetVars ??= new();
    
        Agnostic_GetVars value = default;
        value.cVars = (uint)cVars;
        value.vars_offset =
            (uint)GetVars.AddBuffer(Bytes(*vars_in, (nuint)(sizeof(ILVarInfo) * (cVars))));
        value.extendOthers = (extendOthers ? 1u : 0u);
    
        ulong key = CastHandle(ftn);
        GetVars.Add(key, value);
    }

    public void recGetBoundaries(CORINFO_METHOD_STRUCT_* ftn, ref uint cILOffsets, ref uint* pILOffsets, BoundaryTypes* implicitBoundaries)
    {
        GetBoundaries ??= new();
    
        Agnostic_GetBoundaries value = default;
    
        value.cILOffsets = (uint)cILOffsets;
        value.pILOffset_offset =
            (uint)GetBoundaries.AddBuffer(Bytes(pILOffsets, sizeof(uint) * (cILOffsets)));
        value.implicitBoundaries = (uint)*implicitBoundaries;
    
        ulong key = CastHandle(ftn);
        GetBoundaries.Add(key, value);
    }

    public void recInitClass(CORINFO_FIELD_STRUCT_* field, CORINFO_METHOD_STRUCT_* method, CORINFO_CONTEXT_STRUCT* context, CorInfoInitClassResult result)
    {
        InitClass ??= new();
    
        Agnostic_InitClass key = default;
        key.field   = CastHandle(field);
        key.method  = CastHandle(method);
        key.context = CastHandle(context);
    
        uint value = (uint)result;
        InitClass.Add(key, value);
    }

    public void recGetMethodNameFromMetadata(CORINFO_METHOD_STRUCT_* ftn, byte* methodName, byte** className, byte** namespaceName, byte** enclosingClassNames, nuint maxEnclosingClassNames)
    {
        GetMethodNameFromMetadata ??= new();
        Agnostic_CORINFO_METHODNAME_TOKENout value = default;
        Agnostic_CORINFO_METHODNAME_TOKENin key = default;
        key.ftn = CastHandle(ftn);
        key.className = (className != null) ? 1u : 0u;
        key.namespaceName = (namespaceName != null) ? 1u : 0u;
        key.maxEnclosingClassNames = (uint)maxEnclosingClassNames;
    
        if (methodName != null)
            value.methodName = GetMethodNameFromMetadata.AddBuffer(Bytes(methodName, strlen(methodName) + 1));
        else
            value.methodName = uint.MaxValue;
    
        if ((className != null) && (*className != null))
            value.className = GetMethodNameFromMetadata.AddBuffer(Bytes(*className, strlen(*className) + 1));
        else
            value.className = uint.MaxValue;
    
        if ((namespaceName != null) && (*namespaceName != null))
            value.namespaceName =
            GetMethodNameFromMetadata.AddBuffer(Bytes(*namespaceName, strlen(*namespaceName) + 1));
        else
            value.namespaceName = uint.MaxValue;
    
        uint* enclosingClassNameIndices = stackalloc uint[(int)maxEnclosingClassNames];
        for (nuint i = 0; i < maxEnclosingClassNames; i++)
        {
            byte* enclosingClassName = enclosingClassNames[i];
            if (enclosingClassName != null)
                enclosingClassNameIndices[i] = GetMethodNameFromMetadata.AddBuffer(Bytes(enclosingClassName, strlen(enclosingClassName) + 1));
            else
                enclosingClassNameIndices[i] = uint.MaxValue;
        }
    
        if (maxEnclosingClassNames > 0)
            value.enclosingClassNames = GetMethodNameFromMetadata.AddBuffer(Bytes(enclosingClassNameIndices, sizeof(uint) * (uint)maxEnclosingClassNames));
        else
            value.enclosingClassNames = uint.MaxValue;
    
        GetMethodNameFromMetadata.Add(key, value);
    }

    public void recGetJitFlags(ref CORJIT_FLAGS jitFlags, uint sizeInBytes, uint result)
    {
        GetJitFlags ??= new();
    
        DD value = default;
        value.A = (uint)GetJitFlags.AddBuffer(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<CORJIT_FLAGS, byte>(ref jitFlags), (int)sizeInBytes));
        value.B = result;
    
        // NOTE: getJitFlags() is expected to be idempotent per method, so the mapping key is always
        //       zero.
        GetJitFlags.Add(0, value);
    }

    public void recCanInline(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* calleeHnd, CorInfoInline response, uint exceptionCode)
    {
        CanInline ??= new();
    
        DLDL key = default;
        key.A = CastHandle(callerHnd);
        key.B = CastHandle(calleeHnd);
    
        Agnostic_CanInline value = default;
        value.result           = (uint)response;
        value.exceptionCode    = (uint)exceptionCode;
    
        CanInline.Add(key, value);
    }

    public void recResolveToken(ref CORINFO_RESOLVED_TOKEN pResolvedToken, uint exceptionCode)
    {
        ResolveToken ??= new();
    
        Agnostic_CORINFO_RESOLVED_TOKENin key = default;
        key = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken);
    
        ResolveTokenValue value = default;
        if (exceptionCode != 0)
        {
            // The output token memory might be corrupt or uninitialized, so just zero it out.
            // (Set indexes to -1, indicating no buffer).
            value.tokenOut = default;
            value.tokenOut.pTypeSpec_Index   = uint.MaxValue;
            value.tokenOut.pMethodSpec_Index = uint.MaxValue;
        }
        else
        {
            value.tokenOut = StoreAgnostic_CORINFO_RESOLVED_TOKENout(ref pResolvedToken, ResolveToken);
        }
        value.exceptionCode = exceptionCode;
    
        ResolveToken.Add(key, value);
    }

    public void recGetCallInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_CALLINFO_FLAGS flags, CORINFO_CALL_INFO* pResult, uint exceptionCode)
    {
        GetCallInfo ??= new();
    
        Agnostic_GetCallInfo key = default;
        key.ResolvedToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetCallInfo);
    
        if (pConstrainedResolvedToken != null)
        {
            key.ConstrainedResolvedToken =
                StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref *pConstrainedResolvedToken, GetCallInfo);
        }
    
        key.callerHandle = CastHandle(callerHandle);
        key.flags        = (uint)flags;
    
        Agnostic_CORINFO_CALL_INFO value = default;
    
        if (exceptionCode == 0)
        {
            value.hMethod     = CastHandle(pResult->hMethod);
            value.methodFlags = (uint)pResult->methodFlags;
            value.classFlags  = (uint)pResult->classFlags;
            value.sig         = StoreSig(pResult->sig, GetCallInfo);
    
            value.accessAllowed                   = (uint)pResult->accessAllowed;
            value.callsiteCalloutHelper = StoreAgnostic_CORINFO_HELPER_DESC(pResult->callsiteCalloutHelper);
            value.thisTransform = (uint)pResult->thisTransform;
    
            value.kind                           = (uint)pResult->kind;
            value.nullInstanceCheck              = (pResult->nullInstanceCheck ? 1u : 0u);
            value.contextHandle                  = CastHandle(pResult->contextHandle);
            value.exactContextNeedsRuntimeLookup = (pResult->exactContextNeedsRuntimeLookup ? 1u : 0u);
    
            value.stubLookup = StoreAgnostic_CORINFO_LOOKUP(ref pResult->codePointerOrStubLookup);
    
            value.instParamLookup.accessType = (uint)pResult->instParamLookup.accessType;
            value.instParamLookup.handle     = CastHandle(pResult->instParamLookup.handle);
        }
    
        value.exceptionCode = (uint)exceptionCode;
    
        GetCallInfo.Add(key, value);
    }

    public void recExpandRawHandleIntrinsic(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        ExpandRawHandleIntrinsic ??= new();
    
        Agnostic_ExpandRawHandleIntrinsic key = default;
        key.ResolvedToken = CreateAgnostic_CORINFO_RESOLVED_TOKENin(ref pResolvedToken);
        key.hCallerHandle = CastHandle(callerHandle);
    
        Agnostic_CORINFO_GENERICHANDLE_RESULT value = default;
        value.lookup            = StoreAgnostic_CORINFO_LOOKUP(ref pResult.lookup);
        value.compileTimeHandle = CastHandle(pResult.compileTimeHandle);
        value.handleType        = (uint)pResult.handleType;
    
        ExpandRawHandleIntrinsic.Add(key, value);
    }

    public void recIsIntrinsicType(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsIntrinsicType ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsIntrinsicType.Add(key, value);
    }

    public void recAsCorInfoType(CORINFO_CLASS_STRUCT_* cls, CorInfoType result)
    {
        AsCorInfoType ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        AsCorInfoType.Add(key, value);
    }

    public void recIsValueClass(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsValueClass ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsValueClass.Add(key, value);
    }

    public void recIsByRefLike(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsByRefLike ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsByRefLike.Add(key, value);
    }

    public void recIsSharedInst(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsSharedInst ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsSharedInst.Add(key, value);
    }

    public void recGetClassSize(CORINFO_CLASS_STRUCT_* cls, uint result)
    {
        GetClassSize ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetClassSize.Add(key, value);
    }

    public void recGetHeapClassSize(CORINFO_CLASS_STRUCT_* cls, uint result)
    {
        GetHeapClassSize ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetHeapClassSize.Add(key, value);
    }

    public void recCanAllocateOnStack(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        CanAllocateOnStack ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        CanAllocateOnStack.Add(key, value);
    }

    public void recGetClassNumInstanceFields(CORINFO_CLASS_STRUCT_* cls, uint result)
    {
        GetClassNumInstanceFields ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetClassNumInstanceFields.Add(key, value);
    }

    public void recGetNewArrHelper(CORINFO_CLASS_STRUCT_* arrayCls, CorInfoHelpFunc result)
    {
        GetNewArrHelper ??= new();
    
        ulong key = CastHandle(arrayCls);
        uint value = (uint)result;
        GetNewArrHelper.Add(key, value);
    }

    public void recGetSharedCCtorHelper(CORINFO_CLASS_STRUCT_* clsHnd, CorInfoHelpFunc result)
    {
        GetSharedCCtorHelper ??= new();
    
        ulong key = CastHandle(clsHnd);
        uint value = (uint)result;
        GetSharedCCtorHelper.Add(key, value);
    }

    public void recGetTypeForBox(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetTypeForBox ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetTypeForBox.Add(key, value);
    }

    public void recGetBoxHelper(CORINFO_CLASS_STRUCT_* cls, CorInfoHelpFunc result)
    {
        GetBoxHelper ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetBoxHelper.Add(key, value);
    }

    public void recGetBuiltinClass(CorInfoClassId classId, CORINFO_CLASS_STRUCT_* result)
    {
        GetBuiltinClass ??= new();
    
        uint key = (uint)classId;
        ulong value = CastHandle(result);
        GetBuiltinClass.Add(key, value);
    }

    public void recGetTypeForPrimitiveValueClass(CORINFO_CLASS_STRUCT_* cls, CorInfoType result)
    {
        GetTypeForPrimitiveValueClass ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetTypeForPrimitiveValueClass.Add(key, value);
    }

    public void recGetTypeForPrimitiveNumericClass(CORINFO_CLASS_STRUCT_* cls, CorInfoType result)
    {
        GetTypeForPrimitiveNumericClass ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetTypeForPrimitiveNumericClass.Add(key, value);
    }

    public void recGetParentType(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetParentType ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetParentType.Add(key, value);
    }

    public void recIsSDArray(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsSDArray ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsSDArray.Add(key, value);
    }

    public void recGetFieldClass(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_* result)
    {
        GetFieldClass ??= new();
    
        ulong key = CastHandle(field);
        ulong value = CastHandle(result);
        GetFieldClass.Add(key, value);
    }

    public void recGetFieldOffset(CORINFO_FIELD_STRUCT_* field, uint result)
    {
        GetFieldOffset ??= new();
    
        ulong key = CastHandle(field);
        uint value = (uint)result;
        GetFieldOffset.Add(key, value);
    }

    public void recGetUnBoxHelper(CORINFO_CLASS_STRUCT_* cls, CorInfoHelpFunc result)
    {
        GetUnBoxHelper ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetUnBoxHelper.Add(key, value);
    }

    public void recGetRuntimeTypePointer(CORINFO_CLASS_STRUCT_* cls, CORINFO_OBJECT_STRUCT_* result)
    {
        GetRuntimeTypePointer ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetRuntimeTypePointer.Add(key, value);
    }

    public void recIsObjectImmutable(CORINFO_OBJECT_STRUCT_* objPtr, bool result)
    {
        IsObjectImmutable ??= new();
    
        ulong key = CastHandle(objPtr);
        uint value = (result ? 1u : 0u);
        IsObjectImmutable.Add(key, value);
    }

    public void recGetStringChar(CORINFO_OBJECT_STRUCT_* strObj, int index, ushort* charValue, bool result)
    {
        GetStringChar ??= new();
    
        DLD key = default;
        key.A = CastHandle(strObj);
        key.B = (uint)index;
    
        DD value = default;
        value.A = (result ? 1u : 0u);
        value.B = (uint)*charValue;
    
        GetStringChar.Add(key, value);
    }

    public void recGetObjectType(CORINFO_OBJECT_STRUCT_* objPtr, CORINFO_CLASS_STRUCT_* result)
    {
        GetObjectType ??= new();
    
        ulong key = CastHandle(objPtr);
        ulong value = CastHandle(result);
        GetObjectType.Add(key, value);
    }

    public void recGetReadyToRunHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CorInfoHelpFunc id, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_CONST_LOOKUP pLookup, bool result)
    {
        GetReadyToRunHelper ??= new();
    
        GetReadyToRunHelper_TOKENin key = default;
        key.ResolvedToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetReadyToRunHelper);
        key.id                = (uint)id;
        key.callerHandle      = CastHandle(callerHandle);
        GetReadyToRunHelper_TOKENout value = default;
        value.Lookup = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pLookup);
        value.result = result ? (byte)1 : (byte)0;
    
        GetReadyToRunHelper.Add(key, value);
    }

    public void recGetReadyToRunDelegateCtorHelper(ref CORINFO_RESOLVED_TOKEN pTargetMethod, uint targetConstraint, CORINFO_CLASS_STRUCT_* delegateType, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_LOOKUP pLookup)
    {
        GetReadyToRunDelegateCtorHelper ??= new();
    
        GetReadyToRunDelegateCtorHelper_TOKENIn key = default;
        key.TargetMethod =
            StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pTargetMethod, GetReadyToRunDelegateCtorHelper);
        key.targetConstraint          = targetConstraint;
        key.delegateType              = CastHandle(delegateType);
        key.callerHandle              = CastHandle(callerHandle);
        Agnostic_CORINFO_LOOKUP value = StoreAgnostic_CORINFO_LOOKUP(ref pLookup);
        GetReadyToRunDelegateCtorHelper.Add(key, value);
    }

    public void recGetHelperFtn(CorInfoHelpFunc ftnNum, CORINFO_CONST_LOOKUP pNativeEntrypoint, CORINFO_METHOD_STRUCT_* methodHandle)
    {
        GetHelperFtn ??= new();
    
        uint key = (uint)ftnNum;
    
        Agnostic_GetHelperFtn value = default;
        value.helperLookup = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pNativeEntrypoint);
        value.helperMethod = CastHandle(methodHandle);
    
        GetHelperFtn.Add(key, value);
    }

    public void recGetJustMyCodeHandle(CORINFO_METHOD_STRUCT_* method, ref CORINFO_JUST_MY_CODE_HANDLE_* ppIndirection, CORINFO_JUST_MY_CODE_HANDLE_* result)
    {
        GetJustMyCodeHandle ??= new();
    
        DLDL value = default;
        value.A = CastPointer(ppIndirection);
        value.B = CastHandle(result);
    
        ulong key = CastHandle(method);
        GetJustMyCodeHandle.Add(key, value);
    }

    public void recGetFunctionEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags)
    {
        GetFunctionEntryPoint ??= new();
    
        DLD key = default;
        key.A   = CastHandle(ftn);
        key.B   = (uint)accessFlags;
    
        DLD value = default;
        value.A = CastPointer(pResult.addr); // First union member
        value.B = (uint)pResult.accessType;
    
        GetFunctionEntryPoint.Add(key, value);
    }

    public void recConstructStringLiteral(CORINFO_MODULE_STRUCT_* moduleHandle, uint metaTok, void* pValue, InfoAccessType result)
    {
        ConstructStringLiteral ??= new();
    
        DLD key = default;
        key.A = CastHandle(moduleHandle);
        key.B = (uint)metaTok;
    
        DLD value = default;
        value.A = CastPointer(pValue);
        value.B = (uint)result;
    
        ConstructStringLiteral.Add(key, value);
    }

    public void recConvertPInvokeCalliToCall(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fMustConvert, bool result)
    {
        ConvertPInvokeCalliToCall ??= new();
    
        DLD key = default;
        key.A = CastHandle(pResolvedToken.tokenScope);
        key.B = (uint)pResolvedToken.token;
    
        DLDL value = default;
        value.A = result ? CastHandle(pResolvedToken.hClass) : 0;
        value.B = result ? CastHandle(pResolvedToken.hMethod) : 0;
    
        ConvertPInvokeCalliToCall.Add(key, value);
    }

    public void recEmptyStringLiteral(ref void* pValue, InfoAccessType result)
    {
        EmptyStringLiteral ??= new();
        DLD temp2 = default;
        temp2.A = CastPointer(pValue);
        temp2.B = (uint)result;
    
        EmptyStringLiteral.Append(temp2);
    }

    public void recGetArgType(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_** vcTypeRet, CorInfoTypeWithMod result, uint exceptionCode)
    {
        GetArgType ??= new();
    
        Agnostic_GetArgType_Key key = default;
    
        // Only setting values for CORINFO_SIG_INFO things the EE seems to pay attention to... this is necessary since some of the values
        // are unset and fail our precise comparisons ...
        // TODO: verify that the above comment is still true (that some of the fields of incoming argument `sig` contain garbage), or
        // can we store the whole thing and use StoreAgnostic_CORINFO_SIG_INFO()?
    
        key.flags           = (uint)sig->flags;
        key.numArgs         = (uint)sig->numArgs;
    
        StoreSigInstHandles(sig->sigInst.classInstCount, sig->sigInst.classInst, out key.sigInst_classInstCount, out key.sigInst_classInst_Index);
        StoreSigInstHandles(sig->sigInst.methInstCount, sig->sigInst.methInst, out key.sigInst_methInstCount, out key.sigInst_methInst_Index);
    
        key.methodSignature = CastPointer(sig->methodSignature);
        key.scope           = CastHandle(sig->scope);
        key.args            = CastHandle(args);
    
        Agnostic_GetArgType_Value value = default;
        value.vcTypeRet     = CastHandle(*vcTypeRet);
        value.result        = (uint)result;
        value.exceptionCode = (uint)exceptionCode;
    
        GetArgType.Add(key, value);
    }

    public void recGetExactClasses(CORINFO_CLASS_STRUCT_* baseType, int maxExactClasses, CORINFO_CLASS_STRUCT_** exactClsRet, int result)
    {
        GetExactClasses ??= new();

        DLD key = default;
        key.A = CastHandle(baseType);
        key.B = (uint)maxExactClasses;

        int numResults = result < 0 ? 0 : result;

        var exactClassesAgnostic = new ulong[numResults];
        for (int i = 0; i < numResults; i++)
            exactClassesAgnostic[i] = CastHandle(exactClsRet[i]);

        Agnostic_GetExactClassesResult value = default;
        value.numClasses = result;
        value.classes = GetExactClasses.AddBuffer(MemoryMarshal.AsBytes(exactClassesAgnostic.AsSpan()));

        GetExactClasses.Add(key, value);
    }

    public void recGetArgNext(CORINFO_ARG_LIST_STRUCT_* args, CORINFO_ARG_LIST_STRUCT_* result)
    {
        GetArgNext ??= new();
    
        ulong key = CastHandle(args);
        ulong value = CastHandle(result);
        GetArgNext.Add(key, value);
    }

    public void recGetMethodSig(CORINFO_METHOD_STRUCT_* ftn, CORINFO_SIG_INFO* sig, CORINFO_CLASS_STRUCT_* memberParent)
    {
        GetMethodSig ??= new();
    
        DLDL key = default;
        key.A = CastHandle(ftn);
        key.B = CastHandle(memberParent);
    
        Agnostic_CORINFO_SIG_INFO value = StoreSig(*sig, GetMethodSig);
    
        GetMethodSig.Add(key, value);
    }

    public void recGetArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_STRUCT_* args, CORINFO_CLASS_STRUCT_* result, uint exceptionCode)
    {
        GetArgClass ??= new();
    
        Agnostic_GetArgClass_Key key = default;
    
        // Only setting values for CORINFO_SIG_INFO things the EE seems to pay attention to... this is necessary since some of the values
        // are unset and fail our precise comparisons...
    
        StoreSigInstHandles(sig->sigInst.classInstCount, sig->sigInst.classInst, out key.sigInst_classInstCount, out key.sigInst_classInst_Index);
        StoreSigInstHandles(sig->sigInst.methInstCount, sig->sigInst.methInst, out key.sigInst_methInstCount, out key.sigInst_methInst_Index);
    
        key.methodSignature = CastPointer(sig->methodSignature);
        key.scope           = CastHandle(sig->scope);
        key.args            = CastHandle(args);
    
        Agnostic_GetArgClass_Value value = default;
        value.result        = CastHandle(result);
        value.exceptionCode = exceptionCode;
    
        GetArgClass.Add(key, value);
    }

    public void recGetHFAType(CORINFO_CLASS_STRUCT_* clsHnd, CorInfoHFAElemType result)
    {
        GetHFAType ??= new();
    
        ulong key = CastHandle(clsHnd);
        uint value = (uint)result;
        GetHFAType.Add(key, value);
    }

    public void recGetMethodInfo(CORINFO_METHOD_STRUCT_* ftn, CORINFO_METHOD_INFO* info, CORINFO_CONTEXT_STRUCT* context, bool result, uint exceptionCode)
    {
        GetMethodInfo ??= new();
    
        Agnostic_GetMethodInfo value = default;
    
        if (result)
        {
            value.info.ftn           = CastHandle(info->ftn);
            value.info.scope         = CastHandle(info->scope);
            value.info.ILCode_offset = (uint)GetMethodInfo.AddBuffer(Bytes(info->ILCode, info->ILCodeSize));
            value.info.ILCodeSize    = (uint)info->ILCodeSize;
            value.info.maxStack      = (uint)info->maxStack;
            value.info.EHcount       = (uint)info->EHcount;
            value.info.options       = (uint)info->options;
            value.info.regionKind    = (uint)info->regionKind;
    
            value.info.args   = StoreSig(info->args, GetMethodInfo);
            value.info.locals = StoreSig(info->locals, GetMethodInfo);
        }
        value.result        = result ? (byte)1 : (byte)0;
        value.exceptionCode = (uint)exceptionCode;
    
        DLDL key = default;
        key.A = CastHandle(ftn);
        key.B = CastHandle(context);
        GetMethodInfo.Add(key, value);
    }

    public void recHaveSameMethodDefinition(CORINFO_METHOD_STRUCT_* methHnd1, CORINFO_METHOD_STRUCT_* methHnd2, bool result)
    {
        HaveSameMethodDefinition ??= new();
    
        DLDL key = default;
        key.A = CastHandle(methHnd1);
        key.B = CastHandle(methHnd2);
    
        uint value = result ? 1u : 0u;
        HaveSameMethodDefinition.Add(key, value);
    }

    public void recGetTypeDefinition(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetTypeDefinition ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetTypeDefinition.Add(key, value);
    }

    public void recGetNewHelper(CORINFO_CLASS_STRUCT_* classHandle, bool hasSideEffects, CorInfoHelpFunc result, uint exceptionCode)
    {
        GetNewHelper ??= new();
    
        ulong key = CastHandle(classHandle);
    
        DDD value = default;
        value.A = hasSideEffects ? 1u : 0u;
        value.B = (uint)result;
        value.C = exceptionCode;
    
        GetNewHelper.Add(key, value);
    }

    public void recEmbedGenericHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fEmbedParent, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_GENERICHANDLE_RESULT pResult)
    {
        EmbedGenericHandle ??= new();
    
        Agnostic_EmbedGenericHandle key = default;
        key.ResolvedToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, EmbedGenericHandle);
        key.fEmbedParent  = (fEmbedParent ? 1u : 0u);
        key.hCallerHandle = CastHandle(callerHandle);
    
        Agnostic_CORINFO_GENERICHANDLE_RESULT value = default;
        value.lookup            = StoreAgnostic_CORINFO_LOOKUP(ref pResult.lookup);
        value.compileTimeHandle = CastHandle(pResult.compileTimeHandle);
        value.handleType        = (uint)pResult.handleType;
    
        EmbedGenericHandle.Add(key, value);
    }

    public void recGetEHinfo(CORINFO_METHOD_STRUCT_* ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause)
    {
        GetEHinfo ??= new();
    
        DLD key = default;
        key.A = CastHandle(ftn);
        key.B = (uint)EHnumber;
    
        Agnostic_CORINFO_EH_CLAUSE value = default;
        value.Flags         = (uint)clause.Flags;
        value.TryOffset     = (uint)clause.TryOffset;
        value.TryLength     = (uint)clause.TryLength;
        value.HandlerOffset = (uint)clause.HandlerOffset;
        value.HandlerLength = (uint)clause.HandlerLength;
        value.ClassToken    = (uint)clause.ClassTokenOrOffset;
    
        GetEHinfo.Add(key, value);
    }

    public void recGetMethodVTableOffset(CORINFO_METHOD_STRUCT_* method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, ref bool isRelative)
    {
        GetMethodVTableOffset ??= new();
    
        DDD value = default;
        value.A = (uint)offsetOfIndirection;
        value.B = (uint)offsetAfterIndirection;
        value.C = isRelative ? 1u : 0u;
    
        ulong key = CastHandle(method);
        GetMethodVTableOffset.Add(key, value);
    }

    public void recResolveVirtualMethod(CORINFO_DEVIRTUALIZATION_INFO* info, bool returnValue)
    {
        if (ResolveVirtualMethod == null)
        {
            ResolveVirtualMethod = new LightWeightMap<Agnostic_ResolveVirtualMethodKey, Agnostic_ResolveVirtualMethodResult>();
        }
    
        Agnostic_ResolveVirtualMethodKey key = default;
        key.virtualMethod  = CastHandle(info->virtualMethod);
        key.objClass       = CastHandle(info->objClass);
        key.context        = CastHandle(info->context);
    
        key.pResolvedTokenVirtualMethodNonNull = info->pResolvedTokenVirtualMethod != null ? 1u : 0u;
        if (key.pResolvedTokenVirtualMethodNonNull != 0)
            key.pResolvedTokenVirtualMethod = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref *info->pResolvedTokenVirtualMethod, ResolveToken);
    
        Agnostic_ResolveVirtualMethodResult result = default;
        result.returnValue         = returnValue ? (byte)1 : (byte)0;
        result.devirtualizedMethod = CastHandle(info->devirtualizedMethod);
        result.tokenLookupContext  = CastHandle(info->tokenLookupContext);
        result.detail              = (uint)info->detail;
        result.instParamLookup     = StoreAgnostic_CORINFO_LOOKUP(ref info->instParamLookup);
    
        if (returnValue)
        {
            result.resolvedTokenDevirtualizedMethod        = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref info->resolvedTokenDevirtualizedMethod, ResolveToken);
            result.resolvedTokenDevirtualizedUnboxedMethod = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref info->resolvedTokenDevirtualizedUnboxedMethod, ResolveToken);
        }
        else
        {
            result.resolvedTokenDevirtualizedMethod = default;
            result.resolvedTokenDevirtualizedUnboxedMethod = default;
        }
    
        ResolveVirtualMethod.Add(key, result);
    }

    public void recGetAsyncOtherVariant(CORINFO_METHOD_STRUCT_* ftn, bool variantIsThunk, CORINFO_METHOD_STRUCT_* result)
    {
        if (GetAsyncOtherVariant == null)
        {
            GetAsyncOtherVariant = new LightWeightMap<ulong, DLD>();
        }
    
        ulong key = CastHandle(ftn);
        DLD value = default;
        value.A = CastHandle(result);
        value.B = variantIsThunk ? 1u : 0u;
        GetAsyncOtherVariant.Add(key, value);
    }

    public void recGetDefaultComparerClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetDefaultComparerClass ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetDefaultComparerClass.Add(key, value);
    }

    public void recGetDefaultEqualityComparerClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetDefaultEqualityComparerClass ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetDefaultEqualityComparerClass.Add(key, value);
    }

    public void recGetSZArrayHelperEnumeratorClass(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* result)
    {
        GetSZArrayHelperEnumeratorClass ??= new();
    
        ulong key = CastHandle(cls);
        ulong value = CastHandle(result);
        GetSZArrayHelperEnumeratorClass.Add(key, value);
    }

    public void recGetTokenTypeAsHandle(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_CLASS_STRUCT_* result)
    {
        GetTokenTypeAsHandle ??= new();
    
        GetTokenTypeAsHandleValue key = default;
        key.hMethod = CastHandle(pResolvedToken.hMethod);
        key.hField  = CastHandle(pResolvedToken.hField);
    
        ulong value = CastHandle(result);
        GetTokenTypeAsHandle.Add(key, value);
    }

    public void recGetFieldInfo(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, CORINFO_ACCESS_FLAGS flags, CORINFO_FIELD_INFO* pResult)
    {
        GetFieldInfo ??= new();
    
        Agnostic_GetFieldInfo key = default;
        key.ResolvedToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetFieldInfo);
        key.callerHandle  = CastHandle(callerHandle);
        key.flags         = (uint)flags;
    
        Agnostic_CORINFO_FIELD_INFO value = default;
        value.fieldAccessor                 = (uint)pResult->fieldAccessor;
        value.fieldFlags                    = (uint)pResult->fieldFlags;
        value.helper                        = (uint)pResult->helper;
        value.offset                        = (uint)pResult->offset;
        value.fieldType                     = (uint)pResult->fieldType;
        value.structType                    = CastHandle(pResult->structType);
        value.accessAllowed                 = (uint)pResult->accessAllowed;
        value.accessCalloutHelper           = StoreAgnostic_CORINFO_HELPER_DESC(pResult->accessCalloutHelper);
        value.fieldLookup                   = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pResult->fieldLookup);
        GetFieldInfo.Add(key, value);
    }

    public void recGetThreadLocalFieldInfo(CORINFO_FIELD_STRUCT_* field, bool isGCType, uint result)
    {
        GetThreadLocalFieldInfo ??= new();
    
        DLD key = default;
        key.A = CastHandle(field);
        key.B = isGCType ? 0u : 1u;
        GetThreadLocalFieldInfo.Add(key, result);
    }

    public void recGetThreadLocalStaticBlocksInfo(CORINFO_THREAD_STATIC_BLOCKS_INFO* pInfo)
    {
        GetThreadLocalStaticBlocksInfo ??= new();
    
        Agnostic_GetThreadLocalStaticBlocksInfo value = default;
    
        value.tlsIndex                              = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pInfo->tlsIndex);
        value.tlsGetAddrFtnPtr                      = CastPointer(pInfo->tlsGetAddrFtnPtr);
        value.tlsIndexObject                        = CastPointer(pInfo->tlsIndexObject);
        value.threadVarsSection                     = CastPointer(pInfo->threadVarsSection);
        value.offsetOfThreadLocalStoragePointer     = pInfo->offsetOfThreadLocalStoragePointer;
        value.offsetOfMaxThreadStaticBlocks         = pInfo->offsetOfMaxThreadStaticBlocks;
        value.offsetOfThreadStaticBlocks            = pInfo->offsetOfThreadStaticBlocks;
        value.offsetOfBaseOfThreadLocalData         = pInfo->offsetOfBaseOfThreadLocalData;
    
        // This data is same for entire process, so just add it against key '0'.
        uint key = 0;
        GetThreadLocalStaticBlocksInfo.Add(key, value);
    }

    public void recGetThreadLocalStaticInfo_NativeAOT(CORINFO_THREAD_STATIC_INFO_NATIVEAOT* pInfo)
    {
        GetThreadLocalStaticInfo_NativeAOT ??= new();
    
        Agnostic_GetThreadStaticInfo_NativeAOT value = default;
        value.tlsRootObject       = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pInfo->tlsRootObject);
        value.tlsIndexObject      = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pInfo->tlsIndexObject);
        value.offsetOfThreadLocalStoragePointer       = pInfo->offsetOfThreadLocalStoragePointer;
        value.threadStaticBaseSlow       = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pInfo->threadStaticBaseSlow);
        value.tlsGetAddrFtnPtr           = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pInfo->tlsGetAddrFtnPtr);
    
        ulong key = 1;
    
        GetThreadLocalStaticInfo_NativeAOT.Add(key, value);
    }

    public void recEmbedMethodHandle(CORINFO_METHOD_STRUCT_* handle, ref void* ppIndirection, CORINFO_METHOD_STRUCT_* result)
    {
        EmbedMethodHandle ??= new();
    
        DLDL value = default;
        if (IsNullRef(ref ppIndirection))
            value.A = 0;
        else
            value.A = CastPointer(ppIndirection);
        value.B     = CastHandle(result);
    
        ulong key = CastHandle(handle);
        EmbedMethodHandle.Add(key, value);
    }

    public void recGetStaticFieldContent(CORINFO_FIELD_STRUCT_* field, byte* buffer, int bufferSize, int valueOffset, bool ignoreMovableObjects, bool result)
    {
        GetStaticFieldContent ??= new();
    
        DLDDD key = default;
        key.A = CastHandle(field);
        key.B = (uint)bufferSize;
        key.C = (ignoreMovableObjects ? 1u : 0u);
        key.D = (uint)valueOffset;
    
        uint tmpBuf = uint.MaxValue;
        if (buffer != null && result)
            tmpBuf = (uint)GetStaticFieldContent.AddBuffer(Bytes((byte*)buffer, (uint)bufferSize));
    
        DD value = default;
        value.A = (result ? 1u : 0u);
        value.B = (uint)tmpBuf;
    
        GetStaticFieldContent.Add(key, value);
    }

    public void recGetObjectContent(CORINFO_OBJECT_STRUCT_* obj, byte* buffer, int bufferSize, int valueOffset, bool result)
    {
        GetObjectContent ??= new();
    
        DLDD key = default;
        key.A = CastHandle(obj);
        key.B = (uint)bufferSize;
        key.C = (uint)valueOffset;
    
        uint tmpBuf = uint.MaxValue;
        if (buffer != null && result)
            tmpBuf = (uint)GetObjectContent.AddBuffer(Bytes((byte*)buffer, (uint)bufferSize));
    
        DD value = default;
        value.A = (result ? 1u : 0u);
        value.B = (uint)tmpBuf;
    
        GetObjectContent.Add(key, value);
    }

    public void recGetStaticFieldCurrentClass(CORINFO_FIELD_STRUCT_* field, bool* pIsSpeculative, CORINFO_CLASS_STRUCT_* result)
    {
        GetStaticFieldCurrentClass ??= new();
    
        DLD key = default;
        key.A = CastHandle(field);
        key.B = pIsSpeculative != null ? 1u : 0u;
    
        Agnostic_GetStaticFieldCurrentClass value = default;
        value.classHandle   = CastHandle(result);
        value.isSpeculative = pIsSpeculative != null && *pIsSpeculative ? (byte)1 : (byte)0;
    
        GetStaticFieldCurrentClass.Add(key, value);
    }

    public void recGetClassGClayout(CORINFO_CLASS_STRUCT_* cls, byte* gcPtrs, uint len, uint result)
    {
        GetClassGClayout ??= new();
    
        Agnostic_GetClassGClayout value = default;
    
        value.gcPtrs_Index = (uint)GetClassGClayout.AddBuffer(Bytes(gcPtrs, len * sizeof(byte)));
        value.len          = (uint)len;
        value.valCount     = (uint)result;
    
        ulong key = CastHandle(cls);
        GetClassGClayout.Add(key, value);
    }

    public void recGetClassAlignmentRequirement(CORINFO_CLASS_STRUCT_* cls, bool fDoubleAlignHint, uint result)
    {
        GetClassAlignmentRequirement ??= new();
    
        DLD key = default;
        key.A = CastHandle(cls);
        key.B = (fDoubleAlignHint ? 1u : 0u);
    
        uint value = (uint)result;
        GetClassAlignmentRequirement.Add(key, value);
    }

    public void recCanAccessClass(ref CORINFO_RESOLVED_TOKEN pResolvedToken, CORINFO_METHOD_STRUCT_* callerHandle, ref CORINFO_HELPER_DESC pAccessHelper, CorInfoIsAccessAllowedResult result)
    {
        CanAccessClass ??= new();
    
        Agnostic_CanAccessClassIn key = default;
        key.ResolvedToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, CanAccessClass);
        key.callerHandle  = CastHandle(callerHandle);
    
        Agnostic_CanAccessClassOut value = default;
        value.AccessHelper = StoreAgnostic_CORINFO_HELPER_DESC(pAccessHelper);
        value.result = (uint)result;
    
        CanAccessClass.Add(key, value);
    }

    public void recGetCastingHelper(ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool fThrowing, CorInfoHelpFunc result)
    {
        GetCastingHelper ??= new();
    
        Agnostic_GetCastingHelper key = default;
        key.hClass    = CastHandle(pResolvedToken.hClass);
        key.fThrowing = (fThrowing ? 1u : 0u);
    
        uint value = (uint)result;
        GetCastingHelper.Add(key, value);
    }

    public void recEmbedModuleHandle(CORINFO_MODULE_STRUCT_* handle, ref void* ppIndirection, CORINFO_MODULE_STRUCT_* result)
    {
        EmbedModuleHandle ??= new();
    
        DLDL value = default;
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = CastHandle(result);
    
        ulong key = CastHandle(handle);
        EmbedModuleHandle.Add(key, value);
    }

    public void recEmbedClassHandle(CORINFO_CLASS_STRUCT_* handle, ref void* ppIndirection, CORINFO_CLASS_STRUCT_* result)
    {
        EmbedClassHandle ??= new();
    
        DLDL value = default;
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = CastHandle(result);
    
        ulong key = CastHandle(handle);
        EmbedClassHandle.Add(key, value);
    }

    public void recPInvokeMarshalingRequired(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, bool result)
    {
        PInvokeMarshalingRequired ??= new();
    
        MethodOrSigInfoValue key = default;
        key.method     = CastHandle(method);
        key.pSig_Index = (uint)PInvokeMarshalingRequired.AddBuffer(Bytes(callSiteSig->pSig, callSiteSig->cbSig));
        key.cbSig      = (uint)callSiteSig->cbSig;
        key.scope      = CastHandle(callSiteSig->scope);
    
        uint value = result ? 1u : 0u;
        PInvokeMarshalingRequired.Add(key, value);
    }

    public void recGetUnmanagedCallConv(CORINFO_METHOD_STRUCT_* method, CORINFO_SIG_INFO* callSiteSig, CorInfoCallConvExtension result, bool suppressGCTransitionResult)
    {
        GetUnmanagedCallConv ??= new();
    
        MethodOrSigInfoValue key = default;
        key.method = CastHandle(method);
        if (callSiteSig != null)
        {
            key.pSig_Index = (uint)GetUnmanagedCallConv.AddBuffer(Bytes(callSiteSig->pSig, callSiteSig->cbSig));
            key.cbSig = (uint)callSiteSig->cbSig;
            key.scope = CastHandle(callSiteSig->scope);
        }
        else
        {
            key.pSig_Index = 0;
            key.cbSig = 0;
            key.scope = 0;
        }
    
        DD value = default;
        value.A = (uint)result;
        value.B = (suppressGCTransitionResult ? 1u : 0u);
    
        GetUnmanagedCallConv.Add(key, value);
    }

    public void recFindSig(CORINFO_MODULE_STRUCT_* moduleHandle, uint sigTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        FindSig ??= new();
    
        Agnostic_FindSig key = default;
        key.module  = CastHandle(moduleHandle);
        key.sigTOK  = (uint)sigTOK;
        key.context = CastHandle(context);
    
        Agnostic_CORINFO_SIG_INFO value = StoreSig(*sig, FindSig);
    
        FindSig.Add(key, value);
    }

    public void recGetEEInfo(ref CORINFO_EE_INFO pEEInfoOut)
    {
        GetEEInfo ??= new();
    
        Agnostic_CORINFO_EE_INFO value = default;
    
        value.inlinedCallFrameInfo.size                  = (uint)pEEInfoOut.inlinedCallFrameInfo.size;
        value.inlinedCallFrameInfo.sizeWithSecretStubArg = (uint)pEEInfoOut.inlinedCallFrameInfo.sizeWithSecretStubArg;
        value.inlinedCallFrameInfo.offsetOfFrameLink     = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfFrameLink;
        value.inlinedCallFrameInfo.offsetOfCallSiteSP    = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfCallSiteSP;
        value.inlinedCallFrameInfo.offsetOfCalleeSavedFP = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfCalleeSavedFP;
        value.inlinedCallFrameInfo.offsetOfCallTarget    = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfCallTarget;
        value.inlinedCallFrameInfo.offsetOfReturnAddress = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfReturnAddress;
        value.inlinedCallFrameInfo.offsetOfSecretStubArg = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfSecretStubArg;
        value.inlinedCallFrameInfo.offsetOfSPAfterProlog = (uint)pEEInfoOut.inlinedCallFrameInfo.offsetOfSPAfterProlog;
        value.offsetOfThreadFrame                        = (uint)pEEInfoOut.offsetOfThreadFrame;
        value.offsetOfGCState                            = (uint)pEEInfoOut.offsetOfGCState;
        value.offsetOfDelegateInstance                   = (uint)pEEInfoOut.offsetOfDelegateInstance;
        value.offsetOfDelegateFirstTarget                = (uint)pEEInfoOut.offsetOfDelegateFirstTarget;
        value.sizeOfReversePInvokeFrame                  = (uint)pEEInfoOut.sizeOfReversePInvokeFrame;
        value.osPageSize                                 = (uint)pEEInfoOut.osPageSize;
        value.maxUncheckedOffsetForNullObject            = (uint)pEEInfoOut.maxUncheckedOffsetForNullObject;
        value.targetAbi                                  = (uint)pEEInfoOut.targetAbi;
        value.osType                                     = (uint)pEEInfoOut.osType;
    
        GetEEInfo.Add(0, value);
    }

    public void recGetAsyncInfo(ref CORINFO_ASYNC_INFO pAsyncInfo)
    {
        GetAsyncInfo ??= new();
    
        Agnostic_CORINFO_ASYNC_INFO value = default;
    
        value.continuationClsHnd = CastHandle(pAsyncInfo.continuationClsHnd);
        value.continuationNextFldHnd = CastHandle(pAsyncInfo.continuationNextFldHnd);
        value.continuationResumeInfoFldHnd = CastHandle(pAsyncInfo.continuationResumeInfoFldHnd);
        value.continuationStateFldHnd = CastHandle(pAsyncInfo.continuationStateFldHnd);
        value.continuationFlagsFldHnd = CastHandle(pAsyncInfo.continuationFlagsFldHnd);
        value.captureExecutionContextMethHnd = CastHandle(pAsyncInfo.captureExecutionContextMethHnd);
        value.captureContinuationContextMethHnd = CastHandle(pAsyncInfo.captureContinuationContextMethHnd);
        value.captureContextsMethHnd = CastHandle(pAsyncInfo.captureContextsMethHnd);
        value.restoreContextsMethHnd = CastHandle(pAsyncInfo.restoreContextsMethHnd);
        value.restoreContextsOnSuspensionMethHnd = CastHandle(pAsyncInfo.restoreContextsOnSuspensionMethHnd);
        value.restoreInlinedFrameContextsMethHnd = CastHandle(pAsyncInfo.restoreInlinedFrameContextsMethHnd);
        value.captureInlinedFrameTransitionWithContinuationContextMethHnd = CastHandle(pAsyncInfo.captureInlinedFrameTransitionWithContinuationContextMethHnd);
        value.captureInlinedFrameTransitionNoContinuationContextMethHnd = CastHandle(pAsyncInfo.captureInlinedFrameTransitionNoContinuationContextMethHnd);
        value.captureInlinedFrameTransitionContinueOnThreadPoolMethHnd = CastHandle(pAsyncInfo.captureInlinedFrameTransitionContinueOnThreadPoolMethHnd);
        value.finishSuspensionNoContinuationContextMethHnd = CastHandle(pAsyncInfo.finishSuspensionNoContinuationContextMethHnd);
        value.finishSuspensionWithContinuationContextMethHnd = CastHandle(pAsyncInfo.finishSuspensionWithContinuationContextMethHnd);
    
        GetAsyncInfo.Add(0, value);
    }

    public void recGetWasmWellKnownGlobals(ref CORINFO_WASM_WELLKNOWN_GLOBALS pBaseGlobals)
    {
        GetWasmWellKnownGlobals ??= new();
    
        Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS value = default;
    
        value.stackPointer = CastHandle(pBaseGlobals.stackPointer);
        value.imageBase    = CastHandle(pBaseGlobals.imageBase);
        value.tableBase    = CastHandle(pBaseGlobals.tableBase);
        value.asyncContinuation = CastHandle(pBaseGlobals.asyncContinuation);
    
        GetWasmWellKnownGlobals.Add(0, value);
    }

    public void recGetAwaitReturnCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg, CORINFO_METHOD_STRUCT_* methHnd)
    {
        GetAwaitReturnCall ??= new();
    
        Agnostic_GetAwaitReturnCallResult value = default;
        value.methodHnd = CastHandle(methHnd);
        value.contextHandle = CastHandle(*contextHandle);
        value.instArg = StoreAgnostic_CORINFO_LOOKUP(ref instArg);
    
        GetAwaitReturnCall.Add(CastHandle(callerHnd), value);
    }

    public void recGetAwaitAwaiterInContinuationCall(CORINFO_METHOD_STRUCT_* callerHnd, ref CORINFO_RESOLVED_TOKEN pResolvedToken, bool isUnsafe, CORINFO_CONTEXT_STRUCT** contextHandle, ref CORINFO_LOOKUP instArg, CORINFO_METHOD_STRUCT_* methHnd)
    {
        if (GetAwaitAwaiterInContinuationCall == null)
        {
            GetAwaitAwaiterInContinuationCall =
                new LightWeightMap<Agnostic_GetAwaitAwaiterInContinuationCall, Agnostic_GetAwaitReturnCallResult>();
        }
    
        Agnostic_GetAwaitAwaiterInContinuationCall key = default;
        key.callerHnd = CastHandle(callerHnd);
        key.ResolvedToken =
            StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref pResolvedToken, GetAwaitAwaiterInContinuationCall);
        key.isUnsafe = isUnsafe ? 1u : 0u;
    
        Agnostic_GetAwaitReturnCallResult value = default;
        value.methodHnd = CastHandle(methHnd);
        value.contextHandle = CastHandle(*contextHandle);
        value.instArg = StoreAgnostic_CORINFO_LOOKUP(ref instArg);
    
        GetAwaitAwaiterInContinuationCall.Add(key, value);
    }

    public void recGetGSCookie(nint* pCookieVal, nint** ppCookieVal)
    {
        GetGSCookie ??= new();
    
        DLDL value = default;
    
        if (pCookieVal != null)
            value.A = (ulong)*pCookieVal;
        else
            value.A = 0;
    
        if (ppCookieVal != null)
            value.B = CastPointer(*ppCookieVal);
        else
            value.B = 0;
    
        GetGSCookie.Add(0, value);
    }

    public void recGetOSRInfo(PatchpointInfo* patchpointInfo, ref uint ilOffset)
    {
        GetOSRInfo ??= new();

        Agnostic_GetOSRInfo value = default;

        value.index = GetOSRInfo.AddBuffer(Bytes(patchpointInfo, PatchpointInfoSize(patchpointInfo)));
        value.ilOffset = ilOffset;

        // use 0 for key
        uint key = 0;
        GetOSRInfo.Add(key, value);
    }

    public void recGetClassModuleIdForStatics(CORINFO_CLASS_STRUCT_* cls, CORINFO_MODULE_STRUCT_** pModule, void** ppIndirection, nuint result)
    {
        GetClassModuleIdForStatics ??= new();
    
        Agnostic_GetClassModuleIdForStatics value = default;
    
        if (pModule != null)
            value.Module = CastHandle(*pModule);
        else
            value.Module = 0;
        if (ppIndirection != null)
            value.pIndirection = CastPointer(*ppIndirection);
        else
            value.pIndirection = 0;
        value.result           = (ulong)result;
    
        ulong key = CastHandle(cls);
        GetClassModuleIdForStatics.Add(key, value);
    }

    public void recGetIsClassInitedFlagAddress(CORINFO_CLASS_STRUCT_* cls, ref CORINFO_CONST_LOOKUP addr, ref int offset, bool result)
    {
        GetIsClassInitedFlagAddress ??= new();
    
        Agnostic_GetIsClassInitedFlagAddress value = default;
        value.addr.handle = CastHandle(addr.addr);
        value.addr.accessType = (uint)addr.accessType;
        value.offset = (uint)offset;
        value.result = (result ? 1u : 0u);
    
        ulong key = CastHandle(cls);
        GetIsClassInitedFlagAddress.Add(key, value);
    }

    public void recGetStaticBaseAddress(CORINFO_CLASS_STRUCT_* cls, bool isGc, ref CORINFO_CONST_LOOKUP addr, bool result)
    {
        GetStaticBaseAddress ??= new();
    
        Agnostic_GetStaticBaseAddress value = default;
        value.addr.handle = CastHandle(addr.addr);
        value.addr.accessType = (uint)addr.accessType;
        value.result = result ? 1u : 0u;
    
        DLD key = default;
        key.A = CastHandle(cls);
        key.B = (isGc ? 1u : 0u);
    
        GetStaticBaseAddress.Add(key, value);
    }

    public void recGetThreadTLSIndex(ref void* ppIndirection, uint result)
    {
        GetThreadTLSIndex ??= new();
    
        DLD value = default;
    
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = (uint)result;
    
        GetThreadTLSIndex.Add(0, value);
    }

    public void recGetAddrOfCaptureThreadGlobal(ref void* ppIndirection, int* result)
    {
        GetAddrOfCaptureThreadGlobal ??= new();
    
        DLDL value = default;
    
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = CastPointer(result);
    
        GetAddrOfCaptureThreadGlobal.Add(0, value);
    }

    public void recGetClassStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls, void* result)
    {
        GetClassStaticDynamicInfo ??= new();
    
        DLD value = default;
    
        value.A = CastPointer(result);
        value.B = 0;
    
        ulong key = CastHandle(cls);
        GetClassStaticDynamicInfo.Add(key, value);
    }

    public void recGetClassThreadStaticDynamicInfo(CORINFO_CLASS_STRUCT_* cls, void* result)
    {
        GetClassThreadStaticDynamicInfo ??= new();
    
        DLD value = default;
    
        value.A = CastPointer(result);
        value.B = 0;
    
        ulong key = CastHandle(cls);
        GetClassThreadStaticDynamicInfo.Add(key, value);
    }

    public void recGetLocationOfThisType(CORINFO_METHOD_STRUCT_* context, ref CORINFO_LOOKUP_KIND result)
    {
        GetLocationOfThisType ??= new();
    
        Agnostic_CORINFO_LOOKUP_KIND value = CreateAgnostic_CORINFO_LOOKUP_KIND(ref result);
    
        ulong key = CastHandle(context);
        GetLocationOfThisType.Add(key, value);
    }

    public void recGetDelegateCtor(CORINFO_METHOD_STRUCT_* methHnd, CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_METHOD_STRUCT_* targetMethodHnd, ref DelegateCtorArgs pCtorData, CORINFO_METHOD_STRUCT_* result)
    {
        GetDelegateCtor ??= new();
    
        Agnostic_GetDelegateCtorIn key = default;
        key.methHnd         = CastHandle(methHnd);
        key.clsHnd          = CastHandle(clsHnd);
        key.targetMethodHnd = CastHandle(targetMethodHnd);
    
        Agnostic_GetDelegateCtorOut value = default;
        value.CtorData.pMethod = CastPointer(pCtorData.pMethod);
        value.CtorData.pArg3   = CastPointer(pCtorData.pArg3);
        value.CtorData.pArg4   = CastPointer(pCtorData.pArg4);
        value.CtorData.pArg5   = CastPointer(pCtorData.pArg5);
        value.result           = CastHandle(result);
    
        GetDelegateCtor.Add(key, value);
    }

    public void recGetFunctionFixedEntryPoint(CORINFO_METHOD_STRUCT_* ftn, ref CORINFO_CONST_LOOKUP pResult)
    {
        GetFunctionFixedEntryPoint ??= new();
    
        Agnostic_CORINFO_CONST_LOOKUP value = StoreAgnostic_CORINFO_CONST_LOOKUP(ref pResult);
    
        ulong key = CastHandle(ftn);
        GetFunctionFixedEntryPoint.Add(key, value);
    }

    public void recGetFieldInClass(CORINFO_CLASS_STRUCT_* clsHnd, int num, CORINFO_FIELD_STRUCT_* result)
    {
        GetFieldInClass ??= new();
    
        DLD key = default;
        key.A = CastHandle(clsHnd);
        key.B = (uint)num;
    
        ulong value = CastHandle(result);
        GetFieldInClass.Add(key, value);
    }

    public void recGetTypeLayout(GetTypeLayoutResult result, CORINFO_CLASS_STRUCT_* typeHnd, CORINFO_TYPE_LAYOUT_NODE* nodes, nuint numNodes)
    {
        GetTypeLayout ??= new();

        ulong key = CastHandle(typeHnd);

        int index = GetTypeLayout.GetIndex(key);
        if (index != -1)
        {
            // If we already have this then just skip.
            Agnostic_GetTypeLayoutResult existing = GetTypeLayout.GetItem(index);
            if ((existing.result != (uint)GetTypeLayoutResult.Partial) ||
                (result == GetTypeLayoutResult.Failure) ||
                (numNodes <= existing.numNodes))
            {
                // No new data to add
                return;
            }

            // Otherwise fall through and update the map with more information.
        }

        Agnostic_GetTypeLayoutResult value = default;

        value.result = (uint)result;
        if (result == GetTypeLayoutResult.Failure)
        {
            value.nodesBuffer = uint.MaxValue;
        }
        else
        {
            var agnosticFields = new Agnostic_CORINFO_TYPE_LAYOUT_NODE[(int)numNodes];
            for (int i = 0; i < agnosticFields.Length; i++)
                agnosticFields[i] = StoreAgnostic_CORINFO_TYPE_LAYOUT_NODE(nodes[i]);

            value.nodesBuffer = GetTypeLayout.AddBuffer(MemoryMarshal.AsBytes(agnosticFields.AsSpan()));
            value.numNodes = (uint)numNodes;
        }

        if (index != -1)
        {
            GetTypeLayout.Update(index, value);
        }
        else
        {
            GetTypeLayout.Add(key, value);
        }
    }

    public void recGetFieldType(CORINFO_FIELD_STRUCT_* field, CORINFO_CLASS_STRUCT_** structType, CORINFO_CLASS_STRUCT_* fieldOwnerHint, CorInfoType result)
    {
        GetFieldType ??= new();

        DLDL key = default;
        key.A = CastHandle(field);
        key.B = CastHandle(fieldOwnerHint);

        DLD value = default;
        value.B = (uint)result;
        if (structType == null)
        {
            value.A = 0;
        }
        else
        {
            value.A = CastHandle(*structType);

            // If we had a previous call with null 'structType', we will not have captured the
            // class handle (we use only 'field' and 'fieldOwnerHint' as keys).
            // Update the value in that case.
            int index = GetFieldType.GetIndex(key);
            if ((index != -1) && (GetFieldType.GetItem(index).A == 0))
            {
                GetFieldType.Update(index, value);
                return;
            }
        }
        GetFieldType.Add(key, value);
    }

    public void recSatisfiesMethodConstraints(CORINFO_CLASS_STRUCT_* parent, CORINFO_METHOD_STRUCT_* method, bool result)
    {
        SatisfiesMethodConstraints ??= new();
    
        DLDL key = default;
        key.A = CastHandle(parent);
        key.B = CastHandle(method);
    
        uint value = result ? 1u : 0u;
        SatisfiesMethodConstraints.Add(key, value);
    }

    public void recGetStringLiteral(CORINFO_MODULE_STRUCT_* module, uint metaTOK, char* buffer, int bufferSize, int startIndex, int length)
    {
        GetStringLiteral ??= new();
    
        DLDDD key = default;
        key.A = CastHandle(module);
        key.B = (uint)metaTOK;
        key.C = (uint)bufferSize;
        key.D = (uint)startIndex;
    
        uint strBuf = uint.MaxValue;
        if (buffer != null && length != -1)
        {
            int bufferRealSize = Math.Min(length, bufferSize) * sizeof(char);
            strBuf = (uint)GetStringLiteral.AddBuffer(Bytes(buffer, (uint)bufferRealSize));
        }
    
        DD value = default;
        value.A = (uint)length;
        value.B = (uint)strBuf;
    
        GetStringLiteral.Add(key, value);
    }

    public void recCanCast(CORINFO_CLASS_STRUCT_* child, CORINFO_CLASS_STRUCT_* parent, bool result)
    {
        CanCast ??= new();
    
        DLDL key = default;
        key.A = CastHandle(child);
        key.B = CastHandle(parent);
    
        uint value = result ? 1u : 0u;
        CanCast.Add(key, value);
    }

    public void recGetChildType(CORINFO_CLASS_STRUCT_* clsHnd, CORINFO_CLASS_STRUCT_** clsRet, CorInfoType result)
    {
        GetChildType ??= new();
    
        DLD value = default;
        value.A = CastHandle(*clsRet);
        value.B = (uint)result;
    
        ulong key = CastHandle(clsHnd);
        GetChildType.Add(key, value);
    }

    public void recGetArrayInitializationData(CORINFO_FIELD_STRUCT_* field, uint size, void* result)
    {
        GetArrayInitializationData ??= new();
    
        DLD key = default;
        key.A = CastHandle(field);
        key.B = (uint)size;
    
        ulong value = CastPointer(result);
        GetArrayInitializationData.Add(key, value);
    }

    public void recGetAddressOfPInvokeTarget(CORINFO_METHOD_STRUCT_* method, ref CORINFO_CONST_LOOKUP pLookup)
    {
        GetAddressOfPInvokeTarget ??= new();
    
        DLD value = default;
        value.A = CastPointer(pLookup.addr);
        value.B = (uint)pLookup.accessType;
    
        ulong key = CastHandle(method);
        GetAddressOfPInvokeTarget.Add(key, value);
    }

    public void recGetMethodHash(CORINFO_METHOD_STRUCT_* ftn, uint result)
    {
        GetMethodHash ??= new();
    
        ulong key = CastHandle(ftn);
        uint value = (uint)result;
        GetMethodHash.Add(key, value);
    }

    public void recCanTailCall(CORINFO_METHOD_STRUCT_* callerHnd, CORINFO_METHOD_STRUCT_* declaredCalleeHnd, CORINFO_METHOD_STRUCT_* exactCalleeHnd, bool fIsTailPrefix, bool result)
    {
        CanTailCall ??= new();
    
        Agnostic_CanTailCall key = default;
        key.callerHnd         = CastHandle(callerHnd);
        key.declaredCalleeHnd = CastHandle(declaredCalleeHnd);
        key.exactCalleeHnd    = CastHandle(exactCalleeHnd);
        key.fIsTailPrefix     = fIsTailPrefix ? (ushort)1 : (ushort)0;
    
        uint value = result ? 1u : 0u;
        CanTailCall.Add(key, value);
    }

    public void recIsDelegateCreationAllowed(CORINFO_CLASS_STRUCT_* delegateHnd, CORINFO_METHOD_STRUCT_* calleeHnd, bool result)
    {
        IsDelegateCreationAllowed ??= new();
    
        DLDL key = default;
        key.A = CastHandle(delegateHnd);
        key.B = CastHandle(calleeHnd);
    
        uint value = result ? 1u : 0u;
        IsDelegateCreationAllowed.Add(key, value);
    }

    public void recFindCallSiteSig(CORINFO_MODULE_STRUCT_* module, uint methTOK, CORINFO_CONTEXT_STRUCT* context, CORINFO_SIG_INFO* sig)
    {
        FindCallSiteSig ??= new();
    
        Agnostic_FindCallSiteSig key = default;
        key.module  = CastHandle(module);
        key.methTok = (uint)methTOK;
        key.context = CastHandle(context);
    
        Agnostic_CORINFO_SIG_INFO value = StoreSig(*sig, FindCallSiteSig);
    
        FindCallSiteSig.Add(key, value);
    }

    public void recGetVarArgsHandle(CORINFO_SIG_INFO* pSig, CORINFO_METHOD_STRUCT_* methHnd, ref void* ppIndirection, void* result)
    {
        GetVarArgsHandle ??= new();
    
        GetVarArgsHandleValue key = default;
        key.cbSig      = (uint)pSig->cbSig;
        key.pSig_Index = (uint)GetVarArgsHandle.AddBuffer(Bytes(pSig->pSig, pSig->cbSig));
        key.scope      = CastHandle(pSig->scope);
        key.token      = (uint)pSig->token;
        key.methHnd    = CastHandle(methHnd);
    
        DLDL value = default;
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = CastHandle(result);
    
        GetVarArgsHandle.Add(key, value);
    }

    public void recGetFieldThreadLocalStoreID(CORINFO_FIELD_STRUCT_* field, ref void* ppIndirection, uint result)
    {
        GetFieldThreadLocalStoreID ??= new();
    
        DLD value = default;
    
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = (uint)result;
    
        ulong key = CastHandle(field);
        GetFieldThreadLocalStoreID.Add(key, value);
    }

    public void recAllocPgoInstrumentationBySchema(CORINFO_METHOD_STRUCT_* ftnHnd, PgoInstrumentationSchema* pSchema, uint countSchemaItems, byte** pInstrumentationData, int result)
    {
        AllocPgoInstrumentationBySchema ??= new();
    
        Agnostic_AllocPgoInstrumentationBySchema value = default;
    
        // PGO schema data poses a couple of challenges from SPMI's perspective:
        //
        // First of, the schema requested from the JIT can reasonably be considered
        // a compile result that might change with different JIT versions and/or
        // environment variables set. So that means SPMI should be able to handle
        // seeing a different schema from recording time.
        //
        // On the other hand, the code generated by the JIT will refer to addresses
        // based on pInstrumentationData and the offsets this function assigns in
        // pSchema. That means this function should be consistent with the recorded
        // EE in how it does layout, to ensure replay is going to match recording
        // time.
        //
        // To handle these issues we store the recorded result of layout for the
        // schema and use that if the JIT's requested schema matches it. Otherwise
        // we use our own layout algorithm for the schema. This layout algorithm is
        // probably not going to match the EE's algorithm exactly, but that's not
        // problematic since a different schema likely means there are going to be
        // diffs anyway.
    
        value.instrumentationDataAddress = CastPointer(*pInstrumentationData);
    
        // For the schema, note that we record *after* calling the VM API. Thus, it has already filled in the `Offset` fields.
        // We save the "IN" parts to verify against the replayed call.
        value.countSchemaItems = countSchemaItems;
        var agnosticSchema = new Agnostic_PgoInstrumentationSchema[countSchemaItems];
        for (uint i = 0; i < countSchemaItems; i++)
        {
            agnosticSchema[i].Offset              = (ulong)pSchema[i].Offset;
            agnosticSchema[i].InstrumentationKind = (uint)pSchema[i].InstrumentationKind;
            agnosticSchema[i].ILOffset            = (uint)pSchema[i].ILOffset;
            agnosticSchema[i].Count               = (uint)pSchema[i].Count;
            agnosticSchema[i].Other               = (uint)pSchema[i].Other;
        }
        value.schema_index = AllocPgoInstrumentationBySchema.AddBuffer(MemoryMarshal.AsBytes(agnosticSchema.AsSpan()));
        value.result = (uint)result;
    
        // Even though `countSchemaItems` and (most of) the `pSchema` array are IN parameters, they do not contribute to the lookup key;
        // the `ftnHnd` is the sole key, and the schema passed in for the function is expected to be the same every time the same function
        // handle is used.
        ulong key = CastHandle(ftnHnd);
        AllocPgoInstrumentationBySchema.Add(key, value);
    }

    public void recGetPgoInstrumentationResults(CORINFO_METHOD_STRUCT_* ftnHnd, ref PgoInstrumentationSchema* pSchema, ref uint pCountSchemaItems, byte** pInstrumentationData, ref PgoSource pPgoSource, ref bool pDynamicPgo, int result)
    {
        GetPgoInstrumentationResults ??= new();
    
        Agnostic_GetPgoInstrumentationResults value = default;
    
        value.countSchemaItems = pCountSchemaItems;
    
        PgoInstrumentationSchema* pInSchema = pSchema;
        var agnosticSchema = new Agnostic_PgoInstrumentationSchema[pCountSchemaItems];
        nuint maxOffset = 0;
        for (uint i = 0; i < (pCountSchemaItems); i++)
        {
            agnosticSchema[i].Offset              = (ulong)pInSchema[i].Offset;
            agnosticSchema[i].InstrumentationKind = (uint)pInSchema[i].InstrumentationKind;
            agnosticSchema[i].ILOffset            = (uint)pInSchema[i].ILOffset;
            agnosticSchema[i].Count               = (uint)pInSchema[i].Count;
            agnosticSchema[i].Other               = (uint)pInSchema[i].Other;
    
            uint dataSize = InstrumentationKindToSize(pInSchema[i].InstrumentationKind);
            maxOffset = Math.Max(maxOffset, (nuint)pInSchema[i].Offset + (uint)pInSchema[i].Count * dataSize);
        }
        value.schema_index = GetPgoInstrumentationResults.AddBuffer(MemoryMarshal.AsBytes(agnosticSchema.AsSpan()));
    
        value.data_index    = GetPgoInstrumentationResults.AddBuffer(Bytes(*pInstrumentationData, (uint)maxOffset));
        value.dataByteCount = (uint)maxOffset;
        value.result        = (uint)result;
        value.pgoSource     = (uint)pPgoSource;
        value.dynamicPgo    = (pDynamicPgo ? 1u : 0u);
    
        ulong key = CastHandle(ftnHnd);
        GetPgoInstrumentationResults.Add(key, value);
    }

    public void recIsMoreSpecificType(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2, bool result)
    {
        IsMoreSpecificType ??= new();
    
        DLDL key = default;
        key.A = CastHandle(cls1);
        key.B = CastHandle(cls2);
    
        uint value = result ? 1u : 0u;
        IsMoreSpecificType.Add(key, value);
    }

    public void recIsExactType(CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        IsExactType ??= new();
    
        ulong key = CastHandle(cls);
        uint value = result ? 1u : 0u;
        IsExactType.Add(key, value);
    }

    public void recIsGenericType(CORINFO_CLASS_STRUCT_* cls, TypeCompareState result)
    {
        IsGenericType ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        IsGenericType.Add(key, value);
    }

    public void recIsNullableType(CORINFO_CLASS_STRUCT_* cls, TypeCompareState result)
    {
        IsNullableType ??= new();
    
        ulong key   = CastHandle(cls);
        uint     value = (uint)result;
        IsNullableType.Add(key, value);
    }

    public void recIsEnum(CORINFO_CLASS_STRUCT_* cls, CORINFO_CLASS_STRUCT_* underlyingType, TypeCompareState result)
    {
        IsEnum ??= new();
    
        ulong key = CastHandle(cls);
    
        DLD value = default;
        value.A = CastHandle(underlyingType);
        value.B = (uint)result;
    
        IsEnum.Add(key, value);
    }

    public void recGetCookieForInterpreterCalliSig(CORINFO_SIG_INFO* szMetaSig, void* result)
    {
        GetCookieForInterpreterCalliSig ??= new();
    
        GetCookieForInterpreterCalliSigValue key = default;
        key.cbSig      = (uint)szMetaSig->cbSig;
        key.pSig_Index = (uint)GetCookieForInterpreterCalliSig.AddBuffer(Bytes(szMetaSig->pSig, szMetaSig->cbSig));
        key.scope      = CastHandle(szMetaSig->scope);
        key.token      = (uint)szMetaSig->token;
    
        DLDL value = default;
        value.A = CastPointer(result);
    
        GetCookieForInterpreterCalliSig.Add(key, value);
    }

    public void recErrorList(byte* error)
    {
        ErrorList ??= new();
    
        uint temp = uint.MaxValue;
    
        if (error != null)
            temp = (uint)ErrorList.AddBuffer(Bytes(error, strlen(error) + 1));
    
        ErrorList.Append(temp);
    }

    public void recGetProfilingHandle(ref bool pbHookFunction, ref void* pProfilerHandle, ref bool pbIndirectedHandles)
    {
        GetProfilingHandle ??= new();
    
        Agnostic_GetProfilingHandle value = default;
        value.bHookFunction      = (pbHookFunction ? 1u : 0u);
        value.ProfilerHandle     = CastPointer(pProfilerHandle);
        value.bIndirectedHandles = (pbIndirectedHandles ? 1u : 0u);
        GetProfilingHandle.Add(0, value);
    }

    public void recEmbedFieldHandle(CORINFO_FIELD_STRUCT_* handle, ref void* ppIndirection, CORINFO_FIELD_STRUCT_* result)
    {
        EmbedFieldHandle ??= new();
    
        DLDL value = default;
        if (!IsNullRef(ref ppIndirection))
            value.A = CastPointer(ppIndirection);
        else
            value.A = 0;
        value.B     = CastHandle(result);
    
        ulong key = CastHandle(handle);
        EmbedFieldHandle.Add(key, value);
    }

    public void recCompareTypesForCast(CORINFO_CLASS_STRUCT_* fromClass, CORINFO_CLASS_STRUCT_* toClass, TypeCompareState result)
    {
        CompareTypesForCast ??= new();
    
        DLDL key = default;
        key.A = CastHandle(fromClass);
        key.B = CastHandle(toClass);
    
        uint value = (uint)result;
        CompareTypesForCast.Add(key, value);
    }

    public void recCompareTypesForEquality(CORINFO_CLASS_STRUCT_* cls1, CORINFO_CLASS_STRUCT_* cls2, TypeCompareState result)
    {
        CompareTypesForEquality ??= new();
    
        DLDL key = default;
        key.A = CastHandle(cls1);
        key.B = CastHandle(cls2);
    
        uint value = (uint)result;
        CompareTypesForEquality.Add(key, value);
    }

    public void recGetSystemVAmd64PassStructInRegisterDescriptor(CORINFO_CLASS_STRUCT_* structHnd, SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr, bool result)
    {
        GetSystemVAmd64PassStructInRegisterDescriptor ??= new();

        ulong key = CastHandle(structHnd);

        Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor value = default;
        value.passedInRegisters = structPassInRegDescPtr->passedInRegisters ? 1u : 0u;
        value.eightByteCount    = (uint)structPassInRegDescPtr->eightByteCount;
        value.eightByteClassifications[0] = (uint)structPassInRegDescPtr->eightByteClassifications0;
        value.eightByteClassifications[1] = (uint)structPassInRegDescPtr->eightByteClassifications1;
        value.eightByteSizes[0]           = (uint)structPassInRegDescPtr->eightByteSizes0;
        value.eightByteSizes[1]           = (uint)structPassInRegDescPtr->eightByteSizes1;
        value.eightByteOffsets[0]         = (uint)structPassInRegDescPtr->eightByteOffsets0;
        value.eightByteOffsets[1]         = (uint)structPassInRegDescPtr->eightByteOffsets1;
        value.result = result ? 1u : 0u;

        GetSystemVAmd64PassStructInRegisterDescriptor.Add(key, value);
    }

    public void recGetSwiftLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_SWIFT_LOWERING pLowering)
    {
        GetSwiftLowering ??= new();

        ulong key = CastHandle(structHnd);

        Agnostic_GetSwiftLowering value = default;
        value.byReference = pLowering.byReference ? 1u : 0u;
        if (!pLowering.byReference)
        {
            value.numLoweredElements = (uint)pLowering.numLoweredElements;
            for (int i = 0; i < pLowering.numLoweredElements; i++)
            {
                value.loweredElements[i] = (uint)pLowering.LoweredElements[i];
                value.offsets[i] = pLowering.Offsets[i];
            }
        }

        GetSwiftLowering.Add(key, value);
    }

    public void recGetFpStructLowering(CORINFO_CLASS_STRUCT_* structHnd, ref CORINFO_FPSTRUCT_LOWERING pLowering)
    {
        GetFpStructLowering ??= new();

        ulong key = CastHandle(structHnd);

        Agnostic_GetFpStructLowering value = default;
        value.byIntegerCallConv = pLowering.byIntegerCallConv ? 1u : 0u;
        if (!pLowering.byIntegerCallConv)
        {
            value.numLoweredElements = (uint)pLowering.numLoweredElements;
            for (int i = 0; i < pLowering.numLoweredElements; i++)
            {
                value.loweredElements[i] = (uint)pLowering.LoweredElements[i];
                value.offsets[i] = pLowering.Offsets[i];
            }
        }

        GetFpStructLowering.Add(key, value);
    }

    public void recGetWasmLowering(CORINFO_CLASS_STRUCT_* structHnd, CorInfoWasmType value)
    {
        GetWasmLowering ??= new();
        ulong key = CastHandle(structHnd);
        GetWasmLowering.Add(key, (uint) value);
    }

    public void recGetRelocTypeHint(void* target, CorInfoReloc result)
    {
        GetRelocTypeHint ??= new();
    
        ulong key = CastPointer(target);
        uint value = (uint)result;
        GetRelocTypeHint.Add(key, value);
    }

    public void recGetAddressAlignment(void* address, uint result)
    {
        GetAddressAlignment ??= new();
    
        ulong key   = CastPointer(address);
        uint     value = (uint)result;
        GetAddressAlignment.Add(key, value);
    }

    public void recGetExpectedTargetArchitecture(uint result)
    {
        GetExpectedTargetArchitecture ??= new();
    
        uint key = 0; // There is only ever a single entry to this map
        GetExpectedTargetArchitecture.Add(key, result);
    }

    public void recDoesFieldBelongToClass(CORINFO_FIELD_STRUCT_* fld, CORINFO_CLASS_STRUCT_* cls, bool result)
    {
        DoesFieldBelongToClass ??= new();
    
        DLDL key = default;
        key.A = CastHandle(fld);
        key.B = CastHandle(cls);
    
        uint value = (result ? 1u : 0u);
        DoesFieldBelongToClass.Add(key, value);
    }

    public void recGetClassNameFromMetadata(CORINFO_CLASS_STRUCT_* cls, byte* className, byte** namespaceName)
    {
        GetClassNameFromMetadata ??= new();
    
        DLD key = default;
        key.A = CastHandle(cls);
        key.B = (namespaceName != null) ? 1u : 0u;
    
        DD value = default;
        if (className != null)
            value.A = GetClassNameFromMetadata.AddBuffer(Bytes(className, strlen(className) + 1));
        else
            value.A = uint.MaxValue;
    
        if ((namespaceName != null) && (*namespaceName != null))
            value.B =
                GetClassNameFromMetadata.AddBuffer(Bytes(*namespaceName, strlen(*namespaceName) + 1));
        else
            value.B = uint.MaxValue;
    
        GetClassNameFromMetadata.Add(key, value);
    }

    public void recGetTypeInstantiationArgument(CORINFO_CLASS_STRUCT_* cls, uint index, CORINFO_CLASS_STRUCT_* result)
    {
        GetTypeInstantiationArgument ??= new();
    
        DLD key = default;
        key.A = CastHandle(cls);
        key.B = index;
        ulong value = CastHandle(result);
        GetTypeInstantiationArgument.Add(key, value);
    }

    public void recGetMethodInstantiationArgument(CORINFO_METHOD_STRUCT_* ftn, uint index, CORINFO_CLASS_STRUCT_* result)
    {
        GetMethodInstantiationArgument ??= new();
    
        DLD key = default;
        key.A = CastHandle(ftn);
        key.B = index;
        ulong value = CastHandle(result);
        GetMethodInstantiationArgument.Add(key, value);
    }

    private static void recPrint(LightWeightMap<ulong, Agnostic_PrintResult> map, ulong handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        // Required size of a buffer that contains all data and null terminator.
        uint requiredBufferSize = uint.MaxValue;
        if (pRequiredBufferSize != null)
        {
            requiredBufferSize = (uint)(*pRequiredBufferSize);
        }
        else if (bytesWritten + 1 < bufferSize)
        {
            requiredBufferSize = (uint)(bytesWritten + 1);
        }
    
        Agnostic_PrintResult res = default;
        int index = map.GetIndex(handle);
        if (index != -1)
        {
            // Merge with existing entry
    
            res = map.GetItem(index);
    
            if (requiredBufferSize != uint.MaxValue)
            {
                res.requiredBufferSize = requiredBufferSize;
            }
    
            if (bytesWritten > res.stringBufferSize)
            {
                // Always stored without null terminator.
                res.stringBuffer = map.AddBuffer(Bytes(buffer, (uint)bytesWritten));
                res.stringBufferSize = (uint)bytesWritten;
            }
    
            map.Update(index, res);
    
            return;
        }
    
        if (buffer != null)
        {
            res.stringBuffer = map.AddBuffer(Bytes(buffer, (uint)bytesWritten));
        }
        else
        {
            res.stringBuffer = uint.MaxValue;
        }
    
        res.stringBufferSize = (uint)bytesWritten;
        res.requiredBufferSize = requiredBufferSize;
    
        map.Add(handle, res);
    }

    public void recPrintObjectDescription(CORINFO_OBJECT_STRUCT_* handle, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        recPrint(PrintObjectDescription ??= new(), CastHandle(handle), buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    }

    public void recPrintClassName(CORINFO_CLASS_STRUCT_* cls, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        recPrint(PrintClassName ??= new(), CastHandle(cls), buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    }

    public void recPrintFieldName(CORINFO_FIELD_STRUCT_* fld, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        recPrint(PrintFieldName ??= new(), CastHandle(fld), buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    }

    public void recPrintMethodName(CORINFO_METHOD_STRUCT_* meth, byte* buffer, nuint bufferSize, nuint* pRequiredBufferSize, nuint bytesWritten)
    {
        recPrint(PrintMethodName ??= new(), CastHandle(meth), buffer, bufferSize, pRequiredBufferSize, bytesWritten);
    }

    public void recGetTailCallHelpers(ref CORINFO_RESOLVED_TOKEN callToken, CORINFO_SIG_INFO* sig, CORINFO_GET_TAILCALL_HELPERS_FLAGS flags, bool result,
        ref CORINFO_TAILCALL_HELPERS pResult)
    {
        GetTailCallHelpers ??= new();
    
        Agnostic_GetTailCallHelpers key = default;
        key.callToken = StoreAgnostic_CORINFO_RESOLVED_TOKEN(ref callToken, GetTailCallHelpers);
        key.sig       = StoreSig(*sig, GetTailCallHelpers);
        key.flags     = (uint)flags;
    
        Agnostic_CORINFO_TAILCALL_HELPERS value = default;
        value.result = result ? (byte)1 : (byte)0;
        if (result)
        {
            value.flags       = (uint)pResult.flags;
            value.hStoreArgs  = CastHandle(pResult.hStoreArgs);
            value.hCallTarget = CastHandle(pResult.hCallTarget);
            value.hDispatcher = CastHandle(pResult.hDispatcher);
        }
    
        GetTailCallHelpers.Add(key, value);
    }

    public void recGetAsyncResumptionStub(CORINFO_METHOD_STRUCT_* hnd, void* entryPoint)
    {
        GetAsyncResumptionStub ??= new();
    
        DLDL result = default;
        result.A = CastHandle(hnd);
        result.B = CastPointer(entryPoint);
        GetAsyncResumptionStub.Add(0, result);
    }

    public void recGetContinuationType(nuint dataSize, ref bool objRefs, nuint objRefsSize, CORINFO_CLASS_STRUCT_* result)
    {
        if (GetContinuationType == null)
        {
            GetContinuationType = new LightWeightMap<Agnostic_GetContinuationTypeIn, ulong>();
        }
    
        Agnostic_GetContinuationTypeIn key = default;
        key.dataSize = (ulong)dataSize;
        key.objRefs = GetContinuationType.AddBuffer(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref objRefs, (int)objRefsSize)));
        key.objRefsSize = (uint)objRefsSize;
    
        ulong value = CastHandle(result);
    
        GetContinuationType.Add(key, value);
    }

    public void recUpdateEntryPointForTailCall(CORINFO_CONST_LOOKUP origEntryPoint, CORINFO_CONST_LOOKUP newEntryPoint)
    {
        UpdateEntryPointForTailCall ??= new();
    
        Agnostic_CORINFO_CONST_LOOKUP key = StoreAgnostic_CORINFO_CONST_LOOKUP(ref origEntryPoint);
        Agnostic_CORINFO_CONST_LOOKUP value = StoreAgnostic_CORINFO_CONST_LOOKUP(ref newEntryPoint);
        UpdateEntryPointForTailCall.Add(key, value);
    }

    public void recGetMethodDefFromMethod(CORINFO_METHOD_STRUCT_* hMethod, uint result)
    {
        GetMethodDefFromMethod ??= new();
    
        ulong key = CastHandle(hMethod);
        uint value = (uint)result;
        GetMethodDefFromMethod.Add(key, value);
    }

    public void recCheckMethodModifier(CORINFO_METHOD_STRUCT_* hMethod, byte* modifier, bool fOptional, bool result)
    {
        CheckMethodModifier ??= new();
    
        Agnostic_CheckMethodModifier key = default;
        key.hMethod = CastHandle(hMethod);
        // If the input matches something already in the buffer, just re-use that slot.. easier than searching for a soft
        // key on rep.
        if (modifier != null)
            key.modifier =
                (uint)CheckMethodModifier.AddBuffer(Bytes(modifier, strlen(modifier) + 1));
        else
            key.modifier = uint.MaxValue;
    
        key.fOptional = (fOptional ? 1u : 0u);
    
        uint value = result ? 1u : 0u;
        CheckMethodModifier.Add(key, value);
    }

    public void recGetArrayRank(CORINFO_CLASS_STRUCT_* cls, uint result)
    {
        GetArrayRank ??= new();
    
        ulong key = CastHandle(cls);
        uint value = (uint)result;
        GetArrayRank.Add(key, value);
    }

    public void recGetArrayIntrinsicID(CORINFO_METHOD_STRUCT_* hMethod, CorInfoArrayIntrinsic result)
    {
        GetArrayIntrinsicID ??= new();
    
        ulong key = CastHandle(hMethod);
        uint value = (uint)result;
        GetArrayIntrinsicID.Add(key, value);
    }

    public void recIsFieldStatic(CORINFO_FIELD_STRUCT_* fhld, bool result)
    {
        IsFieldStatic ??= new();
    
        ulong key = CastHandle(fhld);
        uint value = result ? 1u : 0u;
        IsFieldStatic.Add(key, value);
    }

    public void recGetArrayOrStringLength(CORINFO_OBJECT_STRUCT_* objHandle, int result)
    {
        GetArrayOrStringLength ??= new();
    
        ulong key = CastHandle(objHandle);
        uint value = (uint)result;
        GetArrayOrStringLength.Add(key, value);
    }

    public void recGetIntConfigValue(byte* name, int defaultValue, int result)
    {
        GetIntConfigValue ??= new();
    
        ErrorHandling.AssertCodeMsg(name != null, ExceptionCodes.MC, "name != null", "Name can not be null");
    
        Agnostic_ConfigIntInfo key = default;
    
        uint index =
            (uint)GetIntConfigValue.AddBuffer(Bytes(name, strlen(name) + 1));
    
        key.nameIndex    = index;
        key.defaultValue = (uint)defaultValue;
    
        GetIntConfigValue.Add(key, (uint)result);
    }

    public void recGetStringConfigValue(byte* name, byte* result)
    {
        GetStringConfigValue ??= new();
    
        ErrorHandling.AssertCodeMsg(name != null, ExceptionCodes.MC, "name != null", "Name can not be null");
    
        uint nameIndex = (uint)GetStringConfigValue.AddBuffer(Bytes(name, strlen(name) + 1));
    
        uint resultIndex = uint.MaxValue;
        if (result != null)
            resultIndex = (uint)GetStringConfigValue.AddBuffer(Bytes(result, strlen(result) + 1));
    
        GetStringConfigValue.Add(nameIndex, resultIndex);
    }

    public void recGetSpecialCopyHelper(CORINFO_CLASS_STRUCT_* type, CORINFO_METHOD_STRUCT_* helper)
    {
        GetSpecialCopyHelper ??= new();
    
        ulong key;
        key = CastHandle(type);
    
        ulong value = CastHandle(helper);
        GetSpecialCopyHelper.Add(key, value);
    }

    public void recGetWasmTypeSymbol(CorInfoWasmType* types, nuint typesSize, CORINFO_WASM_TYPE_SYMBOL_STRUCT_* result)
    {
        GetWasmTypeSymbol ??= new();
    
        Agnostic_GetWasmTypeSymbol key = default;
        key.types = (uint)GetWasmTypeSymbol.AddBuffer(Bytes(types, (uint)(sizeof(CorInfoWasmType) * typesSize)));
        key.typesSize = (uint)typesSize;
    
        ulong value = CastHandle(result);
        GetWasmTypeSymbol.Add(key, value);
    }

    public void recCanValueClassInstancePointerEscape(CORINFO_METHOD_STRUCT_* ftn, bool result)
    {
        CanValueClassInstancePointerEscape ??= new();
    
        ulong key   = CastHandle(ftn);
        uint     value = result ? 1u : 0u;
        CanValueClassInstancePointerEscape.Add(key, value);
    }
}
