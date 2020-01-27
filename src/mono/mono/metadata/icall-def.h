/**
 * \file
 * This file contains the default set of the mono internal calls.
 * Each type that has internal call methods must be declared here
 * with the ICALL_TYPE macro as follows:
 *
 * 	ICALL_TYPE(typeid, typename, first_icall_id)
 *
 * typeid must be a C symbol name unique to the type, don't worry about namespace
 * 	pollution, since it will be automatically prefixed to avoid it.
 * typename is a C string containing the full name of the type
 * first_icall_id is the symbol ID of the first internal call of the declared
 * 	type (see below)
 *
 * The list of internal calls of the methods of a type must follow the
 * type declaration. Each internal call is defined by the following macro:
 *
 * 	ICALL(icallid, methodname, cfuncptr)
 *
 * icallid must be a C symbol, unique for each icall defined in this file and
 * typically equal to the typeid + '_' + a sequential number.
 * methodname is a C string defining the method name and the optional signature
 * (the signature is required only when several internal calls in the type
 * have the same name)
 * cfuncptr is the C function that implements the internal call. Note that this
 * file is included at the end of metadata/icall.c, so the C function must be
 * visible to the compiler there.
 *
 * *** Adding a new internal call ***
 * Remember that ICALL_TYPE declarations must be kept sorted wrt each other
 * ICALL_TYPE declaration. The same happens for ICALL declarations, but only
 * limited to the icall list of each type. The sorting is based on the type or
 * method name.
 * When adding a new icall, make sure it is inserted correctly in the list and
 * that it defines a unique ID. ID are currently numbered and ordered, but if
 * you need to insert a method in the middle, don't bother renaming all the symbols.
 * Remember to change also the first_icall_id argument in the ICALL_TYPE
 * declaration if you add a new icall at the beginning of a type's icall list.
 *
 *
 * *** (Experimental) Cooperative GC support via Handles and MonoError ***
 * An icall can use the coop GC handles infrastructure from handles.h to avoid some
 * boilerplate when manipulating managed objects from runtime code and to use MonoError for
 * threading exceptions out to managed callerrs:
 *
 * HANDLES(icallid, methodname, cfuncptr, return-type, number-of-parameters, (parameter-types))
 * types:
 *   managed types are just MonoObject, MonoString, etc. `*` and Handle prefix are appended automatically.
 *   Types must be single identifiers, and be handled in icall-table.h.
 *   MonoError is added to the list automatically.
 *   A function with no parameters is "0, ()"
 *   "Out" and "InOut" types are appended with "Out" and "InOut". In is assumed.
 *   Out and InOut raw pointers get "**" appended. In gets just "*".
 *   Out/InOut only applied to managed pointers/handles.
 *   Things like "int*" are supported by typedefs like "typedef int *int_ptr".
 *   "void*" and "HANDLE" are written "gpointer".
 *   The list of available types is in icall-table.h.
 *   Using a type not there errors unceremoniously.
 *
 * An icall with a HANDLES() declaration wrapped around it will have a generated wrapper
 * that:
 *   (1) Updates the coop handle stack on entry and exit
 *   (2) Call the cfuncptr with a new signature:
 *     (a) All managed object reference in arguments will be wrapped in a type-unsafe handle
 *         (i.e., MonoString* becomes struct { MonoString* raw }* aka MonoRawHandle*)
 *     (b) the same for the return value (MonoObject* return becomes MonoObjectHandle)
 *     (c) An additional final argument is added of type MonoError*
 *     example:    class object {
 *                     [MethodImplOptions(InternalCall)]
 *                     String some_icall (object[] x);
 *                 }
 *     should be implemented as:
 *        MonoStringHandle some_icall (MonoObjectHandle this_handle, MonoArrayHandle x_handle, MonoError *error);
 *   (3) The wrapper will automatically call mono_error_set_pending_exception (error) and raise the resulting exception.
 * Note:  valuetypes use the same calling convention as normal.
 *
 * HANDLES() wrappers are generated dynamically by marshal-ilgen.c, using metadata to see types, producing type-unsafe handles (void*).
 * HANDLES() additional small wrappers are generated statically by icall-table.h, using the signatures here, producing
 * type-safe handles, i.e. MonoString* => MonoRawHandle* => struct { MonoString** raw }.
 *
 */

//
// _ref means marshal-ilgen.c created a handle for an interior pointer.
// _ptr means marshal-ilgen.c passed the parameter through unchanged.
// At the C level, they are the same.
//
// If the C# managed declaration for an icall, with 7 parameters, is:
// 	object your_internal_call (int x, object y, ref int z, IntPtr p, ref MyStruct c, ref MyClass, out string s);
//
// you should write:
// 	HANDLES(ID_n, "your_internal_call", "ves_icall_your_internal_call", MonoObject, 7, (gint32, MonoObject, gint32_ref, gpointer, MyStruct_ref, MyClassInOut, MonoStringOut))
//
// 7 is the number of parameters, the length of the last macro parameter.
// IntPtr is unchecked. You could also say gsize or gssize.
//
// and marshal-ilgen.c will generate a call to
// 	MonoRawHandle* ves_icall_your_internal_call_raw (gint32, MonoRawHandle*, gint32*, gpointer, MyStruct*, MonoRawHandle*, MonoRawHandle*);
//
// whose body will be generated by the HANDLES() macro, and which will call the following function that you have to implement:
// 	MonoObjectHandle ves_icall_your_internal_call (gint32, MonoObjectHandle, gint32*, gpointer, MyStruct*, MyClassHandleInOut, MonoStringOut, MonoError *error);
//
// Note the extra MonoError* argument.
// Note that "ref" becomes "HandleInOut" for managed types.
// "_ref" becomes "*" for unmanaged types.
// "_out" becomes "HandleOut" or "*".
// "HandleIn" is the default for managed types, and is just called "Handle".
//

#if ENABLE_NETCORE
#include "icall-def-netcore.h"
#else

ICALL_TYPE(CLR_INTEROP_SYS, "Interop/Sys", CLR_INTEROP_SYS_1)
NOHANDLES(ICALL(CLR_INTEROP_SYS_1, "DoubleToString", ves_icall_Interop_Sys_DoubleToString))

ICALL_TYPE(NATIVEMETHODS, "Microsoft.Win32.NativeMethods", NATIVEMETHODS_1)
NOHANDLES(ICALL(NATIVEMETHODS_1, "CloseProcess", ves_icall_Microsoft_Win32_NativeMethods_CloseProcess))
NOHANDLES(ICALL(NATIVEMETHODS_2, "GetCurrentProcess", ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess))
NOHANDLES(ICALL(NATIVEMETHODS_3, "GetCurrentProcessId", ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcessId))
NOHANDLES(ICALL(NATIVEMETHODS_4, "GetExitCodeProcess", ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess))
NOHANDLES(ICALL(NATIVEMETHODS_5, "GetPriorityClass", ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass))
NOHANDLES(ICALL(NATIVEMETHODS_6, "GetProcessTimes", ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes))
NOHANDLES(ICALL(NATIVEMETHODS_7, "GetProcessWorkingSetSize", ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize))
NOHANDLES(ICALL(NATIVEMETHODS_8, "SetPriorityClass", ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass))
NOHANDLES(ICALL(NATIVEMETHODS_9, "SetProcessWorkingSetSize", ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize))
NOHANDLES(ICALL(NATIVEMETHODS_10, "TerminateProcess", ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess))
NOHANDLES(ICALL(NATIVEMETHODS_11, "WaitForInputIdle", ves_icall_Microsoft_Win32_NativeMethods_WaitForInputIdle))

#ifndef DISABLE_COM
ICALL_TYPE(COMPROX, "Mono.Interop.ComInteropProxy", COMPROX_1)
NOHANDLES(ICALL(COMPROX_1, "AddProxy", ves_icall_Mono_Interop_ComInteropProxy_AddProxy))
NOHANDLES(ICALL(COMPROX_2, "FindProxy", ves_icall_Mono_Interop_ComInteropProxy_FindProxy))
#endif

ICALL_TYPE(TLS_PROVIDER_FACTORY, "Mono.Net.Security.MonoTlsProviderFactory", TLS_PROVIDER_FACTORY_1)
NOHANDLES(ICALL(TLS_PROVIDER_FACTORY_1, "IsBtlsSupported", ves_icall_Mono_TlsProviderFactory_IsBtlsSupported))

ICALL_TYPE(RUNTIME, "Mono.Runtime", RUNTIME_20)
NOHANDLES(ICALL(RUNTIME_20, "AnnotateMicrosoftTelemetry_internal", ves_icall_Mono_Runtime_AnnotateMicrosoftTelemetry))
NOHANDLES(ICALL(RUNTIME_19, "CheckCrashReportLog_internal", ves_icall_Mono_Runtime_CheckCrashReportingLog))
NOHANDLES(ICALL(RUNTIME_1, "DisableMicrosoftTelemetry", ves_icall_Mono_Runtime_DisableMicrosoftTelemetry))
HANDLES(RUNTIME_15, "DumpStateSingle_internal", ves_icall_Mono_Runtime_DumpStateSingle, MonoString, 2, (guint64_ref, guint64_ref))
HANDLES(RUNTIME_16, "DumpStateTotal_internal", ves_icall_Mono_Runtime_DumpStateTotal, MonoString, 2, (guint64_ref, guint64_ref))
NOHANDLES(ICALL(RUNTIME_18, "EnableCrashReportLog_internal", ves_icall_Mono_Runtime_EnableCrashReportingLog))
HANDLES(RUNTIME_2, "EnableMicrosoftTelemetry_internal", ves_icall_Mono_Runtime_EnableMicrosoftTelemetry, void, 6, (const_char_ptr, const_char_ptr, const_char_ptr, const_char_ptr, const_char_ptr, const_char_ptr))
HANDLES(RUNTIME_3, "ExceptionToState_internal", ves_icall_Mono_Runtime_ExceptionToState, MonoString, 3, (MonoException, guint64_ref, guint64_ref))
HANDLES(RUNTIME_4, "GetDisplayName", ves_icall_Mono_Runtime_GetDisplayName, MonoString, 0, ())
HANDLES(RUNTIME_12, "GetNativeStackTrace", ves_icall_Mono_Runtime_GetNativeStackTrace, MonoString, 1, (MonoException))
NOHANDLES(ICALL(RUNTIME_21, "RegisterReportingForAllNativeLibs_internal", ves_icall_Mono_Runtime_RegisterReportingForAllNativeLibs))
NOHANDLES(ICALL(RUNTIME_17, "RegisterReportingForNativeLib_internal", ves_icall_Mono_Runtime_RegisterReportingForNativeLib))
HANDLES(RUNTIME_13, "SendMicrosoftTelemetry_internal", ves_icall_Mono_Runtime_SendMicrosoftTelemetry, void, 3, (const_char_ptr, guint64, guint64))
HANDLES(RUNTIME_14, "WriteStateToFile_internal", ves_icall_Mono_Runtime_DumpTelemetry, void, 3, (const_char_ptr, guint64, guint64))

ICALL_TYPE(RTCLASS, "Mono.RuntimeClassHandle", RTCLASS_1)
NOHANDLES(ICALL(RTCLASS_1, "GetTypeFromClass", ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass))

ICALL_TYPE(RTPTRARRAY, "Mono.RuntimeGPtrArrayHandle", RTPTRARRAY_1)
NOHANDLES(ICALL(RTPTRARRAY_1, "GPtrArrayFree", ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree))

ICALL_TYPE(RTMARSHAL, "Mono.RuntimeMarshal", RTMARSHAL_1)
NOHANDLES(ICALL(RTMARSHAL_1, "FreeAssemblyName", ves_icall_Mono_RuntimeMarshal_FreeAssemblyName))

ICALL_TYPE(SAFESTRMARSHAL, "Mono.SafeStringMarshal", SAFESTRMARSHAL_1)
NOHANDLES(ICALL(SAFESTRMARSHAL_1, "GFree", ves_icall_Mono_SafeStringMarshal_GFree))
NOHANDLES(ICALL(SAFESTRMARSHAL_2, "StringToUtf8_icall", ves_icall_Mono_SafeStringMarshal_StringToUtf8))

#ifndef PLATFORM_RO_FS
ICALL_TYPE(KPAIR, "Mono.Security.Cryptography.KeyPairPersistence", KPAIR_1)
NOHANDLES(ICALL(KPAIR_1, "_CanSecure", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure))
NOHANDLES(ICALL(KPAIR_2, "_IsMachineProtected", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected))
NOHANDLES(ICALL(KPAIR_3, "_IsUserProtected", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected))
NOHANDLES(ICALL(KPAIR_4, "_ProtectMachine", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine))
NOHANDLES(ICALL(KPAIR_5, "_ProtectUser", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser))
#endif /* !PLATFORM_RO_FS */

ICALL_TYPE(APPDOM, "System.AppDomain", APPDOM_23)
HANDLES(APPDOM_23, "DoUnhandledException", ves_icall_System_AppDomain_DoUnhandledException, void, 2, (MonoAppDomain, MonoException))
HANDLES(APPDOM_1, "ExecuteAssembly", ves_icall_System_AppDomain_ExecuteAssembly, gint32, 3, (MonoAppDomain, MonoReflectionAssembly, MonoArray))
HANDLES(APPDOM_2, "GetAssemblies", ves_icall_System_AppDomain_GetAssemblies, MonoArray, 2, (MonoAppDomain, MonoBoolean))
HANDLES(APPDOM_3, "GetData", ves_icall_System_AppDomain_GetData, MonoObject, 2, (MonoAppDomain, MonoString))
HANDLES(APPDOM_4, "InternalGetContext", ves_icall_System_AppDomain_InternalGetContext, MonoAppContext, 0, ())
HANDLES(APPDOM_5, "InternalGetDefaultContext", ves_icall_System_AppDomain_InternalGetDefaultContext, MonoAppContext, 0, ())
HANDLES(APPDOM_6, "InternalGetProcessGuid", ves_icall_System_AppDomain_InternalGetProcessGuid, MonoString, 1, (MonoString))
HANDLES(APPDOM_7, "InternalIsFinalizingForUnload", ves_icall_System_AppDomain_InternalIsFinalizingForUnload, MonoBoolean, 1, (gint32))
HANDLES(APPDOM_8, "InternalPopDomainRef", ves_icall_System_AppDomain_InternalPopDomainRef, void, 0, ())
HANDLES(APPDOM_9, "InternalPushDomainRef", ves_icall_System_AppDomain_InternalPushDomainRef, void, 1, (MonoAppDomain))
HANDLES(APPDOM_10, "InternalPushDomainRefByID", ves_icall_System_AppDomain_InternalPushDomainRefByID, void, 1, (gint32))
HANDLES(APPDOM_11, "InternalSetContext", ves_icall_System_AppDomain_InternalSetContext, MonoAppContext, 1, (MonoAppContext))
HANDLES(APPDOM_12, "InternalSetDomain", ves_icall_System_AppDomain_InternalSetDomain, MonoAppDomain, 1, (MonoAppDomain))
HANDLES(APPDOM_13, "InternalSetDomainByID", ves_icall_System_AppDomain_InternalSetDomainByID, MonoAppDomain, 1, (gint32))
HANDLES(APPDOM_14, "InternalUnload", ves_icall_System_AppDomain_InternalUnload, void, 1, (gint32))
HANDLES(APPDOM_15, "LoadAssembly",    ves_icall_System_AppDomain_LoadAssembly, MonoReflectionAssembly, 5, (MonoAppDomain, MonoString, MonoObject, MonoBoolean, MonoStackCrawlMark_ptr))
HANDLES(APPDOM_16, "LoadAssemblyRaw", ves_icall_System_AppDomain_LoadAssemblyRaw, MonoReflectionAssembly, 5, (MonoAppDomain, MonoArray, MonoArray, MonoObject, MonoBoolean))
HANDLES(APPDOM_17, "SetData", ves_icall_System_AppDomain_SetData, void, 3, (MonoAppDomain, MonoString, MonoObject))
HANDLES(APPDOM_18, "createDomain", ves_icall_System_AppDomain_createDomain, MonoAppDomain, 2, (MonoString, MonoAppDomainSetup))
HANDLES(APPDOM_19, "getCurDomain", ves_icall_System_AppDomain_getCurDomain, MonoAppDomain, 0, ())
HANDLES(APPDOM_20, "getFriendlyName", ves_icall_System_AppDomain_getFriendlyName, MonoString, 1, (MonoAppDomain))
HANDLES(APPDOM_21, "getRootDomain", ves_icall_System_AppDomain_getRootDomain, MonoAppDomain, 0, ())
HANDLES(APPDOM_22, "getSetup", ves_icall_System_AppDomain_getSetup, MonoAppDomainSetup, 1, (MonoAppDomain))

