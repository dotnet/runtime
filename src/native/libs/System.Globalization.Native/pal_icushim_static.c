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

#if !defined(APPLE_HYBRID_GLOBALIZATION)
#include <unicode/utrace.h>
#endif

#if defined(TARGET_UNIX)
#include <strings.h>
#elif defined(TARGET_WINDOWS)
#define strcasecmp _stricmp
#define strncasecmp _strnicmp
#endif
#if !defined(TARGET_MACCATALYST) && !defined(TARGET_IOS) && !defined(TARGET_TVOS)
static int32_t isLoaded = 0;
static int32_t isDataSet = 0;

static void log_shim_error(const char* format, ...)
{
    va_list args;

    va_start(args, format);
    vfprintf(stderr, format, args);
    fputc('\n', stderr);
    va_end(args);
}

static void log_icu_error(const char* name, UErrorCode status)
{
    const char * statusText = u_errorName(status);
    log_shim_error("ICU call %s failed with error #%d '%s'.", name, status, statusText);
}

#if defined(ICU_TRACING)
static void U_CALLCONV icu_trace_data(const void* context, int32_t fnNumber, int32_t level, const char* fmt, va_list args)
{
    char buf[1000];
    utrace_vformat(buf, sizeof(buf), 0, fmt, args);
    printf("[ICUDT] %s: %s\n", utrace_functionName(fnNumber), buf);
}
#endif

static int32_t load_icu_data(const void* pData);

#ifdef __EMSCRIPTEN__
#include <emscripten.h>

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(const void* pData);

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(const void* pData)
{
    return load_icu_data(pData);
}

/*
 * driver.c calls this to make sure this file is linked, otherwise
 * its not, meaning the EMSCRIPTEN_KEEPALIVE functions above
 * are not kept.
 */
void mono_wasm_link_icu_shim(void);

void mono_wasm_link_icu_shim(void)
{
}

#endif

int32_t mono_wasi_load_icu_data(const void* pData);

int32_t mono_wasi_load_icu_data(const void* pData)
{
    return load_icu_data(pData);
}

static int32_t load_icu_data(const void* pData)
{

    UErrorCode status = 0;
    udata_setCommonData(pData, &status);

    if (U_FAILURE(status))
    {
        log_icu_error("udata_setCommonData", status);
        return 0;
    }
    else
    {

#if defined(ICU_TRACING)
        // see https://github.com/unicode-org/icu/blob/master/docs/userguide/icu_data/tracing.md
        utrace_setFunctions(0, 0, 0, icu_trace_data);
        utrace_setLevel(UTRACE_VERBOSE);
#endif
        isDataSet = 1;
        return 1;
    }
}

static const char *
cstdlib_load_icu_data(const char *path)
{
    char *file_buf = NULL;
    FILE *fp = fopen(path, "rb");

    if (fp == NULL)
    {
        log_shim_error("Unable to load ICU dat file '%s'.", path);
        goto error;
    }

    if (fseek(fp, 0L, SEEK_END) != 0)
    {
        log_shim_error("Unable to determine size of the dat file");
        goto error;
    }

    long file_buf_size = ftell(fp);

    if (file_buf_size == -1)
    {
        log_shim_error("Unable to determine size of the ICU dat file.");
        goto error;
    }

    file_buf = malloc(sizeof(char) * (unsigned long)(file_buf_size + 1));

    if (file_buf == NULL)
    {
        log_shim_error("Unable to allocate enough to read the ICU dat file");
        goto error;
    }

    if (fseek(fp, 0L, SEEK_SET) != 0)
    {
        log_shim_error("Unable to seek ICU dat file.");
        goto error;
    }

    fread(file_buf, sizeof(char), (unsigned long)file_buf_size, fp);
    if (ferror( fp ) != 0)
    {
        log_shim_error("Unable to read ICU dat file");
        goto error;
    }

    fclose(fp);
    fp = NULL;

    return file_buf;

error:
    if (fp != NULL)
    {
        fclose(fp);
    }
    if (file_buf != NULL)
    {
        free(file_buf);
    }
    return NULL;
}

int32_t
GlobalizationNative_LoadICUData(const char* path)
{
#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)
    if (path && path[0] != '/')
    {
        // if the path is relative, prepend the app bundle root
        path = GlobalizationNative_GetICUDataPathRelativeToAppBundleRoot(path);
    }
    if (!path)
    {
        // fallback to icudt.dat in the app bundle resources in case the path isn't set
        path = GlobalizationNative_GetICUDataPathFallback();
    }
#endif

    const char *icu_data = cstdlib_load_icu_data(path);

    if (icu_data == NULL)
    {
        log_shim_error("Failed to load ICU data.");
        return 0;
    }

    if (load_icu_data(icu_data) == 0)
    {
        log_shim_error("ICU BAD EXIT.");
        return 0;
    }

    return GlobalizationNative_LoadICU();
}

int32_t GlobalizationNative_LoadICU(void)
{
#if !defined(LOCAL_BUILD)
// Static NativeAOT compilation does not have
// GlobalizationNative_LoadICUData() as entrypoint
    if (!isDataSet)
    {
        // don't try to locate icudt.dat automatically if mono_wasm_load_icu_data wasn't called
        // and fallback to invariant mode
        return 0;
    }
#endif

    UErrorCode status = 0;
    UVersionInfo version;
    // Request the CLDR version to perform basic ICU initialization and find out
    // whether it worked.
    ulocdata_getCLDRVersion(version, &status);

    if (U_FAILURE(status))
    {
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
#endif

