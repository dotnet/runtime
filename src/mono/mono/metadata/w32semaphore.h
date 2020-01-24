/**
 * \file
 */

#ifndef _MONO_METADATA_W32SEMAPHORE_H_
#define _MONO_METADATA_W32SEMAPHORE_H_

#include <config.h>
#include <glib.h>
#include "object.h"
#include "w32handle-namespace.h"
#include <mono/metadata/icalls.h>

void
mono_w32semaphore_init (void);

typedef struct MonoW32HandleNamedSemaphore MonoW32HandleNamedSemaphore;

MonoW32HandleNamespace*
mono_w32semaphore_get_namespace (MonoW32HandleNamedSemaphore *semaphore);

#endif /* _MONO_METADATA_W32SEMAPHORE_H_ */
