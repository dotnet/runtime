// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

#ifndef FCIntrinsic
#define FCIntrinsic(name,impl,intrinsicID)
#endif

#ifndef FCIntrinsicSig
#define FCIntrinsicSig(name,sig,impl,intrinsicID)
#endif

#ifndef QCFuncElement
#define QCFuncElement(name,impl)
#endif

#ifndef FCDynamic
#define FCDynamic(name,intrinsicID,dynamicID)
#endif

#ifndef FCDynamicSig
#define FCDynamicSig(name,sig,intrinsicID,dynamicID)
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

#ifdef FEATURE_REMOTING
FCFuncStart(gMarshalByRefFuncs)
    FCFuncElement("GetComIUnknown", RemotingNative::GetComIUnknown)
FCFuncEnd()

FCFuncStart(gRemotingFuncs)
    FCFuncElement("IsTransparentProxy", RemotingNative::FCIsTransparentProxy)
    FCFuncElement("GetRealProxy", RemotingNative::FCGetRealProxy)
    FCFuncElement("Unwrap", RemotingNative::FCUnwrap)
    FCFuncElement("AlwaysUnwrap", RemotingNative::FCAlwaysUnwrap)
    FCFuncElement("CheckCast", RemotingNative::NativeCheckCast)
    FCFuncElement("nSetRemoteActivationConfigured", RemotingNative::SetRemotingConfiguredFlag)

    FCFuncElement("CORProfilerTrackRemoting", ProfilingFCallHelper::FC_TrackRemoting)
    FCFuncElement("CORProfilerTrackRemotingCookie", ProfilingFCallHelper::FC_TrackRemotingCookie)
    FCFuncElement("CORProfilerTrackRemotingAsync", ProfilingFCallHelper::FC_TrackRemotingAsync)
    FCFuncElement("CORProfilerRemotingClientSendingMessage", ProfilingFCallHelper::FC_RemotingClientSendingMessage)
    FCFuncElement("CORProfilerRemotingClientReceivingReply", ProfilingFCallHelper::FC_RemotingClientReceivingReply)
    FCFuncElement("CORProfilerRemotingServerReceivingMessage", ProfilingFCallHelper::FC_RemotingServerReceivingMessage)
    FCFuncElement("CORProfilerRemotingServerSendingReply", ProfilingFCallHelper::FC_RemotingServerSendingReply)

    FCFuncElement("CreateTransparentProxy", RemotingNative::CreateTransparentProxy)
    FCFuncElement("AllocateUninitializedObject", RemotingNative::AllocateUninitializedObject)
    FCFuncElement("CallDefaultCtor", RemotingNative::CallDefaultCtor)
    FCFuncElement("AllocateInitializedObject", RemotingNative::AllocateInitializedObject)
    FCFuncElement("ResetInterfaceCache", RemotingNative::ResetInterfaceCache)
FCFuncEnd()

FCFuncStart(gRealProxyFuncs)
    FCFuncElement("SetStubData", CRealProxy::SetStubData)
    FCFuncElement("GetStubData", CRealProxy::GetStubData)
    FCFuncElement("GetStub", CRealProxy::GetStub)
    FCFuncElement("GetDefaultStub", CRealProxy::GetDefaultStub)
    FCFuncElement("GetProxiedType", CRealProxy::GetProxiedType)
FCFuncEnd()

FCFuncStart(gContextFuncs)
    FCFuncElement("SetupInternalContext", Context::SetupInternalContext)
    FCFuncElement("CleanupInternalContext", Context::CleanupInternalContext)
    FCFuncElement("ExecuteCallBackInEE", Context::ExecuteCallBack)
FCFuncEnd()
#endif


FCFuncStart(gDependentHandleFuncs)
    FCFuncElement("nInitialize",             DependentHandle::nInitialize)
    FCFuncElement("nGetPrimary",             DependentHandle::nGetPrimary)
    FCFuncElement("nGetPrimaryAndSecondary", DependentHandle::nGetPrimaryAndSecondary)
    FCFuncElement("nFree",                   DependentHandle::nFree)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gSizedRefHandleFuncs)
    FCFuncElement("CreateSizedRef",                 SizedRefHandle::Initialize)
    FCFuncElement("FreeSizedRef",                   SizedRefHandle::Free)
    FCFuncElement("GetTargetOfSizedRef",            SizedRefHandle::GetTarget)
    FCFuncElement("GetApproximateSizeOfSizedRef",   SizedRefHandle::GetApproximateSize)
FCFuncEnd()
#endif // !FEATURE_CORECLR

#ifdef FEATURE_RWLOCK
FCFuncStart(gRWLockFuncs)
    FCFuncElement("AcquireReaderLockInternal",  CRWLock::StaticAcquireReaderLockPublic)
    FCFuncElement("AcquireWriterLockInternal",  CRWLock::StaticAcquireWriterLockPublic)
    FCFuncElement("ReleaseReaderLockInternal",  CRWLock::StaticReleaseReaderLockPublic)
    FCFuncElement("ReleaseWriterLockInternal",  CRWLock::StaticReleaseWriterLockPublic)
    FCFuncElement("FCallUpgradeToWriterLock",  CRWLock::StaticDoUpgradeToWriterLockPublic)
    FCFuncElement("DowngradeFromWriterLockInternal",  CRWLock::StaticDowngradeFromWriterLock)
    FCFuncElement("FCallReleaseLock",  CRWLock::StaticDoReleaseLock)
    FCFuncElement("RestoreLockInternal",  CRWLock::StaticRestoreLockPublic)
    FCFuncElement("PrivateGetIsReaderLockHeld",  CRWLock::StaticIsReaderLockHeld)
    FCFuncElement("PrivateGetIsWriterLockHeld",  CRWLock::StaticIsWriterLockHeld)
    FCFuncElement("PrivateGetWriterSeqNum",  CRWLock::StaticGetWriterSeqNum)
    FCFuncElement("AnyWritersSince",  CRWLock::StaticAnyWritersSince)
    FCFuncElement("PrivateInitialize",  CRWLock::StaticPrivateInitialize)
    FCFuncElement("PrivateDestruct",  CRWLock::StaticPrivateDestruct)
FCFuncEnd()
#endif // FEATURE_RWLOCK

#ifdef FEATURE_REMOTING
FCFuncStart(gMessageFuncs)
    FCFuncElement("InternalGetArgCount", CMessage::GetArgCount)
    FCFuncElement("InternalHasVarArgs", CMessage::HasVarArgs)
    FCFuncElement("InternalGetArg", CMessage::GetArg)
    FCFuncElement("InternalGetArgs", CMessage::GetArgs)
    FCFuncElement("PropagateOutParameters", CMessage::PropagateOutParameters)
    FCFuncElement("GetReturnValue", CMessage::GetReturnValue)
    FCFuncElement("GetAsyncBeginInfo", CMessage::GetAsyncBeginInfo)
    FCFuncElement("GetAsyncResult", CMessage::GetAsyncResult)
    FCFuncElement("GetThisPtr", CMessage::GetAsyncObject)
    FCFuncElement("OutToUnmanagedDebugger", CMessage::DebugOut)
    FCFuncElement("Dispatch", CMessage::Dispatch)
FCFuncEnd()
#endif //FEATURE_REMOTING

#ifdef FEATURE_REMOTING
FCFuncStart(gChannelServicesFuncs)
    FCFuncElement("GetPrivateContextsPerfCounters", GetPrivateContextsPerfCountersEx)
FCFuncEnd()
#endif // FEATURE_REMOTING

FCFuncStart(gEnumFuncs)
    FCFuncElement("InternalGetUnderlyingType",  ReflectionEnum::InternalGetEnumUnderlyingType)
    FCFuncElement("InternalGetCorElementType",  ReflectionEnum::InternalGetCorElementType)
    QCFuncElement("GetEnumValuesAndNames",  ReflectionEnum::GetEnumValuesAndNames)
    FCFuncElement("InternalBoxEnum", ReflectionEnum::InternalBoxEnum)
    FCFuncElement("Equals", ReflectionEnum::InternalEquals)
    FCFuncElement("InternalCompareTo", ReflectionEnum::InternalCompareTo)    
    FCFuncElement("InternalHasFlag", ReflectionEnum::InternalHasFlag)
FCFuncEnd()

#ifdef FEATURE_REMOTING
FCFuncStart(gStackBuilderSinkFuncs)
    FCFuncElement("_PrivateProcessMessage", CStackBuilderSink::PrivateProcessMessage)
FCFuncEnd()
#endif

#ifdef FEATURE_CORECLR
FCFuncStart(gSymWrapperCodePunkSafeHandleFuncs)
    FCFuncElement("nGetDReleaseTarget", COMPunkSafeHandle::nGetDReleaseTarget)
FCFuncEnd()
#endif //FEATURE_CORECLR

FCFuncStart(gParseNumbersFuncs)
    FCFuncElement("IntToString", ParseNumbers::IntToString)
    FCFuncElement("LongToString", ParseNumbers::LongToString)
    FCFuncElement("StringToInt", ParseNumbers::StringToInt)
    FCFuncElement("StringToLong", ParseNumbers::StringToLong)
FCFuncEnd()

#ifndef FEATURE_CORECLR  // FCalls used by System.TimeSpan
FCFuncStart(gTimeSpanFuncs)
    FCFuncElement("LegacyFormatMode", SystemNative::LegacyFormatMode)
FCFuncEnd()
#endif // !FEATURE_CORECLR

#ifndef FEATURE_CORECLR  // FCalls used by System.TimeZone
FCFuncStart(gTimeZoneFuncs)
    FCFuncElement("nativeGetTimeZoneMinuteOffset", COMNlsInfo::nativeGetTimeZoneMinuteOffset)
    FCFuncElement("nativeGetStandardName", COMNlsInfo::nativeGetStandardName)
    FCFuncElement("nativeGetDaylightName", COMNlsInfo::nativeGetDaylightName)
    FCFuncElement("nativeGetDaylightChanges", COMNlsInfo::nativeGetDaylightChanges)
FCFuncEnd()
#endif // FEATURE_CORECLR

FCFuncStart(gObjectFuncs)
    FCIntrinsic("GetType", ObjectNative::GetClass, CORINFO_INTRINSIC_Object_GetType)
    FCFuncElement("MemberwiseClone", ObjectNative::Clone)
FCFuncEnd()

FCFuncStart(gStringFuncs)
    FCDynamic("FastAllocateString", CORINFO_INTRINSIC_Illegal, ECall::FastAllocateString)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharArrayManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_ArrChar_Int_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharArrayStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharPtrManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrChar_Int_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharPtrStartLengthManaged)
    FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_Char_Int_RetVoid, CORINFO_INTRINSIC_Illegal, ECall::CtorCharCountManaged)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_RetVoid, COMString::StringInitCharPtr)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_RetVoid, COMString::StringInitCharPtrPartial)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_PtrSByt_Int_Int_Encoding_RetVoid, COMString::StringInitSBytPtrPartialEx)
    FCFuncElement("IsFastSort", COMString::IsFastSort)
    FCFuncElement("nativeCompareOrdinalIgnoreCaseWC", COMString::FCCompareOrdinalIgnoreCaseWC)
    FCIntrinsic("get_Length", COMString::Length, CORINFO_INTRINSIC_StringLength)
    FCIntrinsic("get_Chars", COMString::GetCharAt, CORINFO_INTRINSIC_StringGetChar)
    FCFuncElement("IsAscii", COMString::IsAscii)
    FCFuncElement("CompareOrdinalHelper", COMString::CompareOrdinalEx)
    FCFuncElement("IndexOfAny", COMString::IndexOfCharArray)
    FCFuncElement("LastIndexOfAny", COMString::LastIndexOfCharArray)
    FCFuncElementSig("ReplaceInternal", &gsig_IM_Str_Str_RetStr, COMString::ReplaceString)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("SetTrailByte", COMString::FCSetTrailByte)
    FCFuncElement("TryGetTrailByte", COMString::FCTryGetTrailByte)
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
    FCFuncElement("InternalMarvin32HashString", COMString::Marvin32HashString)
    QCFuncElement("InternalUseRandomizedHashing", COMString::UseRandomizedHashing) 
#endif // FEATURE_RANDOMIZED_STRING_HASHING
FCFuncEnd()

FCFuncStart(gStringBufferFuncs)
    FCFuncElement("ReplaceBufferInternal", COMStringBuffer::ReplaceBufferInternal)
    FCFuncElement("ReplaceBufferAnsiInternal", COMStringBuffer::ReplaceBufferAnsiInternal)
FCFuncEnd()

FCFuncStart(gValueTypeFuncs)
    FCFuncElement("CanCompareBits", ValueTypeHelper::CanCompareBits)
    FCFuncElement("FastEqualsCheck", ValueTypeHelper::FastEqualsCheck)
    FCFuncElement("GetHashCode", ValueTypeHelper::GetHashCode)
    FCFuncElement("GetHashCodeOfPtr", ValueTypeHelper::GetHashCodeOfPtr)
FCFuncEnd()

FCFuncStart(gDiagnosticsDebugger)
    FCFuncElement("BreakInternal", DebugDebugger::Break)
    FCFuncElement("LaunchInternal", DebugDebugger::Launch)
    FCFuncElement("get_IsAttached", DebugDebugger::IsDebuggerAttached)
    FCFuncElement("Log", DebugDebugger::Log)
    FCFuncElement("IsLogging", DebugDebugger::IsLogging)
    FCFuncElement("CustomNotification", DebugDebugger::CustomNotification)
FCFuncEnd()

FCFuncStart(gDiagnosticsStackTrace)
    FCFuncElement("GetStackFramesInternal", DebugStackTrace::GetStackFramesInternal)
FCFuncEnd()

FCFuncStart(gDiagnosticsLog)
    FCFuncElement("AddLogSwitch", Log::AddLogSwitch)
    FCFuncElement("ModifyLogSwitch", Log::ModifyLogSwitch)
FCFuncEnd()

FCFuncStart(gDiagnosticsAssert)
    FCFuncElement("ShowDefaultAssertDialog", DebuggerAssert::ShowDefaultAssertDialog)
FCFuncEnd()

FCFuncStart(gDateTimeFuncs)
    FCFuncElement("GetSystemTimeAsFileTime", SystemNative::__GetSystemTimeAsFileTime)
#ifndef FEATURE_CORECLR
    QCFuncElement("LegacyParseMode", SystemNative::LegacyDateTimeParseMode)
    QCFuncElement("EnableAmPmParseAdjustment", SystemNative::EnableAmPmParseAdjustment)
#endif
FCFuncEnd()

FCFuncStart(gEnvironmentFuncs)
    FCFuncElement("GetVersion", SystemNative::GetOSVersion)
    FCFuncElement("GetVersionEx", SystemNative::GetOSVersionEx)
    FCFuncElement("get_TickCount", SystemNative::GetTickCount)
    QCFuncElement("_Exit", SystemNative::Exit)
    FCFuncElement("set_ExitCode", SystemNative::SetExitCode)
    FCFuncElement("get_ExitCode", SystemNative::GetExitCode)
    FCFuncElement("get_HasShutdownStarted", SystemNative::HasShutdownStarted)
    QCFuncElement("GetProcessorCount", SystemNative::GetProcessorCount)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetWorkingSet", SystemNative::GetWorkingSet)
    FCFuncElement("nativeGetEnvironmentVariable", SystemNative::_GetEnvironmentVariable)
    FCFuncElement("GetCompatibilityFlag", SystemNative::_GetCompatibilityFlag)
    QCFuncElement("GetCommandLine", SystemNative::_GetCommandLine)
    FCFuncElement("GetResourceFromDefault", GetResourceFromDefault)
#endif // !FEATURE_CORECLR
    FCFuncElement("GetCommandLineArgsNative", SystemNative::GetCommandLineArgs)

#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORESYSTEM)
    QCFuncElement("WinRTSupported", SystemNative::WinRTSupported)
#endif // FEATURE_COMINTEROP
    FCFuncElementSig("FailFast", &gsig_SM_Str_RetVoid, SystemNative::FailFast)
#ifndef FEATURE_CORECLR
    FCFuncElementSig("FailFast", &gsig_SM_Str_Uint_RetVoid, SystemNative::FailFastWithExitCode)
#endif
    FCFuncElementSig("FailFast", &gsig_SM_Str_Exception_RetVoid, SystemNative::FailFastWithException)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetIsCLRHosted", SystemNative::IsCLRHosted)
    QCFuncElement("TriggerCodeContractFailure", SystemNative::TriggerCodeContractFailure)
#endif // !FEATURE_CORECLR
FCFuncEnd()

FCFuncStart(gRuntimeEnvironmentFuncs)
    FCFuncElement("GetModuleFileName", SystemNative::_GetModuleFileName)
    FCFuncElement("GetRuntimeDirectoryImpl", SystemNative::GetRuntimeDirectory)
#ifdef FEATURE_FUSION
    FCFuncElement("GetDeveloperPath", SystemNative::GetDeveloperPath)
    FCFuncElement("GetHostBindingFile", SystemNative::GetHostBindingFile)
#endif // FEATURE_FUSION
#ifndef FEATURE_CORECLR
    QCFuncElement("_GetSystemVersion", SystemNative::_GetSystemVersion)
#endif
#if defined(FEATURE_CLASSIC_COMINTEROP) && !defined(FEATURE_CORECLR)
    QCFuncElement("GetRuntimeInterfaceImpl", SystemNative::GetRuntimeInterfaceImpl)
#endif
FCFuncEnd()

FCFuncStart(gSerializationFuncs)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetEnableUnsafeTypeForwarders", ReflectionSerialization::GetEnableUnsafeTypeForwarders)
    FCFuncElement("nativeGetSafeUninitializedObject", ReflectionSerialization::GetSafeUninitializedObject)
#endif
    FCFuncElement("nativeGetUninitializedObject", ReflectionSerialization::GetUninitializedObject)
FCFuncEnd()

FCFuncStart(gExceptionFuncs)
    FCFuncElement("IsImmutableAgileException", ExceptionNative::IsImmutableAgileException)
    FCFuncElement("nIsTransient", ExceptionNative::IsTransient)
    FCFuncElement("GetMethodFromStackTrace", SystemNative::GetMethodFromStackTrace)
#ifndef FEATURE_CORECLR
    FCFuncElement("StripFileInfo", ExceptionNative::StripFileInfo)
#endif
    QCFuncElement("GetMessageFromNativeResources", ExceptionNative::GetMessageFromNativeResources)
#if defined(FEATURE_EXCEPTIONDISPATCHINFO)
    FCFuncElement("PrepareForForeignExceptionRaise", ExceptionNative::PrepareForForeignExceptionRaise)
    FCFuncElement("CopyStackTrace", ExceptionNative::CopyStackTrace)
    FCFuncElement("CopyDynamicMethods", ExceptionNative::CopyDynamicMethods)
    FCFuncElement("GetStackTracesDeepCopy", ExceptionNative::GetStackTracesDeepCopy)
    FCFuncElement("SaveStackTracesFromDeepCopy", ExceptionNative::SaveStackTracesFromDeepCopy)
