/**
 * \file
 * Interface to the dynamic linker
 *
 * Author:
 *    Mono Team (http://www.mono-project.com)
 *
 * Copyright 2001-2004 Ximian, Inc.
 * Copyright 2004-2009 Novell, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#if defined(_POSIX_VERSION) && !defined (HOST_WASM)

#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-embed.h"
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <dlfcn.h>

#if !defined (TARGET_MACH)
const char *
mono_dl_get_so_prefix (void)
{
	return "lib";
}
const char **
mono_dl_get_so_suffixes (void)
{
	static const char *suffixes[] = {
		".so",
		"",
	};
	return suffixes;
}

int
mono_dl_get_executable_path (char *buf, int buflen)
{
	return readlink ("/proc/self/exe", buf, buflen - 1);
}

const char*
mono_dl_get_system_dir (void)
{
	return NULL;
}

#endif

void *
mono_dl_open_file (const char *file, int flags)
{
#ifdef HOST_ANDROID
	/* Bionic doesn't support NULL filenames */
	if (!file)
		return NULL;
#endif
	return dlopen (file, flags);
}

void
mono_dl_close_handle (MonoDl *module)
{
	dlclose (module->handle);
}

void*
mono_dl_lookup_symbol (MonoDl *module, const char *name)
{
	return dlsym (module->handle, name);
}

int
mono_dl_convert_flags (int flags)
{
	int lflags = flags & MONO_DL_LOCAL? 0: RTLD_GLOBAL;

	if (flags & MONO_DL_LAZY)
		lflags |= RTLD_LAZY;
	else
		lflags |= RTLD_NOW;
	return lflags;
}

char*
mono_dl_current_error_string (void)
{
	return g_strdup (dlerror ());
}

#endif
