// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Shared native entrypoint table for CoreLib QCalls and fixed-address FCalls.

#include "common.h"

//
// Headers for all ECall entrypoints
//
#include "arraynative.h"
#include "dllimport.h"
#include "comdelegate.h"
#include "customattribute.h"
#include "comdynamic.h"
#include "excep.h"
#include "fcall.h"
#include "ecall.h"
#include "clrconfignative.h"
#include "commodule.h"
#include "marshalnative.h"
#include "nativelibrarynative.h"
#include "comutilnative.h"
#include "comsynchronizable.h"
#include "debugdebugger.h"
#include "assemblynative.hpp"

#include "proftoeeinterfaceimpl.h"

#include "appdomainnative.hpp"
#include "conditionalweaktable.h"
#include "runtimehandles.h"
#include "reflectioninvocation.h"
#include "managedmdimport.hpp"
#include "typestring.h"
#include "comdependenthandle.h"
#include "weakreferencenative.h"
#ifdef FEATURE_VARARGS
#include "varargsnative.h"
#endif // FEATURE_VARARGS
#include "mlinfo.h"

#ifdef FEATURE_COMINTEROP
#include "olevariant.h"
#endif // FEATURE_COMINTEROP

#include "interoplibinterface.h"
#if defined(FEATURE_COMWRAPPERS)
#include "interoplibinterface_comwrappers.h"
#endif

#include "stubhelpers.h"
#include "ilmarshalers.h"

#ifdef FEATURE_MULTICOREJIT
#include "multicorejit.h"
#endif

#if defined(FEATURE_EVENTSOURCE_XPLAT)
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"
#include "nativeeventsource.h"
#endif //defined(FEATURE_EVENTSOURCE_XPLAT)

#ifdef FEATURE_PERFTRACING
#include "eventpipeadapter.h"
#include "eventpipeinternal.h"
#include "nativeeventsource.h"
#endif //FEATURE_PERFTRACING

#include "tailcallhelp.h"
#include "JitQCallHelpers.h"

#include <minipal/entrypoints.h>

#include "exceptionhandlingqcalls.h"

#include "versionresilienthashcode.h"

#ifdef TARGET_BROWSER
#include "entrypoints.h"
#endif // TARGET_BROWSER

#ifdef TARGET_WASI
#include "wasm/entrypoints.h"
#endif // TARGET_WASI

#ifdef FEATURE_INTERPRETER
#include "interpexec.h"
#endif // FEATURE_INTERPRETER

#include "MiscNativeHelpers.h"

#if !defined(TARGET_WASM) || !GEN_PINVOKE
// Keep entries sorted by name using strcmp ordering for binary search.
static const Entry s_RuntimeHelpers[] =
{
    DllImportEntry(AppContext_SetFirstChanceExceptionHandler)
    DllImportEntry(AppContext_TryGetHostPropertyValue)
    DllImportEntry(AppDomain_CreateDynamicAssembly)
    DllImportEntry(AppendExceptionStackFrame)
#ifdef FEATURE_VARARGS
    DllImportEntry(ArgIterator_GetNextArg)
    DllImportEntry(ArgIterator_GetNextArg2)
    DllImportEntry(ArgIterator_GetNextArgType)
    DllImportEntry(ArgIterator_Init)
    DllImportEntry(ArgIterator_Init2)
#endif // FEATURE_VARARGS
    DllImportEntry(Array_CreateInstance)
    DllImportEntry(Array_Ctor)
    DllImportEntry(Array_GetCorElementTypeOfElementType)
    DllImportEntry(Array_GetElementConstructorEntrypoint)
    DllImportEntry(AssemblyHandle_GetManifestModuleSlow)
    DllImportEntry(AssemblyLoadContext_IsTracingEnabled)
    DllImportEntry(AssemblyName_InitializeAssemblySpec)
    DllImportEntry(AssemblyNative_ApplyUpdate)
    DllImportEntry(AssemblyNative_GetAssemblyCount)
    DllImportEntry(AssemblyNative_GetCodeBase)
    DllImportEntry(AssemblyNative_GetEntryAssembly)
    DllImportEntry(AssemblyNative_GetEntryPoint)
    DllImportEntry(AssemblyNative_GetExecutingAssembly)
    DllImportEntry(AssemblyNative_GetExportedTypes)
    DllImportEntry(AssemblyNative_GetFlags)
    DllImportEntry(AssemblyNative_GetForwardedType)
    DllImportEntry(AssemblyNative_GetFullName)
    DllImportEntry(AssemblyNative_GetHashAlgorithm)
    DllImportEntry(AssemblyNative_GetImageRuntimeVersion)
    DllImportEntry(AssemblyNative_GetLoadContextForAssembly)
    DllImportEntry(AssemblyNative_GetLoadedAssemblies)
    DllImportEntry(AssemblyNative_GetLocale)
    DllImportEntry(AssemblyNative_GetLocation)
    DllImportEntry(AssemblyNative_GetManifestResourceInfo)
    DllImportEntry(AssemblyNative_GetManifestResourceNames)
    DllImportEntry(AssemblyNative_GetModule)
    DllImportEntry(AssemblyNative_GetModules)
    DllImportEntry(AssemblyNative_GetPublicKey)
    DllImportEntry(AssemblyNative_GetReferencedAssemblies)
    DllImportEntry(AssemblyNative_GetResource)
    DllImportEntry(AssemblyNative_GetSimpleName)
    DllImportEntry(AssemblyNative_GetTypeCore)
    DllImportEntry(AssemblyNative_GetTypeCoreIgnoreCase)
    DllImportEntry(AssemblyNative_GetVersion)
    DllImportEntry(AssemblyNative_InitializeAssemblyLoadContext)
    DllImportEntry(AssemblyNative_InternalLoad)
    DllImportEntry(AssemblyNative_InternalTryGetRawMetadata)
    DllImportEntry(AssemblyNative_IsApplyUpdateSupported)
#ifdef TARGET_WINDOWS
    DllImportEntry(AssemblyNative_LoadFromInMemoryModule)
#endif // TARGET_WINDOWS
    DllImportEntry(AssemblyNative_LoadFromPath)
    DllImportEntry(AssemblyNative_LoadFromStream)
    DllImportEntry(AssemblyNative_PrepareForAssemblyLoadContextRelease)
    DllImportEntry(AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceAssemblyResolveHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceResolvingHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceSatelliteSubdirectoryPathProbed)
    DllImportEntry(AsyncHelpers_AddContinuationToExInternal)
#ifdef FEATURE_INTERPRETER
    DllImportEntry(AsyncHelpers_ResumeInterpreterContinuation)
#endif // FEATURE_INTERPRETER
    DllImportEntry(Buffer_BulkMoveWithWriteBarrierInternal)
    DllImportEntry(CallFilterFunclet)
    DllImportEntry(ClrConfig_GetConfigBoolValue)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ComWeakRefToObject)
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_COMWRAPPERS
    DllImportEntry(ComWrappers_AllocateRefCountedHandle)
    DllImportEntry(ComWrappers_GetIReferenceTrackerTargetVftbl)
    DllImportEntry(ComWrappers_GetIUnknownImpl)
    DllImportEntry(ComWrappers_GetTaggedImpl)
    DllImportEntry(ComWrappers_GetUntrackedAddRefRelease)
    DllImportEntry(ComWrappers_RegisterIsRootedCallback)
