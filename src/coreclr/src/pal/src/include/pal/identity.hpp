//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
