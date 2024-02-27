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
    FCFuncElement("InternalAlloc",                 DependentHandle::InternalAlloc)
    FCFuncElement("InternalGetTarget",             DependentHandle::InternalGetTarget)
    FCFuncElement("InternalGetDependent",          DependentHandle::InternalGetDependent)
    FCFuncElement("InternalGetTargetAndDependent", DependentHandle::InternalGetTargetAndDependent)
    FCFuncElement("InternalSetTargetToNull",       DependentHandle::InternalSetTargetToNull)
    FCFuncElement("InternalSetDependent",          DependentHandle::InternalSetDependent)
    FCFuncElement("InternalFree",                  DependentHandle::InternalFree)
FCFuncEnd()

FCFuncStart(gEnumFuncs)
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
    FCFuncElement("InternalMakeTypedReference", ReflectionInvocation::MakeTypedReference)
FCFuncEnd()

FCFuncStart(gSystem_Type)
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
    FCFuncElement("CanCastTo", RuntimeTypeHandle::CanCastTo)
    FCFuncElement("GetGenericVariableIndex", RuntimeTypeHandle::GetGenericVariableIndex)
    FCFuncElement("IsGenericVariable", RuntimeTypeHandle::IsGenericVariable)
    FCFuncElement("ContainsGenericVariables", RuntimeTypeHandle::ContainsGenericVariables)
    FCFuncElement("SatisfiesConstraints", RuntimeTypeHandle::SatisfiesConstraints)
    FCFuncElement("GetArgumentTypesFromFunctionPointer", RuntimeTypeHandle::GetArgumentTypesFromFunctionPointer)
    FCFuncElement("IsUnmanagedFunctionPointer", RuntimeTypeHandle::IsUnmanagedFunctionPointer)

#ifdef FEATURE_COMINTEROP
    FCFuncElement("AllocateComObject", RuntimeTypeHandle::AllocateComObject)
#endif // FEATURE_COMINTEROP
    FCFuncElement("CompareCanonicalHandles", RuntimeTypeHandle::CompareCanonicalHandles)
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
    FCFuncElement("CompareSig", SignatureNative::CompareSig)
    FCFuncElement("GetParameterOffset", SignatureNative::GetParameterOffset)
    FCFuncElement("GetTypeParameterOffset", SignatureNative::GetTypeParameterOffset)
    FCFuncElement("GetCustomModifiersAtOffset", SignatureNative::GetCustomModifiersAtOffset)
    FCFuncElement("GetCallingConventionFromFunctionPointerAtOffset", SignatureNative::GetCallingConventionFromFunctionPointerAtOffset)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    FCFuncElement("_GetCurrentMethod", RuntimeMethodHandle::GetCurrentMethod)
    FCFuncElement("InvokeMethod", RuntimeMethodHandle::InvokeMethod)
    FCFuncElement("ReboxFromNullable", RuntimeMethodHandle::ReboxFromNullable)
    FCFuncElement("ReboxToNullable", RuntimeMethodHandle::ReboxToNullable)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
    FCFuncElement("GetAttributes", RuntimeMethodHandle::GetAttributes)
    FCFuncElement("GetDeclaringType", RuntimeMethodHandle::GetDeclaringType)
    FCFuncElement("GetSlot", RuntimeMethodHandle::GetSlot)
    FCFuncElement("GetMethodDef", RuntimeMethodHandle::GetMethodDef)
    FCFuncElement("GetName", RuntimeMethodHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeMethodHandle::GetUtf8Name)
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
    FCFuncElement("GetAttributes", RuntimeFieldHandle::GetAttributes)
    FCFuncElement("GetApproxDeclaringType", RuntimeFieldHandle::GetApproxDeclaringType)
    FCFuncElement("GetToken", RuntimeFieldHandle::GetToken)
    FCFuncElement("GetStaticFieldForGenericType", RuntimeFieldHandle::GetStaticFieldForGenericType)
    FCFuncElement("AcquiresContextFromThis", RuntimeFieldHandle::AcquiresContextFromThis)
    FCFuncElement("GetLoaderAllocator", RuntimeFieldHandle::GetLoaderAllocator)
    FCFuncElement("IsFastPathSupported", RuntimeFieldHandle::IsFastPathSupported)
    FCFuncElement("GetInstanceFieldOffset", RuntimeFieldHandle::GetInstanceFieldOffset)
    FCFuncElement("GetStaticFieldAddress", RuntimeFieldHandle::GetStaticFieldAddress)        
