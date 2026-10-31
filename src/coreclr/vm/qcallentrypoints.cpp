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

#ifdef TARGET_BROWSER
extern "C" void* QCall_memmove(void* dest, const void* src, size_t len);
extern "C" void* QCall_memset(void* dest, int value, size_t len);
#endif // TARGET_BROWSER

#define FCallEntry(name, impl) { #name, (const void*)&impl },
static const Entry s_QCall[] =
{
#ifdef FEATURE_VARARGS
    DllImportEntry(ArgIterator_Init)
    DllImportEntry(ArgIterator_Init2)
    DllImportEntry(ArgIterator_GetNextArgType)
    DllImportEntry(ArgIterator_GetNextArg)
    DllImportEntry(ArgIterator_GetNextArg2)
#endif // FEATURE_VARARGS
    DllImportEntry(CustomAttribute_ParseAttributeUsageAttribute)
    DllImportEntry(Enum_GetValuesAndNames)
    DllImportEntry(DebugDebugger_Break)
    DllImportEntry(DebugDebugger_Launch)
    DllImportEntry(DebugDebugger_Log)
    DllImportEntry(DebugDebugger_CustomNotification)
    DllImportEntry(DebugDebugger_IsLoggingHelper)
    DllImportEntry(DebugDebugger_IsManagedDebuggerAttached)
    DllImportEntry(Delegate_BindToMethodName)
    DllImportEntry(Delegate_BindToMethodInfo)
    DllImportEntry(Delegate_InitializeVirtualCallStub)
    DllImportEntry(Delegate_GetMulticastInvokeSlow)
    DllImportEntry(Delegate_AdjustTarget)
    DllImportEntry(Delegate_Construct)
    DllImportEntry(Delegate_CreateMethodInfo)
    DllImportEntry(Delegate_GetMethodDesc)
    DllImportEntry(Environment_Exit)
    DllImportEntry(Environment_FailFast)
    DllImportEntry(Environment_GetProcessorCount)
    DllImportEntry(ExceptionNative_GetFrozenStackTrace)
    DllImportEntry(ExceptionNative_GetMessageFromNativeResources)
    DllImportEntry(ExceptionNative_GetMethodFromStackTrace)
    DllImportEntry(ExceptionNative_ThrowAmbiguousResolutionException)
    DllImportEntry(ExceptionNative_ThrowEntryPointNotFoundException)
    DllImportEntry(ExceptionNative_ThrowMethodAccessException)
    DllImportEntry(ExceptionNative_ThrowFieldAccessException)
    DllImportEntry(ExceptionNative_ThrowClassAccessException)
    DllImportEntry(QCall_GetGCHandleForTypeHandle)
    DllImportEntry(QCall_FreeGCHandleForTypeHandle)
    DllImportEntry(MethodTable_AreTypesEquivalent)
    DllImportEntry(MethodTable_CanCompareBitsOrUseFastGetHashCode)
    DllImportEntry(TypeHandle_CanCastTo_NoCacheLookup)
    DllImportEntry(TypeHandle_GetCorElementType)
    DllImportEntry(ValueType_GetHashCodeStrategy)
    DllImportEntry(Stream_HasOverriddenSlow)
    DllImportEntry(RuntimeTypeHandle_MakePointer)
    DllImportEntry(RuntimeTypeHandle_MakeByRef)
    DllImportEntry(RuntimeTypeHandle_MakeSZArray)
    DllImportEntry(RuntimeTypeHandle_MakeArray)
    DllImportEntry(RuntimeTypeHandle_MakeFunctionPointer)
    DllImportEntry(RuntimeTypeHandle_GetConstraints)
    DllImportEntry(RuntimeTypeHandle_GetArgumentTypesFromFunctionPointer)
    DllImportEntry(RuntimeTypeHandle_GetAssemblySlow)
    DllImportEntry(RuntimeTypeHandle_GetModuleSlow)
    DllImportEntry(RuntimeTypeHandle_GetNumVirtualsAndStaticVirtuals)
    DllImportEntry(RuntimeTypeHandle_GetMethodAt)
    DllImportEntry(RuntimeTypeHandle_GetFields)
    DllImportEntry(RuntimeTypeHandle_VerifyInterfaceIsImplemented)
    DllImportEntry(RuntimeTypeHandle_GetInterfaceMethodImplementation)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringMethodForGenericParameter)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringTypeHandleForGenericVariable)
    DllImportEntry(RuntimeTypeHandle_GetDeclaringTypeHandle)
    DllImportEntry(RuntimeTypeHandle_IsVisible)
    DllImportEntry(RuntimeTypeHandle_ConstructName)
    DllImportEntry(RuntimeTypeHandle_GetInterfaces)
    DllImportEntry(RuntimeTypeHandle_SatisfiesConstraints)
    DllImportEntry(RuntimeTypeHandle_GetInstantiation)
    DllImportEntry(RuntimeTypeHandle_Instantiate)
    DllImportEntry(RuntimeTypeHandle_GetGenericTypeDefinition)
    DllImportEntry(RuntimeTypeHandle_GetActivationInfo)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(RuntimeTypeHandle_AllocateComObject)
#endif // FEATURE_COMINTEROP
    DllImportEntry(RuntimeTypeHandle_GetRuntimeTypeFromHandleSlow)
#ifdef FEATURE_TYPEEQUIVALENCE
    DllImportEntry(RuntimeTypeHandle_IsEquivalentTo)
