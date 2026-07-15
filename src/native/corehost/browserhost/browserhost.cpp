// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdio>
#include <cstdint>
#include <cstring>
#include <cerrno>
#include <climits>
#include <vector>
#include <fcntl.h>
#include <unistd.h>
#include <sys/stat.h>

#include <pal.h>
#include <minipal/utils.h>
#include <host_runtime_contract.h>

typedef void (*coreclr_error_writer_callback_fn)(const char* line);
extern "C"
{
    pal::hresult_t coreclr_initialize(
        const char* exePath,
        const char* appDomainFriendlyName,
        int propertyCount,
        const char** propertyKeys,
        const char** propertyValues,
        void** hostHandle,
        unsigned int* domainId);

    pal::hresult_t coreclr_shutdown_2(
        void* hostHandle,
        unsigned int domainId,
        int* latchedExitCode);

    pal::hresult_t coreclr_execute_assembly(
        void* hostHandle,
        unsigned int domainId,
        int argc,
        const char** argv,
        const char* managedAssemblyPath,
        unsigned int* exitCode);

    pal::hresult_t coreclr_create_delegate(
        void* hostHandle,
        unsigned int domainId,
        const char* entryPointAssemblyName,
        const char* entryPointTypeName,
        const char* entryPointMethodName,
        void** delegate);

    pal::hresult_t coreclr_set_error_writer(
        coreclr_error_writer_callback_fn error_writer);

#if !GEN_PINVOKE
    const void* SystemResolveDllImport(const char* name);
    const void* SystemJSResolveDllImport(const char* name);
    const void* SystemJSInteropResolveDllImport(const char* name);
    const void* GlobalizationResolveDllImport(const char* name);
    const void* CompressionResolveDllImport(const char* name);
#endif // not GEN_PINVOKE

    bool BrowserHost_ExternalAssemblyProbe(const char* pathPtr, /*out*/ void **outDataStartPtr, /*out*/ int64_t* outSize);
}

// The current CoreCLR instance details.
static void* CurrentClrInstance;
static unsigned int CurrentAppDomainId;

static void log_error_info(const char* line)
{
    std::fprintf(stderr, "log error: %s\n", line);
}

