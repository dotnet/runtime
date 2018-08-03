/**
 * \file
 * System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#ifndef _MONO_METADATA_RAND_H_
#define _MONO_METADATA_RAND_H_

#include <glib.h>
#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"
#include <mono/metadata/icalls.h>

ICALL_EXPORT
MonoBoolean
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (MonoError *error);

ICALL_EXPORT
gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (const guchar *seed, gssize seed_length, MonoError *error);

ICALL_EXPORT
gpointer
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, guchar *array, gssize array_length, MonoError *error);

ICALL_EXPORT
void
ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle, MonoError *error);

#endif
