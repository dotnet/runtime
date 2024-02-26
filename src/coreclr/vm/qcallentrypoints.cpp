// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Describes all of the P/Invokes to the special QCall module that resolves to internal runtime methods.
#include "common.h"

//
// Headers for all ECall entrypoints
//
#include "arraynative.h"
#include "objectnative.h"
#include "comdelegate.h"
#include "customattribute.h"
#include "comdynamic.h"
#include "excep.h"
#include "fcall.h"
#include "clrconfignative.h"
#include "commodule.h"
#include "marshalnative.h"
#include "nativelibrarynative.h"
#include "system.h"
#include "comutilnative.h"
#include "comsynchronizable.h"
#include "floatdouble.h"
#include "floatsingle.h"
#include "comdatetime.h"
#include "compatibilityswitch.h"
#include "debugdebugger.h"
#include "assemblynative.hpp"
#include "comthreadpool.h"
#include "comwaithandle.h"

#include "proftoeeinterfaceimpl.h"

#include "appdomainnative.hpp"
#include "runtimehandles.h"
#include "reflectioninvocation.h"
#include "managedmdimport.hpp"
#include "typestring.h"
#include "comdependenthandle.h"
#include "weakreferencenative.h"
#include "varargsnative.h"
#include "mlinfo.h"

#ifdef FEATURE_COMINTEROP
#include "variant.h"
#include "oavariant.h"
#include "mngstdinterfaces.h"
#endif // FEATURE_COMINTEROP

#include "interoplibinterface.h"

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

#include <minipal/entrypoints.h>

#include "exceptionhandlingqcalls.h"

static const Entry s_QCall[] =
{
    DllImportEntry(ArgIterator_Init)
    DllImportEntry(ArgIterator_Init2)
    DllImportEntry(ArgIterator_GetNextArgType)
    DllImportEntry(ArgIterator_GetNextArg)
    DllImportEntry(ArgIterator_GetNextArg2)
    DllImportEntry(Enum_GetValuesAndNames)
    DllImportEntry(DebugDebugger_Launch)
    DllImportEntry(DebugDebugger_Log)
    DllImportEntry(Delegate_BindToMethodName)
    DllImportEntry(Delegate_BindToMethodInfo)
    DllImportEntry(Delegate_InitializeVirtualCallStub)
    DllImportEntry(Delegate_AdjustTarget)
    DllImportEntry(Delegate_InternalAlloc)
    DllImportEntry(Delegate_InternalAllocLike)
    DllImportEntry(Delegate_FindMethodHandle)
    DllImportEntry(Delegate_InternalEqualMethodHandles)
    DllImportEntry(Environment_Exit)
    DllImportEntry(Environment_FailFast)
    DllImportEntry(Environment_FailFastWithException)
    DllImportEntry(Environment_FailFastWithExceptionAndSource)
    DllImportEntry(Environment_GetProcessorCount)
    DllImportEntry(ExceptionNative_GetMessageFromNativeResources)
    DllImportEntry(RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter)
    DllImportEntry(QCall_GetGCHandleForTypeHandle)
    DllImportEntry(QCall_FreeGCHandleForTypeHandle)
    DllImportEntry(MethodTable_AreTypesEquivalent)
    DllImportEntry(MethodTable_CanCompareBitsOrUseFastGetHashCode)
    DllImportEntry(ValueType_GetHashCodeStrategy)
    DllImportEntry(RuntimeTypeHandle_MakePointer)
    DllImportEntry(RuntimeTypeHandle_MakeByRef)
    DllImportEntry(RuntimeTypeHandle_MakeSZArray)
    DllImportEntry(RuntimeTypeHandle_MakeArray)
    DllImportEntry(RuntimeTypeHandle_IsCollectible)
    DllImportEntry(RuntimeTypeHandle_GetConstraints)
    DllImportEntry(RuntimeTypeHandle_VerifyInterfaceIsImplemented)
    DllImportEntry(RuntimeTypeHandle_GetInterfaceMethodImplementation)
    DllImportEntry(RuntimeTypeHandle_IsVisible)
    DllImportEntry(RuntimeTypeHandle_ConstructName)
    DllImportEntry(RuntimeTypeHandle_GetInstantiation)
    DllImportEntry(RuntimeTypeHandle_Instantiate)
    DllImportEntry(RuntimeTypeHandle_GetGenericTypeDefinition)
    DllImportEntry(RuntimeTypeHandle_GetActivationInfo)
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemory)
    DllImportEntry(RuntimeTypeHandle_RegisterCollectibleTypeDependency)
    DllImportEntry(RuntimeMethodHandle_ConstructInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_GetIsCollectible)
    DllImportEntry(RuntimeMethodHandle_GetMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetTypicalMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_StripMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_IsCAVisibleFromDecoratedType)
    DllImportEntry(RuntimeMethodHandle_Destroy)
    DllImportEntry(RuntimeModule_GetScopeName)
    DllImportEntry(RuntimeModule_GetFullyQualifiedName)
    DllImportEntry(RuntimeModule_GetTypes)
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
    DllImportEntry(ModuleHandle_GetModuleType)
    DllImportEntry(ModuleHandle_ResolveType)
    DllImportEntry(ModuleHandle_ResolveMethod)
    DllImportEntry(ModuleHandle_ResolveField)
    DllImportEntry(ModuleHandle_GetPEKind)
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
    DllImportEntry(Array_GetElementConstructorEntrypoint)
    DllImportEntry(Array_CanAssignArrayType)
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
    DllImportEntry(AssemblyNative_GetIsCollectible)
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
#if defined(FEATURE_MULTICOREJIT)
    DllImportEntry(MultiCoreJIT_InternalSetProfileRoot)
    DllImportEntry(MultiCoreJIT_InternalStartProfile)
