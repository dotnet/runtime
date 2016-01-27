// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/identity.hpp

Abstract:

    Header file for identity functions.



--*/

#ifndef _PAL_IDENTITY_HPP_
#define _PAL_IDENTITY_HPP_

#include "config.h"
#include "pal/palinternal.h"

/*++

Function:
  IdentityInitialize

--*/
BOOL IdentityInitialize();

/*++
Function:
  IdentityCleanup

--*/
VOID IdentityCleanup();

#if HAVE_GETPWUID_R
namespace CorUnix
{   
    int
    InternalGetpwuid_r(
        CPalThread *pPalThread,
        uid_t uid,
        struct passwd *pPasswd,
        char *pchBuffer,
        size_t nBufSize,
        struct passwd **ppResult
        ); 
}
#endif /* HAVE_GETPWUID_R */

#endif /* _PAL_IDENTITY_HPP_ */
