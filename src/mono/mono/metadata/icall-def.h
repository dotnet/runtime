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

ICALL_TYPE(RTCLASS, "Mono.RuntimeClassHandle", RTCLASS_1)
NOHANDLES(ICALL(RTCLASS_1, "GetTypeFromClass", ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass))

ICALL_TYPE(RTPTRARRAY, "Mono.RuntimeGPtrArrayHandle", RTPTRARRAY_1)
NOHANDLES(ICALL(RTPTRARRAY_1, "GPtrArrayFree", ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree))

ICALL_TYPE(SAFESTRMARSHAL, "Mono.SafeStringMarshal", SAFESTRMARSHAL_1)
NOHANDLES(ICALL(SAFESTRMARSHAL_1, "GFree", ves_icall_Mono_SafeStringMarshal_GFree))
NOHANDLES(ICALL(SAFESTRMARSHAL_2, "StringToUtf8_icall", ves_icall_Mono_SafeStringMarshal_StringToUtf8))

ICALL_TYPE(ARGI, "System.ArgIterator", ARGI_1)
NOHANDLES(ICALL(ARGI_1, "IntGetNextArg",         ves_icall_System_ArgIterator_IntGetNextArg))
NOHANDLES(ICALL(ARGI_2, "IntGetNextArgType",     ves_icall_System_ArgIterator_IntGetNextArgType))
NOHANDLES(ICALL(ARGI_3, "IntGetNextArgWithType", ves_icall_System_ArgIterator_IntGetNextArgWithType))
NOHANDLES(ICALL(ARGI_4, "Setup",                 ves_icall_System_ArgIterator_Setup))

ICALL_TYPE(ARRAY, "System.Array", ARRAY_0)
NOHANDLES(ICALL(ARRAY_0, "CanChangePrimitive", ves_icall_System_Array_CanChangePrimitive))
NOHANDLES(ICALL(ARRAY_4, "FastCopy",         ves_icall_System_Array_FastCopy))
NOHANDLES(ICALL(ARRAY_4a, "GetCorElementTypeOfElementTypeInternal", ves_icall_System_Array_GetCorElementTypeOfElementTypeInternal))
NOHANDLES(ICALL(ARRAY_5, "GetGenericValue_icall", ves_icall_System_Array_GetGenericValue_icall))
HANDLES(ARRAY_6, "GetLengthInternal",        ves_icall_System_Array_GetLengthInternal, gint32, 2, (MonoObjectHandleOnStack, gint32))
HANDLES(ARRAY_7, "GetLowerBoundInternal",    ves_icall_System_Array_GetLowerBoundInternal, gint32, 2, (MonoObjectHandleOnStack, gint32))
HANDLES(ARRAY_10, "GetValueImpl",    ves_icall_System_Array_GetValueImpl, void, 3, (MonoObjectHandleOnStack, MonoObjectHandleOnStack, guint32))
HANDLES(ARRAY_10_a, "InitializeInternal",  ves_icall_System_Array_InitializeInternal, void, 1, (MonoObjectHandleOnStack))
NOHANDLES(ICALL(ARRAY_10a, "InternalCreate", ves_icall_System_Array_InternalCreate))
NOHANDLES(ICALL(ARRAY_10b, "IsValueOfElementTypeInternal", ves_icall_System_Array_IsValueOfElementTypeInternal))
NOHANDLES(ICALL(ARRAY_11, "SetGenericValue_icall", ves_icall_System_Array_SetGenericValue_icall))
HANDLES(ARRAY_13, "SetValueImpl",  ves_icall_System_Array_SetValueImpl, void, 3, (MonoObjectHandleOnStack, MonoObjectHandleOnStack, guint32))
HANDLES(ARRAY_14, "SetValueRelaxedImpl",  ves_icall_System_Array_SetValueRelaxedImpl, void, 3, (MonoObjectHandleOnStack, MonoObjectHandleOnStack, guint32))

ICALL_TYPE(BUFFER, "System.Buffer", BUFFER_0)
NOHANDLES(ICALL(BUFFER_0, "BulkMoveWithWriteBarrier", ves_icall_System_Buffer_BulkMoveWithWriteBarrier))
NOHANDLES(ICALL(BUFFER_2, "__Memmove", ves_icall_System_Runtime_RuntimeImports_Memmove))
NOHANDLES(ICALL(BUFFER_3, "__ZeroMemory", ves_icall_System_Runtime_RuntimeImports_ZeroMemory))

ICALL_TYPE(DELEGATE, "System.Delegate", DELEGATE_1)
HANDLES(DELEGATE_1, "AllocDelegateLike_internal", ves_icall_System_Delegate_AllocDelegateLike_internal, MonoMulticastDelegate, 1, (MonoDelegate))
HANDLES(DELEGATE_2, "CreateDelegate_internal", ves_icall_System_Delegate_CreateDelegate_internal, MonoObject, 4, (MonoQCallTypeHandle, MonoObject, MonoReflectionMethod, MonoBoolean))
HANDLES(DELEGATE_3, "GetVirtualMethod_internal", ves_icall_System_Delegate_GetVirtualMethod_internal, MonoReflectionMethod, 1, (MonoDelegate))

ICALL_TYPE(DEBUGR, "System.Diagnostics.Debugger", DEBUGR_1)
NOHANDLES(ICALL(DEBUGR_1, "IsAttached_internal", ves_icall_System_Diagnostics_Debugger_IsAttached_internal))
NOHANDLES(ICALL(DEBUGR_2, "IsLogging", ves_icall_System_Diagnostics_Debugger_IsLogging))
NOHANDLES(ICALL(DEBUGR_3, "Log_icall", ves_icall_System_Diagnostics_Debugger_Log))

ICALL_TYPE(STACKFRAME, "System.Diagnostics.StackFrame", STACKFRAME_1)
NOHANDLES(ICALL(STACKFRAME_1, "GetFrameInfo", ves_icall_System_Diagnostics_StackFrame_GetFrameInfo))

ICALL_TYPE(STACKTRACE, "System.Diagnostics.StackTrace", STACKTRACE_1)
NOHANDLES(ICALL(STACKTRACE_1, "GetTrace", ves_icall_System_Diagnostics_StackTrace_GetTrace))

ICALL_TYPE(EVENTPIPE, "System.Diagnostics.Tracing.EventPipeInternal", EVENTPIPE_1)
HANDLES(EVENTPIPE_1, "CreateProvider", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_CreateProvider, gconstpointer, 3, (MonoString, gpointer, gpointer))
NOHANDLES(ICALL(EVENTPIPE_2, "DefineEvent", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DefineEvent))
NOHANDLES(ICALL(EVENTPIPE_3, "DeleteProvider", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DeleteProvider))
NOHANDLES(ICALL(EVENTPIPE_4, "Disable", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Disable))
NOHANDLES(ICALL(EVENTPIPE_5, "Enable", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Enable))
NOHANDLES(ICALL(EVENTPIPE_6, "EventActivityIdControl", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_EventActivityIdControl))
NOHANDLES(ICALL(EVENTPIPE_7, "GetNextEvent", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetNextEvent))
NOHANDLES(ICALL(EVENTPIPE_8, "GetProvider", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetProvider))
NOHANDLES(ICALL(EVENTPIPE_9, "GetRuntimeCounterValue", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetRuntimeCounterValue))
NOHANDLES(ICALL(EVENTPIPE_10, "GetSessionInfo", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetSessionInfo))
NOHANDLES(ICALL(EVENTPIPE_12, "SignalSession", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_SignalSession))
NOHANDLES(ICALL(EVENTPIPE_14, "WaitForSessionSignal", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_WaitForSessionSignal))
NOHANDLES(ICALL(EVENTPIPE_13, "WriteEventData", ves_icall_System_Diagnostics_Tracing_EventPipeInternal_WriteEventData))

