// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace System.Globalization.Tests
{
    /// <summary>
    /// This suite of tests enumerate a set of input and outputs provided by the Unicode consortium.
    /// For more information, see the files within Data\[Unicode version].  This tests positive
    /// and negative test cases for both GetASCII and GetUnicode methods.
    /// </summary>
    public class IdnMappingIdnaConformanceTests
    {
        /// <summary>
        /// Tests positive cases for GetAscii.  Windows fails by design on some entries that should pass.  The recommendation is to take the source input
        /// for this if needed.
        /// 
        /// There are some others that failed which have been commented out and marked in the dataset as "GETASCII DOES FAILS ON WINDOWS 8.1"
        /// Same applies to Windows 10 >= 10.0.15063 in the IdnaTest_9.txt file
        [Fact]
        public void GetAscii_Success()
        {
            Assert.All(Factory.GetDataset().Where(e => e.ASCIIResult.Success), entry =>
            {
                try
                {
                    var map = new IdnMapping();
                    var asciiResult = map.GetAscii(entry.Source);
                    Assert.Equal(entry.ASCIIResult.Value, asciiResult, StringComparer.OrdinalIgnoreCase);
                }
                catch (ArgumentException)
                {
                    string actualCodePoints = GetCodePoints(entry.Source);
                    string expectedCodePoints = GetCodePoints(entry.ASCIIResult.Value);
                    throw new Exception($"Expected IdnMapping.GetAscii(\"{actualCodePoints}\" to return \"{expectedCodePoints}\".");
                }
            });
        }

        /// <summary>
        /// Tests positive cases for GetUnicode.  Windows fails by design on some entries that should pass.  The recommendation is to take the source input
        /// for this if needed.
        /// 
        /// There are some others that failed which have been commented out and marked in the dataset as "GETUNICODE DOES FAILS ON WINDOWS 8.1"
        /// Same applies to Windows 10 >= 10.0.15063 in the IdnaTest_9.txt file
        /// </summary>
        [Fact]
        public void GetUnicode_Success()
        {
            Assert.All(Factory.GetDataset().Where(e => e.UnicodeResult.Success && e.UnicodeResult.ValidDomainName), entry =>
            {
                try
                {
                    var map = new IdnMapping { UseStd3AsciiRules = true, AllowUnassigned = true };
                    var unicodeResult = map.GetUnicode(entry.Source);

                    Assert.Equal(entry.UnicodeResult.Value, unicodeResult, StringComparer.OrdinalIgnoreCase);
                }
                catch (ArgumentException)
                {
                    if (!string.Equals(entry.UnicodeResult.Value, entry.Source, StringComparison.OrdinalIgnoreCase))
                    {
                        string actualCodePoints = GetCodePoints(entry.Source);
                        string expectedCodePoints = GetCodePoints(entry.UnicodeResult.Value);
                        throw new Exception($"Expected IdnMapping.GetUnicode(\"{actualCodePoints}\" to return \"{expectedCodePoints}\".");
                    }
                }
            });
        }

        /// <summary>
        /// Tests negative cases for GetAscii.
        /// </summary>
        /// <remarks>
        /// There are some failures on Windows 8.1 that have been commented out 
        /// from the 6.0\IdnaTest.txt.  To find them, search for "GETASCII DOES NOT FAIL ON WINDOWS 8.1"
        /// Same applies to Windows 10 >= 10.0.15063 in the IdnaTest_9.txt file
        /// </remarks>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // https://github.com/dotnet/corefx/issues/21332
        public void GetAscii_Invalid()
        {
            Assert.All(Factory.GetDataset().Where(entry => !entry.ASCIIResult.Success), entry =>
            {
                try
                {
                    var map = new IdnMapping();
                    AssertExtensions.Throws<ArgumentException>("unicode", () => map.GetAscii(entry.Source));
                }
                catch (ThrowsException)
                {
                    string codePoints = GetCodePoints(entry.Source);
                    throw new Exception($"Expected IdnMapping.GetAscii(\"{codePoints}\") to throw an ArgumentException.");
                }
            });
        }

        /// <summary>
        /// Tests negative cases for GetUnicode.
        /// </summary>
        /// <remarks>
        /// There are some failures on Windows 8.1 that have been commented out 
        /// from the 6.0\IdnaTest.txt.  To find them, search for "GETUNICODE DOES NOT FAIL ON WINDOWS 8.1"
        /// Same applies to Windows 10 >= 10.0.15063 in the IdnaTest_9.txt file
        /// </remarks>
        [Fact]
        public void GetUnicode_Invalid()
        {
            Assert.All(Factory.GetDataset().Where(entry => !entry.UnicodeResult.Success), entry =>
            {
                try
                {
                    var map = new IdnMapping();
                    AssertExtensions.Throws<ArgumentException>("ascii", () => map.GetUnicode(entry.Source));
                }
                catch (ThrowsException)
                {
                    string codePoints = GetCodePoints(entry.Source);
                    throw new Exception($"Expected IdnMapping.GetUnicode(\"{codePoints}\") to throw an ArgumentException.");
                }
            });
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

        private static string GetCodePoints(string s) => string.Concat(s.Select(c => c <= 127 ? c.ToString() : $"\\u{(int)c:X4}"));
    }
}
