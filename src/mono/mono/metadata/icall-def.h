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
 * first_icall_id s the symbol ID of the first internal call of the declared
 * 	type (see below)
 *
 * The list of internal calls of the methods of a type must follow the
 * type declaration. Each internal call is defined by the following macro:
 *
 * 	ICALL(icallid, methodname, cfuncptr)
 *
 * icallid must be a C symbol, unique for each icall defined in this file and
 * tipically equal to the typeid + '_' + a sequential number.
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
 * HANDLES(ICALL(icallid, methodname, cfuncptr))
 *
 * An icall with a HANDLES() declaration wrapped around it will have a generated wrapper
 * that:
 *   (1) Updates the coop handle stack on entry and exit
 *   (2) Call the cfuncptr with a new signature:
 *     (a) All managed object reference in arguments will be wrapped in a handle
 *         (ie, MonoString* becomes MonoStringHandle)
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
 * Limitations: "out" and "ref" arguments are not supported yet. 
 */

#ifndef DISABLE_PROCESS_HANDLING
ICALL_TYPE(NATIVEMETHODS, "Microsoft.Win32.NativeMethods", NATIVEMETHODS_1)
ICALL(NATIVEMETHODS_1, "CloseProcess", ves_icall_Microsoft_Win32_NativeMethods_CloseProcess)
ICALL(NATIVEMETHODS_2, "GetCurrentProcess", ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcess)
ICALL(NATIVEMETHODS_3, "GetCurrentProcessId", ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcessId)
ICALL(NATIVEMETHODS_4, "GetExitCodeProcess", ves_icall_Microsoft_Win32_NativeMethods_GetExitCodeProcess)
ICALL(NATIVEMETHODS_5, "GetPriorityClass", ves_icall_Microsoft_Win32_NativeMethods_GetPriorityClass)
ICALL(NATIVEMETHODS_6, "GetProcessTimes", ves_icall_Microsoft_Win32_NativeMethods_GetProcessTimes)
ICALL(NATIVEMETHODS_7, "GetProcessWorkingSetSize", ves_icall_Microsoft_Win32_NativeMethods_GetProcessWorkingSetSize)
ICALL(NATIVEMETHODS_8, "SetPriorityClass", ves_icall_Microsoft_Win32_NativeMethods_SetPriorityClass)
ICALL(NATIVEMETHODS_9, "SetProcessWorkingSetSize", ves_icall_Microsoft_Win32_NativeMethods_SetProcessWorkingSetSize)
ICALL(NATIVEMETHODS_10, "TerminateProcess", ves_icall_Microsoft_Win32_NativeMethods_TerminateProcess)
ICALL(NATIVEMETHODS_11, "WaitForInputIdle", ves_icall_Microsoft_Win32_NativeMethods_WaitForInputIdle)
#endif /* !DISABLE_PROCESS_HANDLING */

#ifndef DISABLE_COM
ICALL_TYPE(COMPROX, "Mono.Interop.ComInteropProxy", COMPROX_1)
ICALL(COMPROX_1, "AddProxy", ves_icall_Mono_Interop_ComInteropProxy_AddProxy)
ICALL(COMPROX_2, "FindProxy", ves_icall_Mono_Interop_ComInteropProxy_FindProxy)
#endif

ICALL_TYPE(TLS_PROVIDER_FACTORY, "Mono.Net.Security.MonoTlsProviderFactory", TLS_PROVIDER_FACTORY_1)
ICALL(TLS_PROVIDER_FACTORY_1, "IsBtlsSupported", ves_icall_Mono_TlsProviderFactory_IsBtlsSupported)

ICALL_TYPE(RUNTIME, "Mono.Runtime", RUNTIME_1)
HANDLES(ICALL(RUNTIME_1, "GetDisplayName", ves_icall_Mono_Runtime_GetDisplayName))
HANDLES(ICALL(RUNTIME_12, "GetNativeStackTrace", ves_icall_Mono_Runtime_GetNativeStackTrace))

ICALL_TYPE(RTCLASS, "Mono.RuntimeClassHandle", RTCLASS_1)
ICALL(RTCLASS_1, "GetTypeFromClass", ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass)

ICALL_TYPE(RTPTRARRAY, "Mono.RuntimeGPtrArrayHandle", RTPTRARRAY_1)
ICALL(RTPTRARRAY_1, "GPtrArrayFree", ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree)

ICALL_TYPE(RTMARSHAL, "Mono.RuntimeMarshal", RTMARSHAL_1)
ICALL(RTMARSHAL_1, "FreeAssemblyName", ves_icall_Mono_RuntimeMarshal_FreeAssemblyName)

ICALL_TYPE(SAFESTRMARSHAL, "Mono.SafeStringMarshal", SAFESTRMARSHAL_1)
ICALL(SAFESTRMARSHAL_1, "GFree", ves_icall_Mono_SafeStringMarshal_GFree)
ICALL(SAFESTRMARSHAL_2, "StringToUtf8", ves_icall_Mono_SafeStringMarshal_StringToUtf8)

#ifndef PLATFORM_RO_FS
ICALL_TYPE(KPAIR, "Mono.Security.Cryptography.KeyPairPersistence", KPAIR_1)
ICALL(KPAIR_1, "_CanSecure", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure)
ICALL(KPAIR_2, "_IsMachineProtected", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected)
ICALL(KPAIR_3, "_IsUserProtected", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected)
ICALL(KPAIR_4, "_ProtectMachine", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine)
ICALL(KPAIR_5, "_ProtectUser", ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser)
#endif /* !PLATFORM_RO_FS */

ICALL_TYPE(APPDOM, "System.AppDomain", APPDOM_23)
HANDLES(ICALL(APPDOM_23, "DoUnhandledException", ves_icall_System_AppDomain_DoUnhandledException))
HANDLES(ICALL(APPDOM_1, "ExecuteAssembly", ves_icall_System_AppDomain_ExecuteAssembly))
HANDLES(ICALL(APPDOM_2, "GetAssemblies", ves_icall_System_AppDomain_GetAssemblies))
HANDLES(ICALL(APPDOM_3, "GetData", ves_icall_System_AppDomain_GetData))
HANDLES(ICALL(APPDOM_4, "InternalGetContext", ves_icall_System_AppDomain_InternalGetContext))
HANDLES(ICALL(APPDOM_5, "InternalGetDefaultContext", ves_icall_System_AppDomain_InternalGetDefaultContext))
HANDLES(ICALL(APPDOM_6, "InternalGetProcessGuid", ves_icall_System_AppDomain_InternalGetProcessGuid))
HANDLES(ICALL(APPDOM_7, "InternalIsFinalizingForUnload", ves_icall_System_AppDomain_InternalIsFinalizingForUnload))
HANDLES(ICALL(APPDOM_8, "InternalPopDomainRef", ves_icall_System_AppDomain_InternalPopDomainRef))
HANDLES(ICALL(APPDOM_9, "InternalPushDomainRef", ves_icall_System_AppDomain_InternalPushDomainRef))
HANDLES(ICALL(APPDOM_10, "InternalPushDomainRefByID", ves_icall_System_AppDomain_InternalPushDomainRefByID))
HANDLES(ICALL(APPDOM_11, "InternalSetContext", ves_icall_System_AppDomain_InternalSetContext))
HANDLES(ICALL(APPDOM_12, "InternalSetDomain", ves_icall_System_AppDomain_InternalSetDomain))
HANDLES(ICALL(APPDOM_13, "InternalSetDomainByID", ves_icall_System_AppDomain_InternalSetDomainByID))
HANDLES(ICALL(APPDOM_14, "InternalUnload", ves_icall_System_AppDomain_InternalUnload))
HANDLES(ICALL(APPDOM_15, "LoadAssembly", ves_icall_System_AppDomain_LoadAssembly))
HANDLES(ICALL(APPDOM_16, "LoadAssemblyRaw", ves_icall_System_AppDomain_LoadAssemblyRaw))
HANDLES(ICALL(APPDOM_17, "SetData", ves_icall_System_AppDomain_SetData))
HANDLES(ICALL(APPDOM_18, "createDomain", ves_icall_System_AppDomain_createDomain))
HANDLES(ICALL(APPDOM_19, "getCurDomain", ves_icall_System_AppDomain_getCurDomain))
HANDLES(ICALL(APPDOM_20, "getFriendlyName", ves_icall_System_AppDomain_getFriendlyName))
HANDLES(ICALL(APPDOM_21, "getRootDomain", ves_icall_System_AppDomain_getRootDomain))
HANDLES(ICALL(APPDOM_22, "getSetup", ves_icall_System_AppDomain_getSetup))

ICALL_TYPE(ARGI, "System.ArgIterator", ARGI_1)
ICALL(ARGI_1, "IntGetNextArg()",                  mono_ArgIterator_IntGetNextArg)
ICALL(ARGI_2, "IntGetNextArg(intptr)", mono_ArgIterator_IntGetNextArgT)
ICALL(ARGI_3, "IntGetNextArgType",                mono_ArgIterator_IntGetNextArgType)
ICALL(ARGI_4, "Setup",                            mono_ArgIterator_Setup)

ICALL_TYPE(ARRAY, "System.Array", ARRAY_1)
ICALL(ARRAY_1, "ClearInternal",    ves_icall_System_Array_ClearInternal)
ICALL(ARRAY_3, "CreateInstanceImpl",   ves_icall_System_Array_CreateInstanceImpl)
ICALL(ARRAY_14, "CreateInstanceImpl64",   ves_icall_System_Array_CreateInstanceImpl64)
ICALL(ARRAY_4, "FastCopy",         ves_icall_System_Array_FastCopy)
ICALL(ARRAY_5, "GetGenericValueImpl", ves_icall_System_Array_GetGenericValueImpl)
ICALL(ARRAY_6, "GetLength",        ves_icall_System_Array_GetLength)
ICALL(ARRAY_15, "GetLongLength",        ves_icall_System_Array_GetLongLength)
ICALL(ARRAY_7, "GetLowerBound",    ves_icall_System_Array_GetLowerBound)
ICALL(ARRAY_8, "GetRank",          ves_icall_System_Array_GetRank)
ICALL(ARRAY_9, "GetValue",         ves_icall_System_Array_GetValue)
ICALL(ARRAY_10, "GetValueImpl",     ves_icall_System_Array_GetValueImpl)
ICALL(ARRAY_11, "SetGenericValueImpl", ves_icall_System_Array_SetGenericValueImpl)
ICALL(ARRAY_12, "SetValue",         ves_icall_System_Array_SetValue)
HANDLES(ICALL(ARRAY_13, "SetValueImpl",     ves_icall_System_Array_SetValueImpl))

ICALL_TYPE(BUFFER, "System.Buffer", BUFFER_1)
ICALL(BUFFER_1, "InternalBlockCopy", ves_icall_System_Buffer_BlockCopyInternal)
ICALL(BUFFER_2, "_ByteLength", ves_icall_System_Buffer_ByteLengthInternal)
ICALL(BUFFER_3, "_GetByte", ves_icall_System_Buffer_GetByteInternal)
ICALL(BUFFER_4, "_SetByte", ves_icall_System_Buffer_SetByteInternal)

ICALL_TYPE(CLRCONFIG, "System.CLRConfig", CLRCONFIG_1)
ICALL(CLRCONFIG_1, "CheckThrowUnobservedTaskExceptions", ves_icall_System_CLRConfig_CheckThrowUnobservedTaskExceptions)

ICALL_TYPE(DEFAULTC, "System.Configuration.DefaultConfig", DEFAULTC_1)
HANDLES(ICALL(DEFAULTC_1, "get_bundled_machine_config", ves_icall_System_Configuration_DefaultConfig_get_bundled_machine_config))
HANDLES(ICALL(DEFAULTC_2, "get_machine_config_path", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path))

/* Note that the below icall shares the same function as DefaultConfig uses */
ICALL_TYPE(INTCFGHOST, "System.Configuration.InternalConfigurationHost", INTCFGHOST_1)
HANDLES(ICALL(INTCFGHOST_1, "get_bundled_app_config", ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_app_config))
HANDLES(ICALL(INTCFGHOST_2, "get_bundled_machine_config", ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_machine_config))

