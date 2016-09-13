
#ifndef _MONO_METADATA_W32MUTEX_H_
#define _MONO_METADATA_W32MUTEX_H_

#include <config.h>
#include <glib.h>

#include "object.h"
#include "w32handle-namespace.h"

void
mono_w32mutex_init (void);

gpointer
ves_icall_System_Threading_Mutex_CreateMutex_internal (MonoBoolean owned, MonoString *name, MonoBoolean *created);

MonoBoolean
ves_icall_System_Threading_Mutex_ReleaseMutex_internal (gpointer handle);

gpointer
ves_icall_System_Threading_Mutex_OpenMutex_internal (MonoString *name, gint32 rights, gint32 *error);

typedef struct MonoW32HandleNamedMutex MonoW32HandleNamedMutex;

MonoW32HandleNamespace*
mono_w32mutex_get_namespace (MonoW32HandleNamedMutex *mutex);

#endif /* _MONO_METADATA_W32MUTEX_H_ */
