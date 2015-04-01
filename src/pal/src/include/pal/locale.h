//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    locale.h

Abstract:

    Prototypes for codepage initialization, and control of the readwrite locks
    for systems that use them.

Revision History:



--*/

#ifndef _PAL_LOCALE_H_
#define _PAL_LOCALE_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

#if HAVE_LOWERCASE_ISO_NAME
#define ISO_NAME(region, encoding, part)  region ".iso" encoding part
#elif HAVE_UNDERSCORE_ISO_NAME
#define ISO_NAME(region, encoding, part)  region ".ISO_" encoding "-" part
#else
#define ISO_NAME(region, encoding, part)  region ".ISO" encoding "-" part
#endif

#if HAVE_COREFOUNDATION
#define CF_EXCLUDE_CSTD_HEADERS
#include <CoreFoundation/CoreFoundation.h>
#endif  // HAVE_COREFOUNDATION

#if HAVE_COREFOUNDATION
#if !ENABLE_DOWNLEVEL_FOR_NLS
BOOL LocaleInitialize( void );
void LocaleCleanup( void );
#endif // !ENABLE_DOWNLEVEL_FOR_NLS

typedef
struct _CP_MAPPING
{
    UINT                nCodePage;      /* Code page identifier. */
    CFStringEncoding    nCFEncoding;    /* The equivalent CFString encoding. */
    UINT                nMaxByteSize;   /* The max byte size of any character. */
    BYTE                LeadByte[ MAX_LEADBYTES ];  /* The lead byte array. */
} CP_MAPPING;
#elif HAVE_PTHREAD_RWLOCK_T
typedef 
struct _CP_MAPPING
{
    UINT    nCodePage;                  // Code page identifier.
    LPCSTR  lpBSDEquivalent;            // The equivalent BSD locale identifier.
    UINT    nMaxByteSize;               // The max byte size of any character.
    BYTE    LeadByte[ MAX_LEADBYTES ];  // The lead byte array.
} CP_MAPPING;
#else
#error Insufficient platform support for text encodings
#endif
#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_LOCALE_H_ */
