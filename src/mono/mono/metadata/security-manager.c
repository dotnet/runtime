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
	secman.inheritancedemand = mono_class_get_method_from_name (secman.securitymanager,
		"InheritanceDemand", 2);	
	secman.linkdemand = mono_class_get_method_from_name (secman.securitymanager,
		"LinkDemand", 9);

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
