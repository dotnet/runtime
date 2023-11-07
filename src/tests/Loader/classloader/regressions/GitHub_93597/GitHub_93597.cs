// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class ReproGH93597 {
        [Fact]
        public static int TestEntryPoint() {
                var expected = new int[] {5,4,3,2,1};

                const int LowerBound = 5;

                var expectedNzlba = NonZeroLowerBoundArray(expected, LowerBound);

                return Helper(expectedNzlba);
                return 100;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Helper(Array a) {
                IEnumerable<int> ie = null;
                try {
                        ie = (IEnumerable<int>)a;
                } catch (InvalidCastException) {
                        Console.WriteLine ("caught ICE, good");
                        return 100;
                }
                ie.GetEnumerator(); // mono crashes here
                return 101;
        }


        private static Array NonZeroLowerBoundArray(Array szArrayContents, int lowerBound)
        {
                Array array = Array.CreateInstance(szArrayContents.GetType().GetElementType(), new int[] { szArrayContents.Length }, new int[] { lowerBound });
                for (int i = 0; i < szArrayContents.Length; i++)
                {
                        array.SetValue(szArrayContents.GetValue(i), i + lowerBound);
                }
                return array;
        }
                
}

