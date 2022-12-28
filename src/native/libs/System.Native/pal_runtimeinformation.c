// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_runtimeinformation.h"
#include "pal_types.h"
#include <assert.h>
#include <stdio.h>
#include <string.h>
#include <sys/utsname.h>
#if defined(TARGET_ANDROID)
#include <sys/system_properties.h>
#elif defined(TARGET_OSX)
#include <sys/sysctl.h>
#elif defined(TARGET_SUNOS)
#include <sys/systeminfo.h>
#endif

char* SystemNative_GetUnixRelease(void)
{
#if defined(TARGET_ANDROID)
    // get the Android API level
    char sdk_ver_str[PROP_VALUE_MAX];
    if (__system_property_get("ro.build.version.sdk", sdk_ver_str))
    {
        return strdup(sdk_ver_str);
    }
    else
    {
        return NULL;
    }
#else
    struct utsname _utsname;
    return uname(&_utsname) != -1 ?
        strdup(_utsname.release) :
        NULL;
#endif
}

int32_t SystemNative_GetUnixVersion(char* version, int* capacity)
{
    struct utsname _utsname;
    if (uname(&_utsname) != -1)
    {
        int r = snprintf(version, (size_t)(*capacity), "%s %s %s", _utsname.sysname, _utsname.release, _utsname.version);
        if (r > *capacity)
        {
            *capacity = r + 1;
            return -1;
        }
    }

    return 0;
}

// Keep in sync with System.Runtime.InteropServices.Architecture enum
enum
{
    ARCH_X86,
    ARCH_X64,
    ARCH_ARM,
    ARCH_ARM64,
    ARCH_WASM,
    ARCH_S390X,
    ARCH_LOONGARCH64,
    ARCH_ARMV6,
    ARCH_POWERPC64,
    ARCH_RISCV64,
};

int32_t SystemNative_GetOSArchitecture(void)
{
#ifdef TARGET_WASM
    return ARCH_WASM;
#else
    int32_t result = -1;
#ifdef TARGET_SUNOS
    // On illumos/Solaris, the recommended way to obtain machine
    // architecture is using `sysinfo` rather than `utsname.machine`.

    char isa[32];
    if (sysinfo(SI_ARCHITECTURE_K, isa, sizeof(isa)) > -1)
    {
#else
    struct utsname _utsname;
    if (uname(&_utsname) > -1)
    {
        char* isa = _utsname.machine;
#endif
        // aarch64 or arm64: arm64
        if (strcmp("aarch64", isa) == 0 || strcmp("arm64", isa) == 0)
        {
            result = ARCH_ARM64;
        }

        // starts with "armv6" (armv6h or armv6l etc.): armv6
        else if (strncmp("armv6", isa, strlen("armv6")) == 0)
        {
            result = ARCH_ARMV6;
        }

        // starts with "arm": arm
        else if (strncmp("arm", isa, strlen("arm")) == 0)
        {
            result = ARCH_ARM;
        }

        // x86_64 or amd64: x64
        else if (strcmp("x86_64", isa) == 0 || strcmp("amd64", isa) == 0)
        {
#ifdef TARGET_OSX
            int is_translated_process = 0;
            size_t size = sizeof(is_translated_process);
            if (sysctlbyname("sysctl.proc_translated", &is_translated_process, &size, NULL, 0) == 0 && is_translated_process == 1)
                result = ARCH_ARM64;
            else
#endif
            result = ARCH_X64;
        }

        // ix86 (possible values are i286, i386, i486, i586 and i686): x86
        else if (strlen(isa) == strlen("i386") && isa[0] == 'i' && isa[2] == '8' && isa[3] == '6')
        {
            result = ARCH_X86;
        }

        else if (strcmp("s390x", isa) == 0)
        {
            result = ARCH_S390X;
        }

        else if (strcmp("ppc64le", isa) == 0)
        {
            result = ARCH_POWERPC64;
        }

        else if (strcmp("loongarch64", isa) == 0)
        {
            result = ARCH_LOONGARCH64;
        }

        else if (strcmp("riscv64", isa) == 0)
        {
            result = ARCH_RISCV64;
        }
    }

    // catch if we have missed a pattern above.
    assert(result != -1);

    return result;
#endif
}
