// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    /// <summary>
    /// This suite of tests enumerate a set of input and outputs provided by the Unicode consortium.
    /// For more information, see the files within Data\[Unicode version].  This tests positive
    /// and negative test cases for both GetASCII and GetUnicode methods.
    /// </summary>
    public class IdnMappingIdnaConformanceTests
    {
        [Fact]
        public void GetAscii_Success()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (entry.GetASCIIResult.Success)
                {
                    var map = new IdnMapping();
                    var asciiResult = map.GetAscii(entry.Source);
                    Assert.Equal(entry.GetASCIIResult.Value, asciiResult, StringComparer.OrdinalIgnoreCase);
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
        public void GetUnicode_Succes()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (entry.GetUnicodeResult.Success)
                {
                    try
                    {
                        var map = new IdnMapping { UseStd3AsciiRules = true, AllowUnassigned = true };
                        var unicodeResult = map.GetUnicode(entry.Source);

                        Assert.Equal(entry.GetUnicodeResult.Value, unicodeResult, StringComparer.OrdinalIgnoreCase);
                    }
                    catch (ArgumentException)
                    {
                        Assert.Equal(entry.GetUnicodeResult.Value, entry.Source, StringComparer.OrdinalIgnoreCase);
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
        public void GetAscii_Invalid()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (!entry.GetASCIIResult.Success)
                {
                    var map = new IdnMapping();
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
        public void GetUnicode_Invalid()
        {
            foreach (var entry in Factory.GetDataset())
            {
                if (!entry.GetUnicodeResult.Success)
                {
                    var map = new IdnMapping();
                    Assert.Throws<ArgumentException>(() => map.GetUnicode(entry.Source));
                }
            }
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public static void Equals(bool allowUnassigned, bool useStd3AsciiRules)
        {
            // first check for equals
            IdnMapping original = new IdnMapping() { AllowUnassigned = allowUnassigned, UseStd3AsciiRules = useStd3AsciiRules };
            IdnMapping identical = new IdnMapping() { AllowUnassigned = allowUnassigned, UseStd3AsciiRules = useStd3AsciiRules };
            Assert.True(original.Equals(identical));
            Assert.Equal(original.GetHashCode(), identical.GetHashCode());

            //  now three sets of unequals
            IdnMapping unequal1 = new IdnMapping() { AllowUnassigned = allowUnassigned, UseStd3AsciiRules = !useStd3AsciiRules };
            Assert.False(original.Equals(unequal1));
            Assert.NotEqual(original.GetHashCode(), unequal1.GetHashCode());

            IdnMapping unequal2 = new IdnMapping() { AllowUnassigned = !allowUnassigned, UseStd3AsciiRules = useStd3AsciiRules };
            Assert.False(original.Equals(unequal2));
            Assert.NotEqual(original.GetHashCode(), unequal2.GetHashCode());

            IdnMapping unequal3 = new IdnMapping() { AllowUnassigned = !allowUnassigned, UseStd3AsciiRules = useStd3AsciiRules };
            Assert.False(original.Equals(unequal3));
            Assert.NotEqual(original.GetHashCode(), unequal3.GetHashCode());
        }
    }
}
