// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Code that is used by both the Unix corerun and coreconsole.
//

#include <cstdlib>
#include <assert.h>
#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <set>
#include <string>
#include <string.h>
#include <sys/stat.h>
#include "coreruncommon.h"
#include <unistd.h>

#define SUCCEEDED(Status) ((Status) >= 0)

// Name of the environment variable controlling server GC.
// If set to 1, server GC is enabled on startup. If 0, server GC is
// disabled. Server GC is off by default.
static const char* serverGcVar = "CORECLR_SERVER_GC";

// Name of the environment variable controlling concurrent GC,
// used in the same way as serverGcVar. Concurrent GC is on by default.
static const char* concurrentGcVar = "CORECLR_CONCURRENT_GC";

// Prototype of the coreclr_initialize function from the libcoreclr.so
typedef int (*InitializeCoreCLRFunction)(
            const char* exePath,
            const char* appDomainFriendlyName,
            int propertyCount,
            const char** propertyKeys,
            const char** propertyValues,
            void** hostHandle,
            unsigned int* domainId);

// Prototype of the coreclr_shutdown function from the libcoreclr.so
typedef int (*ShutdownCoreCLRFunction)(
            void* hostHandle,
            unsigned int domainId);

// Prototype of the coreclr_execute_assembly function from the libcoreclr.so
typedef int (*ExecuteAssemblyFunction)(
            void* hostHandle,
            unsigned int domainId,
            int argc,
            const char** argv,
            const char* managedAssemblyPath,
            unsigned int* exitCode);

#if defined(__LINUX__)
#define symlinkEntrypointExecutable "/proc/self/exe"
#elif !defined(__APPLE__)
#define symlinkEntrypointExecutable "/proc/curproc/exe"
#endif

bool GetEntrypointExecutableAbsolutePath(std::string& entrypointExecutable)
{
    bool result = false;
    
    entrypointExecutable.clear();

    // Get path to the executable for the current process using
    // platform specific means.
#if defined(__LINUX__)
    // On Linux, fetch the entry point EXE absolute path, inclusive of filename.
    char exe[PATH_MAX];
    ssize_t res = readlink(symlinkEntrypointExecutable, exe, PATH_MAX - 1);
    if (res != -1)
    {
        exe[res] = '\0';
        entrypointExecutable.assign(exe);
        result = true;
    }
    else
    {
        result = false;
    }
#elif defined(__APPLE__)
    
    // On Mac, we ask the OS for the absolute path to the entrypoint executable
    uint32_t lenActualPath = 0;
    if (_NSGetExecutablePath(nullptr, &lenActualPath) == -1)
    {
        // OSX has placed the actual path length in lenActualPath,
        // so re-attempt the operation
        std::string resizedPath(lenActualPath, '\0');
        char *pResizedPath = const_cast<char *>(resizedPath.c_str());
        if (_NSGetExecutablePath(pResizedPath, &lenActualPath) == 0)
        {
            entrypointExecutable.assign(pResizedPath);
            result = true;
        }
    }
#else
    // On non-Mac OS, return the symlink that will be resolved by GetAbsolutePath
    // to fetch the entrypoint EXE absolute path, inclusive of filename.
    entrypointExecutable.assign(symlinkEntrypointExecutable);
    result = true;
#endif 

    return result;
}

bool GetAbsolutePath(const char* path, std::string& absolutePath)
{
    bool result = false;

    char realPath[PATH_MAX];
    if (realpath(path, realPath) != nullptr && realPath[0] != '\0')
    {
        absolutePath.assign(realPath);
        // realpath should return canonicalized path without the trailing slash
        assert(absolutePath.back() != '/');

        result = true;
    }

    return result;
}

bool GetDirectory(const char* absolutePath, std::string& directory)
{
    directory.assign(absolutePath);
    size_t lastSlash = directory.rfind('/');
    if (lastSlash != std::string::npos)
    {
        directory.erase(lastSlash);
        return true;
    }

    return false;
}

bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath)
{
    std::string clrFilesRelativePath;
    const char* clrFilesPathLocal = clrFilesPath;
    if (clrFilesPathLocal == nullptr)
    {
        // There was no CLR files path specified, use the folder of the corerun/coreconsole
        if (!GetDirectory(currentExePath, clrFilesRelativePath))
        {
            perror("Failed to get directory from argv[0]");
            return false;
        }

        clrFilesPathLocal = clrFilesRelativePath.c_str();

        // TODO: consider using an env variable (if defined) as a fall-back.
        // The windows version of the corerun uses core_root env variable
    }

    if (!GetAbsolutePath(clrFilesPathLocal, clrFilesAbsolutePath))
    {
        perror("Failed to convert CLR files path to absolute path");
        return false;
    }

    return true;
}

