// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace ConsoleApplication3
{
    public class Program
    {
        [Fact]
        public static void TestEntryPoint()
        {
            Repro<Program>(null);
        }

        static void Repro<T>(B<T> b)
            where T : Program
        {
        }
    }

    class A<T> { }
    class B<T> where T : class { }
}
