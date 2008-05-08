/*
 * declsec.h:  Declarative Security support
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004-2005 Novell (http://www.novell.com)
 */

#ifndef _MONO_MINI_DECLSEC_H_
#define _MONO_MINI_DECLSEC_H_

#include <string.h>

#include "mono/metadata/class-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/object.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/marshal.h"
#include "mono/metadata/security-manager.h"
#include "mono/metadata/exception.h"
#include "mono/utils/mono-compiler.h"


/* Definitions */

#define	MONO_CAS_INITIAL_STACK_SIZE		6


/* keep in synch with RuntimeSecurityFrame in /mcs/class/corlib/System.Security/SecurityFrame.cs */
typedef struct {
	MonoObject obj;
	MonoAppDomain *domain;
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

enum {
	MONO_JIT_SECURITY_OK			= 0x00,
	MONO_JIT_LINKDEMAND_PERMISSION		= 0x01,
	MONO_JIT_LINKDEMAND_APTC		= 0x02,
	MONO_JIT_LINKDEMAND_ECMA		= 0x04,
	MONO_JIT_LINKDEMAND_PINVOKE		= 0x08
};

/* Prototypes */
MonoBoolean mono_method_has_declsec (MonoMethod *method) MONO_INTERNAL;
void mono_declsec_cache_stack_modifiers (MonoJitInfo *jinfo) MONO_INTERNAL;
MonoSecurityFrame* mono_declsec_create_frame (MonoDomain *domain, MonoJitInfo *jinfo) MONO_INTERNAL;

guint32 mono_declsec_linkdemand (MonoDomain *domain, MonoMethod *caller, MonoMethod *callee) MONO_INTERNAL;

#endif /* _MONO_MINI_DECLSEC_H_ */
