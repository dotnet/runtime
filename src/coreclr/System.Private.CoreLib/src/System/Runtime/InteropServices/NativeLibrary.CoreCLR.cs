// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static partial class NativeLibrary
    {
        private static string[]? s_nativeDllSearchDirectories;

        private static IntPtr LoadFromNativeDllSearchDirectories(string libraryName, int flags, ref LoadLibErrorTracker errorTracker)
        {
            string[]? nativeDllSearchDirectories = s_nativeDllSearchDirectories;
            if (nativeDllSearchDirectories == null)
            {
                GetAppDomainNativeLibrarySearchPaths(ObjectHandleOnStack.Create(ref nativeDllSearchDirectories));
                Debug.Assert(nativeDllSearchDirectories != null);
                s_nativeDllSearchDirectories = nativeDllSearchDirectories;
            }

            IntPtr ret = IntPtr.Zero;

            foreach (string directory in nativeDllSearchDirectories)
            {
                string qualifiedPath = Path.Combine(directory, libraryName);
                if (Path.IsPathRooted(qualifiedPath))
                {
                    ret = LoadLibraryHelper(qualifiedPath, flags, ref errorTracker);
                    if (ret != IntPtr.Zero)
                        break;
                }
            }

            return ret;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "AppDomain_GetNativeLibrarySearchPaths")]
        private static partial void GetAppDomainNativeLibrarySearchPaths(ObjectHandleOnStack searchPaths);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "BundleNative_TryGetBundleInformation")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool TryGetBundleInformation([MarshalAs(UnmanagedType.Bool)] out bool isExtracted, StringHandleOnStack extractPath);
    }
}