ICALL_TYPE(NATIVE_RUNTIME_EVENT_SOURCE, "System.Diagnostics.Tracing.NativeRuntimeEventSource", NATIVE_RUNTIME_EVENT_SOURCE_1)
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_1, "LogThreadPoolIODequeue", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolIODequeue))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_2, "LogThreadPoolIOEnqueue", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolIOEnqueue))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_10, "LogThreadPoolIOPack", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolIOPack))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_11, "LogThreadPoolMinMaxThreads", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolMinMaxThreads))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_3, "LogThreadPoolWorkerThreadAdjustmentAdjustment", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentAdjustment))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_4, "LogThreadPoolWorkerThreadAdjustmentSample", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentSample))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_5, "LogThreadPoolWorkerThreadAdjustmentStats", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadAdjustmentStats))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_6, "LogThreadPoolWorkerThreadStart", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadStart))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_7, "LogThreadPoolWorkerThreadStop", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadStop))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_8, "LogThreadPoolWorkerThreadWait", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkerThreadWait))
NOHANDLES(ICALL(NATIVE_RUNTIME_EVENT_SOURCE_9, "LogThreadPoolWorkingThreadCount", ves_icall_System_Diagnostics_Tracing_NativeRuntimeEventSource_LogThreadPoolWorkingThreadCount))

ICALL_TYPE(ENUM, "System.Enum", ENUM_1)
HANDLES(ENUM_1, "GetEnumValuesAndNames", ves_icall_System_Enum_GetEnumValuesAndNames, void, 3, (MonoQCallTypeHandle, MonoArrayOut, MonoArrayOut))
HANDLES(ENUM_2, "InternalBoxEnum", ves_icall_System_Enum_InternalBoxEnum, void, 3, (MonoQCallTypeHandle, MonoObjectHandleOnStack, guint64))
NOHANDLES(ICALL(ENUM_3, "InternalGetCorElementType", ves_icall_System_Enum_InternalGetCorElementType))
HANDLES(ENUM_4, "InternalGetUnderlyingType", ves_icall_System_Enum_InternalGetUnderlyingType, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))

ICALL_TYPE(ENV, "System.Environment", ENV_1)
NOHANDLES(ICALL(ENV_1, "Exit", ves_icall_System_Environment_Exit))
HANDLES(ENV_1a, "FailFast", ves_icall_System_Environment_FailFast, void, 3, (MonoString, MonoException, MonoString))
HANDLES(ENV_2, "GetCommandLineArgs", ves_icall_System_Environment_GetCommandLineArgs, MonoArray, 0, ())
NOHANDLES(ICALL(ENV_4, "GetProcessorCount", ves_icall_System_Environment_get_ProcessorCount))
NOHANDLES(ICALL(ENV_9, "get_ExitCode", mono_environment_exitcode_get))
NOHANDLES(ICALL(ENV_15, "get_TickCount", ves_icall_System_Environment_get_TickCount))
NOHANDLES(ICALL(ENV_15a, "get_TickCount64", ves_icall_System_Environment_get_TickCount64))
NOHANDLES(ICALL(ENV_20, "set_ExitCode", mono_environment_exitcode_set))

ICALL_TYPE(GC, "System.GC", GC_4a)
NOHANDLES(ICALL(GC_4a, "AddPressure", ves_icall_System_GC_AddPressure))
HANDLES(GC_13, "AllocPinnedArray", ves_icall_System_GC_AllocPinnedArray, MonoArray, 2, (MonoReflectionType, gint32))
NOHANDLES(ICALL(GC_10, "GetAllocatedBytesForCurrentThread", ves_icall_System_GC_GetAllocatedBytesForCurrentThread))
NOHANDLES(ICALL(GC_0, "GetCollectionCount", ves_icall_System_GC_GetCollectionCount))
HANDLES(GC_0a, "GetGeneration", ves_icall_System_GC_GetGeneration, int, 1, (MonoObject))
NOHANDLES(ICALL(GC_0b, "GetMaxGeneration", ves_icall_System_GC_GetMaxGeneration))
HANDLES(GC_11, "GetTotalAllocatedBytes", ves_icall_System_GC_GetTotalAllocatedBytes, guint64, 1, (MonoBoolean))
NOHANDLES(ICALL(GC_1, "GetTotalMemory", ves_icall_System_GC_GetTotalMemory))
NOHANDLES(ICALL(GC_2, "InternalCollect", ves_icall_System_GC_InternalCollect))
NOHANDLES(ICALL(GC_5, "RemovePressure", ves_icall_System_GC_RemovePressure))
NOHANDLES(ICALL(GC_6, "WaitForPendingFinalizers", ves_icall_System_GC_WaitForPendingFinalizers))
NOHANDLES(ICALL(GC_12, "_GetGCMemoryInfo", ves_icall_System_GC_GetGCMemoryInfo))
HANDLES(GC_6b, "_ReRegisterForFinalize", ves_icall_System_GC_ReRegisterForFinalize, void, 1, (MonoObject))
HANDLES(GC_7, "_SuppressFinalize", ves_icall_System_GC_SuppressFinalize, void, 1, (MonoObject))
HANDLES(GC_9, "get_ephemeron_tombstone", ves_icall_System_GC_get_ephemeron_tombstone, MonoObject, 0, ())
HANDLES(GC_8, "register_ephemeron_array", ves_icall_System_GC_register_ephemeron_array, void, 1, (MonoObject))

ICALL_TYPE(STREAM, "System.IO.Stream", STREAM_1)
HANDLES(STREAM_1, "HasOverriddenBeginEndRead", ves_icall_System_IO_Stream_HasOverriddenBeginEndRead, MonoBoolean, 1, (MonoObject))
HANDLES(STREAM_2, "HasOverriddenBeginEndWrite", ves_icall_System_IO_Stream_HasOverriddenBeginEndWrite, MonoBoolean, 1, (MonoObject))

