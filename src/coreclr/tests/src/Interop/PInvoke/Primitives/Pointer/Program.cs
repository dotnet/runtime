// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace NonBlittablePointer
{
    static class NonBlittablePointerNative
    {
        [DllImport(nameof(NonBlittablePointerNative))]
        public static unsafe extern void Negate(bool* ptr);
    }

    class Program
    {
        static unsafe int Main(string[] args)
        {
            bool value = true;
            NonBlittablePointerNative.Negate(&value);
            return value == false ? 100 : 101;
        }
    }
}
