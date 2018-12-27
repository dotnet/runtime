/**
 * \file
 */

#ifndef _MONO_METADATA_W32MUTEX_H_
#define _MONO_METADATA_W32MUTEX_H_

#include <config.h>
#include <glib.h>

#include "object.h"
#include "object-internals.h"
#include "w32handle-namespace.h"
#include <mono/metadata/icalls.h>

void
mono_w32mutex_init (void);

ICALL_EXPORT
MonoBoolean
ves_icall_System_Threading_Mutex_ReleaseMutex_internal (gpointer handle);

typedef struct MonoW32HandleNamedMutex MonoW32HandleNamedMutex;

MonoW32HandleNamespace*
mono_w32mutex_get_namespace (MonoW32HandleNamedMutex *mutex);

#ifndef HOST_WIN32
void
mono_w32mutex_abandon (MonoInternalThread *internal);
#endif

#endif /* _MONO_METADATA_W32MUTEX_H_ */
