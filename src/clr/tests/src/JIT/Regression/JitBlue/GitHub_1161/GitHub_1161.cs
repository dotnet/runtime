// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This test illustrates a limitation in the JIT in that it will not promote
// a struct that has a single double register. See GitHub issue #1161.

using System;
using System.Runtime.CompilerServices;

class GitHub_1161
{
    struct Number
    {
        private double value;
        public static implicit operator Number(double value)
        {
            return new Number { value = value };
        }
        public static implicit operator double(Number number)
        {
            return number.value;
        }
        public static Number operator +(Number x, Number y)
        {
            return x.value + y.value;
        }
    }
    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Test()
        {
            Number x = 4, y = 2;
            return (int)(x + y);
        }
        static int Main()
        {
            return (Test() == 6) ? 100 : -1;
        }
    }
}
