// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ECallList.H
//
// This file contains definitions of FCall entrypoints
//

#ifndef FCFuncElement
#define FCFuncElement(name, impl)
#endif

#ifndef FCFuncElementSig
#define FCFuncElementSig(name,sig,impl)
#endif

#ifndef FCDynamic
#define FCDynamic(name,dynamicID)
#endif

#ifndef FCDynamicSig
#define FCDynamicSig(name,sig,dynamicID)
#endif

#ifndef FCUnreferenced
#define FCUnreferenced
#endif

#ifndef FCFuncStart
#define FCFuncStart(name)
#endif

#ifndef FCFuncEnd
#define FCFuncEnd()
#endif

#ifndef FCClassElement
#define FCClassElement(name,namespace,funcs)
#endif

//
//
// Entrypoint definitions
//
//

FCFuncStart(gDependentHandleFuncs)
    FCFuncElement("InternalInitialize",            DependentHandle::InternalInitialize)
    FCFuncElement("InternalGetTarget",             DependentHandle::InternalGetTarget)
    FCFuncElement("InternalGetDependent",          DependentHandle::InternalGetDependent)
    FCFuncElement("InternalGetTargetAndDependent", DependentHandle::InternalGetTargetAndDependent)
    FCFuncElement("InternalSetTargetToNull",       DependentHandle::InternalSetTargetToNull)
    FCFuncElement("InternalSetDependent",          DependentHandle::InternalSetDependent)
    FCFuncElement("InternalFree",                  DependentHandle::InternalFree)
FCFuncEnd()

FCFuncStart(gEnumFuncs)
    FCFuncElement("InternalGetUnderlyingType",  ReflectionEnum::InternalGetEnumUnderlyingType)
    FCFuncElement("InternalGetCorElementType",  ReflectionEnum::InternalGetCorElementType)
    FCFuncElement("InternalBoxEnum", ReflectionEnum::InternalBoxEnum)
FCFuncEnd()

FCFuncStart(gObjectFuncs)
    FCFuncElement("GetType", ObjectNative::GetClass)
FCFuncEnd()

FCFuncStart(gStringFuncs)
    FCDynamic("FastAllocateString", ECall::FastAllocateString)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_RetVoid, ECall::CtorCharArrayManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_Int_Int_RetVoid, ECall::CtorCharArrayStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_RetVoid, ECall::CtorCharPtrManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_Int_Int_RetVoid, ECall::CtorCharPtrStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_Char_Int_RetVoid, ECall::CtorCharCountManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ReadOnlySpanOfChar_RetVoid, ECall::CtorReadOnlySpanOfCharManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_RetVoid, ECall::CtorSBytePtrManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_RetVoid, ECall::CtorSBytePtrStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_Encoding_RetVoid, ECall::CtorSBytePtrStartLengthEncodingManaged)
    FCFuncElement("SetTrailByte", COMString::FCSetTrailByte)
    FCFuncElement("TryGetTrailByte", COMString::FCTryGetTrailByte)
    FCFuncElement("IsInterned", AppDomainNative::IsStringInterned)
    FCFuncElement("Intern", AppDomainNative::GetOrInternString)
FCFuncEnd()

FCFuncStart(gValueTypeFuncs)
    FCFuncElement("CanCompareBits", ValueTypeHelper::CanCompareBits)
    FCFuncElement("FastEqualsCheck", ValueTypeHelper::FastEqualsCheck)
    FCFuncElement("GetHashCode", ValueTypeHelper::GetHashCode)
    FCFuncElement("GetHashCodeOfPtr", ValueTypeHelper::GetHashCodeOfPtr)
FCFuncEnd()

FCFuncStart(gDiagnosticsDebugger)
    FCFuncElement("BreakInternal", DebugDebugger::Break)
    FCFuncElement("get_IsAttached", DebugDebugger::IsDebuggerAttached)
    FCFuncElement("IsLogging", DebugDebugger::IsLogging)
    FCFuncElement("CustomNotification", DebugDebugger::CustomNotification)
FCFuncEnd()

FCFuncStart(gDiagnosticsStackTrace)
    FCFuncElement("GetStackFramesInternal", DebugStackTrace::GetStackFramesInternal)
FCFuncEnd()

FCFuncStart(gEnvironmentFuncs)
    FCFuncElement("get_CurrentManagedThreadId", JIT_GetCurrentManagedThreadId)
    FCFuncElement("get_TickCount", SystemNative::GetTickCount)
    FCFuncElement("get_TickCount64", SystemNative::GetTickCount64)
    FCFuncElement("set_ExitCode", SystemNative::SetExitCode)
    FCFuncElement("get_ExitCode", SystemNative::GetExitCode)
    FCFuncElement("GetCommandLineArgsNative", SystemNative::GetCommandLineArgs)

    FCFuncElementSig("FailFast", &gsig_SM_Str_RetVoid, SystemNative::FailFast)
    FCFuncElementSig("FailFast", &gsig_SM_Str_Exception_RetVoid, SystemNative::FailFastWithException)
    FCFuncElementSig("FailFast", &gsig_SM_Str_Exception_Str_RetVoid, SystemNative::FailFastWithExceptionAndSource)
FCFuncEnd()

FCFuncStart(gExceptionFuncs)
    FCFuncElement("IsImmutableAgileException", ExceptionNative::IsImmutableAgileException)
    FCFuncElement("GetMethodFromStackTrace", SystemNative::GetMethodFromStackTrace)
    FCFuncElement("PrepareForForeignExceptionRaise", ExceptionNative::PrepareForForeignExceptionRaise)
    FCFuncElement("GetStackTracesDeepCopy", ExceptionNative::GetStackTracesDeepCopy)
    FCFuncElement("SaveStackTracesFromDeepCopy", ExceptionNative::SaveStackTracesFromDeepCopy)
    FCFuncElement("GetExceptionCount", ExceptionNative::GetExceptionCount)
FCFuncEnd()

FCFuncStart(gTypedReferenceFuncs)
    FCFuncElement("InternalToObject", ReflectionInvocation::TypedReferenceToObject)
    FCFuncElement("InternalMakeTypedReference", ReflectionInvocation::MakeTypedReference)
FCFuncEnd()

FCFuncStart(gSystem_Type)
    FCFuncElement("GetTypeFromHandle", RuntimeTypeHandle::GetTypeFromHandle)
    FCFuncElement("GetTypeFromHandleUnsafe", RuntimeTypeHandle::GetRuntimeType)
FCFuncEnd()

