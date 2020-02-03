// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <coreruncommon.h>
#include <string>
#include <string.h>
#include <sys/stat.h>

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

int corerun(const int argc, const char* argv[])
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
    std::string argv0AbsolutePath;
    if (!GetEntrypointExecutableAbsolutePath(argv0AbsolutePath))
    {
        perror("Could not get full path");
        return -1;
    }

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

int main(const int argc, const char* argv[])
{
    return corerun(argc, argv);
}