#endif // FEATURE_COMWRAPPERS
    DllImportEntry(CustomAttribute_ParseAttributeUsageAttribute)
    DllImportEntry(DebugDebugger_Break)
    DllImportEntry(DebugDebugger_CustomNotification)
#if defined(DEBUGGING_SUPPORTED)
    DllImportEntry(DebugDebugger_GetFuncEvalArgumentType)
    DllImportEntry(DebugDebugger_GetFuncEvalMethod)
#endif // DEBUGGING_SUPPORTED
    DllImportEntry(DebugDebugger_IsLoggingHelper)
    DllImportEntry(DebugDebugger_IsManagedDebuggerAttached)
    DllImportEntry(DebugDebugger_Launch)
    DllImportEntry(DebugDebugger_Log)
#if defined(DEBUGGING_SUPPORTED)
    DllImportEntry(DebugDebugger_PrepareFuncEvalResult)
    DllImportEntry(DebugDebugger_ReadFuncEvalPrimitiveRegister)
    DllImportEntry(DebugDebugger_WriteFuncEvalObjectRegister)
    DllImportEntry(DebugDebugger_WriteFuncEvalRegister)
#endif // DEBUGGING_SUPPORTED
    DllImportEntry(Delegate_AdjustTarget)
    DllImportEntry(Delegate_BindToMethodInfo)
    DllImportEntry(Delegate_BindToMethodName)
    DllImportEntry(Delegate_Construct)
    DllImportEntry(Delegate_CreateMethodInfo)
    DllImportEntry(Delegate_GetInvokeMethod)
    DllImportEntry(Delegate_GetMethodDesc)
    DllImportEntry(Delegate_GetMulticastInvoke)
    DllImportEntry(Delegate_GetMulticastInvokeSlow)
    DllImportEntry(Delegate_InitializeVirtualCallStub)
    DllImportEntry(DependentHandle_InternalAlloc)
    DllImportEntry(DependentHandle_InternalAllocWithGCTransition)
    DllImportEntry(DependentHandle_InternalFree)
    DllImportEntry(DependentHandle_InternalFreeWithGCTransition)
    DllImportEntry(DependentHandle_InternalGetDependent)
    DllImportEntry(DependentHandle_InternalGetTargetAndDependent)
    DllImportEntry(DependentHandle_InternalGetTargetWithDebugValidation)
    DllImportEntry(DependentHandle_InternalSetDependent)
    DllImportEntry(DependentHandle_InternalSetTargetToNull)
    DllImportEntry(EHEnumInitFromStackFrameIterator)
    DllImportEntry(EHEnumNext)
    DllImportEntry(Enum_GetValuesAndNames)
    DllImportEntry(Environment_Exit)
    DllImportEntry(Environment_FailFast)
    DllImportEntry(Environment_GetProcessorCount)
    DllImportEntry(Environment_get_ExitCode)
    DllImportEntry(Environment_set_ExitCode)
#ifdef FEATURE_PERFTRACING
    DllImportEntry(EventPipeInternal_CreateProvider)
    DllImportEntry(EventPipeInternal_DefineEvent)
    DllImportEntry(EventPipeInternal_DeleteProvider)
    DllImportEntry(EventPipeInternal_Disable)
    DllImportEntry(EventPipeInternal_Enable)
    DllImportEntry(EventPipeInternal_EventActivityIdControl)
    DllImportEntry(EventPipeInternal_GetNextEvent)
    DllImportEntry(EventPipeInternal_GetProvider)
    DllImportEntry(EventPipeInternal_GetSessionInfo)
    DllImportEntry(EventPipeInternal_SignalSession)
    DllImportEntry(EventPipeInternal_WaitForSessionSignal)
#if defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
    DllImportEntry(EventPipeInternal_WasiRunJobs)
#endif // defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
    DllImportEntry(EventPipeInternal_WriteEventData)
#endif // FEATURE_PERFTRACING
#ifdef FEATURE_EVENTSOURCE_XPLAT
    DllImportEntry(EventSource_GetClrConfig)
