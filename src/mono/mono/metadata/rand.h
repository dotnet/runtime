/*
 * rand.h: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *	Sebastien Pouliot (sebastien@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 * (C) 2004 Novell (http://www.novell.com)
 *
 */

#ifndef _MONO_METADATA_RAND_H_
#define _MONO_METADATA_RAND_H_

#include <mono/metadata/object.h>

void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_InternalGetBytes (MonoObject *self, MonoArray *arry);
void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_Seed (MonoArray *seed);

#endif
