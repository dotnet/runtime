// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include "pal_icushim_internal.h"
#include "pal_icushim.h"
#include <unicode/putil.h>
#include <unicode/uversion.h>
#include <unicode/localpointer.h>
#include <unicode/utrace.h>

#if defined(TARGET_UNIX)
#include <strings.h>
#elif defined(TARGET_WINDOWS)
#define strcasecmp _stricmp
#define strncasecmp _strnicmp
#endif

static int32_t isLoaded = 0;
static int32_t isDataSet = 0;

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

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture);

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture)
{
    // Based on https://github.com/dotnet/icu/tree/maint/maint-67/icu-filters

    // Use full one if culture is null or empty
    if (!culture || strlen(culture) < 2)
        return "icudt.dat";

    // CJK: starts with "ja", "ko" or "zh"
    if (!strncasecmp("ja", culture, 2) || 
        !strncasecmp("ko", culture, 2) || 
        !strncasecmp("zh", culture, 2))
        return "icudt_CJK.dat"; // contains "en" as well.

    // EFIGS
    const char* efigsCultures[15] = {
        "en-US", "fr-FR", "es-ES", "it-IT", "de-DE",
        "en_US", "fr_FR", "es_ES", "it_IT", "de_DE",
        "en",    "fr",    "es",    "it",    "de"
    };

    for (int i = 0; i < 15; i++)
        if (!strcasecmp(culture, efigsCultures[i]))
            return "icudt_EFIGS.dat";

    // full except CJK cultures
    return "icudt_no_CJK.dat";
}

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
        isDataSet = 1;
        return 1;
    }
}

/*
 * driver.c calls this to make sure this file is linked, otherwise
 * its not, meaning the EMSCRIPTEN_KEEPALIVE functions above
 * are not kept.
 */
void mono_wasm_link_icu_shim(void)
{
}

#endif

int32_t GlobalizationNative_LoadICU(void)
{
    if (!isDataSet) {
        // don't try to locate icudt.dat automatically if mono_wasm_load_icu_data wasn't called
        // and fallback to invariant mode
        return 0;
    }
    UErrorCode status = 0;
    UVersionInfo version;
    // Request the CLDR version to perform basic ICU initialization and find out
    // whether it worked.
    ulocdata_getCLDRVersion(version, &status);

    if (U_FAILURE(status)) {
        log_icu_error("ulocdata_getCLDRVersion", status);
        return 0;
    }
    
    isLoaded = 1;
    return 1;
}

void GlobalizationNative_InitICUFunctions(void* icuuc, void* icuin, const char* version, const char* suffix)
{
    // no-op for static
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    // this method is only used from our tests
    // this way we ensure we're testing on the right mode
    // even though we can call u_getVersion without loading since it is statically linked.
    if (!isLoaded)
        return 0;

    UVersionInfo versionInfo;
    u_getVersion(versionInfo);

    return (versionInfo[0] << 24) + (versionInfo[1] << 16) + (versionInfo[2] << 8) + versionInfo[3];
}