#endif // FEATURE_EVENTSOURCE_XPLAT
    DllImportEntry(ExceptionNative_GetFrozenStackTrace)
    DllImportEntry(ExceptionNative_GetMessageFromNativeResources)
    DllImportEntry(ExceptionNative_GetMethodFromStackTrace)
    DllImportEntry(ExceptionNative_ThrowAmbiguousResolutionException)
    DllImportEntry(ExceptionNative_ThrowClassAccessException)
    DllImportEntry(ExceptionNative_ThrowEntryPointNotFoundException)
    DllImportEntry(ExceptionNative_ThrowFieldAccessException)
    DllImportEntry(ExceptionNative_ThrowMethodAccessException)
    DllImportEntry(Exception_GetExceptionCount)
    DllImportEntry(Exception_IsImmutableException)
    DllImportEntry(Exception_PrepareForForeignExceptionRaise)
    DllImportEntry(FileLoadException_GetMessageForHR)
#ifdef TARGET_UNIX
    DllImportEntry(FreeEnvironmentStringsW)
#endif // TARGET_UNIX
    DllImportEntry(GCFrameRegistration_RegisterForGCReporting)
    DllImportEntry(GCFrameRegistration_UnregisterForGCReporting)
    DllImportEntry(GCHandle_InternalAllocWithGCTransition)
    DllImportEntry(GCHandle_InternalCompareExchange)
    DllImportEntry(GCHandle_InternalFreeWithGCTransition)
#ifdef FEATURE_JAVAMARSHAL
    DllImportEntry(GCHandle_InternalGetBridgeWait)
#endif // FEATURE_JAVAMARSHAL
    DllImportEntry(GCHandle_InternalGetWithDebugValidation)
    DllImportEntry(GCHandle_InternalSet)
#ifdef FEATURE_JAVAMARSHAL
    DllImportEntry(GCHandle_InternalTryGetBridgeWait)
#endif // FEATURE_JAVAMARSHAL
    DllImportEntry(GCHandle__InternalAlloc)
    DllImportEntry(GCHandle__InternalFree)
    DllImportEntry(GCInterface_AllocateNewArray)
    DllImportEntry(GCInterface_Collect)
    DllImportEntry(GCInterface_EnableNoGCRegionCallback)
    DllImportEntry(GCInterface_EndNoGCRegion)
    DllImportEntry(GCInterface_EnumerateConfigurationValues)
    DllImportEntry(GCInterface_GetGenerationBudget)
    DllImportEntry(GCInterface_GetNextFinalizableObject)
    DllImportEntry(GCInterface_GetTotalAllocatedBytesPrecise)
    DllImportEntry(GCInterface_GetTotalMemory)
    DllImportEntry(GCInterface_ReRegisterForFinalize)
    DllImportEntry(GCInterface_RefreshMemoryLimit)
    DllImportEntry(GCInterface_RegisterFrozenSegment)
    DllImportEntry(GCInterface_StartNoGCRegion)
    DllImportEntry(GCInterface_UnregisterFrozenSegment)
    DllImportEntry(GCInterface_WaitForFullGCApproach)
    DllImportEntry(GCInterface_WaitForFullGCComplete)
    DllImportEntry(GCInterface_WaitForPendingFinalizers)
    DllImportEntry(GCSettings_GetGCLatencyMode)
    DllImportEntry(GCSettings_GetLOHCompactionMode)
    DllImportEntry(GCSettings_SetGCLatencyMode)
    DllImportEntry(GCSettings_SetLOHCompactionMode)
    DllImportEntry(GCSettings_get_IsServerGC)
    DllImportEntry(GC_GetAllocatedBytesForCurrentThread)
    DllImportEntry(GC_GetCurrentObjSize)
    DllImportEntry(GC_GetGCNow)
    DllImportEntry(GC_GetGenerationInternal)
    DllImportEntry(GC_GetGenerationSize)
    DllImportEntry(GC_GetLastGCDuration)
    DllImportEntry(GC_GetLastGCPercentTimeInGC)
    DllImportEntry(GC_GetLastGCStartTime)
    DllImportEntry(GC_GetMaxGeneration)
    DllImportEntry(GC_GetMemoryInfo)
    DllImportEntry(GC_GetSegmentSize)
    DllImportEntry(GC_GetTotalAllocatedBytesApproximate)
    DllImportEntry(GC_SuppressFinalizeInternal)
    DllImportEntry(GC__CancelFullGCNotification)
    DllImportEntry(GC__CollectionCount)
    DllImportEntry(GC__GetTotalPauseDuration)
    DllImportEntry(GC__RegisterForFullGCNotification)
    DllImportEntry(GenericHandleWorker)
    DllImportEntry(GetCurrentExInfo)
#ifdef TARGET_UNIX
    DllImportEntry(GetEnvironmentStringsW)
    DllImportEntry(GetEnvironmentVariableW)
#endif // TARGET_UNIX
    DllImportEntry(GetFileLoadExceptionMessage)
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(GetThreadStaticsByIndex)
    DllImportEntry(GetThreadStaticsByMethodTable)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(GetTypeLoadExceptionMessage)
    DllImportEntry(InitClassHelper)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(InterfaceMarshaler_ConvertToManaged)
    DllImportEntry(InterfaceMarshaler_ConvertToNative)
    DllImportEntry(InterfaceMarshaler_GetObjectForComCallableWrapperIUnknown)
    DllImportEntry(InterfaceMarshaler_ValidateComVisibilityForIUnknown)
#endif // FEATURE_COMINTEROP
    DllImportEntry(Interlocked_CompareExchange32)
    { "Interlocked_CompareExchange32Pointer", (void*)&Interlocked_CompareExchange32 },
    DllImportEntry(Interlocked_CompareExchange64)
    DllImportEntry(Interlocked_CompareExchangeObject)
    DllImportEntry(Interlocked_Exchange32)
    DllImportEntry(Interlocked_Exchange64)
    DllImportEntry(Interlocked_ExchangeAdd32)
    DllImportEntry(Interlocked_ExchangeAdd64)
    DllImportEntry(Interlocked_ExchangeObject)
    DllImportEntry(Interlocked_MemoryBarrierProcessWide)
