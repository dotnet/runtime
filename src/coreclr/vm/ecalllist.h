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

FCFuncStart(gEnvironmentFuncs)
    FCFuncElement("set_ExitCode", EnvironmentNative::SetExitCode)
    FCFuncElement("get_ExitCode", EnvironmentNative::GetExitCode)
FCFuncEnd()

FCFuncStart(gExceptionFuncs)
    FCFuncElement("IsImmutableAgileException", ExceptionNative::IsImmutableAgileException)
    FCFuncElement("PrepareForForeignExceptionRaise", ExceptionNative::PrepareForForeignExceptionRaise)
    FCFuncElement("GetExceptionCount", ExceptionNative::GetExceptionCount)
FCFuncEnd()

FCFuncStart(gCOMTypeHandleFuncs)
    FCFuncElement("GetFirstIntroducedMethod", RuntimeTypeHandle::GetFirstIntroducedMethod)
    FCFuncElement("GetNextIntroducedMethod", RuntimeTypeHandle::GetNextIntroducedMethod)
    FCFuncElement("GetAssemblyIfExists", RuntimeTypeHandle::GetAssemblyIfExists)
    FCFuncElement("GetModuleIfExists", RuntimeTypeHandle::GetModuleIfExists)
    FCFuncElement("GetElementTypeHandle", RuntimeTypeHandle::GetElementTypeHandle)
    FCFuncElement("GetArrayRank", RuntimeTypeHandle::GetArrayRank)
    FCFuncElement("GetToken", RuntimeTypeHandle::GetToken)
    FCFuncElement("GetUtf8NameInternal", RuntimeTypeHandle::GetUtf8Name)
    FCFuncElement("GetAttributes", RuntimeTypeHandle::GetAttributes)
    FCFuncElement("GetNumVirtuals", RuntimeTypeHandle::GetNumVirtuals)
    FCFuncElement("GetGenericVariableIndex", RuntimeTypeHandle::GetGenericVariableIndex)
    FCFuncElement("IsGenericVariable", RuntimeTypeHandle::IsGenericVariable)
    FCFuncElement("ContainsGenericVariables", RuntimeTypeHandle::ContainsGenericVariables)
    FCFuncElement("IsUnmanagedFunctionPointer", RuntimeTypeHandle::IsUnmanagedFunctionPointer)
    FCFuncElement("CompareCanonicalHandles", RuntimeTypeHandle::CompareCanonicalHandles)
    FCFuncElement("InternalAllocNoChecks_FastPath", RuntimeTypeHandle::InternalAllocNoChecks_FastPath)
FCFuncEnd()

FCFuncStart(gMetaDataImport)
    FCFuncElement("GetMetadataImport", MetaDataImport::GetMetadataImport)
    FCFuncElement("GetDefaultValue", MetaDataImport::GetDefaultValue)
    FCFuncElement("GetName", MetaDataImport::GetName)
    FCFuncElement("GetUserString", MetaDataImport::GetUserString)
    FCFuncElement("GetScopeProps", MetaDataImport::GetScopeProps)
    FCFuncElement("GetClassLayout", MetaDataImport::GetClassLayout)
    FCFuncElement("GetSignatureFromToken", MetaDataImport::GetSignatureFromToken)
    FCFuncElement("GetNamespace", MetaDataImport::GetNamespace)
    FCFuncElement("GetEventProps", MetaDataImport::GetEventProps)
    FCFuncElement("GetFieldDefProps", MetaDataImport::GetFieldDefProps)
    FCFuncElement("GetPropertyProps", MetaDataImport::GetPropertyProps)
    FCFuncElement("GetParentToken", MetaDataImport::GetParentToken)
    FCFuncElement("GetParamDefProps", MetaDataImport::GetParamDefProps)
    FCFuncElement("GetGenericParamProps", MetaDataImport::GetGenericParamProps)

    FCFuncElement("GetMemberRefProps", MetaDataImport::GetMemberRefProps)
    FCFuncElement("GetCustomAttributeProps", MetaDataImport::GetCustomAttributeProps)
    FCFuncElement("GetFieldOffset", MetaDataImport::GetFieldOffset)

    FCFuncElement("GetSigOfFieldDef", MetaDataImport::GetSigOfFieldDef)
    FCFuncElement("GetSigOfMethodDef", MetaDataImport::GetSigOfMethodDef)
    FCFuncElement("GetFieldMarshal", MetaDataImport::GetFieldMarshal)
    FCFuncElement("GetPInvokeMap", MetaDataImport::GetPInvokeMap)
    FCFuncElement("IsValidToken", MetaDataImport::IsValidToken)
