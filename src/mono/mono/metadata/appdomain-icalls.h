/**
 * \file
 * Appdomain-related icalls.
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_METADATA_APPDOMAIN_ICALLS_H__
#define __MONO_METADATA_APPDOMAIN_ICALLS_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/object-internals.h>

MonoAppDomainHandle
ves_icall_System_AppDomain_getCurDomain            (MonoError *error);

MonoAppDomainHandle
ves_icall_System_AppDomain_getRootDomain           (MonoError *error);

MonoAppDomainHandle
ves_icall_System_AppDomain_createDomain            (MonoStringHandle         friendly_name,
						    MonoAppDomainSetupHandle setup,
						    MonoError                *error);

MonoObjectHandle
ves_icall_System_AppDomain_GetData                 (MonoAppDomainHandle ad, 
						    MonoStringHandle    name,
						    MonoError*          error);

MonoReflectionAssemblyHandle
ves_icall_System_AppDomain_LoadAssemblyRaw         (MonoAppDomainHandle ad,
    						    MonoArrayHandle raw_assembly,
						    MonoArrayHandle raw_symbol_store,
						    MonoObjectHandle evidence,
						    MonoBoolean refonly,
						    MonoError *error);

void
ves_icall_System_AppDomain_SetData                 (MonoAppDomainHandle ad, 
						    MonoStringHandle    name, 
						    MonoObjectHandle    data,
						    MonoError           *error);

MonoAppDomainSetupHandle
ves_icall_System_AppDomain_getSetup                (MonoAppDomainHandle ad,
						    MonoError *error);

MonoStringHandle
ves_icall_System_AppDomain_getFriendlyName         (MonoAppDomainHandle ad,
						    MonoError *error);

MonoArrayHandle
ves_icall_System_AppDomain_GetAssemblies           (MonoAppDomainHandle ad,
						    MonoBoolean refonly,
						    MonoError *error);

MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_LoadFrom      (MonoStringHandle fname,
						    MonoBoolean refonly,
						    MonoError *error);

MonoReflectionAssemblyHandle
ves_icall_System_AppDomain_LoadAssembly            (MonoAppDomainHandle ad, 
						    MonoStringHandle assRef,
						    MonoObjectHandle evidence,
						    MonoBoolean refonly,
						    MonoError *error);

gboolean
ves_icall_System_AppDomain_InternalIsFinalizingForUnload (gint32 domain_id, MonoError *error);

void
ves_icall_System_AppDomain_InternalUnload          (gint32 domain_id,
						    MonoError *error);

void
ves_icall_System_AppDomain_DoUnhandledException (MonoExceptionHandle exc, MonoError *error);

gint32
ves_icall_System_AppDomain_ExecuteAssembly         (MonoAppDomainHandle ad,
						    MonoReflectionAssemblyHandle refass,
						    MonoArrayHandle args,
						    MonoError *error);

MonoAppDomainHandle
ves_icall_System_AppDomain_InternalSetDomain	   (MonoAppDomainHandle ad, MonoError *error);

MonoAppDomainHandle
ves_icall_System_AppDomain_InternalSetDomainByID   (gint32 domainid, MonoError *error);

void
ves_icall_System_AppDomain_InternalPushDomainRef (MonoAppDomainHandle ad, MonoError *error);

void
ves_icall_System_AppDomain_InternalPushDomainRefByID (gint32 domain_id, MonoError *error);

void
ves_icall_System_AppDomain_InternalPopDomainRef (MonoError *error);

MonoAppContextHandle
ves_icall_System_AppDomain_InternalGetContext      (MonoError *error);

MonoAppContextHandle
ves_icall_System_AppDomain_InternalGetDefaultContext      (MonoError *error);

MonoAppContextHandle
ves_icall_System_AppDomain_InternalSetContext	   (MonoAppContextHandle mc, MonoError *error);

gint32 
ves_icall_System_AppDomain_GetIDFromDomain (MonoAppDomain * ad);

MonoStringHandle
ves_icall_System_AppDomain_InternalGetProcessGuid (MonoStringHandle newguid, MonoError *error);

MonoBoolean
ves_icall_System_CLRConfig_CheckThrowUnobservedTaskExceptions (void);


#endif /*__MONO_METADATA_APPDOMAIN_ICALLS_H__*/