ICALL_TYPE(MATH, "System.Math", MATH_1)
NOHANDLES_FLAGS(ICALL(MATH_1, "Acos", ves_icall_System_Math_Acos), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_1a, "Acosh", ves_icall_System_Math_Acosh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_2, "Asin", ves_icall_System_Math_Asin), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_2a, "Asinh", ves_icall_System_Math_Asinh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_3, "Atan", ves_icall_System_Math_Atan), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_4, "Atan2", ves_icall_System_Math_Atan2), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_4a, "Atanh", ves_icall_System_Math_Atanh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_4b, "Cbrt", ves_icall_System_Math_Cbrt), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_21, "Ceiling", ves_icall_System_Math_Ceiling), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_5, "Cos", ves_icall_System_Math_Cos), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_6, "Cosh", ves_icall_System_Math_Cosh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_7, "Exp", ves_icall_System_Math_Exp), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_7a, "FMod", ves_icall_System_Math_FMod), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_8, "Floor", ves_icall_System_Math_Floor), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_22, "FusedMultiplyAdd", ves_icall_System_Math_FusedMultiplyAdd), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_9, "Log", ves_icall_System_Math_Log), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_10, "Log10", ves_icall_System_Math_Log10), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_24, "Log2", ves_icall_System_Math_Log2), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_10a, "ModF", ves_icall_System_Math_ModF), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_11, "Pow", ves_icall_System_Math_Pow), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_12, "Round", ves_icall_System_Math_Round), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_14, "Sin", ves_icall_System_Math_Sin), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_15, "Sinh", ves_icall_System_Math_Sinh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_16, "Sqrt", ves_icall_System_Math_Sqrt), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_17, "Tan", ves_icall_System_Math_Tan), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATH_18, "Tanh", ves_icall_System_Math_Tanh), MONO_ICALL_FLAGS_NO_EXCEPTION)

ICALL_TYPE(MATHF, "System.MathF", MATHF_1)
NOHANDLES_FLAGS(ICALL(MATHF_1, "Acos", ves_icall_System_MathF_Acos), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_2, "Acosh", ves_icall_System_MathF_Acosh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_3, "Asin", ves_icall_System_MathF_Asin), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_4, "Asinh", ves_icall_System_MathF_Asinh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_5, "Atan", ves_icall_System_MathF_Atan), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_6, "Atan2", ves_icall_System_MathF_Atan2), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_7, "Atanh", ves_icall_System_MathF_Atanh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_8, "Cbrt", ves_icall_System_MathF_Cbrt), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_9, "Ceiling", ves_icall_System_MathF_Ceiling), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_10, "Cos", ves_icall_System_MathF_Cos), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_11, "Cosh", ves_icall_System_MathF_Cosh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_12, "Exp", ves_icall_System_MathF_Exp), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_22, "FMod", ves_icall_System_MathF_FMod), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_13, "Floor", ves_icall_System_MathF_Floor), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_24, "FusedMultiplyAdd", ves_icall_System_MathF_FusedMultiplyAdd), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_14, "Log", ves_icall_System_MathF_Log), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_15, "Log10", ves_icall_System_MathF_Log10), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_26, "Log2", ves_icall_System_MathF_Log2), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_23, "ModF(single,single*)", ves_icall_System_MathF_ModF), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_16, "Pow", ves_icall_System_MathF_Pow), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_17, "Sin", ves_icall_System_MathF_Sin), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_18, "Sinh", ves_icall_System_MathF_Sinh), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_19, "Sqrt", ves_icall_System_MathF_Sqrt), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_20, "Tan", ves_icall_System_MathF_Tan), MONO_ICALL_FLAGS_NO_EXCEPTION)
NOHANDLES_FLAGS(ICALL(MATHF_21, "Tanh", ves_icall_System_MathF_Tanh), MONO_ICALL_FLAGS_NO_EXCEPTION)

ICALL_TYPE(OBJ, "System.Object", OBJ_3)
HANDLES(OBJ_3, "MemberwiseClone", ves_icall_System_Object_MemberwiseClone, MonoObject, 1, (MonoObject))

ICALL_TYPE(ASSEM, "System.Reflection.Assembly", ASSEM_2)
HANDLES(ASSEM_2, "GetCallingAssembly", ves_icall_System_Reflection_Assembly_GetCallingAssembly, MonoReflectionAssembly, 0, ())
HANDLES(ASSEM_3, "GetEntryAssemblyNative", ves_icall_System_Reflection_Assembly_GetEntryAssembly, MonoReflectionAssembly, 0, ())
HANDLES(ASSEM_4, "GetExecutingAssembly", ves_icall_System_Reflection_Assembly_GetExecutingAssembly, MonoReflectionAssembly, 1, (MonoStackCrawlMark_ptr))
HANDLES(ASSEM_6, "InternalGetType", ves_icall_System_Reflection_Assembly_InternalGetType, MonoReflectionType, 5, (MonoReflectionAssembly, MonoReflectionModule, MonoString, MonoBoolean, MonoBoolean))
HANDLES(ASSEM_7, "InternalLoad", ves_icall_System_Reflection_Assembly_InternalLoad, MonoReflectionAssembly, 3, (MonoString, MonoStackCrawlMark_ptr, gpointer))

ICALL_TYPE(ASSEMN, "System.Reflection.AssemblyName", ASSEMN_1)
NOHANDLES(ICALL(ASSEMN_1, "FreeAssemblyName", ves_icall_System_Reflection_AssemblyName_FreeAssemblyName))
NOHANDLES(ICALL(ASSEMN_2, "GetNativeName", ves_icall_System_Reflection_AssemblyName_GetNativeName))

