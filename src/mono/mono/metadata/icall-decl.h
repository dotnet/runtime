/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ICALL_H__
#define __MONO_METADATA_ICALL_H__

#include "appdomain-icalls.h"
#include "console-io.h"
#include "environment.h"
#include "file-mmap.h"
#include "filewatcher.h"
#include "gc-internals.h"
#include "handle-decl.h"
#include "locales.h"
#include "marshal.h"
#include "monitor.h"
#include "mono-perfcounters.h"
#include "mono-route.h"
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


// This is sorted.
// grep ICALL_EXPORT | sort | uniq
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetConstructors_native (MonoReflectionTypeHandle, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetEvents_native (MonoReflectionTypeHandle, char*, guint32, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetFields_native (MonoReflectionTypeHandle, char*, guint32, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetMethodsByName_native (MonoReflectionTypeHandle, const char*, guint32, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetNestedTypes_native (MonoReflectionTypeHandle, char*, guint32, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_RuntimeType_GetPropertiesByName_native (MonoReflectionTypeHandle, char*, guint32, guint32, MonoError*);
ICALL_EXPORT GPtrArray* ves_icall_System_Reflection_Assembly_InternalGetReferencedAssemblies (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoArray* ves_icall_System_Environment_GetEnvironmentVariableNames (void);
ICALL_EXPORT MonoArray* ves_icall_System_Environment_GetLogicalDrives (void);
ICALL_EXPORT MonoArrayHandle ves_icall_MonoCustomAttrs_GetCustomAttributesDataInternal (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_MonoCustomAttrs_GetCustomAttributesInternal (MonoObjectHandle, MonoReflectionTypeHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_MonoMethod_GetGenericArguments (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_MonoPropertyInfo_GetTypeModifiers (MonoReflectionPropertyHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_ParameterInfo_GetTypeModifiers (MonoReflectionParameterHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_RuntimeType_GetGenericArguments (MonoReflectionTypeHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_RuntimeType_GetInterfaces (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_SignatureHelper_get_signature_field (MonoReflectionSigHelperHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_SignatureHelper_get_signature_local (MonoReflectionSigHelperHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Array_CreateInstanceImpl (MonoReflectionTypeHandle, MonoArrayHandle, MonoArrayHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Environment_GetCommandLineArgs (MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_IO_MonoIO_get_InvalidPathChars (MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_Assembly_GetManifestResourceNames (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_Assembly_GetModulesInternal (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_Assembly_GetTypes (MonoReflectionAssemblyHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_FieldInfo_GetTypeModifiers (MonoReflectionFieldHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_Module_InternalGetTypes (MonoReflectionModuleHandle, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_Module_ResolveSignature (MonoImage*, guint32, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoArrayHandle ves_icall_System_Reflection_MonoMethodInfo_get_parameter_info (MonoMethod*, MonoReflectionMethodHandle member, MonoError*);
ICALL_EXPORT MonoAssemblyName* ves_icall_System_Reflection_AssemblyName_GetNativeName (MonoAssembly*);
ICALL_EXPORT MonoBoolean ves_icall_IsTransparentProxy (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_MonoCustomAttrs_IsDefinedInternal (MonoObjectHandle, MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_MonoMethod_get_IsGenericMethod (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_MonoMethod_get_IsGenericMethodDefinition (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_Mono_TlsProviderFactory_IsBtlsSupported (MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_HasInstantiation (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_HasReferences (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsArray (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsByRef (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsComObject (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsGenericTypeDefinition (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsGenericVariable (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsPointer (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_IsPrimitive (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_RuntimeTypeHandle_is_subclass_of (MonoType* childType, MonoType*);
ICALL_EXPORT MonoBoolean ves_icall_System_Array_FastCopy (MonoArray* source, int, MonoArray*, int, int);
ICALL_EXPORT MonoBoolean ves_icall_System_Buffer_BlockCopyInternal (MonoArray*, gint32, MonoArray*, gint32, gint32);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Debugger_IsAttached_internal (MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Debugger_IsLogging (MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Process_CreateProcess_internal (MonoW32ProcessStartInfoHandle, gpointer, gpointer, gpointer, MonoW32ProcessInfo*, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Diagnostics_Process_ShellExecuteEx_internal (MonoW32ProcessStartInfoHandle, MonoW32ProcessInfo*, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Enum_GetEnumValuesAndNames (MonoReflectionTypeHandle, MonoArrayHandleOut, MonoArrayHandleOut, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Enum_InternalHasFlag (MonoObjectHandle, MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_GetIs64BitOperatingSystem (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Environment_get_HasShutdownStarted (void);
ICALL_EXPORT MonoBoolean ves_icall_System_IO_DriveInfo_GetDiskFreeSpace (MonoString*, guint64*, guint64*, guint64* total_number_of_free_bytes, gint32*);
ICALL_EXPORT MonoBoolean ves_icall_System_IO_MonoIO_FindNextFile (gpointer, MonoStringHandleOut, gint32*, gint32*, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Net_Dns_GetHostByAddr_internal (MonoStringHandle, MonoStringHandleOut, MonoArrayHandleOut, MonoArrayHandleOut, gint32, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Net_Dns_GetHostByName_internal (MonoStringHandle, MonoStringHandleOut, MonoArrayHandleOut, MonoArrayHandleOut, gint32, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Net_Dns_GetHostName_internal (MonoStringHandleOut, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_AssemblyName_ParseAssemblyName (const char*, MonoAssemblyName*, MonoBoolean*, MonoBoolean* is_token_defined_arg);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_Assembly_GetManifestResourceInfoInternal (MonoReflectionAssemblyHandle, MonoStringHandle, MonoManifestResourceInfoHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_Assembly_LoadPermissions (MonoReflectionAssemblyHandle, char**, guint32*, char**, guint32*, char**, guint32*, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_Assembly_get_ReflectionOnly (MonoReflectionAssemblyHandle assembly_h, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Reflection_Assembly_get_global_assembly_cache (MonoReflectionAssemblyHandle assembly, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_RuntimeType_IsTypeExportedToWindowsRuntime (MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_RuntimeType_IsWindowsRuntimeObjectType (MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_SufficientExecutionStack (void);
ICALL_EXPORT MonoBoolean ves_icall_System_Runtime_InteropServices_Marshal_IsComObject (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoOriginateLanguageException (int, MonoStringHandle, gpointer, MonoError*);
ICALL_EXPORT MonoBoolean ves_icall_System_Threading_Thread_YieldInternal (void);
ICALL_EXPORT MonoBoolean ves_icall_System_ValueType_Equals (MonoObject* this_obj, MonoObject* that, MonoArray** fields);
ICALL_EXPORT MonoBoolean ves_icall_get_resources_ptr (MonoReflectionAssemblyHandle assembly, gpointer* result, gint32* size, MonoError*);
ICALL_EXPORT MonoClassField* ves_icall_System_Reflection_Module_ResolveFieldToken (MonoImage*, guint32, MonoArrayHandle, MonoArrayHandle, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoComInteropProxyHandle ves_icall_Mono_Interop_ComInteropProxy_FindProxy (gpointer, MonoError*);
ICALL_EXPORT MonoDelegateHandle ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (gpointer, MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoGenericParamInfo* ves_icall_RuntimeTypeHandle_GetGenericParameterInfo (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoMethod* ves_icall_System_Reflection_Module_ResolveMethodToken (MonoImage*, guint32, MonoArrayHandle, MonoArrayHandle, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoMulticastDelegateHandle ves_icall_System_Delegate_AllocDelegateLike_internal (MonoDelegateHandle, MonoError*);
ICALL_EXPORT MonoObject* ves_icall_InternalExecute (MonoReflectionMethod*, MonoObject*, MonoArray*, MonoArray**);
ICALL_EXPORT MonoObject* ves_icall_InternalInvoke (MonoReflectionMethod*, MonoObject*, MonoArray*, MonoException**);
ICALL_EXPORT MonoObject* ves_icall_MonoField_GetRawConstantValue (MonoReflectionField* rfield);
ICALL_EXPORT MonoObject* ves_icall_MonoField_GetValueInternal (MonoReflectionField* field, MonoObject* obj);
ICALL_EXPORT MonoObject* ves_icall_System_Object_MemberwiseClone (MonoObject*);
ICALL_EXPORT MonoObject* ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_GetObjectValue (MonoObject*);
ICALL_EXPORT MonoObject* ves_icall_property_info_get_default_value (MonoReflectionProperty*);
ICALL_EXPORT MonoObjectHandle mono_TypedReference_ToObject (MonoTypedRef*, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_Remoting_RealProxy_GetTransparentProxy (MonoObjectHandle, MonoStringHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Activator_CreateInstanceInternal (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Array_GetValue (MonoArrayHandle, MonoArrayHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Array_GetValueImpl (MonoArrayHandle, guint32, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_ComObject_CreateRCW (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Delegate_CreateDelegate_internal (MonoReflectionTypeHandle, MonoObjectHandle, MonoReflectionMethodHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Enum_ToObject (MonoReflectionTypeHandle, guint64, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Enum_get_value (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Reflection_Assembly_GetFilesInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Reflection_Module_ResolveMemberToken (MonoImage*, guint32, MonoArrayHandle, MonoArrayHandle, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Runtime_Activation_ActivationServices_AllocateUninitializedClassInstance (MonoReflectionTypeHandle type, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Runtime_InteropServices_Marshal_GetNativeActivationFactory (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW (gpointer, MonoError*);
ICALL_EXPORT MonoObjectHandle ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_GetRestrictedErrorInfo (MonoError*);
ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_RuntimeTypeHandle_GetAssembly (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_System_Reflection_Assembly_GetCallingAssembly (MonoError*);
ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_System_Reflection_Assembly_GetEntryAssembly (MonoError*);
ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_System_Reflection_Assembly_GetExecutingAssembly (MonoError*);
ICALL_EXPORT MonoReflectionAssemblyHandle ves_icall_System_Reflection_Assembly_load_with_partial_name (MonoStringHandle, MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoReflectionEventHandle ves_icall_System_Reflection_EventInfo_internal_from_handle_type (MonoEvent*, MonoType*, MonoError*);
ICALL_EXPORT MonoReflectionFieldHandle ves_icall_System_Reflection_FieldInfo_internal_from_handle_type (MonoClassField*, MonoType*, MonoError*);
ICALL_EXPORT MonoReflectionMarshalAsAttributeHandle ves_icall_System_MonoMethodInfo_get_retval_marshal (MonoMethod*, MonoError*);
ICALL_EXPORT MonoReflectionMarshalAsAttributeHandle ves_icall_System_Reflection_FieldInfo_get_marshal_info (MonoReflectionFieldHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodBodyHandle ves_icall_System_Reflection_MethodBase_GetMethodBodyInternal (MonoMethod*, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_GetCurrentMethod (MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_MonoMethod_GetGenericMethodDefinition (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_MonoMethod_MakeGenericMethod_impl (MonoReflectionMethodHandle, MonoArrayHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_MonoMethod_get_base_method (MonoReflectionMethodHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_Remoting_RemotingServices_GetVirtualMethod (MonoReflectionTypeHandle, MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_RuntimeType_GetCorrespondingInflatedMethod (MonoReflectionTypeHandle, MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_RuntimeType_get_DeclaringMethod (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_System_Delegate_GetVirtualMethod_internal (MonoDelegateHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_System_Reflection_Assembly_get_EntryPoint (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoReflectionMethodHandle ves_icall_System_Reflection_MethodBase_GetMethodFromHandleInternalType_native (MonoMethod*, MonoType*, MonoBoolean, MonoError*);
ICALL_EXPORT MonoReflectionModuleHandle ves_icall_RuntimeTypeHandle_GetModule (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionModuleHandle ves_icall_System_Reflection_Assembly_GetManifestModuleInternal (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoReflectionPropertyHandle ves_icall_System_Reflection_PropertyInfo_internal_from_handle_type (MonoProperty*, MonoType*, MonoError*);
ICALL_EXPORT MonoReflectionType* ves_icall_Remoting_RealProxy_InternalGetProxyType (MonoTransparentProxy*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_MonoField_GetParentType (MonoReflectionFieldHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_MonoField_ResolveType (MonoReflectionFieldHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeTypeHandle_GetBaseType (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeTypeHandle_GetElementType (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeTypeHandle_GetGenericTypeDefinition_impl (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeType_MakeGenericType (MonoReflectionTypeHandle, MonoArrayHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeType_MakePointerType (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeType_get_DeclaringType (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeType_make_array_type (MonoReflectionTypeHandle, int, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_RuntimeType_make_byref_type (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Enum_get_underlying_type (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Object_GetType (MonoObjectHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Reflection_Assembly_InternalGetType (MonoReflectionAssemblyHandle, MonoReflectionModuleHandle, MonoStringHandle, MonoBoolean, MonoBoolean, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Reflection_Module_GetGlobalType (MonoReflectionModuleHandle, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Type_internal_from_handle (MonoType*, MonoError*);
ICALL_EXPORT MonoReflectionTypeHandle ves_icall_System_Type_internal_from_name (MonoStringHandle, MonoBoolean, MonoBoolean, MonoError*);
ICALL_EXPORT MonoString* ves_icall_System_IO_DriveInfo_GetDriveFormat (MonoString*);
ICALL_EXPORT MonoStringHandle ves_icall_MonoMethod_get_name (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_Mono_Runtime_ExceptionToState (MonoExceptionHandle, guint64*, guint64*, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_Mono_Runtime_GetDisplayName (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_Mono_Runtime_GetNativeStackTrace (MonoExceptionHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_RuntimeType_get_Name (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_RuntimeType_get_Namespace (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Configuration_DefaultConfig_get_bundled_machine_config (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Configuration_DefaultConfig_get_machine_config_path (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_app_config (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Configuration_InternalConfigurationHost_get_bundled_machine_config (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetEnvironmentVariable_native (const char*, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetGacPath (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetMachineConfigPath (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetOSVersionString (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetWindowsFolderPath (int folder, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_GetWindowsFolderPath (int, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_InternalGetHome (MonoError*);;
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_get_MachineName (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_get_NewLine (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_get_UserName (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Environment_get_bundled_machine_config (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Globalization_CultureInfo_get_current_locale_name (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_IO_MonoIO_GetCurrentDirectory (gint32*, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_IO_get_temp_path (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Assembly_GetAotId (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Assembly_InternalImageRuntimeVersion (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Assembly_get_code_base (MonoReflectionAssemblyHandle, MonoBoolean, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Assembly_get_fullName (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Assembly_get_location (MonoReflectionAssemblyHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Module_GetGuidInternal (MonoReflectionModuleHandle, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Reflection_Module_ResolveStringToken (MonoImage*, guint32, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_RuntimeType_getFullName (MonoReflectionTypeHandle, MonoBoolean, MonoBoolean, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Runtime_InteropServices_RuntimeInformation_get_RuntimeArchitecture (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Text_EncodingHelper_InternalCodePage (gint32*, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Web_Util_ICalls_GetMachineConfigPath (MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_System_Web_Util_ICalls_get_machine_install_dir (MonoError*);
ICALL_EXPORT MonoType* mono_ArgIterator_IntGetNextArgType (MonoArgIterator* iter);
ICALL_EXPORT MonoType* ves_icall_Mono_RuntimeClassHandle_GetTypeFromClass (MonoClass*, MonoError*);
ICALL_EXPORT MonoType* ves_icall_System_Reflection_Module_ResolveTypeToken (MonoImage*, guint32, MonoArrayHandle, MonoArrayHandle, MonoResolveTokenError*, MonoError*);
ICALL_EXPORT MonoTypedRef mono_ArgIterator_IntGetNextArg (MonoArgIterator* iter);
ICALL_EXPORT MonoTypedRef mono_ArgIterator_IntGetNextArgT (MonoArgIterator*, MonoType*);
ICALL_EXPORT MonoTypedRef mono_TypedReference_MakeTypedReferenceInternal (MonoObject* target, MonoArray* fields);
ICALL_EXPORT char* ves_icall_Mono_SafeStringMarshal_StringToUtf8 (MonoStringHandle, MonoError*);
ICALL_EXPORT char* ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (const gunichar2*, int, MonoError*);
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
ICALL_EXPORT gint32 ves_icall_Microsoft_Win32_NativeMethods_GetCurrentProcessId (MonoError*);
ICALL_EXPORT gint32 ves_icall_Microsoft_Win32_NativeMethods_WaitForInputIdle (gpointer, gint32, MonoError*);
ICALL_EXPORT gint32 ves_icall_MonoField_GetFieldOffset (MonoReflectionFieldHandle, MonoError*);
ICALL_EXPORT gint32 ves_icall_RuntimeTypeHandle_GetArrayRank (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT gint32 ves_icall_RuntimeType_GetGenericParameterPosition (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Array_GetLength (MonoArrayHandle, gint32, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Array_GetLowerBound (MonoArrayHandle, gint32, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Array_GetRank (MonoObjectHandle, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Buffer_ByteLengthInternal (MonoArray*);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_ProcessorCount (void);
ICALL_EXPORT gint32 ves_icall_System_Environment_get_TickCount (void);
ICALL_EXPORT gint32 ves_icall_System_Reflection_Module_GetMDStreamVersion (MonoImage* image, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal (MonoObjectHandle, MonoError*);
ICALL_EXPORT gint32 ves_icall_System_Runtime_Versioning_VersioningHelper_GetRuntimeId (MonoError*);
ICALL_EXPORT gint32 ves_icall_System_ValueType_InternalGetHashCode (MonoObject*, MonoArray**);
ICALL_EXPORT gint64 ves_icall_System_Array_GetLongLength (MonoArrayHandle, gint32, MonoError*);
ICALL_EXPORT gint64 ves_icall_System_DateTime_GetSystemTimeAsFileTime (void);
ICALL_EXPORT gint64 ves_icall_System_Diagnostics_Stopwatch_GetTimestamp (void);
ICALL_EXPORT gint64 ves_icall_System_Threading_Timer_GetTimeMonotonic (void);
ICALL_EXPORT gint8 ves_icall_System_Buffer_GetByteInternal (MonoArray* array, gint32 idx);
ICALL_EXPORT gpointer ves_icall_RuntimeMethodHandle_GetFunctionPointer (MonoMethod*, MonoError*);
ICALL_EXPORT gpointer ves_icall_System_Reflection_Module_GetHINSTANCE (MonoReflectionModuleHandle module, MonoError*);
ICALL_EXPORT gpointer ves_icall_System_Runtime_InteropServices_Marshal_GetRawIUnknownForComObjectNoAddRef (MonoObjectHandle, MonoError*);
ICALL_EXPORT guint32 ves_icall_RuntimeTypeHandle_GetAttributes (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT guint32 ves_icall_RuntimeTypeHandle_IsInstanceOfType (MonoReflectionTypeHandle, MonoObjectHandle, MonoError*);
ICALL_EXPORT guint32 ves_icall_RuntimeTypeHandle_type_is_assignable_from (MonoReflectionTypeHandle, MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT guint32 ves_icall_System_IO_DriveInfo_GetDriveType (MonoString* root_path_name);
ICALL_EXPORT guint32 ves_icall_reflection_get_token (MonoObjectHandle obj, MonoError*);
ICALL_EXPORT guint32 ves_icall_type_GetTypeCodeInternal (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT int ves_icall_Interop_Sys_DoubleToString(double value, char* format, char* buffer, int bufferLength);
ICALL_EXPORT int ves_icall_MonoField_get_core_clr_security_level (MonoReflectionField* rfield);
ICALL_EXPORT int ves_icall_MonoMethod_get_core_clr_security_level (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT int ves_icall_RuntimeType_get_core_clr_security_level (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT int ves_icall_System_Enum_compare_value_to (MonoObjectHandle, MonoObjectHandle, MonoError*);
ICALL_EXPORT int ves_icall_System_Enum_get_hashcode (MonoObjectHandle, MonoError*);
ICALL_EXPORT int ves_icall_System_Environment_get_Platform (void);
ICALL_EXPORT int ves_icall_System_GC_GetCollectionCount (int, MonoError*);
ICALL_EXPORT int ves_icall_System_GC_GetGeneration (MonoObjectHandle, MonoError*);
ICALL_EXPORT int ves_icall_System_GC_GetMaxGeneration (MonoError*);
ICALL_EXPORT int ves_icall_System_Runtime_InteropServices_Marshal_GetHRForException_WinRT (MonoExceptionHandle, MonoError*);
ICALL_EXPORT int ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsCreateString (MonoStringHandle, int, gpointer*, MonoError*);
ICALL_EXPORT int ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsDeleteString (gpointer, MonoError*);
ICALL_EXPORT int ves_icall_System_Threading_Thread_SystemMaxStackSize (void);
ICALL_EXPORT int ves_icall_get_method_attributes (MonoMethod* method);
ICALL_EXPORT mono_unichar2* ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_WindowsGetStringRawBuffer (gpointer, unsigned*, MonoError*);
ICALL_EXPORT void mono_ArgIterator_Setup (MonoArgIterator* iter, char* argsp, char* start);
ICALL_EXPORT void ves_icall_AssemblyBuilder_UpdateNativeCustomAttributes (MonoReflectionAssemblyBuilderHandle, MonoError*);
ICALL_EXPORT void ves_icall_DynamicMethod_create_dynamic_method (MonoReflectionDynamicMethodHandle, MonoError*);
ICALL_EXPORT void ves_icall_MonoEventInfo_get_event_info (MonoReflectionMonoEventHandle, MonoEventInfo*, MonoError*);
ICALL_EXPORT void ves_icall_MonoField_SetValueInternal (MonoReflectionFieldHandle, MonoObjectHandle, MonoObjectHandle, MonoError*);
ICALL_EXPORT void ves_icall_MonoMethod_GetPInvoke (MonoReflectionMethodHandle, int*, MonoStringHandleOut, MonoStringHandleOut, MonoError*);
ICALL_EXPORT void ves_icall_MonoPropertyInfo_get_property_info (MonoReflectionPropertyHandle, MonoPropertyInfo*, PInfo, MonoError*);
ICALL_EXPORT void ves_icall_Mono_Interop_ComInteropProxy_AddProxy (gpointer, MonoComInteropProxyHandle, MonoError*);
ICALL_EXPORT void ves_icall_Mono_RuntimeGPtrArrayHandle_GPtrArrayFree (GPtrArray*, MonoError*);
ICALL_EXPORT void ves_icall_Mono_RuntimeMarshal_FreeAssemblyName (MonoAssemblyName*, MonoBoolean, MonoError*);
ICALL_EXPORT void ves_icall_Mono_Runtime_DisableMicrosoftTelemetry (MonoError*);
ICALL_EXPORT void ves_icall_Mono_Runtime_DumpTelemetry (char*, guint64, guint64, MonoError*);
ICALL_EXPORT void ves_icall_Mono_Runtime_EnableMicrosoftTelemetry (char*, char*, char*, char*, char*, char*, MonoError*);
ICALL_EXPORT void ves_icall_Mono_Runtime_SendMicrosoftTelemetry (char*, guint64, guint64, MonoError*);
ICALL_EXPORT void ves_icall_Mono_SafeStringMarshal_GFree (gpointer, MonoError*);
ICALL_EXPORT void ves_icall_RuntimeType_GetInterfaceMapData (MonoReflectionTypeHandle, MonoReflectionTypeHandle, MonoArrayHandleOut, MonoArrayHandleOut, MonoError*);
ICALL_EXPORT void ves_icall_RuntimeType_GetPacking (MonoReflectionTypeHandle, guint32*, guint32*, MonoError*);
ICALL_EXPORT void ves_icall_System_Array_ClearInternal (MonoArrayHandle, int, int, MonoError*);
ICALL_EXPORT void ves_icall_System_Array_GetGenericValueImpl (MonoArray* arr, guint32 pos, gpointer value);
ICALL_EXPORT void ves_icall_System_Array_SetGenericValueImpl (MonoArray* arr, guint32 pos, gpointer value);
ICALL_EXPORT void ves_icall_System_Array_SetValue (MonoArrayHandle, MonoObjectHandle, MonoArrayHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Array_SetValueImpl (MonoArrayHandle, MonoObjectHandle, guint32, MonoError*);
ICALL_EXPORT void ves_icall_System_Buffer_SetByteInternal (MonoArray* array, gint32 idx, gint8 value);
ICALL_EXPORT void ves_icall_System_ComObject_ReleaseInterfaces (MonoComObjectHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Diagnostics_Debugger_Log (int, MonoStringHandle, MonoStringHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Diagnostics_DefaultTraceListener_WriteWindowsDebugString (const gunichar2*, MonoError*);
ICALL_EXPORT void ves_icall_System_Environment_BroadcastSettingChange (MonoError*);
ICALL_EXPORT void ves_icall_System_Environment_Exit (int result);
ICALL_EXPORT void ves_icall_System_Environment_InternalSetEnvironmentVariable (MonoString* name, MonoString* value);
ICALL_EXPORT void ves_icall_System_GC_RecordPressure (gint64, MonoError*);
ICALL_EXPORT void ves_icall_System_IO_LogcatTextWriter_Log (const char* appname, gint32 level, const char* message);
ICALL_EXPORT void ves_icall_System_NumberFormatter_GetFormatterTables (guint64 const**, gint32 const**, gunichar2 const**, gunichar2 const**, gint64 const**, gint32 const**);
ICALL_EXPORT void ves_icall_System_Reflection_Assembly_InternalGetAssemblyName (MonoStringHandle, MonoAssemblyName*, MonoStringHandleOut, MonoError*);
ICALL_EXPORT void ves_icall_System_Reflection_Module_Close (MonoReflectionModuleHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Reflection_Module_GetPEKind (MonoImage*, gint32*, gint32*, MonoError*);
ICALL_EXPORT void ves_icall_System_RuntimeFieldHandle_SetValueDirect (MonoReflectionField*, MonoReflectionType*, MonoTypedRef*, MonoObject*, MonoReflectionType*);
ICALL_EXPORT void ves_icall_System_Runtime_Activation_ActivationServices_EnableProxyActivation (MonoReflectionTypeHandle, MonoBoolean, MonoError*);
ICALL_EXPORT void ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_InitializeArray (MonoArrayHandle, MonoClassField*, MonoError*);
ICALL_EXPORT void ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunClassConstructor (MonoType*);
ICALL_EXPORT void ves_icall_System_Runtime_CompilerServices_RuntimeHelpers_RunModuleConstructor (MonoImage*);
ICALL_EXPORT void ves_icall_System_Runtime_InteropServices_Marshal_Prelink (MonoReflectionMethodHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Runtime_InteropServices_Marshal_PrelinkAll (MonoReflectionTypeHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Runtime_InteropServices_WindowsRuntime_UnsafeNativeMethods_RoReportUnhandledError (MonoObjectHandle, MonoError*);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_Memmove (guint8*, guint8*, guint);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_Memmove_wbarrier (guint8*, guint8*, guint, MonoType*);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ZeroMemory (guint8*, guint byte_length);
ICALL_EXPORT void ves_icall_System_Runtime_RuntimeImports_ecvt_s(char*, size_t, double, int, int*, int*);
ICALL_EXPORT void ves_icall_get_method_info (MonoMethod*, MonoMethodInfo*, MonoError*);
ICALL_EXPORT void* ves_icall_System_Reflection_Assembly_GetManifestResourceInternal (MonoReflectionAssemblyHandle assembly_h, MonoStringHandle name, gint32* size, MonoReflectionModuleHandleOut ref_module, MonoError*);
ICALL_EXPORT MonoStringHandle ves_icall_Mono_Runtime_DumpStateSingle (guint64 *portable_hash, guint64 *unportable_hash, MonoError *error);
ICALL_EXPORT MonoStringHandle ves_icall_Mono_Runtime_DumpStateTotal (guint64 *portable_hash, guint64 *unportable_hash, MonoError *error);
ICALL_EXPORT void ves_icall_Mono_Runtime_RegisterReportingForNativeLib (const char *path_suffix, const char *module_name);

#endif // __MONO_METADATA_ICALL_H__