#endif
    DllImportEntry(LoaderAllocator_Destroy)
    DllImportEntry(CustomMarshaler_GetMarshalerObject)
    DllImportEntry(String_Intern)
    DllImportEntry(String_IsInterned)
    DllImportEntry(AppDomain_CreateDynamicAssembly)
    DllImportEntry(ThreadNative_Start)
    DllImportEntry(ThreadNative_SetIsBackground)
    DllImportEntry(ThreadNative_InformThreadNameChange)
    DllImportEntry(ThreadNative_YieldThread)
    DllImportEntry(ThreadNative_GetCurrentOSThreadId)
    DllImportEntry(ThreadNative_Abort)
    DllImportEntry(ThreadNative_ResetAbort)
    DllImportEntry(ThreadNative_SpinWait)
    DllImportEntry(ThreadNative_Interrupt)
    DllImportEntry(ThreadNative_Sleep)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(ThreadNative_DisableComObjectEagerCleanup)
#endif // FEATURE_COMINTEROP
#ifdef TARGET_UNIX
    DllImportEntry(WaitHandle_CorWaitOnePrioritizedNative)
#endif
    DllImportEntry(ClrConfig_GetConfigBoolValue)
    DllImportEntry(Buffer_Clear)
    DllImportEntry(Buffer_MemMove)
    DllImportEntry(DependentHandle_InternalAllocWithGCTransition)
    DllImportEntry(DependentHandle_InternalFreeWithGCTransition)
    DllImportEntry(GCInterface_GetTotalAllocatedBytesPrecise)
    DllImportEntry(GCInterface_WaitForFullGCApproach)
    DllImportEntry(GCInterface_WaitForFullGCComplete)
    DllImportEntry(GCInterface_StartNoGCRegion)
    DllImportEntry(GCInterface_EndNoGCRegion)
    DllImportEntry(GCInterface_GetTotalMemory)
    DllImportEntry(GCInterface_Collect)
    DllImportEntry(GCInterface_WaitForPendingFinalizers)
    DllImportEntry(GCInterface_AddMemoryPressure)
    DllImportEntry(GCInterface_RemoveMemoryPressure)