#ifdef FEATURE_EVENTSOURCE_XPLAT
    DllImportEntry(IsEventSourceLoggingEnabled)
#endif // FEATURE_EVENTSOURCE_XPLAT
    DllImportEntry(IsInstanceOf_NoCacheLookup)
#ifdef FEATURE_JAVAMARSHAL
    DllImportEntry(JavaMarshal_CreateReferenceTrackingHandle)
    DllImportEntry(JavaMarshal_FinishCrossReferenceProcessing)
    DllImportEntry(JavaMarshal_GetContext)
    DllImportEntry(JavaMarshal_Initialize)
#endif // FEATURE_JAVAMARSHAL
    DllImportEntry(JitInfo_GetCompilationTimeInTicks)
    DllImportEntry(JitInfo_GetCompiledILBytes)
    DllImportEntry(JitInfo_GetCompiledMethodCount)
    DllImportEntry(LoaderAllocator_Destroy)
#ifdef FEATURE_EVENTSOURCE_XPLAT
    DllImportEntry(LogEventSource)
#endif // FEATURE_EVENTSOURCE_XPLAT
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_ChangeWrapperHandleStrength)
    DllImportEntry(MarshalNative_CleanupUnusedObjectsInCurrentContext)
    DllImportEntry(MarshalNative_CreateAggregatedObject)
    DllImportEntry(MarshalNative_FinalReleaseComObject)
    DllImportEntry(MarshalNative_GetComInterfaceForObject)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetDelegateForFunctionPointerInternal)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetEndComSlot)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetExceptionForHR)
    DllImportEntry(MarshalNative_GetFunctionPointerForDelegateInternal)
    DllImportEntry(MarshalNative_GetHINSTANCE)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetHRForException)
    DllImportEntry(MarshalNative_GetIDispatchForObject)
    DllImportEntry(MarshalNative_GetIUnknownForObject)
    DllImportEntry(MarshalNative_GetIUnknownOrIDispatchForObject)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetIsInCooperativeGCModeFunctionPointer)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetNativeVariantForObject)
    DllImportEntry(MarshalNative_GetObjectForIUnknown)
    DllImportEntry(MarshalNative_GetObjectForNativeVariant)
    DllImportEntry(MarshalNative_GetObjectsForNativeVariants)
    DllImportEntry(MarshalNative_GetStartComSlot)
    DllImportEntry(MarshalNative_GetTypeFromCLSID)
    DllImportEntry(MarshalNative_GetTypedObjectForIUnknown)
    DllImportEntry(MarshalNative_GetUniqueObjectForIUnknown)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_HasLayout)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_InternalCreateWrapperOfType)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_IsBuiltInComSupported)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_IsTypeVisibleFromCom)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_OffsetOf)
    DllImportEntry(MarshalNative_Prelink)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_ReleaseComObject)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_SizeOfHelper)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(Marshal_AreComObjectsAvailableForCleanup)
#endif // FEATURE_COMINTEROP
    DllImportEntry(Marshal_GetExceptionCode)
    DllImportEntry(Marshal_GetExceptionPointers)
    DllImportEntry(Marshal_GetLastPInvokeError)
    DllImportEntry(Marshal_SetLastPInvokeError)
    DllImportEntry(MathF_Acos)
    DllImportEntry(MathF_Acosh)
    DllImportEntry(MathF_Asin)
    DllImportEntry(MathF_Asinh)
    DllImportEntry(MathF_Atan)
    DllImportEntry(MathF_Atan2)
    DllImportEntry(MathF_Atanh)
    DllImportEntry(MathF_Cbrt)
    DllImportEntry(MathF_Ceiling)
    DllImportEntry(MathF_Cos)
    DllImportEntry(MathF_Cosh)
    DllImportEntry(MathF_Exp)
    DllImportEntry(MathF_Floor)
    DllImportEntry(MathF_FusedMultiplyAdd)
    DllImportEntry(MathF_Log)
    DllImportEntry(MathF_Log10)
    DllImportEntry(MathF_Log2)
    DllImportEntry(MathF_ModF)
    DllImportEntry(MathF_Pow)
    DllImportEntry(MathF_Sin)
    DllImportEntry(MathF_SinCos)
    DllImportEntry(MathF_Sinh)
    DllImportEntry(MathF_Sqrt)
    DllImportEntry(MathF_Tan)
    DllImportEntry(MathF_Tanh)
    DllImportEntry(Math_Acos)
    DllImportEntry(Math_Acosh)
    DllImportEntry(Math_Asin)
    DllImportEntry(Math_Asinh)
    DllImportEntry(Math_Atan)
    DllImportEntry(Math_Atan2)
    DllImportEntry(Math_Atanh)
    DllImportEntry(Math_Cbrt)
    DllImportEntry(Math_Ceiling)
    DllImportEntry(Math_Cos)
    DllImportEntry(Math_Cosh)
#ifdef TARGET_32BIT
    DllImportEntry(Math_DivInt32Internal)
    DllImportEntry(Math_DivInt64Internal)
    DllImportEntry(Math_DivUInt32Internal)
    DllImportEntry(Math_DivUInt64Internal)
#endif // TARGET_32BIT
    DllImportEntry(Math_Exp)
    DllImportEntry(Math_Floor)
    DllImportEntry(Math_FusedMultiplyAdd)
    DllImportEntry(Math_Log)
    DllImportEntry(Math_Log10)
    DllImportEntry(Math_Log2)
    DllImportEntry(Math_ModF)
#ifdef TARGET_32BIT
    DllImportEntry(Math_ModInt32Internal)
    DllImportEntry(Math_ModInt64Internal)
    DllImportEntry(Math_ModUInt32Internal)
    DllImportEntry(Math_ModUInt64Internal)