FCFuncEnd()

FCFuncStart(gSignatureNative)
    FCFuncElement("GetParameterOffsetInternal", SignatureNative::GetParameterOffsetInternal)
    FCFuncElement("GetTypeParameterOffsetInternal", SignatureNative::GetTypeParameterOffsetInternal)
    FCFuncElement("GetCallingConventionFromFunctionPointerAtOffsetInternal", SignatureNative::GetCallingConventionFromFunctionPointerAtOffsetInternal)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
    FCFuncElement("IsCollectible", RuntimeMethodHandle::IsCollectible)
    FCFuncElement("GetAttributes", RuntimeMethodHandle::GetAttributes)
    FCFuncElement("GetMethodTable", RuntimeMethodHandle::GetMethodTable)
    FCFuncElement("GetSlot", RuntimeMethodHandle::GetSlot)
    FCFuncElement("GetMethodDef", RuntimeMethodHandle::GetMethodDef)
    FCFuncElement("GetUtf8NameInternal", RuntimeMethodHandle::GetUtf8Name)
    FCFuncElement("HasMethodInstantiation", RuntimeMethodHandle::HasMethodInstantiation)
    FCFuncElement("IsGenericMethodDefinition", RuntimeMethodHandle::IsGenericMethodDefinition)
    FCFuncElement("GetGenericParameterCount", RuntimeMethodHandle::GetGenericParameterCount)
    FCFuncElement("IsTypicalMethodDefinition", RuntimeMethodHandle::IsTypicalMethodDefinition)
    FCFuncElement("GetStubIfNeededInternal", RuntimeMethodHandle::GetStubIfNeededInternal)
    FCFuncElement("GetMethodFromCanonical", RuntimeMethodHandle::GetMethodFromCanonical)
    FCFuncElement("IsDynamicMethod", RuntimeMethodHandle::IsDynamicMethod)
    FCFuncElement("IsConstructor", RuntimeMethodHandle::IsConstructor)
    FCFuncElement("IsAsyncMethod", RuntimeMethodHandle::IsAsyncMethod)
    FCFuncElement("GetResolver", RuntimeMethodHandle::GetResolver)
    FCFuncElement("GetLoaderAllocatorInternal", RuntimeMethodHandle::GetLoaderAllocatorInternal)
FCFuncEnd()

#ifdef FEATURE_INTERPRETER
FCFuncStart(gAsyncHelpers)
    FCFuncElement("ResumeInterpreterContinuation", AsyncHelpers_ResumeInterpreterContinuation)
FCFuncEnd()
#endif // FEATURE_INTERPRETER

FCFuncStart(gCOMFieldHandleNewFuncs)
    FCFuncElement("GetUtf8NameInternal", RuntimeFieldHandle::GetUtf8Name)
    FCFuncElement("GetAttributes", RuntimeFieldHandle::GetAttributes)
    FCFuncElement("GetApproxDeclaringMethodTable", RuntimeFieldHandle::GetApproxDeclaringMethodTable)
    FCFuncElement("GetToken", RuntimeFieldHandle::GetToken)
    FCFuncElement("GetStaticFieldForGenericType", RuntimeFieldHandle::GetStaticFieldForGenericType)
    FCFuncElement("AcquiresContextFromThis", RuntimeFieldHandle::AcquiresContextFromThis)
    FCFuncElement("GetLoaderAllocatorInternal", RuntimeFieldHandle::GetLoaderAllocatorInternal)
    FCFuncElement("IsFastPathSupported", RuntimeFieldHandle::IsFastPathSupported)
    FCFuncElement("GetInstanceFieldOffset", RuntimeFieldHandle::GetInstanceFieldOffset)
    FCFuncElement("GetStaticFieldAddress", RuntimeFieldHandle::GetStaticFieldAddress)
