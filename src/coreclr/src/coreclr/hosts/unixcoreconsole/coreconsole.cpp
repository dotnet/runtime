// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// A simple CoreCLR host that runs a managed binary with the same name as this executable but with the *.dll extension
// The dll binary must contain a main entry point.
//

#include <coreruncommon.h>
#include <string>
#include <string.h>
#include <sys/stat.h>

// Display the help text
void DisplayUsage()
{
     fprintf(
        stderr,
        "Runs executables on CoreCLR\n\n"
        "Usage: <program> [OPTIONS] [ARGUMENTS]\n"
        "Runs <program>.dll on CoreCLR.\n\n"
        "Options:\n"
        "-_c  path to libcoreclr.so and the managed CLR assemblies.\n"
        "-_h  show this help message. \n");
}

// Parse the command line arguments 
bool ParseArguments(
        const int argc,
        const char* argv[],
        const char** clrFilesPath,
        int* managedAssemblyArgc,
        const char*** managedAssemblyArgv)
{
    bool success = true;

    *clrFilesPath = nullptr;
    *managedAssemblyArgv = nullptr;
    *managedAssemblyArgc = 0;

    for (int i = 1; i < argc; i++)
    {
        // Check for options. Options to the Unix coreconsole are prefixed with '-_' to match the convention
        // used in the Windows version of coreconsole.
        if (strncmp(argv[i], "-_", 2) == 0)
        {
            // Path to the libcoreclr.so and the managed CLR assemblies
            if (strcmp(argv[i], "-_c") == 0)
            {
                i++;
                if (i < argc)
                {
                    *clrFilesPath = argv[i];
                }
                else
                {
                    fprintf(stderr, "Option %s: missing path\n", argv[i - 1]);
                    success = false;
                    break;
                }
            }
            else if (strcmp(argv[i], "-_h") == 0)
            {
                DisplayUsage();
                success = false;
                break;
            }
            else
            {
                fprintf(stderr, "Unknown option %s\n", argv[i]);
                success = false;
                break;
            }
        }
        else
        {
            // We treat everything starting from the first non-option argument as arguments
            // to the managed assembly.
            *managedAssemblyArgc = argc - i;
            if (*managedAssemblyArgc != 0)
            {
                *managedAssemblyArgv = &argv[i];
            }

            break;
        }
    }

    return success;
}

int main(const int argc, const char* argv[])
{
    // Make sure we have a full path for argv[0].
    std::string entryPointExecutablePath;

    if (!GetEntrypointExecutableAbsolutePath(entryPointExecutablePath))
    {
        perror("Could not get full path to current executable");
        return -1;
    }

    // We will try to load the managed assembly with the same name as this executable
    // but with the .dll extension.
    std::string programPath(entryPointExecutablePath);
    programPath.append(".dll");
    const char* managedAssemblyAbsolutePath = programPath.c_str();

    // Check if the specified managed assembly file exists
    struct stat sb;
    if (stat(managedAssemblyAbsolutePath, &sb) == -1)
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

    const char* clrFilesPath;
    const char** managedAssemblyArgv;
    int managedAssemblyArgc;

    if (!ParseArguments(
            argc,
            argv,
            &clrFilesPath,
            &managedAssemblyArgc,
            &managedAssemblyArgv
            ))
    {
        // Invalid command line
        return -1;
    }

    std::string clrFilesAbsolutePath;
    if(!GetClrFilesAbsolutePath(entryPointExecutablePath.c_str(), clrFilesPath, clrFilesAbsolutePath))
    {
        return -1;
    }

    int exitCode = ExecuteManagedAssembly(
                            entryPointExecutablePath.c_str(),
                            clrFilesAbsolutePath.c_str(),
                            managedAssemblyAbsolutePath,
                            managedAssemblyArgc,
                            managedAssemblyArgv);

    return exitCode;
}
