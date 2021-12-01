/**
 * \file
 * System.Diagnostics.Process support
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef _MONO_METADATA_W32PROCESS_H_
#define _MONO_METADATA_W32PROCESS_H_

#include <config.h>
#include <glib.h>

#include <mono/utils/mono-compiler.h>

#if HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif

#ifndef HOST_WIN32

gchar*
mono_w32process_get_path (pid_t pid);

#endif
#endif /* _MONO_METADATA_W32PROCESS_H_ */
