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
#include "tokentype.h"
#include "threads.h"
#include "marshal.h"
#include "image.h"


/* Definitions */

#define MONO_ECMA_KEY_LENGTH			16


/* Structures */

typedef struct {
	MonoClass *securitymanager;		/* System.Security.SecurityManager */
	MonoMethod *demand;			/* SecurityManager.InternalDemand */
	MonoMethod *demandchoice;		/* SecurityManager.InternalDemandChoice */
	MonoMethod *assert;			/* SecurityManager.InternalAssert */
	MonoMethod *deny;			/* SecurityManager.InternalDeny */
	MonoMethod *permitonly;			/* SecurityManager.InternalPermitOnly */
	MonoMethod *inheritancedemand;		/* SecurityManager.InheritanceDemand */
	MonoMethod *linkdemand;			/* SecurityManager.LinkDemand */
	MonoMethod *linkdemandfulltrust;	/* SecurityManager.LinkDemandFullTrust */
	MonoMethod *linkdemandunmanaged;	/* SecurityManager.LinkDemandUnmanaged */
	MonoMethod *linkdemandsecurityexception;/* SecurityManager.LinkDemandSecurityException */
	MonoClass *aptc;			/* System.Security.AllowPartiallyTrustedCallersAttribute */
} MonoSecurityManager;


/* Initialization/utility functions */
void mono_activate_security_manager (void);
gboolean mono_is_security_manager_active (void);
MonoSecurityManager* mono_security_manager_get_methods (void);
gboolean mono_is_ecma_key (char *publickey, int size);


/* internal calls */
MonoBoolean ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void);
void ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value);
MonoBoolean ves_icall_System_Security_SecurityManager_get_CheckExecutionRights (void);
void ves_icall_System_Security_SecurityManager_set_CheckExecutionRights (MonoBoolean value);


#endif /* _MONO_METADATA_SECURITY_MANAGER_H_ */
