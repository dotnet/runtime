// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text;

using Xunit;

namespace System.PrivateUri.Tests
{
    /// <summary>
    /// These tests attempt to verify the transitivity of Uri.MakeRelativeUri with new Uri(base, rel).
    /// Specifically these tests address the use of Unicode and Iri with explicit and implicit file Uris.
    /// Note: Many of the test only partially pass with the current Uri implementation.  Known discrepancies
    /// have been marked as expected so that we can still track changes.
    /// </summary>
    public class IriRelativeFileResolutionTest
    {
        private static readonly bool s_isWindowsSystem = PlatformDetection.IsWindows;

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitFileWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeImplicitTestFile = s_isWindowsSystem ? @"c:\path\path3\test.txt" : "/path/path3/test.txt";
            string nonUnicodeImplicitFileBase = s_isWindowsSystem ? @"c:\path\file.txt" : "/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(nonUnicodeImplicitTestFile,
                nonUnicodeImplicitFileBase, out string testResults);
            Assert.True((errorCount == 0), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitFileWithReservedChar_AllPropertiesTheSame()
        {
            string nonUnicodeImplicitTestFile = s_isWindowsSystem ? @"c:\path\path3\test.txt%25%" : "/path/path3/test.txt%25%";
            string nonUnicodeImplicitFileBase = s_isWindowsSystem ? @"c:\path\file.txt" : "/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(nonUnicodeImplicitTestFile,
                nonUnicodeImplicitFileBase, out string testResults);
            Assert.True((errorCount == 4), testResults);
            // AbsolutePath, AbsoluteUri, LocalPath, PathAndQuery
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitFileWithUnicodeIriOn_AllPropertiesTheSame()
        {
            string unicodeImplicitTestFile = s_isWindowsSystem ? @"c:\path\\u30AF\path3\\u30EB\u30DE.text" : "/path//u30AF/path3//u30EB/u30DE.text";
            string nonUnicodeImplicitFileBase = s_isWindowsSystem ? @"c:\path\file.txt" : "/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(unicodeImplicitTestFile,
                nonUnicodeImplicitFileBase, out string testResults);
            Assert.True((errorCount == 0), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitFileWithUnicodeAndReservedCharIriOn_AllPropertiesTheSame()
        {
            string unicodeImplicitTestFile = s_isWindowsSystem ? @"c:\path\\u30AF\path3\\u30EB\u30DE.text%25%" : "/path//u30AF/path3//u30EB/u30DE.text%25%";
            string nonUnicodeImplicitFileBase = s_isWindowsSystem ? @"c:\path\file.txt" : "/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(unicodeImplicitTestFile,
                nonUnicodeImplicitFileBase, out string testResults);
            Assert.True((errorCount == (s_isWindowsSystem ? 4 : 0)), testResults);
            // AbsolutePath, AbsoluteUri, LocalPath, PathAndQuery
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitUncWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeImplicitTestUnc = @"\\c\path\path3\test.txt";
            string nonUnicodeImplicitUncBase = @"\\c/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(nonUnicodeImplicitTestUnc,
                nonUnicodeImplicitUncBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unc paths must start with '\' on Unix
        public void IriRelativeResolution_CompareImplcitAndExplicitUncForwardSlashesWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeImplicitTestUnc = @"//c/path/path3/test.txt";
            string nonUnicodeImplicitUncBase = @"//c/path/file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(nonUnicodeImplicitTestUnc,
                nonUnicodeImplicitUncBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndExplicitUncWithUnicodeIriOn_AllPropertiesTheSame()
        {
            string unicodeImplicitTestUnc = @"\\c\path\\u30AF\path3\\u30EB\u30DE.text";
            string nonUnicodeImplicitUncBase = @"\\c\path\file.txt";

            int errorCount = RelatavizeRestoreCompareImplicitVsExplicitFiles(unicodeImplicitTestUnc,
                nonUnicodeImplicitUncBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        public static int RelatavizeRestoreCompareImplicitVsExplicitFiles(string original,
            string baseString, out string errors)
        {
            string fileSchemePrefix = s_isWindowsSystem ? "file:///" : "file://";
            Uri implicitTestUri = new Uri(original);
            Uri implicitBaseUri = new Uri(baseString);
            Uri explicitBaseUri = new Uri(fileSchemePrefix + baseString);

            Uri rel = implicitBaseUri.MakeRelativeUri(implicitTestUri);
            Uri implicitResultUri = new Uri(implicitBaseUri, rel);
            Uri explicitResultUri = new Uri(explicitBaseUri, rel);
            Type uriType = typeof(Uri);
            PropertyInfo[] infoList = uriType.GetProperties();
            StringBuilder testResults = new StringBuilder();
            int errorCount = 0;
            foreach (PropertyInfo info in infoList)
            {
                string implicitValue = info.GetValue(implicitResultUri, null).ToString();
                string explicitValue = info.GetValue(explicitResultUri, null).ToString();
                if (!(implicitValue.Equals(explicitValue) || (fileSchemePrefix + implicitValue).Equals(explicitValue)))
                {
                    errorCount++;
                    testResults.Append($"Property mismatch: {info.Name}, implicit value: {implicitValue}, explicit value: {explicitValue}; ");
                }
            }

            string implicitString = implicitResultUri.ToString();
            string explicitString = explicitResultUri.ToString();
            if (!implicitString.Equals(explicitString))
            {
                errorCount++;
                testResults.Append($"ToString mismatch; implicit value: {implicitString}, explicit value: {explicitString}");
            }

            errors = testResults.ToString();
            return errorCount;
        }

        [Fact]
        public void IriRelativeResolution_CompareImplcitAndOriginalFileWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeImplicitTestFile = s_isWindowsSystem ? @"c:\path\path3\test.txt" : "/path/path3/test.txt";
            string nonUnicodeImplicitFileBase = s_isWindowsSystem ? @"c:\path\file.txt" : "/path/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(nonUnicodeImplicitTestFile,
                nonUnicodeImplicitFileBase, out string testResults);
            Assert.True((errorCount == (s_isWindowsSystem ? 1 : 0)), testResults);
            if (s_isWindowsSystem)
            {
                Assert.True(IsOriginalString(testResults), testResults);
            }
        }

        [Fact]
        public void IriRelativeResolution_CompareUncAndOriginalFileWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeUncTestFile = @"\\c\path\path3\test.txt";
            string nonUnicodeUncFileBase = @"\\c\path\file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(nonUnicodeUncTestFile,
                nonUnicodeUncFileBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Unc paths must start with '\' on Unix
        public void IriRelativeResolution_CompareUncForwardSlashesAndOriginalFileWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeUncTestFile = @"//c/path/path3/test.txt";
            string nonUnicodeUncFileBase = @"//c/path/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(nonUnicodeUncTestFile,
                nonUnicodeUncFileBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithNoUnicode_AllPropertiesTheSame()
        {
            string nonUnicodeTest = @"http://user:password@host.com:9090/path/path3/test.txt";
            string nonUnicodeBase = @"http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(nonUnicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 0), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithNoUnicodeAndReservedChar_AllPropertiesTheSame()
        {
            string nonUnicodeTest = @"http://user:password@host.com:9090/path/path3/test.txt%25%";
            string nonUnicodeBase = @"http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(nonUnicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithUnicodeIriOff_AllPropertiesTheSame()
        {
            string unicodeTest = "http://user:password@host.com:9090/path\u30AF/path3/\u30EBtest.txt";
            string nonUnicodeBase = "http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(unicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithUnicodeAndReservedCharIriOff_AllPropertiesTheSame()
        {
            string unicodeTest = "http://user:password@host.com:9090/path\u30AF/path3/\u30EBtest.txt%25%";
            string nonUnicodeBase = "http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(unicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithUnicodeIriOn_AllPropertiesTheSame()
        {
            string unicodeTest = "http://user:password@host.com:9090/path\u30AF/path3/\u30EBtest.txt";
            string nonUnicodeBase = "http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(unicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        [Fact]
        public void IriRelativeResolution_CompareRelativeAndOriginalHttpWithUnicodeAndReservedCharIriOn_AllPropertiesTheSame()
        {
            string unicodeTest = "http://user:password@host.com:9090/path\u30AF/path3/\u30EBtest.txt%25%";
            string nonUnicodeBase = "http://user:password@host.com:9090/path2/file.txt";

            int errorCount = RelatavizeRestoreCompareVsOriginal(unicodeTest,
                nonUnicodeBase, out string testResults);
            Assert.True((errorCount == 1), testResults);
            Assert.True(IsOriginalString(testResults), testResults);
        }

        public static int RelatavizeRestoreCompareVsOriginal(string original, string baseString, out string errors)
        {
            Uri startUri = new Uri(original);
            Uri baseUri = new Uri(baseString);

            Uri rel = baseUri.MakeRelativeUri(startUri);
            //string relString = rel.ToString(); // For debugging

            Uri stage2Uri = new Uri(baseUri, rel); // Test for true transitivity with an extra cycle
            rel = baseUri.MakeRelativeUri(stage2Uri);
            Uri resultUri = new Uri(baseUri, rel);

            Type uriType = typeof(Uri);
            PropertyInfo[] infoList = uriType.GetProperties();
            StringBuilder testResults = new StringBuilder();
            int errorCount = 0;
            foreach (PropertyInfo info in infoList)
            {
                string resultValue = info.GetValue(resultUri, null).ToString();
                string startValue = info.GetValue(startUri, null).ToString();
                if (!resultValue.Equals(startValue))
                {
                    errorCount++;
                    testResults.Append($"Property mismatch: {info.Name}, result value: {resultValue}, start value: {startValue}; ");
                }
            }

            string resultString = resultUri.ToString();
            string startString = startUri.ToString();
            if (!resultString.Equals(startString))
            {
                errorCount++;
                testResults.Append($"ToString mismatch; result value: {resultString}, start value: {startString}");
            }

            errors = testResults.ToString();
            return errorCount;
        }

        private static bool IsOriginalString(string error)
        {
            return error.StartsWith("Property mismatch: OriginalString,");
        }
    }
}
