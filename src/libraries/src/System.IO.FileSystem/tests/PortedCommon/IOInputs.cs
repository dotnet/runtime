// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

internal static class IOInputs
{
    // see: http://msdn.microsoft.com/en-us/library/aa365247.aspx
    private static readonly char[] s_invalidFileNameChars = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
        new char[] { '\"', '<', '>', '|', '\0', (Char)1, (Char)2, (Char)3, (Char)4, (Char)5, (Char)6, (Char)7, (Char)8, (Char)9, (Char)10, (Char)11, (Char)12, (Char)13, (Char)14, (Char)15, (Char)16, (Char)17, (Char)18, (Char)19, (Char)20, (Char)21, (Char)22, (Char)23, (Char)24, (Char)25, (Char)26, (Char)27, (Char)28, (Char)29, (Char)30, (Char)31, '*', '?' } :
        new char[] { '\0' };

    public static bool SupportsSettingCreationTime { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); } }
    public static bool SupportsGettingCreationTime { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) | RuntimeInformation.IsOSPlatform(OSPlatform.OSX); } }

    // Max path length (minus trailing \0). Unix values vary system to system; just using really long values here likely to be more than on the average system.
    public static readonly int MaxPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 259 : 10000;

    // Same as MaxPath on Unix
    public static readonly int MaxLongPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? MaxExtendedPath : MaxPath;

    // Windows specific, this is the maximum length that can be passed to APIs taking directory names, such as Directory.CreateDirectory & Directory.Move.
    // Does not include the trailing \0.
    // We now do the appropriate wrapping to allow creating longer directories. Like MaxPath, this is a legacy restriction.
    public static readonly int MaxDirectory = 247;

    // Windows specific, this is the maximum length that can be passed using extended syntax. Does not include the trailing \0.
    public static readonly int MaxExtendedPath = short.MaxValue - 1;


    public const int MaxComponent = 255;

    public const string ExtendedPrefix = @"\\?\";
    public const string ExtendedUncPrefix = @"\\?\UNC\";

    public static IEnumerable<string> GetValidPathComponentNames()
    {
        yield return Path.GetRandomFileName();
        yield return "!@#$%^&";
        yield return "\x65e5\x672c\x8a9e";
        yield return "A";
        yield return " A";
        yield return "  A";
        yield return "FileName";
        yield return "FileName.txt";
        yield return " FileName";
        yield return " FileName.txt";
        yield return "  FileName";
        yield return "  FileName.txt";
        yield return "This is a valid component name";
        yield return "This is a valid component name.txt";
        yield return "V1.0.0.0000";
    }

    public static IEnumerable<string> GetControlWhiteSpace()
    {
        yield return "\t";
        yield return "\t\t";
        yield return "\t\t\t";
        yield return "\n";
        yield return "\n\n";
        yield return "\n\n\n";
        yield return "\t\n";
        yield return "\t\n\t\n";
        yield return "\n\t\n";
        yield return "\n\t\n\t";
    }

    public static IEnumerable<string> GetSimpleWhiteSpace()
    {
        yield return " ";
        yield return "  ";
        yield return "   ";
        yield return "    ";
        yield return "     ";
    }

    public static IEnumerable<string> GetWhiteSpace()
    {
        return GetControlWhiteSpace().Concat(GetSimpleWhiteSpace());
    }

    public static IEnumerable<string> GetUncPathsWithoutShareName()
    {
        foreach (char slash in new[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar })
        {
            string slashes = new string(slash, 2);
            yield return slashes;
            yield return slashes + " ";
            yield return slashes + new string(slash, 5);
            yield return slashes + "S";
            yield return slashes + "S ";
            yield return slashes + "LOCALHOST";
            yield return slashes + "LOCALHOST " + slash;
            yield return slashes + "LOCALHOST " + new string(slash, 2);
            yield return slashes + "LOCALHOST" + slash + " ";
            yield return slashes + "LOCALHOST" + slash + slash + " ";
        }
    }

    public static IEnumerable<string> GetPathsWithReservedDeviceNames()
    {
        string root = Path.GetPathRoot(Directory.GetCurrentDirectory());
        foreach (string deviceName in GetReservedDeviceNames())
        {
            yield return deviceName;
            yield return Path.Combine(root, deviceName);
            yield return Path.Combine(root, "Directory", deviceName);
            yield return Path.Combine(new string(Path.DirectorySeparatorChar, 2), "LOCALHOST", deviceName);
        }
    }

    public static IEnumerable<string> GetPathsWithAlternativeDataStreams()
    {
        yield return @"AA:";
        yield return @"AAA:";
        yield return @"AA:A";
        yield return @"AAA:A";
        yield return @"AA:AA";
        yield return @"AAA:AA";
        yield return @"AA:AAA";
        yield return @"AAA:AAA";
        yield return @"AA:FileName";
        yield return @"AAA:FileName";
        yield return @"AA:FileName.txt";
        yield return @"AAA:FileName.txt";
        yield return @"A:FileName.txt:";
        yield return @"AA:FileName.txt:AA";
        yield return @"AAA:FileName.txt:AAA";
        yield return @"C:\:";
        yield return @"C:\:FileName";
        yield return @"C:\:FileName.txt";
        yield return @"C:\fileName:";
        yield return @"C:\fileName:FileName.txt";
        yield return @"C:\fileName:FileName.txt:";
        yield return @"C:\fileName:FileName.txt:AA";
        yield return @"C:\fileName:FileName.txt:AAA";
        yield return @"ftp://fileName:FileName.txt:AAA";
    }

    public static IEnumerable<string> GetPathsWithInvalidColons()
    {
        // Windows specific. We document that these return NotSupportedException.
        yield return @":";
        yield return @" :";
        yield return @"  :";
        yield return @"C::";
        yield return @"C::FileName";
        yield return @"C::FileName.txt";
        yield return @"C::FileName.txt:";
        yield return @"C::FileName.txt::";
        yield return @":f";
        yield return @":filename";
        yield return @"file:";
        yield return @"file:file";
        yield return @"http:";
        yield return @"http:/";
        yield return @"http://";
        yield return @"http://www";
        yield return @"http://www.microsoft.com";
        yield return @"http://www.microsoft.com/index.html";
        yield return @"http://server";
        yield return @"http://server/";
        yield return @"http://server/home";
        yield return @"file://";
        yield return @"file:///C|/My Documents/ALetter.html";
    }

    public static IEnumerable<string> GetPathsWithInvalidCharacters()
    {
        // NOTE: That I/O treats "file"/http" specially and throws ArgumentException.
        // Otherwise, it treats all other urls as alternative data streams
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) // alternate data streams, drive labels, etc.
        {
            yield return "\0";
            yield return "middle\0path";
            yield return "trailing\0";
            yield return @"\\?\";
            yield return @"\\?\UNC\";
            yield return @"\\?\UNC\LOCALHOST";
        }
        else
        {
            yield return "\0";
            yield return "middle\0path";
            yield return "trailing\0";
        }

        foreach (char c in s_invalidFileNameChars)
        {
            yield return c.ToString();
        }
    }

    public static IEnumerable<string> GetPathsWithComponentLongerThanMaxComponent()
    {
        // While paths themselves can be up to and including 32,000 characters, most volumes
        // limit each component of the path to a total of 255 characters.

        string component = new string('s', MaxComponent + 1);

        yield return String.Format(@"C:\{0}", component);
        yield return String.Format(@"C:\{0}\Filename.txt", component);
        yield return String.Format(@"C:\{0}\Filename.txt\", component);
        yield return String.Format(@"\\{0}\Share", component);
        yield return String.Format(@"\\LOCALHOST\{0}", component);
        yield return String.Format(@"\\LOCALHOST\{0}\FileName.txt", component);
        yield return String.Format(@"\\LOCALHOST\Share\{0}", component);
    }

    public static IEnumerable<string> GetPathsLongerThanMaxDirectory(string rootPath)
    {
        yield return GetLongPath(rootPath, MaxDirectory + 1);
        yield return GetLongPath(rootPath, MaxDirectory + 2);
        yield return GetLongPath(rootPath, MaxDirectory + 3);
    }

    public static IEnumerable<string> GetPathsLongerThanMaxPath(string rootPath, bool useExtendedSyntax = false)
    {
        yield return GetLongPath(rootPath, MaxPath + 1, useExtendedSyntax);
        yield return GetLongPath(rootPath, MaxPath + 2, useExtendedSyntax);
        yield return GetLongPath(rootPath, MaxPath + 3, useExtendedSyntax);
    }

    public static IEnumerable<string> GetPathsLongerThanMaxLongPath(string rootPath, bool useExtendedSyntax = false)
    {
        yield return GetLongPath(rootPath, MaxExtendedPath + 1 - (useExtendedSyntax ? 0 : ExtendedPrefix.Length), useExtendedSyntax);
        yield return GetLongPath(rootPath, MaxExtendedPath + 2 - (useExtendedSyntax ? 0 : ExtendedPrefix.Length), useExtendedSyntax);
    }

    private static string GetLongPath(string rootPath, int characterCount, bool extended = false)
    {
        return IOServices.GetPath(rootPath, characterCount, extended).FullPath;
    }

    public static IEnumerable<string> GetReservedDeviceNames()
    {   // See: http://msdn.microsoft.com/en-us/library/aa365247.aspx
        yield return "CON";
        yield return "AUX";
        yield return "NUL";
        yield return "PRN";
        yield return "COM1";
        yield return "COM2";
        yield return "COM3";
        yield return "COM4";
        yield return "COM5";
        yield return "COM6";
        yield return "COM7";
        yield return "COM8";
        yield return "COM9";
        yield return "LPT1";
        yield return "LPT2";
        yield return "LPT3";
        yield return "LPT4";
        yield return "LPT5";
        yield return "LPT6";
        yield return "LPT7";
        yield return "LPT8";
        yield return "LPT9";
    }
}
