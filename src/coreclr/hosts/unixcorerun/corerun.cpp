// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <cstdlib>
#include <cstring>
#include <assert.h>
#include <dirent.h>
#include <dlfcn.h>
#include <limits.h>
#include <set>
#include <string>
#include <string.h>
#include <sys/stat.h>
#include "coreclrhost.h"
#include <unistd.h>
#ifndef SUCCEEDED
#define SUCCEEDED(Status) ((Status) >= 0)
#endif // !SUCCEEDED

#include <config.h>
#include <getexepath.h>

#if !HAVE_DIRENT_D_TYPE
#define DT_UNKNOWN 0
#define DT_DIR 4
#define DT_REG 8
#define DT_LNK 10
#endif

// Name of the environment variable controlling server GC.
// If set to 1, server GC is enabled on startup. If 0, server GC is
// disabled. Server GC is off by default.
static const char* serverGcVar = "COMPlus_gcServer";

// Name of environment variable to control "System.Globalization.Invariant"
// Set to 1 for Globalization Invariant mode to be true. Default is false.
static const char* globalizationInvariantVar = "CORECLR_GLOBAL_INVARIANT";

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
    for (size_t extIndex = 0; extIndex < sizeof(tpaExtensions) / sizeof(tpaExtensions[0]); extIndex++)
    {
        const char* ext = tpaExtensions[extIndex];
        int extLength = strlen(ext);

        struct dirent* entry;

        // For all entries in the directory
        while ((entry = readdir(dir)) != nullptr)
        {
#if HAVE_DIRENT_D_TYPE
            int dirEntryType = entry->d_type;
#else
            int dirEntryType = DT_UNKNOWN;
#endif

            // We are interested in files only
            switch (dirEntryType)
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

const char* GetEnvValueBoolean(const char* envVariable)
{
    const char* envValue = std::getenv(envVariable);
    if (envValue == nullptr)
    {
        envValue = "0";
    }
    // CoreCLR expects strings "true" and "false" instead of "1" and "0".
    return (std::strcmp(envValue, "1") == 0 || strcasecmp(envValue, "true") == 0) ? "true" : "false";
}

static void *TryLoadHostPolicy(const char *hostPolicyPath)
{
#if defined(__APPLE__)
    static const char LibrarySuffix[] = ".dylib";
#else // Various Linux-related OS-es
    static const char LibrarySuffix[] = ".so";
#endif

    std::string hostPolicyCompletePath(hostPolicyPath);
    hostPolicyCompletePath.append(LibrarySuffix);

    void *libraryPtr = dlopen(hostPolicyCompletePath.c_str(), RTLD_LAZY);
    if (libraryPtr == nullptr)
    {
        fprintf(stderr, "Failed to load mock hostpolicy at path '%s'. Error: %s", hostPolicyCompletePath.c_str(), dlerror());
    }

    return libraryPtr;
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

#ifdef HOST_ARM
    // libunwind library is used to unwind stack frame, but libunwind for ARM
    // does not support ARM vfpv3/NEON registers in DWARF format correctly.
    // Therefore let's disable stack unwinding using DWARF information
    // See https://github.com/dotnet/runtime/issues/6479
    //
    // libunwind use following methods to unwind stack frame.
    // UNW_ARM_METHOD_ALL          0xFF
    // UNW_ARM_METHOD_DWARF        0x01
    // UNW_ARM_METHOD_FRAME        0x02
    // UNW_ARM_METHOD_EXIDX        0x04
    putenv(const_cast<char *>("UNW_ARM_UNWIND_METHOD=6"));
#endif // HOST_ARM

#if defined(__APPLE__)
    static const char* const coreClrDll = "libcoreclr.dylib";
#else
    static const char* const coreClrDll = "libcoreclr.so";
#endif

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

    std::string tpaList;
    // Construct native search directory paths
    std::string nativeDllSearchDirs(appPath);
    char *coreLibraries = getenv("CORE_LIBRARIES");
    if (coreLibraries)
    {
        nativeDllSearchDirs.append(":");
        nativeDllSearchDirs.append(coreLibraries);
        if (std::strcmp(coreLibraries, clrFilesAbsolutePath) != 0)
        {
            AddFilesFromDirectoryToTpaList(coreLibraries, tpaList);
        }
    }
    nativeDllSearchDirs.append(":");
    nativeDllSearchDirs.append(clrFilesAbsolutePath);

    void* hostpolicyLib = nullptr;
    char* mockHostpolicyPath = getenv("MOCK_HOSTPOLICY");
    if (mockHostpolicyPath)
    {
        hostpolicyLib = TryLoadHostPolicy(mockHostpolicyPath);
        if (hostpolicyLib == nullptr)
        {
            return -1;
        }
    }

    AddFilesFromDirectoryToTpaList(clrFilesAbsolutePath, tpaList);

    void* coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW | RTLD_LOCAL);
    if (coreclrLib != nullptr)
    {
        coreclr_initialize_ptr initializeCoreCLR = (coreclr_initialize_ptr)dlsym(coreclrLib, "coreclr_initialize");
        coreclr_execute_assembly_ptr executeAssembly = (coreclr_execute_assembly_ptr)dlsym(coreclrLib, "coreclr_execute_assembly");
        coreclr_shutdown_2_ptr shutdownCoreCLR = (coreclr_shutdown_2_ptr)dlsym(coreclrLib, "coreclr_shutdown_2");

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
            fprintf(stderr, "Function coreclr_shutdown_2 not found in the libcoreclr.so\n");
        }
        else
        {
            // Check whether we are enabling server GC (off by default)
            const char* useServerGc = GetEnvValueBoolean(serverGcVar);

            // Check Globalization Invariant mode (false by default)
            const char* globalizationInvariant = GetEnvValueBoolean(globalizationInvariantVar);

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
                "System.GC.Server",
                "System.Globalization.Invariant",
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
                // System.GC.Server
                useServerGc,
                // System.Globalization.Invariant
                globalizationInvariant,
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

                int latchedExitCode = 0;
                st = shutdownCoreCLR(hostHandle, domainId, &latchedExitCode);
                if (!SUCCEEDED(st))
                {
                    fprintf(stderr, "coreclr_shutdown failed - status: 0x%08x\n", st);
                    exitCode = -1;
                }

                if (exitCode != -1)
                {
                    exitCode = latchedExitCode;
                }
            }
        }
    }
    else
    {
        const char* error = dlerror();
        fprintf(stderr, "dlopen failed to open the libcoreclr.so with error %s\n", error);
    }

    if (hostpolicyLib)
    {
        if(dlclose(hostpolicyLib) != 0)
        {
            fprintf(stderr, "Warning - dlclose of mock hostpolicy failed.\n");
        }
    }

    return exitCode;
}

