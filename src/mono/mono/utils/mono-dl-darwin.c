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

#if defined (TARGET_MACH)

#include "mono/utils/mono-dl.h"
#include "mono/utils/mono-path.h"

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <dlfcn.h>
#include <unistd.h>
#include <mach-o/dyld.h>

const char *
mono_dl_get_so_prefix (void)
{
	return "lib";
}
const char **
mono_dl_get_so_suffixes (void)
{
	static const char *suffixes[] = {
		".dylib",
		".so",
		".bundle",
		"",
	};
	return suffixes;
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_dl_darwin);

#endif