ICALL_TYPE(CONSOLE, "System.ConsoleDriver", CONSOLE_1)
ICALL(CONSOLE_1, "InternalKeyAvailable", ves_icall_System_ConsoleDriver_InternalKeyAvailable )
ICALL(CONSOLE_2, "Isatty", ves_icall_System_ConsoleDriver_Isatty )
ICALL(CONSOLE_3, "SetBreak", ves_icall_System_ConsoleDriver_SetBreak )
ICALL(CONSOLE_4, "SetEcho", ves_icall_System_ConsoleDriver_SetEcho )
ICALL(CONSOLE_5, "TtySetup", ves_icall_System_ConsoleDriver_TtySetup )

ICALL_TYPE(DTIME, "System.DateTime", DTIME_1)
ICALL(DTIME_1, "GetSystemTimeAsFileTime", mono_100ns_datetime)

#ifndef DISABLE_DECIMAL
ICALL_TYPE(DECIMAL, "System.Decimal", DECIMAL_1)
ICALL(DECIMAL_1, ".ctor(double)", mono_decimal_init_double)
ICALL(DECIMAL_2, ".ctor(single)", mono_decimal_init_single)
ICALL(DECIMAL_3, "FCallAddSub(System.Decimal&,System.Decimal&,byte)", mono_decimal_addsub)
ICALL(DECIMAL_4, "FCallCompare", mono_decimal_compare)
ICALL(DECIMAL_5, "FCallDivide", mono_decimal_divide)
ICALL(DECIMAL_6, "FCallFloor", mono_decimal_floor)
ICALL(DECIMAL_7, "FCallMultiply", mono_decimal_multiply)
ICALL(DECIMAL_8, "FCallRound", mono_decimal_round)
ICALL(DECIMAL_9, "FCallToInt32", mono_decimal_to_int32)
ICALL(DECIMAL_10, "FCallTruncate", mono_decimal_truncate)
ICALL(DECIMAL_11, "GetHashCode", mono_decimal_get_hash_code)
ICALL(DECIMAL_12, "ToDouble", mono_decimal_to_double)
ICALL(DECIMAL_13, "ToSingle", mono_decimal_to_float)
#endif

ICALL_TYPE(DELEGATE, "System.Delegate", DELEGATE_1)
HANDLES(ICALL(DELEGATE_1, "AllocDelegateLike_internal", ves_icall_System_Delegate_AllocDelegateLike_internal))
HANDLES(ICALL(DELEGATE_2, "CreateDelegate_internal", ves_icall_System_Delegate_CreateDelegate_internal))
HANDLES(ICALL(DELEGATE_3, "GetVirtualMethod_internal", ves_icall_System_Delegate_GetVirtualMethod_internal))

ICALL_TYPE(DEBUGR, "System.Diagnostics.Debugger", DEBUGR_1)
ICALL(DEBUGR_1, "IsAttached_internal", ves_icall_System_Diagnostics_Debugger_IsAttached_internal)
ICALL(DEBUGR_2, "IsLogging", ves_icall_System_Diagnostics_Debugger_IsLogging)
ICALL(DEBUGR_3, "Log", ves_icall_System_Diagnostics_Debugger_Log)

ICALL_TYPE(TRACEL, "System.Diagnostics.DefaultTraceListener", TRACEL_1)
ICALL(TRACEL_1, "WriteWindowsDebugString", ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString)

ICALL_TYPE(FILEV, "System.Diagnostics.FileVersionInfo", FILEV_1)
ICALL(FILEV_1, "GetVersionInfo_internal(string)", ves_icall_System_Diagnostics_FileVersionInfo_GetVersionInfo_internal)

#ifndef DISABLE_PROCESS_HANDLING
ICALL_TYPE(PERFCTR, "System.Diagnostics.PerformanceCounter", PERFCTR_1)
ICALL(PERFCTR_1, "FreeData", mono_perfcounter_free_data)
ICALL(PERFCTR_2, "GetImpl", mono_perfcounter_get_impl)
ICALL(PERFCTR_3, "GetSample", mono_perfcounter_get_sample)
ICALL(PERFCTR_4, "UpdateValue", mono_perfcounter_update_value)

ICALL_TYPE(PERFCTRCAT, "System.Diagnostics.PerformanceCounterCategory", PERFCTRCAT_1)
ICALL(PERFCTRCAT_1, "CategoryDelete", mono_perfcounter_category_del)
ICALL(PERFCTRCAT_2, "CategoryHelpInternal",   mono_perfcounter_category_help)
ICALL(PERFCTRCAT_3, "CounterCategoryExists", mono_perfcounter_category_exists)
ICALL(PERFCTRCAT_4, "Create",         mono_perfcounter_create)
ICALL(PERFCTRCAT_5, "GetCategoryNames", mono_perfcounter_category_names)
ICALL(PERFCTRCAT_6, "GetCounterNames", mono_perfcounter_counter_names)
ICALL(PERFCTRCAT_7, "GetInstanceNames", mono_perfcounter_instance_names)
ICALL(PERFCTRCAT_8, "InstanceExistsInternal", mono_perfcounter_instance_exists)

ICALL_TYPE(PROCESS, "System.Diagnostics.Process", PROCESS_1)
ICALL(PROCESS_1, "CreateProcess_internal", ves_icall_System_Diagnostics_Process_CreateProcess_internal)
ICALL(PROCESS_4, "GetModules_internal(intptr)", ves_icall_System_Diagnostics_Process_GetModules_internal)
ICALL(PROCESS_5H, "GetProcessData", ves_icall_System_Diagnostics_Process_GetProcessData)
ICALL(PROCESS_6, "GetProcess_internal(int)", ves_icall_System_Diagnostics_Process_GetProcess_internal)
ICALL(PROCESS_7, "GetProcesses_internal()", ves_icall_System_Diagnostics_Process_GetProcesses_internal)
ICALL(PROCESS_10, "ProcessName_internal(intptr)", ves_icall_System_Diagnostics_Process_ProcessName_internal)
ICALL(PROCESS_13, "ShellExecuteEx_internal(System.Diagnostics.ProcessStartInfo,System.Diagnostics.Process/ProcInfo&)", ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal)
#endif /* !DISABLE_PROCESS_HANDLING */

ICALL_TYPE(STOPWATCH, "System.Diagnostics.Stopwatch", STOPWATCH_1)
ICALL(STOPWATCH_1, "GetTimestamp", mono_100ns_ticks)

ICALL_TYPE(ENUM, "System.Enum", ENUM_1)
HANDLES(ICALL(ENUM_1, "GetEnumValuesAndNames", ves_icall_System_Enum_GetEnumValuesAndNames))
ICALL(ENUM_2, "InternalBoxEnum", ves_icall_System_Enum_ToObject)
ICALL(ENUM_3, "InternalCompareTo", ves_icall_System_Enum_compare_value_to)
ICALL(ENUM_4, "InternalGetUnderlyingType", ves_icall_System_Enum_get_underlying_type)
ICALL(ENUM_5, "InternalHasFlag", ves_icall_System_Enum_InternalHasFlag)
ICALL(ENUM_6, "get_hashcode", ves_icall_System_Enum_get_hashcode)
ICALL(ENUM_7, "get_value", ves_icall_System_Enum_get_value)

ICALL_TYPE(ENV, "System.Environment", ENV_1)
ICALL(ENV_1, "Exit", ves_icall_System_Environment_Exit)
ICALL(ENV_2, "GetCommandLineArgs", ves_icall_System_Environment_GetCommandLineArgs)
ICALL(ENV_3, "GetEnvironmentVariableNames", ves_icall_System_Environment_GetEnvironmentVariableNames)
ICALL(ENV_31, "GetIs64BitOperatingSystem", ves_icall_System_Environment_GetIs64BitOperatingSystem)
ICALL(ENV_4, "GetLogicalDrivesInternal", ves_icall_System_Environment_GetLogicalDrives )
HANDLES(ICALL(ENV_5, "GetMachineConfigPath", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path))
HANDLES(ICALL(ENV_51, "GetNewLine", ves_icall_System_Environment_get_NewLine))
HANDLES(ICALL(ENV_6, "GetOSVersionString", ves_icall_System_Environment_GetOSVersionString))
ICALL(ENV_6a, "GetPageSize", mono_pagesize)
HANDLES(ICALL(ENV_7, "GetWindowsFolderPath", ves_icall_System_Environment_GetWindowsFolderPath))
ICALL(ENV_8, "InternalSetEnvironmentVariable", ves_icall_System_Environment_InternalSetEnvironmentVariable)
ICALL(ENV_9, "get_ExitCode", mono_environment_exitcode_get)
ICALL(ENV_10, "get_HasShutdownStarted", ves_icall_System_Environment_get_HasShutdownStarted)
HANDLES(ICALL(ENV_11, "get_MachineName", ves_icall_System_Environment_get_MachineName))
ICALL(ENV_13, "get_Platform", ves_icall_System_Environment_get_Platform)
ICALL(ENV_14, "get_ProcessorCount", mono_cpu_count)
ICALL(ENV_15, "get_TickCount", ves_icall_System_Environment_get_TickCount)
HANDLES(ICALL(ENV_16, "get_UserName", ves_icall_System_Environment_get_UserName))
HANDLES(ICALL(ENV_16b, "get_bundled_machine_config", ves_icall_System_Environment_get_bundled_machine_config))
HANDLES(ICALL(ENV_16m, "internalBroadcastSettingChange", ves_icall_System_Environment_BroadcastSettingChange))
HANDLES(ICALL(ENV_17, "internalGetEnvironmentVariable_native", ves_icall_System_Environment_GetEnvironmentVariable_native))
HANDLES(ICALL(ENV_18, "internalGetGacPath", ves_icall_System_Environment_GetGacPath))
HANDLES(ICALL(ENV_19, "internalGetHome", ves_icall_System_Environment_InternalGetHome))
ICALL(ENV_20, "set_ExitCode", mono_environment_exitcode_set)

ICALL_TYPE(GC, "System.GC", GC_0)
ICALL(GC_0, "GetCollectionCount", mono_gc_collection_count)
ICALL(GC_0a, "GetGeneration", mono_gc_get_generation)
ICALL(GC_0b, "GetMaxGeneration", mono_gc_max_generation)
ICALL(GC_1, "GetTotalMemory", ves_icall_System_GC_GetTotalMemory)
ICALL(GC_2, "InternalCollect", ves_icall_System_GC_InternalCollect)
ICALL(GC_3, "KeepAlive", ves_icall_System_GC_KeepAlive)
ICALL(GC_4a, "RecordPressure", mono_gc_add_memory_pressure)
ICALL(GC_6, "WaitForPendingFinalizers", ves_icall_System_GC_WaitForPendingFinalizers)
ICALL(GC_6b, "_ReRegisterForFinalize", ves_icall_System_GC_ReRegisterForFinalize)
ICALL(GC_7, "_SuppressFinalize", ves_icall_System_GC_SuppressFinalize)
ICALL(GC_9, "get_ephemeron_tombstone", ves_icall_System_GC_get_ephemeron_tombstone)
ICALL(GC_8, "register_ephemeron_array", ves_icall_System_GC_register_ephemeron_array)

ICALL_TYPE(CALDATA, "System.Globalization.CalendarData", CALDATA_1)
ICALL(CALDATA_1, "fill_calendar_data", ves_icall_System_Globalization_CalendarData_fill_calendar_data)

ICALL_TYPE(COMPINF, "System.Globalization.CompareInfo", COMPINF_1)
ICALL(COMPINF_1, "assign_sortkey(object,string,System.Globalization.CompareOptions)", ves_icall_System_Globalization_CompareInfo_assign_sortkey)
ICALL(COMPINF_4, "internal_compare(string,int,int,string,int,int,System.Globalization.CompareOptions)", ves_icall_System_Globalization_CompareInfo_internal_compare)
ICALL(COMPINF_5, "internal_index(string,int,int,char,System.Globalization.CompareOptions,bool)", ves_icall_System_Globalization_CompareInfo_internal_index_char)
ICALL(COMPINF_6, "internal_index(string,int,int,string,System.Globalization.CompareOptions,bool)", ves_icall_System_Globalization_CompareInfo_internal_index)

ICALL_TYPE(CULDATA, "System.Globalization.CultureData", CULDATA_1)
ICALL(CULDATA_1, "fill_culture_data", ves_icall_System_Globalization_CultureData_fill_culture_data)
ICALL(CULDATA_2, "fill_number_data", ves_icall_System_Globalization_CultureData_fill_number_data)