ICALL_TYPE(MCATTR, "System.Reflection.CustomAttribute", MCATTR_1)
HANDLES(MCATTR_1, "GetCustomAttributesDataInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal, MonoArray, 1, (MonoObject))
HANDLES(MCATTR_2, "GetCustomAttributesInternal", ves_icall_MonoCustomAttrs_GetCustomAttributesInternal, MonoArray, 3, (MonoObject, MonoReflectionType, MonoBoolean))
HANDLES(MCATTR_3, "IsDefinedInternal", ves_icall_MonoCustomAttrs_IsDefinedInternal, MonoBoolean, 2, (MonoObject, MonoReflectionType))

#ifndef DISABLE_REFLECTION_EMIT
ICALL_TYPE(CATTRB, "System.Reflection.Emit.CustomAttributeBuilder", CATTRB_1)
HANDLES(CATTRB_1, "GetBlob", ves_icall_CustomAttributeBuilder_GetBlob, MonoArray, 7, (MonoReflectionAssembly, MonoObject, MonoArray, MonoArray, MonoArray, MonoArray, MonoArray))
#endif

ICALL_TYPE(DYNM, "System.Reflection.Emit.DynamicMethod", DYNM_1)
HANDLES(DYNM_1, "create_dynamic_method", ves_icall_DynamicMethod_create_dynamic_method, void, 4, (MonoReflectionDynamicMethod, MonoString, guint32, guint32))

ICALL_TYPE(ASSEMB, "System.Reflection.Emit.RuntimeAssemblyBuilder", ASSEMB_1)
HANDLES(ASSEMB_1, "UpdateNativeCustomAttributes", ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes, void, 1, (MonoReflectionAssemblyBuilder))
HANDLES(ASSEMB_2, "basic_init", ves_icall_AssemblyBuilder_basic_init, void, 1, (MonoReflectionAssemblyBuilder))

ICALL_TYPE(ENUMB, "System.Reflection.Emit.RuntimeEnumBuilder", ENUMB_1)
HANDLES(ENUMB_1, "setup_enum_type", ves_icall_EnumBuilder_setup_enum_type, void, 2, (MonoReflectionType, MonoReflectionType))

ICALL_TYPE(MODULEB, "System.Reflection.Emit.RuntimeModuleBuilder", MODULEB_10)
HANDLES(MODULEB_10, "GetRegisteredToken", ves_icall_ModuleBuilder_GetRegisteredToken, MonoObject, 2, (MonoReflectionModuleBuilder, guint32))
HANDLES(MODULEB_8, "RegisterToken", ves_icall_ModuleBuilder_RegisterToken, void, 3, (MonoReflectionModuleBuilder, MonoObject, guint32))
HANDLES(MODULEB_2, "basic_init", ves_icall_ModuleBuilder_basic_init, void, 1, (MonoReflectionModuleBuilder))
HANDLES(MODULEB_5, "getMethodToken", ves_icall_ModuleBuilder_getMethodToken, gint32, 3, (MonoReflectionModuleBuilder, MonoReflectionMethod, MonoArray))
HANDLES(MODULEB_6, "getToken", ves_icall_ModuleBuilder_getToken, gint32, 3, (MonoReflectionModuleBuilder, MonoObject, MonoBoolean))
HANDLES(MODULEB_7, "getUSIndex", ves_icall_ModuleBuilder_getUSIndex, guint32, 2, (MonoReflectionModuleBuilder, MonoString))
HANDLES(MODULEB_9, "set_wrappers_type", ves_icall_ModuleBuilder_set_wrappers_type, void, 2, (MonoReflectionModuleBuilder, MonoReflectionType))

ICALL_TYPE(TYPEB, "System.Reflection.Emit.RuntimeTypeBuilder", TYPEB_1)
HANDLES(TYPEB_1, "create_runtime_class", ves_icall_TypeBuilder_create_runtime_class, MonoReflectionType, 1, (MonoReflectionTypeBuilder))

ICALL_TYPE(SIGH, "System.Reflection.Emit.SignatureHelper", SIGH_1)
HANDLES(SIGH_1, "get_signature_field", ves_icall_SignatureHelper_get_signature_field, MonoArray, 1, (MonoReflectionSigHelper))
HANDLES(SIGH_2, "get_signature_local", ves_icall_SignatureHelper_get_signature_local, MonoArray, 1, (MonoReflectionSigHelper))

ICALL_TYPE(FIELDI, "System.Reflection.FieldInfo", FILEDI_1)
HANDLES(FILEDI_1, "get_marshal_info", ves_icall_System_Reflection_FieldInfo_get_marshal_info, MonoReflectionMarshalAsAttribute, 1, (MonoReflectionField))
HANDLES(FILEDI_2, "internal_from_handle_type", ves_icall_System_Reflection_FieldInfo_internal_from_handle_type, MonoReflectionField, 2, (MonoClassField_ref, MonoType_ref))

ICALL_TYPE(LOADER_ALLOC, "System.Reflection.LoaderAllocatorScout", LOADER_ALLOC_1)
NOHANDLES(ICALL(LOADER_ALLOC_1, "Destroy", ves_icall_System_Reflection_LoaderAllocatorScout_Destroy))

ICALL_TYPE(MDUP, "System.Reflection.Metadata.MetadataUpdater", MDUP_1)
NOHANDLES(ICALL(MDUP_1, "ApplyUpdateEnabled", ves_icall_AssemblyExtensions_ApplyUpdateEnabled))
NOHANDLES(ICALL(MDUP_2, "ApplyUpdate_internal", ves_icall_AssemblyExtensions_ApplyUpdate))
HANDLES(MDUP_3, "GetApplyUpdateCapabilities", ves_icall_AssemblyExtensions_GetApplyUpdateCapabilities, MonoString, 0, ())

ICALL_TYPE(MBASE, "System.Reflection.MethodBase", MBASE_1)
HANDLES(MBASE_1, "GetCurrentMethod", ves_icall_GetCurrentMethod, MonoReflectionMethod, 0, ())

ICALL_TYPE(MMETHI, "System.Reflection.MonoMethodInfo", MMETHI_4)
NOHANDLES(ICALL(MMETHI_4, "get_method_attributes", ves_icall_get_method_attributes))
HANDLES(MMETHI_1, "get_method_info", ves_icall_get_method_info, void, 2, (MonoMethod_ptr, MonoMethodInfo_ref))
HANDLES(MMETHI_2, "get_parameter_info", ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info, MonoArray, 2, (MonoMethod_ptr, MonoReflectionMethod))
HANDLES(MMETHI_3, "get_retval_marshal", ves_icall_System_MonoMethodInfo_get_retval_marshal, MonoReflectionMarshalAsAttribute, 1, (MonoMethod_ptr))

ICALL_TYPE(RASSEM, "System.Reflection.RuntimeAssembly", RASSEM_1)
HANDLES(RASSEM_1, "GetEntryPoint", ves_icall_System_Reflection_RuntimeAssembly_GetEntryPoint, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_9, "GetExportedTypes", ves_icall_System_Reflection_RuntimeAssembly_GetExportedTypes, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_13, "GetInfo", ves_icall_System_Reflection_RuntimeAssembly_GetInfo, void, 3, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack, guint32))
HANDLES(RASSEM_2, "GetManifestModuleInternal", ves_icall_System_Reflection_Assembly_GetManifestModuleInternal, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_3, "GetManifestResourceInfoInternal", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInfoInternal, MonoBoolean, 3, (MonoQCallAssemblyHandle, MonoString, MonoManifestResourceInfo))
HANDLES(RASSEM_4, "GetManifestResourceInternal", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceInternal, gpointer, 4, (MonoQCallAssemblyHandle, MonoString, gint32_ref, MonoObjectHandleOnStack))
HANDLES(RASSEM_5, "GetManifestResourceNames", ves_icall_System_Reflection_RuntimeAssembly_GetManifestResourceNames, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_6, "GetModulesInternal", ves_icall_System_Reflection_RuntimeAssembly_GetModulesInternal, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_6b, "GetTopLevelForwardedTypes", ves_icall_System_Reflection_RuntimeAssembly_GetTopLevelForwardedTypes, void, 2, (MonoQCallAssemblyHandle, MonoObjectHandleOnStack))
HANDLES(RASSEM_7, "InternalGetReferencedAssemblies", ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies, GPtrArray_ptr, 1, (MonoReflectionAssembly))

ICALL_TYPE(MCMETH, "System.Reflection.RuntimeConstructorInfo", MCMETH_1)
HANDLES(MCMETH_1, "GetGenericMethodDefinition_impl", ves_icall_RuntimeMethodInfo_GetGenericMethodDefinition, MonoReflectionMethod, 1, (MonoReflectionMethod))
HANDLES(MCMETH_2, "InternalInvoke", ves_icall_InternalInvoke, MonoObject, 4, (MonoReflectionMethod, MonoObject, gpointer_ref, MonoExceptionOut))
HANDLES(MCMETH_5, "InvokeClassConstructor", ves_icall_InvokeClassConstructor, void, 1, (MonoQCallTypeHandle))
HANDLES_REUSE_WRAPPER(MCMETH_4, "get_metadata_token", ves_icall_reflection_get_token)

ICALL_TYPE(CATTR_DATA, "System.Reflection.RuntimeCustomAttributeData", CATTR_DATA_1)
HANDLES(CATTR_DATA_1, "ResolveArgumentsInternal", ves_icall_System_Reflection_RuntimeCustomAttributeData_ResolveArgumentsInternal, void, 6, (MonoReflectionMethod, MonoReflectionAssembly, gpointer, guint32, MonoArrayOut, MonoArrayOut))