#endif // FEATURE_TYPEEQUIVALENCE
    DllImportEntry(RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter)
    DllImportEntry(RuntimeTypeHandle_InternalAlloc)
    DllImportEntry(RuntimeTypeHandle_InternalAllocNoChecks)
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemory)
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemoryAligned)
    DllImportEntry(RuntimeTypeHandle_RegisterCollectibleTypeDependency)
    DllImportEntry(MethodBase_GetCurrentMethod)
    DllImportEntry(RuntimeMethodHandle_ConstructInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_GetVirtualFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_GetMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetTypicalMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_StripMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_IsCAVisibleFromDecoratedType)
    DllImportEntry(RuntimeMethodHandle_Destroy)
    DllImportEntry(RuntimeMethodHandle_GetStubIfNeededSlow)
    DllImportEntry(RuntimeMethodHandle_GetDiagnosticCodeStart)
    DllImportEntry(RuntimeMethodHandle_GetMethodBody)
    DllImportEntry(RuntimeModule_GetScopeName)
    DllImportEntry(RuntimeModule_GetFullyQualifiedName)
    DllImportEntry(RuntimeModule_GetTypes)
    DllImportEntry(RuntimeFieldHandle_GetValue)
    DllImportEntry(RuntimeFieldHandle_SetValue)
    DllImportEntry(RuntimeFieldHandle_GetValueDirect)
    DllImportEntry(RuntimeFieldHandle_SetValueDirect)
    DllImportEntry(RuntimeFieldHandle_GetEnCFieldAddr)
    DllImportEntry(RuntimeFieldHandle_GetRVAFieldInfo)
    DllImportEntry(RuntimeFieldHandle_GetFieldDataReference)
    DllImportEntry(UnsafeAccessors_ResolveGenericParamToTypeHandle)
    DllImportEntry(StackTrace_GetStackFramesInternal)
    DllImportEntry(AsyncHelpers_AddContinuationToExInternal)
    DllImportEntry(StackFrame_GetMethodDescFromNativeIP)
    DllImportEntry(ModuleBuilder_GetStringConstant)
    DllImportEntry(ModuleBuilder_GetTypeRef)
    DllImportEntry(ModuleBuilder_GetTokenFromTypeSpec)
    DllImportEntry(ModuleBuilder_GetMemberRef)
    DllImportEntry(ModuleBuilder_GetMemberRefOfMethodInfo)
    DllImportEntry(ModuleBuilder_GetMemberRefOfFieldInfo)
    DllImportEntry(ModuleBuilder_GetMemberRefFromSignature)
    DllImportEntry(ModuleBuilder_GetArrayMethodToken)
    DllImportEntry(ModuleBuilder_SetFieldRVAContent)
    DllImportEntry(ModuleHandle_GetMDStreamVersion)
    DllImportEntry(ModuleHandle_GetModuleType)
    DllImportEntry(ModuleHandle_GetToken)
    DllImportEntry(ModuleHandle_ResolveType)
    DllImportEntry(ModuleHandle_ResolveMethod)
    DllImportEntry(ModuleHandle_ResolveField)
    DllImportEntry(ModuleHandle_GetPEKind)
    DllImportEntry(ModuleHandle_GetDynamicMethod)
    DllImportEntry(AssemblyHandle_GetManifestModuleSlow)
    DllImportEntry(Signature_Init)
    DllImportEntry(Signature_AreEqual)
    DllImportEntry(Signature_GetCustomModifiersAtOffset)
    DllImportEntry(TypeBuilder_DefineGenericParam)
    DllImportEntry(TypeBuilder_DefineType)
    DllImportEntry(TypeBuilder_SetParentType)
    DllImportEntry(TypeBuilder_AddInterfaceImpl)
    DllImportEntry(TypeBuilder_DefineMethod)
    DllImportEntry(TypeBuilder_DefineMethodSpec)
    DllImportEntry(TypeBuilder_SetMethodIL)
    DllImportEntry(TypeBuilder_TermCreateClass)
    DllImportEntry(TypeBuilder_DefineField)
    DllImportEntry(TypeBuilder_DefineProperty)
    DllImportEntry(TypeBuilder_DefineEvent)
    DllImportEntry(TypeBuilder_DefineMethodSemantics)
    DllImportEntry(TypeBuilder_SetMethodImpl)
    DllImportEntry(TypeBuilder_DefineMethodImpl)
    DllImportEntry(TypeBuilder_GetTokenFromSig)
    DllImportEntry(TypeBuilder_SetFieldLayoutOffset)
    DllImportEntry(TypeBuilder_SetClassLayout)
    DllImportEntry(TypeBuilder_SetParamInfo)
    DllImportEntry(TypeBuilder_SetPInvokeData)
    DllImportEntry(TypeBuilder_SetConstantValue)
    DllImportEntry(TypeBuilder_DefineCustomAttribute)
    DllImportEntry(MdUtf8String_EqualsCaseInsensitive)
    DllImportEntry(Array_CreateInstance)
    DllImportEntry(Array_Ctor)
    DllImportEntry(Array_GetElementConstructorEntrypoint)
    DllImportEntry(AssemblyName_InitializeAssemblySpec)
    DllImportEntry(AssemblyNative_GetFullName)
    DllImportEntry(AssemblyNative_GetLocation)
    DllImportEntry(AssemblyNative_GetResource)
    DllImportEntry(AssemblyNative_GetCodeBase)
    DllImportEntry(AssemblyNative_GetFlags)
    DllImportEntry(AssemblyNative_GetHashAlgorithm)
    DllImportEntry(AssemblyNative_GetLocale)
    DllImportEntry(AssemblyNative_GetPublicKey)
    DllImportEntry(AssemblyNative_GetSimpleName)
    DllImportEntry(AssemblyNative_GetVersion)
    DllImportEntry(AssemblyNative_InternalLoad)
    DllImportEntry(AssemblyNative_GetTypeCore)
    DllImportEntry(AssemblyNative_GetTypeCoreIgnoreCase)
    DllImportEntry(AssemblyNative_GetForwardedType)
    DllImportEntry(AssemblyNative_GetManifestResourceInfo)
    DllImportEntry(AssemblyNative_GetManifestResourceNames)
    DllImportEntry(AssemblyNative_GetReferencedAssemblies)
    DllImportEntry(AssemblyNative_GetModules)
    DllImportEntry(AssemblyNative_GetModule)
    DllImportEntry(AssemblyNative_GetExportedTypes)
    DllImportEntry(AssemblyNative_GetEntryPoint)
    DllImportEntry(AssemblyNative_GetImageRuntimeVersion)
    DllImportEntry(AssemblyNative_InternalTryGetRawMetadata)
    DllImportEntry(AssemblyNative_ApplyUpdate)
    DllImportEntry(AssemblyNative_IsApplyUpdateSupported)
    DllImportEntry(AssemblyNative_InitializeAssemblyLoadContext)
    DllImportEntry(AssemblyNative_PrepareForAssemblyLoadContextRelease)
    DllImportEntry(AssemblyNative_LoadFromPath)
    DllImportEntry(AssemblyNative_LoadFromStream)
#ifdef TARGET_WINDOWS
    DllImportEntry(AssemblyNative_LoadFromInMemoryModule)
