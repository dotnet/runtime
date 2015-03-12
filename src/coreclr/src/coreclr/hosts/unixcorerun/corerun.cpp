//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include <assert.h>
#include <dlfcn.h>
#include <dirent.h>
#include <errno.h>
#include <limits.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <string>
#include <set>

// The name of the CoreCLR native runtime DLL.
#if defined(__APPLE__)
static const char * const coreClrDll = "libcoreclr.dylib";
#else
static const char * const coreClrDll = "libcoreclr.so";
#endif

// Windows types used by the ExecuteAssembly function
typedef unsigned int DWORD;
typedef const char16_t* LPCWSTR;
typedef const char* LPCSTR;
typedef int32_t HRESULT;

#define SUCCEEDED(Status) ((HRESULT)(Status) >= 0)

// Prototype of the ExecuteAssembly function from the libcoreclr.do
typedef HRESULT (*ExecuteAssemblyFunction)(
                    LPCSTR exePath,
                    LPCSTR coreClrPath,
                    LPCSTR appDomainFriendlyName,
                    int propertyCount,
                    LPCSTR* propertyKeys,
                    LPCSTR* propertyValues,
                    int argc,
                    LPCSTR* argv,
                    LPCSTR managedAssemblyPath,
                    LPCSTR entryPointAssemblyName,
                    LPCSTR entryPointTypeName,
                    LPCSTR entryPointMethodsName,
                    DWORD* exitCode);

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

// Get absolute path from the specified path.
// Return true in case of a success, false otherwise.
bool GetAbsolutePath(const char* path, std::string& absolutePath)
{
    bool result = false;
    
    char realPath[PATH_MAX];
    if (realpath(path, realPath) != nullptr && realPath[0] != '\0')
    {
        absolutePath.assign(realPath);
        // The realpath should return canonicalized path without the trailing slash
        assert(absolutePath.back() != '/');

        result = true;
    }    
    
    return result;
}

void GetDirectory(const char* path, std::string& directory)
{
    directory.assign(path);
    size_t lastSlash = directory.rfind('/');
    directory.erase(lastSlash);   
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
                else if (strcmp(argv[i], "--help") == 0)
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

// Add all *.dll, *.ni.dll, *.exe, and *.ni.exe files from the specified directory
// to the tpaList string;
void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList)
{
    const char * const tpaExtensions[] = {
                ".ni.dll",		// Probe for .ni.dll first so that it's preferred if ni and il coexist in the same dir
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
            if (entry->d_type != DT_REG)
            {
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

//
// Execute the specified managed assembly. 
//
// Parameters:
//  currentExePath          - Path of the current executable
//  clrFilesAbsolutePath    - Absolute path of a folder where the libcoreclr.so and CLR managed assemblies are stored
//  managedAssemblyPath     - Path to the managed assembly to execute
//  managedAssemblyArgc     - Number of arguments passed to the executed assembly
//  managedAssemblyArgv     - Array of arguments passed to the executed assembly
//
// Returns:
//  ExitCode of the assembly
//
int ExecuteManagedAssembly(
            const char* currentExeAbsolutePath,
            const char* clrFilesAbsolutePath,
            const char* managedAssemblyAbsolutePath,
            int managedAssemblyArgc,
            const char** managedAssemblyArgv)
{
    // Indicates failure
    int exitCode = -1;
    
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
    
    void* coreclrLib = dlopen(coreClrDllPath.c_str(), RTLD_NOW | RTLD_GLOBAL);
    if (coreclrLib != nullptr)
    {
        ExecuteAssemblyFunction executeAssembly = (ExecuteAssemblyFunction)dlsym(coreclrLib, "ExecuteAssembly");
        if (executeAssembly != nullptr)
        {
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
                "NATIVE_DLL_SEARCH_DIRECTORIES"
            };
            const char *propertyValues[] = {
                // TRUSTED_PLATFORM_ASSEMBLIES
                tpaList.c_str(),
                // APP_PATHS
                appPath.c_str(),
                // APP_NI_PATHS
                appPath.c_str(),
                // NATIVE_DLL_SEARCH_DIRECTORIES
                nativeDllSearchDirs.c_str()
            };

            HRESULT st = executeAssembly(
                            currentExeAbsolutePath,
                            coreClrDllPath.c_str(),
                            "unixcorerun",
                            sizeof(propertyKeys) / sizeof(propertyKeys[0]),
                            propertyKeys,
                            propertyValues,
                            managedAssemblyArgc,
                            managedAssemblyArgv,
                            managedAssemblyAbsolutePath,
                            NULL,
                            NULL,
                            NULL,
                            (DWORD*)&exitCode);

            if (!SUCCEEDED(st))
            {
                fprintf(stderr, "ExecuteAssembly failed - status: 0x%08x\n", st);
            }
        }
        else
        {
            fprintf(stderr, "Function ExecuteAssembly not found in the libcoreclr.so\n");
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
    
    // Convert the specified path to CLR files to an absolute path since the libcoreclr.so
    // requires it.
    std::string clrFilesAbsolutePath;
    std::string clrFilesRelativePath;

    if (clrFilesPath == nullptr)
    {
        // There was no CLR files path specified, use the folder of the corerun
        GetDirectory(argv[0], clrFilesRelativePath);
        clrFilesPath = clrFilesRelativePath.c_str();
            
        // TODO: consider using an env variable (if defined) as a fall-back.
        // The windows version of the corerun uses core_root env variable
    }

    if (!GetAbsolutePath(clrFilesPath, clrFilesAbsolutePath))
    {
        perror("Failed to convert CLR files path to absolute path");
        return -1;
    }

    std::string managedAssemblyAbsolutePath;
    if (!GetAbsolutePath(managedAssemblyPath, managedAssemblyAbsolutePath))
    {
        perror("Failed to convert managed assembly path to absolute path");
        return -1;
    }
    
    int exitCode = ExecuteManagedAssembly(
                            argv[0],
                            clrFilesAbsolutePath.c_str(),
                            managedAssemblyAbsolutePath.c_str(),
                            managedAssemblyArgc,
                            managedAssemblyArgv);
    return exitCode;
}