void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList)
{
    const char * const tpaExtensions[] = {
                ".ni.dll",      // Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
                ".dll",
                ".ni.exe",
                ".exe",
                };
                
    DIR* dir = opendir(directory);
    if (dir == nullptr)
    {
        return;
    }

    std::set<std::string> addedAssemblies;

    // Walk the directory for each extension separately so that we first get files with .ni.dll extension,
    // then files with .dll extension, etc.
    for (int extIndex = 0; extIndex < sizeof(tpaExtensions) / sizeof(tpaExtensions[0]); extIndex++)
    {
        const char* ext = tpaExtensions[extIndex];
        int extLength = strlen(ext);

        struct dirent* entry;

        // For all entries in the directory
        while ((entry = readdir(dir)) != nullptr)
        {
            // We are interested in files only
            switch (entry->d_type)
            {
            case DT_REG:
                break;

            // Handle symlinks and file systems that do not support d_type
            case DT_LNK:
            case DT_UNKNOWN:
                {
                    std::string fullFilename;

                    fullFilename.append(directory);
                    fullFilename.append("/");
                    fullFilename.append(entry->d_name);

                    struct stat sb;
                    if (stat(fullFilename.c_str(), &sb) == -1)
                    {
                        continue;
                    }

                    if (!S_ISREG(sb.st_mode))
                    {
                        continue;
                    }
                }
                break;

            default:
                continue;
            }

            std::string filename(entry->d_name);

            // Check if the extension matches the one we are looking for
            int extPos = filename.length() - extLength;
            if ((extPos <= 0) || (filename.compare(extPos, extLength, ext) != 0))
            {
                continue;
            }

            std::string filenameWithoutExt(filename.substr(0, extPos));

            // Make sure if we have an assembly with multiple extensions present,
            // we insert only one version of it.
            if (addedAssemblies.find(filenameWithoutExt) == addedAssemblies.end())
            {
                addedAssemblies.insert(filenameWithoutExt);

                tpaList.append(directory);
                tpaList.append("/");
                tpaList.append(filename);
                tpaList.append(":");
            }
        }
        
        // Rewind the directory stream to be able to iterate over it for the next extension
        rewinddir(dir);
    }
    
    closedir(dir);
}