FCFuncEnd()

FCFuncStart(gCOMModuleHandleFuncs)
    FCFuncElement("GetToken", ModuleHandle::GetToken)
    FCFuncElement("GetDynamicMethod", ModuleHandle::GetDynamicMethod)
    FCFuncElement("_GetMetadataImport", ModuleHandle::GetMetadataImport)
    FCFuncElement("GetMDStreamVersion", ModuleHandle::GetMDStreamVersion)
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
    FCFuncElement("GetManifestModule", AssemblyHandle::GetManifestModule)
    FCFuncElement("GetToken", AssemblyHandle::GetToken)
FCFuncEnd()

FCFuncStart(gAssemblyLoadContextFuncs)
    FCFuncElement("IsTracingEnabled", AssemblyNative::IsTracingEnabled)
FCFuncEnd()

FCFuncStart(gDelegateFuncs)
    FCFuncElement("GetMulticastInvoke", COMDelegate::GetMulticastInvoke)
    FCFuncElement("GetInvokeMethod", COMDelegate::GetInvokeMethod)

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
    FCFuncElement("Initialize", ThreadNative::Initialize)
    FCFuncElement("GetCurrentThreadNative", ThreadNative::GetCurrentThread)
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
    FCFuncElement("get_IsAlive", ThreadNative::IsAlive)
    FCFuncElement("GetIsBackground", ThreadNative::GetIsBackground)
    FCFuncElement("get_IsThreadPoolThread", ThreadNative::IsThreadpoolThread)
    FCFuncElement("set_IsThreadPoolThread", ThreadNative::SetIsThreadpoolThread)
    FCFuncElement("GetPriorityNative", ThreadNative::GetPriority)
    FCFuncElement("SetPriorityNative", ThreadNative::SetPriority)
    FCFuncElement("GetThreadStateNative", ThreadNative::GetThreadState)
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    FCFuncElement("GetApartmentStateNative", ThreadNative::GetApartmentState)
    FCFuncElement("SetApartmentStateNative", ThreadNative::SetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    FCFuncElement("Join", ThreadNative::Join)
    FCFuncElement("get_OptimalMaxSpinWaitsPerSpinIteration", ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
FCFuncEnd()

FCFuncStart(gThreadPoolFuncs)
    FCFuncElement("GetNextConfigUInt32Value", ThreadPoolNative::GetNextConfigUInt32Value)
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

FCFuncStart(gCastHelpers)
    FCFuncElement("IsInstanceOfAny_NoCacheLookup", ::IsInstanceOfAny_NoCacheLookup)
    FCFuncElement("ChkCastAny_NoCacheLookup", ::ChkCastAny_NoCacheLookup)
    FCFuncElement("Unbox_Helper", ::Unbox_Helper)
    FCFuncElement("WriteBarrier", ::WriteBarrier_Helper)
FCFuncEnd()

FCFuncStart(gArrayFuncs)
    FCFuncElement("GetCorElementTypeOfElementType", ArrayNative::GetCorElementTypeOfElementType)
    FCFuncElement("IsSimpleCopy", ArrayNative::IsSimpleCopy)
    FCFuncElement("InternalSetValue", ArrayNative::SetValue)
FCFuncEnd()

FCFuncStart(gBufferFuncs)
    FCFuncElement("__BulkMoveWithWriteBarrier", Buffer::BulkMoveWithWriteBarrier)
FCFuncEnd()

FCFuncStart(gGCFrameRegistration)
    FCFuncElement("RegisterForGCReporting", GCReporting::Register)
    FCFuncElement("UnregisterForGCReporting", GCReporting::Unregister)
FCFuncEnd()

FCFuncStart(gGCInterfaceFuncs)
    FCFuncElement("_RegisterForFullGCNotification", GCInterface::RegisterForFullGCNotification)
    FCFuncElement("_CancelFullGCNotification", GCInterface::CancelFullGCNotification)
    FCFuncElement("_CollectionCount", GCInterface::CollectionCount)
    FCFuncElement("GetMemoryInfo", GCInterface::GetMemoryInfo)
    FCFuncElement("_GetTotalPauseDuration", GCInterface::GetTotalPauseDuration)
    FCFuncElement("GetMemoryLoad", GCInterface::GetMemoryLoad)
    FCFuncElement("GetSegmentSize", GCInterface::GetSegmentSize)
    FCFuncElement("GetLastGCPercentTimeInGC", GCInterface::GetLastGCPercentTimeInGC)
    FCFuncElement("GetGenerationSize", GCInterface::GetGenerationSize)
    FCFuncElement("GetGeneration", GCInterface::GetGeneration)
    FCFuncElement("GetMaxGeneration", GCInterface::GetMaxGeneration)
    FCFuncElement("_SuppressFinalize", GCInterface::SuppressFinalize)
    FCFuncElement("_ReRegisterForFinalize", GCInterface::ReRegisterForFinalize)

    FCFuncElement("GetAllocatedBytesForCurrentThread", GCInterface::GetAllocatedBytesForCurrentThread)
    FCFuncElement("GetTotalAllocatedBytesApproximate", GCInterface::GetTotalAllocatedBytesApproximate)

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
    FCFuncElement("GetExceptionCode", ExceptionNative::GetExceptionCode)
    FCFuncElement("GetExceptionPointers", ExceptionNative::GetExceptionPointers)

#ifdef FEATURE_COMINTEROP
    FCFuncElement("AreComObjectsAvailableForCleanup", MarshalNative::AreComObjectsAvailableForCleanup)
#endif // FEATURE_COMINTEROP
FCFuncEnd()

FCFuncStart(gInterlockedFuncs)
    FCFuncElement("Exchange8", COMInterlocked::Exchange8)
    FCFuncElement("Exchange16", COMInterlocked::Exchange16)
    FCFuncElement("Exchange32", COMInterlocked::Exchange32)
    FCFuncElement("Exchange64", COMInterlocked::Exchange64)
    FCFuncElement("ExchangeObject", COMInterlocked::ExchangeObject)
    FCFuncElement("CompareExchange8", COMInterlocked::CompareExchange8)
    FCFuncElement("CompareExchange16", COMInterlocked::CompareExchange16)
    FCFuncElement("CompareExchange32", COMInterlocked::CompareExchange32)
    FCFuncElement("CompareExchange64", COMInterlocked::CompareExchange64)
    FCFuncElement("CompareExchangeObject", COMInterlocked::CompareExchangeObject)
    FCFuncElement("ExchangeAdd32", COMInterlocked::ExchangeAdd32)
    FCFuncElement("ExchangeAdd64", COMInterlocked::ExchangeAdd64)
FCFuncEnd()

FCFuncStart(gJitInfoFuncs)
    FCFuncElement("GetCompiledILBytes", GetCompiledILBytes)
    FCFuncElement("GetCompiledMethodCount", GetCompiledMethodCount)
    FCFuncElement("GetCompilationTimeInTicks", GetCompilationTimeInTicks)
FCFuncEnd()

FCFuncStart(gMonitorFuncs)
    FCFuncElement("Enter", JIT_MonEnter)
    FCFuncElement("ReliableEnter", JIT_MonReliableEnter)
    FCFuncElement("ReliableEnterTimeout", JIT_MonTryEnter)
    FCFuncElement("Exit", JIT_MonExit)
    FCFuncElement("IsEnteredNative", ObjectNative::IsLockHeld)
FCFuncEnd()

FCFuncStart(gRuntimeHelpers)
    FCFuncElement("InitializeArray", ArrayNative::InitializeArray)
    FCFuncElement("GetSpanDataFrom", ArrayNative::GetSpanDataFrom)
    FCFuncElement("PrepareDelegate", ReflectionInvocation::PrepareDelegate)
    FCFuncElement("GetHashCode", ObjectNative::GetHashCode)
    FCFuncElement("TryGetHashCode", ObjectNative::TryGetHashCode)
    FCFuncElement("ContentEquals", ObjectNative::ContentEquals)
    FCFuncElement("EnsureSufficientExecutionStack", ReflectionInvocation::EnsureSufficientExecutionStack)
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
    FCFuncElement("AllocTailCallArgBuffer", TailCallHelp::AllocTailCallArgBuffer)
    FCFuncElement("GetTailCallInfo", TailCallHelp::GetTailCallInfo)
    FCFuncElement("Box", JIT_Box)
    FCFuncElement("Unbox_Nullable", JIT_Unbox_Nullable)
FCFuncEnd()

FCFuncStart(gMethodTableFuncs)
    FCFuncElement("GetNumInstanceFieldBytes", MethodTableNative::GetNumInstanceFieldBytes)
FCFuncEnd()

FCFuncStart(gStubHelperFuncs)
    FCFuncElement("GetDelegateTarget", StubHelpers::GetDelegateTarget)
    FCFuncElement("TryGetStringTrailByte", StubHelpers::TryGetStringTrailByte)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
    FCFuncElement("InternalGetHRExceptionObject", StubHelpers::GetHRExceptionObject)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("InternalGetCOMHRExceptionObject", StubHelpers::GetCOMHRExceptionObject)
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
#endif // FEATURE_COMINTEROP
#ifdef PROFILING_SUPPORTED
    FCFuncElement("ProfilerBeginTransitionCallback", StubHelpers::ProfilerBeginTransitionCallback)
    FCFuncElement("ProfilerEndTransitionCallback", StubHelpers::ProfilerEndTransitionCallback)
#endif
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
    FCFuncElement("_InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("_InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
FCFuncEnd()

FCFuncStart(gStreamFuncs)
    FCFuncElement("HasOverriddenBeginEndRead", StreamNative::HasOverriddenBeginEndRead)
    FCFuncElement("HasOverriddenBeginEndWrite", StreamNative::HasOverriddenBeginEndWrite)
FCFuncEnd()

FCFuncStart(gComAwareWeakReferenceFuncs)
    FCFuncElement("HasInteropInfo", ComAwareWeakReferenceNative::HasInteropInfo)
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

FCClassElement("Array", "System", gArrayFuncs)
FCClassElement("AssemblyLoadContext", "System.Runtime.Loader", gAssemblyLoadContextFuncs)
FCClassElement("Buffer", "System", gBufferFuncs)
FCClassElement("CastHelpers", "System.Runtime.CompilerServices", gCastHelpers)
FCClassElement("ComAwareWeakReference", "System", gComAwareWeakReferenceFuncs)
FCClassElement("CompatibilitySwitch", "System.Runtime.Versioning", gCompatibilitySwitchFuncs)
FCClassElement("CustomAttribute", "System.Reflection", gCOMCustomAttributeFuncs)
FCClassElement("Debugger", "System.Diagnostics", gDiagnosticsDebugger)
FCClassElement("Delegate", "System", gDelegateFuncs)
FCClassElement("DependentHandle", "System.Runtime", gDependentHandleFuncs)
FCClassElement("Enum", "System", gEnumFuncs)
FCClassElement("Environment", "System", gEnvironmentFuncs)
FCClassElement("Exception", "System", gExceptionFuncs)
FCClassElement("GC", "System", gGCInterfaceFuncs)
FCClassElement("GCFrameRegistration", "System.Runtime", gGCFrameRegistration)
FCClassElement("GCHandle", "System.Runtime.InteropServices", gGCHandleFuncs)
FCClassElement("GCSettings", "System.Runtime", gGCSettingsFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("IEnumerable", "System.Collections", gStdMngIEnumerableFuncs)
FCClassElement("IEnumerator", "System.Collections", gStdMngIEnumeratorFuncs)
FCClassElement("IReflect", "System.Reflection", gStdMngIReflectFuncs)
#endif
FCClassElement("Interlocked", "System.Threading", gInterlockedFuncs)
FCClassElement("JitInfo", "System.Runtime", gJitInfoFuncs)
FCClassElement("Marshal", "System.Runtime.InteropServices", gInteropMarshalFuncs)
FCClassElement("Math", "System", gMathFuncs)
FCClassElement("MathF", "System", gMathFFuncs)
FCClassElement("MetadataImport", "System.Reflection", gMetaDataImport)
FCClassElement("MethodTable", "System.Runtime.CompilerServices", gMethodTableFuncs)
FCClassElement("ModuleHandle", "System", gCOMModuleHandleFuncs)
FCClassElement("Monitor", "System.Threading", gMonitorFuncs)
FCClassElement("Object", "System", gObjectFuncs)

FCClassElement("RuntimeAssembly", "System.Reflection", gRuntimeAssemblyFuncs)
FCClassElement("RuntimeFieldHandle", "System", gCOMFieldHandleNewFuncs)
FCClassElement("RuntimeHelpers", "System.Runtime.CompilerServices", gRuntimeHelpers)
FCClassElement("RuntimeMethodHandle", "System", gRuntimeMethodHandle)
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
#ifdef FEATURE_COMINTEROP
FCClassElement("Variant", "System", gVariantFuncs)
#endif
FCClassElement("WaitHandle", "System.Threading", gWaitHandleFuncs)

#undef FCFuncElement
#undef FCFuncElementSig
#undef FCDynamic
#undef FCDynamicSig
#undef FCUnreferenced
#undef FCFuncStart
#undef FCFuncEnd
#undef FCClassElement
