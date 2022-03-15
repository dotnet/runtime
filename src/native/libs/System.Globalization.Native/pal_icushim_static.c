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
    #include <compression.h> // compression_stream_init, compression_stream_process, compression_stream_destroy
    #include <dirent.h> // fdopendir, readdir, closedir
    #include <fcntl.h> // open
    #include <inttypes.h> // PRIu64
    #include <limits.h> // PATH_MAX
    #include <sys/errno.h> // errno
    #include <sys/mman.h> // mmap, munmap
    #include <sys/stat.h> // fstat
    #include <sysdir.h> // sysdir_start_search_path_enumeration, sysdir_get_next_search_path_enumeration
    #include <unistd.h> // write
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

static int32_t load_icu_data(const void* pData);

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture);

EMSCRIPTEN_KEEPALIVE const char* mono_wasm_get_icudt_name(const char* culture)
{
    return GlobalizationNative_GetICUDTName(culture);
}

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

#if defined(USE_APPLE_DECOMPRESSION)
static int
apple_cache_directory(char path[static (PATH_MAX + 1)])
{
    char buf[PATH_MAX + 1];
    sysdir_search_path_enumeration_state st = sysdir_start_search_path_enumeration(SYSDIR_DIRECTORY_CACHES, SYSDIR_DOMAIN_MASK_USER);
    st = sysdir_get_next_search_path_enumeration(st, buf);
    if (!st)
    {
        log_shim_error("apple_cache_directory: sysdir_get_next_search_path_enumeration did not yield a path");
        return -1;
    }
    size_t path_len = strlen(buf);
    size_t ret_len = path_len + 2 /* '/' + '\0' */;
    const char *home = "";
    size_t home_len = 0;
    if (buf[0] == '~' && buf[1] == '/') {
        home = getenv("HOME");
        if (home == NULL)
        {
            log_shim_error("apple_cache_directory: cache directory begins with ~ but $HOME is not set");
            return -1;
        }
        home_len = strlen(home);
        ret_len += home_len + 1;
    }
    snprintf(path, PATH_MAX, "%s%s", home, &buf[1]);
    return 0;
}

#define APPLE_DECOMPRESSION_BUF_SIZE 16384

static int
apple_decompress_to_fd(int dst_fd, size_t *dst_len, const char *src_buf, size_t src_len)
{
    int cs_init = 0;
    compression_stream cs = { 0 };
    uint8_t buf[APPLE_DECOMPRESSION_BUF_SIZE];
    size_t dst_size = 0;

    compression_status status = compression_stream_init(&cs, COMPRESSION_STREAM_DECODE, COMPRESSION_LZFSE);
    if (status == COMPRESSION_STATUS_ERROR)
    {
        log_shim_error("apple_decompress_to_fd: Failed to initialize decompression stream.");
        goto error;
    }
    cs_init = 1;
    cs.src_ptr = (const uint8_t *) src_buf;
    cs.src_size = src_len;
    cs.dst_ptr = buf;
    cs.dst_size = APPLE_DECOMPRESSION_BUF_SIZE;

    int flags = 0;
    while (status == COMPRESSION_STATUS_OK)
    {
        status = compression_stream_process(&cs, flags);
        if (status == COMPRESSION_STATUS_ERROR)
        {
            log_shim_error("apple_decompress_to_fd: Error while decompressing.");
            goto error;
        }

        size_t bytes_to_write = APPLE_DECOMPRESSION_BUF_SIZE - cs.dst_size;
        uint8_t *write_cursor = buf;
        dst_size += bytes_to_write;
        while (bytes_to_write > 0) {
            ssize_t bytes_written = write(dst_fd, write_cursor, bytes_to_write);
            int last_error = errno;
            if (bytes_written == -1)
            {
                if (last_error == EINTR) continue;
                log_shim_error("apple_decompress_to_fd: Error during write().");
                goto error;
            }
            else if (bytes_written == 0)
            {
                log_shim_error("apple_decompress_to_fd: write() returned 0.");
                goto error;
            }
            bytes_to_write -= (size_t) bytes_written;
            write_cursor += bytes_written;
        }

        cs.dst_ptr = buf;
        cs.dst_size = APPLE_DECOMPRESSION_BUF_SIZE;

        if (cs.src_size == 0)
        {
            flags = COMPRESSION_STREAM_FINALIZE;
        }
    }
    compression_stream_destroy(&cs);
    if (dst_len != NULL)
    {
        *dst_len = dst_size;
    }
    return 1;

error:
    if (cs_init)
    {
        compression_stream_destroy(&cs);
    }
    return 0;
}

static int
apple_clean_stale_cache_files(DIR *d, const char *current_cache_filename)
{
    int dir_fd = dirfd(d);
    struct dirent *de = NULL;
    rewinddir(d);
    while ((de = readdir(d)) != NULL)
    {
        const char *fn = de->d_name;
        if (strcmp(fn, current_cache_filename) != 0)
        {
            if (strstr(fn, "-icudt.dat.decompressed") != NULL)
            {
                unlinkat(dir_fd, fn, 0);
            }
        }
    }
    return 0;
}

#define APPLE_TMPFILE_NAME_SIZE 128