#endif // TARGET_32BIT
    DllImportEntry(Math_Pow)
    DllImportEntry(Math_Sin)
    DllImportEntry(Math_SinCos)
    DllImportEntry(Math_Sinh)
    DllImportEntry(Math_Sqrt)
    DllImportEntry(Math_Tan)
    DllImportEntry(Math_Tanh)
    DllImportEntry(MdUtf8String_EqualsCaseInsensitive)
    DllImportEntry(MetadataImport_Enum)
    DllImportEntry(MetadataImport_GetClassLayout)
    DllImportEntry(MetadataImport_GetCustomAttributeProps)
    DllImportEntry(MetadataImport_GetDefaultValue)
    DllImportEntry(MetadataImport_GetEventProps)
    DllImportEntry(MetadataImport_GetFieldDefProps)
    DllImportEntry(MetadataImport_GetFieldMarshal)
    DllImportEntry(MetadataImport_GetFieldOffset)
    DllImportEntry(MetadataImport_GetGenericParamProps)
    DllImportEntry(MetadataImport_GetMarshalAs)
    DllImportEntry(MetadataImport_GetMemberRefProps)
    DllImportEntry(MetadataImport_GetMetadataImport)
    DllImportEntry(MetadataImport_GetName)
    DllImportEntry(MetadataImport_GetNamespace)
    DllImportEntry(MetadataImport_GetPInvokeMap)
    DllImportEntry(MetadataImport_GetParamDefProps)
    DllImportEntry(MetadataImport_GetParentToken)
    DllImportEntry(MetadataImport_GetPropertyProps)
    DllImportEntry(MetadataImport_GetScopeProps)
    DllImportEntry(MetadataImport_GetSigOfFieldDef)
    DllImportEntry(MetadataImport_GetSigOfMethodDef)
    DllImportEntry(MetadataImport_GetSignatureFromToken)
    DllImportEntry(MetadataImport_GetUserString)
    DllImportEntry(MetadataImport_IsValidToken)
    DllImportEntry(MethodBase_GetCurrentMethod)
    DllImportEntry(MethodTable_AreTypesEquivalent)
    DllImportEntry(MethodTable_CanCompareBitsOrUseFastGetHashCode)
    DllImportEntry(MethodTable_GetLoaderAllocatorHandle)
    DllImportEntry(MethodTable_GetMethodTableMatchingParentClass)
    DllImportEntry(MethodTable_GetNumInstanceFieldBytes)
    DllImportEntry(MethodTable_GetPrimitiveCorElementType)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MngdSafeArrayMarshaler_ClearNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToNative)
    DllImportEntry(MngdSafeArrayMarshaler_CreateMarshaler)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ModuleBuilder_GetArrayMethodToken)
    DllImportEntry(ModuleBuilder_GetMemberRef)
    DllImportEntry(ModuleBuilder_GetMemberRefFromSignature)
    DllImportEntry(ModuleBuilder_GetMemberRefOfFieldInfo)
    DllImportEntry(ModuleBuilder_GetMemberRefOfMethodInfo)
    DllImportEntry(ModuleBuilder_GetStringConstant)
    DllImportEntry(ModuleBuilder_GetTokenFromTypeSpec)
    DllImportEntry(ModuleBuilder_GetTypeRef)
    DllImportEntry(ModuleBuilder_SetFieldRVAContent)
    DllImportEntry(ModuleHandle_GetDynamicMethod)
    DllImportEntry(ModuleHandle_GetMDStreamVersion)
    DllImportEntry(ModuleHandle_GetModuleType)
    DllImportEntry(ModuleHandle_GetPEKind)
    DllImportEntry(ModuleHandle_GetToken)
    DllImportEntry(ModuleHandle_ResolveField)
    DllImportEntry(ModuleHandle_ResolveMethod)
    DllImportEntry(ModuleHandle_ResolveType)
#ifdef FEATURE_MULTICOREJIT
    DllImportEntry(MultiCoreJIT_InternalSetProfileRoot)
    DllImportEntry(MultiCoreJIT_InternalStartProfile)
#endif // FEATURE_MULTICOREJIT
    DllImportEntry(NativeLibrary_LoadByName)
#ifdef FEATURE_PERFTRACING
    DllImportEntry(NativeRuntimeEventSource_LogContentionLockCreated)
    DllImportEntry(NativeRuntimeEventSource_LogContentionStart)
    DllImportEntry(NativeRuntimeEventSource_LogContentionStop)
    DllImportEntry(NativeRuntimeEventSource_LogDecreaseMemoryPressure)
    DllImportEntry(NativeRuntimeEventSource_LogIncreaseMemoryPressure)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIODequeue)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIOEnqueue)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIOPack)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolMinMaxThreads)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount)
    DllImportEntry(NativeRuntimeEventSource_LogWaitHandleWaitStart)
    DllImportEntry(NativeRuntimeEventSource_LogWaitHandleWaitStop)
#endif // FEATURE_PERFTRACING
#ifdef FEATURE_OBJCMARSHAL
    DllImportEntry(ObjCMarshal_AllocateReferenceTrackingHandle)
    DllImportEntry(ObjCMarshal_TryInitializeReferenceTracker)
    DllImportEntry(ObjCMarshal_TrySetGlobalMessageSendCallback)
#endif // FEATURE_OBJCMARSHAL
    DllImportEntry(ObjectHeader_GetLockHandleIfExists)
    DllImportEntry(ObjectHeader_GetOrCreateLockObject)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ObjectMarshaler_ConvertToManaged)
    DllImportEntry(ObjectMarshaler_ConvertToNative)
    DllImportEntry(ObjectMarshaler_ConvertToNativeVariantArrayElement)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ObjectNative_AllocateUninitializedClone)
    DllImportEntry(ObjectNative_GetHashCodeSlow)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ObjectToComWeakRef)
#endif // FEATURE_COMINTEROP
#ifdef TARGET_UNIX
    DllImportEntry(OutputDebugStringW)
