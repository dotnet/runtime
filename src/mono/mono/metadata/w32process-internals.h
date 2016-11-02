
#ifndef _MONO_METADATA_W32PROCESS_INTERNALS_H_
#define _MONO_METADATA_W32PROCESS_INTERNALS_H_

#include <config.h>
#include <glib.h>

#include "io-layer/io-layer.h"

#if !defined(HOST_WIN32)

guint32
mono_w32process_get_pid (gpointer handle);

gboolean
mono_w32process_try_get_modules (gpointer process, gpointer *modules, guint32 size, guint32 *needed);

guint32
mono_w32process_module_get_name (gpointer process, gpointer module, gunichar2 *basename, guint32 size);

guint32
mono_w32process_module_get_filename (gpointer process, gpointer module, gunichar2 *basename, guint32 size);

gboolean
mono_w32process_module_get_information (gpointer process, gpointer module, MODULEINFO *modinfo, guint32 size);

#endif /* !defined(HOST_WIN32) */

#endif /* _MONO_METADATA_W32PROCESS_INTERNALS_H_ */
