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
