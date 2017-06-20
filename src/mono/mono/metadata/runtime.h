/**
 * \file
 * Runtime functions
 *
 * Author:
 *	Jonathan Pryor
 *
 * (C) 2010 Novell, Inc.
 */

#ifndef _MONO_METADATA_RUNTIME_H_
#define _MONO_METADATA_RUNTIME_H_

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>

MONO_BEGIN_DECLS

gboolean mono_runtime_try_shutdown (void);

void mono_runtime_init_tls (void);

char* mono_runtime_get_aotid (void);
MONO_END_DECLS

#endif /* _MONO_METADATA_RUNTIME_H_ */


