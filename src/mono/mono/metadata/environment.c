/*
 * environment.c: System.Environment support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/environment.h>

static gint32 exitcode=0;

gint32 mono_environment_exitcode_get (void)
{
	return(exitcode);
}

void mono_environment_exitcode_set (gint32 value)
{
	exitcode=value;
}
