// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public partial class RuntimeFeature
    {
        public static bool IsDynamicCodeSupported
        {
            [Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
            get => IsDynamicCodeSupported;
        }

        public static bool IsDynamicCodeCompiled
        {
            [Intrinsic]  // the JIT/AOT compiler will change this flag to false for FullAOT scenarios, otherwise true
            get => IsDynamicCodeCompiled;
        }

#if !FEATURE_METADATA_UPDATE
        internal static void LoadMetadataUpdate (Assembly assm, byte[] dmeta_data, byte[] dil_data) {
            throw new NotSupportedException ("Method body replacement not supported in this runtime");
        }
#else
        [MethodImplAttribute (MethodImplOptions.InternalCall)]
        private static unsafe extern void LoadMetadataUpdate_internal (IntPtr base_assm, byte* dmeta_bytes, int dmeta_length, byte *dil_bytes, int dil_length);

        internal static void LoadMetadataUpdate (Assembly assm, byte[] dmeta_data, byte[] dil_data) {
            unsafe {
                fixed (byte* dmeta_bytes = dmeta_data)
                fixed (byte* dil_bytes = dil_data) {
                    IntPtr mono_assembly = ((RuntimeAssembly)assm).GetUnderlyingNativeHandle ();
                    LoadMetadataUpdate_internal (mono_assembly, dmeta_bytes, dmeta_data.Length, dil_bytes, dil_data.Length);
                }
            }
        }
#endif
    }
}
