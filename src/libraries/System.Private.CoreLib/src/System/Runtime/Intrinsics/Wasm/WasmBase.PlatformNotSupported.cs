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

        // Constructing SIMD Values

        //public static Vector128<byte> Constant(ImmByte16 imm) { throw new PlatformNotSupportedException(); }
        public static Vector128<byte> Constant(ulong p1, ulong p2) { throw new PlatformNotSupportedException(); }

        // public static Vector128<sbyte>  SplatByte(int    x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<byte>   SplatByte(uint   x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<short>  SplatShort(int    x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<ushort> SplatShort(uint   x) { throw new PlatformNotSupportedException(); }
        
        // public static Vector128<int>    Splat(int    x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<uint>   Splat(uint   x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<long>   Splat(long   x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<ulong>  Splat(ulong  x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<float>  Splat(float  x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<double> Splat(double x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<nint>   Splat(nint   x) { throw new PlatformNotSupportedException(); }
        // public static Vector128<nuint>  Splat(nuint  x) { throw new PlatformNotSupportedException(); }
    }

    [CLSCompliant(false)]
    public unsafe struct ImmByte16
    {
        public fixed byte bytes[16];
    }
}