#endif
    DllImportEntry(AssemblyNative_GetLoadContextForAssembly)
    DllImportEntry(AssemblyNative_TraceResolvingHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceAssemblyResolveHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceAssemblyLoadFromResolveHandlerInvoked)
    DllImportEntry(AssemblyNative_TraceSatelliteSubdirectoryPathProbed)
    DllImportEntry(AssemblyNative_GetLoadedAssemblies)
    DllImportEntry(AssemblyNative_GetAssemblyCount)
    DllImportEntry(AssemblyNative_GetEntryAssembly)
    DllImportEntry(AssemblyNative_GetExecutingAssembly)
    DllImportEntry(TypeMapLazyDictionary_FindPrecachedExternalTypeMapEntry)
    DllImportEntry(TypeMapLazyDictionary_FindPrecachedProxyTypeMapEntry)
    DllImportEntry(TypeMapLazyDictionary_ProcessAttributes)
#if defined(FEATURE_MULTICOREJIT)
    DllImportEntry(MultiCoreJIT_InternalSetProfileRoot)
    DllImportEntry(MultiCoreJIT_InternalStartProfile)
#endif
    DllImportEntry(LoaderAllocator_Destroy)
    DllImportEntry(String_Intern)
    DllImportEntry(String_IsInterned)
    DllImportEntry(AppDomain_CreateDynamicAssembly)
    DllImportEntry(AppContext_SetFirstChanceExceptionHandler)
    DllImportEntry(AppContext_TryGetHostPropertyValue)
    DllImportEntry(ThreadNative_GetQCallSpecialException)
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_Start)
    DllImportEntry(ThreadNative_SetPriority)
    DllImportEntry(ThreadNative_GetCurrentThread)
    DllImportEntry(ThreadNative_GetIsBackground)
    DllImportEntry(ThreadNative_SetIsBackground)
    DllImportEntry(ThreadNative_InformThreadNameChange)
    DllImportEntry(ThreadNative_Initialize)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_GetThreadState)
    DllImportEntry(ThreadNative_ReentrantWaitAny)
#ifdef FEATURE_COMINTEROP_APARTMENT_SUPPORT
    DllImportEntry(ThreadNative_GetApartmentState)
    DllImportEntry(ThreadNative_SetApartmentState)
#endif // FEATURE_COMINTEROP_APARTMENT_SUPPORT
    DllImportEntry(ThreadNative_Abort)
    DllImportEntry(ThreadNative_ResetAbort)
#ifdef TARGET_WINDOWS
    DllImportEntry(ThreadNative_CheckForPendingInterrupt)
    DllImportEntry(ThreadNative_Interrupt)
    DllImportEntry(ThreadNative_GetOSHandle)
#endif // TARGET_WINDOWS
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(ThreadNative_PollGC)
#endif // FEATURE_MULTITHREADING
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ThreadNative_DisableComObjectEagerCleanup)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ObjectHeader_GetOrCreateLockObject)
    DllImportEntry(ClrConfig_GetConfigBoolValue)
#ifdef TARGET_SUNOS
    // Work around illumos.org/issues/17832
    {"memset", (void*)&std::memset},
#elif defined(TARGET_BROWSER)
    {"memset", (void*)&QCall_memset},
#else // TARGET_SUNOS
    DllImportEntry(memset)
#endif // TARGET_SUNOS
#ifdef TARGET_BROWSER
    {"memmove", (void*)&QCall_memmove},
#else
    DllImportEntry(memmove)
#endif // TARGET_BROWSER
    DllImportEntry(DependentHandle_InternalAllocWithGCTransition)
    DllImportEntry(DependentHandle_InternalFreeWithGCTransition)
    DllImportEntry(GCInterface_GetTotalAllocatedBytesPrecise)
    DllImportEntry(GCInterface_WaitForFullGCApproach)
    DllImportEntry(GCInterface_WaitForFullGCComplete)
    DllImportEntry(GCInterface_StartNoGCRegion)
    DllImportEntry(GCInterface_EndNoGCRegion)
    DllImportEntry(GCInterface_AllocateNewArray)
    DllImportEntry(GCInterface_GetTotalMemory)
    DllImportEntry(GCInterface_Collect)
    DllImportEntry(GCInterface_ReRegisterForFinalize)
    DllImportEntry(GCInterface_GetNextFinalizableObject)
    DllImportEntry(GCInterface_WaitForPendingFinalizers)
    DllImportEntry(GCInterface_AddMemoryPressure)
    DllImportEntry(GCInterface_RemoveMemoryPressure)
    DllImportEntry(GCInterface_RegisterFrozenSegment)
    DllImportEntry(GCInterface_UnregisterFrozenSegment)
    DllImportEntry(GCInterface_EnumerateConfigurationValues)
    DllImportEntry(GCInterface_RefreshMemoryLimit)
    DllImportEntry(GCInterface_EnableNoGCRegionCallback)
    DllImportEntry(GCInterface_GetGenerationBudget)
    DllImportEntry(GCHandle_InternalAllocWithGCTransition)
    DllImportEntry(GCHandle_InternalFreeWithGCTransition)
#ifdef FEATURE_JAVAMARSHAL
    DllImportEntry(GCHandle_InternalGetBridgeWait)
#endif
    DllImportEntry(MarshalNative_OffsetOf)
    DllImportEntry(MarshalNative_Prelink)
    DllImportEntry(MarshalNative_IsBuiltInComSupported)
    DllImportEntry(MarshalNative_HasLayout)
    DllImportEntry(MarshalNative_SizeOfHelper)
    DllImportEntry(MarshalNative_GetDelegateForFunctionPointerInternal)
    DllImportEntry(MarshalNative_GetFunctionPointerForDelegateInternal)
    DllImportEntry(MarshalNative_GetExceptionForHR)
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(MarshalNative_GetHRForException)
#endif // FEATURE_COMINTEROP
    DllImportEntry(MarshalNative_GetHINSTANCE)
#ifdef _DEBUG
    DllImportEntry(MarshalNative_GetIsInCooperativeGCModeFunctionPointer)