FCFuncEnd()

FCFuncStart(gRuntimeAssemblyFuncs)
    FCFuncElement("GetIsDynamic", AssemblyNative::GetIsDynamic)
    FCFuncElement("GetIsCollectible", AssemblyNative::GetIsCollectible)
    FCFuncElement("GetManifestModule", AssemblyHandle::GetManifestModule)
    FCFuncElement("GetTokenInternal", AssemblyHandle::GetTokenInternal)
FCFuncEnd()

FCFuncStart(gAssemblyLoadContextFuncs)
    FCFuncElement("IsTracingEnabled", AssemblyNative::IsTracingEnabled)
FCFuncEnd()

FCFuncStart(gDelegateFuncs)
    FCFuncElement("GetMulticastInvoke", COMDelegate::GetMulticastInvoke)
    FCFuncElement("GetInvokeMethod", COMDelegate::GetInvokeMethod)
FCFuncEnd()

FCFuncStart(gMathFuncs)
    FCFuncElement("Acos", Rhp_acos)
    FCFuncElement("Acosh", Rhp_acosh)
    FCFuncElement("Asin", Rhp_asin)
    FCFuncElement("Asinh", Rhp_asinh)
    FCFuncElement("Atan", Rhp_atan)
    FCFuncElement("Atanh", Rhp_atanh)
    FCFuncElement("Atan2", Rhp_atan2)
    FCFuncElement("Cbrt", Rhp_cbrt)
    FCFuncElement("Ceiling", Rhp_ceil)
    FCFuncElement("Cos", Rhp_cos)
    FCFuncElement("Cosh", Rhp_cosh)
    FCFuncElement("Exp", Rhp_exp)
    FCFuncElement("Floor", Rhp_floor)
    FCFuncElement("FusedMultiplyAdd", Rhp_fma)
    FCFuncElement("Log", Rhp_log)
    FCFuncElement("Log2", Rhp_log2)
    FCFuncElement("Log10", Rhp_log10)
    FCFuncElement("ModF", Rhp_modf)
    FCFuncElement("Pow", Rhp_pow)
    FCFuncElement("Sin", Rhp_sin)
    FCFuncElement("SinCos", Rhp_sincos)
    FCFuncElement("Sinh", Rhp_sinh)
    FCFuncElement("Sqrt", Rhp_sqrt)
    FCFuncElement("Tan", Rhp_tan)
    FCFuncElement("Tanh", Rhp_tanh)
#ifdef TARGET_32BIT
    FCFuncElement("DivInt32Internal", DivInt32Internal)
    FCFuncElement("DivUInt32Internal", DivUInt32Internal)
    FCFuncElement("DivInt64Internal", DivInt64Internal)
    FCFuncElement("DivUInt64Internal", DivUInt64Internal)
    FCFuncElement("ModInt32Internal", ModInt32Internal)
    FCFuncElement("ModUInt32Internal", ModUInt32Internal)
    FCFuncElement("ModInt64Internal", ModInt64Internal)
    FCFuncElement("ModUInt64Internal", ModUInt64Internal)
#endif // TARGET_32BIT
FCFuncEnd()

