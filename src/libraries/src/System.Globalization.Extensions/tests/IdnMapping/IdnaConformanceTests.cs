// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;

namespace System.Globalization.Extensions.Tests
{
    /// <summary>
    /// This suite of tests enumerate a set of input and outputs provided by the Unicode consortium.
    /// For more information, see the files within Data\[Unicode version].  This tests positive
    /// and negative test cases for both GetASCII and GetUnicode methods.
    /// </summary>
    public class IdnaConformanceTests
    {
        /// <summary>
        /// Tests whether the expected object is equal to the actual object as determined by
        /// the supplied comparer and throws an exception if it is not.
        /// </summary>
        /// <param name="expected">Expected object.</param>
        /// <param name="actual">Actual object.</param>
        /// <param name="comparer">Comparer to be used to determine equality of objects.</param>
        /// <param name="message">Message to display upon failure.</param>
        public static void CompareResult<T>(T expected, T actual, IComparer<T> comparer, string message)
        {
            if (comparer.Compare(expected, actual) != 0)
            {
                Assert.False(true, string.Format(@"Expected: <{1}>. Actual:<{2}>. {0}", message, expected, actual));
            }
        }

        /// <summary>
        /// Tests positive cases for GetAscii. 
        /// </summary>
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void TestAsciiPositive()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (entry.GetASCIIResult.Success)
                {
                    var map = new System.Globalization.IdnMapping();
                    var asciiResult = map.GetAscii(entry.Source);

                    CompareResult(entry.GetASCIIResult.Value, asciiResult, StringComparer.OrdinalIgnoreCase, "Error on line number " + entry.LineNumber);
                }
            }
        }

        /// <summary>
        /// Tests positive cases for GetUnicode.  Windows fails by design on some entries that should pass.  The recommendation is to take the source input
        /// for this if needed.
        /// 
        /// There are some others that failed which have been commented out and marked in the dataset as "GETUNICODE DOES FAILS ON WINDOWS 8.1"
        /// </summary>
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void TestUnicodePositive()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (entry.GetUnicodeResult.Success)
                {
                    try
                    {
                        var map = new System.Globalization.IdnMapping { UseStd3AsciiRules = true, AllowUnassigned = true };
                        var unicodeResult = map.GetUnicode(entry.Source);

                        CompareResult(entry.GetUnicodeResult.Value, unicodeResult, StringComparer.OrdinalIgnoreCase, "Error on line number " + entry.LineNumber);
                    }
                    catch (ArgumentException)
                    {
                        CompareResult(entry.GetUnicodeResult.Value, entry.Source, StringComparer.OrdinalIgnoreCase, "Error on line number " + entry.LineNumber);
                    }
                }
            }
        }

        /// <summary>
        /// Tests negative cases for GetAscii.
        /// </summary>
        /// <remarks>
        /// There are some failures on Windows 8.1 that have been commented out 
        /// from the 6.0\IdnaTest.txt.  To find them, search for "GETASCII DOES NOT FAIL ON WINDOWS 8.1"
        /// </remarks>
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void TestAsciiNegative()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (!entry.GetASCIIResult.Success)
                {
                    var map = new System.Globalization.IdnMapping();
                    Assert.Throws<ArgumentException>(() => map.GetAscii(entry.Source));
                }
            }
        }

        /// <summary>
        /// Tests negative cases for GetUnicode.
        /// </summary>
        /// <remarks>
        /// There are some failures on Windows 8.1 that have been commented out 
        /// from the 6.0\IdnaTest.txt.  To find them, search for "GETUNICODE DOES NOT FAIL ON WINDOWS 8.1"
        /// </remarks>
        [Fact]
        [ActiveIssue(810, PlatformID.Linux | PlatformID.OSX)]
        public void TestUnicodeNegative()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (!entry.GetUnicodeResult.Success)
                {
                    var map = new System.Globalization.IdnMapping();
                    Assert.Throws<ArgumentException>(() => map.GetUnicode(entry.Source));
                }
            }
        }
    }
}