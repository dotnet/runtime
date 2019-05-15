/**
 * \file
 * File System Watcher internal calls
 *
 * Authors:
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * (C) 2004 Novell, Inc. (http://www.novell.com)
 */

#ifndef _MONO_METADATA_FILEWATCHER_H
#define _MONO_METADATA_FILEWATCHER_H

#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"
#include <glib.h>
#include <mono/metadata/icalls.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#if !ENABLE_NETCORE

ICALL_EXPORT
gint ves_icall_System_IO_FSW_SupportsFSW (void);

ICALL_EXPORT
gboolean ves_icall_System_IO_FAMW_InternalFAMNextEvent (gpointer conn,
							MonoString **filename,
							gint *code,
							gint *reqnum);
ICALL_EXPORT
int ves_icall_System_IO_KqueueMonitor_kevent_notimeout (int *kq, gpointer changelist, int nchanges, gpointer eventlist, int nevents);

#endif

#ifdef HOST_IOS // This will obsoleted by System.Native as soon as it's ported to iOS
MONO_API char* SystemNative_RealPath(const char* path);
MONO_API void SystemNative_Sync (void);
#endif

#endif