#endif // TARGET_UNIX
    DllImportEntry(QCall_FreeGCHandleForTypeHandle)
    DllImportEntry(QCall_GetGCHandleForTypeHandle)
    DllImportEntry(ReflectionInvocation_CompileMethod)
    DllImportEntry(ReflectionInvocation_GetBoxInfo)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_GetComObjectGuid)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_GetGuid)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_InvokeDispMethod)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_PrepareDelegate)
    DllImportEntry(ReflectionInvocation_PrepareMethod)
    DllImportEntry(ReflectionInvocation_RunClassConstructor)
    DllImportEntry(ReflectionInvocation_RunModuleConstructor)
    DllImportEntry(ReflectionInvocation_SizeOf)
    DllImportEntry(ReflectionSerialization_GetCreateUninitializedObjectInfo)
    DllImportEntry(ResolveVirtualFunctionPointer)
    DllImportEntry(RuntimeAssembly_GetIsCollectible)
    DllImportEntry(RuntimeAssembly_GetIsDynamic)
    DllImportEntry(RuntimeAssembly_GetManifestModule)
    DllImportEntry(RuntimeAssembly_GetTokenInternal)
    DllImportEntry(RuntimeFieldHandle_AcquiresContextFromThis)
    DllImportEntry(RuntimeFieldHandle_GetApproxDeclaringMethodTable)
    DllImportEntry(RuntimeFieldHandle_GetAttributes)
    DllImportEntry(RuntimeFieldHandle_GetEnCFieldAddr)
    DllImportEntry(RuntimeFieldHandle_GetFieldDataReference)
    DllImportEntry(RuntimeFieldHandle_GetInstanceFieldOffset)
    DllImportEntry(RuntimeFieldHandle_GetLoaderAllocatorInternal)
    DllImportEntry(RuntimeFieldHandle_GetRVAFieldInfo)
    DllImportEntry(RuntimeFieldHandle_GetStaticFieldAddress)
    DllImportEntry(RuntimeFieldHandle_GetStaticFieldForGenericType)
    DllImportEntry(RuntimeFieldHandle_GetToken)
    DllImportEntry(RuntimeFieldHandle_GetUtf8NameInternal)
    DllImportEntry(RuntimeFieldHandle_GetValue)
    DllImportEntry(RuntimeFieldHandle_GetValueDirect)
    DllImportEntry(RuntimeFieldHandle_IsFastPathSupported)
    DllImportEntry(RuntimeFieldHandle_SetValue)
    DllImportEntry(RuntimeFieldHandle_SetValueDirect)
    DllImportEntry(RuntimeHelpers_ContentEquals)
    DllImportEntry(RuntimeHelpers_GetTailCallArgBuffer)
    DllImportEntry(RuntimeHelpers_GetTailCallInfo)
    DllImportEntry(RuntimeHelpers_TryEnsureSufficientExecutionStack)
    DllImportEntry(RuntimeHelpers_TryGetHashCode)
    DllImportEntry(RuntimeMethodHandle_ConstructInstantiation)
    DllImportEntry(RuntimeMethodHandle_Destroy)
    DllImportEntry(RuntimeMethodHandle_GetAttributes)
    DllImportEntry(RuntimeMethodHandle_GetDiagnosticCodeStart)
    DllImportEntry(RuntimeMethodHandle_GetFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_GetGenericParameterCount)
    DllImportEntry(RuntimeMethodHandle_GetImplAttributes)
    DllImportEntry(RuntimeMethodHandle_GetLoaderAllocatorInternal)
    DllImportEntry(RuntimeMethodHandle_GetMethodBody)
    DllImportEntry(RuntimeMethodHandle_GetMethodDef)
    DllImportEntry(RuntimeMethodHandle_GetMethodFromCanonical)
    DllImportEntry(RuntimeMethodHandle_GetMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetMethodTable)
    DllImportEntry(RuntimeMethodHandle_GetResolver)
    DllImportEntry(RuntimeMethodHandle_GetSlot)
    DllImportEntry(RuntimeMethodHandle_GetStubIfNeededInternal)
    DllImportEntry(RuntimeMethodHandle_GetStubIfNeededSlow)
    DllImportEntry(RuntimeMethodHandle_GetTypicalMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_GetUtf8NameInternal)
    DllImportEntry(RuntimeMethodHandle_GetVirtualFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_HasMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_IsAsyncMethod)
    DllImportEntry(RuntimeMethodHandle_IsCAVisibleFromDecoratedType)
    DllImportEntry(RuntimeMethodHandle_IsCollectible)
    DllImportEntry(RuntimeMethodHandle_IsConstructor)
    DllImportEntry(RuntimeMethodHandle_IsDynamicMethod)
    DllImportEntry(RuntimeMethodHandle_IsGenericMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_IsTypicalMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_StripMethodInstantiation)
    DllImportEntry(RuntimeModule_GetFullyQualifiedName)
    DllImportEntry(RuntimeModule_GetScopeName)
    DllImportEntry(RuntimeModule_GetTypes)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(RuntimeTypeHandle_AllocateComObject)
