/*
 * declsec.c:  Declarative Security support
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 */

#include "declsec.h"

/*
 * Does the methods (or it's class) as any declarative security attribute ?
 * Is so are they applicable ? (e.g. static class constructor)
 */
MonoBoolean
mono_method_has_declsec (MonoMethod *method)
{
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
		method = mono_marshal_method_from_wrapper (method);
		if (!method)
			return FALSE;
	} else if (method->wrapper_type != MONO_WRAPPER_NONE)
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


/*
 * Execute any LinkDemand, NonCasLinkDemand, LinkDemandChoice declarative
 * security attribute present on the called method or it's class.
 *
 * @domain	The current application domain
 * @caller	The method calling
 * @callee	The called method.
 * return value: TRUE if a security violation is detection, FALSE otherwise.
 *
 * Note: The execution is done in managed code in SecurityManager.LinkDemand
 */
static gboolean
mono_declsec_linkdemand_standard (MonoDomain *domain, MonoMethod *caller, MonoMethod *callee)
{
	MonoDeclSecurityActions linkclass, linkmethod;

	if (mono_declsec_get_linkdemands (callee, &linkclass, &linkmethod)) {
		MonoAssembly *assembly = mono_image_get_assembly (caller->klass->image);
		MonoReflectionAssembly *refass = (MonoReflectionAssembly*) mono_assembly_get_object (domain, assembly);
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		MonoObject *res;
		gpointer args [3];

		args [0] = refass;
		args [1] = &linkclass;
		args [2] = &linkmethod;

		res = mono_runtime_invoke (secman->linkdemand, NULL, args, NULL);
		return !(*(MonoBoolean *) mono_object_unbox(res));
	}
	return FALSE;
}

/*
 * Ensure that the restrictions for partially trusted code are satisfied.
 *
 * @domain	The current application domain
 * @caller	The method calling
 * @callee	The called method
 * return value: TRUE if a security violation is detection, FALSE otherwise.
 *
 * If callee's assembly is strongnamed and doesn't have an 
 * [AllowPartiallyTrustedCallers] attribute then we must enforce a LinkDemand
 * for FullTrust on all public/protected methods on public class.
 *
 * Note: APTC is only effective on stongnamed assemblies.
 */
static gboolean
mono_declsec_linkdemand_aptc (MonoDomain *domain, MonoMethod *caller, MonoMethod *callee)
{
	MonoSecurityManager* secman;
	MonoAssembly *assembly;
	guint32 size = 0;

	/* A - Applicable only if we're calling into *another* assembly */
	if (caller->klass->image == callee->klass->image)
		return FALSE;

	/* B - Applicable if we're calling a public/protected method from a public class */
	if (!(callee->klass->flags & TYPE_ATTRIBUTE_PUBLIC) || !(callee->flags & FIELD_ATTRIBUTE_PUBLIC))
		return FALSE;

	/* C - Applicable if the callee's assembly is strongnamed */
	if ((mono_image_get_public_key (callee->klass->image, &size) == NULL) || (size < MONO_ECMA_KEY_LENGTH))
		return FALSE;

	/* D - the callee's assembly must have [AllowPartiallyTrustedCallers] */
	assembly = mono_image_get_assembly (callee->klass->image);
	if (!MONO_SECMAN_FLAG_INIT (assembly->aptc)) {
		MonoCustomAttrInfo* cinfo = mono_custom_attrs_from_assembly (assembly);
		secman = mono_security_manager_get_methods ();
		gboolean result = FALSE;
		if (secman && cinfo) {
			/* look for AllowPartiallyTrustedCallersAttribute */
			result = mono_custom_attrs_has_attr (cinfo, secman->aptc);
		}
		MONO_SECMAN_FLAG_SET_VALUE (assembly->aptc, result);
	}

	if (MONO_SECMAN_FLAG_GET_VALUE (assembly->aptc))
		return FALSE;

	/* E - the caller's assembly must have full trust permissions */
	if (!MONO_SECMAN_FLAG_INIT (assembly->fulltrust)) {
		MonoReflectionAssembly *refass = (MonoReflectionAssembly*) mono_assembly_get_object (domain, assembly);
		if (!secman)
			secman = mono_security_manager_get_methods ();

		if (secman && refass) {
			MonoObject *res;
			gpointer args [1];
			args [0] = refass;

			res = mono_runtime_invoke (secman->linkdemandfulltrust, NULL, args, NULL);
			if (*(MonoBoolean *) mono_object_unbox(res)) {
				/* keep this value cached as it will be used very often */
				MONO_SECMAN_FLAG_SET_VALUE (assembly->fulltrust, TRUE);
				return FALSE;
			}
		}

		MONO_SECMAN_FLAG_SET_VALUE (assembly->fulltrust, FALSE);
	}

	if (MONO_SECMAN_FLAG_GET_VALUE (assembly->fulltrust))
		return FALSE;

	/* g_warning ("FAILURE *** JIT LinkDemand APTC check *** %s.%s calls into %s.%s",
		caller->klass->name, caller->name, callee->klass->name, callee->name); */

	return TRUE;	/* i.e. throw new SecurityException(); */
}

