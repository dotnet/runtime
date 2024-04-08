// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

public class FromNativePaths
{
    private const string NativeLibraryNameWithoutExtension = "FromNativePaths_lib";

    // See src/pal/src/include/pal/module.h
    private static readonly string[] NativeLibraryExtensions = new string[] { ".dll", ".so", ".dylib", ".a", ".sl" };

    [DllImport(NativeLibraryNameWithoutExtension)]
    private static extern bool NativeFunc();

    private static bool LoadNativeLibrary()
    {
        // Isolate the call to the native function in a separate function so that we can catch exceptions
        return NativeFunc();
    }

    private static bool Test()
    {
        // The loader checks the folder where the assembly that is loading the native library resides. The host-provided native
        // search paths for corerun include the folder where corerun resides. Move the native library there to verify that it
        // can be loaded from there.

        var coreLibraries = Environment.GetEnvironmentVariable("CORE_LIBRARIES");
        if (string.IsNullOrWhiteSpace(coreLibraries))
        {
            Console.WriteLine("FromNativePaths failed: CORE_LIBRARIES is not defined.");
            return false;
        }

        // In case there were multiple paths in CORE_LIBRARIES, assume that the last one is the one added in the test script.
        coreLibraries = coreLibraries.Split(new [] {TestLibrary.Utilities.IsWindows ? ';' : ':'}, StringSplitOptions.RemoveEmptyEntries).Last();

        if (!Directory.Exists(coreLibraries))
        {
            Console.WriteLine(
                "FromNativePaths failed: Directory specified by CORE_LIBRARIES does not exist: {0}",
                coreLibraries);
            return false;
        }

        var movedLibraryDestinationPaths = new List<string>();
        var moveLibraryFailed = false;
        foreach (var nativeLibraryExtension in NativeLibraryExtensions)
        {
            var nativeLibraryName = NativeLibraryNameWithoutExtension + nativeLibraryExtension;
            if (!File.Exists(nativeLibraryName))
                continue;

            var destinationPath = Path.Combine(coreLibraries, nativeLibraryName);
            try
            {
                var destinationFileInfo = new FileInfo(destinationPath);
                if (destinationFileInfo.Exists)
                {
                    destinationFileInfo.IsReadOnly = false;
                    destinationFileInfo.Delete();
                }
                File.Move(nativeLibraryName, destinationPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "FromNativePaths failed: Failed to move native library to CORE_ROOT: {0}",
                    ex.Message);
                moveLibraryFailed = true;
                break;
            }

            movedLibraryDestinationPaths.Add(destinationPath);
        }

        try
        {
            if (moveLibraryFailed)
                return false;

            // Try to load the native library
            try
            {
                if (LoadNativeLibrary())
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("FromNativePaths failed: Failed to load the native library: {0}", ex.Message);
                return false;
            }

            Console.WriteLine("FromNativePaths failed: Unexpected result from native function call.");
            return false;
        }
        finally
        {
            // Copy the native library back to the test folder. Don't move it, since it is loaded and the move may fail.
            foreach (var movedLibraryDestinationPath in movedLibraryDestinationPaths)
            {
                try
                {
                    File.Copy(movedLibraryDestinationPath, Path.GetFileName(movedLibraryDestinationPath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        "FromNativePaths failed: Failed to move the native library back to the test folder: {0}",
                        ex.Message);
                }
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Test() ? 100 : 101;
    }
}
