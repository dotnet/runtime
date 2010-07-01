/*
 * filewatcher.h: File System Watcher internal calls
 *
 * Authors:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2004 Novell, Inc. (http://www.novell.com)
 */

#ifndef _MONO_METADATA_FILEWATCHER_H
#define _MONO_METADATA_FILEWATCHER_H

#include <mono/metadata/object.h>
#include <mono/io-layer/io-layer.h>
#include "mono/utils/mono-compiler.h"
#include <glib.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HAVE_SYS_SYSCALL_H
#include <sys/syscall.h>
#endif

G_BEGIN_DECLS

gint ves_icall_System_IO_FSW_SupportsFSW (void) MONO_INTERNAL;

gboolean ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
							MonoString **filename,
							gint *code,
							gint *reqnum) MONO_INTERNAL;

int ves_icall_System_IO_InotifyWatcher_GetInotifyInstance (void) MONO_INTERNAL;
int ves_icall_System_IO_InotifyWatcher_AddWatch (int fd, MonoString *directory, gint32 mask) MONO_INTERNAL;
int ves_icall_System_IO_InotifyWatcher_RemoveWatch (int fd, gint32 watch_descriptor) MONO_INTERNAL;

G_END_DECLS

#endif

