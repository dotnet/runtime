// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdlib.h>
#include <stdio.h>
#include "pal_icushim_internal.h"
#include "pal_icushim.h"
#include <unicode/putil.h>
#include <unicode/uversion.h>

static void log_icu_error (const char * name, UErrorCode status) {
    const char * statusText = u_errorName(status);
    fprintf(stderr, "ICU call %s failed with error #%d '%s'.\n", name, status, statusText);
}

#ifdef __EMSCRIPTEN__
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data (void * pData);

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data (void * pData) {
    UErrorCode status = 0;
    udata_setCommonData (pData, &status);

    if (U_FAILURE(status)) {
        log_icu_error("udata_setCommonData", status);
        return 0;
    } else {
        return 1;
    }
}
#endif

int32_t GlobalizationNative_LoadICU(void)
{
    const char* icudir = getenv("DOTNET_ICU_DIR");
    if (icudir)
        u_setDataDirectory(icudir);
    else
        ; // default ICU search path behavior will be used, see http://userguide.icu-project.org/icudata

    UErrorCode status = 0;
    // Invoking u_init will probe to see if ICU common data is already available and if it is missing,
    //  attempt to load it from the local filesystem.
    u_init(&status);

    if (U_FAILURE(status)) {
        log_icu_error("u_init", status);
        return 0;
    }

    return 1;
}

void GlobalizationNative_InitICUFunctions(void* icuuc, void* icuin, const char* version, const char* suffix)
{
    // no-op for static
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    UVersionInfo versionInfo;
    u_getVersion(versionInfo);

    return (versionInfo[0] << 24) + (versionInfo[1] << 16) + (versionInfo[2] << 8) + versionInfo[3];
}
