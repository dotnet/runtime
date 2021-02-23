// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <Security/Security.h>

#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
/*
Enumerate the certificates which are root trusted by the user.

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t AppleCryptoNative_StoreEnumerateUserRoot(CFArrayRef* pCertsOut, int32_t* pOSStatusOut);

/*
Enumerate the certificates which are root trusted by the machine ("admin" and "system" domains).

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Duplicate certificates may be reported by this function, if they are trusted at both the admin and
system levels.  De-duplication is the responsibility of the caller.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t AppleCryptoNative_StoreEnumerateMachineRoot(CFArrayRef* pCertsOut, int32_t* pOSStatusOut);

/*
Enumerate the certificates which are disallowed by the user.

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t AppleCryptoNative_StoreEnumerateUserDisallowed(CFArrayRef* pCertsOut, int32_t* pOSStatusOut);

/*
Enumerate the certificates which are disallowed by the machine ("admin" and "system" domains).

Returns 1 on success (including "no certs found"), 0 on failure, any other value for invalid state.

Duplicate certificates may be reported by this function, if they are disallowed at both the admin
and system levels.  De-duplication is the responsibility of the caller.

Output:
pCertsOut: When the return value is not 1, NULL. Otherwise NULL on "no certs found", or a CFArrayRef for the matches
(including a single match).
pOSStatus: Receives the last OSStatus value.
*/
PALEXPORT int32_t AppleCryptoNative_StoreEnumerateMachineDisallowed(CFArrayRef* pCertsOut, int32_t* pOSStatusOut);
#endif
