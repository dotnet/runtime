// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace System.Globalization.Extensions.Tests
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
            foreach (var name in typeof(Factory).GetTypeInfo().Assembly.GetManifestResourceNames())
            {
                if (name.EndsWith("IdnaTest.txt", StringComparison.Ordinal))
                {
                    return typeof(Factory).GetTypeInfo().Assembly.GetManifestResourceStream(name);
                }
            }

            Assert.False(true, "Verify test file 'IdnaTest.txt' is included as an embedded resource");
            return null;
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
            foreach (var entry in ParseFile(GetIdnaTestTxt(), (line, lineCount) => new Unicode_6_0_IdnaTest(line, lineCount)))
            {
                if (entry.Type != IdnType.Nontransitional && entry.Source != String.Empty)
                    yield return entry;
            }
        }
    }
}