#endif
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(MarshalNative_GetTypeFromCLSID)
    DllImportEntry(MarshalNative_GetIUnknownForObject)
    DllImportEntry(MarshalNative_GetIDispatchForObject)
    DllImportEntry(MarshalNative_GetIUnknownOrIDispatchForObject)
    DllImportEntry(MarshalNative_GetComInterfaceForObject)
    DllImportEntry(MarshalNative_GetObjectForIUnknown)
    DllImportEntry(MarshalNative_GetUniqueObjectForIUnknown)
    DllImportEntry(MarshalNative_GetTypedObjectForIUnknown)
    DllImportEntry(MarshalNative_CreateAggregatedObject)
    DllImportEntry(MarshalNative_CleanupUnusedObjectsInCurrentContext)
    DllImportEntry(MarshalNative_ReleaseComObject)
    DllImportEntry(MarshalNative_FinalReleaseComObject)
    DllImportEntry(MarshalNative_InternalCreateWrapperOfType)
    DllImportEntry(MarshalNative_IsTypeVisibleFromCom)
    DllImportEntry(MarshalNative_GetNativeVariantForObject)
    DllImportEntry(MarshalNative_GetObjectForNativeVariant)
    DllImportEntry(MarshalNative_GetObjectsForNativeVariants)
    DllImportEntry(MarshalNative_GetStartComSlot)
    DllImportEntry(MarshalNative_GetEndComSlot)
    DllImportEntry(MarshalNative_ChangeWrapperHandleStrength)
#endif
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MngdSafeArrayMarshaler_CreateMarshaler)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ClearNative)
    DllImportEntry(Variant_ConvertValueTypeToRecord)
#endif // FEATURE_COMINTEROP
    DllImportEntry(NativeLibrary_LoadByName)
    DllImportEntry(GetTypeLoadExceptionMessage)
    DllImportEntry(GetFileLoadExceptionMessage)
    DllImportEntry(FileLoadException_GetMessageForHR)
    DllImportEntry(Interlocked_MemoryBarrierProcessWide)
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(Thread_SpinWait)
    DllImportEntry(Thread_Yield)
    DllImportEntry(Thread_GetOptimalMaxSpinWaitsPerSpinIteration)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(ObjectNative_GetHashCodeSlow)
    DllImportEntry(ObjectNative_AllocateUninitializedClone)
    DllImportEntry(MetadataImport_Enum)
    DllImportEntry(MetadataImport_GetMarshalAs)
    DllImportEntry(ReflectionInvocation_RunClassConstructor)
    DllImportEntry(ReflectionInvocation_RunModuleConstructor)
    DllImportEntry(ReflectionInvocation_CompileMethod)
    DllImportEntry(ReflectionInvocation_PrepareMethod)
    DllImportEntry(ReflectionInvocation_PrepareDelegate)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_InvokeDispMethod)
    DllImportEntry(ReflectionInvocation_GetComObjectGuid)
#endif // FEATURE_COMINTEROP
    DllImportEntry(ReflectionInvocation_GetGuid)
    DllImportEntry(ReflectionInvocation_SizeOf)
    DllImportEntry(ReflectionInvocation_GetBoxInfo)
    DllImportEntry(ReflectionSerialization_GetCreateUninitializedObjectInfo)
#if defined(FEATURE_COMWRAPPERS)
    DllImportEntry(ComWrappers_GetIUnknownImpl)
    DllImportEntry(ComWrappers_GetUntrackedAddRefRelease)
    DllImportEntry(ComWrappers_AllocateRefCountedHandle)
    DllImportEntry(ComWrappers_GetIReferenceTrackerTargetVftbl)
    DllImportEntry(ComWrappers_GetTaggedImpl)
    DllImportEntry(ComWrappers_RegisterIsRootedCallback)
    DllImportEntry(TrackerObjectManager_HasReferenceTrackerManager)
    DllImportEntry(TrackerObjectManager_TryRegisterReferenceTrackerManager)
    DllImportEntry(TrackerObjectManager_RegisterNativeObjectWrapperCache)
    DllImportEntry(TrackerObjectManager_IsGlobalPeggingEnabled)
#endif
#if defined(FEATURE_OBJCMARSHAL)
    DllImportEntry(ObjCMarshal_TrySetGlobalMessageSendCallback)
    DllImportEntry(ObjCMarshal_TryInitializeReferenceTracker)
    DllImportEntry(ObjCMarshal_AllocateReferenceTrackingHandle)
#endif
#if defined(FEATURE_JAVAMARSHAL)
    DllImportEntry(JavaMarshal_Initialize)
    DllImportEntry(JavaMarshal_CreateReferenceTrackingHandle)
    DllImportEntry(JavaMarshal_FinishCrossReferenceProcessing)
    DllImportEntry(JavaMarshal_GetContext)
#endif
#if defined(FEATURE_EVENTSOURCE_XPLAT)
    DllImportEntry(IsEventSourceLoggingEnabled)
    DllImportEntry(LogEventSource)
    DllImportEntry(EventSource_GetClrConfig)
#endif
#if defined(FEATURE_PERFTRACING)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolMinMaxThreads)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIOEnqueue)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIODequeue)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolIOPack)
    DllImportEntry(NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount)
    DllImportEntry(NativeRuntimeEventSource_LogContentionLockCreated)
    DllImportEntry(NativeRuntimeEventSource_LogContentionStart)
    DllImportEntry(NativeRuntimeEventSource_LogContentionStop)
    DllImportEntry(NativeRuntimeEventSource_LogWaitHandleWaitStart)
    DllImportEntry(NativeRuntimeEventSource_LogWaitHandleWaitStop)
    DllImportEntry(EventPipeInternal_Enable)
    DllImportEntry(EventPipeInternal_Disable)
    DllImportEntry(EventPipeInternal_GetSessionInfo)
    DllImportEntry(EventPipeInternal_CreateProvider)
    DllImportEntry(EventPipeInternal_DefineEvent)
    DllImportEntry(EventPipeInternal_DeleteProvider)
    DllImportEntry(EventPipeInternal_EventActivityIdControl)
    DllImportEntry(EventPipeInternal_GetProvider)
    DllImportEntry(EventPipeInternal_WriteEventData)
    DllImportEntry(EventPipeInternal_GetNextEvent)
    DllImportEntry(EventPipeInternal_SignalSession)
    DllImportEntry(EventPipeInternal_WaitForSessionSignal)
#if defined(TARGET_WASI) && defined(PERFTRACING_DISABLE_THREADS)
    DllImportEntry(EventPipeInternal_WasiRunJobs)
#endif // TARGET_WASI && PERFTRACING_DISABLE_THREADS
#endif // FEATURE_PERFTRACING
#if defined(TARGET_UNIX)
    DllImportEntry(FreeEnvironmentStringsW)
    DllImportEntry(GetEnvironmentStringsW)
    DllImportEntry(GetEnvironmentVariableW)
    DllImportEntry(OutputDebugStringW)
    DllImportEntry(SetEnvironmentVariableW)
