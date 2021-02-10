/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Rodrigo Kumpera
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>

#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/verify.h>

GSList*
mono_method_verify (MonoMethod *method, int level)
{
	/* The verifier was disabled at compile time */
	return NULL;
}

void
mono_free_verify_list (GSList *list)
{
	/* The verifier was disabled at compile time */
	/* will always be null if verifier is disabled */
}


char*
mono_verify_corlib (void)
{
        return NULL;
}