FCFuncStart(gMathFFuncs)
    FCFuncElement("Acos", Rhp_acosf)
    FCFuncElement("Acosh", Rhp_acoshf)
    FCFuncElement("Asin", Rhp_asinf)
    FCFuncElement("Asinh", Rhp_asinhf)
    FCFuncElement("Atan", Rhp_atanf)
    FCFuncElement("Atanh", Rhp_atanhf)
    FCFuncElement("Atan2", Rhp_atan2f)
    FCFuncElement("Cbrt", Rhp_cbrtf)
    FCFuncElement("Ceiling", Rhp_ceilf)
    FCFuncElement("Cos", Rhp_cosf)
    FCFuncElement("Cosh", Rhp_coshf)
    FCFuncElement("Exp", Rhp_expf)
    FCFuncElement("Floor", Rhp_floorf)
    FCFuncElement("FusedMultiplyAdd", Rhp_fmaf)
    FCFuncElement("Log", Rhp_logf)
    FCFuncElement("Log2", Rhp_log2f)
    FCFuncElement("Log10", Rhp_log10f)
    FCFuncElement("ModF", Rhp_modff)
    FCFuncElement("Pow", Rhp_powf)
    FCFuncElement("Sin", Rhp_sinf)
    FCFuncElement("SinCos", Rhp_sincosf)
    FCFuncElement("Sinh", Rhp_sinhf)
    FCFuncElement("Sqrt", Rhp_sqrtf)
    FCFuncElement("Tan", Rhp_tanf)
    FCFuncElement("Tanh", Rhp_tanhf)
FCFuncEnd()

FCFuncStart(gThreadFuncs)
#ifdef FEATURE_MULTITHREADING
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
    FCFuncElement("CatchAtSafePoint", ThreadNative::CatchAtSafePoint)
    FCFuncElement("CurrentThreadIsFinalizerThread", ThreadNative::CurrentThreadIsFinalizerThread)
#endif // FEATURE_MULTITHREADING
FCFuncEnd()

FCFuncStart(gObjectHeaderFuncs)
    FCFuncElement("GetLockHandleIfExists", ObjectHeader_GetLockHandleIfExists)
FCFuncEnd()


FCFuncStart(gArrayFuncs)
    FCFuncElement("GetCorElementTypeOfElementType", ArrayNative::GetCorElementTypeOfElementType)
FCFuncEnd()

FCFuncStart(gBufferFuncs)
    FCFuncElement("BulkMoveWithWriteBarrierInternal", Buffer::BulkMoveWithWriteBarrier)
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
    FCFuncElement("GetGenerationInternal", GCInterface::GetGenerationInternal)
    FCFuncElement("GetMaxGeneration", GCInterface::GetMaxGeneration)
    FCFuncElement("SuppressFinalizeInternal", GCInterface::SuppressFinalize)

    FCFuncElement("GetAllocatedBytesForCurrentThread", GCInterface::GetAllocatedBytesForCurrentThread)
    FCFuncElement("GetTotalAllocatedBytesApproximate", GCInterface::GetTotalAllocatedBytesApproximate)
FCFuncEnd()

FCFuncStart(gGCSettingsFuncs)
    FCFuncElement("get_IsServerGC", GCInterface::IsServerGC)
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
    FCFuncElement("Exchange32", COMInterlocked::Exchange32)
    FCFuncElement("Exchange64", COMInterlocked::Exchange64)
    FCFuncElement("ExchangeObject", COMInterlocked::ExchangeObject)
    FCFuncElement("CompareExchange32", COMInterlocked::CompareExchange32)
    FCFuncElement("CompareExchange32Pointer", COMInterlocked::CompareExchange32)
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

FCFuncStart(gRuntimeHelpers)
    FCFuncElement("TryGetHashCode", ObjectNative::TryGetHashCode)
    FCFuncElement("ContentEquals", ObjectNative::ContentEquals)
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
    FCFuncElement("GetTailCallArgBuffer", TailCallHelp::GetTailCallArgBuffer)
    FCFuncElement("GetTailCallInfo", TailCallHelp::GetTailCallInfo)
FCFuncEnd()

