// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace NonBlittablePointer
{
    static class NonBlittablePointerNative
    {
        [DllImport(nameof(NonBlittablePointerNative))]
        public static unsafe extern void Negate(bool* ptr);
    }

    public class Program
    {
        [Fact]
        public static unsafe int TestEntryPoint()
        {
            bool value = true;
            NonBlittablePointerNative.Negate(&value);
            return value == false ? 100 : 101;
        }
    }
}
