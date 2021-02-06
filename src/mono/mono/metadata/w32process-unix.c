/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * Copyright 2002 Ximian, Inc.
 * Copyright 2002-2006 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <stdio.h>
#include <string.h>
#include <pthread.h>
#include <sched.h>
#include <sys/time.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <signal.h>
#include <sys/time.h>
#include <fcntl.h>
#ifdef HAVE_SYS_PARAM_H
#include <sys/param.h>
#endif
#include <ctype.h>

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>
#endif
#ifdef HAVE_SYS_RESOURCE_H
#include <sys/resource.h>
#endif

#ifdef HAVE_SYS_MKDEV_H
#include <sys/mkdev.h>
#endif

#ifdef HAVE_UTIME_H
#include <utime.h>
#endif

#if defined (HAVE_FORK) && defined (HAVE_EXECVE)
// For close_my_fds
#if defined (_AIX)
#include <procinfo.h>
#elif defined (__FreeBSD__)
#include <sys/sysctl.h>
#include <sys/user.h>
#include <libutil.h>
#elif defined(__linux__)
#include <dirent.h>
#endif
#endif

#include <mono/metadata/object-internals.h>
#include <mono/metadata/w32process.h>
#include <mono/metadata/w32process-internals.h>
#include <mono/metadata/w32process-unix-internals.h>
#include <mono/metadata/w32error.h>
#include <mono/metadata/class.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/w32file.h>
#include <mono/utils/mono-membar.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/strenc-internals.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/strenc.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/w32api.h>
#include <mono/utils/mono-errno.h>
#include <mono/utils/mono-once.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-threads-coop.h>
#include "object-internals.h"
#include "icall-decl.h"

void
mono_w32process_init (void)
{
}

void
mono_w32process_cleanup (void)
{
}

void
mono_w32process_set_cli_launcher (gchar *path)
{
}

void
mono_w32process_signal_finished (void)
{
}

guint32
mono_w32process_ver_language_name (guint32 lang, gunichar2 *lang_out, guint32 lang_len)
{
	return 0;
}

gboolean
mono_w32process_get_fileversion_info (const gunichar2 *filename, gpointer *data)
{
	return FALSE;
}

gboolean
mono_w32process_module_get_information (gpointer handle, gpointer module, gpointer modinfo, guint32 size)
{
	return FALSE;
}

gboolean
mono_w32process_ver_query_value (gconstpointer datablock, const gunichar2 *subblock, gpointer *buffer, guint32 *len)
{
	return FALSE;
}
