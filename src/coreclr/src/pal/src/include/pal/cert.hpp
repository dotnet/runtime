// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/cert.hpp

Abstract:
    Header file for cert structures



--*/

#ifndef _PAL_CERT_HPP_
#define _PAL_CERT_HPP_

#include "corunix.hpp"

#include <Security/Security.h>

CorUnix::PAL_ERROR OIDToStr(CSSM_DATA &data, CHAR *&oidStrOut);

CSSM_RETURN InitCSSMModule(const CSSM_GUID *inGuid, CSSM_SERVICE_TYPE inService,
    CSSM_MODULE_HANDLE_PTR outModule);
CSSM_RETURN TermCSSMModule(const CSSM_GUID *inGuid, CSSM_MODULE_HANDLE_PTR inModule);

#endif // !_PAL_CERT_HPP_