ICALL_TYPE(ARGI, "System.ArgIterator", ARGI_1)
NOHANDLES(ICALL(ARGI_1, "IntGetNextArg",         ves_icall_System_ArgIterator_IntGetNextArg))
NOHANDLES(ICALL(ARGI_2, "IntGetNextArgType",     ves_icall_System_ArgIterator_IntGetNextArgType))
NOHANDLES(ICALL(ARGI_3, "IntGetNextArgWithType", ves_icall_System_ArgIterator_IntGetNextArgWithType))
NOHANDLES(ICALL(ARGI_4, "Setup",                 ves_icall_System_ArgIterator_Setup))

ICALL_TYPE(ARRAY, "System.Array", ARRAY_1)
HANDLES(ARRAY_1, "ClearInternal", ves_icall_System_Array_ClearInternal, void, 3, (MonoArray, int, int))
HANDLES(ARRAY_3, "CreateInstanceImpl",   ves_icall_System_Array_CreateInstanceImpl, MonoArray, 3, (MonoReflectionType, MonoArray, MonoArray))
HANDLES(ARRAY_4, "FastCopy",         ves_icall_System_Array_FastCopy, MonoBoolean, 5, (MonoArray, int, MonoArray, int, int))
NOHANDLES(ICALL(ARRAY_5, "GetGenericValue_icall", ves_icall_System_Array_GetGenericValue_icall))
HANDLES(ARRAY_6, "GetLength",        ves_icall_System_Array_GetLength, gint32, 2, (MonoArray, gint32))
HANDLES(ARRAY_15, "GetLongLength",   ves_icall_System_Array_GetLongLength, gint64, 2, (MonoArray, gint32))
HANDLES(ARRAY_7, "GetLowerBound",    ves_icall_System_Array_GetLowerBound, gint32, 2, (MonoArray, gint32))
HANDLES(ARRAY_8, "GetRank",          ves_icall_System_Array_GetRank, gint32, 1, (MonoObject))
HANDLES(ARRAY_9, "GetValue",         ves_icall_System_Array_GetValue, MonoObject, 2, (MonoArray, MonoArray))
HANDLES(ARRAY_10, "GetValueImpl",    ves_icall_System_Array_GetValueImpl, MonoObject, 2, (MonoArray, guint32))
NOHANDLES(ICALL(ARRAY_11, "SetGenericValue_icall", ves_icall_System_Array_SetGenericValue_icall))
HANDLES(ARRAY_12, "SetValue",         ves_icall_System_Array_SetValue, void, 3, (MonoArray, MonoObject, MonoArray))
HANDLES(ARRAY_13, "SetValueImpl",  ves_icall_System_Array_SetValueImpl, void, 3, (MonoArray, MonoObject, guint32))

ICALL_TYPE(BUFFER, "System.Buffer", BUFFER_1)
HANDLES(BUFFER_1, "InternalBlockCopy", ves_icall_System_Buffer_BlockCopyInternal, MonoBoolean, 5, (MonoArray, gint32, MonoArray, gint32, gint32))
NOHANDLES(ICALL(BUFFER_5, "InternalMemcpy", ves_icall_System_Buffer_MemcpyInternal))
HANDLES(BUFFER_2, "_ByteLength", ves_icall_System_Buffer_ByteLengthInternal, gint32, 1, (MonoArray))

ICALL_TYPE(CLRCONFIG, "System.CLRConfig", CLRCONFIG_1)
HANDLES(CLRCONFIG_1, "CheckThrowUnobservedTaskExceptions", ves_icall_System_CLRConfig_CheckThrowUnobservedTaskExceptions, MonoBoolean, 0, ())

ICALL_TYPE(DEFAULTC, "System.Configuration.DefaultConfig", DEFAULTC_1)
HANDLES(DEFAULTC_1, "get_bundled_machine_config", ves_icall_System_Configuration_DefaultConfig_get_bundled_machine_config, MonoString, 0, ())
HANDLES(DEFAULTC_2, "get_machine_config_path", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path, MonoString, 0, ())

/* Note that the below icall shares the same function as DefaultConfig uses */
ICALL_TYPE(INTCFGHOST, "System.Configuration.InternalConfigurationHost", INTCFGHOST_1)
HANDLES(INTCFGHOST_1, "get_bundled_app_config", ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_app_config, MonoString, 0, ())
HANDLES(INTCFGHOST_2, "get_bundled_machine_config", ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_machine_config, MonoString, 0, ())

ICALL_TYPE(CONSOLE, "System.ConsoleDriver", CONSOLE_1)
HANDLES(CONSOLE_1, "InternalKeyAvailable", ves_icall_System_ConsoleDriver_InternalKeyAvailable, gint32, 1, (gint32))
HANDLES(CONSOLE_2, "Isatty", ves_icall_System_ConsoleDriver_Isatty, MonoBoolean, 1, (gpointer/*HANDLE*/))
HANDLES(CONSOLE_3, "SetBreak", ves_icall_System_ConsoleDriver_SetBreak, MonoBoolean, 1, (MonoBoolean))
HANDLES(CONSOLE_4, "SetEcho", ves_icall_System_ConsoleDriver_SetEcho, MonoBoolean, 1, (MonoBoolean))
HANDLES(CONSOLE_5, "TtySetup", ves_icall_System_ConsoleDriver_TtySetup, MonoBoolean, 4, (MonoString, MonoString, MonoArrayOut, int_ptr_ref))

ICALL_TYPE(DTIME, "System.DateTime", DTIME_1)
NOHANDLES(ICALL(DTIME_1, "GetSystemTimeAsFileTime", ves_icall_System_DateTime_GetSystemTimeAsFileTime))

ICALL_TYPE(DELEGATE, "System.Delegate", DELEGATE_1)
HANDLES(DELEGATE_1, "AllocDelegateLike_internal", ves_icall_System_Delegate_AllocDelegateLike_internal, MonoMulticastDelegate, 1, (MonoDelegate))
HANDLES(DELEGATE_2, "CreateDelegate_internal", ves_icall_System_Delegate_CreateDelegate_internal, MonoObject, 4, (MonoReflectionType, MonoObject, MonoReflectionMethod, MonoBoolean))
HANDLES(DELEGATE_3, "GetVirtualMethod_internal", ves_icall_System_Delegate_GetVirtualMethod_internal, MonoReflectionMethod, 1, (MonoDelegate))

ICALL_TYPE(DEBUGR, "System.Diagnostics.Debugger", DEBUGR_1)
NOHANDLES(ICALL(DEBUGR_1, "IsAttached_internal", ves_icall_System_Diagnostics_Debugger_IsAttached_internal))
NOHANDLES(ICALL(DEBUGR_2, "IsLogging", ves_icall_System_Diagnostics_Debugger_IsLogging))
NOHANDLES(ICALL(DEBUGR_3, "Log_icall", ves_icall_System_Diagnostics_Debugger_Log))

ICALL_TYPE(TRACEL, "System.Diagnostics.DefaultTraceListener", TRACEL_1)
HANDLES(TRACEL_1, "WriteWindowsDebugString", ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString, void, 1, (const_gunichar2_ptr))

ICALL_TYPE(FILEV, "System.Diagnostics.FileVersionInfo", FILEV_1)
HANDLES(FILEV_1, "GetVersionInfo_icall", ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal, void, 3, (MonoObject, const_gunichar2_ptr, int))

ICALL_TYPE(PERFCTR, "System.Diagnostics.PerformanceCounter", PERFCTR_1)
NOHANDLES(ICALL(PERFCTR_1, "FreeData", mono_perfcounter_free_data))
HANDLES(PERFCTR_2, "GetImpl_icall", mono_perfcounter_get_impl, gpointer, 8,
	(const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32, gint32_ref, MonoBoolean_ref))
NOHANDLES(ICALL(PERFCTR_3, "GetSample", mono_perfcounter_get_sample))
NOHANDLES(ICALL(PERFCTR_4, "UpdateValue", mono_perfcounter_update_value))

ICALL_TYPE(PERFCTRCAT, "System.Diagnostics.PerformanceCounterCategory", PERFCTRCAT_1)
HANDLES(PERFCTRCAT_1, "CategoryDelete_icall", mono_perfcounter_category_del, MonoBoolean, 2, (const_gunichar2_ptr, gint32))
HANDLES(PERFCTRCAT_2, "CategoryHelpInternal_icall",   mono_perfcounter_category_help, MonoString, 2, (const_gunichar2_ptr, gint32))
HANDLES(PERFCTRCAT_3, "CounterCategoryExists_icall", mono_perfcounter_category_exists, MonoBoolean, 4, (const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32))
HANDLES(PERFCTRCAT_4, "Create_icall",     mono_perfcounter_create, MonoBoolean, 6, (const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32, gint32, MonoArray))
HANDLES(PERFCTRCAT_5, "GetCategoryNames", mono_perfcounter_category_names, MonoArray, 0, ())
HANDLES(PERFCTRCAT_6, "GetCounterNames_icall", mono_perfcounter_counter_names, MonoArray, 2, (const_gunichar2_ptr, gint32))
HANDLES(PERFCTRCAT_7, "GetInstanceNames_icall", mono_perfcounter_instance_names, MonoArray, 2, (const_gunichar2_ptr, gint32))
HANDLES(PERFCTRCAT_8, "InstanceExistsInternal_icall", mono_perfcounter_instance_exists, MonoBoolean, 4, (const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32))

ICALL_TYPE(PROCESS, "System.Diagnostics.Process", PROCESS_1)
HANDLES(PROCESS_1, "CreateProcess_internal", ves_icall_System_Diagnostics_Process_CreateProcess_internal,
	MonoBoolean, 5, (MonoW32ProcessStartInfo, gpointer, gpointer, gpointer, MonoW32ProcessInfo_ref))
HANDLES(PROCESS_4, "GetModules_icall", ves_icall_System_Diagnostics_Process_GetModules_internal, MonoArray, 2, (MonoObject, PROCESS_HANDLE))
NOHANDLES(ICALL(PROCESS_5H, "GetProcessData", ves_icall_System_Diagnostics_Process_GetProcessData))
HANDLES(PROCESS_6, "GetProcess_internal", ves_icall_System_Diagnostics_Process_GetProcess_internal, gpointer, 1, (guint32))
HANDLES(PROCESS_7, "GetProcesses_internal", ves_icall_System_Diagnostics_Process_GetProcesses_internal, MonoArray, 0, ())
HANDLES(PROCESS_10, "ProcessName_icall", ves_icall_System_Diagnostics_Process_ProcessName_internal, MonoString, 1, (PROCESS_HANDLE))
HANDLES(PROCESS_13, "ShellExecuteEx_internal", ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal, MonoBoolean, 2, (MonoW32ProcessStartInfo, MonoW32ProcessInfo_ref))

ICALL_TYPE(STOPWATCH, "System.Diagnostics.Stopwatch", STOPWATCH_1)
NOHANDLES(ICALL(STOPWATCH_1, "GetTimestamp", ves_icall_System_Diagnostics_Stopwatch_GetTimestamp))

ICALL_TYPE(ENUM, "System.Enum", ENUM_1)
HANDLES(ENUM_1, "GetEnumValuesAndNames", ves_icall_System_Enum_GetEnumValuesAndNames, MonoBoolean, 3, (MonoReflectionType, MonoArrayOut, MonoArrayOut))
HANDLES(ENUM_2, "InternalBoxEnum", ves_icall_System_Enum_ToObject, MonoObject, 2, (MonoReflectionType, guint64))
HANDLES(ENUM_3, "InternalCompareTo", ves_icall_System_Enum_compare_value_to, int, 2, (MonoObject, MonoObject))
HANDLES(ENUM_3a, "InternalGetCorElementType", ves_icall_System_Enum_InternalGetCorElementType, int, 1, (MonoObject))
HANDLES(ENUM_4, "InternalGetUnderlyingType", ves_icall_System_Enum_get_underlying_type, MonoReflectionType, 1, (MonoReflectionType))
HANDLES(ENUM_5, "InternalHasFlag", ves_icall_System_Enum_InternalHasFlag, MonoBoolean, 2, (MonoObject, MonoObject))
HANDLES(ENUM_6, "get_hashcode", ves_icall_System_Enum_get_hashcode, int, 1, (MonoObject))
HANDLES(ENUM_7, "get_value", ves_icall_System_Enum_get_value, MonoObject, 1, (MonoObject))

ICALL_TYPE(ENV, "System.Environment", ENV_1)
NOHANDLES(ICALL(ENV_1, "Exit", ves_icall_System_Environment_Exit))
HANDLES(ENV_1a, "FailFast", ves_icall_System_Environment_FailFast, void, 3, (MonoString, MonoException, MonoString))
HANDLES(ENV_2, "GetCommandLineArgs", ves_icall_System_Environment_GetCommandLineArgs, MonoArray, 0, ())
HANDLES(ENV_3, "GetEnvironmentVariableNames", ves_icall_System_Environment_GetEnvironmentVariableNames, MonoArray, 0, ())
NOHANDLES(ICALL(ENV_31, "GetIs64BitOperatingSystem", ves_icall_System_Environment_GetIs64BitOperatingSystem))
HANDLES(ENV_4, "GetLogicalDrivesInternal", ves_icall_System_Environment_GetLogicalDrivesInternal, MonoArray, 0, ())
HANDLES_REUSE_WRAPPER(ENV_5, "GetMachineConfigPath", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path, MonoString, 0, ())
HANDLES(ENV_51, "GetNewLine", ves_icall_System_Environment_get_NewLine, MonoString, 0, ())
HANDLES(ENV_6, "GetOSVersionString", ves_icall_System_Environment_GetOSVersionString, MonoString, 0, ())
NOHANDLES(ICALL(ENV_6a, "GetPageSize", mono_pagesize))
HANDLES(ENV_7, "GetWindowsFolderPath", ves_icall_System_Environment_GetWindowsFolderPath, MonoString, 1, (int))
HANDLES(ENV_8, "InternalSetEnvironmentVariable", ves_icall_System_Environment_InternalSetEnvironmentVariable, void, 4, (const_gunichar2_ptr, gint32, const_gunichar2_ptr, gint32))
NOHANDLES(ICALL(ENV_9, "get_ExitCode", mono_environment_exitcode_get))
NOHANDLES(ICALL(ENV_10, "get_HasShutdownStarted", ves_icall_System_Environment_get_HasShutdownStarted))
HANDLES(ENV_11, "get_MachineName", ves_icall_System_Environment_get_MachineName, MonoString, 0, ())
NOHANDLES(ICALL(ENV_13, "get_Platform", ves_icall_System_Environment_get_Platform))
NOHANDLES(ICALL(ENV_14, "get_ProcessorCount", ves_icall_System_Environment_get_ProcessorCount))
NOHANDLES(ICALL(ENV_15, "get_TickCount", ves_icall_System_Environment_get_TickCount))
HANDLES(ENV_16, "get_UserName", ves_icall_System_Environment_get_UserName, MonoString, 0, ())
HANDLES(ENV_16b, "get_bundled_machine_config", ves_icall_System_Environment_get_bundled_machine_config, MonoString, 0, ())
HANDLES(ENV_16m, "internalBroadcastSettingChange", ves_icall_System_Environment_BroadcastSettingChange, void, 0, ())
HANDLES(ENV_17, "internalGetEnvironmentVariable_native", ves_icall_System_Environment_GetEnvironmentVariable_native, MonoString, 1, (const_char_ptr))
HANDLES(ENV_18, "internalGetGacPath", ves_icall_System_Environment_GetGacPath, MonoString, 0, ())
HANDLES(ENV_19, "internalGetHome", ves_icall_System_Environment_InternalGetHome, MonoString, 0, ())
NOHANDLES(ICALL(ENV_20, "set_ExitCode", mono_environment_exitcode_set))
ICALL_TYPE(GC, "System.GC", GC_10)
NOHANDLES(ICALL(GC_10, "GetAllocatedBytesForCurrentThread", ves_icall_System_GC_GetAllocatedBytesForCurrentThread))
NOHANDLES(ICALL(GC_0, "GetCollectionCount", ves_icall_System_GC_GetCollectionCount))
HANDLES(GC_0a, "GetGeneration", ves_icall_System_GC_GetGeneration, int, 1, (MonoObject))
NOHANDLES(ICALL(GC_0b, "GetMaxGeneration", ves_icall_System_GC_GetMaxGeneration))
NOHANDLES(ICALL(GC_1, "GetTotalMemory", ves_icall_System_GC_GetTotalMemory))
NOHANDLES(ICALL(GC_2, "InternalCollect", ves_icall_System_GC_InternalCollect))
NOHANDLES(ICALL(GC_4a, "RecordPressure", ves_icall_System_GC_RecordPressure))
NOHANDLES(ICALL(GC_6, "WaitForPendingFinalizers", ves_icall_System_GC_WaitForPendingFinalizers))
HANDLES(GC_6b, "_ReRegisterForFinalize", ves_icall_System_GC_ReRegisterForFinalize, void, 1, (MonoObject))
HANDLES(GC_7, "_SuppressFinalize", ves_icall_System_GC_SuppressFinalize, void, 1, (MonoObject))
HANDLES(GC_9, "get_ephemeron_tombstone", ves_icall_System_GC_get_ephemeron_tombstone, MonoObject, 0, ())
HANDLES(GC_8, "register_ephemeron_array", ves_icall_System_GC_register_ephemeron_array, void, 1, (MonoObject))

