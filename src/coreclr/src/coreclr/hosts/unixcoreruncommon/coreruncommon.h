// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <string>

// Get the path to entrypoint executable
bool GetEntrypointExecutableAbsolutePath(std::string& entrypointExecutable);

// Get absolute path from the specified path.
// Return true in case of a success, false otherwise.
bool GetAbsolutePath(const char* path, std::string& absolutePath);

// Get directory of the specified path.
// Return true in case of a success, false otherwise.
bool GetDirectory(const char* absolutePath, std::string& directory);

//
// Get the absolute path to use to locate libcoreclr.so and the CLR assemblies are stored. If clrFilesPath is provided,
// this function will return the absolute path to it. Otherwise, the directory of the current executable is used.
//
// Return true in case of a success, false otherwise.
//
bool GetClrFilesAbsolutePath(const char* currentExePath, const char* clrFilesPath, std::string& clrFilesAbsolutePath);

// Add all *.dll, *.ni.dll, *.exe, and *.ni.exe files from the specified directory to the tpaList string.
void AddFilesFromDirectoryToTpaList(const char* directory, std::string& tpaList);

//
// Execute the specified managed assembly. 
//
// Parameters:
//  currentExePath          - Path to the current executable
//  clrFilesAbsolutePath    - Absolute path to the folder where the libcoreclr.so and CLR managed assemblies are stored
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
            const char** managedAssemblyArgv);


#if defined(__APPLE__)
#include <mach-o/dyld.h>
static const char * const coreClrDll = "libcoreclr.dylib";
#else
static const char * const coreClrDll = "libcoreclr.so";
#endif

