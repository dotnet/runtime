// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics.Arm
{
    // Used to specify or limit the number of elements used within an method.
    // Matches the field "pattern" within the Arm Architecture Reference Manual
    public enum SveMaskPattern : byte
    {
        /// <summary>
        ///   <para>POW2</para>
        /// </summary>
        LargestPowerOf2 = 0,      // The largest power of 2.

        /// <summary>
        ///   <para>VL1</para>
        /// </summary>
        VectorCount1 = 1,         // Exactly 1 element.

        /// <summary>
        ///   <para>VL2</para>
        /// </summary>
        VectorCount2 = 2,         // Exactly 2 elements.

        /// <summary>
        ///   <para>VL3</para>
        /// </summary>
        VectorCount3 = 3,         // Exactly 3 elements.

        /// <summary>
        ///   <para>VL4</para>
        /// </summary>
        VectorCount4 = 4,         // Exactly 4 elements.

        /// <summary>
        ///   <para>VL5</para>
        /// </summary>
        VectorCount5 = 5,         // Exactly 5 elements.

        /// <summary>
        ///   <para>VL6</para>
        /// </summary>
        VectorCount6 = 6,         // Exactly 6 elements.

        /// <summary>
        ///   <para>VL7</para>
        /// </summary>
        VectorCount7 = 7,         // Exactly 7 elements.

        /// <summary>
        ///   <para>VL8</para>
        /// </summary>
        VectorCount8 = 8,         // Exactly 8 elements.

        /// <summary>
        ///   <para>VL16</para>
        /// </summary>
        VectorCount16 = 9,        // Exactly 16 elements.

        /// <summary>
        ///   <para>VL32</para>
        /// </summary>
        VectorCount32 = 10,       // Exactly 32 elements.

        /// <summary>
        ///   <para>VL64</para>
        /// </summary>
        VectorCount64 = 11,       // Exactly 64 elements.

        /// <summary>
        ///   <para>VL128</para>
        /// </summary>
        VectorCount128 = 12,      // Exactly 128 elements.

        /// <summary>
        ///   <para>VL256</para>
        /// </summary>
        VectorCount256 = 13,      // Exactly 256 elements.

        /// <summary>
        ///   <para>MUL4</para>
        /// </summary>
        LargestMultipleOf4 = 29,  // The largest multiple of 4.

        /// <summary>
        ///   <para>MUL3</para>
        /// </summary>
        LargestMultipleOf3 = 30,  // The largest multiple of 3.

        /// <summary>
        ///   <para>ALL</para>
        /// </summary>
        All  = 31                 // All available (implicitly a multiple of two).
    }

    public enum SvePrefetchType : byte
    {
        /// <summary>
        ///   <para>PLDL1KEEP</para>
        /// </summary>
        LoadL1Temporal = 0,

        /// <summary>
        ///   <para>PLDL1STRM</para>
        /// </summary>
        LoadL1NonTemporal = 1,

        /// <summary>
        ///   <para>PLDL2KEEP</para>
        /// </summary>
        LoadL2Temporal = 2,

        /// <summary>
        ///   <para>PLDL2STRM</para>
        /// </summary>
        LoadL2NonTemporal = 3,

        /// <summary>
        ///   <para>PLDL3KEEP</para>
        /// </summary>
        LoadL3Temporal = 4,

        /// <summary>
        ///   <para>PLDL3STRM</para>
        /// </summary>
        LoadL3NonTemporal = 5,

        /// <summary>
        ///   <para>PSTL1KEEP</para>
        /// </summary>
        StoreL1Temporal = 8,

        /// <summary>
        ///   <para>PSTL1STRM</para>
        /// </summary>
        StoreL1NonTemporal = 9,

        /// <summary>
        ///   <para>PSTL2KEEP</para>
        /// </summary>
        StoreL2Temporal = 10,

        /// <summary>
        ///   <para>PSTL2STRM</para>
        /// </summary>
        StoreL2NonTemporal = 11,

        /// <summary>
        ///   <para>PSTL3KEEP</para>
        /// </summary>
        StoreL3Temporal = 12,

        /// <summary>
        ///   <para>PSTL3STRM</para>
        /// </summary>
        StoreL3NonTemporal = 13
    };
}
