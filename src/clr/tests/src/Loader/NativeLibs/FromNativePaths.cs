// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

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

        var coreRoot = Environment.GetEnvironmentVariable("CORE_ROOT");
        if (string.IsNullOrWhiteSpace(coreRoot))
        {
            Console.WriteLine("FromNativePaths failed: CORE_ROOT is not defined.");
            return false;
        }
        if (!Directory.Exists(coreRoot))
        {
            Console.WriteLine(
                "FromNativePaths failed: Directory specified by CORE_ROOT does not exist: {0}",
                coreRoot);
            return false;
        }

        var movedLibraryDestinationPaths = new List<string>();
        var moveLibraryFailed = false;
        foreach (var nativeLibraryExtension in NativeLibraryExtensions)
        {
            var nativeLibraryName = NativeLibraryNameWithoutExtension + nativeLibraryExtension;
            if (!File.Exists(nativeLibraryName))
                continue;

            var destinationPath = Path.Combine(coreRoot, nativeLibraryName);
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

    public static int Main()
    {
        return Test() ? 100 : 101;
    }
}