ICALL_TYPE(CULINF, "System.Globalization.CultureInfo", CULINF_5)
ICALL(CULINF_5, "construct_internal_locale_from_lcid", ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_lcid)
ICALL(CULINF_6, "construct_internal_locale_from_name", ves_icall_System_Globalization_CultureInfo_construct_internal_locale_from_name)
HANDLES(ICALL(CULINF_7, "get_current_locale_name", ves_icall_System_Globalization_CultureInfo_get_current_locale_name))
ICALL(CULINF_9, "internal_get_cultures", ves_icall_System_Globalization_CultureInfo_internal_get_cultures)
//ICALL(CULINF_10, "internal_is_lcid_neutral", ves_icall_System_Globalization_CultureInfo_internal_is_lcid_neutral)

ICALL_TYPE(REGINF, "System.Globalization.RegionInfo", REGINF_1)
ICALL(REGINF_1, "construct_internal_region_from_lcid", ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_lcid)
ICALL(REGINF_2, "construct_internal_region_from_name", ves_icall_System_Globalization_RegionInfo_construct_internal_region_from_name)

#ifndef PLATFORM_NO_DRIVEINFO
ICALL_TYPE(IODRIVEINFO, "System.IO.DriveInfo", IODRIVEINFO_1)
ICALL(IODRIVEINFO_1, "GetDiskFreeSpaceInternal", ves_icall_System_IO_DriveInfo_GetDiskFreeSpace)
ICALL(IODRIVEINFO_2, "GetDriveFormat", ves_icall_System_IO_DriveInfo_GetDriveFormat)
ICALL(IODRIVEINFO_3, "GetDriveTypeInternal", ves_icall_System_IO_DriveInfo_GetDriveType)
#endif

ICALL_TYPE(FAMW, "System.IO.FAMWatcher", FAMW_1)
ICALL(FAMW_1, "InternalFAMNextEvent", ves_icall_System_IO_FAMW_InternalFAMNextEvent)

ICALL_TYPE(FILEW, "System.IO.FileSystemWatcher", FILEW_4)
ICALL(FILEW_4, "InternalSupportsFSW", ves_icall_System_IO_FSW_SupportsFSW)

ICALL_TYPE(INOW, "System.IO.InotifyWatcher", INOW_1)
ICALL(INOW_1, "AddWatch", ves_icall_System_IO_InotifyWatcher_AddWatch)
ICALL(INOW_2, "GetInotifyInstance", ves_icall_System_IO_InotifyWatcher_GetInotifyInstance)
ICALL(INOW_3, "RemoveWatch", ves_icall_System_IO_InotifyWatcher_RemoveWatch)

ICALL_TYPE(KQUEM, "System.IO.KqueueMonitor", KQUEM_1)
ICALL(KQUEM_1, "kevent_notimeout", ves_icall_System_IO_KqueueMonitor_kevent_notimeout)

ICALL_TYPE(MMAPIMPL, "System.IO.MemoryMappedFiles.MemoryMapImpl", MMAPIMPL_1)
ICALL(MMAPIMPL_1, "CloseMapping", mono_mmap_close)
ICALL(MMAPIMPL_2, "ConfigureHandleInheritability", mono_mmap_configure_inheritability)
ICALL(MMAPIMPL_3, "Flush", mono_mmap_flush)
ICALL(MMAPIMPL_4, "MapInternal", mono_mmap_map)
ICALL(MMAPIMPL_5, "OpenFileInternal", mono_mmap_open_file)
ICALL(MMAPIMPL_6, "OpenHandleInternal", mono_mmap_open_handle)
ICALL(MMAPIMPL_7, "Unmap", mono_mmap_unmap)

