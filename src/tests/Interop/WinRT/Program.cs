// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Xunit;

namespace WinRT
{
    [WindowsRuntimeImport]
    interface I {}

    public class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool ObjectIsI(object o) => o is I;

        [Fact]
        [SkipOnMono("WinRT interop was never supported on Mono, so blocking loading WinRT types was never added.")]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/182", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        public static void CannotLoadWinRTType()
        {
            Assert.Throws<TypeLoadException>(() => ObjectIsI(new object()));
        }
    }
}

