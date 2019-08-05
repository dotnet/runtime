/**
 * \file
 * System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Authors:
 *      Mark Crichton (crichton@gimp.org)
 *      Patrik Torstensson (p@rxc.se)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>

#include "object.h"
#include "object-internals.h"
#include "rand.h"
#include "utils/mono-rand.h"
#include "icall-decl.h"

#ifndef ENABLE_NETCORE

MonoBoolean
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (MonoError *error)
{
	return (MonoBoolean) mono_rand_open ();
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (const guchar *seed, gssize seed_length, MonoError *error)
{
	return mono_rand_init (seed, seed_length);
}

gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, guchar *array, gssize array_length, MonoError *error)
{
	g_assert (array || !array_length);
	mono_rand_try_get_bytes (&handle, array, array_length, error);
	return handle;
}

void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle, MonoError *error)
{
	mono_rand_close (handle);
}

#else

MONO_EMPTY_SOURCE_FILE (rand);

#endif /* ENABLE_NETCORE */