#endif // defined(FEATURE_EXCEPTIONDISPATCHINFO)
FCFuncEnd()

FCFuncStart(gSafeHandleFuncs)
    FCFuncElement("InternalDispose", SafeHandle::DisposeNative)
    FCFuncElement("InternalFinalize", SafeHandle::Finalize)
    FCFuncElement("SetHandleAsInvalid", SafeHandle::SetHandleAsInvalid)
    FCFuncElement("DangerousAddRef", SafeHandle::DangerousAddRef)
    FCFuncElement("DangerousRelease", SafeHandle::DangerousRelease)
FCFuncEnd()

FCFuncStart(gCriticalHandleFuncs)
    FCFuncElement("FireCustomerDebugProbe", CriticalHandle::FireCustomerDebugProbe)
FCFuncEnd()

FCFuncStart(gSafeBufferFuncs)
    FCFuncElement("PtrToStructureNative", SafeBuffer::PtrToStructure)
    FCFuncElement("StructureToPtrNative", SafeBuffer::StructureToPtr)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gNormalizationFuncs)
    FCFuncElement("nativeNormalizationIsNormalizedString", COMNlsInfo::nativeNormalizationIsNormalizedString)
    FCFuncElement("nativeNormalizationNormalizeString", COMNlsInfo::nativeNormalizationNormalizeString)
    QCFuncElement("nativeNormalizationInitNormalization", COMNlsInfo::nativeNormalizationInitNormalization)
FCFuncEnd()
#endif // FEATURE_CORECLR

FCFuncStart(gTypedReferenceFuncs)
    FCFuncElement("InternalToObject", ReflectionInvocation::TypedReferenceToObject)
#ifndef FEATURE_CORECLR
    FCFuncElement("InternalSetTypedReference", ReflectionInvocation::SetTypedReference)
    FCFuncElement("InternalMakeTypedReference", ReflectionInvocation::MakeTypedReference)
#endif
FCFuncEnd()

FCFuncStart(gSystem_Type)
    FCIntrinsic("GetTypeFromHandle", RuntimeTypeHandle::GetTypeFromHandle, CORINFO_INTRINSIC_GetTypeFromHandle)
    FCFuncElement("GetTypeFromHandleUnsafe", RuntimeTypeHandle::GetRuntimeType)
    FCIntrinsic("op_Equality", RuntimeTypeHandle::TypeEQ, CORINFO_INTRINSIC_TypeEQ)
    FCIntrinsic("op_Inequality", RuntimeTypeHandle::TypeNEQ, CORINFO_INTRINSIC_TypeNEQ)
FCFuncEnd()

FCFuncStart(gSystem_RuntimeType)
    FCFuncElement("GetGUID", ReflectionInvocation::GetGUID)
    FCFuncElement("_CreateEnum", ReflectionInvocation::CreateEnum)
    FCFuncElement("CanValueSpecialCast", ReflectionInvocation::CanValueSpecialCast)
    FCFuncElement("AllocateValueType", ReflectionInvocation::AllocateValueType)
#if defined(FEATURE_COMINTEROP)
    FCFuncElement("GetTypeFromCLSIDImpl", ReflectionInvocation::GetClassFromCLSID)
    FCFuncElement("GetTypeFromProgIDImpl", ReflectionInvocation::GetClassFromProgID)
    FCFuncElement("InvokeDispMethod", ReflectionInvocation::InvokeDispMethod)
#ifdef FEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION
    FCFuncElement("IsTypeExportedToWindowsRuntime", RuntimeTypeHandle::IsTypeExportedToWindowsRuntime)
#endif
    FCFuncElement("IsWindowsRuntimeObjectType", RuntimeTypeHandle::IsWindowsRuntimeObjectType)
#endif // defined(FEATURE_COMINTEROP) 
FCFuncEnd()

FCFuncStart(gJitHelpers)
    FCFuncElement("UnsafeSetArrayElement", JitHelpers::UnsafeSetArrayElement)
#ifdef _DEBUG
    FCFuncElement("IsAddressInStack", ReflectionInvocation::IsAddressInStack)
#endif
FCFuncEnd()

FCFuncStart(gCOMTypeHandleFuncs)
    FCFuncElement("CreateInstance", RuntimeTypeHandle::CreateInstance)
    FCFuncElement("CreateCaInstance", RuntimeTypeHandle::CreateCaInstance)
    FCFuncElement("CreateInstanceForAnotherGenericParameter", RuntimeTypeHandle::CreateInstanceForGenericType)
    QCFuncElement("GetGCHandle", RuntimeTypeHandle::GetGCHandle)

    FCFuncElement("IsInstanceOfType", RuntimeTypeHandle::IsInstanceOfType)
    FCFuncElement("GetDeclaringMethod", RuntimeTypeHandle::GetDeclaringMethod)
    FCFuncElement("GetDeclaringType", RuntimeTypeHandle::GetDeclaringType)
    QCFuncElement("GetDefaultConstructor", RuntimeTypeHandle::GetDefaultConstructor)
    QCFuncElement("MakePointer", RuntimeTypeHandle::MakePointer)
    QCFuncElement("MakeByRef", RuntimeTypeHandle::MakeByRef)
    QCFuncElement("MakeSZArray", RuntimeTypeHandle::MakeSZArray)
    QCFuncElement("MakeArray", RuntimeTypeHandle::MakeArray)
    QCFuncElement("IsCollectible", RuntimeTypeHandle::IsCollectible)
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
    QCFuncElement("GetConstraints", RuntimeTypeHandle::GetConstraints)
    FCFuncElement("GetAttributes", RuntimeTypeHandle::GetAttributes)
    FCFuncElement("_GetMetadataImport", RuntimeTypeHandle::GetMetadataImport)
    FCFuncElement("GetNumVirtuals", RuntimeTypeHandle::GetNumVirtuals)
    QCFuncElement("VerifyInterfaceIsImplemented", RuntimeTypeHandle::VerifyInterfaceIsImplemented)
    QCFuncElement("GetInterfaceMethodImplementationSlot", RuntimeTypeHandle::GetInterfaceMethodImplementationSlot)
    FCFuncElement("IsComObject", RuntimeTypeHandle::IsComObject)
#ifdef FEATURE_REMOTING        
    FCFuncElement("HasProxyAttribute", RuntimeTypeHandle::HasProxyAttribute)
    FCFuncElement("IsContextful", RuntimeTypeHandle::IsContextful)
#endif    
    FCFuncElement("IsValueType", RuntimeTypeHandle::IsValueType)
    FCFuncElement("IsInterface", RuntimeTypeHandle::IsInterface)
    QCFuncElement("IsSecurityCritical", RuntimeTypeHandle::IsSecurityCritical)
    QCFuncElement("IsSecuritySafeCritical", RuntimeTypeHandle::IsSecuritySafeCritical)
    QCFuncElement("IsSecurityTransparent", RuntimeTypeHandle::IsSecurityTransparent)
    QCFuncElement("_IsVisible", RuntimeTypeHandle::IsVisible)
    QCFuncElement("ConstructName", RuntimeTypeHandle::ConstructName)
    FCFuncElement("CanCastTo", RuntimeTypeHandle::CanCastTo)
    QCFuncElement("GetTypeByName", RuntimeTypeHandle::GetTypeByName)
    QCFuncElement("GetTypeByNameUsingCARules", RuntimeTypeHandle::GetTypeByNameUsingCARules)
    QCFuncElement("GetInstantiation", RuntimeTypeHandle::GetInstantiation)
    QCFuncElement("Instantiate", RuntimeTypeHandle::Instantiate)
    QCFuncElement("GetGenericTypeDefinition", RuntimeTypeHandle::GetGenericTypeDefinition)
    FCFuncElement("HasInstantiation", RuntimeTypeHandle::HasInstantiation)
    FCFuncElement("GetGenericVariableIndex", RuntimeTypeHandle::GetGenericVariableIndex)
    FCFuncElement("IsGenericVariable", RuntimeTypeHandle::IsGenericVariable)
    FCFuncElement("IsGenericTypeDefinition", RuntimeTypeHandle::IsGenericTypeDefinition)
    FCFuncElement("ContainsGenericVariables", RuntimeTypeHandle::ContainsGenericVariables)
    FCFuncElement("SatisfiesConstraints", RuntimeTypeHandle::SatisfiesConstraints)
    FCFuncElement("Allocate", RuntimeTypeHandle::Allocate) //for A.CI
    FCFuncElement("CompareCanonicalHandles", RuntimeTypeHandle::CompareCanonicalHandles)
    FCIntrinsic("GetValueInternal", RuntimeTypeHandle::GetValueInternal, CORINFO_INTRINSIC_RTH_GetValueInternal)
#ifndef FEATURE_CORECLR
    FCFuncElement("IsEquivalentTo", RuntimeTypeHandle::IsEquivalentTo)
    FCFuncElement("IsEquivalentType", RuntimeTypeHandle::IsEquivalentType)
#endif // FEATURE_CORECLR
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

FCFuncStart(gRuntimeFieldInfoFuncs)
    FCFuncElement("PerformVisibilityCheckOnField",  ReflectionInvocation::PerformVisibilityCheckOnField)
FCFuncEnd()

FCFuncStart(gSignatureNative)
    FCFuncElement("GetSignature", SignatureNative::GetSignature)
    FCFuncElement("GetCustomModifiers", SignatureNative::GetCustomModifiers)
    FCFuncElement("CompareSig", SignatureNative::CompareSig)
FCFuncEnd()

FCFuncStart(gRuntimeMethodHandle)
    QCFuncElement("ConstructInstantiation", RuntimeMethodHandle::ConstructInstantiation)
    FCFuncElement("_GetCurrentMethod", RuntimeMethodHandle::GetCurrentMethod)
#ifdef FEATURE_SERIALIZATION
    FCFuncElement("SerializationInvoke", RuntimeMethodHandle::SerializationInvoke)
#endif // FEATURE_SERIALIZATION
    FCFuncElement("InvokeMethod", RuntimeMethodHandle::InvokeMethod)
    QCFuncElement("GetFunctionPointer", RuntimeMethodHandle::GetFunctionPointer)
    FCFuncElement("GetImplAttributes", RuntimeMethodHandle::GetImplAttributes)
    FCFuncElement("GetAttributes", RuntimeMethodHandle::GetAttributes)
    FCFuncElement("GetDeclaringType", RuntimeMethodHandle::GetDeclaringType)
    FCFuncElement("GetSlot", RuntimeMethodHandle::GetSlot)
    FCFuncElement("GetMethodDef", RuntimeMethodHandle::GetMethodDef)
    FCFuncElement("GetName", RuntimeMethodHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeMethodHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeMethodHandle::MatchesNameHash)
    QCFuncElement("GetMethodInstantiation", RuntimeMethodHandle::GetMethodInstantiation)
    FCFuncElement("HasMethodInstantiation", RuntimeMethodHandle::HasMethodInstantiation)
    FCFuncElement("IsGenericMethodDefinition", RuntimeMethodHandle::IsGenericMethodDefinition)
    FCFuncElement("IsTypicalMethodDefinition", RuntimeMethodHandle::IsTypicalMethodDefinition)
    QCFuncElement("GetTypicalMethodDefinition", RuntimeMethodHandle::GetTypicalMethodDefinition)
    QCFuncElement("StripMethodInstantiation", RuntimeMethodHandle::StripMethodInstantiation)
    FCFuncElement("GetStubIfNeeded", RuntimeMethodHandle::GetStubIfNeeded)
    FCFuncElement("GetMethodFromCanonical", RuntimeMethodHandle::GetMethodFromCanonical)
    FCFuncElement("IsDynamicMethod", RuntimeMethodHandle::IsDynamicMethod)
    FCFuncElement("GetMethodBody", RuntimeMethodHandle::GetMethodBody)    
#ifndef FEATURE_CORECLR
    FCFuncElement("_IsTokenSecurityTransparent", RuntimeMethodHandle::IsTokenSecurityTransparent)
    QCFuncElement("_IsSecurityCritical", RuntimeMethodHandle::IsSecurityCritical)
    QCFuncElement("_IsSecuritySafeCritical", RuntimeMethodHandle::IsSecuritySafeCritical)
#endif // FEATURE_CORECLR
    QCFuncElement("_IsSecurityTransparent", RuntimeMethodHandle::IsSecurityTransparent)
    FCFuncElement("CheckLinktimeDemands", RuntimeMethodHandle::CheckLinktimeDemands)    
    QCFuncElement("IsCAVisibleFromDecoratedType", RuntimeMethodHandle::IsCAVisibleFromDecoratedType)
    FCFuncElement("IsConstructor", RuntimeMethodHandle::IsConstructor)    
    QCFuncElement("Destroy", RuntimeMethodHandle::Destroy)
    FCFuncElement("GetResolver", RuntimeMethodHandle::GetResolver)
    FCFuncElement("GetLoaderAllocator", RuntimeMethodHandle::GetLoaderAllocator)
    FCFuncElement("GetSpecialSecurityFlags", ReflectionInvocation::GetSpecialSecurityFlags)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetCallerType", RuntimeMethodHandle::GetCallerType)
    FCFuncElement("PerformSecurityCheck", ReflectionInvocation::PerformSecurityCheck)
#endif // FEATURE_CORECLR
FCFuncEnd()

FCFuncStart(gCOMDefaultBinderFuncs)
    FCFuncElement("CanConvertPrimitive", ReflectionBinder::DBCanConvertPrimitive)
    FCFuncElement("CanConvertPrimitiveObjectToType",  ReflectionBinder::DBCanConvertObjectPrimitive)
FCFuncEnd()


FCFuncStart(gCOMFieldHandleNewFuncs)
    FCFuncElement("GetValue", RuntimeFieldHandle::GetValue)
    FCFuncElement("SetValue", RuntimeFieldHandle::SetValue)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetValueDirect", RuntimeFieldHandle::GetValueDirect)
#endif
#ifdef FEATURE_SERIALIZATION
    FCFuncElement("SetValueDirect", RuntimeFieldHandle::SetValueDirect)
#endif
    FCFuncElement("GetName", RuntimeFieldHandle::GetName)
    FCFuncElement("_GetUtf8Name", RuntimeFieldHandle::GetUtf8Name)
    FCFuncElement("MatchesNameHash", RuntimeFieldHandle::MatchesNameHash)
    FCFuncElement("GetAttributes", RuntimeFieldHandle::GetAttributes)
    FCFuncElement("GetApproxDeclaringType", RuntimeFieldHandle::GetApproxDeclaringType)
    FCFuncElement("GetToken", RuntimeFieldHandle::GetToken)
    FCFuncElement("GetStaticFieldForGenericType", RuntimeFieldHandle::GetStaticFieldForGenericType)
    QCFuncElement("IsSecurityCritical", RuntimeFieldHandle::IsSecurityCritical)
    QCFuncElement("IsSecuritySafeCritical", RuntimeFieldHandle::IsSecuritySafeCritical)
    QCFuncElement("IsSecurityTransparent", RuntimeFieldHandle::IsSecurityTransparent)
    FCFuncElement("AcquiresContextFromThis", RuntimeFieldHandle::AcquiresContextFromThis)
    QCFuncElement("CheckAttributeAccess", RuntimeFieldHandle::CheckAttributeAccess)
FCFuncEnd()


FCFuncStart(gCOMModuleFuncs)
    QCFuncElement("GetType", COMModule::GetType)
    QCFuncElement("GetScopeName", COMModule::GetScopeName)
    FCFuncElement("GetTypes", COMModule::GetTypes)
    QCFuncElement("GetFullyQualifiedName", COMModule::GetFullyQualifiedName)
    QCFuncElement("nIsTransientInternal", COMModule::IsTransient)
    FCFuncElement("IsResource", COMModule::IsResource)    
#if defined(FEATURE_X509) && defined(FEATURE_CAS_POLICY)
    QCFuncElement("GetSignerCertificate", COMModule::GetSignerCertificate)
#endif // defined(FEATURE_X509) && defined(FEATURE_CAS_POLICY)
FCFuncEnd()

FCFuncStart(gCOMModuleBuilderFuncs)
    FCFuncElement("nCreateISymWriterForDynamicModule", COMModule::nCreateISymWriterForDynamicModule)
    QCFuncElement("GetStringConstant", COMModule::GetStringConstant)
    QCFuncElement("GetTypeRef", COMModule::GetTypeRef)
    QCFuncElement("GetTokenFromTypeSpec", COMModule::GetTokenFromTypeSpec)
    QCFuncElement("GetMemberRef", COMModule::GetMemberRef)
    QCFuncElement("GetMemberRefOfMethodInfo", COMModule::GetMemberRefOfMethodInfo)
    QCFuncElement("GetMemberRefOfFieldInfo", COMModule::GetMemberRefOfFieldInfo)
    QCFuncElement("GetMemberRefFromSignature", COMModule::GetMemberRefFromSignature)
#ifndef FEATURE_CORECLR
    QCFuncElement("SetModuleName", COMModule::SetModuleName)
    QCFuncElement("PreSavePEFile", COMDynamicWrite::PreSavePEFile)
    QCFuncElement("SavePEFile", COMDynamicWrite::SavePEFile)
    QCFuncElement("AddResource", COMDynamicWrite::AddResource)
#endif
    QCFuncElement("GetArrayMethodToken", COMModule::GetArrayMethodToken)
    QCFuncElement("SetFieldRVAContent", COMModule::SetFieldRVAContent)
#ifndef FEATURE_CORECLR
    QCFuncElement("DefineNativeResourceFile", COMDynamicWrite::DefineNativeResourceFile)
    QCFuncElement("DefineNativeResourceBytes", COMDynamicWrite::DefineNativeResourceBytes)
#endif // FEATURE_CORECLR
FCFuncEnd()

FCFuncStart(gCOMModuleHandleFuncs)
    FCFuncElement("GetToken", ModuleHandle::GetToken)
    QCFuncElement("GetModuleType", ModuleHandle::GetModuleType)
    FCFuncElement("GetDynamicMethod", ModuleHandle::GetDynamicMethod)
    FCFuncElement("_GetMetadataImport", ModuleHandle::GetMetadataImport)
    QCFuncElement("ResolveType", ModuleHandle::ResolveType)
    QCFuncElement("ResolveMethod", ModuleHandle::ResolveMethod)
    QCFuncElement("_ContainsPropertyMatchingHash", ModuleHandle::ContainsPropertyMatchingHash)
    QCFuncElement("ResolveField", ModuleHandle::ResolveField)
#ifndef FEATURE_CORECLR    
    QCFuncElement("GetAssembly", ModuleHandle::GetAssembly)