ICALL_TYPE(MEV, "System.Reflection.RuntimeEventInfo", MEV_1)
HANDLES(MEV_1, "get_event_info", ves_icall_RuntimeEventInfo_get_event_info, void, 2, (MonoReflectionMonoEvent, MonoEventInfo_ref))
HANDLES_REUSE_WRAPPER(MEV_2, "get_metadata_token", ves_icall_reflection_get_token)
HANDLES(MEV_3, "internal_from_handle_type", ves_icall_System_Reflection_EventInfo_internal_from_handle_type, MonoReflectionEvent, 2, (MonoEvent_ref, MonoType_ref))

ICALL_TYPE(MFIELD, "System.Reflection.RuntimeFieldInfo", MFIELD_1)
HANDLES(MFIELD_1, "GetFieldOffset", ves_icall_RuntimeFieldInfo_GetFieldOffset, gint32, 1, (MonoReflectionField))
HANDLES(MFIELD_2, "GetParentType", ves_icall_RuntimeFieldInfo_GetParentType, MonoReflectionType, 2, (MonoReflectionField, MonoBoolean))
HANDLES(MFIELD_3, "GetRawConstantValue", ves_icall_RuntimeFieldInfo_GetRawConstantValue, MonoObject, 1, (MonoReflectionField))
HANDLES(MFIELD_4, "GetTypeModifiers", ves_icall_System_Reflection_FieldInfo_GetTypeModifiers, MonoArray, 2, (MonoReflectionField, MonoBoolean))
HANDLES(MFIELD_5, "GetValueInternal", ves_icall_RuntimeFieldInfo_GetValueInternal, MonoObject, 2, (MonoReflectionField, MonoObject))
HANDLES(MFIELD_6, "ResolveType", ves_icall_RuntimeFieldInfo_ResolveType, MonoReflectionType, 1, (MonoReflectionField))
HANDLES(MFIELD_7, "SetValueInternal", ves_icall_RuntimeFieldInfo_SetValueInternal, void, 3, (MonoReflectionField, MonoObject, MonoObject))
HANDLES_REUSE_WRAPPER(MFIELD_8, "UnsafeGetValue", ves_icall_RuntimeFieldInfo_GetValueInternal)
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
HANDLES_REUSE_WRAPPER(RMETHODINFO_12, "get_metadata_token", ves_icall_reflection_get_token)
HANDLES(RMETHODINFO_13, "get_name", ves_icall_RuntimeMethodInfo_get_name, MonoString, 1, (MonoReflectionMethod))