FCFuncStart(gSystem_RuntimeType)
    FCFuncElement("GetGUID", ReflectionInvocation::GetGUID)
    FCFuncElement("_CreateEnum", ReflectionInvocation::CreateEnum)
    FCFuncElement("CanValueSpecialCast", ReflectionInvocation::CanValueSpecialCast)
    FCFuncElement("AllocateValueType", ReflectionInvocation::AllocateValueType)
#if defined(FEATURE_COMINTEROP)
    FCFuncElement("InvokeDispMethod", ReflectionInvocation::InvokeDispMethod)
#endif // defined(FEATURE_COMINTEROP)
FCFuncEnd()

FCFuncStart(gCOMTypeHandleFuncs)
    FCFuncElement("IsInstanceOfType", RuntimeTypeHandle::IsInstanceOfType)
    FCFuncElement("GetDeclaringMethod", RuntimeTypeHandle::GetDeclaringMethod)
    FCFuncElement("GetDeclaringType", RuntimeTypeHandle::GetDeclaringType)
    FCFuncElement("GetFirstIntroducedMethod", RuntimeTypeHandle::GetFirstIntroducedMethod)
    FCFuncElement("GetNextIntroducedMethod", RuntimeTypeHandle::GetNextIntroducedMethod)
    FCFuncElement("GetCorElementType", RuntimeTypeHandle::GetCorElementType)
    FCFuncElement("GetAssembly", RuntimeTypeHandle::GetAssembly)
    FCFuncElement("GetModule", RuntimeTypeHandle::GetModule)
    FCFuncElement("GetBaseType", RuntimeTypeHandle::GetBaseType)
    FCFuncElement("GetElementType", RuntimeTypeHandle::GetElementType)
    FCFuncElement("GetArrayRank", RuntimeTypeHandle::GetArrayRank)
    FCFuncElement("GetToken", RuntimeTypeHandle::GetToken)
    FCFuncElement("_GetUtf8Name", RuntimeTypeHandle::GetUtf8Name)
    FCFuncElement("GetMethodAt", RuntimeTypeHandle::GetMethodAt)
    FCFuncElement("GetFields", RuntimeTypeHandle::GetFields)
    FCFuncElement("GetInterfaces", RuntimeTypeHandle::GetInterfaces)
    FCFuncElement("GetAttributes", RuntimeTypeHandle::GetAttributes)
    FCFuncElement("_GetMetadataImport", RuntimeTypeHandle::GetMetadataImport)
    FCFuncElement("GetNumVirtuals", RuntimeTypeHandle::GetNumVirtuals)
    FCFuncElement("GetNumVirtualsAndStaticVirtuals", RuntimeTypeHandle::GetNumVirtualsAndStaticVirtuals)
    FCFuncElement("IsValueType", RuntimeTypeHandle::IsValueType)
    FCFuncElement("IsInterface", RuntimeTypeHandle::IsInterface)
    FCFuncElement("IsByRefLike", RuntimeTypeHandle::IsByRefLike)
    FCFuncElement("CanCastTo", RuntimeTypeHandle::CanCastTo)
    FCFuncElement("HasInstantiation", RuntimeTypeHandle::HasInstantiation)
    FCFuncElement("GetGenericVariableIndex", RuntimeTypeHandle::GetGenericVariableIndex)
    FCFuncElement("IsGenericVariable", RuntimeTypeHandle::IsGenericVariable)
    FCFuncElement("IsGenericTypeDefinition", RuntimeTypeHandle::IsGenericTypeDefinition)
    FCFuncElement("ContainsGenericVariables", RuntimeTypeHandle::ContainsGenericVariables)
    FCFuncElement("SatisfiesConstraints", RuntimeTypeHandle::SatisfiesConstraints)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("AllocateComObject", RuntimeTypeHandle::AllocateComObject)
#endif // FEATURE_COMINTEROP
    FCFuncElement("CompareCanonicalHandles", RuntimeTypeHandle::CompareCanonicalHandles)
    FCFuncElement("GetValueInternal", RuntimeTypeHandle::GetValueInternal)
    FCFuncElement("IsEquivalentTo", RuntimeTypeHandle::IsEquivalentTo)
FCFuncEnd()

FCFuncStart(gMetaDataImport)
    FCFuncElement("_GetDefaultValue", MetaDataImport::GetDefaultValue)
    FCFuncElement("_GetName", MetaDataImport::GetName)
    FCFuncElement("_GetUserString", MetaDataImport::GetUserString)
    FCFuncElement("_GetScopeProps", MetaDataImport::GetScopeProps)
    FCFuncElement("_GetClassLayout", MetaDataImport::GetClassLayout)
    FCFuncElement("_GetSignatureFromToken", MetaDataImport::GetSignatureFromToken)
    FCFuncElement("_GetNamespace", MetaDataImport::GetNamespace)
    FCFuncElement("_GetEventProps", MetaDataImport::GetEventProps)
    FCFuncElement("_GetFieldDefProps", MetaDataImport::GetFieldDefProps)
    FCFuncElement("_GetPropertyProps", MetaDataImport::GetPropertyProps)
    FCFuncElement("_GetParentToken", MetaDataImport::GetParentToken)
    FCFuncElement("_GetParamDefProps", MetaDataImport::GetParamDefProps)
    FCFuncElement("_GetGenericParamProps", MetaDataImport::GetGenericParamProps)

    FCFuncElement("_Enum", MetaDataImport::Enum)
    FCFuncElement("_GetMemberRefProps", MetaDataImport::GetMemberRefProps)
    FCFuncElement("_GetCustomAttributeProps", MetaDataImport::GetCustomAttributeProps)
    FCFuncElement("_GetFieldOffset", MetaDataImport::GetFieldOffset)

    FCFuncElement("_GetSigOfFieldDef", MetaDataImport::GetSigOfFieldDef)
    FCFuncElement("_GetSigOfMethodDef", MetaDataImport::GetSigOfMethodDef)
    FCFuncElement("_GetFieldMarshal", MetaDataImport::GetFieldMarshal)
    FCFuncElement("_GetPInvokeMap", MetaDataImport::GetPinvokeMap)
    FCFuncElement("_IsValidToken", MetaDataImport::IsValidToken)
    FCFuncElement("_GetMarshalAs", MetaDataImport::GetMarshalAs)
FCFuncEnd()

