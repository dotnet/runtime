// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Regression test for failure to maintain FieldSeq annotations in EarlyProp
using System.Runtime.CompilerServices;

namespace N
{
    public static class C
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Exn()
        {
            int[] arr = { 1, 2, 3 };
            // When the "arr.Length" below gets replaced with "3", EarlyProp needs
            // to mark it with a ConstantIndex FieldSeq to avoid a downstream assertion
            // about lost FieldSeq annotations.
            return arr[0] + arr[arr.Length];
        }

        public static int Main(string[] args)
        {
            try
            {
                Exn();
                return -1;
            }
            catch (System.IndexOutOfRangeException) { }
            return 100;
        }
    }
}
