/*
 * declsec.h:  Declarative Security support
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#ifndef _MONO_MINI_DECLSEC_H_
#define _MONO_MINI_DECLSEC_H_

#include <string.h>

#include "mono/metadata/class-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/object.h"
#include "mono/metadata/tabledefs.h"


/* Definitions */

typedef struct {
	MonoObject obj;
	MonoReflectionMethod *method;
	MonoDeclSecurityEntry assert;
	MonoDeclSecurityEntry deny;
	MonoDeclSecurityEntry permitonly;
} MonoSecurityFrame;


/* limited flags used in MonoJitInfo for stack modifiers */
enum {
	MONO_JITINFO_STACKMOD_ASSERT		= 0x01,
	MONO_JITINFO_STACKMOD_DENY		= 0x02,
	MONO_JITINFO_STACKMOD_PERMITONLY	= 0x04
};

/* Prototypes */
MonoBoolean mono_method_has_declsec (MonoMethod *method);
void mono_declsec_cache_stack_modifiers (MonoJitInfo *jinfo);
MonoSecurityFrame* mono_declsec_create_frame (MonoDomain *domain, MonoJitInfo *jinfo);

#endif /* _MONO_MINI_DECLSEC_H_ */