/*
 * Ensure that the restrictions for calling internal calls are satisfied.
 *
 * @caller	The method calling
 * @icall	The internal call method
 * return value: TRUE if a security violation is detection, FALSE otherwise.
 *
 * Executing internal calls (icalls) is a restricted operation. Only 
 * assemblies with an ECMA public key are allowed to call them (unless they
 * are publicly available).
 *
 * This LinkDemand case is special because it can be done without calling 
 * managed code.
 */
static gboolean
mono_declsec_linkdemand_ecma (MonoMethod *caller, MonoMethod *icall)
{
	MonoAssembly *assembly;

	/* some icall are public (i.e. they CAN be called by any code) */
	if (((icall->klass->flags & TYPE_ATTRIBUTE_PUBLIC) == TYPE_ATTRIBUTE_PUBLIC) &&
		((icall->flags & FIELD_ATTRIBUTE_PUBLIC) == FIELD_ATTRIBUTE_PUBLIC)) {
		return FALSE;
	}

	assembly = mono_image_get_assembly (icall->klass->image);
	if (!MONO_SECMAN_FLAG_INIT (assembly->ecma)) {
		guint32 size = 0;
		const char *pk = mono_image_get_public_key (caller->klass->image, &size);
		MONO_SECMAN_FLAG_SET_VALUE (assembly->ecma, mono_is_ecma_key (pk, size));
	}

	if (MONO_SECMAN_FLAG_GET_VALUE (assembly->ecma))
		return FALSE;

	/* g_warning ("FAILURE *** JIT LinkDemand for ECMA check *** %s.%s calls into %s.%s", 
		caller->klass->name, caller->name, icall->klass->name, icall->name); */

	return TRUE;	/* i.e. throw new SecurityException(); */
}

/*
 * Ensure that the restrictions for calling native code are satisfied.
 *
 * @domain	The current application domain
 * @caller	The method calling
 * @native	The native method called
 * return value: TRUE if a security violation is detection, FALSE otherwise.
 *
 * Executing Platform Invokes (P/Invoke) is a is a restricted operation.
 * The security policy must allow (SecurityPermissionFlag.UnmanagedCode)
 * an assembly to do this.
 *
 * This LinkDemand case is special because it only needs to call managed
 * code once per assembly. Further calls on this assembly will use a cached
 * flag for better performance. This is not done before the first call (e.g.
 * when loading the assembly) because that would break the lazy policy
 * evaluation that Mono use (another time saving optimization).
 *
 * Note: P/Invoke checks are ALWAYS (1) done at JIT time (as a LinkDemand). 
 * They are also checked at runtime, using a Demand (stack walk), unless the 
 * method or it's class has a [SuppressUnmanagedCodeSecurity] attribute.
 *
 * (1) well as long as the security manager is active (i.e. --security)
 */
