// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        [DllImport(RuntimeHelpers.QCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool InternalTryGetRawMetadata(QCallAssembly assembly, ref byte* blob, ref int length);

        // Retrieves the metadata section of the assembly, for use with System.Reflection.Metadata.MetadataReader.
        //   - Returns false upon failure. Metadata might not be available for some assemblies, such as AssemblyBuilder, .NET
        //     native images, etc.
        //   - Callers should not write to the metadata blob
        //   - The metadata blob pointer will remain valid as long as the AssemblyLoadContext with which the assembly is
        //     associated, is alive. The caller is responsible for keeping the assembly object alive while accessing the
        //     metadata blob.
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

        [DllImport(RuntimeHelpers.QCall)]
        private static extern unsafe void ApplyUpdate(QCallAssembly assembly, byte* metadataDelta, int metadataDeltaLength, byte* ilDelta, int ilDeltaLength, byte* pdbDelta, int pdbDeltaLength);

        /// <summary>
        /// Updates the specified assembly using the provided metadata, IL and PDB deltas.
        /// </summary>
        /// <remarks>
        /// Currently executing methods will continue to use the existing IL. New executions of modified methods will
        /// use the new IL. Different runtimes may have different limitations on what kinds of changes are supported,
        /// and runtimes make no guarantees as to the state of the assembly and process if the delta includes
        /// unsupported changes.
        /// </remarks>
        /// <param name="assembly">The assembly to update.</param>
        /// <param name="metadataDelta">The metadata changes to be applied.</param>
        /// <param name="ilDelta">The IL changes to be applied.</param>
        /// <param name="pdbDelta">The PDB changes to be applied.</param>
        /// <exception cref="ArgumentNullException">The assembly argument is null.</exception>
        /// <exception cref="NotSupportedException">The update could not be applied.</exception>
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            RuntimeAssembly? runtimeAssembly = assembly as RuntimeAssembly;
            if (runtimeAssembly == null)
            {
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            }

            unsafe
            {
                RuntimeAssembly rtAsm = runtimeAssembly;
                fixed (byte* metadataDeltaPtr = metadataDelta, ilDeltaPtr = ilDelta, pdbDeltaPtr = pdbDelta)
                {
                    ApplyUpdate(new QCallAssembly(ref rtAsm), metadataDeltaPtr, metadataDelta.Length, ilDeltaPtr, ilDelta.Length, pdbDeltaPtr, pdbDelta.Length);
                }
            }
        }
    }
}
