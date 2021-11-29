// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdbool.h>
#include <stdlib.h>
#include "pal_icushim_internal.h"

#if defined(TARGET_UNIX)
#include <dlfcn.h>
#elif defined(TARGET_WINDOWS)
#include <windows.h>
#include <libloaderapi.h>
#include <errhandlingapi.h>
#endif
#include <stdio.h>
#include <string.h>
#include <assert.h>

#include "pal_icushim.h"

// Define pointers to all the used ICU functions
#define PER_FUNCTION_BLOCK(fn, lib, required) TYPEOF(fn)* fn##_ptr;
FOR_ALL_ICU_FUNCTIONS
#undef PER_FUNCTION_BLOCK

// 35 for the actual suffix, 1 for _ and 1 for '\0'
#define SYMBOL_CUSTOM_SUFFIX_SIZE 37
#define SYMBOL_NAME_SIZE (128 + SYMBOL_CUSTOM_SUFFIX_SIZE)
#define MaxICUVersionStringWithSuffixLength (MaxICUVersionStringLength + SYMBOL_CUSTOM_SUFFIX_SIZE)


#if defined(TARGET_WINDOWS) || defined(TARGET_OSX) || defined(TARGET_ANDROID)

#define MaxICUVersionStringLength 33

#endif

static void* libicuuc = NULL;
static void* libicui18n = NULL;
ucol_setVariableTop_func ucol_setVariableTop_ptr = NULL;

#if defined (TARGET_UNIX)

