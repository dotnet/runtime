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


/*
 * Fill actions for the specific index (which may either be an encoded class token or
 * an encoded method token) from the metadata image.
 * Returns TRUE if some actions requiring code generation are present, FALSE otherwise.
 */
void
mono_declsec_cache_stack_modifiers (MonoJitInfo *jinfo)
{
	/* first find the stack modifiers applied to the method */
	guint32 flags = mono_declsec_flags_from_method (jinfo->method);
	jinfo->cas_method_assert = (flags & MONO_DECLSEC_FLAG_ASSERT) != 0;
	jinfo->cas_method_deny = (flags & MONO_DECLSEC_FLAG_DENY) != 0;
	jinfo->cas_method_permitonly = (flags & MONO_DECLSEC_FLAG_PERMITONLY) != 0;

	/* then find the stack modifiers applied to the class */
	flags = mono_declsec_flags_from_class (jinfo->method->klass);
	jinfo->cas_class_assert = (flags & MONO_DECLSEC_FLAG_ASSERT) != 0;
	jinfo->cas_class_deny = (flags & MONO_DECLSEC_FLAG_DENY) != 0;
	jinfo->cas_class_permitonly = (flags & MONO_DECLSEC_FLAG_PERMITONLY) != 0;
}


MonoSecurityFrame*
mono_declsec_create_frame (MonoDomain *domain, MonoJitInfo *jinfo)
{
	MonoDeclSecurityEntry entry;
	MonoSecurityFrame *frame = (MonoSecurityFrame*) mono_object_new (domain, mono_defaults.runtimesecurityframe_class);

	if (!jinfo->cas_inited) {
		if (mono_method_has_declsec (jinfo->method)) {
			/* Cache the stack modifiers into the MonoJitInfo structure to speed up future stack walks */
			mono_declsec_cache_stack_modifiers (jinfo);
		}
		jinfo->cas_inited = TRUE;
	}

	frame->method = mono_method_get_object (domain, jinfo->method, NULL);

	/* stack modifiers on methods have priority on (i.e. replaces) modifiers on class */

	if (jinfo->cas_method_assert) {
		mono_declsec_get_method_action (jinfo->method, SECURITY_ACTION_ASSERT, &frame->assert);
	} else if (jinfo->cas_class_assert) {
		mono_declsec_get_class_action (jinfo->method->klass, SECURITY_ACTION_ASSERT, &frame->assert);
	}

	if (jinfo->cas_method_deny) {
		mono_declsec_get_method_action (jinfo->method, SECURITY_ACTION_DENY, &frame->deny);
	} else if (jinfo->cas_class_deny) {
		mono_declsec_get_class_action (jinfo->method->klass, SECURITY_ACTION_DENY, &frame->deny);
	}

	if (jinfo->cas_method_permitonly) {
		mono_declsec_get_method_action (jinfo->method, SECURITY_ACTION_PERMITONLY, &frame->permitonly);
	} else if (jinfo->cas_class_permitonly) {
		mono_declsec_get_class_action (jinfo->method->klass, SECURITY_ACTION_PERMITONLY, &frame->permitonly);
	}

	/* g_warning ("FRAME %s A(%p,%d) D(%p,%d) PO(%p,%d)", 
	jinfo->method->name, frame->assert.blob, frame->assert.size, frame->deny.blob, frame->deny.size, frame->permitonly.blob,frame->permitonly.size); */

	return frame;
}
