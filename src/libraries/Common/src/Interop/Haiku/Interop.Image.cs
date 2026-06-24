// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class Image
    {
        internal const int MAXPATHLEN = 1024;

        internal enum ImageType : int
        {
            B_APP_IMAGE = 1,
            B_LIBRARY_IMAGE,
            B_ADD_ON_IMAGE,
            B_SYSTEM_IMAGE,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct ImageInfo
        {
            public ImageType type;
            public fixed byte name[MAXPATHLEN];
            public void* text;
            public int text_size;
            public int data_size;
        }

        /// <summary>
        /// Gets information about images owned by a team.
        /// </summary>
        /// <param name="team">The team ID to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="ImageInfo"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more images to iterate.</returns>
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNextImageInfo")]
        internal static partial int GetNextImageInfo(int team, ref int cookie, out ImageInfo info);
    }
}
