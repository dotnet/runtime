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


/* Structures */

typedef struct {
	MonoClass *securitymanager;		/* System.Security.SecurityManager */
	MonoMethod *demand;			/* SecurityManager.InternalDemand */
	MonoMethod *demandchoice;		/* SecurityManager.InternalDemandChoice */
	MonoMethod *assert;			/* SecurityManager.InternalAssert */
	MonoMethod *deny;			/* SecurityManager.InternalDeny */
	MonoMethod *permitonly;			/* SecurityManager.InternalPermitOnly */
	MonoMethod *linkdemand;			/* SecurityManager.LinkDemand */
	MonoMethod *inheritancedemand;		/* SecurityManager.InheritanceDemand */
} MonoSecurityManager;


/* Initialization/utility functions */
MonoSecurityManager* mono_security_manager_get_methods (void);


#endif /* _MONO_METADATA_SECURITY_MANAGER_H_ */
