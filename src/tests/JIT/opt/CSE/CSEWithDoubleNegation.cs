// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace CSEWithDoubleNegation
{
    class DoNotMorphAwayCSEThatRepresentsDoubleNegation
    {
        private static int _static = 0;

        static int Main(string[] args)
        {
            if (DoubleNeg() != 22)
            {
                Console.WriteLine("DoubleNeg() failed to return the expected value of 22");
                return -1;
            }

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int DoubleNeg()
        {
            var a = 21;
            _static = 42;

            return 43 - (0 - (a - _static));
        }
    }
}

