/*
 * rand.h: System.Security.Cryptography.RNGCryptoServiceProvider support
 *
 * Author:
 *      Mark Crichton (crichton@gimp.org)
 *
 * (C) 2001 Ximian, Inc.
 *
 */

#ifndef _MONO_METADATA_RAND_H_
#define _MONO_METADATA_RAND_H_

#include <mono/metadata/object.h>

void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_GetBytes(MonoArray *arry);
void ves_icall_System_Security_Cryptography_RNGCryptoServiceProvider_GetNonZeroBytes(MonoArray *arry);

#endif