#endif // FEATURE_CORECLR
    QCFuncElement("GetPEKind", ModuleHandle::GetPEKind)
    FCFuncElement("GetMDStreamVersion", ModuleHandle::GetMDStreamVersion)
FCFuncEnd()

FCFuncStart(gCustomAttributeEncodedArgument)
    FCFuncElement("ParseAttributeArguments", Attribute::ParseAttributeArguments)
FCFuncEnd()

FCFuncStart(gPseudoCustomAttribute)
    FCFuncElement("_GetSecurityAttributes", COMCustomAttribute::GetSecurityAttributes)
FCFuncEnd()

FCFuncStart(gCOMCustomAttributeFuncs)
    FCFuncElement("_ParseAttributeUsageAttribute", COMCustomAttribute::ParseAttributeUsageAttribute)
    FCFuncElement("_CreateCaObject", COMCustomAttribute::CreateCaObject)
    FCFuncElement("_GetPropertyOrFieldData",  COMCustomAttribute::GetPropertyOrFieldData)
FCFuncEnd()

FCFuncStart(gSecurityContextFrameFuncs)
    FCFuncElement("Push", COMCustomAttribute::PushSecurityContextFrame)
    FCFuncElement("Pop", COMCustomAttribute::PopSecurityContextFrame)
FCFuncEnd()

FCFuncStart(gCOMClassWriter)
    QCFuncElement("DefineGenericParam", COMDynamicWrite::DefineGenericParam)
    QCFuncElement("DefineType", COMDynamicWrite::DefineType)
    QCFuncElement("SetParentType", COMDynamicWrite::SetParentType)
    QCFuncElement("AddInterfaceImpl", COMDynamicWrite::AddInterfaceImpl)
    QCFuncElement("DefineMethod", COMDynamicWrite::DefineMethod)
    QCFuncElement("DefineMethodSpec", COMDynamicWrite::DefineMethodSpec)
    QCFuncElement("SetMethodIL", COMDynamicWrite::SetMethodIL)
    QCFuncElement("TermCreateClass", COMDynamicWrite::TermCreateClass)
    QCFuncElement("DefineField", COMDynamicWrite::DefineField)
    QCFuncElement("DefineProperty", COMDynamicWrite::DefineProperty)
    QCFuncElement("DefineEvent", COMDynamicWrite::DefineEvent)
    QCFuncElement("DefineMethodSemantics", COMDynamicWrite::DefineMethodSemantics)
    QCFuncElement("SetMethodImpl", COMDynamicWrite::SetMethodImpl)
    QCFuncElement("DefineMethodImpl", COMDynamicWrite::DefineMethodImpl)
    QCFuncElement("GetTokenFromSig",  COMDynamicWrite::GetTokenFromSig)
    QCFuncElement("SetFieldLayoutOffset", COMDynamicWrite::SetFieldLayoutOffset)
    QCFuncElement("SetClassLayout", COMDynamicWrite::SetClassLayout)
    QCFuncElement("SetParamInfo", COMDynamicWrite::SetParamInfo)
#ifndef FEATURE_CORECLR
    QCFuncElement("SetPInvokeData", COMDynamicWrite::SetPInvokeData)
    QCFuncElement("SetFieldMarshal", COMDynamicWrite::SetFieldMarshal)
#endif // FEATURE_CORECLR
    QCFuncElement("SetConstantValue", COMDynamicWrite::SetConstantValue)
    QCFuncElement("DefineCustomAttribute", COMDynamicWrite::DefineCustomAttribute)
#ifndef FEATURE_CORECLR
    QCFuncElement("AddDeclarativeSecurity", COMDynamicWrite::AddDeclarativeSecurity)
#endif // FEATURE_CORECLR
FCFuncEnd()

#ifdef FEATURE_METHOD_RENTAL
FCFuncStart(gCOMMethodRental)
    QCFuncElement("SwapMethodBody", COMMethodRental::SwapMethodBody)
FCFuncEnd()
#endif // FEATURE_METHOD_RENTAL

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gFrameSecurityDescriptorFuncs)
    FCFuncElement("IncrementOverridesCount", SecurityPolicy::IncrementOverridesCount)
    FCFuncElement("DecrementOverridesCount", SecurityPolicy::DecrementOverridesCount)
    FCFuncElement("IncrementAssertCount", SecurityPolicy::IncrementAssertCount)
    FCFuncElement("DecrementAssertCount", SecurityPolicy::DecrementAssertCount)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gCodeAccessSecurityEngineFuncs)
    FCFuncElement("SpecialDemand", SecurityStackWalk::FcallSpecialDemand)
    FCFuncElement("Check", SecurityStackWalk::Check)
    FCFuncElement("CheckNReturnSO", SecurityStackWalk::CheckNReturnSO)
    FCFuncElement("GetZoneAndOriginInternal", SecurityStackWalk::GetZoneAndOrigin)
#ifdef FEATURE_COMPRESSEDSTACK    
    FCFuncElement("QuickCheckForAllDemands", SecurityStackWalk::FCallQuickCheckForAllDemands)
    FCFuncElement("AllDomainsHomogeneousWithNoStackModifiers", SecurityStackWalk::FCallAllDomainsHomogeneousWithNoStackModifiers)    
#endif
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

FCFuncStart(gCompatibilitySwitchFuncs)
    FCFuncElement("GetValueInternalCall", CompatibilitySwitch::GetValue)
#ifndef FEATURE_CORECLR
    FCFuncElement("IsEnabledInternalCall", CompatibilitySwitch::IsEnabled)
    FCFuncElement("GetAppContextOverridesInternalCall", CompatibilitySwitch::GetAppContextOverrides)
#endif
FCFuncEnd()


#ifdef FEATURE_COMPRESSEDSTACK    
FCFuncStart(gCompressedStackFuncs)
    FCFuncElement("DestroyDelayedCompressedStack", SecurityStackWalk::FcallDestroyDelayedCompressedStack)
    FCFuncElement("DestroyDCSList", NewCompressedStack::DestroyDCSList)
    FCFuncElement("GetDelayedCompressedStack", SecurityStackWalk::EcallGetDelayedCompressedStack)
    FCFuncElement("GetDCSCount", NewCompressedStack::FCallGetDCSCount)
    FCFuncElement("GetDomainCompressedStack", NewCompressedStack::GetDomainCompressedStack)
    FCFuncElement("GetHomogeneousPLS", NewCompressedStack::FCallGetHomogeneousPLS)
    FCFuncElement("IsImmediateCompletionCandidate", NewCompressedStack::FCallIsImmediateCompletionCandidate)    
FCFuncEnd()

FCFuncStart(gDomainCompressedStackFuncs)
    FCFuncElement("GetDescCount", DomainCompressedStack::GetDescCount)
    FCFuncElement("GetDomainPermissionSets", DomainCompressedStack::GetDomainPermissionSets)
    FCFuncElement("GetDescriptorInfo", DomainCompressedStack::GetDescriptorInfo)
    FCFuncElement("IgnoreDomain", DomainCompressedStack::IgnoreDomain)
FCFuncEnd()
#endif // #ifdef FEATURE_COMPRESSEDSTACK

FCFuncStart(gCOMSecurityManagerFuncs)
    QCFuncElement("IsSameType", SecurityPolicy::IsSameType)
    FCFuncElement("_SetThreadSecurity", SecurityPolicy::SetThreadSecurity)
#ifdef FEATURE_CAS_POLICY
    QCFuncElement("GetGrantedPermissions", SecurityPolicy::GetGrantedPermissions)
#endif
FCFuncEnd()

FCFuncStart(gCOMSecurityContextFuncs)
#ifdef FEATURE_IMPERSONATION
    FCFuncElement("GetImpersonationFlowMode", SecurityPolicy::GetImpersonationFlowMode)
#endif
#ifdef FEATURE_COMPRESSEDSTACK    
    FCFuncElement("IsDefaultThreadSecurityInfo", SecurityPolicy::IsDefaultThreadSecurityInfo)
#endif // #ifdef FEATURE_COMPRESSEDSTACK    
FCFuncEnd()

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gCOMSecurityZone)
    QCFuncElement("_CreateFromUrl", SecurityPolicy::CreateFromUrl)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

FCFuncStart(gCOMFileIOAccessFuncs)
    QCFuncElement("IsLocalDrive", SecurityPolicy::IsLocalDrive)
FCFuncEnd()

FCFuncStart(gCOMStringExpressionSetFuncs)
    QCFuncElement("GetLongPathName", SecurityPolicy::_GetLongPathName)
FCFuncEnd()


FCFuncStart(gCOMUrlStringFuncs)
    QCFuncElement("GetDeviceName", SecurityPolicy::GetDeviceName)
FCFuncEnd()

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gCOMSecurityRuntimeFuncs)
    FCFuncElement("GetSecurityObjectForFrame", SecurityRuntime::GetSecurityObjectForFrame)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_X509

FCFuncStart(gX509CertificateFuncs)
#ifndef FEATURE_CORECLR
FCFuncElement("_AddCertificateToStore", COMX509Store::AddCertificate)
#endif // !FEATURE_CORECLR
    FCFuncElement("_DuplicateCertContext", COMX509Certificate::DuplicateCertContext)
#ifndef FEATURE_CORECLR
    FCFuncElement("_ExportCertificatesToBlob", COMX509Store::ExportCertificatesToBlob)
#endif // !FEATURE_CORECLR
    FCFuncElement("_GetCertRawData", COMX509Certificate::GetCertRawData)
    FCFuncElement("_GetDateNotAfter", COMX509Certificate::GetDateNotAfter)
    FCFuncElement("_GetDateNotBefore", COMX509Certificate::GetDateNotBefore)
    FCFuncElement("_GetIssuerName", COMX509Certificate::GetIssuerName)
    FCFuncElement("_GetPublicKeyOid", COMX509Certificate::GetPublicKeyOid)
    FCFuncElement("_GetPublicKeyParameters", COMX509Certificate::GetPublicKeyParameters)
    FCFuncElement("_GetPublicKeyValue", COMX509Certificate::GetPublicKeyValue)
    FCFuncElement("_GetSerialNumber", COMX509Certificate::GetSerialNumber)
    FCFuncElement("_GetSubjectInfo", COMX509Certificate::GetSubjectInfo)
    FCFuncElement("_GetThumbprint", COMX509Certificate::GetThumbprint)
    FCFuncElement("_LoadCertFromBlob", COMX509Certificate::LoadCertFromBlob)
    FCFuncElement("_LoadCertFromFile", COMX509Certificate::LoadCertFromFile)
#ifndef FEATURE_CORECLR
    FCFuncElement("_OpenX509Store", COMX509Store::OpenX509Store)
#endif // !FEATURE_CORECLR
    FCFuncElement("_QueryCertBlobType", COMX509Certificate::QueryCertBlobType)
    FCFuncElement("_QueryCertFileType", COMX509Certificate::QueryCertFileType)
FCFuncEnd()

FCFuncStart(gX509SafeCertContextHandleFuncs)
    FCFuncElement("_FreePCertContext", COMX509Certificate::FreePCertContext)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gX509SafeCertStoreHandleFuncs)
    FCFuncElement("_FreeCertStoreContext", COMX509Store::FreeCertStoreContext)
FCFuncEnd()
#endif

#endif // FEATURE_X509

FCFuncStart(gBCLDebugFuncs)
    FCFuncElement("GetRegistryLoggingValues", ManagedLoggingHelper::GetRegistryLoggingValues)
FCFuncEnd()

#ifdef FEATURE_CRYPTO
FCFuncStart(gCryptographyUtilsFuncs)
    FCFuncElement("_AcquireCSP", COMCryptography::_AcquireCSP)
    FCFuncElement("_CreateCSP", COMCryptography::_CreateCSP)
    FCFuncElement("_ExportKey", COMCryptography::_ExportKey)
    FCFuncElement("_GenerateKey", COMCryptography::_GenerateKey)
    FCFuncElement("_GetKeyParameter", COMCryptography::_GetKeyParameter)
    FCFuncElement("_GetUserKey", COMCryptography::_GetUserKey)
    FCFuncElement("_ImportKey", COMCryptography::_ImportKey)
    FCFuncElement("_ImportCspBlob", COMCryptography::_ImportCspBlob)
    FCFuncElement("_OpenCSP", COMCryptography::_OpenCSP)
    QCFuncElement("ExportCspBlob", COMCryptography::ExportCspBlob)
    QCFuncElement("GetPersistKeyInCsp", COMCryptography::GetPersistKeyInCsp)
    QCFuncElement("SetPersistKeyInCsp", COMCryptography::SetPersistKeyInCsp)
    QCFuncElement("SignValue", COMCryptography::SignValue)
    QCFuncElement("VerifySign", COMCryptography::VerifySign)
    FCFuncElement("_GetProviderParameter", COMCryptography::_GetProviderParameter)
    FCFuncElement("_ProduceLegacyHmacValues", COMCryptography::_ProduceLegacyHMACValues)
    QCFuncElement("CreateHash", COMCryptography::CreateHash)
    QCFuncElement("EndHash", COMCryptography::EndHash)
    QCFuncElement("HashData", COMCryptography::HashData)
    QCFuncElement("SetProviderParameter", COMCryptography::SetProviderParameter)
#ifndef FEATURE_CORECLR
    FCFuncElement("_DecryptData", COMCryptography::_DecryptData)
    FCFuncElement("_EncryptData", COMCryptography::_EncryptData)
    FCFuncElement("_GetEnforceFipsPolicySetting", COMCryptography::_GetEnforceFipsPolicySetting)
    FCFuncElement("_ImportBulkKey", COMCryptography::_ImportBulkKey)
    FCFuncElement("_GetKeySetSecurityInfo", COMCryptography::_GetKeySetSecurityInfo)
    QCFuncElement("SearchForAlgorithm", COMCryptography::SearchForAlgorithm)
    QCFuncElement("SetKeyParamDw", COMCryptography::SetKeyParamDw)
    QCFuncElement("SetKeyParamRgb", COMCryptography::SetKeyParamRgb)
    QCFuncElement("SetKeySetSecurityInfo", COMCryptography::SetKeySetSecurityInfo)
#endif //FEATURE_CORECLR
FCFuncEnd()

FCFuncStart(gSafeHashHandleFuncs)
    QCFuncElement("FreeHash", COMCryptography::FreeHash)
FCFuncEnd()

FCFuncStart(gSafeKeyHandleFuncs)
    QCFuncElement("FreeKey", COMCryptography::FreeKey)
FCFuncEnd()

FCFuncStart(gSafeProvHandleFuncs)
    QCFuncElement("FreeCsp", COMCryptography::FreeCsp)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gPasswordDeriveBytesFuncs)
    QCFuncElement("DeriveKey", COMCryptography::DeriveKey)
FCFuncEnd()
#endif

#if defined(FEATURE_CRYPTO)
FCFuncStart(gRfc2898DeriveBytesFuncs)
    QCFuncElement("DeriveKey", COMCryptography::DeriveKey)
FCFuncEnd()
#endif

#ifndef FEATURE_CORECLR
FCFuncStart(gRNGCryptoServiceProviderFuncs)
    QCFuncElement("GetBytes", COMCryptography::GetBytes)
    QCFuncElement("GetNonZeroBytes", COMCryptography::GetNonZeroBytes)
FCFuncEnd()
#endif //FEATURE_CORECLR

FCFuncStart(gRSACryptoServiceProviderFuncs)
    QCFuncElement("DecryptKey", COMCryptography::DecryptKey)
    QCFuncElement("EncryptKey", COMCryptography::EncryptKey)
FCFuncEnd()
#endif // FEATURE_CRYPTO

FCFuncStart(gAppDomainManagerFuncs)
    QCFuncElement("GetEntryAssembly", AssemblyNative::GetEntryAssembly)
#ifdef FEATURE_APPDOMAINMANAGER_INITOPTIONS
    FCFuncElement("HasHost", AppDomainNative::HasHost)
    QCFuncElement("RegisterWithHost", AppDomainNative::RegisterWithHost)
#endif    
FCFuncEnd()

#ifdef FEATURE_FUSION
FCFuncStart(gAppDomainSetupFuncs)
    FCFuncElement("UpdateContextProperty", AppDomainNative::UpdateContextProperty)
FCFuncEnd()
#endif // FEATURE_FUSION

#ifndef FEATURE_CORECLR
FCFuncStart(gWindowsRuntimeContextFuncs)
    QCFuncElement("CreateDesignerContext", AppDomainNative::CreateDesignerContext)
    QCFuncElement("SetCurrentContext", AppDomainNative::SetCurrentDesignerContext)
FCFuncEnd()
#endif // FEATURE_CORECLR

FCFuncStart(gAppDomainFuncs)
#ifdef FEATURE_REMOTING 
    FCFuncElement("GetDefaultDomain", AppDomainNative::GetDefaultDomain)
#endif    
#ifdef FEATURE_FUSION
    FCFuncElement("GetFusionContext", AppDomainNative::GetFusionContext)
#endif // FEATURE_FUSION
    FCFuncElement("IsStringInterned", AppDomainNative::IsStringInterned)
    FCFuncElement("IsUnloadingForcedFinalize", AppDomainNative::IsUnloadingForcedFinalize)
#ifdef FEATURE_REMOTING    
    FCFuncElement("nCreateDomain", AppDomainNative::CreateDomain)
    FCFuncElement("nCreateInstance", AppDomainNative::CreateInstance)
#endif    
#ifdef FEATURE_LOADER_OPTIMIZATION
    FCFuncElement("UpdateLoaderOptimization", AppDomainNative::UpdateLoaderOptimization)
#endif // FEATURE_LOADER_OPTIMIZATION
    QCFuncElement("DisableFusionUpdatesFromADManager", AppDomainNative::DisableFusionUpdatesFromADManager)

#ifdef FEATURE_APPX
    QCFuncElement("nGetAppXFlags", AppDomainNative::GetAppXFlags)
#endif
    QCFuncElement("GetAppDomainManagerType", AppDomainNative::GetAppDomainManagerType)
    QCFuncElement("SetAppDomainManagerType", AppDomainNative::SetAppDomainManagerType)
    FCFuncElement("nGetFriendlyName", AppDomainNative::GetFriendlyName)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetSecurityDescriptor", AppDomainNative::GetSecurityDescriptor)
    FCFuncElement("nIsDefaultAppDomainForEvidence", AppDomainNative::IsDefaultAppDomainForEvidence)
    FCFuncElement("nGetAssemblies", AppDomainNative::GetAssemblies)
#endif
#ifdef FEATURE_CAS_POLICY
    FCFuncElement("nSetHostSecurityManagerFlags", AppDomainNative::SetHostSecurityManagerFlags)
    QCFuncElement("SetLegacyCasPolicyEnabled", AppDomainNative::SetLegacyCasPolicyEnabled)
#endif // FEATURE_CAS_POLICY
#ifdef FEATURE_APTCA
    QCFuncElement("SetCanonicalConditionalAptcaList", AppDomainNative::SetCanonicalConditionalAptcaList)
