// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        [DllImport(RuntimeHelpers.QCall, EntryPoint = "AssemblyNative_InternalTryGetRawMetadata")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool InternalTryGetRawMetadata(QCallAssembly assembly, ref byte* blob, ref int length);

        /// <summary>
        /// Retrieves the metadata section of the assembly, for use with <see cref="T:System.Reflection.Metadata.MetadataReader" />.
        /// </summary>
        /// <returns>
        /// Returns <see langword="false" /> upon failure. Metadata might not be available for some assemblies,
        /// such as <see cref="System.Reflection.Emit.AssemblyBuilder" />, AOT images, etc.
        /// </returns>
        /// <remarks>
        /// <para>Callers should not write to the metadata blob.</para>
        /// <para>The metadata blob pointer will remain valid as long as the assembly is alive.
        ///   The caller is responsible for keeping the assembly object alive while accessing the metadata blob.</para>
        /// </remarks>
        [CLSCompliant(false)] // out byte* blob
        public static unsafe bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            blob = null;
            length = 0;

            var runtimeAssembly = assembly as RuntimeAssembly;
            if (runtimeAssembly == null)
            {
                return false;
            }

            RuntimeAssembly rtAsm = runtimeAssembly;

            return InternalTryGetRawMetadata(new QCallAssembly(ref rtAsm), ref blob, ref length);
        }
    }
}
