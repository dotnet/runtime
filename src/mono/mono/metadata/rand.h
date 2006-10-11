/*
 * rand.h: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 * Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
 */

#ifndef _MONO_METADATA_RAND_H_
#define _MONO_METADATA_RAND_H_

#include <mono/metadata/object.h>
#include "mono/utils/mono-compiler.h"

MonoBoolean ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngOpen (void) MONO_INTERNAL;
gpointer ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngInitialize (MonoArray *seed) MONO_INTERNAL;
gpointer ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngGetBytes (gpointer handle, MonoArray *arry) MONO_INTERNAL;
void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_RngClose (gpointer handle) MONO_INTERNAL;

#endif