#endif
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    DllImportEntry(X86Base_CpuId)
#endif
    DllImportEntry(StubHelpers_CreateCustomMarshaler)
    DllImportEntry(StubHelpers_CreateLayoutClassMarshalStubs)
    DllImportEntry(StubHelpers_ThrowInteropParamException)
    DllImportEntry(StubHelpers_ThrowInteropException)
#ifdef FEATURE_VARARGS
    DllImportEntry(StubHelpers_MarshalToManagedVaList)
    DllImportEntry(StubHelpers_MarshalToUnmanagedVaList)
#endif // FEATURE_VARARGS
    DllImportEntry(StubHelpers_ValidateObject)
    DllImportEntry(StubHelpers_ValidateByref)
    DllImportEntry(StubHelpers_MulticastDebuggerTraceHelper)
#ifdef PROFILING_SUPPORTED
    DllImportEntry(StubHelpers_ProfilerBeginTransitionCallback)
    DllImportEntry(StubHelpers_ProfilerEndTransitionCallback)
#endif
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(StubHelpers_GetCOMIPFromRCWSlow)
    DllImportEntry(ObjectMarshaler_ConvertToNative)
    DllImportEntry(ObjectMarshaler_ConvertToNativeVariantArrayElement)
    DllImportEntry(ObjectMarshaler_ConvertToManaged)
    DllImportEntry(InterfaceMarshaler_ConvertToNative)
    DllImportEntry(InterfaceMarshaler_ConvertToManaged)
    DllImportEntry(InterfaceMarshaler_GetObjectForComCallableWrapperIUnknown)
    DllImportEntry(InterfaceMarshaler_ValidateComVisibilityForIUnknown)
#endif
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(ComWeakRefToObject)
    DllImportEntry(ObjectToComWeakRef)
#endif
    DllImportEntry(SfiInit)
    DllImportEntry(SfiNext)
    DllImportEntry(CallFilterFunclet)
    DllImportEntry(EHEnumInitFromStackFrameIterator)
    DllImportEntry(EHEnumNext)
    DllImportEntry(GetCurrentExInfo)
    DllImportEntry(AppendExceptionStackFrame)
    DllImportEntry(InitClassHelper)
    DllImportEntry(ResolveVirtualFunctionPointer)
#ifdef FEATURE_MULTITHREADING
    DllImportEntry(GetThreadStaticsByMethodTable)
    DllImportEntry(GetThreadStaticsByIndex)
#endif // FEATURE_MULTITHREADING
    DllImportEntry(GenericHandleWorker)
    DllImportEntry(ThrowInvalidCastException)
    DllImportEntry(IsInstanceOf_NoCacheLookup)
    DllImportEntry(VersionResilientHashCode_TypeHashCode)
    DllImportEntry(TailCallHelp_AllocTailCallArgBufferInternal)
#ifdef TARGET_BROWSER
    DllImportEntry(SystemJS_MarkAsyncMain)
    DllImportEntry(SystemJS_ResolveMainPromise)
    DllImportEntry(SystemJS_RejectMainPromise)
    DllImportEntry(SystemJS_ScheduleTimer)
    DllImportEntry(SystemJS_ScheduleBackgroundJob)
#endif // TARGET_BROWSER
#ifdef TARGET_WASI
    DllImportEntry(WasiFinalizer_RunWorker)
    DllImportEntry(WasiFinalizer_TryClearPending)
#endif // TARGET_WASI

    FCallEntry(Array_GetCorElementTypeOfElementType, Array_GetCorElementTypeOfElementType)
    FCallEntry(AssemblyLoadContext_IsTracingEnabled, AssemblyLoadContext_IsTracingEnabled)
#if (defined(FEATURE_INTERPRETER))
    FCallEntry(AsyncHelpers_ResumeInterpreterContinuation, AsyncHelpers_ResumeInterpreterContinuation)
#endif
    FCallEntry(Buffer_BulkMoveWithWriteBarrierInternal, Buffer_BulkMoveWithWriteBarrierInternal)
    FCallEntry(Delegate_GetInvokeMethod, Delegate_GetInvokeMethod)
    FCallEntry(Delegate_GetMulticastInvoke, Delegate_GetMulticastInvoke)
    FCallEntry(DependentHandle_InternalAlloc, DependentHandle_InternalAlloc)
    FCallEntry(DependentHandle_InternalFree, DependentHandle_InternalFree)
    FCallEntry(DependentHandle_InternalGetDependent, DependentHandle_InternalGetDependent)
    FCallEntry(DependentHandle_InternalGetTarget, DependentHandle_InternalGetTarget)
    FCallEntry(DependentHandle_InternalGetTargetAndDependent, DependentHandle_InternalGetTargetAndDependent)
    FCallEntry(DependentHandle_InternalSetDependent, DependentHandle_InternalSetDependent)
    FCallEntry(DependentHandle_InternalSetTargetToNull, DependentHandle_InternalSetTargetToNull)
    FCallEntry(Environment_get_ExitCode, Environment_get_ExitCode)
    FCallEntry(Environment_set_ExitCode, Environment_set_ExitCode)
    FCallEntry(Exception_GetExceptionCount, Exception_GetExceptionCount)
    FCallEntry(Exception_IsImmutableAgileException, Exception_IsImmutableAgileException)
    FCallEntry(Exception_PrepareForForeignExceptionRaise, Exception_PrepareForForeignExceptionRaise)
    FCallEntry(GCFrameRegistration_RegisterForGCReporting, GCFrameRegistration_RegisterForGCReporting)
    FCallEntry(GCFrameRegistration_UnregisterForGCReporting, GCFrameRegistration_UnregisterForGCReporting)
    FCallEntry(GCHandle_InternalCompareExchange, GCHandle_InternalCompareExchange)
    FCallEntry(GCHandle_InternalGet, GCHandle_InternalGet)
    FCallEntry(GCHandle_InternalSet, GCHandle_InternalSet)
#if (defined(FEATURE_JAVAMARSHAL))
    FCallEntry(GCHandle_InternalTryGetBridgeWait, GCHandle_InternalTryGetBridgeWait)
