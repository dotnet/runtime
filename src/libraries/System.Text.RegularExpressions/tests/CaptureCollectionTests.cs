// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text.RegularExpressions;
using System.Collections;

namespace Test
{
    /// <summary>
    /// Tests the CaptureCollection class.
    /// </summary>
    public class CaptureCollectionTests
    {
        [Fact]
        public static void CaptureCollection_GetEnumeratorTest_Negative()
        {
            Regex rgx1 = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            String strInput = "aaabbccccccccccaaaabc";
            Match mtch1 = rgx1.Match(strInput);
            CaptureCollection captrc1 = mtch1.Captures;

            IEnumerator enmtr1 = captrc1.GetEnumerator();

            Capture currentCapture;

            Assert.Throws<InvalidOperationException>(() => currentCapture = (Capture)enmtr1.Current);


            for (int i = 0; i < captrc1.Count; i++)
            {
                enmtr1.MoveNext();
            }

            enmtr1.MoveNext();

            Assert.Throws<InvalidOperationException>(() => currentCapture = (Capture)enmtr1.Current);
            enmtr1.Reset();

            Assert.Throws<InvalidOperationException>(() => currentCapture = (Capture)enmtr1.Current);
        }

        [Fact]
        public static void CaptureCollection_GetEnumeratorTest()
        {
            Regex rgx1 = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            String strInput = "aaabbccccccccccaaaabc";
            Match mtch1 = rgx1.Match(strInput);
            CaptureCollection captrc1 = mtch1.Captures;

            IEnumerator enmtr1 = captrc1.GetEnumerator();

            for (int i = 0; i < captrc1.Count; i++)
            {
                enmtr1.MoveNext();

                Assert.Equal(enmtr1.Current, captrc1[i]);
            }

            Assert.False(enmtr1.MoveNext(), "Err_5! enmtr1.MoveNext returned true");

            enmtr1.Reset();

            for (int i = 0; i < captrc1.Count; i++)
            {
                enmtr1.MoveNext();

                Assert.Equal(enmtr1.Current, captrc1[i]);
            }
        }
    }
}