#endif // FEATURE_ATPCA
    QCFuncElement("SetSecurityHomogeneousFlag", AppDomainNative::SetSecurityHomogeneousFlag)
    QCFuncElement("SetupDomainSecurity", AppDomainNative::SetupDomainSecurity)
    FCFuncElement("nSetupFriendlyName", AppDomainNative::SetupFriendlyName)
#if FEATURE_COMINTEROP
    FCFuncElement("nSetDisableInterfaceCache", AppDomainNative::SetDisableInterfaceCache)
#endif // FEATURE_COMINTEROP
#ifndef FEATURE_CORECLR
    FCFuncElement("_nExecuteAssembly", AppDomainNative::ExecuteAssembly)
#endif
#ifdef FEATURE_VERSIONING
    FCFuncElement("nCreateContext", AppDomainNative::CreateContext)
#endif // FEATURE_VERSIONING
#ifdef FEATURE_REMOTING
    FCFuncElement("nUnload", AppDomainNative::Unload)
#endif // FEATURE_REMOTING
    FCFuncElement("GetId", AppDomainNative::GetId)
    FCFuncElement("GetOrInternString", AppDomainNative::GetOrInternString)
    QCFuncElement("GetGrantSet", AppDomainNative::GetGrantSet)
#ifdef FEATURE_REMOTING
    FCFuncElement("GetDynamicDir", AppDomainNative::GetDynamicDir)
#ifdef FEATURE_CAS_POLICY
    QCFuncElement("GetIsLegacyCasPolicyEnabled", AppDomainNative::IsLegacyCasPolicyEnabled)
#endif // FEATURE_CAS_POLICY
    FCFuncElement("nChangeSecurityPolicy", AppDomainNative::ChangeSecurityPolicy)
    FCFuncElement("IsDomainIdValid", AppDomainNative::IsDomainIdValid)
    FCFuncElement("nApplyPolicy", AppDomainNative::nApplyPolicy)
#endif // FEATURE_REMOTING
#ifdef FEATURE_CORECLR    
    QCFuncElement("nSetupBindingPaths", AppDomainNative::SetupBindingPaths)
    QCFuncElement("nSetNativeDllSearchDirectories", AppDomainNative::SetNativeDllSearchDirectories)
#endif    
    FCFuncElement("IsFinalizingForUnload", AppDomainNative::IsFinalizingForUnload)
    FCFuncElement("PublishAnonymouslyHostedDynamicMethodsAssembly", AppDomainNative::PublishAnonymouslyHostedDynamicMethodsAssembly)
#ifdef FEATURE_APPDOMAIN_RESOURCE_MONITORING
    FCFuncElement("nEnableMonitoring", AppDomainNative::EnableMonitoring)
    FCFuncElement("nMonitoringIsEnabled", AppDomainNative::MonitoringIsEnabled)
    FCFuncElement("nGetTotalProcessorTime", AppDomainNative::GetTotalProcessorTime)
    FCFuncElement("nGetTotalAllocatedMemorySize", AppDomainNative::GetTotalAllocatedMemorySize)
    FCFuncElement("nGetLastSurvivedMemorySize", AppDomainNative::GetLastSurvivedMemorySize)
    FCFuncElement("nGetLastSurvivedProcessMemorySize", AppDomainNative::GetLastSurvivedProcessMemorySize)

#endif //FEATURE_APPDOMAIN_RESOURCE_MONITORING
FCFuncEnd()

#if defined(FEATURE_MULTICOREJIT) && !defined(FEATURE_CORECLR)
FCFuncStart(gProfileOptimizationFuncs)
    QCFuncElement("InternalSetProfileRoot", MultiCoreJITNative::InternalSetProfileRoot)
    QCFuncElement("InternalStartProfile",   MultiCoreJITNative::InternalStartProfile)
FCFuncEnd()
#endif  // defined(FEATURE_MULTICOREJIT) && !defined(FEATURE_CORECLR)

FCFuncStart(gUtf8String)
    FCFuncElement("EqualsCaseSensitive", Utf8String::EqualsCaseSensitive)
    QCFuncElement("EqualsCaseInsensitive", Utf8String::EqualsCaseInsensitive)
    QCFuncElement("HashCaseInsensitive", Utf8String::HashCaseInsensitive)
FCFuncEnd()

FCFuncStart(gTypeNameBuilder)
    QCFuncElement("CreateTypeNameBuilder", TypeNameBuilder::_CreateTypeNameBuilder)
    QCFuncElement("ReleaseTypeNameBuilder", TypeNameBuilder::_ReleaseTypeNameBuilder)
    QCFuncElement("OpenGenericArguments", TypeNameBuilder::_OpenGenericArguments)
    QCFuncElement("CloseGenericArguments", TypeNameBuilder::_CloseGenericArguments)
    QCFuncElement("OpenGenericArgument", TypeNameBuilder::_OpenGenericArgument)
    QCFuncElement("CloseGenericArgument", TypeNameBuilder::_CloseGenericArgument)
    QCFuncElement("AddName", TypeNameBuilder::_AddName)
    QCFuncElement("AddPointer", TypeNameBuilder::_AddPointer)
    QCFuncElement("AddByRef", TypeNameBuilder::_AddByRef)
    QCFuncElement("AddSzArray", TypeNameBuilder::_AddSzArray)
    QCFuncElement("AddArray", TypeNameBuilder::_AddArray)
    QCFuncElement("AddAssemblySpec", TypeNameBuilder::_AddAssemblySpec)
    QCFuncElement("ToString", TypeNameBuilder::_ToString)
    QCFuncElement("Clear", TypeNameBuilder::_Clear)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gSafeTypeNameParserHandle)
    QCFuncElement("_ReleaseTypeNameParser", TypeName::QReleaseTypeNameParser)
FCFuncEnd()

FCFuncStart(gTypeNameParser)
    QCFuncElement("_CreateTypeNameParser",  TypeName::QCreateTypeNameParser)
    QCFuncElement("_GetNames",              TypeName::QGetNames)
    QCFuncElement("_GetTypeArguments",      TypeName::QGetTypeArguments)
    QCFuncElement("_GetModifiers",          TypeName::QGetModifiers)
    QCFuncElement("_GetAssemblyName",       TypeName::QGetAssemblyName)
FCFuncEnd()
#endif //!FEATURE_CORECLR

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gPEFileFuncs)
    QCFuncElement("ReleaseSafePEFileHandle", AssemblyNative::ReleaseSafePEFileHandle)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

FCFuncStart(gManifestBasedResourceGrovelerFuncs)
    QCFuncElement("GetNeutralResourcesLanguageAttribute", AssemblyNative::GetNeutralResourcesLanguageAttribute)
FCFuncEnd()

FCFuncStart(gAssemblyFuncs)
    QCFuncElement("GetFullName", AssemblyNative::GetFullName)
    QCFuncElement("GetLocation", AssemblyNative::GetLocation)
    QCFuncElement("GetResource", AssemblyNative::GetResource)
    QCFuncElement("GetCodeBase", AssemblyNative::GetCodeBase)
    QCFuncElement("GetExecutingAssembly", AssemblyNative::GetExecutingAssembly)
    QCFuncElement("GetFlags", AssemblyNative::GetFlags)
    QCFuncElement("GetHashAlgorithm", AssemblyNative::GetHashAlgorithm)
    QCFuncElement("GetLocale", AssemblyNative::GetLocale)
    QCFuncElement("GetPublicKey", AssemblyNative::GetPublicKey)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetSecurityRuleSet", AssemblyNative::GetSecurityRuleSet)
#endif // !FEATURE_CORECLR
    QCFuncElement("GetSimpleName", AssemblyNative::GetSimpleName)
    QCFuncElement("GetVersion", AssemblyNative::GetVersion)
    FCFuncElement("FCallIsDynamic", AssemblyNative::IsDynamic)
    FCFuncElement("_nLoad", AssemblyNative::Load)
#ifndef FEATURE_CORECLR
    FCFuncElement("IsFrameworkAssembly", AssemblyNative::IsFrameworkAssembly)
    FCFuncElement("IsNewPortableAssembly", AssemblyNative::IsNewPortableAssembly)
#endif
    FCFuncElement("nLoadImage", AssemblyNative::LoadImage)
#ifndef FEATURE_CORECLR
    FCFuncElement("nLoadFile", AssemblyNative::LoadFile)
    QCFuncElement("LoadModule", AssemblyNative::LoadModule)
#endif  // FEATURE_CORECLR
    QCFuncElement("GetType", AssemblyNative::GetType)
    QCFuncElement("GetManifestResourceInfo", AssemblyNative::GetManifestResourceInfo)
#ifndef FEATURE_CORECLR
    QCFuncElement("UseRelativeBindForSatellites", AssemblyNative::UseRelativeBindForSatellites)
#endif
    QCFuncElement("GetModules", AssemblyNative::GetModules)
    QCFuncElement("GetModule", AssemblyNative::GetModule)
    FCFuncElement("GetReferencedAssemblies", AssemblyNative::GetReferencedAssemblies)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetForwardedTypes", AssemblyNative::GetForwardedTypes)
#endif  // FEATURE_CORECLR
    QCFuncElement("GetExportedTypes", AssemblyNative::GetExportedTypes)
    FCFuncElement("GetManifestResourceNames", AssemblyNative::GetManifestResourceNames)
    QCFuncElement("GetEntryPoint", AssemblyNative::GetEntryPoint)
    QCFuncElement("IsAllSecurityTransparent", AssemblyNative::IsAllSecurityTransparent)
    QCFuncElement("IsAllSecurityCritical", AssemblyNative::IsAllSecurityCritical)
#ifndef FEATURE_CORECLR
    QCFuncElement("IsAllSecuritySafeCritical", AssemblyNative::IsAllSecuritySafeCritical)
    QCFuncElement("IsAllPublicAreaSecuritySafeCritical", AssemblyNative::IsAllPublicAreaSecuritySafeCritical)
    QCFuncElement("GetGrantSet", AssemblyNative::GetGrantSet)
    FCFuncElement("IsGlobalAssemblyCache", AssemblyNative::IsGlobalAssemblyCache)
#endif // !FEATURE_CORECLR
#ifdef FEATURE_CAS_POLICY
    QCFuncElement("GetEvidence", SecurityPolicy::GetEvidence)
#endif // FEATURE_CAS_POLICY
    QCFuncElement("GetImageRuntimeVersion", AssemblyNative::GetImageRuntimeVersion)
    FCFuncElement("IsReflectionOnly", AssemblyNative::IsReflectionOnly)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetHostContext", AssemblyNative::GetHostContext)
#endif
#ifdef FEATURE_CAS_POLICY
    QCFuncElement("GetIsStrongNameVerified", AssemblyNative::IsStrongNameVerified)
    QCFuncElement("GetRawBytes", AssemblyNative::GetRawBytes)
#endif // FEATURE_CAS_POLICY
    FCFuncElement("GetManifestModule", AssemblyHandle::GetManifestModule)
    FCFuncElement("GetToken", AssemblyHandle::GetToken)
#ifdef FEATURE_APTCA
    FCFuncElement("AptcaCheck", AssemblyHandle::AptcaCheck)
#endif // FEATURE_APTCA
#ifdef FEATURE_APPX
    QCFuncElement("nIsDesignerBindingContext", AssemblyNative::IsDesignerBindingContext)
#endif

FCFuncEnd()

#ifdef FEATURE_CORECLR
FCFuncStart(gAssemblyExtensionsFuncs)
    QCFuncElement("InternalTryGetRawMetadata", AssemblyNative::InternalTryGetRawMetadata)
FCFuncEnd()
#endif

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
FCFuncStart(gAssemblyLoadContextFuncs)
    QCFuncElement("InitializeAssemblyLoadContext", AssemblyNative::InitializeAssemblyLoadContext)
    QCFuncElement("LoadFromPath", AssemblyNative::LoadFromPath)
    QCFuncElement("InternalLoadUnmanagedDllFromPath", AssemblyNative::InternalLoadUnmanagedDllFromPath)
    QCFuncElement("OverrideDefaultAssemblyLoadContextForCurrentDomain", AssemblyNative::OverrideDefaultAssemblyLoadContextForCurrentDomain)
    QCFuncElement("CanUseAppPathAssemblyLoadContextInCurrentDomain", AssemblyNative::CanUseAppPathAssemblyLoadContextInCurrentDomain)
    QCFuncElement("LoadFromStream", AssemblyNative::LoadFromStream)
    FCFuncElement("nGetFileInformation", AssemblyNameNative::GetFileInformation)
    QCFuncElement("GetLoadContextForAssembly", AssemblyNative::GetLoadContextForAssembly)
#if defined(FEATURE_MULTICOREJIT)
    QCFuncElement("InternalSetProfileRoot", MultiCoreJITNative::InternalSetProfileRoot)
    QCFuncElement("InternalStartProfile",   MultiCoreJITNative::InternalStartProfile)
#endif // defined(FEATURE_MULTICOREJIT)
FCFuncEnd()
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

FCFuncStart(gAssemblyNameFuncs)
    FCFuncElement("nInit", AssemblyNameNative::Init)
    FCFuncElement("nToString", AssemblyNameNative::ToString)
    FCFuncElement("nGetPublicKeyToken", AssemblyNameNative::GetPublicKeyToken)
#ifndef FEATURE_CORECLR
    FCFuncElement("EscapeCodeBase", AssemblyNameNative::EscapeCodeBase)
    FCFuncElement("ReferenceMatchesDefinitionInternal", AssemblyNameNative::ReferenceMatchesDefinition)
    FCFuncElement("nGetFileInformation", AssemblyNameNative::GetFileInformation)
#endif // !FEATURE_CORECLR 
FCFuncEnd()

FCFuncStart(gLoaderAllocatorFuncs)
    QCFuncElement("Destroy", LoaderAllocator::Destroy)
FCFuncEnd()

FCFuncStart(gAssemblyBuilderFuncs)
    FCFuncElement("nCreateDynamicAssembly", AppDomainNative::CreateDynamicAssembly)
    FCFuncElement("GetInMemoryAssemblyModule", AssemblyNative::GetInMemoryAssemblyModule)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetOnDiskAssemblyModule", AssemblyNative::GetOnDiskAssemblyModule)
#ifdef FEATURE_MULTIMODULE_ASSEMBLIES    
    QCFuncElement("DefineDynamicModule", COMModule::DefineDynamicModule)
#endif // FEATURE_MULTIMODULE_ASSEMBLIES
    QCFuncElement("PrepareForSavingManifestToDisk", AssemblyNative::PrepareForSavingManifestToDisk)
    QCFuncElement("SaveManifestToDisk", AssemblyNative::SaveManifestToDisk)
    QCFuncElement("AddFile", AssemblyNative::AddFile)
    QCFuncElement("SetFileHashValue", AssemblyNative::SetFileHashValue)
    QCFuncElement("AddStandAloneResource", AssemblyNative::AddStandAloneResource)
    QCFuncElement("AddExportedTypeOnDisk", AssemblyNative::AddExportedTypeOnDisk)
    QCFuncElement("AddExportedTypeInMemory", AssemblyNative::AddExportedTypeInMemory)
    QCFuncElement("AddDeclarativeSecurity", AssemblyNative::AddDeclarativeSecurity)
    QCFuncElement("CreateVersionInfoResource", AssemblyNative::CreateVersionInfoResource)
#endif // !FEATURE_CORECLR
FCFuncEnd()

#ifdef MDA_SUPPORTED 
FCFuncStart(gMda)
    FCFuncElement("MemberInfoCacheCreation", MdaManagedSupport::MemberInfoCacheCreation)
    FCFuncElement("DateTimeInvalidLocalFormat", MdaManagedSupport::DateTimeInvalidLocalFormat)
    FCFuncElement("IsStreamWriterBufferedDataLostEnabled", MdaManagedSupport::IsStreamWriterBufferedDataLostEnabled)
    FCFuncElement("IsStreamWriterBufferedDataLostCaptureAllocatedCallStack", MdaManagedSupport::IsStreamWriterBufferedDataLostCaptureAllocatedCallStack)
    FCFuncElement("ReportStreamWriterBufferedDataLost", MdaManagedSupport::ReportStreamWriterBufferedDataLost)
    FCFuncElement("IsInvalidGCHandleCookieProbeEnabled", MdaManagedSupport::IsInvalidGCHandleCookieProbeEnabled)
    FCFuncElement("FireInvalidGCHandleCookieProbe", MdaManagedSupport::FireInvalidGCHandleCookieProbe)
    FCFuncElement("ReportErrorSafeHandleRelease", MdaManagedSupport::ReportErrorSafeHandleRelease)
FCFuncEnd()
#endif // MDA_SUPPORTED

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
    FCIntrinsicSig("Abs", &gsig_SM_Dbl_RetDbl, COMDouble::Abs, CORINFO_INTRINSIC_Abs)
    FCIntrinsicSig("Abs", &gsig_SM_Flt_RetFlt, COMSingle::Abs, CORINFO_INTRINSIC_Abs)
    FCIntrinsic("Acos", COMDouble::Acos, CORINFO_INTRINSIC_Acos)
    FCIntrinsic("Asin", COMDouble::Asin, CORINFO_INTRINSIC_Asin)
    FCIntrinsic("Atan", COMDouble::Atan, CORINFO_INTRINSIC_Atan)
    FCIntrinsic("Atan2", COMDouble::Atan2, CORINFO_INTRINSIC_Atan2)
    FCIntrinsic("Ceiling", COMDouble::Ceil, CORINFO_INTRINSIC_Ceiling)
    FCIntrinsic("Cos", COMDouble::Cos, CORINFO_INTRINSIC_Cos)
    FCIntrinsic("Cosh", COMDouble::Cosh, CORINFO_INTRINSIC_Cosh)
    FCIntrinsic("Exp", COMDouble::Exp, CORINFO_INTRINSIC_Exp)
    FCIntrinsic("Floor", COMDouble::Floor, CORINFO_INTRINSIC_Floor)
    FCFuncElement("Log", COMDouble::Log)
    FCIntrinsic("Log10", COMDouble::Log10, CORINFO_INTRINSIC_Log10)
    FCIntrinsic("Pow", COMDouble::Pow, CORINFO_INTRINSIC_Pow)
    FCIntrinsic("Round", COMDouble::Round, CORINFO_INTRINSIC_Round)
    FCIntrinsic("Sin", COMDouble::Sin, CORINFO_INTRINSIC_Sin)
    FCIntrinsic("Sinh", COMDouble::Sinh, CORINFO_INTRINSIC_Sinh)
    FCFuncElement("SplitFractionDouble", COMDouble::ModF)
    FCIntrinsic("Sqrt", COMDouble::Sqrt, CORINFO_INTRINSIC_Sqrt)
    FCIntrinsic("Tan", COMDouble::Tan, CORINFO_INTRINSIC_Tan)
    FCIntrinsic("Tanh", COMDouble::Tanh, CORINFO_INTRINSIC_Tanh)
