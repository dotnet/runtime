/**
 * \file
 * Security Manager
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
#include <mono/metadata/icalls.h>

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
} MonoSecurityMode;

/* Structures */

typedef struct {
	MonoClass *securitymanager;		/* System.Security.SecurityManager */
} MonoSecurityManager;

gboolean mono_is_ecma_key (const char *publickey, int size);

MonoSecurityManager* mono_security_manager_get_methods (void);

/* Security mode */
void mono_security_set_mode (MonoSecurityMode mode);
MonoSecurityMode mono_security_get_mode (void);

/* internal calls */
ICALL_EXPORT
MonoBoolean ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void);

ICALL_EXPORT
void ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value);

#ifndef DISABLE_SECURITY
#define mono_security_core_clr_enabled() (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
#else
#define mono_security_core_clr_enabled() (FALSE)
#endif

#endif /* _MONO_METADATA_SECURITY_MANAGER_H_ */