#if GEN_PINVOKE
const void* callhelpers_pinvoke_override(const char* library_name, const char* entry_point_name);
#else
static const void* pinvoke_override(const char* library_name, const char* entry_point_name)
{
    if (strcmp(library_name, "libSystem.Native") == 0)
    {
        return SystemResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.Native.Browser") == 0)
    {
        return SystemJSResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.Runtime.InteropServices.JavaScript.Native") == 0)
    {
        return SystemJSInteropResolveDllImport(entry_point_name);
    }
    if (strcmp(library_name, "libSystem.IO.Compression.Native") == 0)
    {
        return CompressionResolveDllImport(entry_point_name);
    }
    // duplicates https://github.com/dotnet/runtime/blob/7a33b4bb6ced097f081b1eeab575cfb1c8c88bb5/src/coreclr/vm/pinvokeoverride.cpp#L21-L36 for clarity
    if (strcmp(library_name, "libSystem.Globalization.Native") == 0)
    {
        return GlobalizationResolveDllImport(entry_point_name);
    }

    return nullptr;
}
#endif // GEN_PINVOKE

static host_runtime_contract host_contract = { sizeof(host_runtime_contract), nullptr };

extern "C" void* BrowserHost_CreateHostContract(void)
{
#if GEN_PINVOKE
    host_contract.pinvoke_override = &callhelpers_pinvoke_override;
#else
    host_contract.pinvoke_override = &pinvoke_override;
#endif // GEN_PINVOKE
    host_contract.external_assembly_probe = &BrowserHost_ExternalAssemblyProbe;
    return &host_contract;
}

extern "C" int BrowserHost_InitializeDotnet(int propertiesCount, const char** propertyKeys, const char** propertyValues)
{
    coreclr_set_error_writer(log_error_info);

    int retval = coreclr_initialize("/managed", "corehost", propertiesCount, propertyKeys, propertyValues, &CurrentClrInstance, &CurrentAppDomainId);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_initialize failed - Error: 0x%08x\n", retval);
        return -1;
    }
    return 0;
}

static bool executeAssemblyFailed = false;
extern "C" int BrowserHost_ExecuteAssembly(const char* assemblyPath, int argc, const char** argv)
{
    executeAssemblyFailed = false;
    int exit_code = 0;
    int retval = coreclr_execute_assembly(CurrentClrInstance, CurrentAppDomainId, argc, argv, assemblyPath, (uint32_t*)&exit_code);

    if (retval < 0)
    {
        std::fprintf(stderr, "coreclr_execute_assembly failed - Error: 0x%08x\n", retval);
        executeAssemblyFailed = true;
        return -1;
    }
    return exit_code;
}

extern "C" int BrowserHost_ShutdownDotnet(int exit_code)
{
    if (executeAssemblyFailed)
    {
        return exit_code;
    }

    int latched_exit_code = exit_code;
    int result = coreclr_shutdown_2(CurrentClrInstance, CurrentAppDomainId, &latched_exit_code);
    if (result < 0)
    {
        std::fprintf(stderr, "coreclr_shutdown_2 failed - Error: 0x%08x\n", result);
        return -1;
    }

    return latched_exit_code;
}

// Create all missing parent directories of the given path (equivalent to `mkdir -p` of
// the dirname). Best-effort: existing directories (EEXIST) are ignored; a subsequent
// open()/chdir() surfaces any real failure.
static void BrowserHost_EnsureParentDirs(const char* path)
{
    char tmp[PATH_MAX];
    size_t len = strlen(path);
    if (len == 0 || len >= sizeof(tmp))
    {
        return;
    }
    memcpy(tmp, path, len + 1);
    for (char* p = tmp + 1; *p != '\0'; ++p)
    {
        if (*p == '/')
        {
            *p = '\0';
            if (mkdir(tmp, 0755) != 0 && errno != EEXIST)
            {
                // best-effort
            }
            *p = '/';
        }
    }
}

// Write bytes to a file in the (WASMFS) virtual filesystem, creating any missing parent
// directories. This replaces the JS FS.createPath + FS.createDataFile pattern so the
// JavaScript FS API (and -sFORCE_FILESYSTEM) is not required on the browser host.
extern "C" int BrowserHost_WriteFileToVfs(const char* path, const void* data, int32_t length)
{
    BrowserHost_EnsureParentDirs(path);

    int fd = open(path, O_CREAT | O_WRONLY | O_TRUNC, 0644);
    if (fd < 0)
    {
        std::fprintf(stderr, "BrowserHost_WriteFileToVfs: open('%s') failed - errno %d\n", path, errno);
        return -1;
    }

    const uint8_t* cursor = static_cast<const uint8_t*>(data);
    int32_t remaining = length;
    while (remaining > 0)
    {
        ssize_t written = write(fd, cursor, static_cast<size_t>(remaining));
        if (written < 0)
        {
            if (errno == EINTR)
            {
                continue;
            }
            std::fprintf(stderr, "BrowserHost_WriteFileToVfs: write('%s') failed - errno %d\n", path, errno);
            close(fd);
            return -1;
        }
        cursor += written;
        remaining -= static_cast<int32_t>(written);
    }

    close(fd);
    return 0;
}

// Create (mkdir -p) and change into the working directory. Replaces FS.createPath + FS.chdir.
extern "C" int BrowserHost_SetWorkingDirectory(const char* path)
{
    BrowserHost_EnsureParentDirs(path);
    if (mkdir(path, 0755) != 0 && errno != EEXIST)
    {
        // best-effort; chdir() below surfaces a real failure
    }
    if (chdir(path) != 0)
    {
        std::fprintf(stderr, "BrowserHost_SetWorkingDirectory: chdir('%s') failed - errno %d\n", path, errno);
        return -1;
    }
    return 0;
}