#endif
    FCallEntry(GCHandle__InternalAlloc, GCHandle__InternalAlloc)
    FCallEntry(GCHandle__InternalFree, GCHandle__InternalFree)
    FCallEntry(GCSettings_GetGCLatencyMode, GCSettings_GetGCLatencyMode)
    FCallEntry(GCSettings_GetLOHCompactionMode, GCSettings_GetLOHCompactionMode)
    FCallEntry(GCSettings_SetGCLatencyMode, GCSettings_SetGCLatencyMode)
    FCallEntry(GCSettings_SetLOHCompactionMode, GCSettings_SetLOHCompactionMode)
    FCallEntry(GCSettings_get_IsServerGC, GCSettings_get_IsServerGC)
    FCallEntry(GC_GetAllocatedBytesForCurrentThread, GC_GetAllocatedBytesForCurrentThread)
    FCallEntry(GC_GetGenerationInternal, GC_GetGenerationInternal)
    FCallEntry(GC_GetGenerationSize, GC_GetGenerationSize)
    FCallEntry(GC_GetLastGCPercentTimeInGC, GC_GetLastGCPercentTimeInGC)
    FCallEntry(GC_GetMaxGeneration, GC_GetMaxGeneration)
    FCallEntry(GC_GetMemoryInfo, GC_GetMemoryInfo)
    FCallEntry(GC_GetMemoryLoad, GC_GetMemoryLoad)
    FCallEntry(GC_GetSegmentSize, GC_GetSegmentSize)
    FCallEntry(GC_GetTotalAllocatedBytesApproximate, GC_GetTotalAllocatedBytesApproximate)
    FCallEntry(GC_SuppressFinalizeInternal, GC_SuppressFinalizeInternal)
    FCallEntry(GC__CancelFullGCNotification, GC__CancelFullGCNotification)
    FCallEntry(GC__CollectionCount, GC__CollectionCount)
    FCallEntry(GC__GetTotalPauseDuration, GC__GetTotalPauseDuration)
    FCallEntry(GC__RegisterForFullGCNotification, GC__RegisterForFullGCNotification)
    FCallEntry(Interlocked_CompareExchange32, Interlocked_CompareExchange32)
    FCallEntry(Interlocked_CompareExchange32Pointer, Interlocked_CompareExchange32)
    FCallEntry(Interlocked_CompareExchange64, Interlocked_CompareExchange64)
    FCallEntry(Interlocked_CompareExchangeObject, Interlocked_CompareExchangeObject)
    FCallEntry(Interlocked_Exchange32, Interlocked_Exchange32)
    FCallEntry(Interlocked_Exchange64, Interlocked_Exchange64)
    FCallEntry(Interlocked_ExchangeAdd32, Interlocked_ExchangeAdd32)
    FCallEntry(Interlocked_ExchangeAdd64, Interlocked_ExchangeAdd64)
    FCallEntry(Interlocked_ExchangeObject, Interlocked_ExchangeObject)
    FCallEntry(JitInfo_GetCompilationTimeInTicks, JitInfo_GetCompilationTimeInTicks)
    FCallEntry(JitInfo_GetCompiledILBytes, JitInfo_GetCompiledILBytes)
    FCallEntry(JitInfo_GetCompiledMethodCount, JitInfo_GetCompiledMethodCount)
#if (defined(FEATURE_COMINTEROP))
    FCallEntry(Marshal_AreComObjectsAvailableForCleanup, Marshal_AreComObjectsAvailableForCleanup)
#endif
    FCallEntry(Marshal_GetExceptionCode, Marshal_GetExceptionCode)
    FCallEntry(Marshal_GetExceptionPointers, Marshal_GetExceptionPointers)
    FCallEntry(Marshal_GetLastPInvokeError, Marshal_GetLastPInvokeError)
    FCallEntry(Marshal_SetLastPInvokeError, Marshal_SetLastPInvokeError)
    FCallEntry(MathF_Acos, Rhp_acosf)
    FCallEntry(MathF_Acosh, Rhp_acoshf)
    FCallEntry(MathF_Asin, Rhp_asinf)
    FCallEntry(MathF_Asinh, Rhp_asinhf)
    FCallEntry(MathF_Atan, Rhp_atanf)
    FCallEntry(MathF_Atan2, Rhp_atan2f)
    FCallEntry(MathF_Atanh, Rhp_atanhf)
    FCallEntry(MathF_Cbrt, Rhp_cbrtf)
    FCallEntry(MathF_Ceiling, Rhp_ceilf)
    FCallEntry(MathF_Cos, Rhp_cosf)
    FCallEntry(MathF_Cosh, Rhp_coshf)
    FCallEntry(MathF_Exp, Rhp_expf)
    FCallEntry(MathF_Floor, Rhp_floorf)
    FCallEntry(MathF_FusedMultiplyAdd, Rhp_fmaf)
    FCallEntry(MathF_Log, Rhp_logf)
    FCallEntry(MathF_Log10, Rhp_log10f)
    FCallEntry(MathF_Log2, Rhp_log2f)
    FCallEntry(MathF_ModF, Rhp_modff)
    FCallEntry(MathF_Pow, Rhp_powf)
    FCallEntry(MathF_Sin, Rhp_sinf)
    FCallEntry(MathF_SinCos, Rhp_sincosf)
    FCallEntry(MathF_Sinh, Rhp_sinhf)
    FCallEntry(MathF_Sqrt, Rhp_sqrtf)
    FCallEntry(MathF_Tan, Rhp_tanf)
    FCallEntry(MathF_Tanh, Rhp_tanhf)
    FCallEntry(Math_Acos, Rhp_acos)
    FCallEntry(Math_Acosh, Rhp_acosh)
    FCallEntry(Math_Asin, Rhp_asin)
    FCallEntry(Math_Asinh, Rhp_asinh)
    FCallEntry(Math_Atan, Rhp_atan)
    FCallEntry(Math_Atan2, Rhp_atan2)
    FCallEntry(Math_Atanh, Rhp_atanh)
    FCallEntry(Math_Cbrt, Rhp_cbrt)
    FCallEntry(Math_Ceiling, Rhp_ceil)
    FCallEntry(Math_Cos, Rhp_cos)
    FCallEntry(Math_Cosh, Rhp_cosh)
#if (defined(TARGET_32BIT))
    FCallEntry(Math_DivInt32Internal, DivInt32Internal)
    FCallEntry(Math_DivInt64Internal, DivInt64Internal)
    FCallEntry(Math_DivUInt32Internal, DivUInt32Internal)
    FCallEntry(Math_DivUInt64Internal, DivUInt64Internal)
