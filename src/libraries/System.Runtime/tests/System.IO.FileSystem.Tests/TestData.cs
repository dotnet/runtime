// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using Xunit;

internal static class TestData
{
    // see: https://learn.microsoft.com/windows/desktop/FileIO/naming-a-file
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
            TheoryData<string> data = new TheoryData<string>
            {
                "middle\0path",
                "trailing\0"
            };

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

    /// <summary>
    /// Filenames with problematic but valid characters that work on all platforms.
    /// These test scenarios from: https://www.dwheeler.com/essays/fixing-unix-linux-filenames.html
    /// </summary>
    public static TheoryData<string> ValidFileNames
    {
        get
        {
            TheoryData<string> data = new TheoryData<string>
            {
                // Leading spaces
                " leading",
                "  leading",
                "   leading",
                // Leading dots
                ".leading",
                "..leading",
                "...leading",
                // Dash-prefixed names
                "-",
                "--",
                "-filename",
                "--filename",
                // Embedded spaces and periods
                "name with spaces",
                "name  with  multiple  spaces",
                "name.with.periods",
                "name with spaces.txt"
            };

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Unix, control characters are also valid in filenames
                data.Add("file\tname");      // tab
                data.Add("file\rname");      // carriage return
                data.Add("file\vname");      // vertical tab
                data.Add("file\fname");      // form feed
                // Trailing spaces and periods are also valid on Unix (but problematic on Windows)
                data.Add("trailing ");
                data.Add("trailing  ");
                data.Add("trailing.");
                data.Add("trailing..");
                data.Add("trailing .");
                data.Add("trailing. ");
            }

            return data;
        }
    }

    /// <summary>
    /// Filenames with trailing spaces or periods. On Windows, these require \\?\ prefix for creation
    /// but can be enumerated. Direct string-based APIs will have the trailing characters stripped.
    /// </summary>
    public static TheoryData<string> WindowsTrailingProblematicFileNames
    {
        get
        {
            return new TheoryData<string>
            {
                "trailing ",
                "trailing  ",
                "trailing.",
                "trailing..",
                "trailing .",
                "trailing. "
            };
        }
    }
}
