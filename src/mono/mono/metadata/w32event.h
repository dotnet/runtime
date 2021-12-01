/**
 * \file
 */

#ifndef _MONO_METADATA_W32EVENT_H_
#define _MONO_METADATA_W32EVENT_H_

#include <config.h>
#include <glib.h>

#include "object.h"
#include "object-internals.h"
#include "w32handle.h"
#include <mono/metadata/icalls.h>
#include <mono/utils/mono-compiler.h>

void
mono_w32event_init (void);

MONO_COMPONENT_API
gpointer
mono_w32event_create (gboolean manual, gboolean initial);

MONO_COMPONENT_API
gboolean
mono_w32event_close (gpointer handle);

MONO_COMPONENT_API
void
mono_w32event_set (gpointer handle);

#endif /* _MONO_METADATA_W32EVENT_H_ */