static gboolean
mono_declsec_linkdemand_pinvoke (MonoDomain *domain, MonoMethod *caller, MonoMethod *native)
{
	MonoAssembly *assembly = mono_image_get_assembly (caller->klass->image);

	/* Check for P/Invoke flag for the assembly */
	if (!MONO_SECMAN_FLAG_INIT (assembly->unmanaged)) {
		/* Check if we know (and have) or FullTrust status */
		if (MONO_SECMAN_FLAG_INIT (assembly->fulltrust) && MONO_SECMAN_FLAG_GET_VALUE (assembly->fulltrust)) {
			/* FullTrust includes UnmanagedCode permission */
			MONO_SECMAN_FLAG_SET_VALUE (assembly->unmanaged, TRUE);
		} else {
			MonoReflectionAssembly *refass = (MonoReflectionAssembly*) mono_assembly_get_object (domain, assembly);
			MonoSecurityManager* secman = mono_security_manager_get_methods ();
			if (secman && refass) {
				MonoObject *res;
				gpointer args [1];
				args [0] = refass;

				res = mono_runtime_invoke (secman->linkdemandunmanaged, NULL, args, NULL);
				if (*(MonoBoolean *) mono_object_unbox(res)) {
					MONO_SECMAN_FLAG_SET_VALUE (assembly->unmanaged, TRUE);
					return FALSE;
				}
			}
		}

		MONO_SECMAN_FLAG_SET_VALUE (assembly->unmanaged, FALSE);
	}

	if (MONO_SECMAN_FLAG_GET_VALUE (assembly->unmanaged))
		return FALSE;

	/* g_warning ("FAILURE *** JIT LinkDemand P/Invoke check *** %s.%s calls into %s.%s",
		caller->klass->name, caller->name, native->klass->name, native->name); */

	return TRUE;	/* i.e. throw new SecurityException(); */
}


/*
 * Before the JIT can link (call) into a method the following security checks
 * must be done:
 *
 * We check that the code has the permission to link when:
 * 1. the code try to call an internal call;
 * 2. the code try to p/invoke to unmanaged code;
 * 3. the code try to call trusted code without being trusted itself -
 *    or without the trusted code permission (APTC);
 * 4. the code try to call managed code protected by a LinkDemand security 
 *    attribute
 *
 * Failures result in a SecurityException being thrown (later in mini code).
 *
 * Note: Some checks are duplicated in managed code to deal when reflection is
 * used to call the methods.
 */
guint32
mono_declsec_linkdemand (MonoDomain *domain, MonoMethod *caller, MonoMethod *callee)
{
	guint32 violation = MONO_JIT_SECURITY_OK;
	/* first, the special (implied) linkdemand */

	if (callee->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
		/* restrict calls into the runtime to ECMA signed assemblies */
		if (mono_declsec_linkdemand_ecma (caller, callee))
			violation = MONO_JIT_LINKDEMAND_ECMA;
	} else if (callee->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		/* CAS can restrict p/invoke calls with the assembly granted permissions */
		if (mono_declsec_linkdemand_pinvoke (domain, caller, callee))
			violation = MONO_JIT_LINKDEMAND_PINVOKE;
	}

	if (!violation) {
		/* check if we allow partially trusted callers in trusted (signed) assemblies */
		if (mono_declsec_linkdemand_aptc (domain, caller, callee))
			violation = MONO_JIT_LINKDEMAND_APTC;
	}

	/* then the "normal" LinkDemand (only when called method has declarative security) */
	if (!violation && mono_method_has_declsec (callee)) {
		/* LinkDemand are ignored for static constructors (ensured by calling mono_method_has_declsec) */
		if (mono_declsec_linkdemand_standard (domain, caller, callee))
			violation = MONO_JIT_LINKDEMAND_PERMISSION;
	}

	/* if (violation) g_warning ("mono_declsec_linkdemand violation reported %d", violation); */
	return violation;
}