#define PER_FUNCTION_BLOCK(fn, lib, required) \
    c_static_assert_msg((sizeof(#fn) + MaxICUVersionStringWithSuffixLength + 1) <= sizeof(symbolName), "The symbolName is too small for symbol " #fn); \
    sprintf(symbolName, #fn "%s", symbolVersion); \
    fn##_ptr = (TYPEOF(fn)*)dlsym(lib, symbolName); \
    if (fn##_ptr == NULL && required) { fprintf(stderr, "Cannot get symbol %s from " #lib "\nError: %s\n", symbolName, dlerror()); abort(); }

static int FindSymbolVersion(int majorVer, int minorVer, int subVer, char* symbolName, char* symbolVersion, char* suffix)
{
    // Find out the format of the version string added to each symbol
    // First try just the unversioned symbol
    if (dlsym(libicuuc, "u_strlen") == NULL)
    {
        // Now try just the _majorVer added
        sprintf(symbolVersion, "_%d%s", majorVer, suffix);
        sprintf(symbolName, "u_strlen%s", symbolVersion);
        if (dlsym(libicuuc, symbolName) == NULL)
        {
            if (minorVer == -1)
                return false;

            // Now try the _majorVer_minorVer added
            sprintf(symbolVersion, "_%d_%d%s", majorVer, minorVer, suffix);
            sprintf(symbolName, "u_strlen%s", symbolVersion);
            if (dlsym(libicuuc, symbolName) == NULL)
            {
                if (subVer == -1)
                    return false;

                // Finally, try the _majorVer_minorVer_subVer added
                sprintf(symbolVersion, "_%d_%d_%d%s", majorVer, minorVer, subVer, suffix);
                sprintf(symbolName, "u_strlen%s", symbolVersion);
                if (dlsym(libicuuc, symbolName) == NULL)
                {
                    return false;
                }
            }
        }
    }

    return true;
}

#endif // TARGET_UNIX

#if defined(TARGET_WINDOWS)

#define sscanf sscanf_s

#define PER_FUNCTION_BLOCK(fn, lib, required) \
    sprintf_s(symbolName, SYMBOL_NAME_SIZE, #fn "%s", symbolVersion); \
    fn##_ptr = (TYPEOF(fn)*)GetProcAddress((HMODULE)lib, symbolName); \
    if (fn##_ptr == NULL && required) { fprintf(stderr, "Cannot get symbol %s from " #lib "\nError: %u\n", symbolName, GetLastError()); abort(); }

static int FindICULibs()
{
    libicuuc = LoadLibraryExW(L"icu.dll", NULL, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (libicuuc == NULL)
    {
        return false;
    }

    // Windows has a single dll for icu.
    libicui18n = libicuuc;
    return true;
}

static int FindSymbolVersion(int majorVer, int minorVer, int subVer, char* symbolName, char* symbolVersion, char* suffix)
{
    HMODULE lib = (HMODULE)libicuuc;
    // Find out the format of the version string added to each symbol
    // First try just the unversioned symbol
    if (GetProcAddress(lib, "u_strlen") == NULL)
    {
        // Now try just the _majorVer added
        sprintf_s(symbolVersion, MaxICUVersionStringWithSuffixLength,"_%d%s", majorVer, suffix);
        sprintf_s(symbolName, SYMBOL_NAME_SIZE, "u_strlen%s", symbolVersion);
        if (GetProcAddress(lib, symbolName) == NULL)
        {
            if (minorVer == -1)
                return false;

            // Now try the _majorVer_minorVer added
            sprintf_s(symbolVersion, MaxICUVersionStringWithSuffixLength, "_%d_%d%s", majorVer, minorVer, suffix);
            sprintf_s(symbolName, SYMBOL_NAME_SIZE, "u_strlen%s", symbolVersion);
            if (GetProcAddress(lib, symbolName) == NULL)
            {
                if (subVer == -1)
                    return false;
                // Finally, try the _majorVer_minorVer_subVer added
                sprintf_s(symbolVersion, MaxICUVersionStringWithSuffixLength, "_%d_%d_%d%s", majorVer, minorVer, subVer, suffix);
                sprintf_s(symbolName, SYMBOL_NAME_SIZE, "u_strlen%s", symbolVersion);
                if (GetProcAddress(lib, symbolName) == NULL)
                {
                    return false;
                }
            }
        }
    }

    return true;
}

#elif defined(TARGET_OSX)

static int FindICULibs()
{
#ifndef OSX_ICU_LIBRARY_PATH
    c_static_assert_msg(false, "The ICU Library path is not defined");
#endif // OSX_ICU_LIBRARY_PATH

    // Usually OSX_ICU_LIBRARY_PATH is "/usr/lib/libicucore.dylib"
    libicuuc = dlopen(OSX_ICU_LIBRARY_PATH, RTLD_LAZY);

    if (libicuuc == NULL)
    {
        return false;
    }

    // in OSX all ICU APIs exist in the same library libicucore.A.dylib
    libicui18n = libicuuc;

    return true;
}

#elif defined(TARGET_ANDROID)

// support ICU versions from 50-255
#define MinICUVersion 50
#define MaxICUVersion 255

static int FindICULibs(char* symbolName, char* symbolVersion)
{
    libicui18n = dlopen("libicui18n.so", RTLD_LAZY);

    if (libicui18n == NULL)
    {
        return false;
    }

    libicuuc = dlopen("libicuuc.so", RTLD_LAZY);

    if (libicuuc == NULL)
    {
        return false;
    }

    char symbolSuffix[SYMBOL_CUSTOM_SUFFIX_SIZE]="";
    for (int i = MinICUVersion; i <= MaxICUVersion; i++)
    {
        if (FindSymbolVersion(i, -1, -1, symbolName, symbolVersion, symbolSuffix))
        {
            return true;
        }
    }

    fprintf(stderr, "Cannot determine ICU version.");
    return false;
}

#else // !TARGET_WINDOWS && !TARGET_OSX && !TARGET_ANDROID

#define VERSION_PREFIX_NONE ""
#define VERSION_PREFIX_SUSE "suse"

// .[suse]x.x.x, considering the max number of decimal digits for each component
#define MaxICUVersionStringLength (sizeof(VERSION_PREFIX_SUSE) + 33)

// Version ranges to search for each of the three version components
// The rationale for major version range is that we support versions higher or
// equal to the version we are built against and less or equal to that version
// plus 30 to give us enough headspace. The ICU seems to version about twice
// a year.
// On some platforms (mainly Alpine Linux) we want to make our minimum version
// an earlier version than what we build that we know we support.
#define MinICUVersion  50
#define MaxICUVersion  (U_ICU_VERSION_MAJOR_NUM + 30)
#define MinMinorICUVersion  1
#define MaxMinorICUVersion  5
#define MinSubICUVersion 1
#define MaxSubICUVersion 5

// Get filename of an ICU library with the requested version in the name
// There are three possible cases of the version components values:
// 1. Only majorVer is not equal to -1 => result is baseFileName.majorver
// 2. Only majorVer and minorVer are not equal to -1 => result is baseFileName.majorver.minorVer
// 3. All components are not equal to -1 => result is baseFileName.majorver.minorVer.subver
static void GetVersionedLibFileName(const char* baseFileName, int majorVer, int minorVer, int subVer, const char* versionPrefix, char* result)
{
    assert(majorVer != -1);

    int nameLen = sprintf(result, "%s.%s%d", baseFileName, versionPrefix, majorVer);

    if (minorVer != -1)
    {
        nameLen += sprintf(result + nameLen, ".%d", minorVer);
        if (subVer != -1)
        {
            sprintf(result + nameLen, ".%d", subVer);
        }
    }
}

// Try to open the necessary ICU libraries
static int OpenICULibraries(int majorVer, int minorVer, int subVer, const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    char libicuucName[64];
    char libicui18nName[64];

    c_static_assert_msg(sizeof("libicuuc.so") + MaxICUVersionStringLength <= sizeof(libicuucName), "The libicuucName is too small");
    GetVersionedLibFileName("libicuuc.so", majorVer, minorVer, subVer, versionPrefix, libicuucName);

    c_static_assert_msg(sizeof("libicui18n.so") + MaxICUVersionStringLength <= sizeof(libicui18nName), "The libicui18nName is too small");
    GetVersionedLibFileName("libicui18n.so", majorVer, minorVer, subVer, versionPrefix, libicui18nName);

    libicuuc = dlopen(libicuucName, RTLD_LAZY);
    if (libicuuc != NULL)
    {
        char symbolSuffix[SYMBOL_CUSTOM_SUFFIX_SIZE]="";
        if (FindSymbolVersion(majorVer, minorVer, subVer, symbolName, symbolVersion, symbolSuffix))
        {
            libicui18n = dlopen(libicui18nName, RTLD_LAZY);
        }
        if (libicui18n == NULL)
        {
            dlclose(libicuuc);
            libicuuc = NULL;
        }
    }

    return libicuuc != NULL;
}

// Select libraries using the version override specified by the CLR_ICU_VERSION_OVERRIDE
// environment variable.
// The format of the string in this variable is majorVer[.minorVer[.subVer]] (the brackets
// indicate optional parts).
static int FindLibUsingOverride(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    char* versionOverride = getenv("CLR_ICU_VERSION_OVERRIDE");
    if (versionOverride != NULL)
    {
        int first = -1;
        int second = -1;
        int third = -1;

        int matches = sscanf(versionOverride, "%d.%d.%d", &first, &second, &third);
        if (matches > 0)
        {
            if (OpenICULibraries(first, second, third, versionPrefix, symbolName, symbolVersion))
            {
                return true;
            }
        }
    }

    return false;
}

// Search for library files with names including the major version.
static int FindLibWithMajorVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    // ICU packaging documentation (http://userguide.icu-project.org/packaging)
    // describes applications link against the major (e.g. libicuuc.so.54).

    // Select the highest supported version of ICU present on the local machine
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        if (OpenICULibraries(i, -1, -1, versionPrefix, symbolName, symbolVersion))
        {
            return true;
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major and minor version.
static int FindLibWithMajorMinorVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            if (OpenICULibraries(i, j, -1, versionPrefix, symbolName, symbolVersion))
            {
                return true;
            }
        }
    }

    return false;
}

// Select the highest supported version of ICU present on the local machine
// Search for library files with names including the major, minor and sub version.
static int FindLibWithMajorMinorSubVersion(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    for (int i = MaxICUVersion; i >= MinICUVersion; i--)
    {
        for (int j = MaxMinorICUVersion; j >= MinMinorICUVersion; j--)
        {
            for (int k = MaxSubICUVersion; k >= MinSubICUVersion; k--)
            {
                if (OpenICULibraries(i, j, k, versionPrefix, symbolName, symbolVersion))
                {
                    return true;
                }
            }
        }
    }

    return false;
}


static int FindICULibs(const char* versionPrefix, char* symbolName, char* symbolVersion)
{
    return FindLibUsingOverride(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorVersion(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorMinorVersion(versionPrefix, symbolName, symbolVersion) ||
           FindLibWithMajorMinorSubVersion(versionPrefix, symbolName, symbolVersion);
}

#endif

static void ValidateICUDataCanLoad()
{
    UVersionInfo version;
    UErrorCode err = U_ZERO_ERROR;
    ulocdata_getCLDRVersion(version, &err);

    if (U_FAILURE(err))
    {
        fprintf(stderr, "Could not load ICU data. UErrorCode: %d\n", err);
        abort();
    }
}

static void InitializeVariableMaxAndTopPointers(char* symbolVersion)
{
    if (ucol_setMaxVariable_ptr != NULL)
    {
        return;
    }

#if defined(TARGET_OSX) || defined(TARGET_ANDROID)
    // OSX and Android always run against ICU version which has ucol_setMaxVariable.
    // We shouldn't come here.
    assert(false);
#elif defined(TARGET_WINDOWS)
    char symbolName[SYMBOL_NAME_SIZE];
    sprintf_s(symbolName, SYMBOL_NAME_SIZE, "ucol_setVariableTop%s", symbolVersion);
    ucol_setVariableTop_ptr = (ucol_setVariableTop_func)GetProcAddress((HMODULE)libicui18n, symbolName);
#else
    char symbolName[SYMBOL_NAME_SIZE];
    sprintf(symbolName, "ucol_setVariableTop%s", symbolVersion);
    ucol_setVariableTop_ptr = (ucol_setVariableTop_func)dlsym(libicui18n, symbolName);
#endif // defined(TARGET_OSX) || defined(TARGET_ANDROID)

    if (ucol_setVariableTop_ptr == NULL)
    {
        fprintf(stderr, "Cannot get the symbols of ICU APIs ucol_setMaxVariable or ucol_setVariableTop.\n");
        abort();
    }
}

// GlobalizationNative_LoadICU
// This method get called from the managed side during the globalization initialization.
// This method shouldn't get called at all if we are running in globalization invariant mode
// return 0 if failed to load ICU and 1 otherwise
int32_t GlobalizationNative_LoadICU()
{
    char symbolName[SYMBOL_NAME_SIZE];
    char symbolVersion[MaxICUVersionStringLength + 1]="";

#if defined(TARGET_WINDOWS) || defined(TARGET_OSX)

    if (!FindICULibs())
    {
        return false;
    }

#elif defined(TARGET_ANDROID)
    if (!FindICULibs(symbolName, symbolVersion))
    {
        return false;
    }
#else
    if (!FindICULibs(VERSION_PREFIX_NONE, symbolName, symbolVersion))
    {
        if (!FindICULibs(VERSION_PREFIX_SUSE, symbolName, symbolVersion))
        {
            return false;
        }
    }
#endif // TARGET_WINDOWS || TARGET_OSX

    FOR_ALL_ICU_FUNCTIONS
    ValidateICUDataCanLoad();

    InitializeVariableMaxAndTopPointers(symbolVersion);

    return true;
}

void GlobalizationNative_InitICUFunctions(void* icuuc, void* icuin, const char* version, const char* suffix)
{
    assert(icuuc != NULL);
    assert(icuin != NULL);
    assert(version != NULL);

    libicuuc = icuuc;
    libicui18n = icuin;
    int major = -1;
    int minor = -1;
    int build = -1;

    char symbolName[SYMBOL_NAME_SIZE];
    char symbolVersion[MaxICUVersionStringWithSuffixLength + 1]="";
    char symbolSuffix[SYMBOL_CUSTOM_SUFFIX_SIZE]="";

    if (strlen(version) > (size_t)MaxICUVersionStringLength)
    {
        fprintf(stderr, "The resolved version \"%s\" from System.Globalization.AppLocalIcu switch has to be < %zu chars long.\n", version, (size_t)MaxICUVersionStringLength);
        abort();
    }

    sscanf(version, "%d.%d.%d", &major, &minor, &build);

    if (suffix != NULL)
    {
        size_t suffixAllowedSize = SYMBOL_CUSTOM_SUFFIX_SIZE - 2; // SYMBOL_CUSTOM_SUFFIX_SIZE considers `_` and `\0`.
        if (strlen(suffix) > suffixAllowedSize)
        {
            fprintf(stderr, "The resolved suffix \"%s\" from System.Globalization.AppLocalIcu switch has to be < %zu chars long.\n", suffix, suffixAllowedSize);
            abort();
        }

        assert(strlen(suffix) + 1 <= SYMBOL_CUSTOM_SUFFIX_SIZE);

#if defined(TARGET_WINDOWS)
        sprintf_s(symbolSuffix, SYMBOL_CUSTOM_SUFFIX_SIZE, "_%s", suffix);
#else
        sprintf(symbolSuffix, "_%s", suffix);
#endif
    }

    if(!FindSymbolVersion(major, minor, build, symbolName, symbolVersion, symbolSuffix))
    {
        fprintf(stderr, "Could not find symbol: %s from libicuuc\n", symbolName);
        abort();
    }

    FOR_ALL_ICU_FUNCTIONS
    ValidateICUDataCanLoad();

    InitializeVariableMaxAndTopPointers(symbolVersion);
}

#undef PER_FUNCTION_BLOCK

// GlobalizationNative_GetICUVersion
// return the current loaded ICU version
int32_t GlobalizationNative_GetICUVersion()
{
    if (u_getVersion_ptr == NULL)
        return 0;

    UVersionInfo versionInfo;
    u_getVersion(versionInfo);

    return (versionInfo[0] << 24) + (versionInfo[1] << 16) + (versionInfo[2] << 8) + versionInfo[3];
}
