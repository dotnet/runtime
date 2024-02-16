// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace System.Globalization.Tests
{
    public static class Factory
    {
        /// <summary>
        /// Removes comments from the end of a line
        /// </summary>
        private static string RemoveComment(string line)
        {
            var idx = line.IndexOf("#", StringComparison.Ordinal);

            return idx < 0 ? line : line.Substring(0, idx);
        }

        /// <summary>
        /// Retrieves the IdnaTest.txt included in assembly as an embedded resource.
        /// </summary>
        private static Stream GetIdnaTestTxt()
        {
            string fileName = null;
            if (PlatformDetection.ICUVersion >= new Version(66, 0) || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
                fileName = "IdnaTest_13.txt";
            else if (PlatformDetection.IsWindows7)
                fileName = "IdnaTest_Win7.txt";
            else if (PlatformDetection.IsWindows10Version1903OrGreater)
                fileName = "IdnaTest_11.txt";
            else if (PlatformDetection.IsWindows10Version1703OrGreater)
                fileName = "IdnaTest_9.txt";
            else
                fileName = "IdnaTest_6.txt";

            // test file 'IdnaTest.txt' is included as an embedded resource
            var name = typeof(Factory).GetTypeInfo().Assembly.GetManifestResourceNames().First(n => n.EndsWith(fileName, StringComparison.Ordinal));
            return typeof(Factory).GetTypeInfo().Assembly.GetManifestResourceStream(name);
        }

        private static IEnumerable<IConformanceIdnaTest> ParseFile(Stream stream, Func<string, int, IConformanceIdnaTest> f)
        {
            using (var reader = new StreamReader(stream))
            {
                int lineCount = 0;
                while (!reader.EndOfStream)
                {
                    lineCount++;

                    var noComment = RemoveComment(reader.ReadLine());

                    if (!string.IsNullOrWhiteSpace(noComment))
                        yield return f(noComment, lineCount);
                }
            }
        }

        private static IConformanceIdnaTest GetConformanceIdnaTest(string line, int lineCount)
        {
            if (PlatformDetection.ICUVersion >= new Version(66, 0) || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
                return new Unicode_13_0_IdnaTest(line, lineCount);
            else if (PlatformDetection.IsWindows7)
                return new Unicode_Win7_IdnaTest(line, lineCount);
            else if (PlatformDetection.IsWindows10Version1903OrGreater)
                return new Unicode_11_0_IdnaTest(line, lineCount);
            else if (PlatformDetection.IsWindows10Version1703OrGreater)
                return new Unicode_9_0_IdnaTest(line, lineCount);
            else
                return new Unicode_6_0_IdnaTest(line, lineCount);
        }

        /// <summary>
        /// Abstracts retrieving the dataset so this can be changed depending on platform support, such as
        /// when the IDNA implementation is updated to a newer version of Unicode.  Windows 10 up to 1607 supports
        /// and uses 6.0 in IDNA processing. Windows 10 1703 and greater currently uses 9.0 in IDNA processing.
        ///
        /// This method retrieves the dataset to be used by the test.  Windows implementation uses transitional
        /// mappings, which only affect 4 characters, known as deviations.  See the description at
        /// http://www.unicode.org/reports/tr46/#Deviations for more information.  Windows also throws an error
        /// when an empty string is given, so we want to filter that from the IDNA test set
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IConformanceIdnaTest> GetDataset()
        {
            // Nls is transitional so we filter out non transitional test cases.
            // Icu is the opposite.
            IdnType idnFilter = PlatformDetection.IsNlsGlobalization || PlatformDetection.IsHybridGlobalizationOnApplePlatform ? IdnType.Nontransitional : IdnType.Transitional;
            foreach (var entry in ParseFile(GetIdnaTestTxt(), GetConformanceIdnaTest))
            {
                if (entry.Type != idnFilter && entry.Source != string.Empty)
                    yield return entry;
            }
        }
    }
}