FCFuncStart(gSignatureNative)
    FCFuncElement("GetSignature", SignatureNative::GetSignature)
    FCFuncElement("GetCustomModifiers", SignatureNative::GetCustomModifiers)
    FCFuncElement("CompareSig", SignatureNative::CompareSig)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    FCFuncElement("_GetCurrentMethod", RuntimeMethodHandle::GetCurrentMethod)
    FCFuncElement("InvokeMethod", RuntimeMethodHandle::InvokeMethod)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
    FCFuncElement("GetAttributes", RuntimeMethodHandle::GetAttributes)
    FCFuncElement("GetDeclaringType", RuntimeMethodHandle::GetDeclaringType)
    FCFuncElement("GetSlot", RuntimeMethodHandle::GetSlot)
    FCFuncElement("GetMethodDef", RuntimeMethodHandle::GetMethodDef)
    FCFuncElement("GetName", RuntimeMethodHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeMethodHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeMethodHandle::MatchesNameHash)
    FCFuncElement("HasMethodInstantiation", RuntimeMethodHandle::HasMethodInstantiation)
    FCFuncElement("IsGenericMethodDefinition", RuntimeMethodHandle::IsGenericMethodDefinition)
    FCFuncElement("GetGenericParameterCount", RuntimeMethodHandle::GetGenericParameterCount)
    FCFuncElement("IsTypicalMethodDefinition", RuntimeMethodHandle::IsTypicalMethodDefinition)
    FCFuncElement("GetStubIfNeeded", RuntimeMethodHandle::GetStubIfNeeded)
    FCFuncElement("GetMethodFromCanonical", RuntimeMethodHandle::GetMethodFromCanonical)
    FCFuncElement("IsDynamicMethod", RuntimeMethodHandle::IsDynamicMethod)
    FCFuncElement("GetMethodBody", RuntimeMethodHandle::GetMethodBody)
    FCFuncElement("IsConstructor", RuntimeMethodHandle::IsConstructor)
    FCFuncElement("GetResolver", RuntimeMethodHandle::GetResolver)
    FCFuncElement("GetLoaderAllocator", RuntimeMethodHandle::GetLoaderAllocator)
FCFuncEnd()

FCFuncStart(gCOMFieldHandleNewFuncs)
    FCFuncElement("GetValue", RuntimeFieldHandle::GetValue)
    FCFuncElement("SetValue", RuntimeFieldHandle::SetValue)
    FCFuncElement("GetValueDirect", RuntimeFieldHandle::GetValueDirect)
    FCFuncElement("SetValueDirect", RuntimeFieldHandle::SetValueDirect)
    FCFuncElement("GetName", RuntimeFieldHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeFieldHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeFieldHandle::MatchesNameHash)
    FCFuncElement("GetAttributes", RuntimeFieldHandle::GetAttributes)
    FCFuncElement("GetApproxDeclaringType", RuntimeFieldHandle::GetApproxDeclaringType)
    FCFuncElement("GetToken", RuntimeFieldHandle::GetToken)
    FCFuncElement("GetStaticFieldForGenericType", RuntimeFieldHandle::GetStaticFieldForGenericType)
    FCFuncElement("AcquiresContextFromThis", RuntimeFieldHandle::AcquiresContextFromThis)
FCFuncEnd()

FCFuncStart(gCOMModuleFuncs)
    FCFuncElement("GetTypes", COMModule::GetTypes)
FCFuncEnd()

FCFuncStart(gCOMModuleHandleFuncs)
    FCFuncElement("GetToken", ModuleHandle::GetToken)
    FCFuncElement("GetDynamicMethod", ModuleHandle::GetDynamicMethod)
    FCFuncElement("_GetMetadataImport", ModuleHandle::GetMetadataImport)
    FCFuncElement("GetMDStreamVersion", ModuleHandle::GetMDStreamVersion)
FCFuncEnd()

FCFuncStart(gCustomAttributeEncodedArgument)
    FCFuncElement("ParseAttributeArguments", Attribute::ParseAttributeArguments)
FCFuncEnd()

FCFuncStart(gCOMCustomAttributeFuncs)
    FCFuncElement("_ParseAttributeUsageAttribute", COMCustomAttribute::ParseAttributeUsageAttribute)
    FCFuncElement("_CreateCaObject", COMCustomAttribute::CreateCaObject)
    FCFuncElement("_GetPropertyOrFieldData",  COMCustomAttribute::GetPropertyOrFieldData)
FCFuncEnd()

FCFuncStart(gCompatibilitySwitchFuncs)
    FCFuncElement("GetValueInternal", CompatibilitySwitch::GetValue)
FCFuncEnd()

FCFuncStart(gRuntimeAssemblyFuncs)
    FCFuncElement("FCallIsDynamic", AssemblyNative::IsDynamic)
    FCFuncElement("GetReferencedAssemblies", AssemblyNative::GetReferencedAssemblies)
    FCFuncElement("GetManifestResourceNames", AssemblyNative::GetManifestResourceNames)
    FCFuncElement("GetManifestModule", AssemblyHandle::GetManifestModule)
    FCFuncElement("GetToken", AssemblyHandle::GetToken)
FCFuncEnd()

FCFuncStart(gAssemblyLoadContextFuncs)
    FCFuncElement("GetLoadedAssemblies", AppDomainNative::GetLoadedAssemblies)
    FCFuncElement("IsTracingEnabled", AssemblyNative::IsTracingEnabled)
FCFuncEnd()

FCFuncStart(gDelegateFuncs)
    FCFuncElement("BindToMethodName", COMDelegate::BindToMethodName)
    FCFuncElement("BindToMethodInfo", COMDelegate::BindToMethodInfo)
    FCFuncElement("GetMulticastInvoke", COMDelegate::GetMulticastInvoke)
    FCFuncElement("GetInvokeMethod", COMDelegate::GetInvokeMethod)
    FCFuncElement("InternalAlloc", COMDelegate::InternalAlloc)
    FCFuncElement("InternalAllocLike", COMDelegate::InternalAllocLike)
    FCFuncElement("InternalEqualTypes", COMDelegate::InternalEqualTypes)
    FCFuncElement("InternalEqualMethodHandles", COMDelegate::InternalEqualMethodHandles)
    FCFuncElement("FindMethodHandle", COMDelegate::FindMethodHandle)
    FCFuncElement("AdjustTarget", COMDelegate::AdjustTarget)
    FCFuncElement("GetCallStub", COMDelegate::GetCallStub)
    FCFuncElement("CompareUnmanagedFunctionPtrs", COMDelegate::CompareUnmanagedFunctionPtrs)

    // The FCall mechanism knows how to wire multiple different constructor calls into a
    // single entrypoint, without the following entry.  But we need this entry to satisfy
    // frame creation within the body:
    FCFuncElement("DelegateConstruct", COMDelegate::DelegateConstruct)
FCFuncEnd()

