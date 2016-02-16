// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Net.NetworkInformation.Tests
{
    internal static class FileUtil
    {
        /// <summary>
        /// I'm storing the test text assets with their original line endings ('\n').
        /// The parsing logic depends on Environment.NewLine, so we normalize beforehand.
        /// </summary>
        public static void NormalizeLineEndings(string source, string normalizedDest)
        {
            string contents = File.ReadAllText(TestFile(source));
            if (Environment.NewLine == "\r\n")
            {
                if (!contents.Contains(Environment.NewLine))
                {
                    contents = contents.Replace("\n", "\r\n");
                }
            }
            else if (Environment.NewLine == "\n")
            {
                if (contents.Contains("\r\n"))
                {
                    contents = contents.Replace("\r\n", "\n");
                }
            }

            File.WriteAllText(normalizedDest, contents);
        }

        /// <summary>
        /// Gets a path to the desired test file
        /// </summary>
        /// <returns>The correct path to a valid test file</returns>
        public static string TestFile(string source)
        {
            return Path.Combine("NetworkFiles", source);
        }
    }
}
