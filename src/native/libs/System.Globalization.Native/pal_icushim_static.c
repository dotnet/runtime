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

#if defined(TARGET_IOS) || defined(TARGET_OSX) || defined(TARGET_WATCHOS) || defined(TARGET_TVOS)
    #define USE_APPLE_DECOMPRESSION
    #include <compression.h>
#endif

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

static void U_CALLCONV icu_trace_data(const void* context, int32_t fnNumber, int32_t level, const char* fmt, va_list args)
{
    char buf[1000];
    utrace_vformat(buf, sizeof(buf), 0, fmt, args);
    printf("[ICUDT] %s: %s\n", utrace_functionName(fnNumber), buf);
}

#ifdef __EMSCRIPTEN__
#include <emscripten.h>

static int32_t load_icu_data(void* pData);

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture);

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture)
{
    return GlobalizationNative_GetICUDTName(culture);
}

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(void* pData);

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_load_icu_data(void* pData)
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

static int32_t load_icu_data(void* pData)
{

    UErrorCode status = 0;
    udata_setCommonData(pData, &status);

    if (U_FAILURE(status)) {
        log_icu_error("udata_setCommonData", status);
        return 0;
    } else {

#if defined(ICU_TRACING)
        // see https://github.com/unicode-org/icu/blob/master/docs/userguide/icu_data/tracing.md
        utrace_setFunctions(0, 0, 0, icu_trace_data);
        utrace_setLevel(UTRACE_VERBOSE);
#endif
        isDataSet = 1;
        return 1;
    }
}

#if defined(USE_APPLE_DECOMPRESSION)
static int
ends_with(const char *buf, size_t buf_len, const char *query, size_t query_len)
{
    if (query_len > buf_len) {
        return 0;
    }
    return memcmp(buf + buf_len - query_len, query, query_len) == 0;
}

static char *
apple_decompress(const char *src_buf, size_t src_len)
{
    int cs_init = 0;
    compression_stream cs = { 0 };

    // Why this magic number? This is the uncompressed size of the mobile
    // icudt.dat as of 2022-02-05.
    size_t dst_capacity = 2176304;
    size_t dst_size = 0;
    size_t alloc_bytes = sizeof(uint8_t) * dst_capacity;
    uint8_t *dst_buf = malloc(alloc_bytes);
    if (dst_buf == NULL) {
        log_shim_error("apple_decompress: Failed to allocate %zu bytes for the ICU data decompression buffer.", alloc_bytes);
        goto error;
    }

    compression_status status = compression_stream_init(&cs, COMPRESSION_STREAM_DECODE, COMPRESSION_LZFSE);
    if (status == COMPRESSION_STATUS_ERROR) {
        log_shim_error("apple_decompress: Failed to initialize decompression stream.");
        goto error;
    }
    cs_init = 1;
    cs.src_ptr = (const uint8_t *) src_buf;
    cs.src_size = src_len;
    cs.dst_ptr = dst_buf;
    cs.dst_size = dst_capacity;

    int flags = 0;
    while (status == COMPRESSION_STATUS_OK) {
        size_t old_sz = cs.dst_size;
        status = compression_stream_process(&cs, flags);
        if (status == COMPRESSION_STATUS_ERROR) {
            log_shim_error("apple_decompress: Error while decompressing.");
            goto error;
        }

        size_t delta = old_sz - cs.dst_size;
        dst_size += delta;

        if (cs.dst_size == 0) {
            size_t old_sz = dst_capacity;
            size_t new_sz = (dst_capacity * 3) >> 1;
            uint8_t *next_dst_buf = realloc(dst_buf, new_sz);
            if (next_dst_buf == NULL) {
                log_shim_error("apple_decompress: Failed to allocate %zu bytes when resizing decompression buffer.", new_sz);
                goto error;
            }
            dst_buf = next_dst_buf;
            dst_capacity = new_sz;
            cs.dst_ptr = dst_buf + old_sz;
            cs.dst_size = new_sz - old_sz;
        }

        if (cs.src_size == 0) {
            flags = COMPRESSION_STREAM_FINALIZE;
        }
    }
    compression_stream_destroy(&cs);
    return (char *) dst_buf;

error:
    if (dst_buf != NULL) {
        free(dst_buf);
    }
    if (cs_init) {
        compression_stream_destroy(&cs);
    }
    return NULL;
}
#endif

int32_t GlobalizationNative_LoadICUData(const char* path)
{
    int32_t ret = -1;
    char *icu_data = NULL;

    char *file_buf = NULL;
    long file_buf_size = 0;
    char *uncompressed_file_buf = NULL;

    int use_lzfse = 0;
    #if defined(USE_APPLE_DECOMPRESSION)
    {
        size_t path_len = strlen(path);
        use_lzfse = ends_with(path, path_len, ".lzfse", sizeof(".lzfse") - 1);
    }
    #endif
    FILE *fp = fopen(path, "rb");

    if (fp == NULL) {
        log_shim_error("Unable to load ICU dat file '%s'.", path);
        goto error;
    }

    if (fseek(fp, 0L, SEEK_END) != 0) {
        log_shim_error("Unable to determine size of the dat file");
        goto error;
    }

    file_buf_size = ftell(fp);

    if (file_buf_size == -1) {
        log_shim_error("Unable to determine size of the ICU dat file.");
        goto error;
    }

    file_buf = malloc(sizeof(char) * (file_buf_size + 1));

    if (file_buf == NULL) {
        log_shim_error("Unable to allocate enough to read the ICU dat file");
        goto error;
    }

    if (fseek(fp, 0L, SEEK_SET) != 0) {
        log_shim_error("Unable to seek ICU dat file.");
        goto error;
    }

    fread(file_buf, sizeof(char), file_buf_size, fp);
    if (ferror( fp ) != 0 ) {
        log_shim_error("Unable to read ICU dat file");
        goto error;
    }

    fclose(fp);
    fp = NULL;

    #if defined(USE_APPLE_DECOMPRESSION)
    if (use_lzfse) {
        uncompressed_file_buf = apple_decompress(file_buf, file_buf_size);
        if (uncompressed_file_buf == NULL) {
            goto error;
        }
        icu_data = uncompressed_file_buf;
        free(file_buf);
        file_buf = NULL;
    }
    #endif
    if (icu_data == NULL) {
        icu_data = file_buf;
    }

    if (load_icu_data(icu_data) == 0) {
        log_shim_error("ICU BAD EXIT %d.", ret);
        return ret;
    }

    return GlobalizationNative_LoadICU();

error:
    if (fp != NULL) {
        fclose(fp);
    }
    if (file_buf != NULL) {
        free(file_buf);
    }
    if (uncompressed_file_buf == NULL) {
        free(uncompressed_file_buf);
    }
    return ret;
}

const char* GlobalizationNative_GetICUDTName(const char* culture)
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