#endif // FEATURE_COMINTEROP
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemory)
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemoryAligned)
    DllImportEntry(RuntimeTypeHandle_CompareCanonicalHandles)
    DllImportEntry(RuntimeTypeHandle_ConstructName)
    DllImportEntry(RuntimeTypeHandle_ContainsGenericVariables)
    DllImportEntry(RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter)
    DllImportEntry(RuntimeTypeHandle_GetActivationInfo)
    DllImportEntry(RuntimeTypeHandle_GetArgumentTypesFromFunctionPointer)
    DllImportEntry(RuntimeTypeHandle_GetArrayRank)
    DllImportEntry(RuntimeTypeHandle_GetAssemblyIfExists)
    DllImportEntry(RuntimeTypeHandle_GetAssemblySlow)
    DllImportEntry(RuntimeTypeHandle_GetAttributes)
    DllImportEntry(RuntimeTypeHandle_GetConstraints)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringMethodForGenericParameter)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringTypeHandle)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringTypeHandleForGenericVariable)
    DllImportEntry(RuntimeTypeHandle_GetElementTypeHandle)
    DllImportEntry(RuntimeTypeHandle_GetFields)
    DllImportEntry(RuntimeTypeHandle_GetFirstIntroducedMethod)
    DllImportEntry(RuntimeTypeHandle_GetGenericTypeDefinition)
    DllImportEntry(RuntimeTypeHandle_GetGenericVariableIndex)
    DllImportEntry(RuntimeTypeHandle_GetInstantiation)
    DllImportEntry(RuntimeTypeHandle_GetInterfaceMethodImplementation)
    DllImportEntry(RuntimeTypeHandle_GetInterfaces)
    DllImportEntry(RuntimeTypeHandle_GetMethodAt)
    DllImportEntry(RuntimeTypeHandle_GetModuleIfExists)
    DllImportEntry(RuntimeTypeHandle_GetModuleSlow)
    DllImportEntry(RuntimeTypeHandle_GetNextIntroducedMethod)
    DllImportEntry(RuntimeTypeHandle_GetNumVirtuals)
    DllImportEntry(RuntimeTypeHandle_GetNumVirtualsAndStaticVirtuals)
    DllImportEntry(RuntimeTypeHandle_GetRuntimeTypeFromHandleSlow)
    DllImportEntry(RuntimeTypeHandle_GetToken)
    DllImportEntry(RuntimeTypeHandle_GetUtf8NameInternal)
    DllImportEntry(RuntimeTypeHandle_Instantiate)
    DllImportEntry(RuntimeTypeHandle_InternalAlloc)
    DllImportEntry(RuntimeTypeHandle_InternalAllocNoChecks)
    DllImportEntry(RuntimeTypeHandle_InternalAllocNoChecks_FastPath)
#ifdef FEATURE_TYPEEQUIVALENCE
    DllImportEntry(RuntimeTypeHandle_IsEquivalentTo)
#endif // FEATURE_TYPEEQUIVALENCE
    DllImportEntry(RuntimeTypeHandle_IsGenericVariable)
    DllImportEntry(RuntimeTypeHandle_IsUnmanagedFunctionPointer)
    DllImportEntry(RuntimeTypeHandle_IsVisible)
    DllImportEntry(RuntimeTypeHandle_MakeArray)
    DllImportEntry(RuntimeTypeHandle_MakeByRef)
    DllImportEntry(RuntimeTypeHandle_MakeFunctionPointer)
    DllImportEntry(RuntimeTypeHandle_MakePointer)
    DllImportEntry(RuntimeTypeHandle_MakeSZArray)
    DllImportEntry(RuntimeTypeHandle_RegisterCollectibleTypeDependency)
    DllImportEntry(RuntimeTypeHandle_SatisfiesConstraints)
    DllImportEntry(RuntimeTypeHandle_VerifyInterfaceIsImplemented)
#ifdef TARGET_UNIX
    DllImportEntry(SetEnvironmentVariableW)
#endif // TARGET_UNIX
    DllImportEntry(SfiInit)
    DllImportEntry(SfiNext)
    DllImportEntry(Signature_AreEqual)
    DllImportEntry(Signature_GetCallingConventionFromFunctionPointerAtOffsetInternal)
    DllImportEntry(Signature_GetCustomModifiersAtOffset)
    DllImportEntry(Signature_GetParameterOffsetInternal)
    DllImportEntry(Signature_GetTypeParameterOffsetInternal)
    DllImportEntry(Signature_Init)
    DllImportEntry(StackFrame_GetMethodDescFromNativeIP)
    DllImportEntry(StackTrace_GetStackFramesInternal)
    DllImportEntry(Stream_HasOverriddenSlow)
    DllImportEntry(String_Intern)
    DllImportEntry(String_IsInterned)
#ifdef FEATURE_VARARGS
    DllImportEntry(StubHelpers_CalcVaListSize)
#endif // FEATURE_VARARGS
    DllImportEntry(StubHelpers_ClearLastError)
    DllImportEntry(StubHelpers_CreateCustomMarshaler)
    DllImportEntry(StubHelpers_CreateLayoutClassMarshalStubs)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(StubHelpers_GetCOMIPFromRCW)
    DllImportEntry(StubHelpers_GetCOMIPFromRCWSlow)
#endif // FEATURE_COMINTEROP
    DllImportEntry(StubHelpers_LogPinnedArgument)
#ifdef FEATURE_VARARGS
    DllImportEntry(StubHelpers_MarshalToManagedVaList)
    DllImportEntry(StubHelpers_MarshalToUnmanagedVaList)
#endif // FEATURE_VARARGS
    DllImportEntry(StubHelpers_MulticastDebuggerTraceHelper)
#ifdef PROFILING_SUPPORTED
    DllImportEntry(StubHelpers_ProfilerBeginTransitionCallback)
    DllImportEntry(StubHelpers_ProfilerEndTransitionCallback)
#endif // PROFILING_SUPPORTED
    DllImportEntry(StubHelpers_SetLastError)
    DllImportEntry(StubHelpers_ThrowInteropException)
    DllImportEntry(StubHelpers_ThrowInteropParamException)
    DllImportEntry(StubHelpers_ValidateByref)
    DllImportEntry(StubHelpers_ValidateObject)
#ifdef TARGET_BROWSER
    DllImportEntry(SystemJS_MarkAsyncMain)
    DllImportEntry(SystemJS_RejectMainPromise)
    DllImportEntry(SystemJS_ResolveMainPromise)
    DllImportEntry(SystemJS_ScheduleBackgroundJob)
    DllImportEntry(SystemJS_ScheduleTimer)
