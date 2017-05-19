// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

internal static class TestData
{
    // see: http://msdn.microsoft.com/en-us/library/aa365247.aspx
    private static readonly char[] s_invalidFileNameChars = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        new char[]
        {
            '\"', '<', '>', '|', '\0', (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7,
            (char)8, (char)9, (char)10, (char)11, (char)12, (char)13, (char)14, (char)15, (char)16,
            (char)17, (char)18, (char)19, (char)20, (char)21, (char)22, (char)23, (char)24, (char)25,
            (char)26, (char)27, (char)28, (char)29, (char)30, (char)31, '*', '?'
        } :
        new char[] { '\0' };

    public static TheoryData<string> PathsWithInvalidColons
    {
        get
        {
            return new TheoryData<string>
            {
                // Windows specific. We document that these return NotSupportedException.
                @":",
                @" :",
                @"  :",
                @"C::",
                @"C::FileName",
                @"C::FileName.txt",
                @"C::FileName.txt:",
                @"C::FileName.txt::",
                @":f",
                @":filename",
                @"file:",
                @"file:file",
                @"http:",
                @"http:/",
                @"http://",
                @"http://www",
                @"http://www.microsoft.com",
                @"http://www.microsoft.com/index.html",
                @"http://server",
                @"http://server/",
                @"http://server/home",
                @"file://",
                @"file:///C|/My Documents/ALetter.html"
            };
        }
    }

    public static TheoryData<string> PathsWithInvalidCharacters
    {
        get
        {
            TheoryData<string> data = new TheoryData<string>();

            // NOTE: That I/O treats "file"/http" specially and throws ArgumentException.
            // Otherwise, it treats all other urls as alternative data streams
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // alternate data streams, drive labels, etc.
            {
                data.Add("\0");
                data.Add("middle\0path");
                data.Add("trailing\0");
                data.Add(@"\\?\");
                data.Add(@"\\?\UNC\");
                data.Add(@"\\?\UNC\LOCALHOST");
            }
            else
            {
                data.Add("\0");
                data.Add("middle\0path");
                data.Add("trailing\0");
            }

            foreach (char c in s_invalidFileNameChars)
            {
                data.Add(c.ToString());
            }

            return data;
        }
    }

    /// <summary>
    /// Normal path char and any valid directory separators
    /// </summary>
    public static TheoryData<char> TrailingCharacters
    {
        get
        {
            TheoryData<char> data = new TheoryData<char>
            {
                // A valid, non separator
                'a',
                Path.DirectorySeparatorChar
            };

            if (Path.DirectorySeparatorChar != Path.AltDirectorySeparatorChar)
                data.Add(Path.AltDirectorySeparatorChar);

            return data;
        }
    }
}
