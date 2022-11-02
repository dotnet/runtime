// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Describes all of the P/Invokes to the special QCall module that resolves to internal runtime methods.
#include "common.h"

//
// Headers for all ECall entrypoints
//
#include "arraynative.h"
#include "stringnative.h"
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

static const Entry s_QCall[] =
{
    DllImportEntry(Enum_GetValuesAndNames)
    DllImportEntry(DebugDebugger_Launch)
    DllImportEntry(DebugDebugger_Log)
    DllImportEntry(Environment_Exit)
    DllImportEntry(Environment_GetProcessorCount)
    DllImportEntry(ExceptionNative_GetMessageFromNativeResources)
    DllImportEntry(RuntimeTypeHandle_CreateInstanceForAnotherGenericParameter)
    DllImportEntry(QCall_GetGCHandleForTypeHandle)
    DllImportEntry(QCall_FreeGCHandleForTypeHandle)
    DllImportEntry(MethodTable_AreTypesEquivalent)
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
    DllImportEntry(RuntimeTypeHandle_GetTypeByName)
    DllImportEntry(RuntimeTypeHandle_GetTypeByNameUsingCARules)
    DllImportEntry(RuntimeTypeHandle_GetInstantiation)
    DllImportEntry(RuntimeTypeHandle_Instantiate)
    DllImportEntry(RuntimeTypeHandle_GetGenericTypeDefinition)
    DllImportEntry(RuntimeTypeHandle_GetActivationInfo)
    DllImportEntry(RuntimeTypeHandle_AllocateTypeAssociatedMemory)
    DllImportEntry(RuntimeMethodHandle_ConstructInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetFunctionPointer)
    DllImportEntry(RuntimeMethodHandle_GetIsCollectible)
    DllImportEntry(RuntimeMethodHandle_GetMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_GetTypicalMethodDefinition)
    DllImportEntry(RuntimeMethodHandle_StripMethodInstantiation)
    DllImportEntry(RuntimeMethodHandle_IsCAVisibleFromDecoratedType)
    DllImportEntry(RuntimeMethodHandle_Destroy)
    DllImportEntry(RuntimeModule_GetType)
    DllImportEntry(RuntimeModule_GetScopeName)
    DllImportEntry(RuntimeModule_GetFullyQualifiedName)
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
    DllImportEntry(MdUtf8String_HashCaseInsensitive)
    DllImportEntry(TypeName_ReleaseTypeNameParser)
    DllImportEntry(TypeName_CreateTypeNameParser)
    DllImportEntry(TypeName_GetNames)
    DllImportEntry(TypeName_GetTypeArguments)
    DllImportEntry(TypeName_GetModifiers)
    DllImportEntry(TypeName_GetAssemblyName)
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
    DllImportEntry(AssemblyNative_GetType)
    DllImportEntry(AssemblyNative_GetForwardedType)
    DllImportEntry(AssemblyNative_GetManifestResourceInfo)
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
    DllImportEntry(AssemblyNative_GetAssemblyCount)
    DllImportEntry(AssemblyNative_GetEntryAssembly)
    DllImportEntry(AssemblyNative_GetExecutingAssembly)
#if defined(FEATURE_MULTICOREJIT)
    DllImportEntry(MultiCoreJIT_InternalSetProfileRoot)
    DllImportEntry(MultiCoreJIT_InternalStartProfile)
#endif
    DllImportEntry(LoaderAllocator_Destroy)
    DllImportEntry(AppDomain_CreateDynamicAssembly)
    DllImportEntry(ThreadNative_Start)
    DllImportEntry(ThreadNative_UninterruptibleSleep0)
    DllImportEntry(ThreadNative_InformThreadNameChange)
    DllImportEntry(ThreadNative_YieldThread)
    DllImportEntry(ThreadNative_GetCurrentOSThreadId)
    DllImportEntry(ThreadNative_Abort)
    DllImportEntry(ThreadNative_ResetAbort)
#ifdef TARGET_UNIX
    DllImportEntry(WaitHandle_CorWaitOnePrioritizedNative)
#endif
    DllImportEntry(ClrConfig_GetConfigBoolValue)
    DllImportEntry(Buffer_Clear)
    DllImportEntry(Buffer_MemMove)
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
    DllImportEntry(MarshalNative_Prelink)
    DllImportEntry(MarshalNative_IsBuiltInComSupported)
    DllImportEntry(MarshalNative_GetHINSTANCE)
#ifdef _DEBUG
    DllImportEntry(MarshalNative_GetIsInCooperativeGCModeFunctionPointer)
#endif
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(MarshalNative_GetTypeFromCLSID)
#endif
    DllImportEntry(NativeLibrary_LoadFromPath)
    DllImportEntry(NativeLibrary_LoadByName)
    DllImportEntry(NativeLibrary_FreeLib)
    DllImportEntry(NativeLibrary_GetSymbol)
    DllImportEntry(GetTypeLoadExceptionMessage)
    DllImportEntry(GetFileLoadExceptionMessage)
    DllImportEntry(FileLoadException_GetMessageForHR)
    DllImportEntry(Interlocked_MemoryBarrierProcessWide)
    DllImportEntry(ObjectNative_GetMonitorLockContentionCount)
    DllImportEntry(ReflectionInvocation_RunClassConstructor)
    DllImportEntry(ReflectionInvocation_RunModuleConstructor)
    DllImportEntry(ReflectionInvocation_CompileMethod)
    DllImportEntry(ReflectionInvocation_PrepareMethod)
    DllImportEntry(ReflectionSerialization_GetUninitializedObject)
#if defined(FEATURE_COMWRAPPERS)
    DllImportEntry(ComWrappers_GetIUnknownImpl)
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
    DllImportEntry(ReleaseMutex)
    DllImportEntry(ReleaseSemaphore)
    DllImportEntry(ResetEvent)
    DllImportEntry(SetEnvironmentVariableW)
    DllImportEntry(SetEvent)
#endif
#if defined(TARGET_X86) || defined(TARGET_AMD64)
    DllImportEntry(X86BaseCpuId)
#endif
#if defined(FEATURE_COMINTEROP)
    DllImportEntry(InterfaceMarshaler__ClearNative)
#endif
#if defined(FEATURE_COMINTEROP) || defined(FEATURE_COMWRAPPERS)
    DllImportEntry(ComWeakRefToObject)
    DllImportEntry(ObjectToComWeakRef)
#endif
};

const void* QCallResolveDllImport(const char* name)
{
    return minipal_resolve_dllimport(s_QCall, ARRAY_SIZE(s_QCall), name);
}
