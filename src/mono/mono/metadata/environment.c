/**
 * \file
 * System.Environment support internal calls
 *
 * Authors:
 *	Dick Porter (dick@ximian.com)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/environment-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/w32api.h>
#if !defined(HOST_WIN32) && defined(HAVE_SYS_UTSNAME_H)
#include <sys/utsname.h>
#endif
#include "icall-decl.h"

static gint32 exitcode=0;

/**
 * mono_environment_exitcode_get:
 */
gint32
mono_environment_exitcode_get (void)
{
	return(exitcode);
}

/**
 * mono_environment_exitcode_set:
 */
void
mono_environment_exitcode_set (gint32 value)
{
	exitcode=value;
}

static int mini_argc = 0;
static char **mini_argv = NULL;

void
mono_set_os_args (int argc, char **argv)
{
	mini_argc = argc;
	mini_argv = argv;
}

char *
mono_get_os_cmd_line (void)
{
	return mono_runtime_get_cmd_line (mini_argc, mini_argv);
}

#ifndef ENABLE_NETCORE
/* note: we better manipulate the string in managed code (easier and safer) */
MonoStringHandle
ves_icall_System_Environment_GetOSVersionString (MonoError *error)
{
	error_init (error);
#ifdef HOST_WIN32
	MONO_DISABLE_WARNING (4996) // 'GetVersionExW': was declared deprecated
	OSVERSIONINFOEXW verinfo;
	verinfo.dwOSVersionInfoSize = sizeof (OSVERSIONINFOEX);
	if (GetVersionExW ((OSVERSIONINFO*)&verinfo)) {
		char version [128];
		/* maximum string length is 45 bytes
		   4 x 10 bytes per number, 1 byte for 0, 3 x 1 byte for dots, 1 for NULL */
		sprintf (version, "%ld.%ld.%ld.%d",
				 verinfo.dwMajorVersion,
				 verinfo.dwMinorVersion,
				 verinfo.dwBuildNumber,
				 verinfo.wServicePackMajor << 16);
		return mono_string_new_handle (mono_domain_get (), version, error);
	}
	MONO_RESTORE_WARNING
#elif defined(HAVE_SYS_UTSNAME_H) && defined(_AIX)
	/*
	 * AIX puts the major version number in .version and minor in .release; so make a
	 * version string based on that; other Unices seem to cram everything in .release
	 * and .version is for things like kernel variants.
	 */
	struct utsname name;
	char version [sizeof(name)];

	if (uname (&name) >= 0) {
		sprintf (version, "%s.%s", name.version, name.release);
		return mono_string_new_handle (mono_domain_get (), version, error);
	}
#elif defined(HAVE_SYS_UTSNAME_H)
	struct utsname name;

	memset (&name, 0, sizeof (name)); // WSL does not always nul terminate.

	if (uname (&name) >= 0) {
		return mono_string_new_handle (mono_domain_get (), name.release, error);
	}
#endif
	return mono_string_new_handle (mono_domain_get (), "0.0.0.0", error);
}
#endif
