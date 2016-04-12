// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            string fileName = PlatformDetection.IsWindows7 ? "IdnaTest_Win7.txt" : "IdnaTest_6.txt";
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

                    if (!String.IsNullOrWhiteSpace(noComment))
                        yield return f(noComment, lineCount);
                }
            }
        }

        /// <summary>
        /// Abstracts retrieving the dataset so this can be changed depending on platform support, such as
        /// when the IDNA implementation is updated to a newer version of Unicode.  Windows currently supports
        /// and uses 6.0 in IDNA processing but 6.3 is the latest version available and may be used at
        /// some point.
        /// 
        /// This method retrieves the dataset to be used by the test.  Windows implementation uses transitional 
        /// mappings, which only affect 4 characters, known as deviations.  See the description at
        /// http://www.unicode.org/reports/tr46/#Deviations for more information.  Windows also throws an error
        /// when an empty string is given, so we want to filter that from the IDNA test set
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IConformanceIdnaTest> GetDataset()
        {
            foreach (var entry in ParseFile(GetIdnaTestTxt(), (line, lineCount) => PlatformDetection.IsWindows7 ?
                                                                                     (IConformanceIdnaTest) new Unicode_Win7_IdnaTest(line, lineCount) : 
                                                                                     (IConformanceIdnaTest) new Unicode_6_0_IdnaTest(line, lineCount))
                                                                                     )
            {
                if (entry.Type != IdnType.Nontransitional && entry.Source != String.Empty)
                    yield return entry;
            }
        }
    }
}
