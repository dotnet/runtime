// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable CA1823 // analyzer incorrectly flags fixed buffer length const (https://github.com/dotnet/roslyn/issues/37593)

internal static partial class Interop
{
    internal static partial class Image
    {
        internal const int MAXPATHLEN = 1024;

        [LibraryImportAttribute(Interop.Libraries.libroot, SetLastError = false)]
        private static unsafe partial int _get_next_image_info(int team, ref int cookie, out image_info info, nuint size);

        internal enum image_type : int
        {
            B_APP_IMAGE = 1,
            B_LIBRARY_IMAGE,
            B_ADD_ON_IMAGE,
            B_SYSTEM_IMAGE,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct image_info
        {
            public int id;
            public image_type type;
            public int sequence;
            public int init_order;
            public delegate* unmanaged<void> init_routine;
            public delegate* unmanaged<void> term_routine;
            public int device;
            public long node;
            public fixed byte name[MAXPATHLEN];
            public void* text;
            public void* data;
            public int text_size;
            public int data_size;
            public int api_version;
            public int abi;
        }

        /// <summary>
        /// Gets information about images owned by a team.
        /// </summary>
        /// <param name="team">The team ID to iterate.</param>
        /// <param name="cookie">A cookie to track the iteration.</param>
        /// <param name="info">The <see cref="image_info"/> structure to fill in.</param>
        /// <returns>Returns 0 on success. Returns an error code on failure or when there are no more images to iterate.</returns>
        internal static unsafe int GetNextImageInfo(int team, ref int cookie, out image_info info)
        {
            return _get_next_image_info(team, ref cookie, out info, (nuint)sizeof(image_info));
        }
    }
}