FCFuncStart(gMathFuncs)
    FCFuncElement("Acos", COMDouble::Acos)
    FCFuncElement("Acosh", COMDouble::Acosh)
    FCFuncElement("Asin", COMDouble::Asin)
    FCFuncElement("Asinh", COMDouble::Asinh)
    FCFuncElement("Atan", COMDouble::Atan)
    FCFuncElement("Atanh", COMDouble::Atanh)
    FCFuncElement("Atan2", COMDouble::Atan2)
    FCFuncElement("Cbrt", COMDouble::Cbrt)
    FCFuncElement("Ceiling", COMDouble::Ceil)
    FCFuncElement("Cos", COMDouble::Cos)
    FCFuncElement("Cosh", COMDouble::Cosh)
    FCFuncElement("Exp", COMDouble::Exp)
    FCFuncElement("Floor", COMDouble::Floor)
    FCFuncElement("FMod", COMDouble::FMod)
    FCFuncElement("FusedMultiplyAdd", COMDouble::FusedMultiplyAdd)
    FCFuncElement("Log", COMDouble::Log)
    FCFuncElement("Log2", COMDouble::Log2)
    FCFuncElement("Log10", COMDouble::Log10)
    FCFuncElement("ModF", COMDouble::ModF)
    FCFuncElement("Pow", COMDouble::Pow)
    FCFuncElement("Sin", COMDouble::Sin)
    FCFuncElement("SinCos", COMDouble::SinCos)
    FCFuncElement("Sinh", COMDouble::Sinh)
    FCFuncElement("Sqrt", COMDouble::Sqrt)
    FCFuncElement("Tan", COMDouble::Tan)
    FCFuncElement("Tanh", COMDouble::Tanh)
FCFuncEnd()

FCFuncStart(gMathFFuncs)
    FCFuncElement("Acos", COMSingle::Acos)
    FCFuncElement("Acosh", COMSingle::Acosh)
    FCFuncElement("Asin", COMSingle::Asin)
    FCFuncElement("Asinh", COMSingle::Asinh)
    FCFuncElement("Atan", COMSingle::Atan)
    FCFuncElement("Atanh", COMSingle::Atanh)
    FCFuncElement("Atan2", COMSingle::Atan2)
    FCFuncElement("Cbrt", COMSingle::Cbrt)
    FCFuncElement("Ceiling", COMSingle::Ceil)
    FCFuncElement("Cos", COMSingle::Cos)
    FCFuncElement("Cosh", COMSingle::Cosh)
    FCFuncElement("Exp", COMSingle::Exp)
    FCFuncElement("Floor", COMSingle::Floor)
    FCFuncElement("FMod", COMSingle::FMod)
    FCFuncElement("FusedMultiplyAdd", COMSingle::FusedMultiplyAdd)
    FCFuncElement("Log", COMSingle::Log)
    FCFuncElement("Log2", COMSingle::Log2)
    FCFuncElement("Log10", COMSingle::Log10)
    FCFuncElement("ModF", COMSingle::ModF)
    FCFuncElement("Pow", COMSingle::Pow)
    FCFuncElement("Sin", COMSingle::Sin)
    FCFuncElement("SinCos", COMSingle::SinCos)
    FCFuncElement("Sinh", COMSingle::Sinh)
    FCFuncElement("Sqrt", COMSingle::Sqrt)
    FCFuncElement("Tan", COMSingle::Tan)
    FCFuncElement("Tanh", COMSingle::Tanh)
FCFuncEnd()

FCFuncStart(gThreadFuncs)
    FCFuncElement("InternalGetCurrentThread", GetThread)
#undef Sleep
    FCFuncElement("SleepInternal", ThreadNative::Sleep)