FCFuncStart(gMethodTableFuncs)
    FCFuncElement("GetNumInstanceFieldBytes", MethodTableNative::GetNumInstanceFieldBytes)
    FCFuncElement("GetPrimitiveCorElementType", MethodTableNative::GetPrimitiveCorElementType)
    FCFuncElement("GetMethodTableMatchingParentClass", MethodTableNative::GetMethodTableMatchingParentClass)
    FCFuncElement("GetLoaderAllocatorHandle", MethodTableNative::GetLoaderAllocatorHandle)
FCFuncEnd()

FCFuncStart(gStubHelperFuncs)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_VARARGS
    FCFuncElement("CalcVaListSize", StubHelpers::CalcVaListSize)
#endif // FEATURE_VARARGS
    FCFuncElement("LogPinnedArgument", StubHelpers::LogPinnedArgument)
FCFuncEnd()

FCFuncStart(gGCHandleFuncs)
    FCFuncElement("_InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("_InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
#ifdef FEATURE_JAVAMARSHAL
    FCFuncElement("InternalTryGetBridgeWait", MarshalNative::GCHandleInternalTryGetBridgeWait)
#endif
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
FCFuncEnd()

//
//
// Class definitions
//
//

// Note these have to remain sorted by name:namespace pair (Assert will wack you if you don't)
// The sorting is case-sensitive

FCClassElement("Array", "System", gArrayFuncs)
FCClassElement("AssemblyLoadContext", "System.Runtime.Loader", gAssemblyLoadContextFuncs)
#ifdef FEATURE_INTERPRETER
FCClassElement("AsyncHelpers", "System.Runtime.CompilerServices", gAsyncHelpers)
#endif
FCClassElement("Buffer", "System", gBufferFuncs)
FCClassElement("Delegate", "System", gDelegateFuncs)
FCClassElement("DependentHandle", "System.Runtime", gDependentHandleFuncs)
FCClassElement("Environment", "System", gEnvironmentFuncs)
FCClassElement("Exception", "System", gExceptionFuncs)
FCClassElement("GC", "System", gGCInterfaceFuncs)
FCClassElement("GCFrameRegistration", "System.Runtime", gGCFrameRegistration)
FCClassElement("GCHandle", "System.Runtime.InteropServices", gGCHandleFuncs)
FCClassElement("GCSettings", "System.Runtime", gGCSettingsFuncs)
FCClassElement("Interlocked", "System.Threading", gInterlockedFuncs)
FCClassElement("JitInfo", "System.Runtime", gJitInfoFuncs)
FCClassElement("Marshal", "System.Runtime.InteropServices", gInteropMarshalFuncs)
FCClassElement("Math", "System", gMathFuncs)
FCClassElement("MathF", "System", gMathFFuncs)
FCClassElement("MetadataImport", "System.Reflection", gMetaDataImport)
FCClassElement("MethodTable", "System.Runtime.CompilerServices", gMethodTableFuncs)
FCClassElement("ObjectHeader", "System.Threading", gObjectHeaderFuncs)
FCClassElement("RuntimeAssembly", "System.Reflection", gRuntimeAssemblyFuncs)
FCClassElement("RuntimeFieldHandle", "System", gCOMFieldHandleNewFuncs)
FCClassElement("RuntimeHelpers", "System.Runtime.CompilerServices", gRuntimeHelpers)
FCClassElement("RuntimeMethodHandle", "System", gRuntimeMethodHandle)
FCClassElement("RuntimeTypeHandle", "System", gCOMTypeHandleFuncs)

FCClassElement("Signature", "System", gSignatureNative)
FCClassElement("String", "System", gStringFuncs)
FCClassElement("StubHelpers", "System.StubHelpers", gStubHelperFuncs)

FCClassElement("Thread", "System.Threading", gThreadFuncs)

#undef FCFuncElement
#undef FCFuncElementSig
#undef FCDynamic
#undef FCDynamicSig
#undef FCUnreferenced
#undef FCFuncStart
#undef FCFuncEnd
#undef FCClassElement
