// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Pointer
{
    static class PointerNative
    {
        [DllImport(nameof(PointerNative))]
        public static unsafe extern void Negate(bool* ptr);

        [DllImport(nameof(PointerNative))]
        public static unsafe extern void GetNaN(float* ptr);

        [DllImport(nameof(PointerNative))]
        public static unsafe extern void NegateDecimal(decimal* ptr);

        [DllImport(nameof(PointerNative))]
        public static unsafe extern void GetNaN(BlittableWrapper<float>* ptr);

        public struct BlittableWrapper<T>
        {
            public T Value;
        }
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public class Program
    {
        [Fact]
        public static unsafe void PointerToBool()
        {
            bool value = true;
            PointerNative.Negate(&value);
            Assert.False(value);
        }

        [Fact]
        public static unsafe void PointerToFloat()
        {
            float value = 1.0f;
            PointerNative.GetNaN(&value);
            Assert.True(float.IsNaN(value));
        }

        [Fact]
        public static unsafe void PointerToDecimal()
        {
            decimal value = 1.0m;
            PointerNative.NegateDecimal(&value);
            Assert.Equal(-1.0m, value);
        }

        [Fact]
        public static unsafe void PointerToStructOfGeneric()
        {
            PointerNative.BlittableWrapper<float> wrapper = new(){ Value = 1.0f };
            PointerNative.GetNaN(&wrapper);
            Assert.True(float.IsNaN(wrapper.Value));
        }
    }
}
