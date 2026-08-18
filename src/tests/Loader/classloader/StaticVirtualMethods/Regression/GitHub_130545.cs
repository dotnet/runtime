// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TestLibrary;
using Xunit;

// Regression test for https://github.com/dotnet/runtime/issues/130545
//
// Creating a delegate over a static abstract interface method that is resolved
// through generic sharing (gsharedvt) trapped under Mono wasm AOT (llvmonly) with
// "function signature mismatch": the generic newobj delegate-ctor fallback passed a
// runtime generic-sharing argument to the runtime delegate constructor, which does
// not accept one, so the containing static constructor's call_indirect hit a wasm
// signature mismatch. This mirrors the SixLabors.ImageSharp PixelOperations<TPixel>
// shape from the original report.
//
// The reproduction is AOT-heuristic sensitive: the value-type generic method
// (Decode<Rgba32>) must stay on the shared gsharedvt path, which is why it is
// reached only through reflection and why the surrounding types (a non-generic
// IPixel base with its own static abstract, a second static abstract factory,
// a derived PixelOperations override and the FromRgba32/FromRgba32Bytes work in
// Decode) are all present - reducing them further lets the JIT specialize the
// method and masks the bug. Verified against the Mono wasm AOT browser sample.
namespace GitHub_130545
{
    public interface IPixel
    {
        static abstract int GetPixelTypeInfo();

        Rgba32 ToRgba32();
    }

    public interface IPackedVector<TPacked>
        where TPacked : struct
    {
        TPacked PackedValue { get; set; }
    }

    public interface IPixel<TSelf> : IPixel, IEquatable<TSelf>
        where TSelf : unmanaged, IPixel<TSelf>
    {
        static abstract PixelOperations<TSelf> CreatePixelOperations();

        static abstract TSelf FromRgba32(Rgba32 source);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rgba32 : IPixel<Rgba32>, IPackedVector<uint>
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public uint PackedValue
        {
            get => Unsafe.As<Rgba32, uint>(ref this);
            set => Unsafe.As<Rgba32, uint>(ref this) = value;
        }

        public static int GetPixelTypeInfo() => 32;

        public static PixelOperations<Rgba32> CreatePixelOperations() => new PixelOperations();

        public static Rgba32 FromRgba32(Rgba32 source) => source;

        public readonly Rgba32 ToRgba32() => this;

        public readonly bool Equals(Rgba32 other) =>
            R == other.R && G == other.G && B == other.B && A == other.A;

        // Mirrors ImageSharp's nested Rgba32.PixelOperations : PixelOperations<Rgba32>.
        internal sealed class PixelOperations : PixelOperations<Rgba32>
        {
            public override void FromRgba32Bytes(Span<Rgba32> destination)
            {
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = new Rgba32 { R = 8, G = 8, B = 8, A = 255 };
                }
            }
        }
    }

    public class PixelOperations<TPixel>
        where TPixel : unmanaged, IPixel<TPixel>
    {
        // The static constructor builds a Lazy<> from the static-abstract method group
        // TPixel.CreatePixelOperations (ldftn of a static virtual + newobj delegate).
        private static readonly Lazy<PixelOperations<TPixel>> s_instance = new(TPixel.CreatePixelOperations, true);

        public static PixelOperations<TPixel> Instance => s_instance.Value;

        public virtual void FromRgba32Bytes(Span<TPixel> destination)
        {
        }
    }

    public static class PngDecoder
    {
        public static Rgba32 Decode<TPixel>()
            where TPixel : unmanaged, IPixel<TPixel>
        {
            PixelOperations<TPixel> ops = PixelOperations<TPixel>.Instance;

            Rgba32 source = new() { R = 1, G = 2, B = 3, A = 4 };
            Span<TPixel> destination = stackalloc TPixel[1];
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = TPixel.FromRgba32(source);
            }

            ops.FromRgba32Bytes(destination);
            return destination[0].ToRgba32();
        }
    }

    public class Test
    {
        [ActiveIssue("https://github.com/dotnet/runtime/issues/132030", typeof(Utilities), nameof(Utilities.IsNativeAot))]
        [Fact]
        public static void DelegateOverStaticAbstractThroughGsharedvt()
        {
            // Invoke through reflection so the runtime uses the shared (gsharedvt)
            // instantiation of Decode<TPixel>. A direct Decode<Rgba32>() call would be
            // fully specialized and hide the regression (it is a known workaround).
            MethodInfo decode = typeof(PngDecoder)
                .GetMethod(nameof(PngDecoder.Decode))!
                .MakeGenericMethod(typeof(Rgba32));

            Rgba32 result = (Rgba32)decode.Invoke(null, null)!;

            // The nested Rgba32.PixelOperations override sets every pixel to (8, 8, 8, 255).
            Assert.Equal(8, result.R);
            Assert.Equal(8, result.G);
            Assert.Equal(8, result.B);
            Assert.Equal(255, result.A);
        }
    }
}