ICALL_TYPE(MONOIO, "System.IO.MonoIO", MONOIO_1)
ICALL(MONOIO_1, "Close(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Close)
#ifndef PLATFORM_RO_FS
ICALL(MONOIO_2, "CopyFile(string,string,bool,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CopyFile)
ICALL(MONOIO_3, "CreateDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_CreateDirectory)
ICALL(MONOIO_4, "CreatePipe", ves_icall_System_IO_MonoIO_CreatePipe)
ICALL(MONOIO_5, "DeleteFile(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_DeleteFile)
#endif /* !PLATFORM_RO_FS */
ICALL(MONOIO_38, "DumpHandles", ves_icall_System_IO_MonoIO_DumpHandles)
ICALL(MONOIO_34, "DuplicateHandle", ves_icall_System_IO_MonoIO_DuplicateHandle)
ICALL(MONOIO_37, "FindClose", ves_icall_System_IO_MonoIO_FindClose)
ICALL(MONOIO_37a, "FindCloseFile", ves_icall_System_IO_MonoIO_FindCloseFile)
ICALL(MONOIO_35, "FindFirst", ves_icall_System_IO_MonoIO_FindFirst)
ICALL(MONOIO_35a, "FindFirstFile", ves_icall_System_IO_MonoIO_FindFirstFile)
ICALL(MONOIO_36, "FindNext", ves_icall_System_IO_MonoIO_FindNext)
ICALL(MONOIO_36a, "FindNextFile", ves_icall_System_IO_MonoIO_FindNextFile)
ICALL(MONOIO_6, "Flush(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Flush)
ICALL(MONOIO_7, "GetCurrentDirectory(System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetCurrentDirectory)
ICALL(MONOIO_8, "GetFileAttributes(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileAttributes)
ICALL(MONOIO_9, "GetFileStat(string,System.IO.MonoIOStat&,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileStat)
ICALL(MONOIO_10, "GetFileSystemEntries", ves_icall_System_IO_MonoIO_GetFileSystemEntries)
ICALL(MONOIO_11, "GetFileType(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetFileType)
ICALL(MONOIO_12, "GetLength(intptr,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_GetLength)
#ifndef PLATFORM_RO_FS
ICALL(MONOIO_14, "Lock(intptr,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Lock)
ICALL(MONOIO_15, "MoveFile(string,string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_MoveFile)
#endif /* !PLATFORM_RO_FS */
ICALL(MONOIO_16, "Open(string,System.IO.FileMode,System.IO.FileAccess,System.IO.FileShare,System.IO.FileOptions,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Open)
ICALL(MONOIO_17, "Read(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Read)
#ifndef PLATFORM_RO_FS
ICALL(MONOIO_18, "RemoveDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_RemoveDirectory)
ICALL(MONOIO_18M, "ReplaceFile(string,string,string,bool,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_ReplaceFile)
#endif /* !PLATFORM_RO_FS */
ICALL(MONOIO_19, "Seek(intptr,long,System.IO.SeekOrigin,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Seek)
ICALL(MONOIO_20, "SetCurrentDirectory(string,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetCurrentDirectory)
ICALL(MONOIO_21, "SetFileAttributes(string,System.IO.FileAttributes,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileAttributes)
ICALL(MONOIO_22, "SetFileTime(intptr,long,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetFileTime)
ICALL(MONOIO_23, "SetLength(intptr,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_SetLength)
#ifndef PLATFORM_RO_FS
ICALL(MONOIO_24, "Unlock(intptr,long,long,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Unlock)
#endif
ICALL(MONOIO_25, "Write(intptr,byte[],int,int,System.IO.MonoIOError&)", ves_icall_System_IO_MonoIO_Write)
ICALL(MONOIO_26, "get_AltDirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_AltDirectorySeparatorChar)
ICALL(MONOIO_27, "get_ConsoleError", ves_icall_System_IO_MonoIO_get_ConsoleError)
ICALL(MONOIO_28, "get_ConsoleInput", ves_icall_System_IO_MonoIO_get_ConsoleInput)
ICALL(MONOIO_29, "get_ConsoleOutput", ves_icall_System_IO_MonoIO_get_ConsoleOutput)
ICALL(MONOIO_30, "get_DirectorySeparatorChar", ves_icall_System_IO_MonoIO_get_DirectorySeparatorChar)
ICALL(MONOIO_31, "get_InvalidPathChars", ves_icall_System_IO_MonoIO_get_InvalidPathChars)
ICALL(MONOIO_32, "get_PathSeparator", ves_icall_System_IO_MonoIO_get_PathSeparator)
ICALL(MONOIO_33, "get_VolumeSeparatorChar", ves_icall_System_IO_MonoIO_get_VolumeSeparatorChar)

ICALL_TYPE(IOPATH, "System.IO.Path", IOPATH_1)
HANDLES(ICALL(IOPATH_1, "get_temp_path", ves_icall_System_IO_get_temp_path))

ICALL_TYPE(IOSELECTOR, "System.IOSelector", IOSELECTOR_1)
ICALL(IOSELECTOR_1, "Add", ves_icall_System_IOSelector_Add)
ICALL(IOSELECTOR_2, "Remove", ves_icall_System_IOSelector_Remove)

ICALL_TYPE(MATH, "System.Math", MATH_19)
ICALL(MATH_19, "Abs(double)", ves_icall_System_Math_Abs_double)
ICALL(MATH_20, "Abs(single)", ves_icall_System_Math_Abs_single)
ICALL(MATH_1, "Acos", ves_icall_System_Math_Acos)
ICALL(MATH_2, "Asin", ves_icall_System_Math_Asin)
ICALL(MATH_3, "Atan", ves_icall_System_Math_Atan)
ICALL(MATH_4, "Atan2", ves_icall_System_Math_Atan2)
ICALL(MATH_21, "Ceiling", ves_icall_System_Math_Ceiling)
ICALL(MATH_5, "Cos", ves_icall_System_Math_Cos)
ICALL(MATH_6, "Cosh", ves_icall_System_Math_Cosh)
ICALL(MATH_7, "Exp", ves_icall_System_Math_Exp)
ICALL(MATH_8, "Floor", ves_icall_System_Math_Floor)
ICALL(MATH_9, "Log", ves_icall_System_Math_Log)
ICALL(MATH_10, "Log10", ves_icall_System_Math_Log10)
ICALL(MATH_11, "Pow", ves_icall_System_Math_Pow)
ICALL(MATH_12, "Round", ves_icall_System_Math_Round)
ICALL(MATH_14, "Sin", ves_icall_System_Math_Sin)
ICALL(MATH_15, "Sinh", ves_icall_System_Math_Sinh)
ICALL(MATH_22, "SplitFractionDouble", ves_icall_System_Math_SplitFractionDouble)
ICALL(MATH_16, "Sqrt", ves_icall_System_Math_Sqrt)
ICALL(MATH_17, "Tan", ves_icall_System_Math_Tan)
ICALL(MATH_18, "Tanh", ves_icall_System_Math_Tanh)

ICALL_TYPE(MCATTR, "System.MonoCustomAttrs", MCATTR_1)
HANDLES(ICALL(MCATTR_1, "GetCustomAttributesDataInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal))
HANDLES(ICALL(MCATTR_2, "GetCustomAttributesInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesInternal))
HANDLES(ICALL(MCATTR_3, "IsDefinedInternal", ves_icall_MonoCustomAttrs_IsDefinedInternal))

#ifndef DISABLE_SOCKETS
ICALL_TYPE(NDNS, "System.Net.Dns", NDNS_1)
HANDLES(ICALL(NDNS_1, "GetHostByAddr_internal(string,string&,string[]&,string[]&,int)", ves_icall_System_Net_Dns_GetHostByAddr_internal))
HANDLES(ICALL(NDNS_2, "GetHostByName_internal(string,string&,string[]&,string[]&,int)", ves_icall_System_Net_Dns_GetHostByName_internal))
HANDLES(ICALL(NDNS_3, "GetHostName_internal(string&)", ves_icall_System_Net_Dns_GetHostName_internal))

#if defined(PLATFORM_MACOSX) || defined(PLATFORM_BSD)
ICALL_TYPE(MAC_IFACE_PROPS, "System.Net.NetworkInformation.MacOsIPInterfaceProperties", MAC_IFACE_PROPS_1)
ICALL(MAC_IFACE_PROPS_1, "ParseRouteInfo_internal", ves_icall_System_Net_NetworkInformation_MacOsIPInterfaceProperties_ParseRouteInfo_internal)
#endif

ICALL_TYPE(SOCK, "System.Net.Sockets.Socket", SOCK_1)
HANDLES(ICALL(SOCK_1, "Accept_internal(intptr,int&,bool)", ves_icall_System_Net_Sockets_Socket_Accept_internal))
HANDLES(ICALL(SOCK_2, "Available_internal(intptr,int&)", ves_icall_System_Net_Sockets_Socket_Available_internal))
HANDLES(ICALL(SOCK_3, "Bind_internal(intptr,System.Net.SocketAddress,int&)", ves_icall_System_Net_Sockets_Socket_Bind_internal))
HANDLES(ICALL(SOCK_4, "Blocking_internal(intptr,bool,int&)", ves_icall_System_Net_Sockets_Socket_Blocking_internal))
HANDLES(ICALL(SOCK_5, "Close_internal(intptr,int&)", ves_icall_System_Net_Sockets_Socket_Close_internal))
HANDLES(ICALL(SOCK_6, "Connect_internal(intptr,System.Net.SocketAddress,int&,bool)", ves_icall_System_Net_Sockets_Socket_Connect_internal))
HANDLES(ICALL (SOCK_6a, "Disconnect_internal(intptr,bool,int&)", ves_icall_System_Net_Sockets_Socket_Disconnect_internal))
HANDLES(ICALL(SOCK_7, "GetSocketOption_arr_internal(intptr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,byte[]&,int&)", ves_icall_System_Net_Sockets_Socket_GetSocketOption_arr_internal))
HANDLES(ICALL(SOCK_8, "GetSocketOption_obj_internal(intptr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,object&,int&)", ves_icall_System_Net_Sockets_Socket_GetSocketOption_obj_internal))
HANDLES(ICALL(SOCK_21, "IOControl_internal(intptr,int,byte[],byte[],int&)", ves_icall_System_Net_Sockets_Socket_IOControl_internal))
HANDLES(ICALL(SOCK_9, "Listen_internal(intptr,int,int&)", ves_icall_System_Net_Sockets_Socket_Listen_internal))
HANDLES(ICALL(SOCK_10, "LocalEndPoint_internal(intptr,int,int&)", ves_icall_System_Net_Sockets_Socket_LocalEndPoint_internal))
HANDLES(ICALL(SOCK_11, "Poll_internal", ves_icall_System_Net_Sockets_Socket_Poll_internal))
HANDLES(ICALL(SOCK_13, "ReceiveFrom_internal(intptr,byte[],int,int,System.Net.Sockets.SocketFlags,System.Net.SocketAddress&,int&,bool)", ves_icall_System_Net_Sockets_Socket_ReceiveFrom_internal))
HANDLES(ICALL(SOCK_11a, "Receive_internal(intptr,System.Net.Sockets.Socket/WSABUF[],System.Net.Sockets.SocketFlags,int&,bool)", ves_icall_System_Net_Sockets_Socket_Receive_array_internal))
HANDLES(ICALL(SOCK_12, "Receive_internal(intptr,byte[],int,int,System.Net.Sockets.SocketFlags,int&,bool)", ves_icall_System_Net_Sockets_Socket_Receive_internal))
HANDLES(ICALL(SOCK_14, "RemoteEndPoint_internal(intptr,int,int&)", ves_icall_System_Net_Sockets_Socket_RemoteEndPoint_internal))
HANDLES(ICALL(SOCK_15, "Select_internal(System.Net.Sockets.Socket[]&,int,int&)", ves_icall_System_Net_Sockets_Socket_Select_internal))
HANDLES(ICALL(SOCK_15a, "SendFile_internal(intptr,string,byte[],byte[],System.Net.Sockets.TransmitFileOptions,int&,bool)", ves_icall_System_Net_Sockets_Socket_SendFile_internal))
HANDLES(ICALL(SOCK_16, "SendTo_internal(intptr,byte[],int,int,System.Net.Sockets.SocketFlags,System.Net.SocketAddress,int&,bool)", ves_icall_System_Net_Sockets_Socket_SendTo_internal))
HANDLES(ICALL(SOCK_16a, "Send_internal(intptr,System.Net.Sockets.Socket/WSABUF[],System.Net.Sockets.SocketFlags,int&,bool)", ves_icall_System_Net_Sockets_Socket_Send_array_internal))
HANDLES(ICALL(SOCK_17, "Send_internal(intptr,byte[],int,int,System.Net.Sockets.SocketFlags,int&,bool)", ves_icall_System_Net_Sockets_Socket_Send_internal))
HANDLES(ICALL(SOCK_18, "SetSocketOption_internal(intptr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,object,byte[],int,int&)", ves_icall_System_Net_Sockets_Socket_SetSocketOption_internal))
HANDLES(ICALL(SOCK_19, "Shutdown_internal(intptr,System.Net.Sockets.SocketShutdown,int&)", ves_icall_System_Net_Sockets_Socket_Shutdown_internal))
HANDLES(ICALL(SOCK_20, "Socket_internal(System.Net.Sockets.AddressFamily,System.Net.Sockets.SocketType,System.Net.Sockets.ProtocolType,int&)", ves_icall_System_Net_Sockets_Socket_Socket_internal))
HANDLES(ICALL(SOCK_20a, "SupportsPortReuse", ves_icall_System_Net_Sockets_Socket_SupportPortReuse))
HANDLES(ICALL(SOCK_21a, "cancel_blocking_socket_operation", icall_cancel_blocking_socket_operation))

ICALL_TYPE(SOCKEX, "System.Net.Sockets.SocketException", SOCKEX_1)
ICALL(SOCKEX_1, "WSAGetLastError_internal", ves_icall_System_Net_Sockets_SocketException_WSAGetLastError_internal)
#endif /* !DISABLE_SOCKETS */

ICALL_TYPE(NUMBER, "System.Number", NUMBER_1)
ICALL(NUMBER_1, "NumberBufferToDecimal", mono_decimal_from_number)
ICALL(NUMBER_2, "NumberBufferToDouble", mono_double_from_number)

ICALL_TYPE(NUMBER_FORMATTER, "System.NumberFormatter", NUMBER_FORMATTER_1)
ICALL(NUMBER_FORMATTER_1, "GetFormatterTables", ves_icall_System_NumberFormatter_GetFormatterTables)

ICALL_TYPE(OBJ, "System.Object", OBJ_1)
HANDLES(ICALL(OBJ_1, "GetType", ves_icall_System_Object_GetType))
ICALL(OBJ_2, "InternalGetHashCode", mono_object_hash)
ICALL(OBJ_3, "MemberwiseClone", ves_icall_System_Object_MemberwiseClone)

ICALL_TYPE(ASSEM, "System.Reflection.Assembly", ASSEM_1a)
HANDLES(ICALL(ASSEM_1a, "GetAotId", ves_icall_System_Reflection_Assembly_GetAotId))
HANDLES(ICALL(ASSEM_2, "GetCallingAssembly", ves_icall_System_Reflection_Assembly_GetCallingAssembly))
HANDLES(ICALL(ASSEM_3, "GetEntryAssembly", ves_icall_System_Reflection_Assembly_GetEntryAssembly))
HANDLES(ICALL(ASSEM_4, "GetExecutingAssembly", ves_icall_System_Reflection_Assembly_GetExecutingAssembly))
HANDLES(ICALL(ASSEM_5, "GetFilesInternal", ves_icall_System_Reflection_Assembly_GetFilesInternal))
HANDLES(ICALL(ASSEM_6, "GetManifestModuleInternal", ves_icall_System_Reflection_Assembly_GetManifestModuleInternal))
HANDLES(ICALL(ASSEM_7, "GetManifestResourceInfoInternal", ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal))
HANDLES(ICALL(ASSEM_8, "GetManifestResourceInternal", ves_icall_System_Reflection_Assembly_GetManifestResourceInternal))
HANDLES(ICALL(ASSEM_9, "GetManifestResourceNames", ves_icall_System_Reflection_Assembly_GetManifestResourceNames))
HANDLES(ICALL(ASSEM_10, "GetModulesInternal", ves_icall_System_Reflection_Assembly_GetModulesInternal))
//ICALL(ASSEM_11, "GetNamespaces", ves_icall_System_Reflection_Assembly_GetNamespaces)
HANDLES(ICALL(ASSEM_13, "GetTypes", ves_icall_System_Reflection_Assembly_GetTypes))
HANDLES(ICALL(ASSEM_14, "InternalGetAssemblyName", ves_icall_System_Reflection_Assembly_InternalGetAssemblyName))
HANDLES(ICALL(ASSEM_12, "InternalGetReferencedAssemblies", ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies))
HANDLES(ICALL(ASSEM_15, "InternalGetType", ves_icall_System_Reflection_Assembly_InternalGetType))
HANDLES(ICALL(ASSEM_16, "InternalImageRuntimeVersion", ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion))
HANDLES(ICALL(ASSEM_17, "LoadFrom", ves_icall_System_Reflection_Assembly_LoadFrom))
HANDLES(ICALL(ASSEM_18, "LoadPermissions", ves_icall_System_Reflection_Assembly_LoadPermissions))

	/* normal icalls again */
HANDLES(ICALL(ASSEM_20, "get_EntryPoint", ves_icall_System_Reflection_Assembly_get_EntryPoint))
HANDLES(ICALL(ASSEM_21, "get_ReflectionOnly", ves_icall_System_Reflection_Assembly_get_ReflectionOnly))
HANDLES(ICALL(ASSEM_22, "get_code_base", ves_icall_System_Reflection_Assembly_get_code_base))
HANDLES(ICALL(ASSEM_23, "get_fullname", ves_icall_System_Reflection_Assembly_get_fullName))
HANDLES(ICALL(ASSEM_24, "get_global_assembly_cache", ves_icall_System_Reflection_Assembly_get_global_assembly_cache))
HANDLES(ICALL(ASSEM_25, "get_location", ves_icall_System_Reflection_Assembly_get_location))
HANDLES(ICALL(ASSEM_26, "load_with_partial_name", ves_icall_System_Reflection_Assembly_load_with_partial_name))

ICALL_TYPE(ASSEMN, "System.Reflection.AssemblyName", ASSEMN_0)
ICALL(ASSEMN_0, "GetNativeName", ves_icall_System_Reflection_AssemblyName_GetNativeName)
ICALL(ASSEMN_3, "ParseAssemblyName", ves_icall_System_Reflection_AssemblyName_ParseAssemblyName)
ICALL(ASSEMN_2, "get_public_token", mono_digest_get_public_token)

ICALL_TYPE(CATTR_DATA, "System.Reflection.CustomAttributeData", CATTR_DATA_1)
ICALL(CATTR_DATA_1, "ResolveArgumentsInternal", ves_icall_System_Reflection_CustomAttributeData_ResolveArgumentsInternal)

ICALL_TYPE(ASSEMB, "System.Reflection.Emit.AssemblyBuilder", ASSEMB_2)
ICALL(ASSEMB_2, "basic_init", ves_icall_AssemblyBuilder_basic_init)

#ifndef DISABLE_REFLECTION_EMIT
ICALL_TYPE(CATTRB, "System.Reflection.Emit.CustomAttributeBuilder", CATTRB_1)
ICALL(CATTRB_1, "GetBlob", ves_icall_CustomAttributeBuilder_GetBlob)
#endif

ICALL_TYPE(DYNM, "System.Reflection.Emit.DynamicMethod", DYNM_1)
HANDLES(ICALL(DYNM_1, "create_dynamic_method", ves_icall_DynamicMethod_create_dynamic_method))

ICALL_TYPE(ENUMB, "System.Reflection.Emit.EnumBuilder", ENUMB_1)
HANDLES(ICALL(ENUMB_1, "setup_enum_type", ves_icall_EnumBuilder_setup_enum_type))

ICALL_TYPE(MODULEB, "System.Reflection.Emit.ModuleBuilder", MODULEB_10)
HANDLES(ICALL(MODULEB_10, "GetRegisteredToken", ves_icall_ModuleBuilder_GetRegisteredToken))
HANDLES(ICALL(MODULEB_8, "RegisterToken", ves_icall_ModuleBuilder_RegisterToken))
ICALL(MODULEB_1, "WriteToFile", ves_icall_ModuleBuilder_WriteToFile)
HANDLES(ICALL(MODULEB_2, "basic_init", ves_icall_ModuleBuilder_basic_init))
ICALL(MODULEB_3, "build_metadata", ves_icall_ModuleBuilder_build_metadata)
HANDLES(ICALL(MODULEB_5, "getMethodToken", ves_icall_ModuleBuilder_getMethodToken))
HANDLES(ICALL(MODULEB_6, "getToken", ves_icall_ModuleBuilder_getToken))
HANDLES(ICALL(MODULEB_7, "getUSIndex", ves_icall_ModuleBuilder_getUSIndex))
HANDLES(ICALL(MODULEB_9, "set_wrappers_type", ves_icall_ModuleBuilder_set_wrappers_type))

ICALL_TYPE(SIGH, "System.Reflection.Emit.SignatureHelper", SIGH_1)
HANDLES(ICALL(SIGH_1, "get_signature_field", ves_icall_SignatureHelper_get_signature_field))
HANDLES(ICALL(SIGH_2, "get_signature_local", ves_icall_SignatureHelper_get_signature_local))

ICALL_TYPE(TYPEB, "System.Reflection.Emit.TypeBuilder", TYPEB_1)
HANDLES(ICALL(TYPEB_1, "create_runtime_class", ves_icall_TypeBuilder_create_runtime_class))

ICALL_TYPE(EVENTI, "System.Reflection.EventInfo", EVENTI_1)
HANDLES(ICALL(EVENTI_1, "internal_from_handle_type", ves_icall_System_Reflection_EventInfo_internal_from_handle_type))

ICALL_TYPE(FIELDI, "System.Reflection.FieldInfo", FILEDI_1)
HANDLES(ICALL(FILEDI_1, "GetTypeModifiers", ves_icall_System_Reflection_FieldInfo_GetTypeModifiers))
HANDLES(ICALL(FILEDI_2, "get_marshal_info", ves_icall_System_Reflection_FieldInfo_get_marshal_info))
HANDLES(ICALL(FILEDI_3, "internal_from_handle_type", ves_icall_System_Reflection_FieldInfo_internal_from_handle_type))

ICALL_TYPE(MEMBERI, "System.Reflection.MemberInfo", MEMBERI_1)
HANDLES(ICALL(MEMBERI_1, "get_MetadataToken", ves_icall_reflection_get_token))

ICALL_TYPE(MBASE, "System.Reflection.MethodBase", MBASE_1)
HANDLES(ICALL(MBASE_1, "GetCurrentMethod", ves_icall_GetCurrentMethod))
HANDLES(ICALL(MBASE_2, "GetMethodBodyInternal", ves_icall_System_Reflection_MethodBase_GetMethodBodyInternal))
HANDLES(ICALL(MBASE_4, "GetMethodFromHandleInternalType_native", ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternalType_native))

ICALL_TYPE(MODULE, "System.Reflection.Module", MODULE_1)
HANDLES(ICALL(MODULE_1, "Close", ves_icall_System_Reflection_Module_Close))
HANDLES(ICALL(MODULE_2, "GetGlobalType", ves_icall_System_Reflection_Module_GetGlobalType))
HANDLES(ICALL(MODULE_3, "GetGuidInternal", ves_icall_System_Reflection_Module_GetGuidInternal))
HANDLES(ICALL(MODULE_14, "GetHINSTANCE", ves_icall_System_Reflection_Module_GetHINSTANCE))
HANDLES(ICALL(MODULE_4, "GetMDStreamVersion", ves_icall_System_Reflection_Module_GetMDStreamVersion))
HANDLES(ICALL(MODULE_5, "GetPEKind", ves_icall_System_Reflection_Module_GetPEKind))
HANDLES(ICALL(MODULE_6, "InternalGetTypes", ves_icall_System_Reflection_Module_InternalGetTypes))
HANDLES(ICALL(MODULE_7, "ResolveFieldToken", ves_icall_System_Reflection_Module_ResolveFieldToken))
HANDLES(ICALL(MODULE_8, "ResolveMemberToken", ves_icall_System_Reflection_Module_ResolveMemberToken))
HANDLES(ICALL(MODULE_9, "ResolveMethodToken", ves_icall_System_Reflection_Module_ResolveMethodToken))
HANDLES(ICALL(MODULE_10, "ResolveSignature", ves_icall_System_Reflection_Module_ResolveSignature))
ICALL(MODULE_11, "ResolveStringToken", ves_icall_System_Reflection_Module_ResolveStringToken)
HANDLES(ICALL(MODULE_12, "ResolveTypeToken", ves_icall_System_Reflection_Module_ResolveTypeToken))
HANDLES(ICALL(MODULE_13, "get_MetadataToken", ves_icall_reflection_get_token))

ICALL_TYPE(MCMETH, "System.Reflection.MonoCMethod", MCMETH_1)
HANDLES(ICALL(MCMETH_1, "GetGenericMethodDefinition_impl", ves_icall_MonoMethod_GetGenericMethodDefinition))
ICALL(MCMETH_2, "InternalInvoke", ves_icall_InternalInvoke)
HANDLES(ICALL(MCMETH_3, "get_core_clr_security_level", ves_icall_MonoMethod_get_core_clr_security_level))

ICALL_TYPE(MEVIN, "System.Reflection.MonoEventInfo", MEVIN_1)
HANDLES(ICALL(MEVIN_1, "get_event_info", ves_icall_MonoEventInfo_get_event_info))

ICALL_TYPE(MFIELD, "System.Reflection.MonoField", MFIELD_1)
ICALL(MFIELD_1, "GetFieldOffset", ves_icall_MonoField_GetFieldOffset)
HANDLES(ICALL(MFIELD_2, "GetParentType", ves_icall_MonoField_GetParentType))
ICALL(MFIELD_5, "GetRawConstantValue", ves_icall_MonoField_GetRawConstantValue)
ICALL(MFIELD_3, "GetValueInternal", ves_icall_MonoField_GetValueInternal)
HANDLES(ICALL(MFIELD_6, "ResolveType", ves_icall_MonoField_ResolveType))
HANDLES(ICALL(MFIELD_4, "SetValueInternal", ves_icall_MonoField_SetValueInternal))
ICALL(MFIELD_7, "get_core_clr_security_level", ves_icall_MonoField_get_core_clr_security_level)

ICALL_TYPE(MMETH, "System.Reflection.MonoMethod", MMETH_2)
HANDLES(ICALL(MMETH_2, "GetGenericArguments", ves_icall_MonoMethod_GetGenericArguments))
HANDLES(ICALL(MMETH_3, "GetGenericMethodDefinition_impl", ves_icall_MonoMethod_GetGenericMethodDefinition))
HANDLES(ICALL(MMETH_11, "GetPInvoke", ves_icall_MonoMethod_GetPInvoke))
ICALL(MMETH_4, "InternalInvoke", ves_icall_InternalInvoke)
HANDLES(ICALL(MMETH_5, "MakeGenericMethod_impl", ves_icall_MonoMethod_MakeGenericMethod_impl))
HANDLES(ICALL(MMETH_6, "get_IsGenericMethod", ves_icall_MonoMethod_get_IsGenericMethod))
HANDLES(ICALL(MMETH_7, "get_IsGenericMethodDefinition", ves_icall_MonoMethod_get_IsGenericMethodDefinition))
HANDLES(ICALL(MMETH_8, "get_base_method", ves_icall_MonoMethod_get_base_method))
HANDLES(ICALL(MMETH_10, "get_core_clr_security_level", ves_icall_MonoMethod_get_core_clr_security_level))
HANDLES(ICALL(MMETH_9, "get_name", ves_icall_MonoMethod_get_name))

ICALL_TYPE(MMETHI, "System.Reflection.MonoMethodInfo", MMETHI_4)
ICALL(MMETHI_4, "get_method_attributes", vell_icall_get_method_attributes)
HANDLES(ICALL(MMETHI_1, "get_method_info", ves_icall_get_method_info))
HANDLES(ICALL(MMETHI_2, "get_parameter_info", ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info))
HANDLES(ICALL(MMETHI_3, "get_retval_marshal", ves_icall_System_MonoMethodInfo_get_retval_marshal))

ICALL_TYPE(MPROPI, "System.Reflection.MonoPropertyInfo", MPROPI_1)
HANDLES(ICALL(MPROPI_1, "GetTypeModifiers", ves_icall_MonoPropertyInfo_GetTypeModifiers))
ICALL(MPROPI_3, "get_default_value", property_info_get_default_value)
HANDLES(ICALL(MPROPI_2, "get_property_info", ves_icall_MonoPropertyInfo_get_property_info))

ICALL_TYPE(PARAMI, "System.Reflection.ParameterInfo", PARAMI_1)
HANDLES(ICALL(PARAMI_1, "GetMetadataToken", ves_icall_reflection_get_token))
HANDLES(ICALL(PARAMI_2, "GetTypeModifiers", ves_icall_ParameterInfo_GetTypeModifiers))

ICALL_TYPE(PROPI, "System.Reflection.PropertyInfo", PROPI_1)
HANDLES(ICALL(PROPI_1, "internal_from_handle_type", ves_icall_System_Reflection_PropertyInfo_internal_from_handle_type))

ICALL_TYPE(RTFIELD, "System.Reflection.RtFieldInfo", RTFIELD_1)
ICALL(RTFIELD_1, "UnsafeGetValue", ves_icall_MonoField_GetValueInternal)

ICALL_TYPE(RUNH, "System.Runtime.CompilerServices.RuntimeHelpers", RUNH_1)
ICALL(RUNH_1, "GetObjectValue", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue)
	 /* REMOVEME: no longer needed, just so we dont break things when not needed */
ICALL(RUNH_2, "GetOffsetToStringData", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData)
HANDLES(ICALL(RUNH_3, "InitializeArray", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray))
ICALL(RUNH_4, "RunClassConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor)
ICALL(RUNH_5, "RunModuleConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor)
ICALL(RUNH_5h, "SufficientExecutionStack", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack)
ICALL(RUNH_6, "get_OffsetToStringData", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetOffsetToStringData)

ICALL_TYPE(GCH, "System.Runtime.InteropServices.GCHandle", GCH_1)
ICALL(GCH_1, "CheckCurrentDomain", mono_gc_GCHandle_CheckCurrentDomain)
ICALL(GCH_2, "FreeHandle", ves_icall_System_GCHandle_FreeHandle)
ICALL(GCH_3, "GetAddrOfPinnedObject", ves_icall_System_GCHandle_GetAddrOfPinnedObject)
ICALL(GCH_4, "GetTarget", ves_icall_System_GCHandle_GetTarget)
ICALL(GCH_5, "GetTargetHandle", ves_icall_System_GCHandle_GetTargetHandle)

#ifndef DISABLE_COM
ICALL_TYPE(MARSHAL, "System.Runtime.InteropServices.Marshal", MARSHAL_1)
ICALL(MARSHAL_1, "AddRefInternal", ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal)
#else
ICALL_TYPE(MARSHAL, "System.Runtime.InteropServices.Marshal", MARSHAL_2)
#endif
ICALL(MARSHAL_2, "AllocCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem)
ICALL(MARSHAL_51,"AllocCoTaskMemSize(uintptr)", ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMemSize)
ICALL(MARSHAL_3, "AllocHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal)
ICALL(MARSHAL_50, "BufferToBSTR", ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR)
ICALL(MARSHAL_4, "DestroyStructure", ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure)
ICALL(MARSHAL_5, "FreeBSTR", ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR)
ICALL(MARSHAL_6, "FreeCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem)
ICALL(MARSHAL_7, "FreeHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal)
#ifndef DISABLE_COM
ICALL(MARSHAL_44, "GetCCW", ves_icall_System_Runtime_InteropServices_Marshal_GetCCW)
ICALL(MARSHAL_8, "GetComSlotForMethodInfoInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal)
#endif
HANDLES(ICALL(MARSHAL_9, "GetDelegateForFunctionPointerInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal))
HANDLES(ICALL(MARSHAL_10, "GetFunctionPointerForDelegateInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal))
#ifndef DISABLE_COM
ICALL(MARSHAL_52, "GetHRForException_WinRT", ves_icall_System_Runtime_InteropServices_Marshal_GetHRForException_WinRT)
ICALL(MARSHAL_45, "GetIDispatchForObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal)
ICALL(MARSHAL_46, "GetIUnknownForObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal)
#endif
ICALL(MARSHAL_11, "GetLastWin32Error", ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error)
#ifndef DISABLE_COM
ICALL(MARSHAL_53, "GetNativeActivationFactory", ves_icall_System_Runtime_InteropServices_Marshal_GetNativeActivationFactory)
ICALL(MARSHAL_47, "GetObjectForCCW", ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW)
ICALL(MARSHAL_54, "GetRawIUnknownForComObjectNoAddRef", ves_icall_System_Runtime_InteropServices_Marshal_GetRawIUnknownForComObjectNoAddRef)
ICALL(MARSHAL_48, "IsComObject", ves_icall_System_Runtime_InteropServices_Marshal_IsComObject)
#endif
HANDLES(ICALL(MARSHAL_12, "OffsetOf", ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf))
HANDLES(ICALL(MARSHAL_13, "Prelink", ves_icall_System_Runtime_InteropServices_Marshal_Prelink))
HANDLES(ICALL(MARSHAL_14, "PrelinkAll", ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll))
HANDLES(ICALL(MARSHAL_15, "PtrToStringAnsi(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi))
ICALL(MARSHAL_16, "PtrToStringAnsi(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len)
ICALL(MARSHAL_17, "PtrToStringBSTR", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR)
ICALL(MARSHAL_18, "PtrToStringUni(intptr)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni)
ICALL(MARSHAL_19, "PtrToStringUni(intptr,int)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len)
ICALL(MARSHAL_20, "PtrToStructure(intptr,System.Type)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type)
ICALL(MARSHAL_21, "PtrToStructure(intptr,object)", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure)
#ifndef DISABLE_COM
ICALL(MARSHAL_22, "QueryInterfaceInternal", ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal)
#endif
ICALL(MARSHAL_43, "ReAllocCoTaskMem", ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem)
ICALL(MARSHAL_23, "ReAllocHGlobal", ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal)
#ifndef DISABLE_COM
ICALL(MARSHAL_49, "ReleaseComObjectInternal", ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal)
ICALL(MARSHAL_29, "ReleaseInternal", ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal)
#endif
HANDLES(ICALL(MARSHAL_30, "SizeOf", ves_icall_System_Runtime_InteropServices_Marshal_SizeOf))
ICALL(MARSHAL_31, "StringToBSTR", ves_icall_System_Runtime_InteropServices_Marshal_StringToBSTR)
ICALL(MARSHAL_32, "StringToHGlobalAnsi", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi)
ICALL(MARSHAL_33, "StringToHGlobalUni", ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni)
ICALL(MARSHAL_34, "StructureToPtr", ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr)
ICALL(MARSHAL_35, "UnsafeAddrOfPinnedArrayElement", ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement)

ICALL(MARSHAL_41, "copy_from_unmanaged", ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged)
ICALL(MARSHAL_42, "copy_to_unmanaged", ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged)

#ifndef DISABLE_COM
ICALL_TYPE(WINDOWSRUNTIME_UNM, "System.Runtime.InteropServices.WindowsRuntime.UnsafeNativeMethods", WINDOWSRUNTIME_UNM_0)
ICALL(WINDOWSRUNTIME_UNM_0, "GetRestrictedErrorInfo", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_GetRestrictedErrorInfo)
ICALL(WINDOWSRUNTIME_UNM_1, "RoOriginateLanguageException", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoOriginateLanguageException)
ICALL(WINDOWSRUNTIME_UNM_2, "RoReportUnhandledError", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoReportUnhandledError)
ICALL(WINDOWSRUNTIME_UNM_3, "WindowsCreateString", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsCreateString)
ICALL(WINDOWSRUNTIME_UNM_4, "WindowsDeleteString", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsDeleteString)
ICALL(WINDOWSRUNTIME_UNM_5, "WindowsGetStringRawBuffer", ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsGetStringRawBuffer)
#endif

ICALL_TYPE(ACTS, "System.Runtime.Remoting.Activation.ActivationServices", ACTS_1)
HANDLES(ICALL(ACTS_1, "AllocateUninitializedClassInstance", ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance))
HANDLES(ICALL(ACTS_2, "EnableProxyActivation", ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation))

ICALL_TYPE(CONTEXT, "System.Runtime.Remoting.Contexts.Context", CONTEXT_1)
HANDLES(ICALL(CONTEXT_1, "RegisterContext", ves_icall_System_Runtime_Remoting_Contexts_Context_RegisterContext))
HANDLES(ICALL(CONTEXT_2, "ReleaseContext", ves_icall_System_Runtime_Remoting_Contexts_Context_ReleaseContext))

ICALL_TYPE(ARES, "System.Runtime.Remoting.Messaging.AsyncResult", ARES_1)
ICALL(ARES_1, "Invoke", ves_icall_System_Runtime_Remoting_Messaging_AsyncResult_Invoke)

#ifndef DISABLE_REMOTING
ICALL_TYPE(REALP, "System.Runtime.Remoting.Proxies.RealProxy", REALP_1)
ICALL(REALP_1, "InternalGetProxyType", ves_icall_Remoting_RealProxy_InternalGetProxyType)
HANDLES(ICALL(REALP_2, "InternalGetTransparentProxy", ves_icall_Remoting_RealProxy_GetTransparentProxy))

ICALL_TYPE(REMSER, "System.Runtime.Remoting.RemotingServices", REMSER_0)
HANDLES(ICALL(REMSER_0, "GetVirtualMethod", ves_icall_Remoting_RemotingServices_GetVirtualMethod))
ICALL(REMSER_1, "InternalExecute", ves_icall_InternalExecute)
HANDLES(ICALL(REMSER_2, "IsTransparentProxy", ves_icall_IsTransparentProxy))
#endif

ICALL_TYPE(RVH, "System.Runtime.Versioning.VersioningHelper", RVH_1)
ICALL(RVH_1, "GetRuntimeId", ves_icall_System_Runtime_Versioning_VersioningHelper_GetRuntimeId)

ICALL_TYPE(RFH, "System.RuntimeFieldHandle", RFH_1)
ICALL(RFH_1, "SetValueDirect", ves_icall_System_RuntimeFieldHandle_SetValueDirect)
HANDLES(ICALL(RFH_2, "SetValueInternal", ves_icall_MonoField_SetValueInternal))

ICALL_TYPE(MHAN, "System.RuntimeMethodHandle", MHAN_1)
HANDLES(ICALL(MHAN_1, "GetFunctionPointer", ves_icall_RuntimeMethodHandle_GetFunctionPointer))

ICALL_TYPE(RT, "System.RuntimeType", RT_1)
HANDLES(ICALL(RT_1, "CreateInstanceInternal", ves_icall_System_Activator_CreateInstanceInternal))
HANDLES(ICALL(RT_2, "GetConstructors_native", ves_icall_RuntimeType_GetConstructors_native))
HANDLES(ICALL(RT_30, "GetCorrespondingInflatedConstructor", ves_icall_RuntimeType_GetCorrespondingInflatedMethod))
HANDLES(ICALL(RT_31, "GetCorrespondingInflatedMethod", ves_icall_RuntimeType_GetCorrespondingInflatedMethod))
HANDLES(ICALL(RT_3, "GetEvents_native", ves_icall_RuntimeType_GetEvents_native))
HANDLES(ICALL(RT_5, "GetFields_native", ves_icall_RuntimeType_GetFields_native))
HANDLES(ICALL(RT_6, "GetGenericArgumentsInternal", ves_icall_RuntimeType_GetGenericArguments))
HANDLES(ICALL(RT_9, "GetGenericParameterPosition", ves_icall_RuntimeType_GetGenericParameterPosition))
HANDLES(ICALL(RT_10, "GetInterfaceMapData", ves_icall_RuntimeType_GetInterfaceMapData))
HANDLES(ICALL(RT_11, "GetInterfaces", ves_icall_RuntimeType_GetInterfaces))
HANDLES(ICALL(RT_12, "GetMethodsByName_native", ves_icall_RuntimeType_GetMethodsByName_native))
HANDLES(ICALL(RT_13, "GetNestedTypes_native", ves_icall_RuntimeType_GetNestedTypes_native))
HANDLES(ICALL(RT_14, "GetPacking", ves_icall_RuntimeType_GetPacking))
HANDLES(ICALL(RT_15, "GetPropertiesByName_native", ves_icall_RuntimeType_GetPropertiesByName_native))
HANDLES(ICALL(RT_16, "GetTypeCodeImplInternal", ves_icall_type_GetTypeCodeInternal))
HANDLES(ICALL(RT_28, "IsTypeExportedToWindowsRuntime", ves_icall_System_RuntimeType_IsTypeExportedToWindowsRuntime))
HANDLES(ICALL(RT_29, "IsWindowsRuntimeObjectType", ves_icall_System_RuntimeType_IsWindowsRuntimeObjectType))
HANDLES(ICALL(RT_17, "MakeGenericType", ves_icall_RuntimeType_MakeGenericType))
HANDLES(ICALL(RT_18, "MakePointerType", ves_icall_RuntimeType_MakePointerType))
HANDLES(ICALL(RT_19, "getFullName", ves_icall_System_RuntimeType_getFullName))
HANDLES(ICALL(RT_21, "get_DeclaringMethod", ves_icall_RuntimeType_get_DeclaringMethod))
HANDLES(ICALL(RT_22, "get_DeclaringType", ves_icall_RuntimeType_get_DeclaringType))
HANDLES(ICALL(RT_23, "get_Name", ves_icall_RuntimeType_get_Name))
HANDLES(ICALL(RT_24, "get_Namespace", ves_icall_RuntimeType_get_Namespace))
HANDLES(ICALL(RT_25, "get_core_clr_security_level", vell_icall_RuntimeType_get_core_clr_security_level))
HANDLES(ICALL(RT_26, "make_array_type", ves_icall_RuntimeType_make_array_type))
HANDLES(ICALL(RT_27, "make_byref_type", ves_icall_RuntimeType_make_byref_type))

ICALL_TYPE(RTH, "System.RuntimeTypeHandle", RTH_1)
HANDLES(ICALL(RTH_1, "GetArrayRank", ves_icall_RuntimeTypeHandle_GetArrayRank))
HANDLES(ICALL(RTH_2, "GetAssembly", ves_icall_RuntimeTypeHandle_GetAssembly))
HANDLES(ICALL(RTH_3, "GetAttributes", ves_icall_RuntimeTypeHandle_GetAttributes))
HANDLES(ICALL(RTH_4, "GetBaseType", ves_icall_RuntimeTypeHandle_GetBaseType))
HANDLES(ICALL(RTH_5, "GetElementType", ves_icall_RuntimeTypeHandle_GetElementType))
HANDLES(ICALL(RTH_19, "GetGenericParameterInfo", ves_icall_RuntimeTypeHandle_GetGenericParameterInfo))
HANDLES(ICALL(RTH_6, "GetGenericTypeDefinition_impl", ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl))
HANDLES(ICALL(RTH_7, "GetMetadataToken", ves_icall_reflection_get_token))
HANDLES(ICALL(RTH_8, "GetModule", ves_icall_RuntimeTypeHandle_GetModule))
HANDLES(ICALL(RTH_9, "HasInstantiation", ves_icall_RuntimeTypeHandle_HasInstantiation))
HANDLES(ICALL(RTH_20, "HasReferences", ves_icall_RuntimeTypeHandle_HasReferences))
HANDLES(ICALL(RTH_10, "IsArray", ves_icall_RuntimeTypeHandle_IsArray))
HANDLES(ICALL(RTH_11, "IsByRef", ves_icall_RuntimeTypeHandle_IsByRef))
HANDLES(ICALL(RTH_12, "IsComObject", ves_icall_RuntimeTypeHandle_IsComObject))
HANDLES(ICALL(RTH_13, "IsGenericTypeDefinition", ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition))
HANDLES(ICALL(RTH_14, "IsGenericVariable", ves_icall_RuntimeTypeHandle_IsGenericVariable))
HANDLES(ICALL(RTH_15, "IsInstanceOfType", ves_icall_RuntimeTypeHandle_IsInstanceOfType))
HANDLES(ICALL(RTH_16, "IsPointer", ves_icall_RuntimeTypeHandle_IsPointer))
HANDLES(ICALL(RTH_17, "IsPrimitive", ves_icall_RuntimeTypeHandle_IsPrimitive))
HANDLES(ICALL(RTH_18, "type_is_assignable_from", ves_icall_RuntimeTypeHandle_type_is_assignable_from))

ICALL_TYPE(RNG, "System.Security.Cryptography.RNGCryptoServiceProvider", RNG_1)
ICALL(RNG_1, "RngClose", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose)
ICALL(RNG_2, "RngGetBytes", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes)
ICALL(RNG_3, "RngInitialize", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize)
ICALL(RNG_4, "RngOpen", ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen)

#ifndef DISABLE_POLICY_EVIDENCE
ICALL_TYPE(EVID, "System.Security.Policy.Evidence", EVID_1)
HANDLES(ICALL(EVID_1, "IsAuthenticodePresent", ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent))

ICALL_TYPE(WINID, "System.Security.Principal.WindowsIdentity", WINID_1)
HANDLES(ICALL(WINID_1, "GetCurrentToken", ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken))
HANDLES(ICALL(WINID_2, "GetTokenName", ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName))
HANDLES(ICALL(WINID_3, "GetUserToken", ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken))
ICALL(WINID_4, "_GetRoles", ves_icall_System_Security_Principal_WindowsIdentity_GetRoles)

ICALL_TYPE(WINIMP, "System.Security.Principal.WindowsImpersonationContext", WINIMP_1)
ICALL(WINIMP_1, "CloseToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken)
ICALL(WINIMP_2, "DuplicateToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken)
ICALL(WINIMP_3, "RevertToSelf", ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf)
ICALL(WINIMP_4, "SetCurrentToken", ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken)

ICALL_TYPE(WINPRIN, "System.Security.Principal.WindowsPrincipal", WINPRIN_1)
ICALL(WINPRIN_1, "IsMemberOfGroupId", ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId)
ICALL(WINPRIN_2, "IsMemberOfGroupName", ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName)

ICALL_TYPE(SECSTRING, "System.Security.SecureString", SECSTRING_1)
ICALL(SECSTRING_1, "DecryptInternal", ves_icall_System_Security_SecureString_DecryptInternal)
ICALL(SECSTRING_2, "EncryptInternal", ves_icall_System_Security_SecureString_EncryptInternal)
#endif /* !DISABLE_POLICY_EVIDENCE */

ICALL_TYPE(SECMAN, "System.Security.SecurityManager", SECMAN_1)
ICALL(SECMAN_1, "get_RequiresElevatedPermissions", mono_security_core_clr_require_elevated_permissions)
ICALL(SECMAN_2, "get_SecurityEnabled", ves_icall_System_Security_SecurityManager_get_SecurityEnabled)
ICALL(SECMAN_3, "set_SecurityEnabled", ves_icall_System_Security_SecurityManager_set_SecurityEnabled)

ICALL_TYPE(STRING, "System.String", STRING_1)
ICALL(STRING_1, ".ctor(char*)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_2, ".ctor(char*,int,int)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_3, ".ctor(char,int)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_4, ".ctor(char[])", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_5, ".ctor(char[],int,int)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_6, ".ctor(sbyte*)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_7, ".ctor(sbyte*,int,int)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_8, ".ctor(sbyte*,int,int,System.Text.Encoding)", ves_icall_System_String_ctor_RedirectToCreateString)
ICALL(STRING_9, "FastAllocateString", ves_icall_System_String_InternalAllocateStr)
ICALL(STRING_10, "InternalIntern", ves_icall_System_String_InternalIntern)
ICALL(STRING_11, "InternalIsInterned", ves_icall_System_String_InternalIsInterned)

ICALL_TYPE(TENC, "System.Text.EncodingHelper", TENC_1)
HANDLES(ICALL(TENC_1, "InternalCodePage", ves_icall_System_Text_EncodingHelper_InternalCodePage))

ICALL_TYPE(UNORM, "System.Text.Normalization", UNORM_1)
HANDLES(ICALL(UNORM_1, "load_normalization_resource", ves_icall_System_Text_Normalization_load_normalization_resource))

ICALL_TYPE(ILOCK, "System.Threading.Interlocked", ILOCK_1)
ICALL(ILOCK_1, "Add(int&,int)", ves_icall_System_Threading_Interlocked_Add_Int)
ICALL(ILOCK_2, "Add(long&,long)", ves_icall_System_Threading_Interlocked_Add_Long)
ICALL(ILOCK_3, "CompareExchange(T&,T,T)", ves_icall_System_Threading_Interlocked_CompareExchange_T)
ICALL(ILOCK_4, "CompareExchange(double&,double,double)", ves_icall_System_Threading_Interlocked_CompareExchange_Double)
ICALL(ILOCK_5, "CompareExchange(int&,int,int)", ves_icall_System_Threading_Interlocked_CompareExchange_Int)
ICALL(ILOCK_6, "CompareExchange(int&,int,int,bool&)", ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success)
ICALL(ILOCK_7, "CompareExchange(intptr&,intptr,intptr)", ves_icall_System_Threading_Interlocked_CompareExchange_IntPtr)
ICALL(ILOCK_8, "CompareExchange(long&,long,long)", ves_icall_System_Threading_Interlocked_CompareExchange_Long)
ICALL(ILOCK_9, "CompareExchange(object&,object,object)", ves_icall_System_Threading_Interlocked_CompareExchange_Object)
ICALL(ILOCK_10, "CompareExchange(single&,single,single)", ves_icall_System_Threading_Interlocked_CompareExchange_Single)
ICALL(ILOCK_11, "Decrement(int&)", ves_icall_System_Threading_Interlocked_Decrement_Int)
ICALL(ILOCK_12, "Decrement(long&)", ves_icall_System_Threading_Interlocked_Decrement_Long)
ICALL(ILOCK_13, "Exchange(T&,T)", ves_icall_System_Threading_Interlocked_Exchange_T)
ICALL(ILOCK_14, "Exchange(double&,double)", ves_icall_System_Threading_Interlocked_Exchange_Double)
ICALL(ILOCK_15, "Exchange(int&,int)", ves_icall_System_Threading_Interlocked_Exchange_Int)
ICALL(ILOCK_16, "Exchange(intptr&,intptr)", ves_icall_System_Threading_Interlocked_Exchange_IntPtr)
ICALL(ILOCK_17, "Exchange(long&,long)", ves_icall_System_Threading_Interlocked_Exchange_Long)
ICALL(ILOCK_18, "Exchange(object&,object)", ves_icall_System_Threading_Interlocked_Exchange_Object)
ICALL(ILOCK_19, "Exchange(single&,single)", ves_icall_System_Threading_Interlocked_Exchange_Single)
ICALL(ILOCK_20, "Increment(int&)", ves_icall_System_Threading_Interlocked_Increment_Int)
ICALL(ILOCK_21, "Increment(long&)", ves_icall_System_Threading_Interlocked_Increment_Long)
ICALL(ILOCK_22, "Read(long&)", ves_icall_System_Threading_Interlocked_Read_Long)

ICALL_TYPE(ITHREAD, "System.Threading.InternalThread", ITHREAD_1)
ICALL(ITHREAD_1, "Thread_free_internal", ves_icall_System_Threading_InternalThread_Thread_free_internal)

ICALL_TYPE(MONIT, "System.Threading.Monitor", MONIT_8)
ICALL(MONIT_8, "Enter", mono_monitor_enter_internal)
ICALL(MONIT_1, "Exit", mono_monitor_exit)
ICALL(MONIT_2, "Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse)
ICALL(MONIT_3, "Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all)
ICALL(MONIT_4, "Monitor_test_owner", ves_icall_System_Threading_Monitor_Monitor_test_owner)
ICALL(MONIT_5, "Monitor_test_synchronised", ves_icall_System_Threading_Monitor_Monitor_test_synchronised)
ICALL(MONIT_7, "Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait)
ICALL(MONIT_9, "try_enter_with_atomic_var", ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var)

ICALL_TYPE(MUTEX, "System.Threading.Mutex", MUTEX_1)
HANDLES(ICALL(MUTEX_1, "CreateMutex_internal(bool,string,bool&)", ves_icall_System_Threading_Mutex_CreateMutex_internal))
HANDLES(ICALL(MUTEX_2, "OpenMutex_internal(string,System.Security.AccessControl.MutexRights,System.IO.MonoIOError&)", ves_icall_System_Threading_Mutex_OpenMutex_internal))
ICALL(MUTEX_3, "ReleaseMutex_internal(intptr)", ves_icall_System_Threading_Mutex_ReleaseMutex_internal)

ICALL_TYPE(NATIVEC, "System.Threading.NativeEventCalls", NATIVEC_1)
ICALL(NATIVEC_1, "CloseEvent_internal", ves_icall_System_Threading_Events_CloseEvent_internal)
HANDLES(ICALL(NATIVEC_2, "CreateEvent_internal(bool,bool,string,int&)", ves_icall_System_Threading_Events_CreateEvent_internal))
HANDLES(ICALL(NATIVEC_3, "OpenEvent_internal(string,System.Security.AccessControl.EventWaitHandleRights,int&)", ves_icall_System_Threading_Events_OpenEvent_internal))
ICALL(NATIVEC_4, "ResetEvent_internal",  ves_icall_System_Threading_Events_ResetEvent_internal)
ICALL(NATIVEC_5, "SetEvent_internal",    ves_icall_System_Threading_Events_SetEvent_internal)

ICALL_TYPE(SEMA, "System.Threading.Semaphore", SEMA_1)
ICALL(SEMA_1, "CreateSemaphore_internal(int,int,string,int&)", ves_icall_System_Threading_Semaphore_CreateSemaphore_internal)
ICALL(SEMA_2, "OpenSemaphore_internal(string,System.Security.AccessControl.SemaphoreRights,int&)", ves_icall_System_Threading_Semaphore_OpenSemaphore_internal)
ICALL(SEMA_3, "ReleaseSemaphore_internal(intptr,int,int&)", ves_icall_System_Threading_Semaphore_ReleaseSemaphore_internal)

ICALL_TYPE(THREAD, "System.Threading.Thread", THREAD_1)
ICALL(THREAD_1, "Abort_internal(System.Threading.InternalThread,object)", ves_icall_System_Threading_Thread_Abort)
ICALL(THREAD_1a, "ByteArrayToCurrentDomain(byte[])", ves_icall_System_Threading_Thread_ByteArrayToCurrentDomain)
ICALL(THREAD_1b, "ByteArrayToRootDomain(byte[])", ves_icall_System_Threading_Thread_ByteArrayToRootDomain)
ICALL(THREAD_2, "ClrState(System.Threading.InternalThread,System.Threading.ThreadState)", ves_icall_System_Threading_Thread_ClrState)
ICALL(THREAD_2a, "ConstructInternalThread", ves_icall_System_Threading_Thread_ConstructInternalThread)
ICALL(THREAD_55, "GetAbortExceptionState", ves_icall_System_Threading_Thread_GetAbortExceptionState)
ICALL(THREAD_60, "GetCurrentThread", ves_icall_System_Threading_Thread_GetCurrentThread)
ICALL(THREAD_7, "GetDomainID", ves_icall_System_Threading_Thread_GetDomainID)
ICALL(THREAD_8, "GetName_internal(System.Threading.InternalThread)", ves_icall_System_Threading_Thread_GetName_internal)
ICALL(THREAD_57, "GetPriorityNative", ves_icall_System_Threading_Thread_GetPriority)
ICALL(THREAD_59, "GetStackTraces", ves_icall_System_Threading_Thread_GetStackTraces)
ICALL(THREAD_11, "GetState(System.Threading.InternalThread)", ves_icall_System_Threading_Thread_GetState)
ICALL(THREAD_53, "InterruptInternal", ves_icall_System_Threading_Thread_Interrupt_internal)
ICALL(THREAD_12, "JoinInternal", ves_icall_System_Threading_Thread_Join_internal)
ICALL(THREAD_13, "MemoryBarrier", ves_icall_System_Threading_Thread_MemoryBarrier)
ICALL(THREAD_14, "ResetAbortNative", ves_icall_System_Threading_Thread_ResetAbort)
ICALL(THREAD_15, "ResumeInternal", ves_icall_System_Threading_Thread_Resume)
ICALL(THREAD_18, "SetName_internal(System.Threading.InternalThread,string)", ves_icall_System_Threading_Thread_SetName_internal)
ICALL(THREAD_58, "SetPriorityNative", ves_icall_System_Threading_Thread_SetPriority)
ICALL(THREAD_21, "SetState(System.Threading.InternalThread,System.Threading.ThreadState)", ves_icall_System_Threading_Thread_SetState)
ICALL(THREAD_22, "SleepInternal", ves_icall_System_Threading_Thread_Sleep_internal)
ICALL(THREAD_54, "SpinWait_nop", ves_icall_System_Threading_Thread_SpinWait_nop)
ICALL(THREAD_23, "SuspendInternal", ves_icall_System_Threading_Thread_Suspend)
ICALL(THREAD_56, "SystemMaxStackStize", mono_threads_get_max_stack_size)
ICALL(THREAD_25, "Thread_internal", ves_icall_System_Threading_Thread_Thread_internal)
ICALL(THREAD_26, "VolatileRead(byte&)", ves_icall_System_Threading_Thread_VolatileRead1)
ICALL(THREAD_27, "VolatileRead(double&)", ves_icall_System_Threading_Thread_VolatileReadDouble)
ICALL(THREAD_28, "VolatileRead(int&)", ves_icall_System_Threading_Thread_VolatileRead4)
ICALL(THREAD_29, "VolatileRead(int16&)", ves_icall_System_Threading_Thread_VolatileRead2)
ICALL(THREAD_30, "VolatileRead(intptr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr)
ICALL(THREAD_31, "VolatileRead(long&)", ves_icall_System_Threading_Thread_VolatileRead8)
ICALL(THREAD_32, "VolatileRead(object&)", ves_icall_System_Threading_Thread_VolatileReadObject)
ICALL(THREAD_33, "VolatileRead(sbyte&)", ves_icall_System_Threading_Thread_VolatileRead1)
ICALL(THREAD_34, "VolatileRead(single&)", ves_icall_System_Threading_Thread_VolatileReadFloat)
ICALL(THREAD_35, "VolatileRead(uint&)", ves_icall_System_Threading_Thread_VolatileRead4)
ICALL(THREAD_36, "VolatileRead(uint16&)", ves_icall_System_Threading_Thread_VolatileRead2)
ICALL(THREAD_37, "VolatileRead(uintptr&)", ves_icall_System_Threading_Thread_VolatileReadIntPtr)
ICALL(THREAD_38, "VolatileRead(ulong&)", ves_icall_System_Threading_Thread_VolatileRead8)
ICALL(THREAD_39, "VolatileWrite(byte&,byte)", ves_icall_System_Threading_Thread_VolatileWrite1)
ICALL(THREAD_40, "VolatileWrite(double&,double)", ves_icall_System_Threading_Thread_VolatileWriteDouble)
ICALL(THREAD_41, "VolatileWrite(int&,int)", ves_icall_System_Threading_Thread_VolatileWrite4)
ICALL(THREAD_42, "VolatileWrite(int16&,int16)", ves_icall_System_Threading_Thread_VolatileWrite2)
ICALL(THREAD_43, "VolatileWrite(intptr&,intptr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr)
ICALL(THREAD_44, "VolatileWrite(long&,long)", ves_icall_System_Threading_Thread_VolatileWrite8)
ICALL(THREAD_45, "VolatileWrite(object&,object)", ves_icall_System_Threading_Thread_VolatileWriteObject)
ICALL(THREAD_46, "VolatileWrite(sbyte&,sbyte)", ves_icall_System_Threading_Thread_VolatileWrite1)
ICALL(THREAD_47, "VolatileWrite(single&,single)", ves_icall_System_Threading_Thread_VolatileWriteFloat)
ICALL(THREAD_48, "VolatileWrite(uint&,uint)", ves_icall_System_Threading_Thread_VolatileWrite4)
ICALL(THREAD_49, "VolatileWrite(uint16&,uint16)", ves_icall_System_Threading_Thread_VolatileWrite2)
ICALL(THREAD_50, "VolatileWrite(uintptr&,uintptr)", ves_icall_System_Threading_Thread_VolatileWriteIntPtr)
ICALL(THREAD_51, "VolatileWrite(ulong&,ulong)", ves_icall_System_Threading_Thread_VolatileWrite8)
ICALL(THREAD_9, "YieldInternal", ves_icall_System_Threading_Thread_Yield)
ICALL(THREAD_52, "current_lcid()", ves_icall_System_Threading_Thread_current_lcid)

ICALL_TYPE(THREADP, "System.Threading.ThreadPool", THREADP_1)
ICALL(THREADP_1, "BindIOCompletionCallbackNative", ves_icall_System_Threading_ThreadPool_BindIOCompletionCallbackNative)
ICALL(THREADP_2, "GetAvailableThreadsNative", ves_icall_System_Threading_ThreadPool_GetAvailableThreadsNative)
ICALL(THREADP_3, "GetMaxThreadsNative", ves_icall_System_Threading_ThreadPool_GetMaxThreadsNative)
ICALL(THREADP_4, "GetMinThreadsNative", ves_icall_System_Threading_ThreadPool_GetMinThreadsNative)
ICALL(THREADP_5, "InitializeVMTp", ves_icall_System_Threading_ThreadPool_InitializeVMTp)
ICALL(THREADP_6, "IsThreadPoolHosted", ves_icall_System_Threading_ThreadPool_IsThreadPoolHosted)
ICALL(THREADP_7, "NotifyWorkItemComplete", ves_icall_System_Threading_ThreadPool_NotifyWorkItemComplete)
ICALL(THREADP_8, "NotifyWorkItemProgressNative", ves_icall_System_Threading_ThreadPool_NotifyWorkItemProgressNative)
ICALL(THREADP_9, "PostQueuedCompletionStatus", ves_icall_System_Threading_ThreadPool_PostQueuedCompletionStatus)
ICALL(THREADP_11, "ReportThreadStatus", ves_icall_System_Threading_ThreadPool_ReportThreadStatus)
ICALL(THREADP_12, "RequestWorkerThread", ves_icall_System_Threading_ThreadPool_RequestWorkerThread)
ICALL(THREADP_13, "SetMaxThreadsNative", ves_icall_System_Threading_ThreadPool_SetMaxThreadsNative)
ICALL(THREADP_14, "SetMinThreadsNative", ves_icall_System_Threading_ThreadPool_SetMinThreadsNative)

ICALL_TYPE(TTIMER, "System.Threading.Timer", TTIMER_1)
ICALL(TTIMER_1, "GetTimeMonotonic", mono_100ns_ticks)

ICALL_TYPE(VOLATILE, "System.Threading.Volatile", VOLATILE_28)
ICALL(VOLATILE_28, "Read(T&)", ves_icall_System_Threading_Volatile_Read_T)
ICALL(VOLATILE_1, "Read(bool&)", ves_icall_System_Threading_Volatile_Read1)
ICALL(VOLATILE_2, "Read(byte&)", ves_icall_System_Threading_Volatile_Read1)
ICALL(VOLATILE_3, "Read(double&)", ves_icall_System_Threading_Volatile_ReadDouble)
ICALL(VOLATILE_4, "Read(int&)", ves_icall_System_Threading_Volatile_Read4)
ICALL(VOLATILE_5, "Read(int16&)", ves_icall_System_Threading_Volatile_Read2)
ICALL(VOLATILE_6, "Read(intptr&)", ves_icall_System_Threading_Volatile_ReadIntPtr)
ICALL(VOLATILE_7, "Read(long&)", ves_icall_System_Threading_Volatile_Read8)
ICALL(VOLATILE_8, "Read(sbyte&)", ves_icall_System_Threading_Volatile_Read1)
ICALL(VOLATILE_9, "Read(single&)", ves_icall_System_Threading_Volatile_ReadFloat)
ICALL(VOLATILE_10, "Read(uint&)", ves_icall_System_Threading_Volatile_Read4)
ICALL(VOLATILE_11, "Read(uint16&)", ves_icall_System_Threading_Volatile_Read2)
ICALL(VOLATILE_12, "Read(uintptr&)", ves_icall_System_Threading_Volatile_ReadIntPtr)
ICALL(VOLATILE_13, "Read(ulong&)", ves_icall_System_Threading_Volatile_Read8)
ICALL(VOLATILE_27, "Write(T&,T)", ves_icall_System_Threading_Volatile_Write_T)
ICALL(VOLATILE_14, "Write(bool&,bool)", ves_icall_System_Threading_Volatile_Write1)
ICALL(VOLATILE_15, "Write(byte&,byte)", ves_icall_System_Threading_Volatile_Write1)
ICALL(VOLATILE_16, "Write(double&,double)", ves_icall_System_Threading_Volatile_WriteDouble)
ICALL(VOLATILE_17, "Write(int&,int)", ves_icall_System_Threading_Volatile_Write4)
ICALL(VOLATILE_18, "Write(int16&,int16)", ves_icall_System_Threading_Volatile_Write2)
ICALL(VOLATILE_19, "Write(intptr&,intptr)", ves_icall_System_Threading_Volatile_WriteIntPtr)
ICALL(VOLATILE_20, "Write(long&,long)", ves_icall_System_Threading_Volatile_Write8)
ICALL(VOLATILE_21, "Write(sbyte&,sbyte)", ves_icall_System_Threading_Volatile_Write1)
ICALL(VOLATILE_22, "Write(single&,single)", ves_icall_System_Threading_Volatile_WriteFloat)
ICALL(VOLATILE_23, "Write(uint&,uint)", ves_icall_System_Threading_Volatile_Write4)
ICALL(VOLATILE_24, "Write(uint16&,uint16)", ves_icall_System_Threading_Volatile_Write2)
ICALL(VOLATILE_25, "Write(uintptr&,uintptr)", ves_icall_System_Threading_Volatile_WriteIntPtr)
ICALL(VOLATILE_26, "Write(ulong&,ulong)", ves_icall_System_Threading_Volatile_Write8)

ICALL_TYPE(WAITH, "System.Threading.WaitHandle", WAITH_1)
HANDLES(ICALL(WAITH_1, "SignalAndWait_Internal", ves_icall_System_Threading_WaitHandle_SignalAndWait_Internal))
HANDLES(ICALL(WAITH_2, "Wait_internal", ves_icall_System_Threading_WaitHandle_Wait_internal))

ICALL_TYPE(TYPE, "System.Type", TYPE_1)
HANDLES(ICALL(TYPE_1, "internal_from_handle", ves_icall_System_Type_internal_from_handle))
HANDLES(ICALL(TYPE_2, "internal_from_name", ves_icall_System_Type_internal_from_name))

ICALL_TYPE(TYPEDR, "System.TypedReference", TYPEDR_1)
ICALL(TYPEDR_1, "InternalToObject",	mono_TypedReference_ToObject)
ICALL(TYPEDR_2, "MakeTypedReferenceInternal", mono_TypedReference_MakeTypedReferenceInternal)

ICALL_TYPE(VALUET, "System.ValueType", VALUET_1)
ICALL(VALUET_1, "InternalEquals", ves_icall_System_ValueType_Equals)
ICALL(VALUET_2, "InternalGetHashCode", ves_icall_System_ValueType_InternalGetHashCode)

ICALL_TYPE(WEBIC, "System.Web.Util.ICalls", WEBIC_1)
HANDLES(ICALL(WEBIC_1, "GetMachineConfigPath", ves_icall_System_Configuration_DefaultConfig_get_machine_config_path))
HANDLES(ICALL(WEBIC_2, "GetMachineInstallDirectory", ves_icall_System_Web_Util_ICalls_get_machine_install_dir))
HANDLES(ICALL(WEBIC_3, "GetUnmanagedResourcesPtr", ves_icall_get_resources_ptr))

#ifndef DISABLE_COM
ICALL_TYPE(COMOBJ, "System.__ComObject", COMOBJ_1)
ICALL(COMOBJ_1, "CreateRCW", ves_icall_System_ComObject_CreateRCW)
ICALL(COMOBJ_2, "GetInterfaceInternal", ves_icall_System_ComObject_GetInterfaceInternal)
ICALL(COMOBJ_3, "ReleaseInterfaces", ves_icall_System_ComObject_ReleaseInterfaces)
#endif
