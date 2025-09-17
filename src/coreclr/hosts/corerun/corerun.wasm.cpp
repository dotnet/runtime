// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <malloc.h>
#include <string.h>
#include <fcntl.h>
#include <sys/stat.h>
#include <unistd.h>

#define _In_z_
#define _In_
#include "pinvokeoverride.h"

extern "C" const void* SystemResolveDllImport(const char* name);

// pinvoke_override:
// Check if given function belongs to one of statically linked libraries and return a pointer if found.
const void* pinvoke_override(const char* library_name, const char* entry_point_name)
{
    // This function is only called with the library name specified for a p/invoke, not any variations.
    // It must handle exact matches to the names specified. See Interop.Libraries.cs for each platform.
    if (strcmp(library_name, "libSystem.Native") == 0)
    {
        return SystemResolveDllImport(entry_point_name);
    }

    return nullptr;
}

void wasm_add_pinvoke_override()
{
    PInvokeOverride::SetPInvokeOverride(pinvoke_override, PInvokeOverride::Source::RuntimeConfiguration);
}

extern "C" int32_t mono_wasm_load_icu_data(const void* pData);

static char* _wasm_get_icu_dat_file_path(const char* assemblyPath)
{
    if (!assemblyPath || !*assemblyPath)
        return nullptr;

    const char* lastSlash = strrchr(assemblyPath, '/');
    const char* lastBackslash = strrchr(assemblyPath, '\\');

    const char* lastSeparator = nullptr;
    if (lastSlash && lastBackslash)
        lastSeparator = (lastSlash > lastBackslash) ? lastSlash : lastBackslash;
    else if (lastSlash)
        lastSeparator = lastSlash;
    else if (lastBackslash)
        lastSeparator = lastBackslash;

    const char icuFileName[] = "icudt.dat";

    if (!lastSeparator)
        return strdup(icuFileName);

    size_t dirLen = (size_t)(lastSeparator - assemblyPath) + 1; // include separator
    size_t fileNameLen = sizeof(icuFileName) - 1;
    
    char* icuPath = (char*)malloc(dirLen + fileNameLen + 1);
    if (!icuPath)
        return nullptr;

    memcpy(icuPath, assemblyPath, dirLen);
    memcpy(icuPath + dirLen, icuFileName, fileNameLen);
    icuPath[dirLen + fileNameLen] = '\0';

    return icuPath;
}

int32_t wasm_load_icu_data(const char* assemblyPath)
{
    char* icuFile = _wasm_get_icu_dat_file_path(assemblyPath);
    if (!icuFile) {
        return 0;
    }

    int fd = open(icuFile, O_RDONLY);
    if (fd < 0) {
        free(icuFile);
        return 0;
    }

    struct stat st;
    if (stat(icuFile, &st) != 0 || !S_ISREG(st.st_mode) || st.st_size <= 0) {
        close(fd);
        free(icuFile);
        return 0;
    }

    free(icuFile);

    size_t size = static_cast<size_t>(st.st_size);
    void* buffer = malloc(size);
    if (!buffer) {
        close(fd);
        return 0;
    }

    size_t total = 0;
    while (total < size) {
        ssize_t n = read(fd, static_cast<char*>(buffer) + total, size - total);
        if (n <= 0) {
            free(buffer);
            close(fd);
            return 0;
        }
        total += static_cast<size_t>(n);
    }
    close(fd);

    return mono_wasm_load_icu_data(buffer);
}
