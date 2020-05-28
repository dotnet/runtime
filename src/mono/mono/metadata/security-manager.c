/**
 * \file
 * Security Manager (Unmanaged side)
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright 2005-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "security-manager.h"
#include "class-init.h"

/* Class lazy loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (security_manager, "System.Security", "SecurityManager")

static MonoSecurityMode mono_security_mode = MONO_SECURITY_MODE_NONE;

void
mono_security_set_mode (MonoSecurityMode mode)
{
	mono_security_mode = mode;
}

MonoSecurityMode
mono_security_get_mode (void)
{
	return mono_security_mode;
}

#ifndef DISABLE_SECURITY

static MonoSecurityManager secman;

MonoSecurityManager*
mono_security_manager_get_methods (void)
{
	/* Already initialized ? */
	if (secman.securitymanager)
		return &secman;

	/* Initialize */
	secman.securitymanager = mono_class_get_security_manager_class ();
	if (!m_class_is_inited (secman.securitymanager))
		mono_class_init_internal (secman.securitymanager);

	return &secman;
}

#else

MonoSecurityManager*
mono_security_manager_get_methods (void)
{
	return NULL;
}

#endif /* DISABLE_SECURITY */

/*
 * @publickey	An encoded (with header) public key
 * @size	The length of the public key
 *
 * returns TRUE if the public key is the ECMA "key", FALSE otherwise
 *
 * ECMA key isn't a real public key - it's simply an empty (but valid) header
 * so it's length (16) and value (00000000000000000400000000000000) are 
 * constants.
 */
gboolean 
mono_is_ecma_key (const char *publickey, int size)
{
	int i;
	if ((publickey == NULL) || (size != MONO_ECMA_KEY_LENGTH) || (publickey [8] != 0x04))
		return FALSE;

	for (i=0; i < size; i++) {
		if ((publickey [i] != 0x00) && (i != 8))
			return FALSE;
	}
	return TRUE;
}

/* System.Security icalls */

MonoBoolean
ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void)
{
	/* SecurityManager is internal for Moonlight and SecurityEnabled is used to know if CoreCLR is active
	 * (e.g. plugin executing in the browser) or not (e.g. smcs compiling source code with corlib 2.1)
	 */
	return (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR);
}

void
ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value)
{
}
