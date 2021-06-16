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
            if (assembly is not RuntimeAssembly runtimeAssembly)
            {
                if (assembly is null) throw new ArgumentNullException(nameof(assembly));
                throw new ArgumentException(SR.Argument_MustBeRuntimeAssembly);
            }

            // System.Private.CoreLib is not editable
            if (runtimeAssembly == typeof(AssemblyExtensions).Assembly)
                throw new InvalidOperationException (SR.InvalidOperation_AssemblyNotEditable);

            unsafe
            {
                IntPtr monoAssembly = runtimeAssembly.GetUnderlyingNativeHandle ();
                fixed (byte* metadataDeltaPtr = metadataDelta, ilDeltaPtr = ilDelta, pdbDeltaPtr = pdbDelta)
                {
                    ApplyUpdate_internal(monoAssembly, metadataDeltaPtr, metadataDelta.Length, ilDeltaPtr, ilDelta.Length, pdbDeltaPtr, pdbDelta.Length);
                }
            }
        }

        private static Lazy<string> s_ApplyUpdateCapabilities = new Lazy<string>(() => InitializeApplyUpdateCapabilities());

        internal static string GetApplyUpdateCapabilities() => s_ApplyUpdateCapabilities.Value;

        private static string InitializeApplyUpdateCapabilities()
        {
            return ApplyUpdateEnabled() != 0 ? "Baseline" : string.Empty ;
        }


        [MethodImpl (MethodImplOptions.InternalCall)]
        private static extern int ApplyUpdateEnabled ();

        [MethodImpl (MethodImplOptions.InternalCall)]
        private static unsafe extern void ApplyUpdate_internal (IntPtr base_assm, byte* dmeta_bytes, int dmeta_length, byte *dil_bytes, int dil_length, byte *dpdb_bytes, int dpdb_length);
    }
}
