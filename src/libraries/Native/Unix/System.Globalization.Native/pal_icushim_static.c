// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdlib.h>
#include <stdio.h>
#include "pal_icushim_internal.h"
#include "pal_icushim.h"
#include <unicode/putil.h>
#include <unicode/uversion.h>
#include <unicode/localpointer.h>
#include <unicode/utrace.h>

static void log_icu_error(const char* name, UErrorCode status)
{
    const char * statusText = u_errorName(status);
    fprintf(stderr, "ICU call %s failed with error #%d '%s'.\n", name, status, statusText);
}

static void U_CALLCONV icu_trace_data(const void* context, int32_t fnNumber, int32_t level, const char* fmt, va_list args)
{
    char buf[1000];
    utrace_vformat(buf, sizeof(buf), 0, fmt, args);
    printf("[ICUDT] %s: %s\n", utrace_functionName(fnNumber), buf);
}

#ifdef __EMSCRIPTEN__
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(void * pData);

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(void * pData)
{
    UErrorCode status = 0;
    udata_setCommonData(pData, &status);

    if (U_FAILURE(status)) {
        log_icu_error("udata_setCommonData", status);
        return 0;
    } else {
        //// Uncomment to enable ICU tracing,
        //// see https://github.com/unicode-org/icu/blob/master/docs/userguide/icu_data/tracing.md
        // utrace_setFunctions(0, 0, 0, icu_trace_data);
        // utrace_setLevel(UTRACE_VERBOSE);
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
    UVersionInfo version;
    // Request the CLDR version to perform basic ICU initialization and find out
    // whether it worked.
    ulocdata_getCLDRVersion(version, &status);

    if (U_FAILURE(status)) {
        log_icu_error("ulocdata_getCLDRVersion", status);
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
