/*
 * security-manager.c:  Security Manager (Unmanaged side)
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 */

#include "security-manager.h"


/* Internal stuff */

static MonoSecurityManager secman;
static MonoBoolean mono_security_manager_activated = FALSE;
static MonoBoolean mono_security_manager_enabled = TRUE;
static MonoBoolean mono_security_manager_execution = TRUE;


/* Public stuff */

MonoSecurityManager*
mono_security_manager_get_methods (void)
{
	/* Already initialized ? */
	if (secman.securitymanager)
		return &secman;

	/* Initialize */
	secman.securitymanager = mono_class_from_name (mono_defaults.corlib, 
		"System.Security", "SecurityManager");
	g_assert (secman.securitymanager);
	if (!secman.securitymanager->inited)
		mono_class_init (secman.securitymanager);
		
	secman.demand = mono_class_get_method_from_name (secman.securitymanager,
		"InternalDemand", 2);	
	g_assert (secman.demand);

	secman.inheritancedemand = mono_class_get_method_from_name (secman.securitymanager,
		"InheritanceDemand", 3);	
	g_assert (secman.inheritancedemand);

	secman.linkdemand = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemand", 3);
	g_assert (secman.linkdemand);

	secman.linkdemandunmanaged = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandUnmanaged", 1);
	g_assert (secman.linkdemandunmanaged);

	secman.linkdemandfulltrust = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandFullTrust", 1);
	g_assert (secman.linkdemandfulltrust);

	secman.linkdemandsecurityexception = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemandSecurityException", 3);
	g_assert (secman.linkdemandsecurityexception);

	secman.aptc = mono_class_from_name (mono_defaults.corlib, "System.Security", 
		"AllowPartiallyTrustedCallersAttribute");
	g_assert (secman.aptc);

	return &secman;
}

/*
 * Note: The security manager is activate once when executing the Mono. This 
 * is not meant to be a turn on/off runtime switch.
 */
void
mono_activate_security_manager (void)
{
	mono_security_manager_activated = TRUE;
}

gboolean
mono_is_security_manager_active (void)
{
	return mono_security_manager_activated;
}

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
mono_is_ecma_key (char *publickey, int size)
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

MonoBoolean
ves_icall_System_Security_SecurityManager_get_SecurityEnabled (void)
{
	if (!mono_security_manager_activated)
		return FALSE;
	return mono_security_manager_enabled;
}

void
ves_icall_System_Security_SecurityManager_set_SecurityEnabled (MonoBoolean value)
{
	/* value can be changed only if the security manager is activated */
	if (mono_security_manager_activated) {
		mono_security_manager_enabled = value;
	}
}

MonoBoolean
ves_icall_System_Security_SecurityManager_get_CheckExecutionRights (void)
{
	if (!mono_security_manager_activated)
		return FALSE;
	return mono_security_manager_execution;
}

void
ves_icall_System_Security_SecurityManager_set_CheckExecutionRights (MonoBoolean value)
{
	/* value can be changed only id the security manager is activated */
	if (mono_security_manager_activated) {
		mono_security_manager_execution = value;
	}
}