ICALL_TYPE(CALDATA, "System.Globalization.CalendarData", CALDATA_1)
HANDLES(CALDATA_1, "fill_calendar_data", ves_icall_System_Globalization_CalendarData_fill_calendar_data, MonoBoolean, 3, (MonoCalendarData, MonoString, gint32))

ICALL_TYPE(COMPINF, "System.Globalization.CompareInfo", COMPINF_4)
NOHANDLES(ICALL(COMPINF_4, "internal_compare_icall", ves_icall_System_Globalization_CompareInfo_internal_compare))
NOHANDLES(ICALL(COMPINF_6, "internal_index_icall", ves_icall_System_Globalization_CompareInfo_internal_index))

ICALL_TYPE(CULDATA, "System.Globalization.CultureData", CULDATA_1)
HANDLES(CULDATA_1, "fill_culture_data", ves_icall_System_Globalization_CultureData_fill_culture_data, void, 2, (MonoCultureData, gint32))
NOHANDLES(ICALL(CULDATA_2, "fill_number_data", ves_icall_System_Globalization_CultureData_fill_number_data))

ICALL_TYPE(CULINF, "System.Globalization.CultureInfo", CULINF_5)
HANDLES(CULINF_5, "construct_internal_locale_from_lcid", ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid, MonoBoolean, 2, (MonoCultureInfo, gint32))
HANDLES(CULINF_6, "construct_internal_locale_from_name", ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name, MonoBoolean, 2, (MonoCultureInfo, MonoString))
HANDLES(CULINF_7, "get_current_locale_name", ves_icall_System_Globalization_CultureInfo_get_current_locale_name, MonoString, 0, ())
HANDLES(CULINF_9, "internal_get_cultures", ves_icall_System_Globalization_CultureInfo_internal_get_cultures, MonoArray, 3, (MonoBoolean, MonoBoolean, MonoBoolean))

ICALL_TYPE(REGINF, "System.Globalization.RegionInfo", REGINF_2)
HANDLES(REGINF_2, "construct_internal_region_from_name", ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name, MonoBoolean, 2, (MonoRegionInfo, MonoString))

#if defined(ENABLE_MONODROID) || defined(ENABLE_MONOTOUCH) || defined(TARGET_WASM)
ICALL_TYPE(DEFLATESTREAM, "System.IO.Compression.DeflateStreamNative", DEFLATESTREAM_1)
NOHANDLES(ICALL(DEFLATESTREAM_1, "CloseZStream", ves_icall_System_IO_Compression_DeflateStreamNative_CloseZStream))
NOHANDLES(ICALL(DEFLATESTREAM_2, "CreateZStream", ves_icall_System_IO_Compression_DeflateStreamNative_CreateZStream))
NOHANDLES(ICALL(DEFLATESTREAM_3, "Flush", ves_icall_System_IO_Compression_DeflateStreamNative_Flush))
NOHANDLES(ICALL(DEFLATESTREAM_4, "ReadZStream", ves_icall_System_IO_Compression_DeflateStreamNative_ReadZStream))
NOHANDLES(ICALL(DEFLATESTREAM_5, "WriteZStream", ves_icall_System_IO_Compression_DeflateStreamNative_WriteZStream))
#endif

#ifndef PLATFORM_NO_DRIVEINFO
ICALL_TYPE(IODRIVEINFO, "System.IO.DriveInfo", IODRIVEINFO_1)
NOHANDLES(ICALL(IODRIVEINFO_1, "GetDiskFreeSpaceInternal", ves_icall_System_IO_DriveInfo_GetDiskFreeSpace))
HANDLES(IODRIVEINFO_2, "GetDriveFormatInternal", ves_icall_System_IO_DriveInfo_GetDriveFormat, MonoString, 2, (const_gunichar2_ptr, int))
HANDLES(IODRIVEINFO_3, "GetDriveTypeInternal", ves_icall_System_IO_DriveInfo_GetDriveType, guint32, 2, (const_gunichar2_ptr, int))
#endif

ICALL_TYPE(FAMW, "System.IO.FAMWatcher", FAMW_1)
HANDLES(FAMW_1, "InternalFAMNextEvent", ves_icall_System_IO_FAMW_InternalFAMNextEvent, int, 4, (gpointer, MonoStringOut, int_ref, int_ref))

ICALL_TYPE(FILEW, "System.IO.FileSystemWatcher", FILEW_4)
NOHANDLES(ICALL(FILEW_4, "InternalSupportsFSW", ves_icall_System_IO_FSW_SupportsFSW))

ICALL_TYPE(KQUEM, "System.IO.KqueueMonitor", KQUEM_1)
NOHANDLES(ICALL(KQUEM_1, "kevent_notimeout", ves_icall_System_IO_KqueueMonitor_kevent_notimeout))

ICALL_TYPE(LOGCATEXTWRITER, "System.IO.LogcatTextWriter", LOGCATEXTWRITER_1)
NOHANDLES(ICALL(LOGCATEXTWRITER_1, "Log", ves_icall_System_IO_LogcatTextWriter_Log))

ICALL_TYPE(MMAPIMPL, "System.IO.MemoryMappedFiles.MemoryMapImpl", MMAPIMPL_1)
// FIXME rename to ves_icall...
HANDLES(MMAPIMPL_1, "CloseMapping", mono_mmap_close, void, 1, (gpointer))
HANDLES(MMAPIMPL_2, "ConfigureHandleInheritability", mono_mmap_configure_inheritability, void, 2, (gpointer, gint32))
HANDLES(MMAPIMPL_3, "Flush", mono_mmap_flush, void,  1, (gpointer))
HANDLES(MMAPIMPL_4, "MapInternal", mono_mmap_map, int, 6, (gpointer, gint64, gint64_ref, int, gpointer_ref, gpointer_ref))
HANDLES(MMAPIMPL_5, "OpenFileInternal", mono_mmap_open_file, gpointer, 9, (const_gunichar2_ptr, int, int, const_gunichar2_ptr, int, gint64_ref, int, int, int_ref))
HANDLES(MMAPIMPL_6, "OpenHandleInternal", mono_mmap_open_handle, gpointer, 7, (gpointer, const_gunichar2_ptr, int, gint64_ref, int, int, int_ref))
HANDLES(MMAPIMPL_7, "Unmap", mono_mmap_unmap, MonoBoolean, 1, (gpointer))

