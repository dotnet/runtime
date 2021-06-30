// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        [CLSCompliant(false)]
        public static unsafe bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length) => throw new NotImplementedException();

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
            MetadataUpdater.ApplyUpdate(assembly, metadataDelta, ilDelta, pdbDelta);
        }

        internal static string GetApplyUpdateCapabilities() => MetadataUpdater.GetCapabilities();
    }
}
