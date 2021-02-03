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
        private static extern unsafe int ApplyHotReloadUpdate(QCallAssembly assembly, byte* metadataDelta, int metadataDeltaLength, byte* ilDelta, int ilDeltaLength, byte* pdbDelta, int pdbDeltaLength);

        /// <summary>
        /// Hot reload update API
        /// Applies an update to the given assembly using the metadata, IL and PDB deltas. Currently executing
        /// methods will continue to use the existing IL. New executions of modified methods will use the new
        /// IL. The supported changes are runtime specific - different .NET runtimes may have different
        /// limitations. The runtime makes no guarantees if the delta includes unsupported changes.
        /// </summary>
        /// <param name="assembly">The assembly to update</param>
        /// <param name="metadataDelta">The metadata changes</param>
        /// <param name="ilDelta">The IL changes</param>
        /// <param name="pdbDelta">The PDB changes. Current not supported on .NET Core</param>
        /// <exception cref="ArgumentNullException">if assembly parameter is null</exception>
        /// <exception cref="NotSupportedException">update failed</exception>
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta = default)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            RuntimeAssembly? runtimeAssembly = assembly as RuntimeAssembly;
            if (runtimeAssembly == null)
            {
                throw new ArgumentException("Not a RuntimeAssembly", nameof(assembly));
            }

            unsafe
            {
                RuntimeAssembly rtAsm = runtimeAssembly;
                fixed (byte* metadataDeltaPtr = metadataDelta, ilDeltaPtr = ilDelta, pdbDeltaPtr = pdbDelta)
                {
                    if (ApplyHotReloadUpdate(
                        new QCallAssembly(ref rtAsm),
                        metadataDeltaPtr,
                        metadataDelta.Length,
                        ilDeltaPtr,
                        ilDelta.Length,
                        pdbDeltaPtr,
                        pdbDelta.Length) != 0)
                    {
                        throw new NotSupportedException();
                    }
                }
            }
        }
    }
}
