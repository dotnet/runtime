/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_H__
#define __MONO_METADATA_ICALL_H__

#include "appdomain-icalls.h"
#include "class.h"
#include "console-io.h"
#include "environment.h"
#include "file-mmap.h"
#include "filewatcher.h"
#include "gc-internals.h"
#include "handle-decl.h"
#include "handle.h"
#include "locales.h"
#include "marshal.h"
#include "monitor.h"
#include "mono-perfcounters.h"
#include "mono-route.h"
#include "object-forward.h"
#include "object-internals.h"
#include "rand.h"
#include "reflection.h"
#include "security-core-clr.h"
#include "security-manager.h"
#include "security.h"
#include "string-icalls.h"
#include "threadpool-io.h"
#include "threadpool.h"
#include "utils/mono-digest.h"
#include "utils/mono-forward-internal.h"
#include "w32event.h"
#include "w32file.h"
#include "w32mutex.h"
#include "w32process.h"
#include "w32semaphore.h"
#include "w32socket.h"

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

// Generate prototypes for coop icall wrappers.
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

// This is sorted.
// grep ICALL_EXPORT | sort | uniq
ICALL_EXPORT MonoArray* ves_icall_System_Environment_GetEnvironmentVariableNames (void);
ICALL_EXPORT MonoArray* ves_icall_System_Environment_GetLogicalDrives (void);
ICALL_EXPORT MonoAssemblyName* ves_icall_System_Reflection_AssemblyName_GetNativeName (MonoAssembly*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_is_subclass_of (MonoType*, MonoType*);
ICALL_EXPORT MonoBoolean ves_icall_System_Array_FastCopy (MonoArray*, int, MonoArray*, int, int);
ICALL_EXPORT MonoBoolean ves_icall_System_Buffer_BlockCopyInternal (MonoArray*, gint32, MonoArray*, gint32, gint32);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_GetIs64BitOperatingSystem (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_get_HasShutdownStarted (void);
ICALL_EXPORT MonoBoolean ves_icall_System_GCHandle_CheckCurrentDomain (guint32 gchandle);
ICALL_EXPORT MonoBoolean ves_icall_System_IO_DriveInfo_GetDiskFreeSpace (MonoString*, guint64*, guint64*, guint64*, gint32*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_AssemblyName_ParseAssemblyName (const char*, MonoAssemblyName*, MonoBoolean*, MonoBoolean* is_token_defined_arg);
ICALL_EXPORT MonoBoolean ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Threading_Thread_YieldInternal (void);
ICALL_EXPORT MonoBoolean ves_icall_System_ValueType_Equals (MonoObject* this_obj, MonoObject* that, MonoArray** fields);
ICALL_EXPORT MonoObject* ves_icall_InternalExecute (MonoReflectionMethod*, MonoObject*, MonoArray*, MonoArray**);
ICALL_EXPORT MonoObject* ves_icall_InternalInvoke (MonoReflectionMethod*, MonoObject*, MonoArray*, MonoException**);
ICALL_EXPORT MonoObject* ves_icall_MonoField_GetRawConstantValue (MonoReflectionField* rfield);
ICALL_EXPORT MonoObject* ves_icall_MonoField_GetValueInternal (MonoReflectionField* field, MonoObject* obj);
ICALL_EXPORT MonoObject* ves_icall_property_info_get_default_value (MonoReflectionProperty*);
ICALL_EXPORT MonoReflectionType* ves_icall_Remoting_RealProxy_InternalGetProxyType (MonoTransparentProxy*);
ICALL_EXPORT MonoString* ves_icall_System_IO_DriveInfo_GetDriveFormat (MonoString*);
ICALL_EXPORT MonoType* mono_ArgIterator_IntGetNextArgType (MonoArgIterator*);
ICALL_EXPORT MonoTypedRef mono_ArgIterator_IntGetNextArg (MonoArgIterator*);
ICALL_EXPORT MonoTypedRef mono_ArgIterator_IntGetNextArgT (MonoArgIterator*, MonoType*);
ICALL_EXPORT MonoTypedRef mono_TypedReference_MakeTypedReferenceInternal (MonoObject*, MonoArray*);
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
ICALL_EXPORT gint ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData (void);
ICALL_EXPORT gint32 ves_icall_System_Buffer_ByteLengthInternal (MonoArray*);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_ProcessorCount (void);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_TickCount (void);
ICALL_EXPORT gint32 ves_icall_System_ValueType_InternalGetHashCode (MonoObject*, MonoArray**);
ICALL_EXPORT gint64 ves_icall_System_DateTime_GetSystemTimeAsFileTime (void);
ICALL_EXPORT gint64 ves_icall_System_Diagnostics_Stopwatch_GetTimestamp (void);
ICALL_EXPORT gint64 ves_icall_System_GC_GetTotalMemory (MonoBoolean forceCollection);
ICALL_EXPORT gint64 ves_icall_System_Threading_Timer_GetTimeMonotonic (void);
ICALL_EXPORT gint8 ves_icall_System_Buffer_GetByteInternal (MonoArray*, gint32);
ICALL_EXPORT gpointer ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle);
ICALL_EXPORT guint32 ves_icall_System_IO_DriveInfo_GetDriveType (MonoString*);
ICALL_EXPORT int ves_icall_Interop_Sys_DoubleToString (double, char*, char*, int);
ICALL_EXPORT int ves_icall_System_Environment_get_Platform (void);
ICALL_EXPORT int ves_icall_System_GC_GetCollectionCount (int);
ICALL_EXPORT int ves_icall_System_GC_GetMaxGeneration (void);
ICALL_EXPORT int ves_icall_System_Threading_Thread_SystemMaxStackSize (void);
ICALL_EXPORT int ves_icall_get_method_attributes (MonoMethod* method);
ICALL_EXPORT void mono_ArgIterator_Setup (MonoArgIterator*, char*, char*);
ICALL_EXPORT void ves_icall_Mono_Runtime_RegisterReportingForNativeLib (const char*, const char*);
ICALL_EXPORT void ves_icall_System_Array_GetGenericValueImpl (MonoArray*, guint32, gpointer);
ICALL_EXPORT void ves_icall_System_Array_SetGenericValueImpl (MonoArray*, guint32, gpointer);
ICALL_EXPORT void ves_icall_System_Buffer_MemcpyInternal (gpointer dest, gconstpointer src, gint32 count);
ICALL_EXPORT void ves_icall_System_Buffer_SetByteInternal (MonoArray*, gint32, gint8);
ICALL_EXPORT void ves_icall_System_Environment_Exit (int);
ICALL_EXPORT void ves_icall_System_GCHandle_FreeHandle (guint32 handle);
ICALL_EXPORT void ves_icall_System_GC_InternalCollect (int generation);
ICALL_EXPORT void ves_icall_System_GC_RecordPressure (gint64);
ICALL_EXPORT void ves_icall_System_GC_WaitForPendingFinalizers (void);
ICALL_EXPORT void ves_icall_System_IO_LogcatTextWriter_Log (const char*, gint32, const char*);
ICALL_EXPORT void ves_icall_System_NumberFormatter_GetFormatterTables (guint64 const**, gint32 const**, gunichar2 const**, gunichar2 const**, gint64 const**, gint32 const**);
ICALL_EXPORT void ves_icall_System_RuntimeFieldHandle_SetValueDirect (MonoReflectionField*, MonoReflectionType*, MonoTypedRef*, MonoObject*, MonoReflectionType*);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_Memmove (guint8*, guint8*, guint);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_Memmove_wbarrier (guint8*, guint8*, guint, MonoType*);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ZeroMemory (guint8*, guint);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ecvt_s(char*, size_t, double, int, int*, int*);

#endif // __MONO_METADATA_ICALL_H__
