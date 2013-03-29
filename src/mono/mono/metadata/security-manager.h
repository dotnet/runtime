/*
 * security-manager.h:  Security Manager
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 */

#ifndef _MONO_METADATA_SECURITY_MANAGER_H_
#define _MONO_METADATA_SECURITY_MANAGER_H_

#include <string.h>

#include "object.h"
#include "metadata-internals.h"
#include "domain-internals.h"
#include "tokentype.h"
#include "threads.h"
#include "marshal.h"
#include "image.h"
#include "reflection.h"
#include "tabledefs.h"


/* Definitions */

#define MONO_ECMA_KEY_LENGTH			16
#define MONO_PUBLIC_KEY_HEADER_LENGTH		32
#define MONO_MINIMUM_PUBLIC_KEY_LENGTH		48
#define MONO_DEFAULT_PUBLIC_KEY_LENGTH		128

#define MONO_PUBLIC_KEY_BIT_SIZE(x)		((x - MONO_PUBLIC_KEY_HEADER_LENGTH) << 3)

enum {
	MONO_METADATA_SECURITY_OK		= 0x00,
	MONO_METADATA_INHERITANCEDEMAND_CLASS	= 0x01,
	MONO_METADATA_INHERITANCEDEMAND_METHOD	= 0x02
};

typedef enum {
	MONO_SECURITY_MODE_NONE,
	MONO_SECURITY_MODE_CORE_CLR,
	MONO_SECURITY_MODE_CAS,
	MONO_SECURITY_MODE_SMCS_HACK
} MonoSecurityMode;

/* Structures */

typedef struct {
	MonoClass *securitymanager;		/* System.Security.SecurityManager */
	MonoMethod *demand;			/* SecurityManager.InternalDemand */
	MonoMethod *demandchoice;		/* SecurityManager.InternalDemandChoice */
	MonoMethod *demandunmanaged;		/* SecurityManager.DemandUnmanaged */
	MonoMethod *inheritancedemand;		/* SecurityManager.InheritanceDemand */
	MonoMethod *inheritsecurityexception;	/* SecurityManager.InheritanceDemandSecurityException */
	MonoMethod *linkdemand;			/* SecurityManager.LinkDemand */
	MonoMethod *linkdemandfulltrust;	/* SecurityManager.LinkDemandFullTrust */
	MonoMethod *linkdemandunmanaged;	/* SecurityManager.LinkDemandUnmanaged */
	MonoMethod *linkdemandsecurityexception;/* SecurityManager.LinkDemandSecurityException */

	MonoClass *allowpartiallytrustedcallers;	/* System.Security.AllowPartiallyTrustedCallersAttribute */
	MonoClass *suppressunmanagedcodesecurity;	/* System.Security.SuppressUnmanagedCodeSecurityAttribute */
} MonoSecurityManager;

gboolean mono_is_ecma_key (const char *publickey, int size) MONO_INTERNAL;
MonoMethod* mono_get_context_capture_method (void) MONO_INTERNAL;

void mono_secman_inheritancedemand_class (MonoClass *klass, MonoClass *parent) MONO_INTERNAL;
void mono_secman_inheritancedemand_method (MonoMethod *override, MonoMethod *base) MONO_INTERNAL;

/* Initialization/utility functions */
void mono_activate_security_manager (void) MONO_INTERNAL;
MonoSecurityManager* mono_security_manager_get_methods (void) MONO_INTERNAL;

/* Security mode */
gboolean mono_is_security_manager_active (void) MONO_INTERNAL;
void mono_security_set_mode (MonoSecurityMode mode) MONO_INTERNAL;
MonoSecurityMode mono_security_get_mode (void) MONO_INTERNAL;

/* internal calls */
MonoBoolean ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void) MONO_INTERNAL;
void ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value) MONO_INTERNAL;
MonoBoolean ves_icall_System_Security_SecurityManager_get_CheckExecutionRights (void) MONO_INTERNAL;
void ves_icall_System_Security_SecurityManager_set_CheckExecutionRights (MonoBoolean value) MONO_INTERNAL;
MonoBoolean ves_icall_System_Security_SecurityManager_GetLinkDemandSecurity (MonoReflectionMethod *m, MonoDeclSecurityActions *kactions, MonoDeclSecurityActions *mactions) MONO_INTERNAL;

#ifndef DISABLE_SECURITY
#define mono_security_enabled() (mono_is_security_manager_active ())
#define mono_security_cas_enabled() (mono_security_get_mode () == MONO_SECURITY_MODE_CAS)
#define mono_security_core_clr_enabled() (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
#define mono_security_smcs_hack_enabled() (mono_security_get_mode () == MONO_SECURITY_MODE_SMCS_HACK)
#else
#define mono_security_enabled() (FALSE)
#define mono_security_cas_enabled() (FALSE)
#define mono_security_core_clr_enabled() (FALSE)
#define mono_security_smcs_hack_enabled() (FALSE)
#endif

#endif /* _MONO_METADATA_SECURITY_MANAGER_H_ */