#ifdef FEATURE_BASICFREEZE
    DllImportEntry(GCInterface_RegisterFrozenSegment)
    DllImportEntry(GCInterface_UnregisterFrozenSegment)
#endif
    DllImportEntry(GCInterface_EnumerateConfigurationValues)
    DllImportEntry(GCInterface_RefreshMemoryLimit)
    DllImportEntry(GCInterface_EnableNoGCRegionCallback)
    DllImportEntry(GCInterface_GetGenerationBudget)
    DllImportEntry(GCHandle_InternalAllocWithGCTransition)
    DllImportEntry(GCHandle_InternalFreeWithGCTransition)
    DllImportEntry(MarshalNative_OffsetOf)
    DllImportEntry(MarshalNative_Prelink)
    DllImportEntry(MarshalNative_IsBuiltInComSupported)
    DllImportEntry(MarshalNative_TryGetStructMarshalStub)
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
    DllImportEntry(MngdNativeArrayMarshaler_ConvertSpaceToNative)
    DllImportEntry(MngdNativeArrayMarshaler_ConvertContentsToNative)
    DllImportEntry(MngdNativeArrayMarshaler_ConvertSpaceToManaged)
    DllImportEntry(MngdNativeArrayMarshaler_ConvertContentsToManaged)
    DllImportEntry(MngdNativeArrayMarshaler_ClearNativeContents)
    DllImportEntry(MngdFixedArrayMarshaler_ConvertContentsToNative)
    DllImportEntry(MngdFixedArrayMarshaler_ConvertSpaceToManaged)
    DllImportEntry(MngdFixedArrayMarshaler_ConvertContentsToManaged)
    DllImportEntry(MngdFixedArrayMarshaler_ClearNativeContents)
#ifdef FEATURE_COMINTEROP
    DllImportEntry(MngdSafeArrayMarshaler_CreateMarshaler)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToNative)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertSpaceToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ConvertContentsToManaged)
    DllImportEntry(MngdSafeArrayMarshaler_ClearNative)
    DllImportEntry(OAVariant_ChangeType)
#endif // FEATURE_COMINTEROP
    DllImportEntry(NativeLibrary_LoadFromPath)
    DllImportEntry(NativeLibrary_LoadByName)
    DllImportEntry(NativeLibrary_FreeLib)
    DllImportEntry(NativeLibrary_GetSymbol)
    DllImportEntry(GetTypeLoadExceptionMessage)
    DllImportEntry(GetFileLoadExceptionMessage)
    DllImportEntry(FileLoadException_GetMessageForHR)
    DllImportEntry(Interlocked_MemoryBarrierProcessWide)
    DllImportEntry(Monitor_Wait)
    DllImportEntry(Monitor_Pulse)
    DllImportEntry(Monitor_PulseAll)
    DllImportEntry(Monitor_GetLockContentionCount)
    DllImportEntry(ReflectionInvocation_RunClassConstructor)
    DllImportEntry(ReflectionInvocation_RunModuleConstructor)
    DllImportEntry(ReflectionInvocation_CompileMethod)
    DllImportEntry(ReflectionInvocation_PrepareMethod)
    DllImportEntry(ReflectionSerialization_GetCreateUninitializedObjectInfo)
#if defined(FEATURE_COMWRAPPERS)
    DllImportEntry(ComWrappers_GetIUnknownImpl)
    DllImportEntry(ComWrappers_TryGetComInstance)
    DllImportEntry(ComWrappers_TryGetObject)
    DllImportEntry(ComWrappers_TryGetOrCreateComInterfaceForObject)
    DllImportEntry(ComWrappers_TryGetOrCreateObjectForComInstance)
    DllImportEntry(ComWrappers_SetGlobalInstanceRegisteredForMarshalling)
    DllImportEntry(ComWrappers_SetGlobalInstanceRegisteredForTrackerSupport)