// Display the command line options
void DisplayUsage()
{
    fprintf(
        stderr,
        "Usage: corerun [OPTIONS] assembly [ARGUMENTS]\n"
        "Execute the specified managed assembly with the passed in arguments\n\n"
        "Options:\n"
        "-c, --clr-path  path to the libcoreclr.so and the managed CLR assemblies\n");
}

// Parse the command line arguments
bool ParseArguments(
        const int argc,
        const char* argv[],
        const char** clrFilesPath,
        const char** managedAssemblyPath,
        int* managedAssemblyArgc,
        const char*** managedAssemblyArgv)
{
    bool success = false;

    *clrFilesPath = nullptr;
    *managedAssemblyPath = nullptr;
    *managedAssemblyArgv = nullptr;
    *managedAssemblyArgc = 0;

    // The command line must contain at least the current exe name and the managed assembly path
    if (argc >= 2)
    {
        for (int i = 1; i < argc; i++)
        {
            // Check for an option
            if (argv[i][0] == '-')
            {
                // Path to the libcoreclr.so and the managed CLR assemblies
                if (strcmp(argv[i], "-c") == 0 || strcmp(argv[i], "--clr-path") == 0)
                {
                    i++;
                    if (i < argc)
                    {
                        *clrFilesPath = argv[i];
                    }
                    else
                    {
                        fprintf(stderr, "Option %s: missing path\n", argv[i - 1]);
                        break;
                    }
                }
                else if (strcmp(argv[i], "-?") == 0 || strcmp(argv[i], "-h") == 0 || strcmp(argv[i], "--help") == 0)
                {
                    DisplayUsage();
                    break;
                }
                else
                {
                    fprintf(stderr, "Unknown option %s\n", argv[i]);
                    break;
                }
            }
            else
            {
                // First argument that is not an option is the managed assembly to execute
                *managedAssemblyPath = argv[i];

                int managedArgvOffset = (i + 1);
                *managedAssemblyArgc = argc - managedArgvOffset;
                if (*managedAssemblyArgc != 0)
                {
                    *managedAssemblyArgv = &argv[managedArgvOffset];
                }
                success = true;
                break;
            }
        }
    }
    else
    {
        DisplayUsage();
    }

    return success;
}

int main(const int argc, const char* argv[])
{
    const char* clrFilesPath;
    const char* managedAssemblyPath;
    const char** managedAssemblyArgv;
    int managedAssemblyArgc;

    if (!ParseArguments(
            argc,
            argv,
            &clrFilesPath,
            &managedAssemblyPath,
            &managedAssemblyArgc,
            &managedAssemblyArgv))
    {
        // Invalid command line
        return -1;
    }

    // Check if the specified managed assembly file exists
    struct stat sb;
    if (stat(managedAssemblyPath, &sb) == -1)
    {
        perror("Managed assembly not found");
        return -1;
    }

    // Verify that the managed assembly path points to a file
    if (!S_ISREG(sb.st_mode))
    {
        fprintf(stderr, "The specified managed assembly is not a file\n");
        return -1;
    }

    // Make sure we have a full path for argv[0].
    char* path = getexepath();
    if (!path)
    {
        perror("Could not get full path");
        return -1;
    }

    std::string argv0AbsolutePath(path);
    free(path);

    std::string clrFilesAbsolutePath;
    if(!GetClrFilesAbsolutePath(argv0AbsolutePath.c_str(), clrFilesPath, clrFilesAbsolutePath))
    {
        return -1;
    }

    std::string managedAssemblyAbsolutePath;
    if (!GetAbsolutePath(managedAssemblyPath, managedAssemblyAbsolutePath))
    {
        perror("Failed to convert managed assembly path to absolute path");
        return -1;
    }

    int exitCode = ExecuteManagedAssembly(
                            argv0AbsolutePath.c_str(),
                            clrFilesAbsolutePath.c_str(),
                            managedAssemblyAbsolutePath.c_str(),
                            managedAssemblyArgc,
                            managedAssemblyArgv);

    return exitCode;
}
