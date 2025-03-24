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
    FCFuncElement("get_CurrentManagedThreadId", JIT_GetCurrentManagedThreadId)
    FCFuncElement("get_TickCount", SystemNative::GetTickCount)
    FCFuncElement("get_TickCount64", SystemNative::GetTickCount64)
    FCFuncElement("set_ExitCode", SystemNative::SetExitCode)
    FCFuncElement("get_ExitCode", SystemNative::GetExitCode)
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
    FCFuncElement("GetMarshalAs", MetaDataImport::GetMarshalAs)
FCFuncEnd()

FCFuncStart(gSignatureNative)
    FCFuncElement("GetParameterOffsetInternal", SignatureNative::GetParameterOffsetInternal)
    FCFuncElement("GetTypeParameterOffsetInternal", SignatureNative::GetTypeParameterOffsetInternal)
    FCFuncElement("GetCallingConventionFromFunctionPointerAtOffsetInternal", SignatureNative::GetCallingConventionFromFunctionPointerAtOffsetInternal)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
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
    FCFuncElement("GetResolver", RuntimeMethodHandle::GetResolver)
    FCFuncElement("GetLoaderAllocatorInternal", RuntimeMethodHandle::GetLoaderAllocatorInternal)
FCFuncEnd()

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
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
    FCFuncElement("CatchAtSafePoint", ThreadNative::CatchAtSafePoint)
    FCFuncElement("get_OptimalMaxSpinWaitsPerSpinIteration", ThreadNative::GetOptimalMaxSpinWaitsPerSpinIteration)
FCFuncEnd()

FCFuncStart(gCastHelpers)
    FCFuncElement("WriteBarrier", ::WriteBarrier_Helper)
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
    FCFuncElement("Exchange32", COMInterlocked::Exchange32)
    FCFuncElement("Exchange64", COMInterlocked::Exchange64)
    FCFuncElement("ExchangeObject", COMInterlocked::ExchangeObject)
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
    FCFuncElement("IsEnteredNative", ObjectNative::IsLockHeld)

    FCFuncElement("TryEnter_FastPath", ObjectNative::Monitor_TryEnter_FastPath)
    FCFuncElement("TryEnter_FastPath_WithTimeout", ObjectNative::Monitor_TryEnter_FastPath_WithTimeout)
    FCFuncElement("Exit_FastPath", ObjectNative::Monitor_Exit_FastPath)
FCFuncEnd()

FCFuncStart(gRuntimeHelpers)
    FCFuncElement("TryGetHashCode", ObjectNative::TryGetHashCode)
    FCFuncElement("ContentEquals", ObjectNative::ContentEquals)
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
    FCFuncElement("AllocTailCallArgBufferWorker", TailCallHelp::AllocTailCallArgBufferWorker)
    FCFuncElement("GetTailCallInfo", TailCallHelp::GetTailCallInfo)
    FCFuncElement("Box", JIT_Box)
FCFuncEnd()

FCFuncStart(gMethodTableFuncs)
    FCFuncElement("GetNumInstanceFieldBytes", MethodTableNative::GetNumInstanceFieldBytes)
    FCFuncElement("GetPrimitiveCorElementType", MethodTableNative::GetPrimitiveCorElementType)
    FCFuncElement("GetMethodTableMatchingParentClass", MethodTableNative::GetMethodTableMatchingParentClass)
    FCFuncElement("InstantiationArg0", MethodTableNative::InstantiationArg0)
FCFuncEnd()

FCFuncStart(gStubHelperFuncs)
    FCFuncElement("GetDelegateTarget", StubHelpers::GetDelegateTarget)
    FCFuncElement("TryGetStringTrailByte", StubHelpers::TryGetStringTrailByte)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
#endif // FEATURE_COMINTEROP
    FCFuncElement("CalcVaListSize", StubHelpers::CalcVaListSize)
    FCFuncElement("LogPinnedArgument", StubHelpers::LogPinnedArgument)
    FCFuncElement("GetStubContext", StubHelpers::GetStubContext)
    FCFuncElement("NextCallReturnAddress", StubHelpers::NextCallReturnAddress)
FCFuncEnd()

FCFuncStart(gGCHandleFuncs)
    FCFuncElement("_InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("_InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
FCFuncEnd()

FCFuncStart(gComAwareWeakReferenceFuncs)
    FCFuncElement("HasInteropInfo", ComAwareWeakReferenceNative::HasInteropInfo)
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
FCClassElement("Buffer", "System", gBufferFuncs)
FCClassElement("CastHelpers", "System.Runtime.CompilerServices", gCastHelpers)
FCClassElement("ComAwareWeakReference", "System", gComAwareWeakReferenceFuncs)
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
FCClassElement("Monitor", "System.Threading", gMonitorFuncs)

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
