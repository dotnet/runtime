// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    // Extension to TargetDetails related to code generation
    public partial class TargetDetails
    {
        public TargetDetails(TargetArchitecture architecture, TargetOS targetOS, TargetAbi abi, SimdVectorLength simdVectorLength)
            : this(architecture, targetOS, abi)
        {
            MaximumSimdVectorLength = simdVectorLength;
        }

        /// <summary>
        /// Specifies the maximum size of native vectors on the target architecture.
        /// </summary>
        public SimdVectorLength MaximumSimdVectorLength
        {
            get;
        }
    }

    /// <summary>
    /// Specifies the size of native vectors.
    /// </summary>
    public enum SimdVectorLength
    {
        /// <summary>
        /// Specifies that native vectors are not supported.
        /// </summary>
        None,

        /// <summary>
        /// Specifies that native vectors are 128 bit (e.g. SSE on x86).
        /// </summary>
        Vector128Bit,

        /// <summary>
        /// Specifies that native vectors are 256 bit (e.g. AVX on x86).
        /// </summary>
        Vector256Bit,
    }
}