#endif // TARGET_BROWSER
    DllImportEntry(TailCallHelp_AllocTailCallArgBufferInternal)
    DllImportEntry(ThreadNative_Abort)
#ifdef TARGET_WINDOWS
    DllImportEntry(ThreadNative_CheckForPendingInterrupt)
#endif // TARGET_WINDOWS
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ThreadNative_DisableComObjectEagerCleanup)
#endif // FEATURE_COMINTEROP
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    DllImportEntry(ThreadNative_GetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_GetCurrentThread)
    DllImportEntry(ThreadNative_GetIsBackground)
#endif // FEATURE_MULTITHREADING
#ifdef TARGET_WINDOWS
    DllImportEntry(ThreadNative_GetOSHandle)
#endif // TARGET_WINDOWS
    DllImportEntry(ThreadNative_GetQCallSpecialException)
    DllImportEntry(ThreadNative_GetThreadState)
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_InformThreadNameChange)
    DllImportEntry(ThreadNative_Initialize)
#endif // FEATURE_MULTITHREADING
#ifdef TARGET_WINDOWS
    DllImportEntry(ThreadNative_Interrupt)
#endif // TARGET_WINDOWS
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_PollGC)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_ReentrantWaitAny)
    DllImportEntry(ThreadNative_ResetAbort)
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    DllImportEntry(ThreadNative_SetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_SetIsBackground)
    DllImportEntry(ThreadNative_SetPriority)
    DllImportEntry(ThreadNative_Start)
    DllImportEntry(Thread_CatchAtSafePoint)
    DllImportEntry(Thread_CurrentThreadIsFinalizerThread)
    DllImportEntry(Thread_GetOptimalMaxSpinWaitsPerSpinIteration)
    DllImportEntry(Thread_InternalFinalize)
    DllImportEntry(Thread_SpinWait)
    DllImportEntry(Thread_Yield)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(ThrowInvalidCastException)
#ifdef FEATURE_COMWRAPPERS
    DllImportEntry(TrackerObjectManager_HasReferenceTrackerManager)
    DllImportEntry(TrackerObjectManager_IsGlobalPeggingEnabled)
    DllImportEntry(TrackerObjectManager_RegisterNativeObjectWrapperCache)
    DllImportEntry(TrackerObjectManager_TryRegisterReferenceTrackerManager)
#endif // FEATURE_COMWRAPPERS
    DllImportEntry(TypeBuilder_AddInterfaceImpl)
    DllImportEntry(TypeBuilder_DefineCustomAttribute)
    DllImportEntry(TypeBuilder_DefineEvent)
    DllImportEntry(TypeBuilder_DefineField)
    DllImportEntry(TypeBuilder_DefineGenericParam)
    DllImportEntry(TypeBuilder_DefineMethod)
    DllImportEntry(TypeBuilder_DefineMethodImpl)
    DllImportEntry(TypeBuilder_DefineMethodSemantics)
    DllImportEntry(TypeBuilder_DefineMethodSpec)
    DllImportEntry(TypeBuilder_DefineProperty)
    DllImportEntry(TypeBuilder_DefineType)
    DllImportEntry(TypeBuilder_GetTokenFromSig)
    DllImportEntry(TypeBuilder_SetClassLayout)
    DllImportEntry(TypeBuilder_SetConstantValue)
    DllImportEntry(TypeBuilder_SetFieldLayoutOffset)
    DllImportEntry(TypeBuilder_SetMethodIL)
    DllImportEntry(TypeBuilder_SetMethodImpl)
    DllImportEntry(TypeBuilder_SetPInvokeData)
    DllImportEntry(TypeBuilder_SetParamInfo)
    DllImportEntry(TypeBuilder_SetParentType)
    DllImportEntry(TypeBuilder_TermCreateClass)
    DllImportEntry(TypeHandle_CanCastTo_NoCacheLookup)
    DllImportEntry(TypeHandle_GetCorElementType)
    DllImportEntry(TypeMapLazyDictionary_FindPrecachedExternalTypeMapEntry)
    DllImportEntry(TypeMapLazyDictionary_FindPrecachedProxyTypeMapEntry)
    DllImportEntry(TypeMapLazyDictionary_ProcessAttributes)
    DllImportEntry(UnsafeAccessors_ResolveGenericParamToTypeHandle)
    DllImportEntry(ValueType_GetHashCodeStrategy)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(Variant_ConvertValueTypeToRecord)
#endif // FEATURE_COMINTEROP
    DllImportEntry(VersionResilientHashCode_TypeHashCode)
#ifdef TARGET_WASI
    DllImportEntry(WasiFinalizer_RunWorker)
    DllImportEntry(WasiFinalizer_TryClearPending)
#endif // TARGET_WASI
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    DllImportEntry(X86Base_CpuId)
#endif // defined(TARGET_X86) || defined(TARGET_AMD64)
    DllImportEntry(memmove)
#ifdef TARGET_SUNOS
    // Work around illumos.org/issues/17832
    {"memset", (void*)&std::memset},
#else // TARGET_SUNOS
    DllImportEntry(memset)
#endif // TARGET_SUNOS
};

class RuntimeHelperSearch final : public CBinarySearch<Entry>
{
public:
    RuntimeHelperSearch(const Entry* entries, int count) : CBinarySearch<Entry>(entries, count)
    {
        LIMITED_METHOD_CONTRACT;
    }

    int Compare(const Entry* first, const Entry* second) override
    {
        LIMITED_METHOD_CONTRACT;
        return strcmp(first->name, second->name);
    }
};

const void* ResolveRuntimeHelperEntrypoint(const char* name)
{
    RuntimeHelperSearch search(s_RuntimeHelpers, ARRAY_SIZE(s_RuntimeHelpers));
    const Entry key = { name, nullptr };
    const Entry* entry = search.Find(&key);
    return entry != nullptr ? entry->method : nullptr;
}
#endif // !defined(TARGET_WASM) || !GEN_PINVOKE