FCFuncEnd()

FCFuncStart(gThreadFuncs)
    FCDynamic("InternalGetCurrentThread", CORINFO_INTRINSIC_Illegal, ECall::InternalGetCurrentThread)
    FCFuncElement("StartInternal", ThreadNative::Start)
#ifndef FEATURE_CORECLR
    FCFuncElement("SuspendInternal", ThreadNative::Suspend)
    FCFuncElement("ResumeInternal", ThreadNative::Resume)
    FCFuncElement("InterruptInternal", ThreadNative::Interrupt)
#endif
    FCFuncElement("get_IsAlive", ThreadNative::IsAlive)
    FCFuncElement("GetThreadStateNative", ThreadNative::GetThreadState)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetPriorityNative", ThreadNative::GetPriority)
    FCFuncElement("SetPriorityNative", ThreadNative::SetPriority)
#endif
#ifdef FEATURE_LEAK_CULTURE_INFO
    FCFuncElement("nativeGetSafeCulture", ThreadNative::nativeGetSafeCulture)
#else
    QCFuncElement("nativeInitCultureAccessors", ThreadNative::nativeInitCultureAccessors)
#endif
    FCFuncElement("JoinInternal", ThreadNative::Join)
#undef Sleep
    FCFuncElement("SleepInternal", ThreadNative::Sleep)
#define Sleep(a) Dont_Use_Sleep(a)
    FCFuncElement("SetStart", ThreadNative::SetStart)
    FCFuncElement("SetBackgroundNative", ThreadNative::SetBackground)
    FCFuncElement("IsBackgroundNative", ThreadNative::IsBackground)
#ifdef FEATURE_REMOTING         
    FCFuncElement("GetContextInternal", ThreadNative::GetContextFromContextID)
#endif    
    FCFuncElement("GetDomainInternal", ThreadNative::GetDomain)
    FCFuncElement("GetFastDomainInternal", ThreadNative::FastGetDomain)
#ifdef FEATURE_COMPRESSEDSTACK    
    FCFuncElement("SetAppDomainStack", ThreadNative::SetAppDomainStack)
    FCFuncElement("RestoreAppDomainStack", ThreadNative::RestoreAppDomainStack)    
#endif // #ifdef FEATURE_COMPRESSEDSTACK    
#ifdef FEATURE_REMOTING         
    FCFuncElement("InternalCrossContextCallback", ThreadNative::InternalCrossContextCallback)
#endif    
    QCFuncElement("InformThreadNameChange", ThreadNative::InformThreadNameChange)
#ifndef FEATURE_CORECLR
    QCFuncElement("GetProcessDefaultStackSize", ThreadNative::GetProcessDefaultStackSize)
    FCFuncElement("BeginCriticalRegion", ThreadNative::BeginCriticalRegion)
    FCFuncElement("EndCriticalRegion", ThreadNative::EndCriticalRegion)
    FCFuncElement("BeginThreadAffinity", ThreadNative::BeginThreadAffinity)
    FCFuncElement("EndThreadAffinity", ThreadNative::EndThreadAffinity)
#endif // FEATURE_CORECLR
#ifdef FEATURE_LEGACYSURFACE
    FCFuncElement("AbortInternal", ThreadNative::Abort)
#endif // FEATURE_LEGACYSURFACE
#ifndef FEATURE_CORECLR
    FCFuncElement("ResetAbortNative", ThreadNative::ResetAbort)
#endif // FEATURE_CORECLR
    FCFuncElement("get_IsThreadPoolThread", ThreadNative::IsThreadpoolThread)
    FCFuncElement("SpinWaitInternal", ThreadNative::SpinWait)
    QCFuncElement("YieldInternal", ThreadNative::YieldThread)
    FCIntrinsic("GetCurrentThreadNative", ThreadNative::GetCurrentThread, CORINFO_INTRINSIC_GetCurrentManagedThread)
    FCIntrinsic("get_ManagedThreadId", ThreadNative::GetManagedThreadId, CORINFO_INTRINSIC_GetManagedThreadId)
    FCFuncElement("InternalFinalize", ThreadNative::Finalize)
#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
    FCFuncElement("DisableComObjectEagerCleanup", ThreadNative::DisableComObjectEagerCleanup)
#endif // defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
#ifdef FEATURE_LEAK_CULTURE_INFO
    FCFuncElement("nativeSetThreadUILocale", ThreadNative::SetThreadUILocale)
#endif
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
#ifndef FEATURE_CORECLR
    FCFuncElement("SetApartmentStateNative", ThreadNative::SetApartmentState)
    FCFuncElement("GetApartmentStateNative", ThreadNative::GetApartmentState)
#endif // FEATURE_CORECLR
    FCFuncElement("StartupSetApartmentStateInternal", ThreadNative::StartupSetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    FCIntrinsic("MemoryBarrier", ThreadNative::FCMemoryBarrier, CORINFO_INTRINSIC_MemoryBarrier)
#ifndef FEATURE_CORECLR    // coreclr does not support abort reason 
    FCFuncElement("SetAbortReason", ThreadNative::SetAbortReason)
    FCFuncElement("GetAbortReason", ThreadNative::GetAbortReason)
    FCFuncElement("ClearAbortReason", ThreadNative::ClearAbortReason)
#endif    
FCFuncEnd()

FCFuncStart(gThreadPoolFuncs)
#ifndef FEATURE_CORECLR
    FCFuncElement("PostQueuedCompletionStatus", ThreadPoolNative::CorPostQueuedCompletionStatus)
    FCFuncElement("GetAvailableThreadsNative", ThreadPoolNative::CorGetAvailableThreads)
#endif // FEATURE_CORECLR
    FCFuncElement("SetMinThreadsNative", ThreadPoolNative::CorSetMinThreads)
    FCFuncElement("GetMinThreadsNative", ThreadPoolNative::CorGetMinThreads)
    FCFuncElement("RegisterWaitForSingleObjectNative", ThreadPoolNative::CorRegisterWaitForSingleObject)
    FCFuncElement("BindIOCompletionCallbackNative", ThreadPoolNative::CorBindIoCompletionCallback)
    FCFuncElement("SetMaxThreadsNative", ThreadPoolNative::CorSetMaxThreads)
    FCFuncElement("GetMaxThreadsNative", ThreadPoolNative::CorGetMaxThreads)
    FCFuncElement("NotifyWorkItemComplete", ThreadPoolNative::NotifyRequestComplete)
    FCFuncElement("NotifyWorkItemProgressNative", ThreadPoolNative::NotifyRequestProgress)
    FCFuncElement("IsThreadPoolHosted", ThreadPoolNative::IsThreadPoolHosted)   
    QCFuncElement("InitializeVMTp", ThreadPoolNative::InitializeVMTp)
    FCFuncElement("ReportThreadStatus", ThreadPoolNative::ReportThreadStatus)   
    QCFuncElement("RequestWorkerThread", ThreadPoolNative::RequestWorkerThread)
FCFuncEnd()

FCFuncStart(gTimerFuncs)
    QCFuncElement("CreateAppDomainTimer", AppDomainTimerNative::CreateAppDomainTimer)
    QCFuncElement("ChangeAppDomainTimer", AppDomainTimerNative::ChangeAppDomainTimer)
    QCFuncElement("DeleteAppDomainTimer", AppDomainTimerNative::DeleteAppDomainTimer)
FCFuncEnd()


FCFuncStart(gRegisteredWaitHandleFuncs)
    FCFuncElement("UnregisterWaitNative", ThreadPoolNative::CorUnregisterWait)
    FCFuncElement("WaitHandleCleanupNative", ThreadPoolNative::CorWaitHandleCleanupNative)
FCFuncEnd()

FCFuncStart(gWaitHandleFuncs)
    FCFuncElement("WaitOneNative", WaitHandleNative::CorWaitOneNative)
    FCFuncElement("WaitMultiple", WaitHandleNative::CorWaitMultipleNative)
#ifndef FEATURE_PAL
    FCFuncElement("SignalAndWaitOne", WaitHandleNative::CorSignalAndWaitOneNative)
#endif // !FEATURE_PAL
FCFuncEnd()

FCFuncStart(gNumberFuncs)
    FCFuncElement("FormatDecimal", COMNumber::FormatDecimal)
    FCFuncElement("FormatDouble", COMNumber::FormatDouble)
    FCFuncElement("FormatInt32", COMNumber::FormatInt32)
    FCFuncElement("FormatUInt32", COMNumber::FormatUInt32)
    FCFuncElement("FormatInt64", COMNumber::FormatInt64)
    FCFuncElement("FormatUInt64", COMNumber::FormatUInt64)
    FCFuncElement("FormatSingle", COMNumber::FormatSingle)
#if !defined(FEATURE_CORECLR)
    FCFuncElement("FormatNumberBuffer", COMNumber::FormatNumberBuffer)
#endif // !FEATURE_CORECLR
    FCFuncElement("NumberBufferToDecimal", COMNumber::NumberBufferToDecimal)
    FCFuncElement("NumberBufferToDouble", COMNumber::NumberBufferToDouble)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP
FCFuncStart(gVariantFuncs)
    FCFuncElement("SetFieldsObject", COMVariant::SetFieldsObject)
    FCFuncElement("SetFieldsR4", COMVariant::SetFieldsR4)
    FCFuncElement("SetFieldsR8", COMVariant::SetFieldsR8)
    FCFuncElement("GetR4FromVar", COMVariant::GetR4FromVar)
    FCFuncElement("GetR8FromVar", COMVariant::GetR8FromVar)
    FCFuncElement("BoxEnum", COMVariant::BoxEnum)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
FCFuncStart(gOAVariantFuncs)
    FCFuncElement("ChangeTypeEx", COMOAVariant::ChangeTypeEx)
FCFuncEnd()
#endif // FEATURE_COMINTEROP

FCFuncStart(gDecimalFuncs)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_Flt_RetVoid, COMDecimal::InitSingle)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_Dbl_RetVoid, COMDecimal::InitDouble)
    FCFuncElement("FCallAddSub", COMDecimal::DoAddSubThrow)
    FCFuncElement("FCallMultiply", COMDecimal::DoMultiplyThrow)
    FCFuncElement("FCallDivide", COMDecimal::DoDivideThrow)
#ifndef FEATURE_CORECLR
    FCFuncElement("FCallAddSubOverflowed", COMDecimal::DoAddSub)
    FCFuncElement("FCallMultiplyOverflowed", COMDecimal::DoMultiply)
    FCFuncElement("FCallDivideOverflowed", COMDecimal::DoDivide)
#endif // FEATURE_CORECLR
    FCFuncElement("FCallCompare", COMDecimal::DoCompare)
    FCFuncElement("FCallFloor", COMDecimal::DoFloor)
    FCFuncElement("GetHashCode", COMDecimal::GetHashCode)
    FCFuncElement("FCallRound", COMDecimal::DoRound)
    FCFuncElement("FCallToCurrency", COMDecimal::DoToCurrency)
    FCFuncElement("FCallToInt32", COMDecimal::ToInt32)    
    FCFuncElement("ToDouble", COMDecimal::ToDouble)
    FCFuncElement("ToSingle", COMDecimal::ToSingle)
    FCFuncElement("FCallTruncate", COMDecimal::DoTruncate)
FCFuncEnd()

FCFuncStart(gCurrencyFuncs)
    FCFuncElement("FCallToDecimal", COMCurrency::DoToDecimal)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gCLRConfigFuncs)
    FCFuncElement("CheckLegacyManagedDeflateStream", SystemNative::CheckLegacyManagedDeflateStream) 
    FCFuncElement("CheckThrowUnobservedTaskExceptions", SystemNative::CheckThrowUnobservedTaskExceptions) 
FCFuncEnd()
#endif // ifndef FEATURE_CORECLR

#if !defined(FEATURE_COREFX_GLOBALIZATION)
FCFuncStart(gCompareInfoFuncs)
    QCFuncElement("InternalGetGlobalizedHashCode", COMNlsInfo::InternalGetGlobalizedHashCode)
    QCFuncElement("InternalCompareString", COMNlsInfo::InternalCompareString)
    QCFuncElement("InternalFindNLSStringEx", COMNlsInfo::InternalFindNLSStringEx)
    QCFuncElement("NativeInternalInitSortHandle", COMNlsInfo::InternalInitSortHandle)
    QCFuncElement("InternalIsSortable", COMNlsInfo::InternalIsSortable)
    QCFuncElement("InternalGetSortKey", COMNlsInfo::InternalGetSortKey)
#ifndef FEATURE_CORECLR
    QCFuncElement("InternalGetSortVersion", COMNlsInfo::InternalGetSortVersion)
    QCFuncElement("InternalGetNlsVersionEx", COMNlsInfo::InternalGetNlsVersionEx)
#endif
FCFuncEnd()

FCFuncStart(gEncodingTableFuncs)
    FCFuncElement("GetNumEncodingItems", COMNlsInfo::nativeGetNumEncodingItems)
    FCFuncElement("GetEncodingData", COMNlsInfo::nativeGetEncodingTableDataPointer)
    FCFuncElement("GetCodePageData", COMNlsInfo::nativeGetCodePageTableDataPointer)
#if FEATURE_CODEPAGES_FILE
    FCFuncElement("nativeCreateOpenFileMapping", COMNlsInfo::nativeCreateOpenFileMapping)
#endif
FCFuncEnd()

FCFuncStart(gCalendarDataFuncs)
    FCFuncElement("nativeGetTwoDigitYearMax", CalendarData::nativeGetTwoDigitYearMax)
    FCFuncElement("nativeGetCalendarData", CalendarData::nativeGetCalendarData)
    FCFuncElement("nativeGetCalendars", CalendarData::nativeGetCalendars)
FCFuncEnd()

FCFuncStart(gCultureDataFuncs)
    FCFuncElement("nativeInitCultureData", COMNlsInfo::nativeInitCultureData)
    FCFuncElement("nativeGetNumberFormatInfoValues", COMNlsInfo::nativeGetNumberFormatInfoValues)
    FCFuncElement("nativeEnumTimeFormats", CalendarData::nativeEnumTimeFormats)
    FCFuncElement("LCIDToLocaleName", COMNlsInfo::LCIDToLocaleName)
    FCFuncElement("LocaleNameToLCID", COMNlsInfo::LocaleNameToLCID)    

    QCFuncElement("nativeEnumCultureNames", COMNlsInfo::nativeEnumCultureNames)

FCFuncEnd()

FCFuncStart(gCultureInfoFuncs)
    QCFuncElement("InternalGetDefaultLocaleName", COMNlsInfo::InternalGetDefaultLocaleName)
    FCFuncElement("nativeGetLocaleInfoEx", COMNlsInfo::nativeGetLocaleInfoEx)
    FCFuncElement("nativeGetLocaleInfoExInt", COMNlsInfo::nativeGetLocaleInfoExInt)
    
#ifndef FEATURE_CORECLR
    FCFuncElement("nativeSetThreadLocale", COMNlsInfo::nativeSetThreadLocale)
#endif
    QCFuncElement("InternalGetUserDefaultUILanguage", COMNlsInfo::InternalGetUserDefaultUILanguage)
    QCFuncElement("InternalGetSystemDefaultUILanguage", COMNlsInfo::InternalGetSystemDefaultUILanguage)
// Added but disabled from desktop in .NET 4.0, stayed disabled in .NET 4.5
#ifdef FEATURE_CORECLR
    FCFuncElement("nativeGetResourceFallbackArray", COMNlsInfo::nativeGetResourceFallbackArray)
#endif
FCFuncEnd()

FCFuncStart(gTextInfoFuncs)
    FCFuncElement("InternalChangeCaseChar", COMNlsInfo::InternalChangeCaseChar)
    FCFuncElement("InternalChangeCaseString", COMNlsInfo::InternalChangeCaseString)
    FCFuncElement("InternalGetCaseInsHash", COMNlsInfo::InternalGetCaseInsHash)
    QCFuncElement("InternalCompareStringOrdinalIgnoreCase", COMNlsInfo::InternalCompareStringOrdinalIgnoreCase)
    QCFuncElement("InternalTryFindStringOrdinalIgnoreCase", COMNlsInfo::InternalTryFindStringOrdinalIgnoreCase)
FCFuncEnd()
#endif // !defined(FEATURE_COREFX_GLOBALIZATION)

#ifdef FEATURE_COREFX_GLOBALIZATION
FCFuncStart(gCompareInfoFuncs)
    QCFuncElement("InternalHashSortKey", CoreFxGlobalization::HashSortKey)
FCFuncEnd()
#endif

FCFuncStart(gArrayFuncs)
    FCFuncElement("get_Rank", ArrayNative::GetRank)
    FCFuncElement("GetLowerBound", ArrayNative::GetLowerBound)
    FCFuncElement("GetUpperBound", ArrayNative::GetUpperBound)
    FCIntrinsicSig("GetLength", &gsig_IM_Int_RetInt, ArrayNative::GetLength, CORINFO_INTRINSIC_Array_GetDimLength)
    FCFuncElement("get_Length", ArrayNative::GetLengthNoRank)
    FCFuncElement("get_LongLength", ArrayNative::GetLongLengthNoRank)
    FCFuncElement("GetDataPtrOffsetInternal", ArrayNative::GetDataPtrOffsetInternal)
    FCFuncElement("Initialize", ArrayNative::Initialize)
    FCFuncElement("Copy", ArrayNative::ArrayCopy)
    FCFuncElement("Clear", ArrayNative::ArrayClear)
    FCFuncElement("InternalCreate", ArrayNative::CreateInstance)
    FCFuncElement("InternalGetReference", ArrayNative::GetReference)
    FCFuncElement("InternalSetValue", ArrayNative::SetValue)
    FCFuncElement("TrySZIndexOf", ArrayHelper::TrySZIndexOf)
    FCFuncElement("TrySZLastIndexOf", ArrayHelper::TrySZLastIndexOf)
    FCFuncElement("TrySZBinarySearch", ArrayHelper::TrySZBinarySearch)
    FCFuncElement("TrySZSort", ArrayHelper::TrySZSort)
    FCFuncElement("TrySZReverse", ArrayHelper::TrySZReverse)
FCFuncEnd()

FCFuncStart(gBufferFuncs)
    FCFuncElement("BlockCopy", Buffer::BlockCopy)
    FCFuncElement("InternalBlockCopy", Buffer::InternalBlockCopy)
    FCFuncElement("_GetByte", Buffer::GetByte)
    FCFuncElement("_SetByte", Buffer::SetByte)
    FCFuncElement("IsPrimitiveTypeArray", Buffer::IsPrimitiveTypeArray)
    FCFuncElement("_ByteLength", Buffer::ByteLength)
#ifdef _TARGET_ARM_
    FCFuncElement("Memcpy", FCallMemcpy)
