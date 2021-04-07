/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_DECL_H__
#define __MONO_METADATA_ICALL_DECL_H__

#include "appdomain-icalls.h"
#include "class.h"
#include "environment.h"
#include "file-mmap.h"
#include "gc-internals.h"
#include "handle-decl.h"
#include "handle.h"
#include "marshal.h"
#include "monitor.h"
#include "mono-perfcounters.h"
#include "object-forward.h"
#include "object-internals.h"
#include "reflection.h"
#include "security-core-clr.h"
#include "security-manager.h"
#include "security.h"
#include "string-icalls.h"
#include "mono/utils/mono-digest.h"
#include "mono/utils/mono-forward-internal.h"
#include "w32event.h"
#include "w32file.h"
#include "mono/utils/mono-proclib.h"

/* From MonoProperty.cs */
typedef enum {
	PInfo_Attributes = 1,
	PInfo_GetMethod  = 1 << 1,
	PInfo_SetMethod  = 1 << 2,
	PInfo_ReflectedType = 1 << 3,
	PInfo_DeclaringType = 1 << 4,
	PInfo_Name = 1 << 5
} PInfo;

#include "icall-table.h"

#define NOHANDLES(inner) inner
#define HANDLES_REUSE_WRAPPER(...) /* nothing */

// Generate prototypes for coop icall wrappers and coop icalls.
#define MONO_HANDLE_REGISTER_ICALL(func, ret, nargs, argtypes) \
	MONO_HANDLE_DECLARE (,,func ## _impl, ret, nargs, argtypes); \
	MONO_HANDLE_REGISTER_ICALL_DECLARE_RAW (func, ret, nargs, argtypes);
#define ICALL_TYPE(id, name, first)	/* nothing */
#define ICALL(id, name, func) 		/* nothing */
#define HANDLES(id, name, func, ret, nargs, argtypes) \
	MONO_HANDLE_DECLARE_RAW (id, name, func, ret, nargs, argtypes); \
	MONO_HANDLE_DECLARE (id, name, func, ret, nargs, argtypes);
#include "icall-def.h"
#undef ICALL_TYPE
#undef ICALL
#undef HANDLES
#undef HANDLES_REUSE_WRAPPER
#undef NOHANDLES
#undef MONO_HANDLE_REGISTER_ICALL

// This is sorted.
// grep ICALL_EXPORT | sort | uniq
ICALL_EXPORT MonoAssemblyName* ves_icall_System_Reflection_AssemblyName_GetNativeName (MonoAssembly*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_is_subclass_of (MonoType*, MonoType*);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_GetIs64BitOperatingSystem (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_get_HasShutdownStarted (void);
ICALL_EXPORT MonoBoolean ves_icall_System_GCHandle_CheckCurrentDomain (MonoGCHandle gchandle);
ICALL_EXPORT MonoBoolean ves_icall_System_IO_DriveInfo_GetDiskFreeSpace (const gunichar2*, gint32, guint64*, guint64*, guint64*, gint32*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_AssemblyName_ParseAssemblyName (const char*, MonoAssemblyName*, MonoBoolean*, MonoBoolean* is_token_defined_arg);
ICALL_EXPORT MonoBoolean ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Threading_Thread_YieldInternal (void);
ICALL_EXPORT MonoThread *ves_icall_System_Threading_Thread_GetCurrentThread (void);
ICALL_EXPORT void ves_icall_System_ArgIterator_Setup (MonoArgIterator*, char*, char*);
ICALL_EXPORT MonoType* ves_icall_System_ArgIterator_IntGetNextArgType (MonoArgIterator*);
ICALL_EXPORT void ves_icall_System_ArgIterator_IntGetNextArg (MonoArgIterator*, MonoTypedRef*);
ICALL_EXPORT void ves_icall_System_ArgIterator_IntGetNextArgWithType (MonoArgIterator*, MonoTypedRef*, MonoType*);
ICALL_EXPORT double ves_icall_System_Math_Abs_double (double);
ICALL_EXPORT double ves_icall_System_Math_Acos (double);
ICALL_EXPORT double ves_icall_System_Math_Acosh (double);
ICALL_EXPORT double ves_icall_System_Math_Asin (double);
ICALL_EXPORT double ves_icall_System_Math_Asinh (double);
ICALL_EXPORT double ves_icall_System_Math_Atan (double);
ICALL_EXPORT double ves_icall_System_Math_Atan2 (double, double);
ICALL_EXPORT double ves_icall_System_Math_Atanh (double);
ICALL_EXPORT double ves_icall_System_Math_Cbrt (double);
ICALL_EXPORT double ves_icall_System_Math_Ceiling (double);
ICALL_EXPORT double ves_icall_System_Math_Cos (double);
ICALL_EXPORT double ves_icall_System_Math_Cosh (double);
ICALL_EXPORT double ves_icall_System_Math_Exp (double);
ICALL_EXPORT double ves_icall_System_Math_FMod (double, double);
ICALL_EXPORT double ves_icall_System_Math_Floor (double);
ICALL_EXPORT double ves_icall_System_Math_Log (double);
ICALL_EXPORT double ves_icall_System_Math_Log10 (double);
ICALL_EXPORT double ves_icall_System_Math_ModF (double, double*);
ICALL_EXPORT double ves_icall_System_Math_Pow (double, double);
ICALL_EXPORT double ves_icall_System_Math_Round (double);
ICALL_EXPORT double ves_icall_System_Math_Sin (double);
ICALL_EXPORT double ves_icall_System_Math_Sinh (double);
ICALL_EXPORT double ves_icall_System_Math_Sqrt (double);
ICALL_EXPORT double ves_icall_System_Math_Tan (double);
ICALL_EXPORT double ves_icall_System_Math_Tanh (double);
ICALL_EXPORT float ves_icall_System_MathF_Acos (float);
ICALL_EXPORT float ves_icall_System_MathF_Acosh (float);
ICALL_EXPORT float ves_icall_System_MathF_Asin (float);
ICALL_EXPORT float ves_icall_System_MathF_Asinh (float);
ICALL_EXPORT float ves_icall_System_MathF_Atan (float);
ICALL_EXPORT float ves_icall_System_MathF_Atan2 (float, float);
ICALL_EXPORT float ves_icall_System_MathF_Atanh (float);
ICALL_EXPORT float ves_icall_System_MathF_Cbrt (float);
ICALL_EXPORT float ves_icall_System_MathF_Ceiling (float);
ICALL_EXPORT float ves_icall_System_MathF_Cos (float);
ICALL_EXPORT float ves_icall_System_MathF_Cosh (float);
ICALL_EXPORT float ves_icall_System_MathF_Exp (float);
ICALL_EXPORT float ves_icall_System_MathF_FMod (float, float);
ICALL_EXPORT float ves_icall_System_MathF_Floor (float);
ICALL_EXPORT float ves_icall_System_MathF_Log (float);
ICALL_EXPORT float ves_icall_System_MathF_Log10 (float);
ICALL_EXPORT float ves_icall_System_MathF_ModF (float, float*);
ICALL_EXPORT float ves_icall_System_MathF_Pow (float, float);
ICALL_EXPORT float ves_icall_System_MathF_Sin (float);
ICALL_EXPORT float ves_icall_System_MathF_Sinh (float);
ICALL_EXPORT float ves_icall_System_MathF_Sqrt (float);
ICALL_EXPORT float ves_icall_System_MathF_Tan (float);
ICALL_EXPORT float ves_icall_System_MathF_Tanh (float);
ICALL_EXPORT float ves_icall_System_Math_Abs_single (float);
ICALL_EXPORT gint32 ves_icall_System_Math_ILogB (double);
ICALL_EXPORT double ves_icall_System_Math_Log2 (double);
ICALL_EXPORT double ves_icall_System_Math_FusedMultiplyAdd (double, double, double);
ICALL_EXPORT gint32 ves_icall_System_MathF_ILogB (float);
ICALL_EXPORT float ves_icall_System_MathF_Log2 (float);
ICALL_EXPORT float ves_icall_System_MathF_FusedMultiplyAdd (float, float, float);
ICALL_EXPORT gint ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData (void);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_ProcessorCount (void);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_TickCount (void);
ICALL_EXPORT gint64 ves_icall_System_Environment_get_TickCount64 (void);
ICALL_EXPORT gint64 ves_icall_System_DateTime_GetSystemTimeAsFileTime (void);
ICALL_EXPORT gint64 ves_icall_System_Diagnostics_Process_GetProcessData (int, gint32, MonoProcessError*);
ICALL_EXPORT gint64 ves_icall_System_Diagnostics_Stopwatch_GetTimestamp (void);
ICALL_EXPORT gint64 ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection);
ICALL_EXPORT gint64 ves_icall_System_Threading_Timer_GetTimeMonotonic (void);
ICALL_EXPORT gpointer ves_icall_System_GCHandle_GetAddrOfPinnedObject (MonoGCHandle handle);
ICALL_EXPORT int ves_icall_Interop_Sys_DoubleToString (double, char*, char*, int);
ICALL_EXPORT int ves_icall_System_GC_GetCollectionCount (int);
ICALL_EXPORT int ves_icall_System_GC_GetMaxGeneration (void);
ICALL_EXPORT gint64 ves_icall_System_GC_GetAllocatedBytesForCurrentThread (void);
ICALL_EXPORT int ves_icall_System_Threading_Thread_SystemMaxStackSize (void);
ICALL_EXPORT int ves_icall_get_method_attributes (MonoMethod* method);
ICALL_EXPORT void ves_icall_Mono_Runtime_RegisterReportingForNativeLib (const char*, const char*);
ICALL_EXPORT void ves_icall_Mono_Runtime_RegisterReportingForAllNativeLibs (void);
ICALL_EXPORT void ves_icall_System_Array_GetGenericValue_icall (MonoArray**, guint32, gpointer);
ICALL_EXPORT void ves_icall_System_Array_SetGenericValue_icall (MonoArray**, guint32, gpointer);
ICALL_EXPORT void ves_icall_System_Buffer_MemcpyInternal (gpointer dest, gconstpointer src, gint32 count);
ICALL_EXPORT void ves_icall_System_Environment_Exit (int);
ICALL_EXPORT void ves_icall_System_GCHandle_FreeHandle (MonoGCHandle handle);
ICALL_EXPORT void ves_icall_System_GC_InternalCollect (int generation);
ICALL_EXPORT void ves_icall_System_GC_RecordPressure (gint64);
ICALL_EXPORT void ves_icall_System_GC_WaitForPendingFinalizers (void);
ICALL_EXPORT void ves_icall_System_GC_GetGCMemoryInfo (gint64*, gint64*, gint64*, gint64*, gint64*);

ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_Memmove (guint8*, guint8*, size_t);
ICALL_EXPORT void ves_icall_System_Buffer_BulkMoveWithWriteBarrier (guint8 *, guint8 *, size_t, MonoType *);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ZeroMemory (guint8*, size_t);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ecvt_s(char*, size_t, double, int, int*, int*);

ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_CloseProcess (gpointer handle);
ICALL_EXPORT gpointer ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess (void);
ICALL_EXPORT gint32 ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcessId (void);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess (gpointer handle, gint32 *exitcode);
ICALL_EXPORT gint32 ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass (gpointer handle);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes (gpointer handle, gint64 *creation_time, gint64 *exit_time, gint64 *kernel_time, gint64 *user_time);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize (gpointer handle, gsize *min, gsize *max);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass (gpointer handle, gint32 priorityClass);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize (gpointer handle, gsize min, gsize max);
ICALL_EXPORT MonoBoolean ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess (gpointer handle, gint32 exitcode);
ICALL_EXPORT gint32 ves_icall_Microsoft_Win32_NativeMethods_WaitForInputIdle (gpointer handle, gint32 milliseconds);

ICALL_EXPORT void ves_icall_Mono_Runtime_AnnotateMicrosoftTelemetry (const char *key, const char *value);
ICALL_EXPORT void ves_icall_Mono_Runtime_DisableMicrosoftTelemetry (void);
ICALL_EXPORT int ves_icall_Mono_Runtime_CheckCrashReportingLog (const char *directory, MonoBoolean clear);
ICALL_EXPORT void ves_icall_Mono_Runtime_EnableCrashReportingLog (const char *directory);

ICALL_EXPORT void ves_icall_System_Array_InternalCreate (MonoArray *volatile* result, MonoType* type, gint32 rank, gint32* pLengths, gint32* pLowerBounds);
ICALL_EXPORT MonoBoolean ves_icall_System_Array_CanChangePrimitive (MonoReflectionType *volatile* ref_src_type_handle, MonoReflectionType *volatile* ref_dst_type_handle, MonoBoolean reliable);

ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Debugger_IsAttached_internal (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Debugger_IsLogging (void);
ICALL_EXPORT void ves_icall_System_Diagnostics_Debugger_Log (int level, MonoString *volatile* category, MonoString *volatile* message);

ICALL_EXPORT intptr_t ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DefineEvent (intptr_t prov_handle, uint32_t event_id, int64_t keywords, uint32_t event_version, uint32_t level, const uint8_t *metadata, uint32_t metadata_len);
ICALL_EXPORT void ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DeleteProvider (intptr_t prov_handle);
ICALL_EXPORT void ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Disable (uint64_t session_id);
ICALL_EXPORT uint64_t ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Enable (const_gunichar2_ptr output_file, int32_t format, uint32_t circular_buffer_size_mb, const void *providers, uint32_t num_providers);
ICALL_EXPORT int32_t ves_icall_System_Diagnostics_Tracing_EventPipeInternal_EventActivityIdControl (uint32_t control_code, uint8_t *activity_id);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetNextEvent (uint64_t session_id, void *instance);
ICALL_EXPORT intptr_t ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetProvider (const_gunichar2_ptr provider_name);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetSessionInfo (uint64_t session_id, void *session_info);
ICALL_EXPORT intptr_t ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetWaitHandle (uint64_t session_id);
ICALL_EXPORT void ves_icall_System_Diagnostics_Tracing_EventPipeInternal_WriteEventData (intptr_t event_handle, void *event_data, uint32_t event_data_len, const uint8_t *activity_id, const uint8_t *related_activity_id);

ICALL_EXPORT void ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (GPtrArray *ptr_array);
ICALL_EXPORT void ves_icall_Mono_RuntimeMarshal_FreeAssemblyName (MonoAssemblyName *aname, MonoBoolean free_struct);
ICALL_EXPORT void ves_icall_Mono_SafeStringMarshal_GFree (void *c_str);
ICALL_EXPORT char* ves_icall_Mono_SafeStringMarshal_StringToUtf8 (MonoString *volatile* s);
ICALL_EXPORT MonoType* ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (MonoClass *klass);

ICALL_EXPORT MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (const gunichar2*);
ICALL_EXPORT MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (const gunichar2*);
ICALL_EXPORT MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (const gunichar2*);
ICALL_EXPORT MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (const gunichar2*);
ICALL_EXPORT MonoBoolean ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (const gunichar2*);
ICALL_EXPORT void ves_icall_Mono_Interop_ComInteropProxy_AddProxy  (gpointer pUnk, MonoComInteropProxy *volatile* proxy_handle);
ICALL_EXPORT void ves_icall_Mono_Interop_ComInteropProxy_FindProxy (gpointer pUnk, MonoComInteropProxy *volatile* proxy_handle);

ICALL_EXPORT gpointer   ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem		(int);
ICALL_EXPORT gpointer   ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMemSize	(gsize);
ICALL_EXPORT gpointer   ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal		(gsize);
ICALL_EXPORT gpointer   ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem	(gpointer ptr, int size);
ICALL_EXPORT gpointer   ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal		(gpointer, gsize);
ICALL_EXPORT      char* ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi    (const gunichar2*, int);
ICALL_EXPORT gunichar2*	ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni	(const gunichar2*, int);

ICALL_EXPORT gpointer ves_icall_System_Threading_LowLevelLifoSemaphore_InitInternal (void);
ICALL_EXPORT void     ves_icall_System_Threading_LowLevelLifoSemaphore_DeleteInternal (gpointer sem_ptr);
ICALL_EXPORT gint32   ves_icall_System_Threading_LowLevelLifoSemaphore_TimedWaitInternal (gpointer sem_ptr, gint32 timeout_ms);
ICALL_EXPORT void     ves_icall_System_Threading_LowLevelLifoSemaphore_ReleaseInternal (gpointer sem_ptr, gint32 count);

#ifdef TARGET_AMD64
ICALL_EXPORT void ves_icall_System_Runtime_Intrinsics_X86_X86Base___cpuidex (int abcd[4], int function_id, int subfunction_id);
#endif

#ifdef ENABLE_METADATA_UPDATE
ICALL_EXPORT void ves_icall_AssemblyExtensions_ApplyUpdate (MonoAssembly *assm, gconstpointer dmeta_bytes, int32_t dmeta_len, gconstpointer dil_bytes, int32_t dil_len, gconstpointer dpdb_bytes, int32_t dpdb_len);
#endif

#endif // __MONO_METADATA_ICALL_DECL_H__