int ExecuteManagedAssembly(
            const char* currentExeAbsolutePath,
            const char* clrFilesAbsolutePath,
            const char* managedAssemblyAbsolutePath,
            int managedAssemblyArgc,
            const char** managedAssemblyArgv)
{
    // Indicates failure
    int exitCode = -1;

#ifdef _ARM_
    // LIBUNWIND-ARM has a bug of side effect with DWARF mode
    // Ref: https://github.com/dotnet/coreclr/issues/3462
    // This is why Fedora is disabling it by default as well.
    // Assuming that we cannot enforce the user to set
    // environmental variables for third party packages,
    // we set the environmental variable of libunwind locally here.

    // Without this, any exception handling will fail, so let's do this
    // as early as possible.
    // 0x1: DWARF / 0x2: FRAME / 0x4: EXIDX
    putenv(const_cast<char *>("UNW_ARM_UNWIND_METHOD=6"));
#endif // _ARM_

    std::string coreClrDllPath(clrFilesAbsolutePath);
    coreClrDllPath.append("/");
    coreClrDllPath.append(coreClrDll);

    if (coreClrDllPath.length() >= PATH_MAX)
    {
        fprintf(stderr, "Absolute path to libcoreclr.so too long\n");
        return -1;
    }

    // Get just the path component of the managed assembly path
    std::string appPath;
    GetDirectory(managedAssemblyAbsolutePath, appPath);

    std::string nativeDllSearchDirs(appPath);
    nativeDllSearchDirs.append(":");
    nativeDllSearchDirs.append(clrFilesAbsolutePath);

    std::string tpaList;
    AddFilesFromDirectoryToTpaList(clrFilesAbsolutePath, tpaList);

    void* coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW | RTLD_LOCAL);
    if (coreclrLib != nullptr)
    {
        InitializeCoreCLRFunction initializeCoreCLR = (InitializeCoreCLRFunction)dlsym(coreclrLib, "coreclr_initialize");
        ExecuteAssemblyFunction executeAssembly = (ExecuteAssemblyFunction)dlsym(coreclrLib, "coreclr_execute_assembly");
        ShutdownCoreCLRFunction shutdownCoreCLR = (ShutdownCoreCLRFunction)dlsym(coreclrLib, "coreclr_shutdown");

        if (initializeCoreCLR == nullptr)
        {
            fprintf(stderr, "Function coreclr_initialize not found in the libcoreclr.so\n");
        }
        else if (executeAssembly == nullptr)
        {
            fprintf(stderr, "Function coreclr_execute_assembly not found in the libcoreclr.so\n");
        }
        else if (shutdownCoreCLR == nullptr)
        {
            fprintf(stderr, "Function coreclr_shutdown not found in the libcoreclr.so\n");
        }
        else
        {
            // check if we are enabling server GC or concurrent GC.
            // Server GC is off by default, while concurrent GC is on by default.
            // Actual checking of these string values is done in coreclr_initialize.
            const char* useServerGc = std::getenv(serverGcVar);
            if (useServerGc == nullptr)
            {
                useServerGc = "0";
            }
            
            const char* useConcurrentGc = std::getenv(concurrentGcVar);
            if (useConcurrentGc == nullptr)
            {
                useConcurrentGc = "1";
            }
            
            // Allowed property names:
            // APPBASE
            // - The base path of the application from which the exe and other assemblies will be loaded
            //
            // TRUSTED_PLATFORM_ASSEMBLIES
            // - The list of complete paths to each of the fully trusted assemblies
            //
            // APP_PATHS
            // - The list of paths which will be probed by the assembly loader
            //
            // APP_NI_PATHS
            // - The list of additional paths that the assembly loader will probe for ngen images
            //
            // NATIVE_DLL_SEARCH_DIRECTORIES
            // - The list of paths that will be probed for native DLLs called by PInvoke
            //
            const char *propertyKeys[] = {
                "TRUSTED_PLATFORM_ASSEMBLIES",
                "APP_PATHS",
                "APP_NI_PATHS",
                "NATIVE_DLL_SEARCH_DIRECTORIES",
                "AppDomainCompatSwitch",
                "SERVER_GC",
                "CONCURRENT_GC"
            };
            const char *propertyValues[] = {
                // TRUSTED_PLATFORM_ASSEMBLIES
                tpaList.c_str(),
                // APP_PATHS
                appPath.c_str(),
                // APP_NI_PATHS
                appPath.c_str(),
                // NATIVE_DLL_SEARCH_DIRECTORIES
                nativeDllSearchDirs.c_str(),
                // AppDomainCompatSwitch
                "UseLatestBehaviorWhenTFMNotSpecified",
                // SERVER_GC
                useServerGc,
                // CONCURRENT_GC
                useConcurrentGc
            };

            void* hostHandle;
            unsigned int domainId;

            int st = initializeCoreCLR(
                        currentExeAbsolutePath, 
                        "unixcorerun", 
                        sizeof(propertyKeys) / sizeof(propertyKeys[0]), 
                        propertyKeys, 
                        propertyValues, 
                        &hostHandle, 
                        &domainId);

            if (!SUCCEEDED(st))
            {
                fprintf(stderr, "coreclr_initialize failed - status: 0x%08x\n", st);
                exitCode = -1;
            }
            else 
            {
                st = executeAssembly(
                        hostHandle,
                        domainId,
                        managedAssemblyArgc,
                        managedAssemblyArgv,
                        managedAssemblyAbsolutePath,
                        (unsigned int*)&exitCode);

                if (!SUCCEEDED(st))
                {
                    fprintf(stderr, "coreclr_execute_assembly failed - status: 0x%08x\n", st);
                    exitCode = -1;
                }

                st = shutdownCoreCLR(hostHandle, domainId);
                if (!SUCCEEDED(st))
                {
                    fprintf(stderr, "coreclr_shutdown failed - status: 0x%08x\n", st);
                    exitCode = -1;
                }
            }
        }

        if (dlclose(coreclrLib) != 0)
        {
            fprintf(stderr, "Warning - dlclose failed\n");
        }
    }
    else
    {
        char* error = dlerror();
        fprintf(stderr, "dlopen failed to open the libcoreclr.so with error %s\n", error);
    }

    return exitCode;
}