ICALL_TYPE(MONOIO, "System.IO.MonoIO", MONOIO_39)
NOHANDLES(ICALL(MONOIO_39, "Cancel_internal", ves_icall_System_IO_MonoIO_Cancel))
NOHANDLES(ICALL(MONOIO_1, "Close(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Close))
#ifndef PLATFORM_RO_FS
NOHANDLES(ICALL(MONOIO_2, "CopyFile(char*,char*,bool,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CopyFile))
NOHANDLES(ICALL(MONOIO_3, "CreateDirectory(char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CreateDirectory))
NOHANDLES(ICALL(MONOIO_4, "CreatePipe", ves_icall_System_IO_MonoIO_CreatePipe))
NOHANDLES(ICALL(MONOIO_5, "DeleteFile(char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_DeleteFile))
#endif /* !PLATFORM_RO_FS */
NOHANDLES(ICALL(MONOIO_38, "DumpHandles", ves_icall_System_IO_MonoIO_DumpHandles))
NOHANDLES(ICALL(MONOIO_34, "DuplicateHandle", ves_icall_System_IO_MonoIO_DuplicateHandle))
NOHANDLES(ICALL(MONOIO_37a, "FindCloseFile", ves_icall_System_IO_MonoIO_FindCloseFile))
HANDLES(MONOIO_35a, "FindFirstFile", ves_icall_System_IO_MonoIO_FindFirstFile, gpointer, 4, (const_gunichar2_ptr, MonoStringOut, gint32_ref, gint32_ref))
HANDLES(MONOIO_36a, "FindNextFile", ves_icall_System_IO_MonoIO_FindNextFile, MonoBoolean, 4, (gpointer, MonoStringOut, gint32_ref, gint32_ref))
NOHANDLES(ICALL(MONOIO_6, "Flush(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Flush))
HANDLES(MONOIO_7, "GetCurrentDirectory(System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetCurrentDirectory, MonoString, 1, (gint32_ref))
NOHANDLES(ICALL(MONOIO_8, "GetFileAttributes(char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileAttributes))
NOHANDLES(ICALL(MONOIO_9, "GetFileStat(char*,System.IO.MonoIOStat&,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileStat))
NOHANDLES(ICALL(MONOIO_11, "GetFileType(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileType))
NOHANDLES(ICALL(MONOIO_12, "GetLength(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetLength))
#ifndef PLATFORM_RO_FS
NOHANDLES(ICALL(MONOIO_14, "Lock(intptr,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Lock))
NOHANDLES(ICALL(MONOIO_15, "MoveFile(char*,char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_MoveFile))
#endif /* !PLATFORM_RO_FS */
NOHANDLES(ICALL(MONOIO_16, "Open(char*,System.IO.FileMode,System.IO.FileAccess,System.IO.FileShare,System.IO.FileOptions,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Open))
HANDLES(MONOIO_17, "Read(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Read, gint32, 5, (gpointer, MonoArray, gint32, gint32, gint32_ref))
#ifndef PLATFORM_RO_FS
NOHANDLES(ICALL(MONOIO_18, "RemoveDirectory(char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_RemoveDirectory))
NOHANDLES(ICALL(MONOIO_18M, "ReplaceFile(char*,char*,char*,bool,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_ReplaceFile))
#endif /* !PLATFORM_RO_FS */
NOHANDLES(ICALL(MONOIO_19, "Seek(intptr,long,System.IO.SeekOrigin,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Seek))
NOHANDLES(ICALL(MONOIO_20, "SetCurrentDirectory(char*,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetCurrentDirectory))
NOHANDLES(ICALL(MONOIO_21, "SetFileAttributes(char*,System.IO.FileAttributes,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileAttributes))
NOHANDLES(ICALL(MONOIO_22, "SetFileTime(intptr,long,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileTime))
NOHANDLES(ICALL(MONOIO_23, "SetLength(intptr,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetLength))
#ifndef PLATFORM_RO_FS
NOHANDLES(ICALL(MONOIO_24, "Unlock(intptr,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Unlock))
#endif
HANDLES(MONOIO_25, "Write(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Write, gint32, 5, (gpointer, MonoArray, gint32, gint32, gint32_ref))
NOHANDLES(ICALL(MONOIO_26, "get_AltDirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar))
NOHANDLES(ICALL(MONOIO_27, "get_ConsoleError", ves_icall_System_IO_MonoIO_get_ConsoleError))
NOHANDLES(ICALL(MONOIO_28, "get_ConsoleInput", ves_icall_System_IO_MonoIO_get_ConsoleInput))
NOHANDLES(ICALL(MONOIO_29, "get_ConsoleOutput", ves_icall_System_IO_MonoIO_get_ConsoleOutput))
NOHANDLES(ICALL(MONOIO_30, "get_DirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar))
HANDLES(MONOIO_31, "get_InvalidPathChars", ves_icall_System_IO_MonoIO_get_InvalidPathChars, MonoArray, 0, ())
NOHANDLES(ICALL(MONOIO_32, "get_PathSeparator", ves_icall_System_IO_MonoIO_get_PathSeparator))
NOHANDLES(ICALL(MONOIO_33, "get_VolumeSeparatorChar", ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar))

ICALL_TYPE(IOPATH, "System.IO.Path", IOPATH_1)
HANDLES(IOPATH_1, "get_temp_path", ves_icall_System_IO_get_temp_path, MonoString, 0, ())

ICALL_TYPE(IOSELECTOR, "System.IOSelector", IOSELECTOR_1)
HANDLES(IOSELECTOR_1, "Add", ves_icall_System_IOSelector_Add, void, 2, (gpointer, MonoIOSelectorJob))
NOHANDLES(ICALL(IOSELECTOR_2, "Remove", ves_icall_System_IOSelector_Remove))

ICALL_TYPE(MATH, "System.Math", MATH_19)
NOHANDLES(ICALL(MATH_19, "Abs(double)", ves_icall_System_Math_Abs_double))
NOHANDLES(ICALL(MATH_20, "Abs(single)", ves_icall_System_Math_Abs_single))
NOHANDLES(ICALL(MATH_1, "Acos", ves_icall_System_Math_Acos))
NOHANDLES(ICALL(MATH_1a, "Acosh", ves_icall_System_Math_Acosh))
NOHANDLES(ICALL(MATH_2, "Asin", ves_icall_System_Math_Asin))
NOHANDLES(ICALL(MATH_2a, "Asinh", ves_icall_System_Math_Asinh))
NOHANDLES(ICALL(MATH_3, "Atan", ves_icall_System_Math_Atan))
NOHANDLES(ICALL(MATH_4, "Atan2", ves_icall_System_Math_Atan2))
NOHANDLES(ICALL(MATH_4a, "Atanh", ves_icall_System_Math_Atanh))
NOHANDLES(ICALL(MATH_4b, "Cbrt", ves_icall_System_Math_Cbrt))
NOHANDLES(ICALL(MATH_21, "Ceiling", ves_icall_System_Math_Ceiling))
NOHANDLES(ICALL(MATH_5, "Cos", ves_icall_System_Math_Cos))
NOHANDLES(ICALL(MATH_6, "Cosh", ves_icall_System_Math_Cosh))
NOHANDLES(ICALL(MATH_7, "Exp", ves_icall_System_Math_Exp))
NOHANDLES(ICALL(MATH_7a, "FMod", ves_icall_System_Math_FMod))
NOHANDLES(ICALL(MATH_8, "Floor", ves_icall_System_Math_Floor))
NOHANDLES(ICALL(MATH_9, "Log", ves_icall_System_Math_Log))
NOHANDLES(ICALL(MATH_10, "Log10", ves_icall_System_Math_Log10))
NOHANDLES(ICALL(MATH_10a, "ModF", ves_icall_System_Math_ModF))
NOHANDLES(ICALL(MATH_11, "Pow", ves_icall_System_Math_Pow))
NOHANDLES(ICALL(MATH_12, "Round", ves_icall_System_Math_Round))
NOHANDLES(ICALL(MATH_14, "Sin", ves_icall_System_Math_Sin))
NOHANDLES(ICALL(MATH_15, "Sinh", ves_icall_System_Math_Sinh))
NOHANDLES(ICALL(MATH_16, "Sqrt", ves_icall_System_Math_Sqrt))
NOHANDLES(ICALL(MATH_17, "Tan", ves_icall_System_Math_Tan))
NOHANDLES(ICALL(MATH_18, "Tanh", ves_icall_System_Math_Tanh))

ICALL_TYPE(MATHF, "System.MathF", MATHF_1)
NOHANDLES(ICALL(MATHF_1, "Acos", ves_icall_System_MathF_Acos))
NOHANDLES(ICALL(MATHF_2, "Acosh", ves_icall_System_MathF_Acosh))
NOHANDLES(ICALL(MATHF_3, "Asin", ves_icall_System_MathF_Asin))
NOHANDLES(ICALL(MATHF_4, "Asinh", ves_icall_System_MathF_Asinh))
NOHANDLES(ICALL(MATHF_5, "Atan", ves_icall_System_MathF_Atan))
NOHANDLES(ICALL(MATHF_6, "Atan2", ves_icall_System_MathF_Atan2))
NOHANDLES(ICALL(MATHF_7, "Atanh", ves_icall_System_MathF_Atanh))
NOHANDLES(ICALL(MATHF_8, "Cbrt", ves_icall_System_MathF_Cbrt))
NOHANDLES(ICALL(MATHF_9, "Ceiling", ves_icall_System_MathF_Ceiling))
NOHANDLES(ICALL(MATHF_10, "Cos", ves_icall_System_MathF_Cos))
NOHANDLES(ICALL(MATHF_11, "Cosh", ves_icall_System_MathF_Cosh))
NOHANDLES(ICALL(MATHF_12, "Exp", ves_icall_System_MathF_Exp))
NOHANDLES(ICALL(MATHF_22, "FMod", ves_icall_System_MathF_FMod))
NOHANDLES(ICALL(MATHF_13, "Floor", ves_icall_System_MathF_Floor))
NOHANDLES(ICALL(MATHF_14, "Log", ves_icall_System_MathF_Log))
NOHANDLES(ICALL(MATHF_15, "Log10", ves_icall_System_MathF_Log10))
NOHANDLES(ICALL(MATHF_23, "ModF(single,single*)", ves_icall_System_MathF_ModF))
NOHANDLES(ICALL(MATHF_16, "Pow", ves_icall_System_MathF_Pow))
NOHANDLES(ICALL(MATHF_17, "Sin", ves_icall_System_MathF_Sin))
NOHANDLES(ICALL(MATHF_18, "Sinh", ves_icall_System_MathF_Sinh))
NOHANDLES(ICALL(MATHF_19, "Sqrt", ves_icall_System_MathF_Sqrt))
NOHANDLES(ICALL(MATHF_20, "Tan", ves_icall_System_MathF_Tan))
NOHANDLES(ICALL(MATHF_21, "Tanh", ves_icall_System_MathF_Tanh))

ICALL_TYPE(MCATTR, "System.MonoCustomAttrs", MCATTR_1)
HANDLES(MCATTR_1, "GetCustomAttributesDataInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal, MonoArray, 1, (MonoObject))
HANDLES(MCATTR_2, "GetCustomAttributesInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesInternal, MonoArray, 3, (MonoObject, MonoReflectionType, MonoBoolean))
HANDLES(MCATTR_3, "IsDefinedInternal", ves_icall_MonoCustomAttrs_IsDefinedInternal, MonoBoolean, 2, (MonoObject, MonoReflectionType))

#ifndef DISABLE_SOCKETS
ICALL_TYPE(NDNS, "System.Net.Dns", NDNS_1)
HANDLES(NDNS_1, "GetHostByAddr_icall", ves_icall_System_Net_Dns_GetHostByAddr, MonoBoolean, 5, (MonoString, MonoStringOut, MonoArrayOut, MonoArrayOut, gint32))
HANDLES(NDNS_2, "GetHostByName_icall", ves_icall_System_Net_Dns_GetHostByName, MonoBoolean, 5, (MonoString, MonoStringOut, MonoArrayOut, MonoArrayOut, gint32))
HANDLES(NDNS_3, "GetHostName_icall", ves_icall_System_Net_Dns_GetHostName, MonoBoolean, 1, (MonoStringOut))
#endif

#if defined(ENABLE_MONODROID)
ICALL_TYPE(LINUXNETWORKCHANGE, "System.Net.NetworkInformation.LinuxNetworkChange", LINUXNETWORKCHANGE_1)
NOHANDLES(ICALL(LINUXNETWORKCHANGE_1, "CloseNLSocket", ves_icall_System_Net_NetworkInformation_LinuxNetworkChange_CloseNLSocket))
NOHANDLES(ICALL(LINUXNETWORKCHANGE_2, "CreateNLSocket", ves_icall_System_Net_NetworkInformation_LinuxNetworkChange_CreateNLSocket))
NOHANDLES(ICALL(LINUXNETWORKCHANGE_3, "ReadEvents", ves_icall_System_Net_NetworkInformation_LinuxNetworkChange_ReadEvents))
#endif

#if !defined(DISABLE_SOCKETS)
ICALL_TYPE(MAC_IFACE_PROPS, "System.Net.NetworkInformation.MacOsIPInterfaceProperties", MAC_IFACE_PROPS_1)
HANDLES(MAC_IFACE_PROPS_1, "ParseRouteInfo_icall", ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo, MonoBoolean, 2, (MonoString, MonoArrayOut))

ICALL_TYPE(SOCK, "System.Net.Sockets.Socket", SOCK_1)
NOHANDLES(ICALL(SOCK_1, "Accept_icall", ves_icall_System_Net_Sockets_Socket_Accept_icall))
NOHANDLES(ICALL(SOCK_2, "Available_icall", ves_icall_System_Net_Sockets_Socket_Available_icall))
HANDLES(SOCK_3, "Bind_icall", ves_icall_System_Net_Sockets_Socket_Bind_icall, void, 3, (gsize, MonoObject, gint32_ref))
NOHANDLES(ICALL(SOCK_4, "Blocking_icall", ves_icall_System_Net_Sockets_Socket_Blocking_icall))
NOHANDLES(ICALL(SOCK_5, "Close_icall", ves_icall_System_Net_Sockets_Socket_Close_icall))
HANDLES(SOCK_6, "Connect_icall", ves_icall_System_Net_Sockets_Socket_Connect_icall, void, 4, (gsize, MonoObject, gint32_ref, MonoBoolean))
NOHANDLES(ICALL(SOCK_6a, "Disconnect_icall", ves_icall_System_Net_Sockets_Socket_Disconnect_icall))
NOHANDLES(ICALL(SOCK_6b, "Duplicate_icall", ves_icall_System_Net_Sockets_Socket_Duplicate_icall))
//FIXME The array is ref but the icall does not write to it.
HANDLES(SOCK_7, "GetSocketOption_arr_icall", ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_icall, void, 5, (gsize, gint32, gint32, MonoArray, gint32_ref))
HANDLES(SOCK_8, "GetSocketOption_obj_icall", ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_icall, void, 5, (gsize, gint32, gint32, MonoObjectOut, gint32_ref))
HANDLES(SOCK_21, "IOControl_icall", ves_icall_System_Net_Sockets_Socket_IOControl_icall, int, 5, (gsize, gint32, MonoArray, MonoArray, gint32_ref))
NOHANDLES(ICALL(SOCK_9, "Listen_icall", ves_icall_System_Net_Sockets_Socket_Listen_icall))
HANDLES(SOCK_10, "LocalEndPoint_icall", ves_icall_System_Net_Sockets_Socket_LocalEndPoint_icall, MonoObject, 3, (gsize, gint32, gint32_ref))
NOHANDLES(ICALL(SOCK_11, "Poll_icall", ves_icall_System_Net_Sockets_Socket_Poll_icall))
HANDLES(SOCK_13, "ReceiveFrom_icall", ves_icall_System_Net_Sockets_Socket_ReceiveFrom_icall, gint32, 7, (gsize, char_ptr, gint32, gint32, MonoObjectInOut, gint32_ref, MonoBoolean))
NOHANDLES(ICALL(SOCK_11a, "Receive_array_icall", ves_icall_System_Net_Sockets_Socket_Receive_array_icall))
NOHANDLES(ICALL(SOCK_12, "Receive_icall", ves_icall_System_Net_Sockets_Socket_Receive_icall))
HANDLES(SOCK_14, "RemoteEndPoint_icall", ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_icall, MonoObject, 3, (gsize, gint32, gint32_ref))
HANDLES(SOCK_15, "Select_icall", ves_icall_System_Net_Sockets_Socket_Select_icall, void, 3, (MonoArrayInOut, gint32, gint32_ref))
HANDLES(SOCK_15a, "SendFile_icall", ves_icall_System_Net_Sockets_Socket_SendFile_icall, MonoBoolean, 7, (gsize, MonoString, MonoArray, MonoArray, int, gint32_ref, MonoBoolean))
HANDLES(SOCK_16, "SendTo_icall", ves_icall_System_Net_Sockets_Socket_SendTo_icall, gint32, 7, (gsize, char_ptr, gint32, gint32, MonoObject, gint32_ref, MonoBoolean))
NOHANDLES(ICALL(SOCK_16a, "Send_array_icall", ves_icall_System_Net_Sockets_Socket_Send_array_icall))
NOHANDLES(ICALL(SOCK_17, "Send_icall", ves_icall_System_Net_Sockets_Socket_Send_icall))
HANDLES(SOCK_18, "SetSocketOption_icall", ves_icall_System_Net_Sockets_Socket_SetSocketOption_icall, void, 7, (gsize, gint32, gint32, MonoObject, MonoArray, gint32, gint32_ref))
NOHANDLES(ICALL(SOCK_19, "Shutdown_icall", ves_icall_System_Net_Sockets_Socket_Shutdown_icall))
HANDLES(SOCK_20, "Socket_icall", ves_icall_System_Net_Sockets_Socket_Socket_icall, gpointer, 4, (gint32, gint32, gint32, gint32_ref))
NOHANDLES(ICALL(SOCK_20a, "SupportsPortReuse", ves_icall_System_Net_Sockets_Socket_SupportPortReuse_icall))
HANDLES(SOCK_21a, "cancel_blocking_socket_operation", ves_icall_cancel_blocking_socket_operation, void, 1, (MonoThreadObject))

ICALL_TYPE(SOCKEX, "System.Net.Sockets.SocketException", SOCKEX_1)
NOHANDLES(ICALL(SOCKEX_1, "WSAGetLastError_icall", ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_icall))
#endif /* !DISABLE_SOCKETS */

ICALL_TYPE(NUMBER_FORMATTER, "System.NumberFormatter", NUMBER_FORMATTER_1)
NOHANDLES(ICALL(NUMBER_FORMATTER_1, "GetFormatterTables", ves_icall_System_NumberFormatter_GetFormatterTables))

ICALL_TYPE(OBJ, "System.Object", OBJ_1)
HANDLES(OBJ_1, "GetType", ves_icall_System_Object_GetType, MonoReflectionType, 1, (MonoObject))
HANDLES(OBJ_2, "InternalGetHashCode", mono_object_hash_icall, int, 1, (MonoObject))
HANDLES(OBJ_3, "MemberwiseClone", ves_icall_System_Object_MemberwiseClone, MonoObject, 1, (MonoObject))

ICALL_TYPE(ASSEM, "System.Reflection.Assembly", ASSEM_2)
HANDLES(ASSEM_2, "GetCallingAssembly", ves_icall_System_Reflection_Assembly_GetCallingAssembly, MonoReflectionAssembly, 0, ())
HANDLES(ASSEM_3, "GetEntryAssembly", ves_icall_System_Reflection_Assembly_GetEntryAssembly, MonoReflectionAssembly, 0, ())
HANDLES(ASSEM_4, "GetExecutingAssembly", ves_icall_System_Reflection_Assembly_GetExecutingAssembly, MonoReflectionAssembly, 0, ())
HANDLES(ASSEM_13, "GetTypes", ves_icall_System_Reflection_Assembly_GetTypes, MonoArray, 2, (MonoReflectionAssembly, MonoBoolean))
HANDLES(ASSEM_14, "InternalGetAssemblyName", ves_icall_System_Reflection_Assembly_InternalGetAssemblyName, void, 3, (MonoString, MonoAssemblyName_ref, MonoStringOut))
HANDLES(ASSEM_12, "InternalGetReferencedAssemblies", ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies, GPtrArray_ptr, 1, (MonoReflectionAssembly))
HANDLES(ASSEM_15, "InternalGetType", ves_icall_System_Reflection_Assembly_InternalGetType, MonoReflectionType, 5, (MonoReflectionAssembly, MonoReflectionModule, MonoString, MonoBoolean, MonoBoolean))
HANDLES(ASSEM_16a, "LoadFile_internal", ves_icall_System_Reflection_Assembly_LoadFile_internal, MonoReflectionAssembly, 2, (MonoString, MonoStackCrawlMark_ptr))
HANDLES(ASSEM_17, "LoadFrom", ves_icall_System_Reflection_Assembly_LoadFrom, MonoReflectionAssembly, 3, (MonoString, MonoBoolean, MonoStackCrawlMark_ptr))
HANDLES(ASSEM_26, "load_with_partial_name", ves_icall_System_Reflection_Assembly_load_with_partial_name, MonoReflectionAssembly, 2, (MonoString, MonoObject))

ICALL_TYPE(ASSEMN, "System.Reflection.AssemblyName", ASSEMN_0)
NOHANDLES(ICALL(ASSEMN_0, "GetNativeName", ves_icall_System_Reflection_AssemblyName_GetNativeName))
NOHANDLES(ICALL(ASSEMN_3, "ParseAssemblyName", ves_icall_System_Reflection_AssemblyName_ParseAssemblyName))
NOHANDLES(ICALL(ASSEMN_2, "get_public_token", mono_digest_get_public_token))

ICALL_TYPE(CATTR_DATA, "System.Reflection.CustomAttributeData", CATTR_DATA_1)
HANDLES(CATTR_DATA_1, "ResolveArgumentsInternal", ves_icall_System_Reflection_CustomAttributeData_ResolveArgumentsInternal, void, 6, (MonoReflectionMethod, MonoReflectionAssembly, gpointer, guint32, MonoArrayOut, MonoArrayOut))

ICALL_TYPE(ASSEMB, "System.Reflection.Emit.AssemblyBuilder", ASSEMB_1)
HANDLES(ASSEMB_1, "UpdateNativeCustomAttributes", ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes, void, 1, (MonoReflectionAssemblyBuilder))
HANDLES(ASSEMB_2, "basic_init", ves_icall_AssemblyBuilder_basic_init, void, 1, (MonoReflectionAssemblyBuilder))

#ifndef DISABLE_REFLECTION_EMIT
ICALL_TYPE(CATTRB, "System.Reflection.Emit.CustomAttributeBuilder", CATTRB_1)
HANDLES(CATTRB_1, "GetBlob", ves_icall_CustomAttributeBuilder_GetBlob, MonoArray, 7, (MonoReflectionAssembly, MonoObject, MonoArray, MonoArray, MonoArray, MonoArray, MonoArray))
#endif

ICALL_TYPE(DYNM, "System.Reflection.Emit.DynamicMethod", DYNM_1)
HANDLES(DYNM_1, "create_dynamic_method", ves_icall_DynamicMethod_create_dynamic_method, void, 1, (MonoReflectionDynamicMethod))

ICALL_TYPE(ENUMB, "System.Reflection.Emit.EnumBuilder", ENUMB_1)
HANDLES(ENUMB_1, "setup_enum_type", ves_icall_EnumBuilder_setup_enum_type, void, 2, (MonoReflectionType, MonoReflectionType))

ICALL_TYPE(MODULEB, "System.Reflection.Emit.ModuleBuilder", MODULEB_10)
HANDLES(MODULEB_10, "GetRegisteredToken", ves_icall_ModuleBuilder_GetRegisteredToken, MonoObject, 2, (MonoReflectionModuleBuilder, guint32))
HANDLES(MODULEB_8, "RegisterToken", ves_icall_ModuleBuilder_RegisterToken, void, 3, (MonoReflectionModuleBuilder, MonoObject, guint32))
HANDLES(MODULEB_1, "WriteToFile", ves_icall_ModuleBuilder_WriteToFile, void, 2, (MonoReflectionModuleBuilder, FILE_HANDLE))
HANDLES(MODULEB_2, "basic_init", ves_icall_ModuleBuilder_basic_init, void, 1, (MonoReflectionModuleBuilder))
HANDLES(MODULEB_3, "build_metadata", ves_icall_ModuleBuilder_build_metadata, void, 1, (MonoReflectionModuleBuilder))
HANDLES(MODULEB_5, "getMethodToken", ves_icall_ModuleBuilder_getMethodToken, gint32, 3, (MonoReflectionModuleBuilder, MonoReflectionMethod, MonoArray))
HANDLES(MODULEB_6, "getToken", ves_icall_ModuleBuilder_getToken, gint32, 3, (MonoReflectionModuleBuilder, MonoObject, MonoBoolean))
HANDLES(MODULEB_7, "getUSIndex", ves_icall_ModuleBuilder_getUSIndex, guint32, 2, (MonoReflectionModuleBuilder, MonoString))
HANDLES(MODULEB_9, "set_wrappers_type", ves_icall_ModuleBuilder_set_wrappers_type, void, 2, (MonoReflectionModuleBuilder, MonoReflectionType))

ICALL_TYPE(SIGH, "System.Reflection.Emit.SignatureHelper", SIGH_1)
HANDLES(SIGH_1, "get_signature_field", ves_icall_SignatureHelper_get_signature_field, MonoArray, 1, (MonoReflectionSigHelper))
HANDLES(SIGH_2, "get_signature_local", ves_icall_SignatureHelper_get_signature_local, MonoArray, 1, (MonoReflectionSigHelper))

ICALL_TYPE(TYPEB, "System.Reflection.Emit.TypeBuilder", TYPEB_1)
HANDLES(TYPEB_1, "create_runtime_class", ves_icall_TypeBuilder_create_runtime_class, MonoReflectionType, 1, (MonoReflectionTypeBuilder))

ICALL_TYPE(EVENTI, "System.Reflection.EventInfo", EVENTI_1)
HANDLES(EVENTI_1, "internal_from_handle_type", ves_icall_System_Reflection_EventInfo_internal_from_handle_type, MonoReflectionEvent, 2, (MonoEvent_ref, MonoType_ref))

ICALL_TYPE(FIELDI, "System.Reflection.FieldInfo", FILEDI_1)
HANDLES(FILEDI_1, "get_marshal_info", ves_icall_System_Reflection_FieldInfo_get_marshal_info, MonoReflectionMarshalAsAttribute, 1, (MonoReflectionField))

HANDLES(FILEDI_2, "internal_from_handle_type", ves_icall_System_Reflection_FieldInfo_internal_from_handle_type, MonoReflectionField, 2, (MonoClassField_ref, MonoType_ref))

ICALL_TYPE(MBASE, "System.Reflection.MethodBase", MBASE_1)
HANDLES(MBASE_1, "GetCurrentMethod", ves_icall_GetCurrentMethod, MonoReflectionMethod, 0, ())

ICALL_TYPE(MMETHI, "System.Reflection.MonoMethodInfo", MMETHI_4)
NOHANDLES(ICALL(MMETHI_4, "get_method_attributes", ves_icall_get_method_attributes))
HANDLES(MMETHI_1, "get_method_info", ves_icall_get_method_info, void, 2, (MonoMethod_ptr, MonoMethodInfo_ref))
HANDLES(MMETHI_2, "get_parameter_info", ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info, MonoArray, 2, (MonoMethod_ptr, MonoReflectionMethod))
HANDLES(MMETHI_3, "get_retval_marshal", ves_icall_System_MonoMethodInfo_get_retval_marshal, MonoReflectionMarshalAsAttribute, 1, (MonoMethod_ptr))

ICALL_TYPE(RASSEM, "System.Reflection.RuntimeAssembly", RASSEM_1)
HANDLES(RASSEM_1, "GetAotIdInternal", ves_icall_System_Reflection_RuntimeAssembly_GetAotIdInternal, MonoBoolean, 1, (MonoArray))
HANDLES(RASSEM_2, "GetFilesInternal", ves_icall_System_Reflection_RuntimeAssembly_GetFilesInternal, MonoObject, 3, (MonoReflectionAssembly, MonoString, MonoBoolean))
HANDLES(RASSEM_3, "GetManifestModuleInternal", ves_icall_System_Reflection_Assembly_GetManifestModuleInternal, MonoReflectionModule, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_4, "GetManifestResourceInfoInternal", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInfoInternal, MonoBoolean, 3, (MonoReflectionAssembly, MonoString, MonoManifestResourceInfo))
HANDLES(RASSEM_5, "GetManifestResourceInternal", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal, gpointer, 4, (MonoReflectionAssembly, MonoString, gint32_ref, MonoReflectionModuleOut))
HANDLES(RASSEM_6, "GetManifestResourceNames", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames, MonoArray, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_7, "GetModulesInternal", ves_icall_System_Reflection_RuntimeAssembly_GetModulesInternal, MonoArray, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_8, "InternalImageRuntimeVersion", ves_icall_System_Reflection_RuntimeAssembly_InternalImageRuntimeVersion, MonoString, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_9, "LoadPermissions", ves_icall_System_Reflection_RuntimeAssembly_LoadPermissions, MonoBoolean, 7, (MonoReflectionAssembly, char_ptr_ref, guint32_ref, char_ptr_ref, guint32_ref, char_ptr_ref, guint32_ref))
HANDLES(RASSEM_10, "get_EntryPoint", ves_icall_System_Reflection_RuntimeAssembly_get_EntryPoint, MonoReflectionMethod, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_11, "get_ReflectionOnly", ves_icall_System_Reflection_RuntimeAssembly_get_ReflectionOnly, MonoBoolean, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_12, "get_code_base", ves_icall_System_Reflection_RuntimeAssembly_get_code_base, MonoString, 2, (MonoReflectionAssembly, MonoBoolean))
HANDLES(RASSEM_13, "get_fullname", ves_icall_System_Reflection_RuntimeAssembly_get_fullname, MonoString, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_14, "get_global_assembly_cache", ves_icall_System_Reflection_RuntimeAssembly_get_global_assembly_cache, MonoBoolean, 1, (MonoReflectionAssembly))
HANDLES(RASSEM_15, "get_location", ves_icall_System_Reflection_RuntimeAssembly_get_location, MonoString, 1, (MonoReflectionAssembly))

ICALL_TYPE(MCMETH, "System.Reflection.RuntimeConstructorInfo", MCMETH_1)
HANDLES(MCMETH_1, "GetGenericMethodDefinition_impl", ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition, MonoReflectionMethod, 1, (MonoReflectionMethod))
HANDLES(MCMETH_2, "InternalInvoke", ves_icall_InternalInvoke, MonoObject, 4, (MonoReflectionMethod, MonoObject, MonoArray, MonoExceptionOut))
HANDLES(MCMETH_3, "get_core_clr_security_level", ves_icall_RuntimeMethodInfo_get_core_clr_security_level, int, 1, (MonoReflectionMethod))
HANDLES_REUSE_WRAPPER(MCMETH_4, "get_metadata_token", ves_icall_reflection_get_token)

ICALL_TYPE(MEV, "System.Reflection.RuntimeEventInfo", MEV_1)
HANDLES(MEV_1, "get_event_info", ves_icall_RuntimeEventInfo_get_event_info, void, 2, (MonoReflectionMonoEvent, MonoEventInfo_ref))
HANDLES_REUSE_WRAPPER(MEV_2, "get_metadata_token", ves_icall_reflection_get_token)

ICALL_TYPE(MFIELD, "System.Reflection.RuntimeFieldInfo", MFIELD_1)
HANDLES(MFIELD_1, "GetFieldOffset", ves_icall_RuntimeFieldInfo_GetFieldOffset, gint32, 1, (MonoReflectionField))
HANDLES(MFIELD_2, "GetParentType", ves_icall_RuntimeFieldInfo_GetParentType, MonoReflectionType, 2, (MonoReflectionField, MonoBoolean))
HANDLES(MFIELD_3, "GetRawConstantValue", ves_icall_RuntimeFieldInfo_GetRawConstantValue, MonoObject, 1, (MonoReflectionField))
HANDLES(MFIELD_4, "GetTypeModifiers", ves_icall_System_Reflection_FieldInfo_GetTypeModifiers, MonoArray, 2, (MonoReflectionField, MonoBoolean))
HANDLES(MFIELD_5, "GetValueInternal", ves_icall_RuntimeFieldInfo_GetValueInternal, MonoObject, 2, (MonoReflectionField, MonoObject))
HANDLES(MFIELD_6, "ResolveType", ves_icall_RuntimeFieldInfo_ResolveType, MonoReflectionType, 1, (MonoReflectionField))
HANDLES(MFIELD_7, "SetValueInternal", ves_icall_RuntimeFieldInfo_SetValueInternal, void, 3, (MonoReflectionField, MonoObject, MonoObject))
HANDLES_REUSE_WRAPPER(MFIELD_8, "UnsafeGetValue", ves_icall_RuntimeFieldInfo_GetValueInternal)
HANDLES(MFIELD_9, "get_core_clr_security_level", ves_icall_RuntimeFieldInfo_get_core_clr_security_level, int, 1, (MonoReflectionField))
HANDLES_REUSE_WRAPPER(MFIELD_10, "get_metadata_token", ves_icall_reflection_get_token)

ICALL_TYPE(RMETHODINFO, "System.Reflection.RuntimeMethodInfo", RMETHODINFO_1)
HANDLES(RMETHODINFO_1, "GetGenericArguments", ves_icall_RuntimeMethodInfo_GetGenericArguments, MonoArray, 1, (MonoReflectionMethod))
HANDLES_REUSE_WRAPPER(RMETHODINFO_2, "GetGenericMethodDefinition_impl", ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition)
HANDLES(RMETHODINFO_3, "GetMethodBodyInternal", ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodBodyInternal, MonoReflectionMethodBody, 1, (MonoMethod_ptr))
HANDLES(RMETHODINFO_4, "GetMethodFromHandleInternalType_native", ves_icall_System_Reflection_RuntimeMethodInfo_GetMethodFromHandleInternalType_native, MonoReflectionMethod, 3, (MonoMethod_ptr, MonoType_ptr, MonoBoolean))
HANDLES(RMETHODINFO_5, "GetPInvoke", ves_icall_RuntimeMethodInfo_GetPInvoke, void, 4, (MonoReflectionMethod, int_ref, MonoStringOut, MonoStringOut))
HANDLES_REUSE_WRAPPER(RMETHODINFO_6, "InternalInvoke", ves_icall_InternalInvoke)
HANDLES(RMETHODINFO_7, "MakeGenericMethod_impl", ves_icall_RuntimeMethodInfo_MakeGenericMethod_impl, MonoReflectionMethod, 2, (MonoReflectionMethod, MonoArray))
HANDLES(RMETHODINFO_8, "get_IsGenericMethod", ves_icall_RuntimeMethodInfo_get_IsGenericMethod, MonoBoolean, 1, (MonoReflectionMethod))
HANDLES(RMETHODINFO_9, "get_IsGenericMethodDefinition", ves_icall_RuntimeMethodInfo_get_IsGenericMethodDefinition, MonoBoolean, 1, (MonoReflectionMethod))
HANDLES(RMETHODINFO_10, "get_base_method", ves_icall_RuntimeMethodInfo_get_base_method, MonoReflectionMethod, 2, (MonoReflectionMethod, MonoBoolean))
HANDLES_REUSE_WRAPPER(RMETHODINFO_11, "get_core_clr_security_level", ves_icall_RuntimeMethodInfo_get_core_clr_security_level)
HANDLES_REUSE_WRAPPER(RMETHODINFO_12, "get_metadata_token", ves_icall_reflection_get_token)
HANDLES(RMETHODINFO_13, "get_name", ves_icall_RuntimeMethodInfo_get_name, MonoString, 1, (MonoReflectionMethod))

ICALL_TYPE(MODULE, "System.Reflection.RuntimeModule", MODULE_2)
HANDLES(MODULE_2, "GetGlobalType", ves_icall_System_Reflection_RuntimeModule_GetGlobalType, MonoReflectionType, 1, (MonoImage_ptr))
HANDLES(MODULE_3, "GetGuidInternal", ves_icall_System_Reflection_RuntimeModule_GetGuidInternal, void, 2, (MonoImage_ptr, MonoArray))
HANDLES(MODULE_14, "GetHINSTANCE", ves_icall_System_Reflection_RuntimeModule_GetHINSTANCE, gpointer, 1, (MonoImage_ptr))
HANDLES(MODULE_4, "GetMDStreamVersion", ves_icall_System_Reflection_RuntimeModule_GetMDStreamVersion, gint32, 1, (MonoImage_ptr))
HANDLES(MODULE_5, "GetPEKind", ves_icall_System_Reflection_RuntimeModule_GetPEKind, void, 3, (MonoImage_ptr, gint32_ptr, gint32_ptr))
HANDLES(MODULE_6, "InternalGetTypes", ves_icall_System_Reflection_RuntimeModule_InternalGetTypes, MonoArray, 1, (MonoImage_ptr))
HANDLES(MODULE_7, "ResolveFieldToken", ves_icall_System_Reflection_RuntimeModule_ResolveFieldToken, MonoClassField_ptr, 5, (MonoImage_ptr, guint32, MonoArray, MonoArray, MonoResolveTokenError_ref))
HANDLES(MODULE_8, "ResolveMemberToken", ves_icall_System_Reflection_RuntimeModule_ResolveMemberToken, MonoObject, 5, (MonoImage_ptr, guint32, MonoArray, MonoArray, MonoResolveTokenError_ref))
HANDLES(MODULE_9, "ResolveMethodToken", ves_icall_System_Reflection_RuntimeModule_ResolveMethodToken, MonoMethod_ptr, 5, (MonoImage_ptr, guint32, MonoArray, MonoArray, MonoResolveTokenError_ref))
HANDLES(MODULE_10, "ResolveSignature", ves_icall_System_Reflection_RuntimeModule_ResolveSignature, MonoArray, 3, (MonoImage_ptr, guint32, MonoResolveTokenError_ref))
HANDLES(MODULE_11, "ResolveStringToken", ves_icall_System_Reflection_RuntimeModule_ResolveStringToken, MonoString, 3, (MonoImage_ptr, guint32, MonoResolveTokenError_ref))
HANDLES(MODULE_12, "ResolveTypeToken", ves_icall_System_Reflection_RuntimeModule_ResolveTypeToken, MonoType_ptr, 5, (MonoImage_ptr, guint32, MonoArray, MonoArray, MonoResolveTokenError_ref))
HANDLES(MODULE_13, "get_MetadataToken", ves_icall_reflection_get_token, guint32, 1, (MonoObject))

ICALL_TYPE(PARAMI, "System.Reflection.RuntimeParameterInfo", MPARAMI_1)
HANDLES_REUSE_WRAPPER(MPARAMI_1, "GetMetadataToken", ves_icall_reflection_get_token)
HANDLES(MPARAMI_2, "GetTypeModifiers", ves_icall_RuntimeParameterInfo_GetTypeModifiers, MonoArray, 4, (MonoReflectionType, MonoObject, int, MonoBoolean))

ICALL_TYPE(MPROP, "System.Reflection.RuntimePropertyInfo", MPROP_1)
HANDLES(MPROP_1, "GetTypeModifiers", ves_icall_RuntimePropertyInfo_GetTypeModifiers, MonoArray, 2, (MonoReflectionProperty, MonoBoolean))
HANDLES(MPROP_2, "get_default_value", ves_icall_property_info_get_default_value, MonoObject, 1, (MonoReflectionProperty))
HANDLES_REUSE_WRAPPER(MPROP_3, "get_metadata_token", ves_icall_reflection_get_token)
HANDLES(MPROP_4, "get_property_info", ves_icall_RuntimePropertyInfo_get_property_info, void, 3, (MonoReflectionProperty, MonoPropertyInfo_ref, PInfo))
HANDLES(MPROP_5, "internal_from_handle_type", ves_icall_System_Reflection_RuntimePropertyInfo_internal_from_handle_type, MonoReflectionProperty, 2, (MonoProperty_ptr, MonoType_ptr))

ICALL_TYPE(RUNH, "System.Runtime.CompilerServices.RuntimeHelpers", RUNH_1)
HANDLES(RUNH_1, "GetObjectValue", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue, MonoObject, 1, (MonoObject))
HANDLES(RUNH_3, "InitializeArray", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray, void, 2, (MonoArray, MonoClassField_ptr))
HANDLES(RUNH_4, "RunClassConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor, void, 1, (MonoType_ptr))
HANDLES(RUNH_5, "RunModuleConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor, void, 1, (MonoImage_ptr))
NOHANDLES(ICALL(RUNH_5h, "SufficientExecutionStack", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack))
NOHANDLES(ICALL(RUNH_6, "get_OffsetToStringData", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData))

ICALL_TYPE(GCH, "System.Runtime.InteropServices.GCHandle", GCH_1)
NOHANDLES(ICALL(GCH_1, "CheckCurrentDomain", ves_icall_System_GCHandle_CheckCurrentDomain))
NOHANDLES(ICALL(GCH_2, "FreeHandle", ves_icall_System_GCHandle_FreeHandle))
NOHANDLES(ICALL(GCH_3, "GetAddrOfPinnedObject", ves_icall_System_GCHandle_GetAddrOfPinnedObject))
HANDLES(GCH_4, "GetTarget", ves_icall_System_GCHandle_GetTarget, MonoObject, 1, (guint32))
HANDLES(GCH_5, "GetTargetHandle", ves_icall_System_GCHandle_GetTargetHandle, guint32, 3, (MonoObject, guint32, gint32))

#if !defined(DISABLE_COM) || defined (HOST_WIN32)
ICALL_TYPE(MARSHAL, "System.Runtime.InteropServices.Marshal", MARSHAL_1)
NOHANDLES(ICALL(MARSHAL_1, "AddRefInternal", ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal))
#else
ICALL_TYPE(MARSHAL, "System.Runtime.InteropServices.Marshal", MARSHAL_2)
#endif
NOHANDLES(ICALL(MARSHAL_2, "AllocCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem))
NOHANDLES(ICALL(MARSHAL_51,"AllocCoTaskMemSize(uintptr)", ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMemSize))
NOHANDLES(ICALL(MARSHAL_3, "AllocHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal))
NOHANDLES(ICALL(MARSHAL_50, "BufferToBSTR", ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR))
HANDLES(MARSHAL_4, "DestroyStructure", ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure, void, 2, (gpointer, MonoReflectionType))
NOHANDLES(ICALL(MARSHAL_5, "FreeBSTR", ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR))
NOHANDLES(ICALL(MARSHAL_6, "FreeCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem))
NOHANDLES(ICALL(MARSHAL_7, "FreeHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal))
#ifndef DISABLE_COM
HANDLES(MARSHAL_44, "GetCCW", ves_icall_System_Runtime_InteropServices_Marshal_GetCCW, gpointer, 2, (MonoObject, MonoReflectionType))
HANDLES(MARSHAL_8, "GetComSlotForMethodInfoInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal, guint32, 1, (MonoReflectionMethod))
#endif
HANDLES(MARSHAL_9, "GetDelegateForFunctionPointerInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal, MonoDelegate, 2, (gpointer, MonoReflectionType))
HANDLES(MARSHAL_10, "GetFunctionPointerForDelegateInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal, gpointer, 1, (MonoDelegate))
#ifndef DISABLE_COM
HANDLES(MARSHAL_52, "GetHRForException_WinRT", ves_icall_System_Runtime_InteropServices_Marshal_GetHRForException_WinRT, int, 1, (MonoException))
HANDLES(MARSHAL_45, "GetIDispatchForObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal, gpointer, 1, (MonoObject))
HANDLES(MARSHAL_46, "GetIUnknownForObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal, gpointer, 1, (MonoObject))
#endif
NOHANDLES(ICALL(MARSHAL_11, "GetLastWin32Error", ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error))
#ifndef DISABLE_COM
HANDLES(MARSHAL_53, "GetNativeActivationFactory", ves_icall_System_Runtime_InteropServices_Marshal_GetNativeActivationFactory, MonoObject, 1, (MonoObject))
HANDLES(MARSHAL_47, "GetObjectForCCW", ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW, MonoObject, 1, (gpointer))
HANDLES(MARSHAL_54, "GetRawIUnknownForComObjectNoAddRef", ves_icall_System_Runtime_InteropServices_Marshal_GetRawIUnknownForComObjectNoAddRef, gpointer, 1, (MonoObject))
HANDLES(MARSHAL_48, "IsComObject", ves_icall_System_Runtime_InteropServices_Marshal_IsComObject, MonoBoolean, 1, (MonoObject))
#endif
HANDLES(MARSHAL_48a, "IsPinnableType", ves_icall_System_Runtime_InteropServices_Marshal_IsPinnableType, MonoBoolean, 1, (MonoReflectionType))
HANDLES(MARSHAL_12, "OffsetOf", ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf, int, 2, (MonoReflectionType, MonoString))
HANDLES(MARSHAL_13, "Prelink", ves_icall_System_Runtime_InteropServices_Marshal_Prelink, void, 1, (MonoReflectionMethod))
HANDLES(MARSHAL_14, "PrelinkAll", ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll, void, 1, (MonoReflectionType))
HANDLES(MARSHAL_15, "PtrToStringAnsi(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi, MonoString, 1, (const_char_ptr))
HANDLES(MARSHAL_16, "PtrToStringAnsi(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len, MonoString, 2, (const_char_ptr, gint32))
HANDLES(MARSHAL_17, "PtrToStringBSTR", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR, MonoString, 1, (mono_bstr_const))
HANDLES(MARSHAL_18, "PtrToStringUni(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni, MonoString, 1, (const_gunichar2_ptr))
HANDLES(MARSHAL_19, "PtrToStringUni(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len, MonoString, 2, (const_gunichar2_ptr, gint32))
HANDLES(MARSHAL_20, "PtrToStructure(intptr,System.Type)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type, MonoObject, 2, (gconstpointer, MonoReflectionType))
HANDLES(MARSHAL_21, "PtrToStructure(intptr,object)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure, void, 2, (gconstpointer, MonoObject))
#if !defined (DISABLE_COM) || defined (HOST_WIN32)
NOHANDLES(ICALL(MARSHAL_22, "QueryInterfaceInternal", ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal))
#endif
NOHANDLES(ICALL(MARSHAL_43, "ReAllocCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem))
NOHANDLES(ICALL(MARSHAL_23, "ReAllocHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal))
#ifndef DISABLE_COM
HANDLES(MARSHAL_49, "ReleaseComObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal, gint32, 1, (MonoObject))
#endif
#if !defined (DISABLE_COM) || defined (HOST_WIN32)
NOHANDLES(ICALL(MARSHAL_29, "ReleaseInternal", ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal))
#endif
NOHANDLES(ICALL(MARSHAL_29a, "SetLastWin32Error", ves_icall_System_Runtime_InteropServices_Marshal_SetLastWin32Error))
HANDLES(MARSHAL_30, "SizeOf", ves_icall_System_Runtime_InteropServices_Marshal_SizeOf, guint32, 1, (MonoReflectionType))
HANDLES(MARSHAL_31, "SizeOfHelper", ves_icall_System_Runtime_InteropServices_Marshal_SizeOfHelper, guint32, 2, (MonoReflectionType, MonoBoolean))
NOHANDLES(ICALL(MARSHAL_32, "StringToHGlobalAnsi", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi))
NOHANDLES(ICALL(MARSHAL_33, "StringToHGlobalUni", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni))
HANDLES(MARSHAL_34, "StructureToPtr", ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr, void, 3, (MonoObject, gpointer, MonoBoolean))
HANDLES(MARSHAL_35, "UnsafeAddrOfPinnedArrayElement", ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement, gpointer, 2, (MonoArray, int))
HANDLES(MARSHAL_41, "copy_from_unmanaged_fixed", ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged, void, 5, (gconstpointer, gint32, MonoArray, gint32, gpointer))
HANDLES(MARSHAL_42, "copy_to_unmanaged_fixed", ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged, void, 5, (MonoArray, gint32, gpointer, gint32, gconstpointer))

ICALL_TYPE(RUNTIMEINFO, "System.Runtime.InteropServices.RuntimeInformation", RUNTIMEINFO_1)
HANDLES(RUNTIMEINFO_1, "GetOSName", ves_icall_System_Runtime_InteropServices_RuntimeInformation_GetOSName, MonoString, 0, ())
HANDLES(RUNTIMEINFO_2, "GetRuntimeArchitecture", ves_icall_System_Runtime_InteropServices_RuntimeInformation_GetRuntimeArchitecture, MonoString, 0, ())

#ifndef DISABLE_COM
ICALL_TYPE(WINDOWSRUNTIME_UNM, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods", WINDOWSRUNTIME_UNM_0)
HANDLES(WINDOWSRUNTIME_UNM_0, "GetRestrictedErrorInfo", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_GetRestrictedErrorInfo, MonoObject, 0, ())
HANDLES(WINDOWSRUNTIME_UNM_1, "RoOriginateLanguageException", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoOriginateLanguageException, MonoBoolean, 3, (int, MonoString, gpointer))
HANDLES(WINDOWSRUNTIME_UNM_2, "RoReportUnhandledError", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoReportUnhandledError, void, 1, (MonoObject))
HANDLES(WINDOWSRUNTIME_UNM_3, "WindowsCreateString", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsCreateString, int, 3, (MonoString, int, gpointer_ptr))
HANDLES(WINDOWSRUNTIME_UNM_4, "WindowsDeleteString", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsDeleteString, int, 1, (gpointer))
HANDLES(WINDOWSRUNTIME_UNM_5, "WindowsGetStringRawBuffer", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsGetStringRawBuffer, mono_unichar2_ptr, 2, (gpointer, unsigned_ptr))
#endif

ICALL_TYPE(ACTS, "System.Runtime.Remoting.Activation.ActivationServices", ACTS_1)
HANDLES(ACTS_1, "AllocateUninitializedClassInstance", ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance, MonoObject, 1, (MonoReflectionType))
HANDLES(ACTS_2, "EnableProxyActivation", ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation, void, 2, (MonoReflectionType, MonoBoolean))

ICALL_TYPE(CONTEXT, "System.Runtime.Remoting.Contexts.Context", CONTEXT_1)
HANDLES(CONTEXT_1, "RegisterContext", ves_icall_System_Runtime_Remoting_Contexts_Context_RegisterContext, void, 1, (MonoAppContext))
HANDLES(CONTEXT_2, "ReleaseContext", ves_icall_System_Runtime_Remoting_Contexts_Context_ReleaseContext, void, 1, (MonoAppContext))

ICALL_TYPE(ARES, "System.Runtime.Remoting.Messaging.AsyncResult", ARES_1)
HANDLES(ARES_1, "Invoke", ves_icall_System_Runtime_Remoting_Messaging_AsyncResult_Invoke, MonoObject, 1, (MonoAsyncResult))

#ifndef DISABLE_REMOTING
ICALL_TYPE(REALP, "System.Runtime.Remoting.Proxies.RealProxy", REALP_1)
HANDLES(REALP_1, "InternalGetProxyType", ves_icall_Remoting_RealProxy_InternalGetProxyType, MonoReflectionType, 1, (MonoTransparentProxy))
HANDLES(REALP_2, "InternalGetTransparentProxy", ves_icall_Remoting_RealProxy_GetTransparentProxy, MonoObject, 2, (MonoObject, MonoString))

ICALL_TYPE(REMSER, "System.Runtime.Remoting.RemotingServices", REMSER_0)
HANDLES(REMSER_0, "GetVirtualMethod", ves_icall_Remoting_RemotingServices_GetVirtualMethod, MonoReflectionMethod, 2, (MonoReflectionType, MonoReflectionMethod))
HANDLES(REMSER_1, "InternalExecute", ves_icall_InternalExecute, MonoObject, 4, (MonoReflectionMethod, MonoObject, MonoArray, MonoArrayOut))
HANDLES(REMSER_2, "IsTransparentProxy", ves_icall_IsTransparentProxy, MonoBoolean, 1, (MonoObject))
#endif

ICALL_TYPE(RUNIMPORT, "System.Runtime.RuntimeImports", RUNIMPORT_1)
NOHANDLES(ICALL(RUNIMPORT_1, "Memmove", ves_icall_System_Runtime_RuntimeImports_Memmove))
NOHANDLES(ICALL(RUNIMPORT_2, "Memmove_wbarrier", ves_icall_System_Runtime_RuntimeImports_Memmove_wbarrier))
NOHANDLES(ICALL(RUNIMPORT_3, "ZeroMemory", ves_icall_System_Runtime_RuntimeImports_ZeroMemory))
NOHANDLES(ICALL(RUNIMPORT_4, "_ecvt_s", ves_icall_System_Runtime_RuntimeImports_ecvt_s))

ICALL_TYPE(RVH, "System.Runtime.Versioning.VersioningHelper", RVH_1)
HANDLES(RVH_1, "GetRuntimeId", ves_icall_System_Runtime_Versioning_VersioningHelper_GetRuntimeId, gint32, 0, ())

ICALL_TYPE(RFH, "System.RuntimeFieldHandle", RFH_1)
HANDLES(RFH_1, "GetValueDirect", ves_icall_System_RuntimeFieldHandle_GetValueDirect, MonoObject, 4, (MonoReflectionField, MonoReflectionType, MonoTypedRef_ptr, MonoReflectionType))
HANDLES(RFH_1a, "SetValueDirect", ves_icall_System_RuntimeFieldHandle_SetValueDirect, void, 5, (MonoReflectionField, MonoReflectionType, MonoTypedRef_ptr, MonoObject, MonoReflectionType))
HANDLES_REUSE_WRAPPER(RFH_2, "SetValueInternal", ves_icall_RuntimeFieldInfo_SetValueInternal)

ICALL_TYPE(MHAN, "System.RuntimeMethodHandle", MHAN_1)
HANDLES(MHAN_1, "GetFunctionPointer", ves_icall_RuntimeMethodHandle_GetFunctionPointer, gpointer, 1, (MonoMethod_ptr))

ICALL_TYPE(RT, "System.RuntimeType", RT_1)
HANDLES(RT_1, "CreateInstanceInternal", ves_icall_System_Activator_CreateInstanceInternal, MonoObject, 1, (MonoReflectionType))
HANDLES(RT_2, "GetConstructors_native", ves_icall_RuntimeType_GetConstructors_native, GPtrArray_ptr, 2, (MonoReflectionType, guint32))
HANDLES(RT_30, "GetCorrespondingInflatedConstructor", ves_icall_RuntimeType_GetCorrespondingInflatedMethod, MonoReflectionMethod, 2, (MonoReflectionType, MonoReflectionMethod))
HANDLES_REUSE_WRAPPER(RT_31, "GetCorrespondingInflatedMethod", ves_icall_RuntimeType_GetCorrespondingInflatedMethod)
HANDLES(RT_3, "GetEvents_native", ves_icall_RuntimeType_GetEvents_native, GPtrArray_ptr, 3, (MonoReflectionType, char_ptr, guint32))
HANDLES(RT_5, "GetFields_native", ves_icall_RuntimeType_GetFields_native, GPtrArray_ptr, 4, (MonoReflectionType, char_ptr, guint32, guint32))
HANDLES(RT_6, "GetGenericArgumentsInternal", ves_icall_RuntimeType_GetGenericArguments, MonoArray, 2, (MonoReflectionType, MonoBoolean))
HANDLES(RT_9, "GetGenericParameterPosition", ves_icall_RuntimeType_GetGenericParameterPosition, gint32, 1, (MonoReflectionType))
HANDLES(RT_10, "GetInterfaceMapData", ves_icall_RuntimeType_GetInterfaceMapData, void, 4, (MonoReflectionType, MonoReflectionType, MonoArrayOut, MonoArrayOut))
HANDLES(RT_11, "GetInterfaces", ves_icall_RuntimeType_GetInterfaces, MonoArray, 1, (MonoReflectionType))
HANDLES(RT_12, "GetMethodsByName_native", ves_icall_RuntimeType_GetMethodsByName_native, GPtrArray_ptr, 4, (MonoReflectionType, const_char_ptr, guint32, guint32))
HANDLES(RT_13, "GetNestedTypes_native", ves_icall_RuntimeType_GetNestedTypes_native, GPtrArray_ptr, 4, (MonoReflectionType, char_ptr, guint32, guint32))
HANDLES(RT_14, "GetPacking", ves_icall_RuntimeType_GetPacking, void, 3, (MonoReflectionType, guint32_ref, guint32_ref))
HANDLES(RT_15, "GetPropertiesByName_native", ves_icall_RuntimeType_GetPropertiesByName_native, GPtrArray_ptr, 4, (MonoReflectionType, char_ptr, guint32, guint32))
HANDLES(RT_16, "GetTypeCodeImplInternal", ves_icall_type_GetTypeCodeInternal, guint32, 1, (MonoReflectionType))
HANDLES(RT_28, "IsTypeExportedToWindowsRuntime", ves_icall_System_RuntimeType_IsTypeExportedToWindowsRuntime, MonoBoolean, 0, ())
HANDLES(RT_29, "IsWindowsRuntimeObjectType", ves_icall_System_RuntimeType_IsWindowsRuntimeObjectType, MonoBoolean, 0, ())
HANDLES(RT_17, "MakeGenericType", ves_icall_RuntimeType_MakeGenericType, MonoReflectionType, 2, (MonoReflectionType, MonoArray))
HANDLES(RT_18, "MakePointerType", ves_icall_RuntimeType_MakePointerType, MonoReflectionType, 1, (MonoReflectionType))
HANDLES(RT_19, "getFullName", ves_icall_System_RuntimeType_getFullName, MonoString, 3, (MonoReflectionType, MonoBoolean, MonoBoolean))
HANDLES(RT_21, "get_DeclaringMethod", ves_icall_RuntimeType_get_DeclaringMethod, MonoReflectionMethod, 1, (MonoReflectionType))
HANDLES(RT_22, "get_DeclaringType", ves_icall_RuntimeType_get_DeclaringType, MonoReflectionType, 1, (MonoReflectionType))
HANDLES(RT_23, "get_Name", ves_icall_RuntimeType_get_Name, MonoString, 1, (MonoReflectionType))
HANDLES(RT_24, "get_Namespace", ves_icall_RuntimeType_get_Namespace, MonoString, 1, (MonoReflectionType))
HANDLES(RT_25, "get_core_clr_security_level", ves_icall_RuntimeType_get_core_clr_security_level, int, 1, (MonoReflectionType))
HANDLES(RT_26, "make_array_type", ves_icall_RuntimeType_make_array_type, MonoReflectionType, 2, (MonoReflectionType, int))
HANDLES(RT_27, "make_byref_type", ves_icall_RuntimeType_make_byref_type, MonoReflectionType, 1, (MonoReflectionType))

ICALL_TYPE(RTH, "System.RuntimeTypeHandle", RTH_1)
HANDLES(RTH_1, "GetArrayRank", ves_icall_RuntimeTypeHandle_GetArrayRank, gint32, 1, (MonoReflectionType))
HANDLES(RTH_2, "GetAssembly", ves_icall_RuntimeTypeHandle_GetAssembly, MonoReflectionAssembly, 1, (MonoReflectionType))
HANDLES(RTH_3, "GetAttributes", ves_icall_RuntimeTypeHandle_GetAttributes, guint32, 1, (MonoReflectionType))
HANDLES(RTH_4, "GetBaseType", ves_icall_RuntimeTypeHandle_GetBaseType, MonoReflectionType, 1, (MonoReflectionType))
HANDLES(RTH_4a, "GetCorElementType", ves_icall_RuntimeTypeHandle_GetCorElementType, guint32, 1, (MonoReflectionType))
HANDLES(RTH_5, "GetElementType", ves_icall_RuntimeTypeHandle_GetElementType, MonoReflectionType, 1, (MonoReflectionType))
HANDLES(RTH_19, "GetGenericParameterInfo", ves_icall_RuntimeTypeHandle_GetGenericParameterInfo, MonoGenericParamInfo_ptr, 1, (MonoReflectionType))
HANDLES(RTH_6, "GetGenericTypeDefinition_impl", ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl, MonoReflectionType, 1, (MonoReflectionType))
HANDLES_REUSE_WRAPPER(RTH_7, "GetMetadataToken", ves_icall_reflection_get_token)
HANDLES(RTH_8, "GetModule", ves_icall_RuntimeTypeHandle_GetModule, MonoReflectionModule, 1, (MonoReflectionType))
HANDLES(RTH_9, "HasInstantiation", ves_icall_RuntimeTypeHandle_HasInstantiation, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_20, "HasReferences", ves_icall_RuntimeTypeHandle_HasReferences, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_21, "IsByRefLike", ves_icall_RuntimeTypeHandle_IsByRefLike, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_12, "IsComObject", ves_icall_RuntimeTypeHandle_IsComObject, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_13, "IsGenericTypeDefinition", ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_14, "IsGenericVariable", ves_icall_RuntimeTypeHandle_IsGenericVariable, MonoBoolean, 1, (MonoReflectionType))
HANDLES(RTH_15, "IsInstanceOfType", ves_icall_RuntimeTypeHandle_IsInstanceOfType, guint32, 2, (MonoReflectionType, MonoObject))
//HANDLES(RTH_17a, "is_subclass_of", ves_icall_RuntimeTypeHandle_is_subclass_of, MonoBoolean, 2, (MonoType_ptr, MonoType_ptr))
HANDLES(RTH_17a, "internal_from_name", ves_icall_System_RuntimeTypeHandle_internal_from_name, MonoReflectionType, 6, (MonoString, MonoStackCrawlMark_ptr, MonoReflectionAssembly, MonoBoolean, MonoBoolean, MonoBoolean))
NOHANDLES(ICALL(RTH_17b, "is_subclass_of", ves_icall_RuntimeTypeHandle_is_subclass_of))
HANDLES(RTH_18, "type_is_assignable_from", ves_icall_RuntimeTypeHandle_type_is_assignable_from, guint32, 2, (MonoReflectionType, MonoReflectionType))

ICALL_TYPE(RNG, "System.Security.Cryptography.RNGCryptoServiceProvider", RNG_1)
HANDLES(RNG_1, "RngClose", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose, void, 1, (gpointer))
HANDLES(RNG_2, "RngGetBytes", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes, gpointer, 3, (gpointer, guchar_ptr, gssize))
HANDLES(RNG_3, "RngInitialize", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize, gpointer, 2, (const_guchar_ptr, gssize))
HANDLES(RNG_4, "RngOpen", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen, MonoBoolean, 0, ())

ICALL_TYPE(EVID, "System.Security.Policy.Evidence", EVID_1)
HANDLES(EVID_1, "IsAuthenticodePresent", ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent, MonoBoolean, 1, (MonoReflectionAssembly))

ICALL_TYPE(WINID, "System.Security.Principal.WindowsIdentity", WINID_1)
HANDLES(WINID_1, "GetCurrentToken", ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken, gpointer, 0, ())
HANDLES(WINID_2, "GetTokenName", ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName, MonoString, 1, (gpointer))
HANDLES(WINID_3, "GetUserToken", ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken, gpointer, 1, (MonoString))
HANDLES(WINID_4, "_GetRoles", ves_icall_System_Security_Principal_WindowsIdentity_GetRoles, MonoArray, 1, (gpointer))

ICALL_TYPE(WINIMP, "System.Security.Principal.WindowsImpersonationContext", WINIMP_1)
HANDLES(WINIMP_1, "CloseToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken, MonoBoolean, 1, (gpointer))
HANDLES(WINIMP_2, "DuplicateToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken, gpointer, 1, (gpointer))
HANDLES(WINIMP_3, "RevertToSelf", ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf, MonoBoolean, 0, ())
HANDLES(WINIMP_4, "SetCurrentToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken, MonoBoolean, 1, (gpointer))

ICALL_TYPE(WINPRIN, "System.Security.Principal.WindowsPrincipal", WINPRIN_1)
HANDLES(WINPRIN_1, "IsMemberOfGroupId", ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId, MonoBoolean, 2, (gpointer, gpointer))
HANDLES(WINPRIN_2, "IsMemberOfGroupName", ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName, MonoBoolean, 2, (gpointer, const_char_ptr))

ICALL_TYPE(SECSTRING, "System.Security.SecureString", SECSTRING_1)
HANDLES(SECSTRING_1, "DecryptInternal", ves_icall_System_Security_SecureString_DecryptInternal, void, 2, (MonoArray, MonoObject))
HANDLES(SECSTRING_2, "EncryptInternal", ves_icall_System_Security_SecureString_EncryptInternal, void, 2, (MonoArray, MonoObject))

ICALL_TYPE(SECMAN, "System.Security.SecurityManager", SECMAN_1)
NOHANDLES(ICALL(SECMAN_1, "get_RequiresElevatedPermissions", mono_security_core_clr_require_elevated_permissions))
NOHANDLES(ICALL(SECMAN_2, "get_SecurityEnabled", ves_icall_System_Security_SecurityManager_get_SecurityEnabled))
NOHANDLES(ICALL(SECMAN_3, "set_SecurityEnabled", ves_icall_System_Security_SecurityManager_set_SecurityEnabled))

ICALL_TYPE(STRING, "System.String", STRING_1)
NOHANDLES(ICALL(STRING_1, ".ctor(System.ReadOnlySpan`1<char>)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_1a, ".ctor(char*)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_2, ".ctor(char*,int,int)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_3, ".ctor(char,int)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_4, ".ctor(char[])", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_5, ".ctor(char[],int,int)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_6, ".ctor(sbyte*)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_7, ".ctor(sbyte*,int,int)", ves_icall_System_String_ctor_RedirectToCreateString))
NOHANDLES(ICALL(STRING_8, ".ctor(sbyte*,int,int,System.Text.Encoding)", ves_icall_System_String_ctor_RedirectToCreateString))
HANDLES(STRING_9, "FastAllocateString", ves_icall_System_String_FastAllocateString, MonoString, 1, (gint32))
HANDLES(STRING_10, "InternalIntern", ves_icall_System_String_InternalIntern, MonoString, 1, (MonoString))
HANDLES(STRING_11, "InternalIsInterned", ves_icall_System_String_InternalIsInterned, MonoString, 1, (MonoString))

ICALL_TYPE(TENC, "System.Text.EncodingHelper", TENC_1)
HANDLES(TENC_1, "InternalCodePage", ves_icall_System_Text_EncodingHelper_InternalCodePage, MonoString, 1, (gint32_ref))

ICALL_TYPE(UNORM, "System.Text.Normalization", UNORM_1)
HANDLES(UNORM_1, "load_normalization_resource", ves_icall_System_Text_Normalization_load_normalization_resource, void, 6, (guint8_ptr_ref, guint8_ptr_ref, guint8_ptr_ref, guint8_ptr_ref, guint8_ptr_ref, guint8_ptr_ref))

ICALL_TYPE(ILOCK, "System.Threading.Interlocked", ILOCK_1)
NOHANDLES(ICALL(ILOCK_1, "Add(int&,int)", ves_icall_System_Threading_Interlocked_Add_Int))
NOHANDLES(ICALL(ILOCK_2, "Add(long&,long)", ves_icall_System_Threading_Interlocked_Add_Long))
NOHANDLES(ICALL(ILOCK_4, "CompareExchange(double&,double,double)", ves_icall_System_Threading_Interlocked_CompareExchange_Double))
NOHANDLES(ICALL(ILOCK_5, "CompareExchange(int&,int,int)", ves_icall_System_Threading_Interlocked_CompareExchange_Int))
NOHANDLES(ICALL(ILOCK_6, "CompareExchange(int&,int,int,bool&)", ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success))
NOHANDLES(ICALL(ILOCK_7, "CompareExchange(intptr&,intptr,intptr)", ves_icall_System_Threading_Interlocked_CompareExchange_IntPtr))
NOHANDLES(ICALL(ILOCK_8, "CompareExchange(long&,long,long)", ves_icall_System_Threading_Interlocked_CompareExchange_Long))
NOHANDLES(ICALL(ILOCK_9, "CompareExchange(object&,object&,object&,object&)", ves_icall_System_Threading_Interlocked_CompareExchange_Object))
NOHANDLES(ICALL(ILOCK_10, "CompareExchange(single&,single,single)", ves_icall_System_Threading_Interlocked_CompareExchange_Single))
NOHANDLES(ICALL(ILOCK_11, "Decrement(int&)", ves_icall_System_Threading_Interlocked_Decrement_Int))
NOHANDLES(ICALL(ILOCK_12, "Decrement(long&)", ves_icall_System_Threading_Interlocked_Decrement_Long))
NOHANDLES(ICALL(ILOCK_14, "Exchange(double&,double)", ves_icall_System_Threading_Interlocked_Exchange_Double))
NOHANDLES(ICALL(ILOCK_15, "Exchange(int&,int)", ves_icall_System_Threading_Interlocked_Exchange_Int))
NOHANDLES(ICALL(ILOCK_16, "Exchange(intptr&,intptr)", ves_icall_System_Threading_Interlocked_Exchange_IntPtr))
NOHANDLES(ICALL(ILOCK_17, "Exchange(long&,long)", ves_icall_System_Threading_Interlocked_Exchange_Long))
NOHANDLES(ICALL(ILOCK_18, "Exchange(object&,object&,object&)", ves_icall_System_Threading_Interlocked_Exchange_Object))
NOHANDLES(ICALL(ILOCK_19, "Exchange(single&,single)", ves_icall_System_Threading_Interlocked_Exchange_Single))
NOHANDLES(ICALL(ILOCK_20, "Increment(int&)", ves_icall_System_Threading_Interlocked_Increment_Int))
NOHANDLES(ICALL(ILOCK_21, "Increment(long&)", ves_icall_System_Threading_Interlocked_Increment_Long))
NOHANDLES(ICALL(ILOCK_22, "MemoryBarrierProcessWide", ves_icall_System_Threading_Interlocked_MemoryBarrierProcessWide))
NOHANDLES(ICALL(ILOCK_23, "Read(long&)", ves_icall_System_Threading_Interlocked_Read_Long))

ICALL_TYPE(ITHREAD, "System.Threading.InternalThread", ITHREAD_1)
HANDLES(ITHREAD_1, "Thread_free_internal", ves_icall_System_Threading_InternalThread_Thread_free_internal, void, 1, (MonoInternalThread))

ICALL_TYPE(MONIT, "System.Threading.Monitor", MONIT_8)
HANDLES(MONIT_8, "Enter", ves_icall_System_Threading_Monitor_Monitor_Enter, void, 1, (MonoObject))
HANDLES(MONIT_1, "Exit", mono_monitor_exit_icall, void, 1, (MonoObject))
HANDLES(MONIT_2, "Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse, void, 1, (MonoObject))
HANDLES(MONIT_3, "Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all, void, 1, (MonoObject))
HANDLES(MONIT_4, "Monitor_test_owner", ves_icall_System_Threading_Monitor_Monitor_test_owner, MonoBoolean, 1, (MonoObject))
HANDLES(MONIT_5, "Monitor_test_synchronised", ves_icall_System_Threading_Monitor_Monitor_test_synchronised, MonoBoolean, 1, (MonoObject))
HANDLES(MONIT_7, "Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait, MonoBoolean, 2, (MonoObject, guint32))
HANDLES(MONIT_9, "try_enter_with_atomic_var", ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var, void, 3, (MonoObject, guint32, MonoBoolean_ref))

ICALL_TYPE(MUTEX, "System.Threading.Mutex", MUTEX_1)
HANDLES(MUTEX_1, "CreateMutex_icall", ves_icall_System_Threading_Mutex_CreateMutex_icall, gpointer, 4, (MonoBoolean, const_gunichar2_ptr, gint32, MonoBoolean_ref))
HANDLES(MUTEX_2, "OpenMutex_icall", ves_icall_System_Threading_Mutex_OpenMutex_icall, gpointer, 4, (const_gunichar2_ptr, gint32, gint32, gint32_ref))
NOHANDLES(ICALL(MUTEX_3, "ReleaseMutex_internal", ves_icall_System_Threading_Mutex_ReleaseMutex_internal))

ICALL_TYPE(NATIVEC, "System.Threading.NativeEventCalls", NATIVEC_1)
NOHANDLES(ICALL(NATIVEC_1, "CloseEvent_internal", ves_icall_System_Threading_Events_CloseEvent_internal))
HANDLES(NATIVEC_2, "CreateEvent_icall", ves_icall_System_Threading_Events_CreateEvent_icall, gpointer, 5, (MonoBoolean, MonoBoolean, const_gunichar2_ptr, gint32, gint32_ref))
HANDLES(NATIVEC_3, "OpenEvent_icall", ves_icall_System_Threading_Events_OpenEvent_icall, gpointer, 4, (const_gunichar2_ptr, gint32, gint32, gint32_ref))
NOHANDLES(ICALL(NATIVEC_4, "ResetEvent_internal",  ves_icall_System_Threading_Events_ResetEvent_internal))
NOHANDLES(ICALL(NATIVEC_5, "SetEvent_internal",    ves_icall_System_Threading_Events_SetEvent_internal))

ICALL_TYPE(SEMA, "System.Threading.Semaphore", SEMA_1)
NOHANDLES(ICALL(SEMA_1, "CreateSemaphore_icall", ves_icall_System_Threading_Semaphore_CreateSemaphore_icall))
NOHANDLES(ICALL(SEMA_2, "OpenSemaphore_icall", ves_icall_System_Threading_Semaphore_OpenSemaphore_icall))
NOHANDLES(ICALL(SEMA_3, "ReleaseSemaphore_internal", ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal))

ICALL_TYPE(THREAD, "System.Threading.Thread", THREAD_1)
HANDLES(THREAD_1, "Abort_internal(System.Threading.InternalThread,object)", ves_icall_System_Threading_Thread_Abort, void, 2, (MonoInternalThread, MonoObject))
HANDLES(THREAD_1a, "ByteArrayToCurrentDomain(byte[])", ves_icall_System_Threading_Thread_ByteArrayToCurrentDomain, MonoArray, 1, (MonoArray))
HANDLES(THREAD_1b, "ByteArrayToRootDomain(byte[])", ves_icall_System_Threading_Thread_ByteArrayToRootDomain, MonoArray, 1, (MonoArray))
HANDLES(THREAD_2, "ClrState(System.Threading.InternalThread,System.Threading.ThreadState)", ves_icall_System_Threading_Thread_ClrState, void, 2, (MonoInternalThread, guint32))
HANDLES(THREAD_2a, "ConstructInternalThread", ves_icall_System_Threading_Thread_ConstructInternalThread, void, 1, (MonoThreadObject))
HANDLES(THREAD_55, "GetAbortExceptionState", ves_icall_System_Threading_Thread_GetAbortExceptionState, MonoObject, 1, (MonoThreadObject))
NOHANDLES(ICALL(THREAD_60, "GetCurrentThread_icall", ves_icall_System_Threading_Thread_GetCurrentThread))
HANDLES(THREAD_7, "GetDomainID", ves_icall_System_Threading_Thread_GetDomainID, gint32, 0, ())
HANDLES(THREAD_8, "GetName_internal(System.Threading.InternalThread)", ves_icall_System_Threading_Thread_GetName_internal, MonoString, 1, (MonoInternalThread))
HANDLES(THREAD_57, "GetPriorityNative", ves_icall_System_Threading_Thread_GetPriority, int, 1, (MonoThreadObject))
HANDLES(THREAD_59, "GetStackTraces", ves_icall_System_Threading_Thread_GetStackTraces, void, 2, (MonoArrayOut, MonoArrayOut))
HANDLES(THREAD_11, "GetState(System.Threading.InternalThread)", ves_icall_System_Threading_Thread_GetState, guint32, 1, (MonoInternalThread))
HANDLES(THREAD_53, "InterruptInternal", ves_icall_System_Threading_Thread_Interrupt_internal, void, 1, (MonoThreadObject))
HANDLES(THREAD_12, "JoinInternal", ves_icall_System_Threading_Thread_Join_internal, MonoBoolean, 2, (MonoThreadObject, int))
NOHANDLES(ICALL(THREAD_13, "MemoryBarrier", ves_icall_System_Threading_Thread_MemoryBarrier))
HANDLES(THREAD_14, "ResetAbortNative", ves_icall_System_Threading_Thread_ResetAbort, void, 1, (MonoThreadObject))
HANDLES(THREAD_15, "ResumeInternal", ves_icall_System_Threading_Thread_Resume, void, 1, (MonoThreadObject))
HANDLES(THREAD_18, "SetName_icall", ves_icall_System_Threading_Thread_SetName_icall, void, 3, (MonoInternalThread, const_gunichar2_ptr, gint32))
HANDLES(THREAD_58, "SetPriorityNative", ves_icall_System_Threading_Thread_SetPriority, void, 2, (MonoThreadObject, int))
HANDLES(THREAD_21, "SetState(System.Threading.InternalThread,System.Threading.ThreadState)", ves_icall_System_Threading_Thread_SetState, void, 2, (MonoInternalThread, guint32))
HANDLES(THREAD_22, "SleepInternal", ves_icall_System_Threading_Thread_Sleep_internal, void, 1, (gint32))
HANDLES(THREAD_54, "SpinWait_nop", ves_icall_System_Threading_Thread_SpinWait_nop, void, 0, ())
HANDLES(THREAD_23, "SuspendInternal", ves_icall_System_Threading_Thread_Suspend, void, 1, (MonoThreadObject))
// FIXME SystemMaxStackStize should be SystemMaxStackSize
NOHANDLES(ICALL(THREAD_56, "SystemMaxStackStize", ves_icall_System_Threading_Thread_SystemMaxStackSize))
HANDLES(THREAD_25, "Thread_internal", ves_icall_System_Threading_Thread_Thread_internal, MonoBoolean, 2, (MonoThreadObject, MonoObject))
NOHANDLES(ICALL(THREAD_26, "VolatileRead(byte&)", ves_icall_System_Threading_Thread_VolatileRead1))
NOHANDLES(ICALL(THREAD_27, "VolatileRead(double&)", ves_icall_System_Threading_Thread_VolatileReadDouble))
NOHANDLES(ICALL(THREAD_28, "VolatileRead(int&)", ves_icall_System_Threading_Thread_VolatileRead4))
NOHANDLES(ICALL(THREAD_29, "VolatileRead(int16&)", ves_icall_System_Threading_Thread_VolatileRead2))
NOHANDLES(ICALL(THREAD_30, "VolatileRead(intptr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr))
NOHANDLES(ICALL(THREAD_31, "VolatileRead(long&)", ves_icall_System_Threading_Thread_VolatileRead8))
NOHANDLES(ICALL(THREAD_32, "VolatileRead(object&)", ves_icall_System_Threading_Thread_VolatileReadObject))
NOHANDLES(ICALL(THREAD_33, "VolatileRead(sbyte&)", ves_icall_System_Threading_Thread_VolatileRead1))
NOHANDLES(ICALL(THREAD_34, "VolatileRead(single&)", ves_icall_System_Threading_Thread_VolatileReadFloat))
NOHANDLES(ICALL(THREAD_35, "VolatileRead(uint&)", ves_icall_System_Threading_Thread_VolatileRead4))
NOHANDLES(ICALL(THREAD_36, "VolatileRead(uint16&)", ves_icall_System_Threading_Thread_VolatileRead2))
NOHANDLES(ICALL(THREAD_37, "VolatileRead(uintptr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr))
NOHANDLES(ICALL(THREAD_38, "VolatileRead(ulong&)", ves_icall_System_Threading_Thread_VolatileRead8))
NOHANDLES(ICALL(THREAD_39, "VolatileWrite(byte&,byte)", ves_icall_System_Threading_Thread_VolatileWrite1))
NOHANDLES(ICALL(THREAD_40, "VolatileWrite(double&,double)", ves_icall_System_Threading_Thread_VolatileWriteDouble))
NOHANDLES(ICALL(THREAD_41, "VolatileWrite(int&,int)", ves_icall_System_Threading_Thread_VolatileWrite4))
NOHANDLES(ICALL(THREAD_42, "VolatileWrite(int16&,int16)", ves_icall_System_Threading_Thread_VolatileWrite2))
NOHANDLES(ICALL(THREAD_43, "VolatileWrite(intptr&,intptr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr))
NOHANDLES(ICALL(THREAD_44, "VolatileWrite(long&,long)", ves_icall_System_Threading_Thread_VolatileWrite8))
NOHANDLES(ICALL(THREAD_45, "VolatileWrite(object&,object)", ves_icall_System_Threading_Thread_VolatileWriteObject))
NOHANDLES(ICALL(THREAD_46, "VolatileWrite(sbyte&,sbyte)", ves_icall_System_Threading_Thread_VolatileWrite1))
NOHANDLES(ICALL(THREAD_47, "VolatileWrite(single&,single)", ves_icall_System_Threading_Thread_VolatileWriteFloat))
NOHANDLES(ICALL(THREAD_48, "VolatileWrite(uint&,uint)", ves_icall_System_Threading_Thread_VolatileWrite4))
NOHANDLES(ICALL(THREAD_49, "VolatileWrite(uint16&,uint16)", ves_icall_System_Threading_Thread_VolatileWrite2))
NOHANDLES(ICALL(THREAD_50, "VolatileWrite(uintptr&,uintptr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr))
NOHANDLES(ICALL(THREAD_51, "VolatileWrite(ulong&,ulong)", ves_icall_System_Threading_Thread_VolatileWrite8))
NOHANDLES(ICALL(THREAD_9, "YieldInternal", ves_icall_System_Threading_Thread_YieldInternal))

ICALL_TYPE(THREADP, "System.Threading.ThreadPool", THREADP_2)
HANDLES(THREADP_2, "GetAvailableThreadsNative", ves_icall_System_Threading_ThreadPool_GetAvailableThreadsNative, void, 2, (gint32_ref, gint32_ref))
HANDLES(THREADP_3, "GetMaxThreadsNative", ves_icall_System_Threading_ThreadPool_GetMaxThreadsNative, void, 2, (gint32_ref, gint32_ref))
HANDLES(THREADP_4, "GetMinThreadsNative", ves_icall_System_Threading_ThreadPool_GetMinThreadsNative, void, 2, (gint32_ref, gint32_ref))
HANDLES(THREADP_5, "InitializeVMTp", ves_icall_System_Threading_ThreadPool_InitializeVMTp, void, 1, (MonoBoolean_ref))
HANDLES(THREADP_7, "NotifyWorkItemComplete", ves_icall_System_Threading_ThreadPool_NotifyWorkItemComplete, MonoBoolean, 0, ())
HANDLES(THREADP_8, "NotifyWorkItemProgressNative", ves_icall_System_Threading_ThreadPool_NotifyWorkItemProgressNative, void, 0, ())
HANDLES(THREADP_8m, "NotifyWorkItemQueued", ves_icall_System_Threading_ThreadPool_NotifyWorkItemQueued, void, 0, ())
HANDLES(THREADP_11, "ReportThreadStatus", ves_icall_System_Threading_ThreadPool_ReportThreadStatus, void, 1, (MonoBoolean))
HANDLES(THREADP_12, "RequestWorkerThread", ves_icall_System_Threading_ThreadPool_RequestWorkerThread, MonoBoolean, 0, ())
HANDLES(THREADP_13, "SetMaxThreadsNative", ves_icall_System_Threading_ThreadPool_SetMaxThreadsNative, MonoBoolean, 2, (gint32, gint32))
HANDLES(THREADP_14, "SetMinThreadsNative", ves_icall_System_Threading_ThreadPool_SetMinThreadsNative, MonoBoolean, 2, (gint32, gint32))

ICALL_TYPE(TTIMER, "System.Threading.Timer", TTIMER_1)
NOHANDLES(ICALL(TTIMER_1, "GetTimeMonotonic", ves_icall_System_Threading_Timer_GetTimeMonotonic))

ICALL_TYPE(VOLATILE, "System.Threading.Volatile", VOLATILE_1)
NOHANDLES(ICALL(VOLATILE_1, "Read(double&)", ves_icall_System_Threading_Volatile_ReadDouble))
NOHANDLES(ICALL(VOLATILE_2, "Read(long&)", ves_icall_System_Threading_Volatile_Read8))
NOHANDLES(ICALL(VOLATILE_3, "Read(ulong&)", ves_icall_System_Threading_Volatile_ReadU8))
NOHANDLES(ICALL(VOLATILE_4, "Write(double&,double)", ves_icall_System_Threading_Volatile_WriteDouble))
NOHANDLES(ICALL(VOLATILE_5, "Write(long&,long)", ves_icall_System_Threading_Volatile_Write8))
NOHANDLES(ICALL(VOLATILE_6, "Write(ulong&,ulong)", ves_icall_System_Threading_Volatile_WriteU8))

ICALL_TYPE(WAITH, "System.Threading.WaitHandle", WAITH_1)
HANDLES(WAITH_1, "SignalAndWait_Internal", ves_icall_System_Threading_WaitHandle_SignalAndWait_Internal, gint32, 3, (gpointer, gpointer, gint32))
HANDLES(WAITH_2, "Wait_internal", ves_icall_System_Threading_WaitHandle_Wait_internal, gint32, 4, (gpointer_ptr, gint32, MonoBoolean, gint32))

ICALL_TYPE(TYPE, "System.Type", TYPE_1)
HANDLES(TYPE_1, "internal_from_handle", ves_icall_System_Type_internal_from_handle, MonoReflectionType, 1, (MonoType_ref))

ICALL_TYPE(TYPEDR, "System.TypedReference", TYPEDR_1)
HANDLES(TYPEDR_1, "InternalMakeTypedReference", ves_icall_System_TypedReference_InternalMakeTypedReference, void, 4, (MonoTypedRef_ptr, MonoObject, MonoArray, MonoReflectionType))
HANDLES(TYPEDR_2, "InternalToObject", ves_icall_System_TypedReference_ToObject, MonoObject, 1, (MonoTypedRef_ptr))

ICALL_TYPE(VALUET, "System.ValueType", VALUET_1)
HANDLES(VALUET_1, "InternalEquals", ves_icall_System_ValueType_Equals, MonoBoolean, 3, (MonoObject, MonoObject, MonoArrayOut))
HANDLES(VALUET_2, "InternalGetHashCode", ves_icall_System_ValueType_InternalGetHashCode, gint32, 2, (MonoObject, MonoArrayOut))

ICALL_TYPE(WEBIC, "System.Web.Util.ICalls", WEBIC_1)
HANDLES_REUSE_WRAPPER(WEBIC_1, "GetMachineConfigPath", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path, MonoString, 0, ())
HANDLES(WEBIC_2, "GetMachineInstallDirectory", ves_icall_System_Web_Util_ICalls_get_machine_install_dir, MonoString, 0, ())
HANDLES(WEBIC_3, "GetUnmanagedResourcesPtr", ves_icall_get_resources_ptr, MonoBoolean, 3, (MonoReflectionAssembly, gpointer_ref, gint32_ref))

#ifndef DISABLE_COM
ICALL_TYPE(COMOBJ, "System.__ComObject", COMOBJ_1)
HANDLES(COMOBJ_1, "CreateRCW", ves_icall_System_ComObject_CreateRCW, MonoObject, 1, (MonoReflectionType))
HANDLES(COMOBJ_2, "GetInterfaceInternal", ves_icall_System_ComObject_GetInterfaceInternal, gpointer, 3, (MonoComObject, MonoReflectionType, MonoBoolean))
HANDLES(COMOBJ_3, "ReleaseInterfaces", ves_icall_System_ComObject_ReleaseInterfaces, void, 1, (MonoComObject))
#endif

#endif

// This is similar to HANDLES() but is for icalls passed to register_jit_icall.
// There is no metadata for these. No signature matching.
// Presently their wrappers are less efficient, but hopefully that can be fixed,
// by making a direct call to them, inserting the LMF below them (possibly,
// providing it to them via a function pointer, if it cannot be done in C),
// and of course using managed-style coop handles within them.
// Alternately, ilgen.
//
// This is not just for register_icall, for any time coop wrappers are needed,
// that there is no metadata for. For example embedding API.

// helper for the managed alloc support
MONO_HANDLE_REGISTER_ICALL (ves_icall_string_alloc, MonoString, 1, (int))

// Windows: Allocates with CoTaskMemAlloc.
// Unix: Allocates with g_malloc.
// Either way: Free with mono_marshal_free (Windows:CoTaskMemFree, Unix:g_free).
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf8str, gpointer, 1, (MonoString))

MONO_HANDLE_REGISTER_ICALL (mono_array_to_byte_byvalarray, void, 3, (gpointer, MonoArray, guint32))
MONO_HANDLE_REGISTER_ICALL (mono_array_to_lparray, gpointer, 1, (MonoArray))
MONO_HANDLE_REGISTER_ICALL (mono_array_to_savearray, gpointer, 1, (MonoArray))
MONO_HANDLE_REGISTER_ICALL (mono_byvalarray_to_byte_array, void, 3, (MonoArray, const_char_ptr, guint32))
MONO_HANDLE_REGISTER_ICALL (mono_delegate_to_ftnptr, gpointer, 1, (MonoDelegate))
MONO_HANDLE_REGISTER_ICALL (mono_free_lparray, void, 2, (MonoArray, gpointer_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_ftnptr_to_delegate, MonoDelegate, 2, (MonoClass_ptr, gpointer))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_asany, gpointer, 3, (MonoObject, MonoMarshalNative, int))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_free_asany, void, 4, (MonoObject, gpointer, MonoMarshalNative, int))
MONO_HANDLE_REGISTER_ICALL (mono_marshal_string_to_utf16_copy, gunichar2_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_object_isinst_icall, MonoObject, 2, (MonoObject, MonoClass_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_builder_to_utf16, gunichar2_ptr, 1, (MonoStringBuilder))
MONO_HANDLE_REGISTER_ICALL (mono_string_builder_to_utf8, char_ptr, 1, (MonoStringBuilder))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_bstr_icall, MonoString, 1, (mono_bstr_const))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalstr, MonoString, 2, (const_char_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalwstr, MonoString, 2, (const_gunichar2_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_len_wrapper, MonoString, 2, (const_char_ptr, guint))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_wrapper_internal, MonoString, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_ansibstr, gpointer, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_bstr, mono_bstr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalstr, void, 3, (char_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalwstr, void, 3, (gunichar2_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf16_internal, mono_unichar2_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf32_internal, mono_unichar4_ptr, 1, (MonoString)) // embedding API
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder, void, 2, (MonoStringBuilder, const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder2, MonoStringBuilder, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder, void, 2, (MonoStringBuilder, const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder2, MonoStringBuilder, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_type_from_handle, MonoReflectionType, 1, (MonoType_ptr)) // called by icalls
MONO_HANDLE_REGISTER_ICALL (ves_icall_marshal_alloc, gpointer, 1, (gsize))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_marshal_xdomain_copy_value, MonoObject, 1, (MonoObject))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_from_utf16, MonoString, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_to_utf8, char_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (ves_icall_string_new_wrapper, MonoString, 1, (const_char_ptr))
