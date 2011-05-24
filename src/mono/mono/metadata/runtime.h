/*
 * runtime.h: Runtime functions
 *
 * Author:
 *	Jonathan Pryor
 *
 * (C) 2010 Novell, Inc.
 */

#ifndef _MONO_METADATA_RUNTIME_H_
#define _MONO_METADATA_RUNTIME_H_

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

gboolean mono_runtime_is_critical_method (MonoMethod *method) MONO_INTERNAL;
void mono_runtime_shutdown (void) MONO_INTERNAL;

MONO_END_DECLS

#endif /* _MONO_METADATA_RUNTIME_H_ */