ICALL_TYPE(MODULE, "System.Reflection.RuntimeModule", MODULE_2)
HANDLES(MODULE_2, "GetGlobalType", ves_icall_System_Reflection_RuntimeModule_GetGlobalType, MonoReflectionType, 1, (MonoImage_ptr))
HANDLES(MODULE_3, "GetGuidInternal", ves_icall_System_Reflection_RuntimeModule_GetGuidInternal, void, 2, (MonoImage_ptr, MonoArray))
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
HANDLES(RUNH_6, "GetSpanDataFrom", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetSpanDataFrom, gpointer, 3, (MonoClassField_ptr, MonoType_ptr, gpointer))
HANDLES(RUNH_2, "GetUninitializedObjectInternal", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetUninitializedObjectInternal, MonoObject, 1, (MonoType_ptr))
HANDLES(RUNH_3, "InitializeArray", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray, void, 2, (MonoArray, MonoClassField_ptr))
HANDLES(RUNH_7, "InternalGetHashCode", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalGetHashCode, int, 1, (MonoObject))
HANDLES(RUNH_8, "InternalTryGetHashCode", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InternalTryGetHashCode, int, 1, (MonoObject))
HANDLES(RUNH_3a, "PrepareMethod", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_PrepareMethod, void, 3, (MonoMethod_ptr, gpointer, int))
HANDLES(RUNH_4, "RunClassConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor, void, 1, (MonoType_ptr))
HANDLES(RUNH_5, "RunModuleConstructor", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor, void, 1, (MonoImage_ptr))
NOHANDLES(ICALL(RUNH_5h, "SufficientExecutionStack", ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack))

ICALL_TYPE(GCH, "System.Runtime.InteropServices.GCHandle", GCH_1)
HANDLES(GCH_1, "InternalAlloc", ves_icall_System_GCHandle_InternalAlloc, gpointer, 2, (MonoObject, gint32))
HANDLES(GCH_2, "InternalFree", ves_icall_System_GCHandle_InternalFree, void, 1, (gpointer))
HANDLES(GCH_3, "InternalGet", ves_icall_System_GCHandle_InternalGet, MonoObject, 1, (gpointer))
HANDLES(GCH_4, "InternalSet", ves_icall_System_GCHandle_InternalSet, void, 2, (gpointer, MonoObject))

ICALL_TYPE(MARSHAL, "System.Runtime.InteropServices.Marshal", MARSHAL_4)
HANDLES(MARSHAL_4, "DestroyStructure", ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure, void, 2, (gpointer, MonoReflectionType))
HANDLES(MARSHAL_9, "GetDelegateForFunctionPointerInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal, void, 3, (MonoQCallTypeHandle, gpointer, MonoObjectHandleOnStack))
HANDLES(MARSHAL_10, "GetFunctionPointerForDelegateInternal", ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal, gpointer, 1, (MonoDelegate))
NOHANDLES(ICALL(MARSHAL_11, "GetLastPInvokeError", ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError))
HANDLES(MARSHAL_12, "OffsetOf", ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf, int, 2, (MonoReflectionType, MonoString))
HANDLES(MARSHAL_13, "PrelinkInternal", ves_icall_System_Runtime_InteropServices_Marshal_Prelink, void, 1, (MonoReflectionMethod))
HANDLES(MARSHAL_20, "PtrToStructureInternal", ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructureInternal, void, 3, (gconstpointer, MonoObject, MonoBoolean))
NOHANDLES(ICALL(MARSHAL_29a, "SetLastPInvokeError", ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError))
HANDLES(MARSHAL_31, "SizeOfHelper", ves_icall_System_Runtime_InteropServices_Marshal_SizeOfHelper, guint32, 2, (MonoQCallTypeHandle, MonoBoolean))
HANDLES(MARSHAL_34, "StructureToPtr", ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr, void, 3, (MonoObject, gpointer, MonoBoolean))

ICALL_TYPE(NATIVEL, "System.Runtime.InteropServices.NativeLibrary", NATIVEL_1)
HANDLES(NATIVEL_1, "FreeLib", ves_icall_System_Runtime_InteropServices_NativeLibrary_FreeLib, void, 1, (gpointer))
HANDLES(NATIVEL_2, "GetSymbol", ves_icall_System_Runtime_InteropServices_NativeLibrary_GetSymbol, gpointer, 3, (gpointer, MonoString, MonoBoolean))
HANDLES(NATIVEL_3, "LoadByName", ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName, gpointer, 5, (MonoString, MonoReflectionAssembly, MonoBoolean, guint32, MonoBoolean))
HANDLES(NATIVEL_4, "LoadFromPath", ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadFromPath, gpointer, 2, (MonoString, MonoBoolean))

#if defined(TARGET_AMD64)
ICALL_TYPE(X86BASE, "System.Runtime.Intrinsics.X86.X86Base", X86BASE_1)
NOHANDLES(ICALL(X86BASE_1, "__cpuidex", ves_icall_System_Runtime_Intrinsics_X86_X86Base___cpuidex))
#endif

ICALL_TYPE(ALC, "System.Runtime.Loader.AssemblyLoadContext", ALC_5)
HANDLES(ALC_5, "GetLoadContextForAssembly", ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly, gpointer, 1, (MonoReflectionAssembly))
HANDLES(ALC_4, "InternalGetLoadedAssemblies", ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies, MonoArray, 0, ())
HANDLES(ALC_2, "InternalInitializeNativeALC", ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC, gpointer, 4, (gpointer, const_char_ptr, MonoBoolean, MonoBoolean))
HANDLES(ALC_1, "InternalLoadFile", ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile, MonoReflectionAssembly, 3, (gpointer, MonoString, MonoStackCrawlMark_ptr))
HANDLES(ALC_3, "InternalLoadFromStream", ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream, MonoReflectionAssembly, 5, (gpointer, gpointer, gint32, gpointer, gint32))
HANDLES(ALC_6, "PrepareForAssemblyLoadContextRelease", ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease, void, 2, (gpointer, gpointer))

ICALL_TYPE(RFH, "System.RuntimeFieldHandle", RFH_1)
HANDLES(RFH_1, "GetValueDirect", ves_icall_System_RuntimeFieldHandle_GetValueDirect, MonoObject, 4, (MonoReflectionField, MonoReflectionType, MonoTypedRef_ptr, MonoReflectionType))
HANDLES(RFH_1a, "SetValueDirect", ves_icall_System_RuntimeFieldHandle_SetValueDirect, void, 5, (MonoReflectionField, MonoReflectionType, MonoTypedRef_ptr, MonoObject, MonoReflectionType))
HANDLES_REUSE_WRAPPER(RFH_2, "SetValueInternal", ves_icall_RuntimeFieldInfo_SetValueInternal)

ICALL_TYPE(MHAN, "System.RuntimeMethodHandle", MHAN_1)
HANDLES(MHAN_1, "GetFunctionPointer", ves_icall_RuntimeMethodHandle_GetFunctionPointer, gpointer, 1, (MonoMethod_ptr))
HANDLES(MAHN_3, "ReboxFromNullable", ves_icall_RuntimeMethodHandle_ReboxFromNullable, void, 2, (MonoObject, MonoObjectHandleOnStack))
HANDLES(MAHN_2, "ReboxToNullable", ves_icall_RuntimeMethodHandle_ReboxToNullable, void, 3, (MonoObject, MonoQCallTypeHandle, MonoObjectHandleOnStack))

ICALL_TYPE(RT, "System.RuntimeType", RT_31)
HANDLES(RT_31, "AllocateValueType", ves_icall_System_RuntimeType_AllocateValueType, void, 3, (MonoQCallTypeHandle, MonoObject, MonoObjectHandleOnStack))
HANDLES(RT_1, "CreateInstanceInternal", ves_icall_System_RuntimeType_CreateInstanceInternal, MonoObject, 1, (MonoQCallTypeHandle))
HANDLES(RT_2, "GetConstructors_native", ves_icall_RuntimeType_GetConstructors_native, GPtrArray_ptr, 2, (MonoQCallTypeHandle, guint32))
HANDLES(RT_30, "GetCorrespondingInflatedMethod", ves_icall_RuntimeType_GetCorrespondingInflatedMethod, MonoReflectionMethod, 2, (MonoQCallTypeHandle, MonoReflectionMethod))
HANDLES(RT_21, "GetDeclaringMethod", ves_icall_RuntimeType_GetDeclaringMethod, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_22, "GetDeclaringType", ves_icall_RuntimeType_GetDeclaringType, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_3, "GetEvents_native", ves_icall_RuntimeType_GetEvents_native, GPtrArray_ptr, 3, (MonoQCallTypeHandle, char_ptr, guint32))
HANDLES(RT_5, "GetFields_native", ves_icall_RuntimeType_GetFields_native, GPtrArray_ptr, 4, (MonoQCallTypeHandle, char_ptr, guint32, guint32))
HANDLES(RT_6, "GetGenericArgumentsInternal", ves_icall_RuntimeType_GetGenericArgumentsInternal, void, 3, (MonoQCallTypeHandle, MonoObjectHandleOnStack, MonoBoolean))
NOHANDLES(ICALL(RT_9, "GetGenericParameterPosition", ves_icall_RuntimeType_GetGenericParameterPosition))
HANDLES(RT_10, "GetInterfaceMapData", ves_icall_RuntimeType_GetInterfaceMapData, void, 4, (MonoQCallTypeHandle, MonoQCallTypeHandle, MonoArrayOut, MonoArrayOut))
HANDLES(RT_11, "GetInterfaces", ves_icall_RuntimeType_GetInterfaces, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_12, "GetMethodsByName_native", ves_icall_RuntimeType_GetMethodsByName_native, GPtrArray_ptr, 4, (MonoQCallTypeHandle, const_char_ptr, guint32, guint32))
HANDLES(RT_23, "GetName", ves_icall_RuntimeType_GetName, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_24, "GetNamespace", ves_icall_RuntimeType_GetNamespace, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_13, "GetNestedTypes_native", ves_icall_RuntimeType_GetNestedTypes_native, GPtrArray_ptr, 4, (MonoQCallTypeHandle, char_ptr, guint32, guint32))
HANDLES(RT_14, "GetPacking", ves_icall_RuntimeType_GetPacking, void, 3, (MonoQCallTypeHandle, guint32_ref, guint32_ref))
HANDLES(RT_15, "GetPropertiesByName_native", ves_icall_RuntimeType_GetPropertiesByName_native, GPtrArray_ptr, 4, (MonoQCallTypeHandle, char_ptr, guint32, guint32))
HANDLES(RT_17, "MakeGenericType", ves_icall_RuntimeType_MakeGenericType, void, 3, (MonoReflectionType, MonoArray, MonoObjectHandleOnStack))
HANDLES(RT_19, "getFullName", ves_icall_System_RuntimeType_getFullName, void, 4, (MonoQCallTypeHandle, MonoObjectHandleOnStack, MonoBoolean, MonoBoolean))
HANDLES(RT_26, "make_array_type", ves_icall_RuntimeType_make_array_type, void, 3, (MonoQCallTypeHandle, int, MonoObjectHandleOnStack))
HANDLES(RT_27, "make_byref_type", ves_icall_RuntimeType_make_byref_type, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RT_18, "make_pointer_type", ves_icall_RuntimeType_make_pointer_type, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))

ICALL_TYPE(RTH, "System.RuntimeTypeHandle", RTH_1)
HANDLES(RTH_1, "GetArrayRank", ves_icall_RuntimeTypeHandle_GetArrayRank, gint32, 1, (MonoQCallTypeHandle))
HANDLES(RTH_2, "GetAssembly", ves_icall_RuntimeTypeHandle_GetAssembly, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
NOHANDLES(ICALL(RTH_3, "GetAttributes", ves_icall_RuntimeTypeHandle_GetAttributes))
HANDLES(RTH_4, "GetBaseType", ves_icall_RuntimeTypeHandle_GetBaseType, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
NOHANDLES(ICALL(RTH_4a, "GetCorElementType", ves_icall_RuntimeTypeHandle_GetCorElementType))
HANDLES(RTH_5, "GetElementType", ves_icall_RuntimeTypeHandle_GetElementType, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RTH_19, "GetGenericParameterInfo", ves_icall_RuntimeTypeHandle_GetGenericParameterInfo, MonoGenericParamInfo_ptr, 1, (MonoQCallTypeHandle))
HANDLES(RTH_6, "GetGenericTypeDefinition_impl", ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
HANDLES(RTH_7, "GetMetadataToken", ves_icall_RuntimeTypeHandle_GetMetadataToken, guint32, 1, (MonoQCallTypeHandle))
HANDLES(RTH_8, "GetModule", ves_icall_RuntimeTypeHandle_GetModule, void, 2, (MonoQCallTypeHandle, MonoObjectHandleOnStack))
NOHANDLES(ICALL(RTH_9, "HasInstantiation", ves_icall_RuntimeTypeHandle_HasInstantiation))
HANDLES(RTH_20, "HasReferences", ves_icall_RuntimeTypeHandle_HasReferences, MonoBoolean, 1, (MonoQCallTypeHandle))
HANDLES(RTH_21, "IsByRefLike", ves_icall_RuntimeTypeHandle_IsByRefLike, MonoBoolean, 1, (MonoQCallTypeHandle))
HANDLES(RTH_12, "IsComObject", ves_icall_RuntimeTypeHandle_IsComObject, MonoBoolean, 1, (MonoQCallTypeHandle))
NOHANDLES(ICALL(RTH_13, "IsGenericTypeDefinition", ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition))
HANDLES(RTH_15, "IsInstanceOfType", ves_icall_RuntimeTypeHandle_IsInstanceOfType, guint32, 2, (MonoQCallTypeHandle, MonoObject))
HANDLES(RTH_17a, "internal_from_name", ves_icall_System_RuntimeTypeHandle_internal_from_name, void, 5, (char_ptr, MonoStackCrawlMark_ptr, MonoObjectHandleOnStack, MonoBoolean, MonoBoolean))
HANDLES(RTH_17b, "is_subclass_of", ves_icall_RuntimeTypeHandle_is_subclass_of, MonoBoolean, 2, (MonoQCallTypeHandle, MonoQCallTypeHandle))
HANDLES(RTH_18, "type_is_assignable_from", ves_icall_RuntimeTypeHandle_type_is_assignable_from, MonoBoolean, 2, (MonoQCallTypeHandle, MonoQCallTypeHandle))

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

ICALL_TYPE(ILOCK, "System.Threading.Interlocked", ILOCK_1)
NOHANDLES(ICALL(ILOCK_1, "Add(int&,int)", ves_icall_System_Threading_Interlocked_Add_Int))
NOHANDLES(ICALL(ILOCK_2, "Add(long&,long)", ves_icall_System_Threading_Interlocked_Add_Long))
NOHANDLES(ICALL(ILOCK_4, "CompareExchange(double&,double,double)", ves_icall_System_Threading_Interlocked_CompareExchange_Double))
NOHANDLES(ICALL(ILOCK_5, "CompareExchange(int&,int,int)", ves_icall_System_Threading_Interlocked_CompareExchange_Int))
NOHANDLES(ICALL(ILOCK_6, "CompareExchange(int&,int,int,bool&)", ves_icall_System_Threading_Interlocked_CompareExchange_Int_Success))
NOHANDLES(ICALL(ILOCK_8, "CompareExchange(long&,long,long)", ves_icall_System_Threading_Interlocked_CompareExchange_Long))
NOHANDLES(ICALL(ILOCK_9, "CompareExchange(object&,object&,object&,object&)", ves_icall_System_Threading_Interlocked_CompareExchange_Object))
NOHANDLES(ICALL(ILOCK_10, "CompareExchange(single&,single,single)", ves_icall_System_Threading_Interlocked_CompareExchange_Single))
NOHANDLES(ICALL(ILOCK_11, "Decrement(int&)", ves_icall_System_Threading_Interlocked_Decrement_Int))
NOHANDLES(ICALL(ILOCK_12, "Decrement(long&)", ves_icall_System_Threading_Interlocked_Decrement_Long))
NOHANDLES(ICALL(ILOCK_14, "Exchange(double&,double)", ves_icall_System_Threading_Interlocked_Exchange_Double))
NOHANDLES(ICALL(ILOCK_15, "Exchange(int&,int)", ves_icall_System_Threading_Interlocked_Exchange_Int))
NOHANDLES(ICALL(ILOCK_17, "Exchange(long&,long)", ves_icall_System_Threading_Interlocked_Exchange_Long))
NOHANDLES(ICALL(ILOCK_18, "Exchange(object&,object&,object&)", ves_icall_System_Threading_Interlocked_Exchange_Object))
NOHANDLES(ICALL(ILOCK_19, "Exchange(single&,single)", ves_icall_System_Threading_Interlocked_Exchange_Single))
NOHANDLES(ICALL(ILOCK_20, "Increment(int&)", ves_icall_System_Threading_Interlocked_Increment_Int))
NOHANDLES(ICALL(ILOCK_21, "Increment(long&)", ves_icall_System_Threading_Interlocked_Increment_Long))
NOHANDLES(ICALL(ILOCK_22, "MemoryBarrierProcessWide", ves_icall_System_Threading_Interlocked_MemoryBarrierProcessWide))
NOHANDLES(ICALL(ILOCK_23, "Read(long&)", ves_icall_System_Threading_Interlocked_Read_Long))

/* include these icalls if we're in the threaded wasm runtime, or if we're building a wasm-targeting cross compiler and we need to support --print-icall-table */
#if (defined(HOST_BROWSER) && !defined(DISABLE_THREADS)) || (defined(TARGET_WASM) && defined(ENABLE_ICALL_SYMBOL_MAP))
ICALL_TYPE(LIFOASYNCSEM, "System.Threading.LowLevelLifoAsyncWaitSemaphore", LIFOASYNCSEM_1)
NOHANDLES(ICALL(LIFOASYNCSEM_1, "DeleteInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_DeleteInternal))
NOHANDLES(ICALL(LIFOASYNCSEM_2, "InitInternal", ves_icall_System_Threading_LowLevelLifoAsyncWaitSemaphore_InitInternal))
NOHANDLES(ICALL(LIFOASYNCSEM_3, "PrepareAsyncWaitInternal", ves_icall_System_Threading_LowLevelLifoAsyncWaitSemaphore_PrepareAsyncWaitInternal))
NOHANDLES(ICALL(LIFOASYNCSEM_4, "ReleaseInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_ReleaseInternal))
#endif


ICALL_TYPE(LIFOSEM, "System.Threading.LowLevelLifoSemaphore", LIFOSEM_1)
NOHANDLES(ICALL(LIFOSEM_1, "DeleteInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_DeleteInternal))
NOHANDLES(ICALL(LIFOSEM_2, "InitInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_InitInternal))
NOHANDLES(ICALL(LIFOSEM_3, "ReleaseInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_ReleaseInternal))
NOHANDLES(ICALL(LIFOSEM_4, "TimedWaitInternal", ves_icall_System_Threading_LowLevelLifoSemaphore_TimedWaitInternal))


ICALL_TYPE(MONIT, "System.Threading.Monitor", MONIT_0)
HANDLES(MONIT_0, "Enter", ves_icall_System_Threading_Monitor_Monitor_Enter, void, 1, (MonoObject))
HANDLES(MONIT_1, "InternalExit", mono_monitor_exit_icall, void, 1, (MonoObject))
HANDLES(MONIT_2, "Monitor_pulse", ves_icall_System_Threading_Monitor_Monitor_pulse, void, 1, (MonoObject))
HANDLES(MONIT_3, "Monitor_pulse_all", ves_icall_System_Threading_Monitor_Monitor_pulse_all, void, 1, (MonoObject))
HANDLES(MONIT_7, "Monitor_wait", ves_icall_System_Threading_Monitor_Monitor_wait, MonoBoolean, 3, (MonoObject, guint32, MonoBoolean))
NOHANDLES(ICALL(MONIT_8, "get_LockContentionCount", ves_icall_System_Threading_Monitor_Monitor_LockContentionCount))
HANDLES(MONIT_9, "try_enter_with_atomic_var", ves_icall_System_Threading_Monitor_Monitor_try_enter_with_atomic_var, void, 4, (MonoObject, guint32, MonoBoolean, MonoBoolean_ref))

ICALL_TYPE(THREAD, "System.Threading.Thread", THREAD_1)
HANDLES(THREAD_1, "ClrState", ves_icall_System_Threading_Thread_ClrState, void, 2, (MonoInternalThread, guint32))
HANDLES(ITHREAD_2, "FreeInternal", ves_icall_System_Threading_InternalThread_Thread_free_internal, void, 1, (MonoInternalThread))
HANDLES(THREAD_15, "GetCurrentOSThreadId", ves_icall_System_Threading_Thread_GetCurrentOSThreadId, guint64, 0, ())
NOHANDLES(ICALL(THREAD_5, "GetCurrentThread", ves_icall_System_Threading_Thread_GetCurrentThread))
HANDLES(THREAD_3, "GetState", ves_icall_System_Threading_Thread_GetState, guint32, 1, (MonoInternalThread))
HANDLES(THREAD_4, "InitInternal", ves_icall_System_Threading_Thread_InitInternal, void, 1, (MonoThreadObject))
HANDLES(THREAD_6, "InterruptInternal", ves_icall_System_Threading_Thread_Interrupt_internal, void, 1, (MonoThreadObject))
HANDLES(THREAD_7, "JoinInternal", ves_icall_System_Threading_Thread_Join_internal, MonoBoolean, 2, (MonoThreadObject, int))
HANDLES(THREAD_8, "SetName_icall", ves_icall_System_Threading_Thread_SetName_icall, void, 3, (MonoInternalThread, const_gunichar2_ptr, gint32))
HANDLES(THREAD_9, "SetPriority", ves_icall_System_Threading_Thread_SetPriority, void, 2, (MonoThreadObject, int))
HANDLES(THREAD_10, "SetState", ves_icall_System_Threading_Thread_SetState, void, 2, (MonoInternalThread, guint32))
HANDLES(THREAD_13, "StartInternal", ves_icall_System_Threading_Thread_StartInternal, void, 2, (MonoThreadObject, gint32))
NOHANDLES(ICALL(THREAD_14, "YieldInternal", ves_icall_System_Threading_Thread_YieldInternal))

/* include these icalls if we're in the threaded wasm runtime, or if we're building a wasm-targeting cross compiler and we need to support --print-icall-table */
#if (defined(HOST_BROWSER) && !defined(DISABLE_THREADS)) || (defined(TARGET_WASM) && defined(ENABLE_ICALL_SYMBOL_MAP))
ICALL_TYPE(WEBWORKERLOOP, "System.Threading.WebWorkerEventLoop", WEBWORKERLOOP_1)
NOHANDLES(ICALL(WEBWORKERLOOP_1, "HasUnsettledInteropPromisesNative", ves_icall_System_Threading_WebWorkerEventLoop_HasUnsettledInteropPromisesNative))
NOHANDLES(ICALL(WEBWORKERLOOP_2, "KeepalivePopInternal", ves_icall_System_Threading_WebWorkerEventLoop_KeepalivePopInternal))
NOHANDLES(ICALL(WEBWORKERLOOP_3, "KeepalivePushInternal", ves_icall_System_Threading_WebWorkerEventLoop_KeepalivePushInternal))
#endif

ICALL_TYPE(TYPE, "System.Type", TYPE_1)
HANDLES(TYPE_1, "internal_from_handle", ves_icall_System_Type_internal_from_handle, MonoReflectionType, 1, (MonoType_ref))

ICALL_TYPE(TYPEDR, "System.TypedReference", TYPEDR_1)
HANDLES(TYPEDR_1, "InternalMakeTypedReference", ves_icall_System_TypedReference_InternalMakeTypedReference, void, 4, (MonoTypedRef_ptr, MonoObject, MonoArray, MonoReflectionType))
HANDLES(TYPEDR_2, "InternalToObject", ves_icall_System_TypedReference_ToObject, MonoObject, 1, (MonoTypedRef_ptr))

ICALL_TYPE(VALUET, "System.ValueType", VALUET_1)
HANDLES(VALUET_1, "InternalEquals", ves_icall_System_ValueType_Equals, MonoBoolean, 3, (MonoObject, MonoObject, MonoArrayOut))
HANDLES(VALUET_2, "InternalGetHashCode", ves_icall_System_ValueType_InternalGetHashCode, gint32, 2, (MonoObject, MonoArrayOut))

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
MONO_HANDLE_REGISTER_ICALL (mono_string_from_ansibstr, MonoString, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_bstr_icall, MonoString, 1, (mono_bstr_const))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalstr, MonoString, 2, (const_char_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_byvalwstr, MonoString, 2, (const_gunichar2_ptr, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_from_tbstr, MonoString, 1, (gpointer))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_len_wrapper, MonoString, 2, (const_char_ptr, guint))
MONO_HANDLE_REGISTER_ICALL (mono_string_new_wrapper_internal, MonoString, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_ansibstr, char_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_bstr, mono_bstr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalstr, void, 3, (char_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_byvalwstr, void, 3, (gunichar2_ptr, MonoString, int))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_tbstr, gpointer, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf16_internal, mono_unichar2_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (mono_string_to_utf32_internal, mono_unichar4_ptr, 1, (MonoString)) // embedding API
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder, void, 2, (MonoStringBuilder, const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf16_to_builder2, MonoStringBuilder, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder, void, 2, (MonoStringBuilder, const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_string_utf8_to_builder2, MonoStringBuilder, 1, (const_char_ptr))
MONO_HANDLE_REGISTER_ICALL (mono_type_from_handle, MonoReflectionType, 1, (MonoType_ptr)) // called by icalls
MONO_HANDLE_REGISTER_ICALL (ves_icall_marshal_alloc, gpointer, 1, (gsize))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_from_utf16, MonoString, 1, (const_gunichar2_ptr))
MONO_HANDLE_REGISTER_ICALL (ves_icall_mono_string_to_utf8, char_ptr, 1, (MonoString))
MONO_HANDLE_REGISTER_ICALL (ves_icall_string_new_wrapper, MonoString, 1, (const_char_ptr))
