// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Wasm
{
    [CLSCompliant(false)]
    public abstract class WasmBase
    {
        public bool IsSupported { get; }

        public static Vector128<byte> Constant(ulong p1, ulong p2) { throw new PlatformNotSupportedException(); }

        public static Vector128<sbyte>  Splat(sbyte  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte>   Splat(byte   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<short>  Splat(short  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<ushort> Splat(ushort x) { throw new PlatformNotSupportedException(); }
        public static Vector128<int>    Splat(int    x) { throw new PlatformNotSupportedException(); }
        public static Vector128<uint>   Splat(uint   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<long>   Splat(long   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<ulong>  Splat(ulong  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<float>  Splat(float  x) { throw new PlatformNotSupportedException(); }
        public static Vector128<double> Splat(double x) { throw new PlatformNotSupportedException(); }
        public static Vector128<nint>   Splat(nint   x) { throw new PlatformNotSupportedException(); }
        public static Vector128<nuint>  Splat(nuint  x) { throw new PlatformNotSupportedException(); }

        public static int    ExtractLane(Vector128<sbyte>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<byte>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<short>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<ushort> a, byte imm) { throw new PlatformNotSupportedException(); }
        public static int    ExtractLane(Vector128<int>    a, byte imm) { throw new PlatformNotSupportedException(); }
        public static uint   ExtractLane(Vector128<uint>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static long   ExtractLane(Vector128<long>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static ulong  ExtractLane(Vector128<ulong>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static float  ExtractLane(Vector128<float>  a, byte imm) { throw new PlatformNotSupportedException(); }
        public static double ExtractLane(Vector128<double> a, byte imm) { throw new PlatformNotSupportedException(); }
        public static nint   ExtractLane(Vector128<nint>   a, byte imm) { throw new PlatformNotSupportedException(); }
        public static nuint  ExtractLane(Vector128<nuint>  a, byte imm) { throw new PlatformNotSupportedException(); }
    }
}