static const char *
apple_mmap_icu_data(const char *path)
{
    const char *ret = NULL;
    DIR *dirp = NULL;
    int cache_fd = -1;
    int src_fd = open(path, O_RDONLY | O_CLOEXEC);
    if (src_fd == -1)
    {
        log_shim_error("apple_mmap_icu_data: failed to open %s", path);
        goto error;
    }

    struct stat src_st;
    int result = fstat(src_fd, &src_st);
    if (result == -1)
    {
        log_shim_error("apple_mmap_icu_data: failed to fstat %s", path);
        goto error;
    }

    char tmp_file[APPLE_TMPFILE_NAME_SIZE];
    size_t src_size = src_st.st_size;
    size_t icu_data_size = 0;
    const char *last_period = strrchr(path, '.');
    const char *icu_data_name = NULL;
    if (last_period && strcmp(last_period, ".lzfse") == 0)
    {
        char *cache_file = tmp_file;

        uint64_t pid = (uint64_t) getpid();
        int written = snprintf(tmp_file, APPLE_TMPFILE_NAME_SIZE, "%" PRIu64 ".", pid);
        if (written < 0)
        {
            log_shim_error("apple_mmap_icu_data: failed to generate tmpfile PID prefix");
            goto error;
        }
        cache_file += written;
        written = snprintf(cache_file, APPLE_TMPFILE_NAME_SIZE - written, "%" PRIu64 "-%" PRIu64 "-%" PRIu64 "-icudt.dat.decompressed",
            (uint64_t) src_st.st_ino,
            (uint64_t) src_st.st_size,
            (uint64_t) src_st.st_mtimespec.tv_sec);
        if (written < 0)
        {
            log_shim_error("apple_mmap_icu_data: failed to generate cache file name");
            goto error;
        }

        char cache_dir[PATH_MAX + 1];
        result = apple_cache_directory(cache_dir);
        if (result == -1)
        {
            goto error;
        }
        int dir_fd = open(cache_dir, O_DIRECTORY | O_RDONLY | O_CLOEXEC);
        if (dir_fd == -1)
        {
            log_shim_error("apple_mmap_icu_data: failed to open directory %s", cache_dir);
            goto error;
        }
        dirp = fdopendir(dir_fd);
        apple_clean_stale_cache_files(dirp, cache_file);

        cache_fd = openat(dir_fd, cache_file, O_RDONLY | O_CLOEXEC);
        if (cache_fd == -1)
        {
            cache_fd = openat(dir_fd, tmp_file, O_RDWR | O_CREAT, 0640);
            if (cache_fd == -1)
            {
                log_shim_error("apple_mmap_icu_data: failed to open %s/%s for writing", cache_dir, tmp_file);
                goto error;
            }
            const char *src_mem = mmap(NULL, src_size, PROT_READ, MAP_SHARED, src_fd, 0);
            if (src_mem == MAP_FAILED)
            {
                log_shim_error("apple_mmap_icu_data: failed to map %s with size %zu", path, src_size);
                goto error;
            }
            size_t decompressed_data_size = 0;
            int result = apple_decompress_to_fd(cache_fd, &decompressed_data_size, src_mem, src_st.st_size);
            munmap((void *) src_mem, src_st.st_size);
            fsync(cache_fd);
            if (!result)
            {
                goto error;
            }
            result = renameat(dir_fd, tmp_file, dir_fd, cache_file);
            fsync(dir_fd);
            if (result == -1)
            {
                log_shim_error("apple_mmap_icu_data: failed to rename %s to %s", tmp_file, cache_file);
                goto error;
            }
        }
        close(src_fd);
        src_fd = -1;
        struct stat cache_st;
        result = fstat(cache_fd, &cache_st);
        if (result == -1)
        {
            log_shim_error("apple_mmap_icu_data: failed to fstat %s", cache_file);
            goto error;
        }
        icu_data_size = cache_st.st_size;
        icu_data_name = cache_file;
    }
    else
    {
        cache_fd = src_fd;
        src_fd = -1;
        icu_data_size = src_size;
        icu_data_name = path;
    }
    const char *cache_mem = mmap(NULL, icu_data_size, PROT_READ, MAP_SHARED, cache_fd, 0);
    if (cache_mem == MAP_FAILED)
    {
        log_shim_error("apple_mmap_icu_data: failed to map %s with size %zu", icu_data_name, icu_data_size);
        goto error;
    }
    ret = cache_mem;
error:
    if (dirp != 0)
    {
        closedir(dirp);
    }
    if (src_fd >= 0)
    {
        close(src_fd);
    }
    if (cache_fd >= 0)
    {
        close(cache_fd);
    }
    return ret;
}
#else
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

    file_buf = malloc(sizeof(char) * (file_buf_size + 1));

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

    fread(file_buf, sizeof(char), file_buf_size, fp);
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
#endif

int32_t
GlobalizationNative_LoadICUData(const char* path)
{
    const char *icu_data =
        #if defined(USE_APPLE_DECOMPRESSION)
        apple_mmap_icu_data(path)
        #else
        cstdlib_load_icu_data(path)
        #endif
        ;

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
    if (!isDataSet)
    {
        // don't try to locate icudt.dat automatically if mono_wasm_load_icu_data wasn't called
        // and fallback to invariant mode
        return 0;
    }
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
