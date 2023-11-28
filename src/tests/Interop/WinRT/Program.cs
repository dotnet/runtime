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
        public static int TestEntryPoint()
        {
            try
            {
                Assert.Throws<TypeLoadException>(() => ObjectIsI(new object()));
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
                return 101;
            }
            return 100;
        }
    }
}

