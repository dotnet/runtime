// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>Provides access to the LoongArch64 LAM hardware instructions via intrinsics.</summary>
    [Intrinsic]
    internal abstract class LAM
    {
        internal LAM() { }

        [Intrinsic]
        internal abstract class BH
        {
            internal BH() { }
            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            internal static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>AMSWAP[_DB].B rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static byte Exchange(ref byte location1, byte value) => Exchange(ref location1, value);

            /// <summary>
            ///   <para>AMSWAP[_DB].H rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static ushort Exchange(ref ushort location1, ushort value) => Exchange(ref location1, value);
        }

        [Intrinsic]
        internal abstract class CAS
        {
            internal CAS() { }
            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            internal static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>AMCAS[_DB].B rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static byte CompareExchange(ref byte location1, byte value, byte comparand) => CompareExchange(ref location1, value, comparand);

            /// <summary>
            ///   <para>AMCAS[_DB].H rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static ushort CompareExchange(ref ushort location1, ushort value, ushort comparand) => CompareExchange(ref location1, value, comparand);

            /// <summary>
            ///   <para>AMCAS[_DB].W rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static int CompareExchange(ref int location1, int value, int comparand) => CompareExchange(ref location1, value, comparand);

            /// <summary>
            ///   <para>AMCAS[_DB].W rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static unsafe int CompareExchange(int* location1, int value, int comparand) => CompareExchange(location1, value, comparand);

            /// <summary>
            ///   <para>AMCAS[_DB].D rd, rk, rj</para>
            ///   <para>This is the newly added atomic instruction on ISA1.1</para>
            /// </summary>
            internal static long CompareExchange(ref long location1, long value, long comparand) => CompareExchange(ref location1, value, comparand);
        }
    }
}
