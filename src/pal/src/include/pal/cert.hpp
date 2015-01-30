//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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
