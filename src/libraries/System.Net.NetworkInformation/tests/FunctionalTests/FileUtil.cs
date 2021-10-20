// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            string contents = File.ReadAllText(source);
            string normalizedContents = contents.ReplaceLineEndings();
            File.WriteAllText(normalizedDest, normalizedContents);
        }
    }
}