#endif
    FCallEntry(Math_Exp, Rhp_exp)
    FCallEntry(Math_Floor, Rhp_floor)
    FCallEntry(Math_FusedMultiplyAdd, Rhp_fma)
    FCallEntry(Math_Log, Rhp_log)
    FCallEntry(Math_Log10, Rhp_log10)
    FCallEntry(Math_Log2, Rhp_log2)
    FCallEntry(Math_ModF, Rhp_modf)
#if (defined(TARGET_32BIT))
    FCallEntry(Math_ModInt32Internal, ModInt32Internal)
    FCallEntry(Math_ModInt64Internal, ModInt64Internal)
    FCallEntry(Math_ModUInt32Internal, ModUInt32Internal)
    FCallEntry(Math_ModUInt64Internal, ModUInt64Internal)
#endif
    FCallEntry(Math_Pow, Rhp_pow)
    FCallEntry(Math_Sin, Rhp_sin)
    FCallEntry(Math_SinCos, Rhp_sincos)
    FCallEntry(Math_Sinh, Rhp_sinh)
    FCallEntry(Math_Sqrt, Rhp_sqrt)
    FCallEntry(Math_Tan, Rhp_tan)
    FCallEntry(Math_Tanh, Rhp_tanh)
    FCallEntry(MetadataImport_GetClassLayout, MetadataImport_GetClassLayout)
    FCallEntry(MetadataImport_GetCustomAttributeProps, MetadataImport_GetCustomAttributeProps)
    FCallEntry(MetadataImport_GetDefaultValue, MetadataImport_GetDefaultValue)
    FCallEntry(MetadataImport_GetEventProps, MetadataImport_GetEventProps)
    FCallEntry(MetadataImport_GetFieldDefProps, MetadataImport_GetFieldDefProps)
    FCallEntry(MetadataImport_GetFieldMarshal, MetadataImport_GetFieldMarshal)
    FCallEntry(MetadataImport_GetFieldOffset, MetadataImport_GetFieldOffset)
    FCallEntry(MetadataImport_GetGenericParamProps, MetadataImport_GetGenericParamProps)
    FCallEntry(MetadataImport_GetMemberRefProps, MetadataImport_GetMemberRefProps)
    FCallEntry(MetadataImport_GetMetadataImport, MetadataImport_GetMetadataImport)
    FCallEntry(MetadataImport_GetName, MetadataImport_GetName)
    FCallEntry(MetadataImport_GetNamespace, MetadataImport_GetNamespace)
    FCallEntry(MetadataImport_GetPInvokeMap, MetadataImport_GetPInvokeMap)
    FCallEntry(MetadataImport_GetParamDefProps, MetadataImport_GetParamDefProps)
    FCallEntry(MetadataImport_GetParentToken, MetadataImport_GetParentToken)
    FCallEntry(MetadataImport_GetPropertyProps, MetadataImport_GetPropertyProps)
    FCallEntry(MetadataImport_GetScopeProps, MetadataImport_GetScopeProps)
    FCallEntry(MetadataImport_GetSigOfFieldDef, MetadataImport_GetSigOfFieldDef)
    FCallEntry(MetadataImport_GetSigOfMethodDef, MetadataImport_GetSigOfMethodDef)
    FCallEntry(MetadataImport_GetSignatureFromToken, MetadataImport_GetSignatureFromToken)
    FCallEntry(MetadataImport_GetUserString, MetadataImport_GetUserString)
    FCallEntry(MetadataImport_IsValidToken, MetadataImport_IsValidToken)
    FCallEntry(MethodTable_GetLoaderAllocatorHandle, MethodTable_GetLoaderAllocatorHandle)
    FCallEntry(MethodTable_GetMethodTableMatchingParentClass, MethodTable_GetMethodTableMatchingParentClass)
    FCallEntry(MethodTable_GetNumInstanceFieldBytes, MethodTable_GetNumInstanceFieldBytes)
    FCallEntry(MethodTable_GetPrimitiveCorElementType, MethodTable_GetPrimitiveCorElementType)
    FCallEntry(ObjectHeader_GetLockHandleIfExists, ObjectHeader_GetLockHandleIfExists)
    FCallEntry(RuntimeAssembly_GetIsCollectible, RuntimeAssembly_GetIsCollectible)
    FCallEntry(RuntimeAssembly_GetIsDynamic, RuntimeAssembly_GetIsDynamic)
    FCallEntry(RuntimeAssembly_GetManifestModule, RuntimeAssembly_GetManifestModule)
    FCallEntry(RuntimeAssembly_GetTokenInternal, RuntimeAssembly_GetTokenInternal)
    FCallEntry(RuntimeFieldHandle_AcquiresContextFromThis, RuntimeFieldHandle_AcquiresContextFromThis)
    FCallEntry(RuntimeFieldHandle_GetApproxDeclaringMethodTable, RuntimeFieldHandle_GetApproxDeclaringMethodTable)
    FCallEntry(RuntimeFieldHandle_GetAttributes, RuntimeFieldHandle_GetAttributes)
    FCallEntry(RuntimeFieldHandle_GetInstanceFieldOffset, RuntimeFieldHandle_GetInstanceFieldOffset)
    FCallEntry(RuntimeFieldHandle_GetLoaderAllocatorInternal, RuntimeFieldHandle_GetLoaderAllocatorInternal)
    FCallEntry(RuntimeFieldHandle_GetStaticFieldAddress, RuntimeFieldHandle_GetStaticFieldAddress)
    FCallEntry(RuntimeFieldHandle_GetStaticFieldForGenericType, RuntimeFieldHandle_GetStaticFieldForGenericType)
    FCallEntry(RuntimeFieldHandle_GetToken, RuntimeFieldHandle_GetToken)
    FCallEntry(RuntimeFieldHandle_GetUtf8NameInternal, RuntimeFieldHandle_GetUtf8NameInternal)
    FCallEntry(RuntimeFieldHandle_IsFastPathSupported, RuntimeFieldHandle_IsFastPathSupported)
    FCallEntry(RuntimeHelpers_ContentEquals, RuntimeHelpers_ContentEquals)
    FCallEntry(RuntimeHelpers_GetTailCallArgBuffer, RuntimeHelpers_GetTailCallArgBuffer)
    FCallEntry(RuntimeHelpers_GetTailCallInfo, RuntimeHelpers_GetTailCallInfo)
    FCallEntry(RuntimeHelpers_TryEnsureSufficientExecutionStack, RuntimeHelpers_TryEnsureSufficientExecutionStack)
    FCallEntry(RuntimeHelpers_TryGetHashCode, RuntimeHelpers_TryGetHashCode)
    FCallEntry(RuntimeMethodHandle_GetAttributes, RuntimeMethodHandle_GetAttributes)
    FCallEntry(RuntimeMethodHandle_GetGenericParameterCount, RuntimeMethodHandle_GetGenericParameterCount)
    FCallEntry(RuntimeMethodHandle_GetImplAttributes, RuntimeMethodHandle_GetImplAttributes)
    FCallEntry(RuntimeMethodHandle_GetLoaderAllocatorInternal, RuntimeMethodHandle_GetLoaderAllocatorInternal)
    FCallEntry(RuntimeMethodHandle_GetMethodDef, RuntimeMethodHandle_GetMethodDef)
    FCallEntry(RuntimeMethodHandle_GetMethodFromCanonical, RuntimeMethodHandle_GetMethodFromCanonical)
    FCallEntry(RuntimeMethodHandle_GetMethodTable, RuntimeMethodHandle_GetMethodTable)
    FCallEntry(RuntimeMethodHandle_GetResolver, RuntimeMethodHandle_GetResolver)
    FCallEntry(RuntimeMethodHandle_GetSlot, RuntimeMethodHandle_GetSlot)
    FCallEntry(RuntimeMethodHandle_GetStubIfNeededInternal, RuntimeMethodHandle_GetStubIfNeededInternal)
    FCallEntry(RuntimeMethodHandle_GetUtf8NameInternal, RuntimeMethodHandle_GetUtf8NameInternal)
    FCallEntry(RuntimeMethodHandle_HasMethodInstantiation, RuntimeMethodHandle_HasMethodInstantiation)
    FCallEntry(RuntimeMethodHandle_IsAsyncMethod, RuntimeMethodHandle_IsAsyncMethod)
    FCallEntry(RuntimeMethodHandle_IsCollectible, RuntimeMethodHandle_IsCollectible)
    FCallEntry(RuntimeMethodHandle_IsConstructor, RuntimeMethodHandle_IsConstructor)
    FCallEntry(RuntimeMethodHandle_IsDynamicMethod, RuntimeMethodHandle_IsDynamicMethod)
    FCallEntry(RuntimeMethodHandle_IsGenericMethodDefinition, RuntimeMethodHandle_IsGenericMethodDefinition)
    FCallEntry(RuntimeMethodHandle_IsTypicalMethodDefinition, RuntimeMethodHandle_IsTypicalMethodDefinition)
    FCallEntry(RuntimeTypeHandle_CompareCanonicalHandles, RuntimeTypeHandle_CompareCanonicalHandles)
    FCallEntry(RuntimeTypeHandle_ContainsGenericVariables, RuntimeTypeHandle_ContainsGenericVariables)
    FCallEntry(RuntimeTypeHandle_GetArrayRank, RuntimeTypeHandle_GetArrayRank)
    FCallEntry(RuntimeTypeHandle_GetAssemblyIfExists, RuntimeTypeHandle_GetAssemblyIfExists)
    FCallEntry(RuntimeTypeHandle_GetAttributes, RuntimeTypeHandle_GetAttributes)
    FCallEntry(RuntimeTypeHandle_GetElementTypeHandle, RuntimeTypeHandle_GetElementTypeHandle)
    FCallEntry(RuntimeTypeHandle_GetFirstIntroducedMethod, RuntimeTypeHandle_GetFirstIntroducedMethod)
    FCallEntry(RuntimeTypeHandle_GetGenericVariableIndex, RuntimeTypeHandle_GetGenericVariableIndex)
    FCallEntry(RuntimeTypeHandle_GetModuleIfExists, RuntimeTypeHandle_GetModuleIfExists)
    FCallEntry(RuntimeTypeHandle_GetNextIntroducedMethod, RuntimeTypeHandle_GetNextIntroducedMethod)
    FCallEntry(RuntimeTypeHandle_GetNumVirtuals, RuntimeTypeHandle_GetNumVirtuals)
    FCallEntry(RuntimeTypeHandle_GetToken, RuntimeTypeHandle_GetToken)
    FCallEntry(RuntimeTypeHandle_GetUtf8NameInternal, RuntimeTypeHandle_GetUtf8NameInternal)
    FCallEntry(RuntimeTypeHandle_InternalAllocNoChecks_FastPath, RuntimeTypeHandle_InternalAllocNoChecks_FastPath)
    FCallEntry(RuntimeTypeHandle_IsGenericVariable, RuntimeTypeHandle_IsGenericVariable)
    FCallEntry(RuntimeTypeHandle_IsUnmanagedFunctionPointer, RuntimeTypeHandle_IsUnmanagedFunctionPointer)
    FCallEntry(Signature_GetCallingConventionFromFunctionPointerAtOffsetInternal, Signature_GetCallingConventionFromFunctionPointerAtOffsetInternal)
    FCallEntry(Signature_GetParameterOffsetInternal, Signature_GetParameterOffsetInternal)
    FCallEntry(Signature_GetTypeParameterOffsetInternal, Signature_GetTypeParameterOffsetInternal)
#if (defined(FEATURE_VARARGS))
    FCallEntry(StubHelpers_CalcVaListSize, StubHelpers_CalcVaListSize)
#endif
    FCallEntry(StubHelpers_ClearLastError, StubHelpers_ClearLastError)
#if (defined(FEATURE_COMINTEROP))
    FCallEntry(StubHelpers_GetCOMIPFromRCW, StubHelpers_GetCOMIPFromRCW)
#endif
    FCallEntry(StubHelpers_LogPinnedArgument, StubHelpers_LogPinnedArgument)
    FCallEntry(StubHelpers_SetLastError, StubHelpers_SetLastError)
#if (defined(FEATURE_MULTITHREADING))
    FCallEntry(Thread_CatchAtSafePoint, Thread_CatchAtSafePoint)
    FCallEntry(Thread_CurrentThreadIsFinalizerThread, Thread_CurrentThreadIsFinalizerThread)
    FCallEntry(Thread_InternalFinalize, Thread_InternalFinalize)
#endif
};
#undef FCallEntry

const void* QCallResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_QCall, ARRAY_SIZE(s_QCall), name);
}
