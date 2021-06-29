// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Metadata
{
    public static class MetadataUpdater
    {
        [DllImport(RuntimeHelpers.QCall)]
        private static extern unsafe void ApplyUpdate(QCallAssembly assembly, byte* metadataDelta, int metadataDeltaLength, byte* ilDelta, int ilDeltaLength, byte* pdbDelta, int pdbDeltaLength);

        [DllImport(RuntimeHelpers.QCall)]
        private static extern unsafe bool IsApplyUpdateSupported();

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
        /// <exception cref="ArgumentException">The assembly argument is not a runtime assembly.</exception>
        /// <exception cref="ArgumentNullException">The assembly argument is null.</exception>
        /// <exception cref="InvalidOperationException">The assembly is not editable.</exception>
        /// <exception cref="NotSupportedException">The update could not be applied.</exception>
        public static void ApplyUpdate(Assembly assembly, ReadOnlySpan<byte> metadataDelta, ReadOnlySpan<byte> ilDelta, ReadOnlySpan<byte> pdbDelta)
        {
            if (assembly is not RuntimeAssembly runtimeAssembly)
            {
                if (assembly is null) throw new ArgumentNullException(nameof(assembly));
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

        /// <summary>
        /// Returns the metadata update capabilities.
        /// </summary>
        internal static string GetCapabilities() => "Baseline AddMethodToExistingType AddStaticFieldToExistingType AddInstanceFieldToExistingType NewTypeDefinition ChangeCustomAttributes";

        /// <summary>
        /// Returns true if the apply assembly update is enabled and available.
        /// </summary>
        public static bool IsSupported { get; } = IsApplyUpdateSupported();
    }
}