#define Sleep(a) Dont_Use_Sleep(a)
    FCFuncElement("Initialize", ThreadNative::Initialize)
    FCFuncElement("SpinWaitInternal", ThreadNative::SpinWait)
    FCFuncElement("GetCurrentThreadNative", ThreadNative::GetCurrentThread)
    FCFuncElement("get_ManagedThreadId", ThreadNative::GetManagedThreadId)
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
    FCFuncElement("get_IsAlive", ThreadNative::IsAlive)
    FCFuncElement("IsBackgroundNative", ThreadNative::IsBackground)
    FCFuncElement("SetBackgroundNative", ThreadNative::SetBackground)
    FCFuncElement("get_IsThreadPoolThread", ThreadNative::IsThreadpoolThread)
    FCFuncElement("set_IsThreadPoolThread", ThreadNative::SetIsThreadpoolThread)
    FCFuncElement("GetPriorityNative", ThreadNative::GetPriority)
    FCFuncElement("SetPriorityNative", ThreadNative::SetPriority)
    FCFuncElement("GetThreadStateNative", ThreadNative::GetThreadState)
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    FCFuncElement("GetApartmentStateNative", ThreadNative::GetApartmentState)
    FCFuncElement("SetApartmentStateNative", ThreadNative::SetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
#ifdef FEATURE_COMINTEROP
    FCFuncElement("DisableComObjectEagerCleanup", ThreadNative::DisableComObjectEagerCleanup)
#endif // FEATURE_COMINTEROP
    FCFuncElement("Interrupt", ThreadNative::Interrupt)
    FCFuncElement("Join", ThreadNative::Join)
    FCFuncElement("get_OptimalMaxSpinWaitsPerSpinIteration", ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
    FCFuncElement("GetCurrentProcessorNumber", ThreadNative::GetCurrentProcessorNumber)
FCFuncEnd()

FCFuncStart(gThreadPoolFuncs)
    FCFuncElement("GetNextConfigUInt32Value", ThreadPoolNative::GetNextConfigUInt32Value)
    FCFuncElement("PostQueuedCompletionStatus", ThreadPoolNative::CorPostQueuedCompletionStatus)
    FCFuncElement("GetAvailableThreadsNative", ThreadPoolNative::CorGetAvailableThreads)
    FCFuncElement("CanSetMinIOCompletionThreads", ThreadPoolNative::CorCanSetMinIOCompletionThreads)
    FCFuncElement("CanSetMaxIOCompletionThreads", ThreadPoolNative::CorCanSetMaxIOCompletionThreads)
    FCFuncElement("SetMinThreadsNative", ThreadPoolNative::CorSetMinThreads)
    FCFuncElement("GetMinThreadsNative", ThreadPoolNative::CorGetMinThreads)
    FCFuncElement("GetThreadCount", ThreadPoolNative::GetThreadCount)
    FCFuncElement("GetPendingUnmanagedWorkItemCount", ThreadPoolNative::GetPendingUnmanagedWorkItemCount)
    FCFuncElement("RegisterWaitForSingleObjectNative", ThreadPoolNative::CorRegisterWaitForSingleObject)
    FCFuncElement("BindIOCompletionCallbackNative", ThreadPoolNative::CorBindIoCompletionCallback)
    FCFuncElement("SetMaxThreadsNative", ThreadPoolNative::CorSetMaxThreads)
    FCFuncElement("GetMaxThreadsNative", ThreadPoolNative::CorGetMaxThreads)
    FCFuncElement("NotifyWorkItemCompleteNative", ThreadPoolNative::NotifyRequestComplete)
    FCFuncElement("NotifyWorkItemProgressNative", ThreadPoolNative::NotifyRequestProgress)
    FCFuncElement("GetEnableWorkerTrackingNative", ThreadPoolNative::GetEnableWorkerTracking)
    FCFuncElement("ReportThreadStatusNative", ThreadPoolNative::ReportThreadStatus)
FCFuncEnd()

FCFuncStart(gRegisteredWaitHandleFuncs)
    FCFuncElement("UnregisterWaitNative", ThreadPoolNative::CorUnregisterWait)
    FCFuncElement("WaitHandleCleanupNative", ThreadPoolNative::CorWaitHandleCleanupNative)
FCFuncEnd()

FCFuncStart(gWaitHandleFuncs)
    FCFuncElement("WaitOneCore", WaitHandleNative::CorWaitOneNative)
    FCFuncElement("WaitMultipleIgnoringSyncContext", WaitHandleNative::CorWaitMultipleNative)
    FCFuncElement("SignalAndWaitNative", WaitHandleNative::CorSignalAndWaitOneNative)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP
FCFuncStart(gVariantFuncs)
    FCFuncElement("SetFieldsObject", COMVariant::SetFieldsObject)
    FCFuncElement("BoxEnum", COMVariant::BoxEnum)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
FCFuncStart(gOAVariantFuncs)
    FCFuncElement("ChangeTypeEx", COMOAVariant::ChangeTypeEx)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

FCFuncStart(gCastHelpers)
    FCFuncElement("IsInstanceOfAny_NoCacheLookup", ::IsInstanceOfAny_NoCacheLookup)
    FCFuncElement("ChkCastAny_NoCacheLookup", ::ChkCastAny_NoCacheLookup)
    FCFuncElement("Unbox_Helper", ::Unbox_Helper)
    FCFuncElement("WriteBarrier", ::WriteBarrier_Helper)
FCFuncEnd()

FCFuncStart(gArrayFuncs)
    FCFuncElement("GetCorElementTypeOfElementType", ArrayNative::GetCorElementTypeOfElementType)
    FCFuncElement("Initialize", ArrayNative::Initialize)
    FCFuncElement("IsSimpleCopy", ArrayNative::IsSimpleCopy)
    FCFuncElement("CopySlow", ArrayNative::CopySlow)
    FCFuncElement("InternalCreate", ArrayNative::CreateInstance)
    FCFuncElement("InternalGetValue", ArrayNative::GetValue)
    FCFuncElement("InternalSetValue", ArrayNative::SetValue)
FCFuncEnd()

FCFuncStart(gBufferFuncs)
    FCFuncElement("__BulkMoveWithWriteBarrier", Buffer::BulkMoveWithWriteBarrier)
FCFuncEnd()

FCFuncStart(gGCInterfaceFuncs)
    FCFuncElement("GetGenerationWR", GCInterface::GetGenerationWR)
    FCFuncElement("_RegisterForFullGCNotification", GCInterface::RegisterForFullGCNotification)
    FCFuncElement("_CancelFullGCNotification", GCInterface::CancelFullGCNotification)
    FCFuncElement("_WaitForFullGCApproach", GCInterface::WaitForFullGCApproach)
    FCFuncElement("_WaitForFullGCComplete", GCInterface::WaitForFullGCComplete)
    FCFuncElement("_CollectionCount", GCInterface::CollectionCount)
    FCFuncElement("GetMemoryInfo", GCInterface::GetMemoryInfo)
    FCFuncElement("GetMemoryLoad", GCInterface::GetMemoryLoad)
    FCFuncElement("GetSegmentSize", GCInterface::GetSegmentSize)
    FCFuncElement("GetLastGCPercentTimeInGC", GCInterface::GetLastGCPercentTimeInGC)
    FCFuncElement("GetGenerationSize", GCInterface::GetGenerationSize)
    FCFuncElement("GetGeneration", GCInterface::GetGeneration)
    FCFuncElement("GetMaxGeneration", GCInterface::GetMaxGeneration)
    FCFuncElement("_SuppressFinalize", GCInterface::SuppressFinalize)
    FCFuncElement("_ReRegisterForFinalize", GCInterface::ReRegisterForFinalize)

    FCFuncElement("GetAllocatedBytesForCurrentThread", GCInterface::GetAllocatedBytesForCurrentThread)
    FCFuncElement("GetTotalAllocatedBytes", GCInterface::GetTotalAllocatedBytes)

    FCFuncElement("AllocateNewArray", GCInterface::AllocateNewArray)
FCFuncEnd()

FCFuncStart(gGCSettingsFuncs)
    FCFuncElement("get_IsServerGC", SystemNative::IsServerGC)
    FCFuncElement("GetGCLatencyMode", GCInterface::GetGcLatencyMode)
    FCFuncElement("GetLOHCompactionMode", GCInterface::GetLOHCompactionMode)
    FCFuncElement("SetGCLatencyMode", GCInterface::SetGcLatencyMode)
    FCFuncElement("SetLOHCompactionMode", GCInterface::SetLOHCompactionMode)
FCFuncEnd()

FCFuncStart(gInteropMarshalFuncs)
    FCFuncElement("GetLastPInvokeError", MarshalNative::GetLastPInvokeError)
    FCFuncElement("SetLastPInvokeError", MarshalNative::SetLastPInvokeError)
    FCFuncElement("SizeOfHelper", MarshalNative::SizeOfClass)
    FCFuncElement("StructureToPtr", MarshalNative::StructureToPtr)
    FCFuncElement("PtrToStructureHelper", MarshalNative::PtrToStructureHelper)
    FCFuncElement("DestroyStructure", MarshalNative::DestroyStructure)
    FCFuncElement("GetExceptionCode", ExceptionNative::GetExceptionCode)
    FCFuncElement("GetExceptionPointers", ExceptionNative::GetExceptionPointers)

    FCFuncElement("OffsetOfHelper", MarshalNative::OffsetOfHelper)
    FCFuncElement("GetExceptionForHRInternal", MarshalNative::GetExceptionForHR)
    FCFuncElement("GetDelegateForFunctionPointerInternal", MarshalNative::GetDelegateForFunctionPointerInternal)
    FCFuncElement("GetFunctionPointerForDelegateInternal", MarshalNative::GetFunctionPointerForDelegateInternal)

#ifdef FEATURE_COMINTEROP
    FCFuncElement("GetHRForException", MarshalNative::GetHRForException)
    FCFuncElement("GetObjectForIUnknownNative", MarshalNative::GetObjectForIUnknownNative)
    FCFuncElement("GetUniqueObjectForIUnknownNative", MarshalNative::GetUniqueObjectForIUnknownNative)
    FCFuncElement("GetNativeVariantForObjectNative", MarshalNative::GetNativeVariantForObjectNative)
    FCFuncElement("GetObjectForNativeVariantNative", MarshalNative::GetObjectForNativeVariantNative)
    FCFuncElement("InternalFinalReleaseComObject", MarshalNative::FinalReleaseComObject)
    FCFuncElement("IsTypeVisibleFromCom", MarshalNative::IsTypeVisibleFromCom)
    FCFuncElement("CreateAggregatedObjectNative", MarshalNative::CreateAggregatedObjectNative)
    FCFuncElement("AreComObjectsAvailableForCleanup", MarshalNative::AreComObjectsAvailableForCleanup)
    FCFuncElement("InternalCreateWrapperOfType", MarshalNative::InternalCreateWrapperOfType)
    FCFuncElement("GetObjectsForNativeVariantsNative", MarshalNative::GetObjectsForNativeVariantsNative)
    FCFuncElement("GetStartComSlot", MarshalNative::GetStartComSlot)
    FCFuncElement("GetEndComSlot", MarshalNative::GetEndComSlot)
    FCFuncElement("GetIUnknownForObjectNative", MarshalNative::GetIUnknownForObjectNative)
    FCFuncElement("GetIDispatchForObjectNative", MarshalNative::GetIDispatchForObjectNative)
    FCFuncElement("GetComInterfaceForObjectNative", MarshalNative::GetComInterfaceForObjectNative)
    FCFuncElement("InternalReleaseComObject", MarshalNative::ReleaseComObject)
    FCFuncElement("GetTypedObjectForIUnknown", MarshalNative::GetTypedObjectForIUnknown)
    FCFuncElement("ChangeWrapperHandleStrength", MarshalNative::ChangeWrapperHandleStrength)
    FCFuncElement("CleanupUnusedObjectsInCurrentContext", MarshalNative::CleanupUnusedObjectsInCurrentContext)
#endif // FEATURE_COMINTEROP
FCFuncEnd()

FCFuncStart(gMissingMemberExceptionFuncs)
    FCFuncElement("FormatSignature", MissingMemberException_FormatSignature)
FCFuncEnd()

FCFuncStart(gInterlockedFuncs)
    FCFuncElementSig("Exchange", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::Exchange)
    FCFuncElementSig("Exchange", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::Exchange64)
    FCFuncElementSig("Exchange", &gsig_SM_RefDbl_Dbl_RetDbl, COMInterlocked::ExchangeDouble)
    FCFuncElementSig("Exchange", &gsig_SM_RefFlt_Flt_RetFlt, COMInterlocked::ExchangeFloat)
    FCFuncElementSig("Exchange", &gsig_SM_RefObj_Obj_RetObj, COMInterlocked::ExchangeObject)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefInt_Int_Int_RetInt, COMInterlocked::CompareExchange)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefLong_Long_Long_RetLong, COMInterlocked::CompareExchange64)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefDbl_Dbl_Dbl_RetDbl, COMInterlocked::CompareExchangeDouble)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefFlt_Flt_Flt_RetFlt, COMInterlocked::CompareExchangeFloat)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefObj_Obj_Obj_RetObj, COMInterlocked::CompareExchangeObject)
    FCFuncElementSig("ExchangeAdd", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::ExchangeAdd32)
    FCFuncElementSig("ExchangeAdd", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::ExchangeAdd64)
    FCFuncElement("MemoryBarrier", COMInterlocked::FCMemoryBarrier)
    FCFuncElement("ReadMemoryBarrier", COMInterlocked::FCMemoryBarrierLoad)
