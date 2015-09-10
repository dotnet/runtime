// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        [DllImport(JitHelpers.QCall)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private unsafe static extern bool InternalTryGetRawMetadata(RuntimeAssembly assembly, ref byte* blob, ref int length);

        // Retrieves the metadata section of the assembly, for use with System.Reflection.Metadata.MetadataReader.
        //   - Returns false upon failure. Metadata might not be available for some assemblies, such as AssemblyBuilder, .NET
        //     native images, etc.
        //   - Callers should not write to the metadata blob
        //   - The metadata blob pointer will remain valid as long as the AssemblyLoadContext with which the assembly is
        //     associated, is alive. The caller is responsible for keeping the assembly object alive while accessing the
        //     metadata blob.
        [CLSCompliant(false)] // out byte* blob
        public unsafe static bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            blob = null;
            length = 0;
            return InternalTryGetRawMetadata((RuntimeAssembly)assembly, ref blob, ref length);
        }
    }
}
