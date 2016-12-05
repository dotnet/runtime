/*
 * Appdomain-related icalls.
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef __MONO_METADATA_APPDOMAIN_ICALLS_H__
#define __MONO_METADATA_APPDOMAIN_ICALLS_H__

#include <mono/metadata/appdomain.h>
#include <mono/metadata/handle.h>
#include <mono/metadata/object-internals.h>

MonoAppDomain *
ves_icall_System_AppDomain_getCurDomain            (void);

MonoAppDomain *
ves_icall_System_AppDomain_getRootDomain           (void);

MonoAppDomain *
ves_icall_System_AppDomain_createDomain            (MonoString         *friendly_name,
						    MonoAppDomainSetup *setup);

MonoObject *
ves_icall_System_AppDomain_GetData                 (MonoAppDomain *ad, 
						    MonoString    *name);

MonoReflectionAssemblyHandle
ves_icall_System_AppDomain_LoadAssemblyRaw         (MonoAppDomainHandle ad,
    						    MonoArrayHandle raw_assembly,
						    MonoArrayHandle raw_symbol_store,
						    MonoObjectHandle evidence,
						    MonoBoolean refonly,
						    MonoError *error);

void
ves_icall_System_AppDomain_SetData                 (MonoAppDomain *ad, 
						    MonoString    *name, 
						    MonoObject    *data);

MonoAppDomainSetup *
ves_icall_System_AppDomain_getSetup                (MonoAppDomain *ad);

MonoString *
ves_icall_System_AppDomain_getFriendlyName         (MonoAppDomain *ad);

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
ves_icall_System_AppDomain_InternalIsFinalizingForUnload (gint32 domain_id);

void
ves_icall_System_AppDomain_InternalUnload          (gint32 domain_id);

void
ves_icall_System_AppDomain_DoUnhandledException (MonoException *exc);

gint32
ves_icall_System_AppDomain_ExecuteAssembly         (MonoAppDomainHandle ad,
						    MonoReflectionAssemblyHandle refass,
						    MonoArrayHandle args,
						    MonoError *error);

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomain	   (MonoAppDomain *ad);

MonoAppDomain * 
ves_icall_System_AppDomain_InternalSetDomainByID   (gint32 domainid);

void
ves_icall_System_AppDomain_InternalPushDomainRef (MonoAppDomain *ad);

void
ves_icall_System_AppDomain_InternalPushDomainRefByID (gint32 domain_id);

void
ves_icall_System_AppDomain_InternalPopDomainRef (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetContext      (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalGetDefaultContext      (void);

MonoAppContext * 
ves_icall_System_AppDomain_InternalSetContext	   (MonoAppContext *mc);

gint32 
ves_icall_System_AppDomain_GetIDFromDomain (MonoAppDomain * ad);

MonoString *
ves_icall_System_AppDomain_InternalGetProcessGuid (MonoString* newguid);

MonoBoolean
ves_icall_System_CLRConfig_CheckThrowUnobservedTaskExceptions (void);


#endif /*__MONO_METADATA_APPDOMAIN_ICALLS_H__*/