FCFuncEnd()

FCFuncStart(gJitInfoFuncs)
    FCFuncElement("GetCompiledILBytes", GetCompiledILBytes)
    FCFuncElement("GetCompiledMethodCount", GetCompiledMethodCount)
    FCFuncElement("GetCompilationTimeInTicks", GetCompilationTimeInTicks)
FCFuncEnd()

FCFuncStart(gVarArgFuncs)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_PtrVoid_RetVoid, VarArgsNative::Init2)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_RetVoid, VarArgsNative::Init)
    FCFuncElement("GetRemainingCount", VarArgsNative::GetRemainingCount)
    FCFuncElement("_GetNextArgType", VarArgsNative::GetNextArgType)
    FCFuncElement("FCallGetNextArg", VarArgsNative::DoGetNextArg)
    FCFuncElement("InternalGetNextArg", VarArgsNative::GetNextArg2)
FCFuncEnd()

FCFuncStart(gMonitorFuncs)
    FCFuncElement("Enter", JIT_MonEnter)
    FCFuncElement("ReliableEnter", JIT_MonReliableEnter)
    FCFuncElement("ReliableEnterTimeout", JIT_MonTryEnter)
    FCFuncElement("Exit", JIT_MonExit)
    FCFuncElement("ObjWait", ObjectNative::WaitTimeout)
    FCFuncElement("ObjPulse", ObjectNative::Pulse)
    FCFuncElement("ObjPulseAll", ObjectNative::PulseAll)
    FCFuncElement("IsEnteredNative", ObjectNative::IsLockHeld)
FCFuncEnd()

FCFuncStart(gOverlappedFuncs)
    FCFuncElement("AllocateNativeOverlapped", AllocateNativeOverlapped)
    FCFuncElement("FreeNativeOverlapped", FreeNativeOverlapped)
    FCFuncElement("CheckVMForIOPacket", CheckVMForIOPacket)
    FCFuncElement("GetOverlappedFromNative", GetOverlappedFromNative)
FCFuncEnd()

