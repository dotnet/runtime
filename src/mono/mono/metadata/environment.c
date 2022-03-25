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