#endif
    QCFuncElement("__Memmove", Buffer::MemMove)
FCFuncEnd()

FCFuncStart(gGCInterfaceFuncs)
    FCFuncElement("GetGenerationWR", GCInterface::GetGenerationWR)
    FCFuncElement("_RegisterForFullGCNotification", GCInterface::RegisterForFullGCNotification)
    FCFuncElement("_CancelFullGCNotification", GCInterface::CancelFullGCNotification)
    FCFuncElement("_WaitForFullGCApproach", GCInterface::WaitForFullGCApproach)
    FCFuncElement("_WaitForFullGCComplete", GCInterface::WaitForFullGCComplete)
    FCFuncElement("_CollectionCount", GCInterface::CollectionCount)
    FCFuncElement("GetGCLatencyMode", GCInterface::GetGcLatencyMode)
    FCFuncElement("SetGCLatencyMode", GCInterface::SetGcLatencyMode)
    FCFuncElement("GetLOHCompactionMode", GCInterface::GetLOHCompactionMode)
    FCFuncElement("SetLOHCompactionMode", GCInterface::SetLOHCompactionMode)
    QCFuncElement("_StartNoGCRegion", GCInterface::StartNoGCRegion)
    QCFuncElement("_EndNoGCRegion", GCInterface::EndNoGCRegion)
    FCFuncElement("IsServerGC", SystemNative::IsServerGC)
    QCFuncElement("_AddMemoryPressure", GCInterface::_AddMemoryPressure)
    QCFuncElement("_RemoveMemoryPressure", GCInterface::_RemoveMemoryPressure)
    FCFuncElement("GetGeneration", GCInterface::GetGeneration)
    QCFuncElement("GetTotalMemory", GCInterface::GetTotalMemory)
    QCFuncElement("_Collect", GCInterface::Collect)
    FCFuncElement("GetMaxGeneration", GCInterface::GetMaxGeneration)
    QCFuncElement("_WaitForPendingFinalizers", GCInterface::WaitForPendingFinalizers)

    FCFuncElement("_SuppressFinalize", GCInterface::SuppressFinalize)
    FCFuncElement("_ReRegisterForFinalize", GCInterface::ReRegisterForFinalize)
    
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gMemoryFailPointFuncs)
    FCFuncElement("GetMemorySettings", COMMemoryFailPoint::GetMemorySettings)
FCFuncEnd()
#endif // FEATURE_CORECLR

FCFuncStart(gInteropMarshalFuncs)
    FCFuncElement("GetLastWin32Error", MarshalNative::GetLastWin32Error)
    FCFuncElement("SetLastWin32Error", MarshalNative::SetLastWin32Error)
    FCFuncElement("SizeOfHelper", MarshalNative::SizeOfClass)
    FCFuncElement("GetSystemMaxDBCSCharSize", MarshalNative::GetSystemMaxDBCSCharSize)
    FCFuncElement("PtrToStructureHelper", MarshalNative::PtrToStructureHelper)
    FCFuncElement("DestroyStructure", MarshalNative::DestroyStructure)
    FCFuncElement("UnsafeAddrOfPinnedArrayElement", MarshalNative::FCUnsafeAddrOfPinnedArrayElement)
    FCFuncElement("GetExceptionCode", ExceptionNative::GetExceptionCode)
#ifndef FEATURE_CORECLR
    QCFuncElement("InternalNumParamBytes", MarshalNative::NumParamBytes)
    FCFuncElement("GetExceptionPointers", ExceptionNative::GetExceptionPointers)
    QCFuncElement("GetHINSTANCE", COMModule::GetHINSTANCE)
    FCFuncElement("GetUnmanagedThunkForManagedMethodPtr", MarshalNative::GetUnmanagedThunkForManagedMethodPtr)
    FCFuncElement("GetManagedThunkForUnmanagedMethodPtr", MarshalNative::GetManagedThunkForUnmanagedMethodPtr)
    FCFuncElement("InternalGetThreadFromFiberCookie", MarshalNative::GetThreadFromFiberCookie)
#endif

    FCFuncElement("OffsetOfHelper", MarshalNative::OffsetOfHelper)
    FCFuncElement("SizeOfType", SafeBuffer::SizeOfType)
    FCFuncElement("AlignedSizeOfType", SafeBuffer::AlignedSizeOfType)

    QCFuncElement("InternalPrelink", MarshalNative::Prelink)
    FCFuncElement("CopyToNative", MarshalNative::CopyToNative)
    FCFuncElement("CopyToManaged", MarshalNative::CopyToManaged)
    FCFuncElement("StructureToPtr", MarshalNative::StructureToPtr)
    FCFuncElement("ThrowExceptionForHRInternal", MarshalNative::ThrowExceptionForHR)
    FCFuncElement("GetExceptionForHRInternal", MarshalNative::GetExceptionForHR)
    FCFuncElement("GetDelegateForFunctionPointerInternal", MarshalNative::GetDelegateForFunctionPointerInternal)
    FCFuncElement("GetFunctionPointerForDelegateInternal", MarshalNative::GetFunctionPointerForDelegateInternal)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("GetHRForException", MarshalNative::GetHRForException)
    FCFuncElement("GetHRForException_WinRT", MarshalNative::GetHRForException_WinRT)
    FCFuncElement("GetRawIUnknownForComObjectNoAddRef", MarshalNative::GetRawIUnknownForComObjectNoAddRef)
    FCFuncElement("IsComObject", MarshalNative::IsComObject)
    FCFuncElement("GetObjectForIUnknown", MarshalNative::GetObjectForIUnknown)
    FCFuncElement("GetUniqueObjectForIUnknown", MarshalNative::GetUniqueObjectForIUnknown)
    FCFuncElement("AddRef", MarshalNative::AddRef)
    FCFuncElement("GetNativeVariantForObject", MarshalNative::GetNativeVariantForObject)
    FCFuncElement("GetObjectForNativeVariant", MarshalNative::GetObjectForNativeVariant)
    FCFuncElement("InternalFinalReleaseComObject", MarshalNative::FinalReleaseComObject)
    FCFuncElement("QueryInterface", MarshalNative::QueryInterface)
    FCFuncElement("CreateAggregatedObject", MarshalNative::CreateAggregatedObject)
    FCFuncElement("AreComObjectsAvailableForCleanup", MarshalNative::AreComObjectsAvailableForCleanup)
    FCFuncElement("InternalCreateWrapperOfType", MarshalNative::InternalCreateWrapperOfType)
    FCFuncElement("GetObjectsForNativeVariants", MarshalNative::GetObjectsForNativeVariants)
    FCFuncElement("GetStartComSlot", MarshalNative::GetStartComSlot)

    FCFuncElement("InitializeManagedWinRTFactoryObject", MarshalNative::InitializeManagedWinRTFactoryObject)

    FCFuncElement("GetNativeActivationFactory", MarshalNative::GetNativeActivationFactory)
    FCFuncElement("GetIUnknownForObjectNative", MarshalNative::GetIUnknownForObjectNative)
    FCFuncElement("GetIDispatchForObjectNative", MarshalNative::GetIDispatchForObjectNative)
    FCFuncElement("GetComInterfaceForObjectNative", MarshalNative::GetComInterfaceForObjectNative)
    FCFuncElement("InternalReleaseComObject", MarshalNative::ReleaseComObject)
    FCFuncElement("Release", MarshalNative::Release)
    FCFuncElement("InitializeWrapperForWinRT", MarshalNative::InitializeWrapperForWinRT)
#ifndef FEATURE_CORECLR
    FCFuncElement("GetLoadedTypeForGUID", MarshalNative::GetLoadedTypeForGUID)
    FCFuncElement("GetITypeInfoForType", MarshalNative::GetITypeInfoForType)
    FCFuncElement("GetTypedObjectForIUnknown", MarshalNative::GetTypedObjectForIUnknown)
    FCFuncElement("CleanupUnusedObjectsInCurrentContext", MarshalNative::CleanupUnusedObjectsInCurrentContext)
    FCFuncElement("IsTypeVisibleFromCom", MarshalNative::IsTypeVisibleFromCom)
    FCFuncElement("FCallGenerateGuidForType", MarshalNative::DoGenerateGuidForType)
    FCFuncElement("FCallGetTypeLibGuid", MarshalNative::DoGetTypeLibGuid)
    FCFuncElement("GetTypeLibLcid", MarshalNative::GetTypeLibLcid)
    FCFuncElement("GetTypeLibVersion", MarshalNative::GetTypeLibVersion)
    FCFuncElement("FCallGetTypeInfoGuid", MarshalNative::DoGetTypeInfoGuid)
    FCFuncElement("FCallGetTypeLibGuidForAssembly", MarshalNative::DoGetTypeLibGuidForAssembly)
    FCFuncElement("_GetTypeLibVersionForAssembly", MarshalNative::GetTypeLibVersionForAssembly)
    FCFuncElement("GetEndComSlot", MarshalNative::GetEndComSlot)
    FCFuncElement("GetMethodInfoForComSlot", MarshalNative::GetMethodInfoForComSlot)
    FCFuncElement("InternalGetComSlotForMethodInfo", MarshalNative::GetComSlotForMethodInfo)
    FCFuncElement("InternalSwitchCCW", MarshalNative::SwitchCCW)
    FCFuncElement("InternalWrapIUnknownWithComObject", MarshalNative::WrapIUnknownWithComObject)
    FCFuncElement("ChangeWrapperHandleStrength", MarshalNative::ChangeWrapperHandleStrength)
    QCFuncElement("_GetInspectableIids", MarshalNative::GetInspectableIIDs)
    QCFuncElement("_GetCachedWinRTTypes", MarshalNative::GetCachedWinRTTypes)
    QCFuncElement("_GetCachedWinRTTypeByIid", MarshalNative::GetCachedWinRTTypeByIID)
#endif // FEATURE_CORECLR
#endif // FEATURE_COMINTEROP
FCFuncEnd()

FCFuncStart(gArrayWithOffsetFuncs)
    FCFuncElement("CalculateCount", MarshalNative::CalculateCount)
FCFuncEnd()

#ifdef FEATURE_COMINTEROP

#ifndef FEATURE_CORECLR
FCFuncStart(gExtensibleClassFactoryFuncs)
    FCFuncElement("RegisterObjectCreationCallback", RegisterObjectCreationCallback)
FCFuncEnd()
#endif


#ifdef FEATURE_COMINTEROP_TLB_SUPPORT
FCFuncStart(gTypeLibConverterFuncs)
    FCFuncElement("nConvertAssemblyToTypeLib", COMTypeLibConverter::ConvertAssemblyToTypeLib)
    FCFuncElement("nConvertTypeLibToMetadata", COMTypeLibConverter::ConvertTypeLibToMetadata)
    QCFuncElement("LoadInMemoryTypeByName", COMModule::LoadInMemoryTypeByName)
FCFuncEnd()
#endif // FEATURE_COMINTEROP_TLB_SUPPORT

#ifdef FEATURE_COMINTEROP_MANAGED_ACTIVATION
FCFuncStart(gRegistrationFuncs)
    FCFuncElement("RegisterTypeForComClientsNative", RegisterTypeForComClientsNative)
    FCFuncElement("RegisterTypeForComClientsExNative", RegisterTypeForComClientsExNative)
FCFuncEnd()
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION

#endif // FEATURE_COMINTEROP

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gPolicyManagerFuncs)
#ifdef _DEBUG
    QCFuncElement("DebugOut", SecurityConfig::DebugOut)
#endif
FCFuncEnd()

FCFuncStart(gPolicyConfigFuncs)
    QCFuncElement("ResetCacheData", SecurityConfig::ResetCacheData)
    QCFuncElement("SaveDataByte", SecurityConfig::SaveDataByte)
    QCFuncElement("RecoverData", SecurityConfig::RecoverData)
    QCFuncElement("SetQuickCache", SecurityConfig::SetQuickCache)
    QCFuncElement("GetCacheEntry", SecurityConfig::GetCacheEntry)
    QCFuncElement("AddCacheEntry", SecurityConfig::AddCacheEntry)
    QCFuncElement("GetMachineDirectory", SecurityConfig::_GetMachineDirectory)
    QCFuncElement("GetUserDirectory", SecurityConfig::_GetUserDirectory)
    QCFuncElement("WriteToEventLog", SecurityConfig::WriteToEventLog)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

#ifndef FEATURE_CORECLR
FCFuncStart(gPrincipalFuncs)
    FCFuncElement("OpenThreadToken", COMPrincipal::OpenThreadToken)
    QCFuncElement("ImpersonateLoggedOnUser", COMPrincipal::ImpersonateLoggedOnUser)
    QCFuncElement("RevertToSelf", COMPrincipal::RevertToSelf)
    QCFuncElement("SetThreadToken", COMPrincipal::SetThreadToken)
FCFuncEnd()
#endif // !FEATURE_CORECLR

#ifdef FEATURE_CAS_POLICY
FCFuncStart(gEvidenceFuncs)
FCFuncEnd()

FCFuncStart(gAssemblyEvidenceFactoryFuncs)
    QCFuncElement("GetAssemblyPermissionRequests", SecurityPolicy::GetAssemblyPermissionRequests)
    QCFuncElement("GetStrongNameInformation", SecurityPolicy::GetStrongNameInformation)
FCFuncEnd()

FCFuncStart(gPEFileEvidenceFactoryFuncs)
    QCFuncElement("GetAssemblySuppliedEvidence", SecurityPolicy::GetAssemblySuppliedEvidence)
    QCFuncElement("GetLocationEvidence", SecurityPolicy::GetLocationEvidence)
    QCFuncElement("GetPublisherCertificate", SecurityPolicy::GetPublisherCertificate)
    QCFuncElement("FireEvidenceGeneratedEvent", SecurityPolicy::FireEvidenceGeneratedEvent)
FCFuncEnd()

FCFuncStart(gHostExecutionContextManagerFuncs)
    FCFuncElement("ReleaseHostSecurityContext", HostExecutionContextManager::ReleaseSecurityContext)
    FCFuncElement("CloneHostSecurityContext", HostExecutionContextManager::CloneSecurityContext)
    FCFuncElement("CaptureHostSecurityContext", HostExecutionContextManager::CaptureSecurityContext)
    FCFuncElement("SetHostSecurityContext", HostExecutionContextManager::SetSecurityContext)
    FCFuncElement("HostSecurityManagerPresent", HostExecutionContextManager::HostPresent)
FCFuncEnd()
#endif // FEATURE_CAS_POLICY

#if defined(FEATURE_ISOSTORE) && !defined(FEATURE_ISOSTORE_LIGHT)
FCFuncStart(gIsolatedStorage)
    QCFuncElement("GetCaller", COMIsolatedStorage::GetCaller)
FCFuncEnd()

FCFuncStart(gIsolatedStorageFile)
    QCFuncElement("GetRootDir", COMIsolatedStorageFile::GetRootDir)
    QCFuncElement("GetQuota", COMIsolatedStorageFile::GetQuota)
    QCFuncElement("SetQuota", COMIsolatedStorageFile::SetQuota)
    QCFuncElement("Reserve", COMIsolatedStorageFile::Reserve)
    QCFuncElement("GetUsage", COMIsolatedStorageFile::GetUsage)
    QCFuncElement("Open", COMIsolatedStorageFile::Open)
    QCFuncElement("Lock", COMIsolatedStorageFile::Lock)
    QCFuncElement("CreateDirectoryWithDacl", COMIsolatedStorageFile::CreateDirectoryWithDacl)
FCFuncEnd()

FCFuncStart(gIsolatedStorageFileHandle)
    QCFuncElement("Close", COMIsolatedStorageFile::Close)
FCFuncEnd()
#endif // FEATURE_ISOSTORE && !FEATURE_ISOSTORE_LIGHT

FCFuncStart(gTypeLoadExceptionFuncs)
    QCFuncElement("GetTypeLoadExceptionMessage", GetTypeLoadExceptionMessage)
FCFuncEnd()

FCFuncStart(gFileLoadExceptionFuncs)
    QCFuncElement("GetFileLoadExceptionMessage", GetFileLoadExceptionMessage)
    QCFuncElement("GetMessageForHR", FileLoadException_GetMessageForHR)
FCFuncEnd()

FCFuncStart(gMissingMemberExceptionFuncs)
    FCFuncElement("FormatSignature", MissingMemberException_FormatSignature)
FCFuncEnd()

FCFuncStart(gInterlockedFuncs)
    FCIntrinsicSig("Exchange", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::Exchange, CORINFO_INTRINSIC_InterlockedXchg32)
    FCIntrinsicSig("Exchange", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::Exchange64, CORINFO_INTRINSIC_InterlockedXchg64)    
    FCFuncElementSig("Exchange", &gsig_SM_RefDbl_Dbl_RetDbl, COMInterlocked::ExchangeDouble)
    FCFuncElementSig("Exchange", &gsig_SM_RefFlt_Flt_RetFlt, COMInterlocked::ExchangeFloat)
    FCFuncElementSig("Exchange", &gsig_SM_RefObj_Obj_RetObj, COMInterlocked::ExchangeObject)
    FCFuncElementSig("Exchange", &gsig_SM_RefIntPtr_IntPtr_RetIntPtr, COMInterlocked::ExchangePointer)
    FCIntrinsicSig("CompareExchange", &gsig_SM_RefInt_Int_Int_RetInt, COMInterlocked::CompareExchange, CORINFO_INTRINSIC_InterlockedCmpXchg32)
    FCIntrinsicSig("CompareExchange", &gsig_SM_RefLong_Long_Long_RetLong, COMInterlocked::CompareExchange64, CORINFO_INTRINSIC_InterlockedCmpXchg64)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefDbl_Dbl_Dbl_RetDbl, COMInterlocked::CompareExchangeDouble)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefFlt_Flt_Flt_RetFlt, COMInterlocked::CompareExchangeFloat)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefObj_Obj_Obj_RetObj, COMInterlocked::CompareExchangeObject)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefInt_Int_Int_RefBool_RetInt, COMInterlocked::CompareExchangeReliableResult)
    FCFuncElementSig("CompareExchange", &gsig_SM_RefIntPtr_IntPtr_IntPtr_RetIntPtr, COMInterlocked::CompareExchangePointer)
    FCIntrinsicSig("ExchangeAdd", &gsig_SM_RefInt_Int_RetInt, COMInterlocked::ExchangeAdd32, CORINFO_INTRINSIC_InterlockedXAdd32)
    FCIntrinsicSig("ExchangeAdd", &gsig_SM_RefLong_Long_RetLong, COMInterlocked::ExchangeAdd64, CORINFO_INTRINSIC_InterlockedXAdd64)

    FCFuncElement("_Exchange", COMInterlocked::ExchangeGeneric)
    FCFuncElement("_CompareExchange", COMInterlocked::CompareExchangeGeneric)
    
FCFuncEnd()

