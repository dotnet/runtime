/*
 * declsec.c:  Declarative Security support
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004 Novell, Inc (http://www.novell.com)
 */

#include "declsec.h"

/*
 * Does the methods (or it's class) as any declarative security attribute ?
 * Is so are they applicable ? (e.g. static class constructor)
 */
MonoBoolean
mono_method_has_declsec (MonoMethod *method)
{
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;
		
	if ((method->klass->flags & TYPE_ATTRIBUTE_HAS_SECURITY) || (method->flags & METHOD_ATTRIBUTE_HAS_SECURITY)) {
		/* ignore static constructors */
		if (strcmp (method->name, ".cctor"))
			return TRUE;
	}
	return FALSE;
}
