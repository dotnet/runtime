
#ifndef __MONO_METADATA_FDHANDLE_H__
#define __MONO_METADATA_FDHANDLE_H__

#include <config.h>
#include <glib.h>

#include "utils/refcount.h"

typedef enum {
	MONO_FDTYPE_FILE,
	MONO_FDTYPE_CONSOLE,
	MONO_FDTYPE_PIPE,
	MONO_FDTYPE_SOCKET,
	MONO_FDTYPE_COUNT
} MonoFDType;

typedef struct {
	MonoRefCount ref;
	MonoFDType type;
	gint fd;
} MonoFDHandle;

typedef struct {
	void (*close) (MonoFDHandle *fdhandle);
	void (*destroy) (MonoFDHandle *fdhandle);
} MonoFDHandleCallback;

void
mono_fdhandle_register (MonoFDType type, MonoFDHandleCallback *callback);

void
mono_fdhandle_init (MonoFDHandle *fdhandle, MonoFDType type, gint fd);

void
mono_fdhandle_insert (MonoFDHandle *fdhandle);

gboolean
mono_fdhandle_try_insert (MonoFDHandle *fdhandle);

gboolean
mono_fdhandle_lookup_and_ref (gint fd, MonoFDHandle **fdhandle);

void
mono_fdhandle_unref (MonoFDHandle *fdhandle);

gboolean
mono_fdhandle_close (gint fd);

#endif /* __MONO_METADATA_FDHANDLE_H__ */