FCFuncStart(gVarArgFuncs)
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_PtrVoid_RetVoid, VarArgsNative::Init2)
#ifndef FEATURE_CORECLR
    FCFuncElementSig(COR_CTOR_METHOD_NAME, &gsig_IM_IntPtr_RetVoid, VarArgsNative::Init)
    FCFuncElement("GetRemainingCount", VarArgsNative::GetRemainingCount)
    FCFuncElement("_GetNextArgType", VarArgsNative::GetNextArgType)
    FCFuncElement("FCallGetNextArg", VarArgsNative::DoGetNextArg)
    FCFuncElement("InternalGetNextArg", VarArgsNative::GetNextArg2)
#endif // FEATURE_CORECLR
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

FCFuncStart(gCompilerFuncs)
    FCFuncElement("GetObjectValue", ObjectNative::GetObjectValue)
    FCIntrinsic("InitializeArray", ArrayNative::InitializeArray, CORINFO_INTRINSIC_InitializeArray)
    FCFuncElement("_RunClassConstructor", ReflectionInvocation::RunClassConstructor)
#ifndef FEATURE_CORECLR
    FCFuncElement("_RunModuleConstructor", ReflectionInvocation::RunModuleConstructor)
    FCFuncElement("_PrepareMethod", ReflectionInvocation::PrepareMethod)
#endif // !FEATURE_CORECLR
    QCFuncElement("_CompileMethod", ReflectionInvocation::CompileMethod)
#ifndef FEATURE_CORECLR
    FCFuncElement("PrepareDelegate", ReflectionInvocation::PrepareDelegate)
#endif // !FEATURE_CORECLR
    FCFuncElement("PrepareContractedDelegate", ReflectionInvocation::PrepareContractedDelegate)
    FCFuncElement("ProbeForSufficientStack", ReflectionInvocation::ProbeForSufficientStack)
    FCFuncElement("ExecuteCodeWithGuaranteedCleanup", ReflectionInvocation::ExecuteCodeWithGuaranteedCleanup)
    FCFuncElement("GetHashCode", ObjectNative::GetHashCode)
    FCFuncElement("Equals", ObjectNative::Equals)
    FCFuncElement("EnsureSufficientExecutionStack", ReflectionInvocation::EnsureSufficientExecutionStack)
#ifdef FEATURE_CORECLR
    FCFuncElement("TryEnsureSufficientExecutionStack", ReflectionInvocation::TryEnsureSufficientExecutionStack)
#endif // FEATURE_CORECLR
FCFuncEnd()

FCFuncStart(gContextSynchronizationFuncs)
#ifdef FEATURE_SYNCHRONIZATIONCONTEXT_WAIT
    FCFuncElement("WaitHelper", SynchronizationContextNative::WaitHelper)
#endif // #ifdef FEATURE_SYNCHRONIZATIONCONTEXT_WAIT
#ifdef FEATURE_APPX
    QCFuncElement("GetWinRTDispatcherForCurrentThread", SynchronizationContextNative::GetWinRTDispatcherForCurrentThread)
#endif
FCFuncEnd()

FCFuncStart(gDateMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::DateMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::DateMarshaler__ConvertToManaged)
FCFuncEnd()

FCFuncStart(gValueClassMarshalerFuncs)
    FCFuncElement("ConvertToNative", StubHelpers::ValueClassMarshaler__ConvertToNative)
    FCFuncElement("ConvertToManaged", StubHelpers::ValueClassMarshaler__ConvertToManaged)
    FCFuncElement("ClearNative", StubHelpers::ValueClassMarshaler__ClearNative)
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
    QCFuncElement("ClearNative", StubHelpers::InterfaceMarshaler__ClearNative)
    FCFuncElement("ConvertToManagedWithoutUnboxing", StubHelpers::InterfaceMarshaler__ConvertToManagedWithoutUnboxing)
FCFuncEnd()

FCFuncStart(gUriMarshalerFuncs)
    FCFuncElement("GetRawUriFromNative", StubHelpers::UriMarshaler__GetRawUriFromNative)
    FCFuncElement("CreateNativeUriInstanceHelper", StubHelpers::UriMarshaler__CreateNativeUriInstance)
FCFuncEnd()

FCFuncStart(gEventArgsMarshalerFuncs)
    QCFuncElement("CreateNativeNCCEventArgsInstanceHelper", StubHelpers::EventArgsMarshaler__CreateNativeNCCEventArgsInstance)
    QCFuncElement("CreateNativePCEventArgsInstance", StubHelpers::EventArgsMarshaler__CreateNativePCEventArgsInstance)
FCFuncEnd()

FCFuncStart(gMngdSafeArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdSafeArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdSafeArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdSafeArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdSafeArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdSafeArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNative", MngdSafeArrayMarshaler::ClearNative)
FCFuncEnd()

FCFuncStart(gMngdHiddenLengthArrayMarshalerFuncs)
    FCFuncElement("CreateMarshaler", MngdHiddenLengthArrayMarshaler::CreateMarshaler)
    FCFuncElement("ConvertSpaceToNative", MngdHiddenLengthArrayMarshaler::ConvertSpaceToNative)
    FCFuncElement("ConvertContentsToNative", MngdHiddenLengthArrayMarshaler::ConvertContentsToNative)
    FCFuncElement("ConvertSpaceToManaged", MngdHiddenLengthArrayMarshaler::ConvertSpaceToManaged)
    FCFuncElement("ConvertContentsToManaged", MngdHiddenLengthArrayMarshaler::ConvertContentsToManaged)
    FCFuncElement("ClearNativeContents", MngdHiddenLengthArrayMarshaler::ClearNativeContents)
FCFuncEnd()
    
FCFuncStart(gWinRTTypeNameConverterFuncs)
    FCFuncElement("ConvertToWinRTTypeName", StubHelpers::WinRTTypeNameConverter__ConvertToWinRTTypeName)
    FCFuncElement("GetTypeFromWinRTTypeName", StubHelpers::WinRTTypeNameConverter__GetTypeFromWinRTTypeName)
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
#ifndef FEATURE_CORECLR
#ifndef _WIN64
    FCFuncElement("GetFinalStubTarget", StubHelpers::GetFinalStubTarget)
#endif // !_WIN64
    FCFuncElement("DemandPermission", StubHelpers::DemandPermission)
#endif // !FEATURE_CORECLR
    FCFuncElement("IsQCall", StubHelpers::IsQCall)
    FCFuncElement("InitDeclaringType", StubHelpers::InitDeclaringType)
    FCIntrinsic("GetNDirectTarget", StubHelpers::GetNDirectTarget, CORINFO_INTRINSIC_StubHelpers_GetNDirectTarget)
    FCFuncElement("GetDelegateTarget", StubHelpers::GetDelegateTarget)
    FCFuncElement("SetLastError", StubHelpers::SetLastError)
#ifdef FEATURE_CORECLR
    FCFuncElement("ClearLastError", StubHelpers::ClearLastError)
#endif
    FCFuncElement("ThrowInteropParamException", StubHelpers::ThrowInteropParamException)
    FCFuncElement("InternalGetHRExceptionObject", StubHelpers::GetHRExceptionObject)
#ifdef FEATURE_COMINTEROP
    FCFuncElement("InternalGetCOMHRExceptionObject", StubHelpers::GetCOMHRExceptionObject)
    FCFuncElement("GetCOMIPFromRCW", StubHelpers::GetCOMIPFromRCW)
    FCFuncElement("GetCOMIPFromRCW_WinRT", StubHelpers::GetCOMIPFromRCW_WinRT)
    FCFuncElement("GetCOMIPFromRCW_WinRTSharedGeneric", StubHelpers::GetCOMIPFromRCW_WinRTSharedGeneric)
    FCFuncElement("GetCOMIPFromRCW_WinRTDelegate", StubHelpers::GetCOMIPFromRCW_WinRTDelegate)
    FCFuncElement("ShouldCallWinRTInterface", StubHelpers::ShouldCallWinRTInterface)
    FCFuncElement("GetTargetForAmbiguousVariantCall", StubHelpers::GetTargetForAmbiguousVariantCall)
    FCFuncElement("StubRegisterRCW", StubHelpers::StubRegisterRCW)
    FCFuncElement("StubUnregisterRCW", StubHelpers::StubUnregisterRCW)
    FCFuncElement("GetDelegateInvokeMethod", StubHelpers::GetDelegateInvokeMethod)
    FCFuncElement("GetWinRTFactoryObject", StubHelpers::GetWinRTFactoryObject)
    FCFuncElement("GetWinRTFactoryReturnValue", StubHelpers::GetWinRTFactoryReturnValue)
    FCFuncElement("GetOuterInspectable", StubHelpers::GetOuterInspectable)
#ifdef MDA_SUPPORTED
    FCFuncElement("TriggerExceptionSwallowedMDA", StubHelpers::TriggerExceptionSwallowedMDA)
#endif
#endif // FEATURE_COMINTEROP
#ifdef MDA_SUPPORTED
    FCFuncElement("CheckCollectedDelegateMDA", StubHelpers::CheckCollectedDelegateMDA)
#endif // MDA_SUPPORTED
#ifdef PROFILING_SUPPORTED    
    FCFuncElement("ProfilerBeginTransitionCallback", StubHelpers::ProfilerBeginTransitionCallback)
    FCFuncElement("ProfilerEndTransitionCallback", StubHelpers::ProfilerEndTransitionCallback)
#endif    
    FCFuncElement("CreateCustomMarshalerHelper", StubHelpers::CreateCustomMarshalerHelper)
    FCFuncElement("DecimalCanonicalizeInternal", StubHelpers::DecimalCanonicalizeInternal)
    FCFuncElement("FmtClassUpdateNativeInternal", StubHelpers::FmtClassUpdateNativeInternal)
    FCFuncElement("FmtClassUpdateCLRInternal", StubHelpers::FmtClassUpdateCLRInternal)
    FCFuncElement("LayoutDestroyNativeInternal", StubHelpers::LayoutDestroyNativeInternal)
    FCFuncElement("AllocateInternal", StubHelpers::AllocateInternal)
    FCFuncElement("strlen", StubHelpers::AnsiStrlen)    
    FCFuncElement("MarshalToUnmanagedVaListInternal", StubHelpers::MarshalToUnmanagedVaListInternal)
    FCFuncElement("MarshalToManagedVaListInternal", StubHelpers::MarshalToManagedVaListInternal)
    FCFuncElement("CalcVaListSize", StubHelpers::CalcVaListSize)
    FCFuncElement("ValidateObject", StubHelpers::ValidateObject)
    FCFuncElement("ValidateByref", StubHelpers::ValidateByref)
    FCFuncElement("LogPinnedArgument", StubHelpers::LogPinnedArgument)
    FCIntrinsic("GetStubContext", StubHelpers::GetStubContext, CORINFO_INTRINSIC_StubHelpers_GetStubContext)
#ifdef _WIN64
    FCIntrinsic("GetStubContextAddr", StubHelpers::GetStubContextAddr, CORINFO_INTRINSIC_StubHelpers_GetStubContextAddr)
#endif // _WIN64
#ifdef MDA_SUPPORTED
    FCFuncElement("TriggerGCForMDA", StubHelpers::TriggerGCForMDA)
#endif // MDA_SUPPORTED
#ifdef FEATURE_ARRAYSTUB_AS_IL
    FCFuncElement("ArrayTypeCheck", StubHelpers::ArrayTypeCheck)
#endif //FEATURE_ARRAYSTUB_AS_IL
#ifdef FEATURE_STUBS_AS_IL
    FCFuncElement("MulticastDebuggerTraceHelper", StubHelpers::MulticastDebuggerTraceHelper)
#endif //FEATURE_STUBS_AS_IL
FCFuncEnd()

FCFuncStart(gCoverageFuncs)
    FCUnreferenced FCFuncElement("nativeCoverBlock", COMCoverage::nativeCoverBlock)
FCFuncEnd()

FCFuncStart(gGCHandleFuncs)
    FCFuncElement("InternalAlloc", MarshalNative::GCHandleInternalAlloc)
    FCFuncElement("InternalFree", MarshalNative::GCHandleInternalFree)
    FCFuncElement("InternalGet", MarshalNative::GCHandleInternalGet)
    FCFuncElement("InternalSet", MarshalNative::GCHandleInternalSet)
    FCFuncElement("InternalCompareExchange", MarshalNative::GCHandleInternalCompareExchange)
    FCFuncElement("InternalAddrOfPinnedObject", MarshalNative::GCHandleInternalAddrOfPinnedObject)
    FCFuncElement("InternalCheckDomain", MarshalNative::GCHandleInternalCheckDomain)
#ifndef FEATURE_CORECLR
    FCFuncElement("InternalGetHandleType", MarshalNative::GCHandleInternalGetHandleType)
#endif
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gConfigHelper)
    FCFuncElement("RunParser", ConfigNative::RunParser)
FCFuncEnd()
#endif // FEATURE_CORECLR

#ifndef FEATURE_CORECLR
FCFuncStart(gConsoleFuncs)    
    QCFuncElement("GetTitleNative", ConsoleNative::GetTitle) 
FCFuncEnd()
#endif // ifndef FEATURE_CORECLR

FCFuncStart(gVersioningHelperFuncs)
    FCFuncElement("GetRuntimeId", GetRuntimeId_Wrapper)
FCFuncEnd()

FCFuncStart(gStreamFuncs)
    FCFuncElement("HasOverriddenBeginEndRead", StreamNative::HasOverriddenBeginEndRead)
    FCFuncElement("HasOverriddenBeginEndWrite", StreamNative::HasOverriddenBeginEndWrite)
FCFuncEnd()

#ifndef FEATURE_CORECLR
FCFuncStart(gConsoleStreamFuncs)
    FCFuncElement("WaitForAvailableConsoleInput", ConsoleStreamHelper::WaitForAvailableConsoleInput)
FCFuncEnd()
#endif

#if defined(FEATURE_COMINTEROP) && defined(FEATURE_REFLECTION_ONLY_LOAD)
FCFuncStart(gWindowsRuntimeMetadata)
    QCFuncElement("nResolveNamespace", CLRPrivTypeCacheReflectionOnlyWinRT::ResolveNamespace)
FCFuncEnd()
#endif //FEATURE_COMINTEROP && FEATURE_REFLECTION_ONLY_LOAD

#ifdef FEATURE_COMINTEROP
FCFuncStart(gWindowsRuntimeBufferHelperFuncs)
    QCFuncElement("StoreOverlappedPtrInCCW", WindowsRuntimeBufferHelper::StoreOverlappedPtrInCCW) 
    //QCFuncElement("ReleaseOverlapped", WindowsRuntimeBufferHelper::ReleaseOverlapped) 
FCFuncEnd()
#endif // ifdef FEATURE_COMINTEROP

#if defined(FEATURE_EVENTSOURCE_XPLAT)
FCFuncStart(gEventLogger)
    QCFuncElement("IsEventSourceLoggingEnabled", XplatEventSourceLogger::IsEventSourceLoggingEnabled)
    QCFuncElement("LogEventSource", XplatEventSourceLogger::LogEventSource)
FCFuncEnd()
#endif // defined(FEATURE_EVENTSOURCE_XPLAT)

#ifdef FEATURE_COMINTEROP
FCFuncStart(gRuntimeClassFuncs)
    FCFuncElement("GetRedirectedGetHashCodeMD", ComObject::GetRedirectedGetHashCodeMD)
    FCFuncElement("RedirectGetHashCode", ComObject::RedirectGetHashCode)
    FCFuncElement("GetRedirectedToStringMD", ComObject::GetRedirectedToStringMD)
    FCFuncElement("RedirectToString", ComObject::RedirectToString)
    FCFuncElement("GetRedirectedEqualsMD", ComObject::GetRedirectedEqualsMD)
    FCFuncElement("RedirectEquals", ComObject::RedirectEquals)
FCFuncEnd()
#endif // ifdef FEATURE_COMINTEROP
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

FCClassElement("AppDomain", "System", gAppDomainFuncs)
FCClassElement("AppDomainManager", "System", gAppDomainManagerFuncs)
#ifdef FEATURE_FUSION
FCClassElement("AppDomainSetup", "System", gAppDomainSetupFuncs)
#endif // FEATURE_FUSION
FCClassElement("ArgIterator", "System", gVarArgFuncs)
FCClassElement("Array", "System", gArrayFuncs)
FCClassElement("ArrayWithOffset", "System.Runtime.InteropServices", gArrayWithOffsetFuncs)
FCClassElement("AssemblyBuilder", "System.Reflection.Emit", gAssemblyBuilderFuncs)
#ifdef FEATURE_CAS_POLICY
FCClassElement("AssemblyEvidenceFactory", "System.Security.Policy", gAssemblyEvidenceFactoryFuncs)
#endif // FEATURE_CAS_POLICY

#ifdef FEATURE_CORECLR
FCClassElement("AssemblyExtensions", "System.Reflection.Metadata", gAssemblyExtensionsFuncs)
#endif

#if defined(FEATURE_HOST_ASSEMBLY_RESOLVER)
FCClassElement("AssemblyLoadContext", "System.Runtime.Loader", gAssemblyLoadContextFuncs)
#endif // defined(FEATURE_HOST_ASSEMBLY_RESOLVER)