FCFuncStart(gRuntimeHelpers)
    FCFuncElement("GetObjectValue", ObjectNative::GetObjectValue)
    FCFuncElement("InitializeArray", ArrayNative::InitializeArray)
    FCFuncElement("GetSpanDataFrom", ArrayNative::GetSpanDataFrom)
    FCFuncElement("PrepareDelegate", ReflectionInvocation::PrepareDelegate)
    FCFuncElement("GetHashCode", ObjectNative::GetHashCode)
    FCFuncElement("Equals", ObjectNative::Equals)
    FCFuncElement("AllocateUninitializedClone", ObjectNative::AllocateUninitializedClone)
    FCFuncElement("EnsureSufficientExecutionStack", ReflectionInvocation::EnsureSufficientExecutionStack)
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
    FCFuncElement("AllocTailCallArgBuffer", TailCallHelp::AllocTailCallArgBuffer)
    FCFuncElement("GetTailCallInfo", TailCallHelp::GetTailCallInfo)
    FCFuncElement("RegisterForGCReporting", GCReporting::Register)
    FCFuncElement("UnregisterForGCReporting", GCReporting::Unregister)
FCFuncEnd()

FCFuncStart(gMngdFixedArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdFixedArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdFixedArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdFixedArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdFixedArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdFixedArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNativeContents", MngdFixedArrayMarshaler::ClearNativeContents)
FCFuncEnd()

FCFuncStart(gMngdNativeArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdNativeArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdNativeArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdNativeArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdNativeArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdNativeArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdNativeArrayMarshaler::ClearNative)
    FCFuncElement("ClearNativeContents", MngdNativeArrayMarshaler::ClearNativeContents)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP
FCFuncStart(gObjectMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::ObjectMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::ObjectMarshaler__ConvertToManaged)
    FCFuncElement("ClearNative", StubHelpers::ObjectMarshaler__ClearNative)
FCFuncEnd()

FCFuncStart(gInterfaceMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::InterfaceMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::InterfaceMarshaler__ConvertToManaged)
FCFuncEnd()

FCFuncStart(gMngdSafeArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdSafeArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdSafeArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdSafeArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdSafeArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdSafeArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdSafeArrayMarshaler::ClearNative)
FCFuncEnd()

#endif // FEATURE_COMINTEROP

FCFuncStart(gMngdRefCustomMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdRefCustomMarshaler::CreateMarshaler)
    FCFuncElement("ConvertContentsToNative", MngdRefCustomMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertContentsToManaged", MngdRefCustomMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdRefCustomMarshaler::ClearNative)
    FCFuncElement("ClearManaged", MngdRefCustomMarshaler::ClearManaged)
FCFuncEnd()

FCFuncStart(gStubHelperFuncs)
    FCFuncElement("GetNDirectTarget", StubHelpers::GetNDirectTarget)
    FCFuncElement("GetDelegateTarget", StubHelpers::GetDelegateTarget)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
    FCFuncElement("ThrowInteropParamException", StubHelpers::ThrowInteropParamException)
    FCFuncElement("InternalGetHRExceptionObject", StubHelpers::GetHRExceptionObject)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("InternalGetCOMHRExceptionObject", StubHelpers::GetCOMHRExceptionObject)
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
#endif // FEATURE_COMINTEROP
#ifdef PROFILING_SUPPORTED
    FCFuncElement("ProfilerBeginTransitionCallback", StubHelpers::ProfilerBeginTransitionCallback)
    FCFuncElement("ProfilerEndTransitionCallback", StubHelpers::ProfilerEndTransitionCallback)
#endif
    FCFuncElement("CreateCustomMarshalerHelper", StubHelpers::CreateCustomMarshalerHelper)
    FCFuncElement("FmtClassUpdateNativeInternal", StubHelpers::FmtClassUpdateNativeInternal)
    FCFuncElement("FmtClassUpdateCLRInternal", StubHelpers::FmtClassUpdateCLRInternal)
    FCFuncElement("LayoutDestroyNativeInternal", StubHelpers::LayoutDestroyNativeInternal)
    FCFuncElement("AllocateInternal", StubHelpers::AllocateInternal)
    FCFuncElement("MarshalToUnmanagedVaListInternal", StubHelpers::MarshalToUnmanagedVaListInternal)
    FCFuncElement("MarshalToManagedVaListInternal", StubHelpers::MarshalToManagedVaListInternal)
    FCFuncElement("CalcVaListSize", StubHelpers::CalcVaListSize)
    FCFuncElement("ValidateObject", StubHelpers::ValidateObject)
    FCFuncElement("ValidateByref", StubHelpers::ValidateByref)
    FCFuncElement("LogPinnedArgument", StubHelpers::LogPinnedArgument)
    FCFuncElement("GetStubContext", StubHelpers::GetStubContext)
#ifdef FEATURE_ARRAYSTUB_AS_IL
    FCFuncElement("ArrayTypeCheck", StubHelpers::ArrayTypeCheck)
#endif //FEATURE_ARRAYSTUB_AS_IL
#ifdef FEATURE_MULTICASTSTUB_AS_IL
    FCFuncElement("MulticastDebuggerTraceHelper", StubHelpers::MulticastDebuggerTraceHelper)
#endif //FEATURE_MULTICASTSTUB_AS_IL
    FCFuncElement("NextCallReturnAddress", StubHelpers::NextCallReturnAddress)
FCFuncEnd()

FCFuncStart(gGCHandleFuncs)
    FCFuncElement("InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
FCFuncEnd()


FCFuncStart(gStreamFuncs)
    FCFuncElement("HasOverriddenBeginEndRead", StreamNative::HasOverriddenBeginEndRead)
    FCFuncElement("HasOverriddenBeginEndWrite", StreamNative::HasOverriddenBeginEndWrite)
FCFuncEnd()

FCFuncStart(gWeakReferenceFuncs)
    FCFuncElement("Create", WeakReferenceNative::Create)
    FCFuncElement("Finalize", WeakReferenceNative::Finalize)
    FCFuncElement("get_Target", WeakReferenceNative::GetTarget)
    FCFuncElement("set_Target", WeakReferenceNative::SetTarget)
    FCFuncElement("get_IsAlive", WeakReferenceNative::IsAlive)
    FCFuncElement("IsTrackResurrection", WeakReferenceNative::IsTrackResurrection)
FCFuncEnd()

FCFuncStart(gWeakReferenceOfTFuncs)
    FCFuncElement("Create", WeakReferenceOfTNative::Create)
    FCFuncElement("Finalize", WeakReferenceOfTNative::Finalize)
    FCFuncElement("get_Target", WeakReferenceOfTNative::GetTarget)
    FCFuncElement("set_Target", WeakReferenceOfTNative::SetTarget)
    FCFuncElement("IsTrackResurrection", WeakReferenceOfTNative::IsTrackResurrection)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP

//
// ECall helpers for the standard managed interfaces.
//

#define MNGSTDITF_BEGIN_INTERFACE(FriendlyName, strMngItfName, strUCOMMngItfName, strCustomMarshalerName, strCustomMarshalerCookie, strManagedViewName, NativeItfIID, bCanCastOnNativeItfQI) \
FCFuncStart(g##FriendlyName##Funcs)

#define MNGSTDITF_DEFINE_METH_IMPL(FriendlyName, FCallMethName, MethName, MethSig, FcallDecl) \
    FCUnreferenced FCFuncElementSig(#MethName, MethSig, FriendlyName::FCallMethName)

#define MNGSTDITF_END_INTERFACE(FriendlyName) \
FCFuncEnd()

#include "mngstditflist.h"

#undef MNGSTDITF_BEGIN_INTERFACE
#undef MNGSTDITF_DEFINE_METH_IMPL
#undef MNGSTDITF_END_INTERFACE

#endif // FEATURE_COMINTEROP

//
//
// Class definitions
//
//

// Note these have to remain sorted by name:namespace pair (Assert will wack you if you don't)
// The sorting is case-sensitive

FCClassElement("ArgIterator", "System", gVarArgFuncs)
FCClassElement("Array", "System", gArrayFuncs)
FCClassElement("AssemblyLoadContext", "System.Runtime.Loader", gAssemblyLoadContextFuncs)
FCClassElement("Buffer", "System", gBufferFuncs)
FCClassElement("CastHelpers", "System.Runtime.CompilerServices", gCastHelpers)
FCClassElement("CompatibilitySwitch", "System.Runtime.Versioning", gCompatibilitySwitchFuncs)
FCClassElement("CustomAttribute", "System.Reflection", gCOMCustomAttributeFuncs)
FCClassElement("CustomAttributeEncodedArgument", "System.Reflection", gCustomAttributeEncodedArgument)
FCClassElement("Debugger", "System.Diagnostics", gDiagnosticsDebugger)
FCClassElement("Delegate", "System", gDelegateFuncs)
FCClassElement("DependentHandle", "System.Runtime", gDependentHandleFuncs)
FCClassElement("Enum", "System", gEnumFuncs)
FCClassElement("Environment", "System", gEnvironmentFuncs)
FCClassElement("Exception", "System", gExceptionFuncs)
FCClassElement("GC", "System", gGCInterfaceFuncs)
FCClassElement("GCHandle", "System.Runtime.InteropServices", gGCHandleFuncs)
FCClassElement("GCSettings", "System.Runtime", gGCSettingsFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("IEnumerable", "System.Collections", gStdMngIEnumerableFuncs)
FCClassElement("IEnumerator", "System.Collections", gStdMngIEnumeratorFuncs)
FCClassElement("IReflect", "System.Reflection", gStdMngIReflectFuncs)
FCClassElement("InterfaceMarshaler", "System.StubHelpers", gInterfaceMarshalerFuncs)
#endif
FCClassElement("Interlocked", "System.Threading", gInterlockedFuncs)
FCClassElement("JitInfo", "System.Runtime", gJitInfoFuncs)
FCClassElement("Marshal", "System.Runtime.InteropServices", gInteropMarshalFuncs)
FCClassElement("Math", "System", gMathFuncs)
FCClassElement("MathF", "System", gMathFFuncs)
FCClassElement("MetadataImport", "System.Reflection", gMetaDataImport)
FCClassElement("MissingMemberException", "System",  gMissingMemberExceptionFuncs)
FCClassElement("MngdFixedArrayMarshaler", "System.StubHelpers", gMngdFixedArrayMarshalerFuncs)
FCClassElement("MngdNativeArrayMarshaler", "System.StubHelpers", gMngdNativeArrayMarshalerFuncs)
FCClassElement("MngdRefCustomMarshaler", "System.StubHelpers", gMngdRefCustomMarshalerFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("MngdSafeArrayMarshaler", "System.StubHelpers", gMngdSafeArrayMarshalerFuncs)
#endif // FEATURE_COMINTEROP
FCClassElement("ModuleHandle", "System", gCOMModuleHandleFuncs)
FCClassElement("Monitor", "System.Threading", gMonitorFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("OAVariantLib", "Microsoft.Win32", gOAVariantFuncs)
#endif
FCClassElement("Object", "System", gObjectFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("ObjectMarshaler", "System.StubHelpers", gObjectMarshalerFuncs)
#endif
FCClassElement("OverlappedData", "System.Threading", gOverlappedFuncs)

FCClassElement("RegisteredWaitHandle", "System.Threading", gRegisteredWaitHandleFuncs)

FCClassElement("RuntimeAssembly", "System.Reflection", gRuntimeAssemblyFuncs)
FCClassElement("RuntimeFieldHandle", "System", gCOMFieldHandleNewFuncs)
FCClassElement("RuntimeHelpers", "System.Runtime.CompilerServices", gRuntimeHelpers)
FCClassElement("RuntimeMethodHandle", "System", gRuntimeMethodHandle)
FCClassElement("RuntimeModule", "System.Reflection", gCOMModuleFuncs)
FCClassElement("RuntimeType", "System", gSystem_RuntimeType)
FCClassElement("RuntimeTypeHandle", "System", gCOMTypeHandleFuncs)

FCClassElement("Signature", "System", gSignatureNative)
FCClassElement("StackTrace", "System.Diagnostics", gDiagnosticsStackTrace)
FCClassElement("Stream", "System.IO", gStreamFuncs)
FCClassElement("String", "System", gStringFuncs)
FCClassElement("StubHelpers", "System.StubHelpers", gStubHelperFuncs)
FCClassElement("Thread", "System.Threading", gThreadFuncs)
FCClassElement("ThreadPool", "System.Threading", gThreadPoolFuncs)
FCClassElement("Type", "System", gSystem_Type)
FCClassElement("TypedReference", "System", gTypedReferenceFuncs)
FCClassElement("ValueType", "System", gValueTypeFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("Variant", "System", gVariantFuncs)
#endif
FCClassElement("WaitHandle", "System.Threading", gWaitHandleFuncs)
FCClassElement("WeakReference", "System", gWeakReferenceFuncs)
FCClassElement("WeakReference`1", "System", gWeakReferenceOfTFuncs)

#undef FCFuncElement
#undef FCFuncElementSig
#undef FCDynamic
#undef FCDynamicSig
#undef FCUnreferenced
#undef FCFuncStart
#undef FCFuncEnd
#undef FCClassElement