#endif
#if defined(FEATURE_OBJCMARSHAL)
    DllImportEntry(ObjCMarshal_TrySetGlobalMessageSendCallback)
    DllImportEntry(ObjCMarshal_TryInitializeReferenceTracker)
    DllImportEntry(ObjCMarshal_CreateReferenceTrackingHandle)
#endif
#if defined(FEATURE_EVENTSOURCE_XPLAT)
    DllImportEntry(IsEventSourceLoggingEnabled)
    DllImportEntry(LogEventSource)
#endif
#if defined(FEATURE_PERFTRACING)
    DllImportEntry(LogThreadPoolWorkerThreadStart)
    DllImportEntry(LogThreadPoolWorkerThreadStop)
    DllImportEntry(LogThreadPoolWorkerThreadWait)
    DllImportEntry(LogThreadPoolMinMaxThreads)
    DllImportEntry(LogThreadPoolWorkerThreadAdjustmentSample)
    DllImportEntry(LogThreadPoolWorkerThreadAdjustmentAdjustment)
    DllImportEntry(LogThreadPoolWorkerThreadAdjustmentStats)
    DllImportEntry(LogThreadPoolIOEnqueue)
    DllImportEntry(LogThreadPoolIODequeue)
    DllImportEntry(LogThreadPoolIOPack)
    DllImportEntry(LogThreadPoolWorkingThreadCount)
    DllImportEntry(LogContentionLockCreated)
    DllImportEntry(LogContentionStart)
    DllImportEntry(LogContentionStop)
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
#endif
#if defined(TARGET_UNIX)
    DllImportEntry(CloseHandle)
    DllImportEntry(CreateEventExW)
    DllImportEntry(CreateMutexExW)
    DllImportEntry(CreateSemaphoreExW)
    DllImportEntry(FormatMessageW)
    DllImportEntry(FreeEnvironmentStringsW)
    DllImportEntry(GetEnvironmentStringsW)
    DllImportEntry(GetEnvironmentVariableW)
    DllImportEntry(OpenEventW)
    DllImportEntry(OpenMutexW)
    DllImportEntry(OpenSemaphoreW)
    DllImportEntry(OutputDebugStringW)
    DllImportEntry(PAL_CreateMutexW)
    DllImportEntry(PAL_OpenMutexW)
    DllImportEntry(ReleaseMutex)
    DllImportEntry(ReleaseSemaphore)
    DllImportEntry(ResetEvent)
    DllImportEntry(SetEnvironmentVariableW)
    DllImportEntry(SetEvent)
#endif
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    DllImportEntry(X86BaseCpuId)
#endif
    DllImportEntry(StubHelpers_CreateCustomMarshalerHelper)
    DllImportEntry(StubHelpers_SetStringTrailByte)
    DllImportEntry(StubHelpers_ThrowInteropParamException)
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(ObjectMarshaler_ConvertToNative)
    DllImportEntry(ObjectMarshaler_ConvertToManaged)
    DllImportEntry(InterfaceMarshaler_ConvertToNative)
    DllImportEntry(InterfaceMarshaler_ConvertToManaged)
#endif
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    DllImportEntry(ComWeakRefToObject)
    DllImportEntry(ObjectToComWeakRef)
#endif
#ifdef FEATURE_EH_FUNCLETS
    DllImportEntry(SfiInit)
    DllImportEntry(SfiNext)
    DllImportEntry(CallCatchFunclet)
    DllImportEntry(ResumeAtInterceptionLocation)
    DllImportEntry(CallFilterFunclet)
    DllImportEntry(CallFinallyFunclet)
    DllImportEntry(EHEnumInitFromStackFrameIterator)
    DllImportEntry(EHEnumNext)
    DllImportEntry(AppendExceptionStackFrame)
#endif // FEATURE_EH_FUNCLETS
};

const void* QCallResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_QCall, ARRAY_SIZE(s_QCall), name);
}