FCClassElement("AssemblyName", "System.Reflection", gAssemblyNameFuncs)
FCClassElement("Assert", "System.Diagnostics", gDiagnosticsAssert)
FCClassElement("BCLDebug", "System", gBCLDebugFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("BaseConfigHandler", "System", gConfigHelper)
#endif // FEATURE_CORECLR
FCClassElement("Buffer", "System", gBufferFuncs)
#ifndef FEATURE_CORECLR
// Since the 2nd letter of the classname is capital, we need to sort this before all class names
// that start with Cx where x is any small letter (strcmp is used for verification).
FCClassElement("CLRConfig", "System", gCLRConfigFuncs)
#endif // FEATURE_CORECLR
#if !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("CalendarData", "System.Globalization", gCalendarDataFuncs)
#endif // !defined(FEATURE_COREFX_GLOBALIZATION)
#ifndef FEATURE_CORECLR
FCClassElement("ChannelServices", "System.Runtime.Remoting.Channels", gChannelServicesFuncs)
#endif // FEATURE_CORECLR
#ifdef FEATURE_CAS_POLICY
FCClassElement("CodeAccessSecurityEngine", "System.Security", gCodeAccessSecurityEngineFuncs)
#endif
FCClassElement("CompareInfo", "System.Globalization", gCompareInfoFuncs)
FCClassElement("CompatibilitySwitch", "System.Runtime.Versioning", gCompatibilitySwitchFuncs)
#ifdef FEATURE_COMPRESSEDSTACK    
FCClassElement("CompressedStack", "System.Threading", gCompressedStackFuncs)
#endif // FEATURE_COMPRESSEDSTACK    
#ifdef FEATURE_CAS_POLICY
FCClassElement("Config", "System.Security.Util", gPolicyConfigFuncs)
#endif // FEATURE_CAS_POLICY
#ifndef FEATURE_CORECLR
FCClassElement("Console", "System", gConsoleFuncs)
#endif // ifndef FEATURE_CORECLR
#ifdef FEATURE_REMOTING    
FCClassElement("Context", "System.Runtime.Remoting.Contexts", gContextFuncs)
#endif
FCClassElement("CriticalHandle", "System.Runtime.InteropServices", gCriticalHandleFuncs)
#if !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("CultureData", "System.Globalization", gCultureDataFuncs)
FCClassElement("CultureInfo", "System.Globalization", gCultureInfoFuncs)
#endif
FCClassElement("Currency", "System", gCurrencyFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("CurrentSystemTimeZone", "System", gTimeZoneFuncs)
#endif // FEATURE_CORECLR
FCClassElement("CustomAttribute", "System.Reflection", gCOMCustomAttributeFuncs)
FCClassElement("CustomAttributeEncodedArgument", "System.Reflection", gCustomAttributeEncodedArgument)
FCClassElement("DateMarshaler", "System.StubHelpers", gDateMarshalerFuncs)
FCClassElement("DateTime", "System", gDateTimeFuncs)
FCClassElement("Debugger", "System.Diagnostics", gDiagnosticsDebugger)
FCClassElement("Decimal", "System", gDecimalFuncs)
FCClassElement("DefaultBinder", "System", gCOMDefaultBinderFuncs)
FCClassElement("Delegate", "System", gDelegateFuncs)
FCClassElement("DependentHandle", "System.Runtime.CompilerServices", gDependentHandleFuncs)
#ifdef FEATURE_COMPRESSEDSTACK
FCClassElement("DomainCompressedStack", "System.Threading", gDomainCompressedStackFuncs)    
#endif // FEATURE_COMPRESSEDSTACK    
#if !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("EncodingTable", "System.Globalization", gEncodingTableFuncs)
#endif // !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("Enum", "System", gEnumFuncs)
FCClassElement("Environment", "System", gEnvironmentFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("EventArgsMarshaler", "System.StubHelpers", gEventArgsMarshalerFuncs)
#endif // FEATURE_COMINTEROP
FCClassElement("Exception", "System", gExceptionFuncs)
#if defined(FEATURE_COMINTEROP) && !defined(FEATURE_CORECLR)
FCClassElement("ExtensibleClassFactory", "System.Runtime.InteropServices", gExtensibleClassFactoryFuncs)
#endif
FCClassElement("FileIOAccess", "System.Security.Permissions", gCOMFileIOAccessFuncs)
FCClassElement("FileLoadException", "System.IO", gFileLoadExceptionFuncs)
FCClassElement("FormatterServices", "System.Runtime.Serialization", gSerializationFuncs)
#ifdef FEATURE_CAS_POLICY
FCClassElement("FrameSecurityDescriptor", "System.Security", gFrameSecurityDescriptorFuncs)
#endif
FCClassElement("GC", "System", gGCInterfaceFuncs)
FCClassElement("GCHandle", "System.Runtime.InteropServices", gGCHandleFuncs)
#ifdef FEATURE_CAS_POLICY
FCClassElement("HostExecutionContextManager", "System.Threading", gHostExecutionContextManagerFuncs)
#endif // FEATURE_CAS_POLICY
#ifdef FEATURE_COMINTEROP
FCClassElement("IEnumerable", "System.Collections", gStdMngIEnumerableFuncs)
FCClassElement("IEnumerator", "System.Collections", gStdMngIEnumeratorFuncs)
FCClassElement("IExpando", "System.Runtime.InteropServices.Expando", gStdMngIExpandoFuncs)
#endif // FEATURE_COMINTEROP
FCClassElement("ILCover", "System.Coverage", gCoverageFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("IReflect", "System.Reflection", gStdMngIReflectFuncs)
#endif
#ifdef FEATURE_COMINTEROP
FCClassElement("InterfaceMarshaler", "System.StubHelpers", gInterfaceMarshalerFuncs)
#endif
FCClassElement("Interlocked", "System.Threading", gInterlockedFuncs)
#if defined(FEATURE_ISOSTORE) && !defined(FEATURE_ISOSTORE_LIGHT)
FCClassElement("IsolatedStorage", "System.IO.IsolatedStorage", gIsolatedStorage)
FCClassElement("IsolatedStorageFile", "System.IO.IsolatedStorage", gIsolatedStorageFile)
#endif // FEATURE_ISOSTORE && !FEATURE_ISOSTORE_LIGHT
FCClassElement("JitHelpers", "System.Runtime.CompilerServices", gJitHelpers)
FCClassElement("LoaderAllocatorScout", "System.Reflection", gLoaderAllocatorFuncs)
FCClassElement("Log", "System.Diagnostics", gDiagnosticsLog)
FCClassElement("ManifestBasedResourceGroveler", "System.Resources",  gManifestBasedResourceGrovelerFuncs)
FCClassElement("Marshal", "System.Runtime.InteropServices", gInteropMarshalFuncs)
#ifdef FEATURE_REMOTING
FCClassElement("MarshalByRefObject", "System", gMarshalByRefFuncs)
#endif
FCClassElement("Math", "System", gMathFuncs)
#ifdef MDA_SUPPORTED 
FCClassElement("Mda", "System", gMda)
#endif
#ifndef FEATURE_CORECLR
FCClassElement("MemoryFailPoint", "System.Runtime", gMemoryFailPointFuncs)
#endif // FEATURE_CORECLR
#ifdef FEATURE_REMOTING    
FCClassElement("Message", "System.Runtime.Remoting.Messaging", gMessageFuncs)
#endif    
FCClassElement("MetadataImport", "System.Reflection", gMetaDataImport)
#ifdef FEATURE_METHOD_RENTAL
FCClassElement("MethodRental", "System.Reflection.Emit", gCOMMethodRental)
#endif // FEATURE_METHOD_RENTAL
FCClassElement("MissingMemberException", "System",  gMissingMemberExceptionFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("MngdHiddenLengthArrayMarshaler", "System.StubHelpers", gMngdHiddenLengthArrayMarshalerFuncs)
#endif // FEATURE_COMINTEROP
FCClassElement("MngdNativeArrayMarshaler", "System.StubHelpers", gMngdNativeArrayMarshalerFuncs)    
FCClassElement("MngdRefCustomMarshaler", "System.StubHelpers", gMngdRefCustomMarshalerFuncs)    
#ifdef FEATURE_COMINTEROP
FCClassElement("MngdSafeArrayMarshaler", "System.StubHelpers", gMngdSafeArrayMarshalerFuncs)  
#endif // FEATURE_COMINTEROP
FCClassElement("ModuleBuilder", "System.Reflection.Emit", gCOMModuleBuilderFuncs)
FCClassElement("ModuleHandle", "System", gCOMModuleHandleFuncs)
FCClassElement("Monitor", "System.Threading", gMonitorFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("Normalization", "System.Text", gNormalizationFuncs)
#endif // FEATURE_CORECLR
FCClassElement("Number", "System", gNumberFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("OAVariantLib", "Microsoft.Win32", gOAVariantFuncs)
#endif
FCClassElement("Object", "System", gObjectFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("ObjectMarshaler", "System.StubHelpers", gObjectMarshalerFuncs)
#endif
FCClassElement("OverlappedData", "System.Threading", gOverlappedFuncs)
#ifdef FEATURE_CAS_POLICY
FCClassElement("PEFileEvidenceFactory", "System.Security.Policy", gPEFileEvidenceFactoryFuncs)
#endif // FEATURE_CAS_POLICY
FCClassElement("ParseNumbers", "System", gParseNumbersFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("PasswordDeriveBytes", "System.Security.Cryptography", gPasswordDeriveBytesFuncs)
#endif
#ifdef FEATURE_CAS_POLICY
FCClassElement("PolicyManager", "System.Security", gPolicyManagerFuncs)
#endif    

#if defined(FEATURE_MULTICOREJIT) && !defined(FEATURE_CORECLR)
FCClassElement("ProfileOptimization", "System.Runtime", gProfileOptimizationFuncs)
#endif  // defined(FEATURE_MULTICOREJIT) && !defined(FEATURE_CORECLR)

FCClassElement("PseudoCustomAttribute", "System.Reflection", gPseudoCustomAttribute)
#ifdef FEATURE_CORECLR
FCClassElement("PunkSafeHandle", "System.Reflection.Emit", gSymWrapperCodePunkSafeHandleFuncs)
#endif
#ifndef FEATURE_CORECLR
FCClassElement("RNGCryptoServiceProvider", "System.Security.Cryptography", gRNGCryptoServiceProviderFuncs)
#endif
#ifdef FEATURE_CRYPTO
FCClassElement("RSACryptoServiceProvider", "System.Security.Cryptography", gRSACryptoServiceProviderFuncs)
#endif
#ifdef FEATURE_RWLOCK
FCClassElement("ReaderWriterLock", "System.Threading", gRWLockFuncs)
#endif  // FEATURE_RWLOCK
#ifdef FEATURE_REMOTING    
FCClassElement("RealProxy", "System.Runtime.Remoting.Proxies", gRealProxyFuncs)
#endif    
FCClassElement("RegisteredWaitHandleSafe", "System.Threading", gRegisteredWaitHandleFuncs)
#ifdef FEATURE_COMINTEROP
#ifdef FEATURE_COMINTEROP_MANAGED_ACTIVATION
FCClassElement("RegistrationServices", "System.Runtime.InteropServices", gRegistrationFuncs)
#endif // FEATURE_COMINTEROP_MANAGED_ACTIVATION
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_REMOTING
FCClassElement("RemotingServices", "System.Runtime.Remoting", gRemotingFuncs)
#endif
#if defined(FEATURE_CRYPTO)
FCClassElement("Rfc2898DeriveBytes", "System.Security.Cryptography", gRfc2898DeriveBytesFuncs)
#endif
FCClassElement("RtFieldInfo", "System.Reflection", gRuntimeFieldInfoFuncs)
FCClassElement("RuntimeAssembly", "System.Reflection", gAssemblyFuncs)
#ifdef FEATURE_COMINTEROP    
FCClassElement("RuntimeClass", "System.Runtime.InteropServices.WindowsRuntime", gRuntimeClassFuncs)
#endif // FEATURE_COMINTEROP    
FCClassElement("RuntimeEnvironment", "System.Runtime.InteropServices", gRuntimeEnvironmentFuncs)
FCClassElement("RuntimeFieldHandle", "System", gCOMFieldHandleNewFuncs)
FCClassElement("RuntimeHelpers", "System.Runtime.CompilerServices", gCompilerFuncs)
FCClassElement("RuntimeMethodHandle", "System", gRuntimeMethodHandle)
FCClassElement("RuntimeModule", "System.Reflection", gCOMModuleFuncs)
FCClassElement("RuntimeType", "System", gSystem_RuntimeType)
FCClassElement("RuntimeTypeHandle", "System", gCOMTypeHandleFuncs)
FCClassElement("SafeBuffer", "System.Runtime.InteropServices", gSafeBufferFuncs)
#ifdef FEATURE_X509
FCClassElement("SafeCertContextHandle", "System.Security.Cryptography.X509Certificates", gX509SafeCertContextHandleFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("SafeCertStoreHandle", "System.Security.Cryptography.X509Certificates", gX509SafeCertStoreHandleFuncs)
#endif // FEATURE_CORECLR
#endif // FEATURE_X509
FCClassElement("SafeHandle", "System.Runtime.InteropServices", gSafeHandleFuncs)
#ifdef FEATURE_CRYPTO
FCClassElement("SafeHashHandle", "System.Security.Cryptography", gSafeHashHandleFuncs)
#endif // FEATURE_CRYPTO
#if defined(FEATURE_ISOSTORE) && !defined(FEATURE_ISOSTORE_LIGHT)
FCClassElement("SafeIsolatedStorageFileHandle", "System.IO.IsolatedStorage", gIsolatedStorageFileHandle)
#endif // FEATURE_ISOSTORE && !FEATURE_ISOSTORE_LIGHT
#ifdef FEATURE_CRYPTO
FCClassElement("SafeKeyHandle", "System.Security.Cryptography", gSafeKeyHandleFuncs)
#endif
#ifdef FEATURE_CAS_POLICY
FCClassElement("SafePEFileHandle", "Microsoft.Win32.SafeHandles", gPEFileFuncs)
#endif // FEATURE_CAS_POLICY
#ifdef FEATURE_CRYPTO
FCClassElement("SafeProvHandle", "System.Security.Cryptography", gSafeProvHandleFuncs)
#endif
#ifndef FEATURE_CORECLR
FCClassElement("SafeTypeNameParserHandle", "System", gSafeTypeNameParserHandle)
#endif //!FEATURE_CORECLR
#if defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
FCClassElement("SecurityContext", "System.Security", gCOMSecurityContextFuncs)
#endif // defined(FEATURE_IMPERSONATION) || defined(FEATURE_COMPRESSEDSTACK)
FCClassElement("SecurityContextFrame", "System.Reflection", gSecurityContextFrameFuncs)
FCClassElement("SecurityManager", "System.Security", gCOMSecurityManagerFuncs)
#ifdef FEATURE_CAS_POLICY
FCClassElement("SecurityRuntime", "System.Security", gCOMSecurityRuntimeFuncs)
#endif
FCClassElement("Signature", "System", gSignatureNative)
#ifndef FEATURE_CORECLR
FCClassElement("SizedReference", "System", gSizedRefHandleFuncs)
#endif // !FEATURE_CORECLR
#ifdef FEATURE_REMOTING    
FCClassElement("StackBuilderSink", "System.Runtime.Remoting.Messaging", gStackBuilderSinkFuncs)
#endif    
FCClassElement("StackTrace", "System.Diagnostics", gDiagnosticsStackTrace)
FCClassElement("Stream", "System.IO", gStreamFuncs)
FCClassElement("String", "System", gStringFuncs)
FCClassElement("StringBuilder", "System.Text", gStringBufferFuncs)
FCClassElement("StringExpressionSet", "System.Security.Util", gCOMStringExpressionSetFuncs)
FCClassElement("StubHelpers", "System.StubHelpers", gStubHelperFuncs)
#if defined(FEATURE_SYNCHRONIZATIONCONTEXT_WAIT) || defined(FEATURE_APPX)
FCClassElement("SynchronizationContext", "System.Threading", gContextSynchronizationFuncs)
#endif // FEATURE_SYNCHRONIZATIONCONTEXT_WAIT || FEATURE_APPX
#if !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("TextInfo", "System.Globalization", gTextInfoFuncs)
#endif // !defined(FEATURE_COREFX_GLOBALIZATION)
FCClassElement("Thread", "System.Threading", gThreadFuncs)
FCClassElement("ThreadPool", "System.Threading", gThreadPoolFuncs)
#ifndef FEATURE_CORECLR
FCClassElement("TimeSpan", "System", gTimeSpanFuncs)
#endif // !FEATURE_CORECLR
FCClassElement("TimerQueue", "System.Threading", gTimerFuncs)
FCClassElement("Type", "System", gSystem_Type)
FCClassElement("TypeBuilder", "System.Reflection.Emit", gCOMClassWriter)
#ifdef FEATURE_COMINTEROP_TLB_SUPPORT
FCClassElement("TypeLibConverter", "System.Runtime.InteropServices", gTypeLibConverterFuncs)
#endif
FCClassElement("TypeLoadException", "System", gTypeLoadExceptionFuncs)
FCClassElement("TypeNameBuilder", "System.Reflection.Emit", gTypeNameBuilder)
#ifndef FEATURE_CORECLR
FCClassElement("TypeNameParser", "System", gTypeNameParser)
#endif //!FEATURE_CORECLR
FCClassElement("TypedReference", "System", gTypedReferenceFuncs)
FCClassElement("URLString", "System.Security.Util", gCOMUrlStringFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("UriMarshaler", "System.StubHelpers", gUriMarshalerFuncs)
#endif
FCClassElement("Utf8String", "System", gUtf8String)
#ifdef FEATURE_CRYPTO
FCClassElement("Utils", "System.Security.Cryptography", gCryptographyUtilsFuncs)
#endif
FCClassElement("ValueClassMarshaler", "System.StubHelpers", gValueClassMarshalerFuncs)
FCClassElement("ValueType", "System", gValueTypeFuncs)
#ifdef FEATURE_COMINTEROP
FCClassElement("Variant", "System", gVariantFuncs)
#endif
FCClassElement("VersioningHelper", "System.Runtime.Versioning", gVersioningHelperFuncs)
FCClassElement("WaitHandle", "System.Threading", gWaitHandleFuncs)
FCClassElement("WeakReference", "System", gWeakReferenceFuncs)
FCClassElement("WeakReference`1", "System", gWeakReferenceOfTFuncs)

#ifndef FEATURE_CORECLR
FCClassElement("Win32", "System.Security.Principal", gPrincipalFuncs)
#endif

#ifdef FEATURE_COMINTEROP
FCClassElement("WinRTTypeNameConverter", "System.StubHelpers", gWinRTTypeNameConverterFuncs)
#endif // FEATURE_COMINTEROP

#ifdef FEATURE_COMINTEROP
FCClassElement("WindowsRuntimeBufferHelper", "System.Runtime.InteropServices.WindowsRuntime", gWindowsRuntimeBufferHelperFuncs)                    
#endif

#ifndef FEATURE_CORECLR
FCClassElement("WindowsRuntimeDesignerContext", "System.Runtime.DesignerServices", gWindowsRuntimeContextFuncs)
#endif

#if defined(FEATURE_COMINTEROP) && defined(FEATURE_REFLECTION_ONLY_LOAD)
FCClassElement("WindowsRuntimeMetadata", "System.Runtime.InteropServices.WindowsRuntime", gWindowsRuntimeMetadata)
#endif

#ifdef FEATURE_X509
FCClassElement("X509Utils", "System.Security.Cryptography.X509Certificates", gX509CertificateFuncs)
#endif // FEATURE_X509
#if defined(FEATURE_EVENTSOURCE_XPLAT)
FCClassElement("XplatEventLogger", "System.Diagnostics.Tracing", gEventLogger)
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)
#ifdef FEATURE_CAS_POLICY
FCClassElement("Zone", "System.Security.Policy", gCOMSecurityZone)
#endif // FEATURE_CAS_POLICY
#ifndef FEATURE_CORECLR
FCClassElement("__ConsoleStream", "System.IO", gConsoleStreamFuncs)
#endif


#undef FCFuncElement
#undef FCFuncElementSig
#undef FCIntrinsic
#undef FCIntrinsicSig
#undef QCFuncElement
#undef FCDynamic
#undef FCDynamicSig
#undef FCUnreferenced
#undef FCFuncStart
#undef FCFuncEnd
#undef FCClassElement
