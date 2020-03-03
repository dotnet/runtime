// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// CoreCLR boot loader for OSX app packages.
//
// Assumes the following app package structure
//
//   /Contents/MacOS/yourAppName    (osxbundlerun renamed to your app name)
//   /Contents/CoreClrBundle/       The CoreCLR runtime, or a symlink to it if external
//   /Contents/ManagedBundle/       Your managed assemblies, including yourAppName.exe
//
// Of course you can also include whatever else you might need in the app package
//
// Symlinking the CoreClrBundle is handy for dev/debug builds. eg:
//
//   Contents> ln -s ~/dotnet/runtime/ CoreClrBundle
//
// All command line arguments are passed directly to the managed assembly's Main(args)
// Note that args[0] will be /Contents/MacOS/yourAppName, not /Contents/ManagedBundle/yourAppName.exe


#include <coreruncommon.h>
#include <string>
#include <string.h>
#include <unistd.h>
#include <sys/stat.h>

int main(const int argc, const char* argv[])
{
    // Make sure we have a full path for argv[0].
    std::string argv0AbsolutePath;
    if (!GetAbsolutePath(argv[0], argv0AbsolutePath))
    {
        perror("Could not get full path to current executable");
        return -1;
    }

    // Get name of self and containing folder (typically the MacOS folder)
    int lastSlashPos = argv0AbsolutePath.rfind('/');
    std::string appName = argv0AbsolutePath.substr(lastSlashPos+1);
    std::string appFolder = argv0AbsolutePath.substr(0, lastSlashPos);

    // Strip off "MacOS" to get to the "Contents" folder
    std::string contentsFolder;
    if (!GetDirectory(appFolder.c_str(), contentsFolder))
    {
        perror("Could not get Contents folder");
        return -1;
    }

    // Append standard locations
    std::string clrFilesAbsolutePath = contentsFolder + "/CoreClrBundle";
    std::string managedFolderAbsolutePath = contentsFolder + "/ManagedBundle/";
    std::string managedAssemblyAbsolutePath = managedFolderAbsolutePath + appName + ".exe";

    // Pass all command line arguments to managed executable
    const char** managedAssemblyArgv = argv;
    int managedAssemblyArgc = argc;

    // Check if the specified managed assembly file exists
    struct stat sb;
    if (stat(managedAssemblyAbsolutePath.c_str(), &sb) == -1)
    {
        perror(managedAssemblyAbsolutePath.c_str());
        return -1;
    }

    // Verify that the managed assembly path points to a file
    if (!S_ISREG(sb.st_mode))
    {
        fprintf(stderr, "The specified managed assembly is not a file\n");
        return -1;
    }

    // And go...
    int exitCode = ExecuteManagedAssembly(
                            argv0AbsolutePath.c_str(),
                            clrFilesAbsolutePath.c_str(),
                            managedAssemblyAbsolutePath.c_str(),
                            managedAssemblyArgc,
                            managedAssemblyArgv);

    return exitCode;
}
