/*
 * security-core-clr.h: CoreCLR security
 *
 * Author:
 *	Mark Probst <mark.probst@gmail.com>
 *
 * (C) 2007 Novell, Inc
 */

#ifndef _MONO_METADATA_SECURITY_CORE_CLR_H_
#define _MONO_METADATA_SECURITY_CORE_CLR_H_

#include <mono/metadata/reflection.h>

typedef enum {
	/* We compare these values as integers, so the order must not
	   be changed. */
	MONO_SECURITY_CORE_CLR_TRANSPARENT = 0,
	MONO_SECURITY_CORE_CLR_SAFE_CRITICAL,
	MONO_SECURITY_CORE_CLR_CRITICAL
} MonoSecurityCoreCLRLevel;

extern gboolean mono_security_core_clr_test;

extern MonoSecurityCoreCLRLevel mono_security_core_clr_class_level (MonoClass *class) MONO_INTERNAL;
extern MonoSecurityCoreCLRLevel mono_security_core_clr_method_level (MonoMethod *method, gboolean with_class_level) MONO_INTERNAL;

extern gboolean mono_security_core_clr_is_platform_image (MonoImage *image) MONO_INTERNAL;

#endif	/* _MONO_METADATA_SECURITY_CORE_CLR_H_ */
