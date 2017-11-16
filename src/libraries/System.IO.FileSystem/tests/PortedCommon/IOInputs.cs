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
    public static bool SupportsSettingCreationTime { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); } }
    public static bool SupportsGettingCreationTime { get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) | RuntimeInformation.IsOSPlatform(OSPlatform.OSX); } }

    // Max path length (minus trailing \0). Unix values vary system to system; just using really long values here likely to be more than on the average system.
    public static readonly int MaxPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 259 : 10000;

    // Windows specific, this is the maximum length that can be passed using extended syntax. Does not include the trailing \0.
    public static readonly int MaxExtendedPath = short.MaxValue - 1;

    // Same as MaxPath on Unix
    public static readonly int MaxLongPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? MaxExtendedPath : MaxPath;

    // Windows specific, this is the maximum length that can be passed to APIs taking directory names, such as Directory.CreateDirectory & Directory.Move.
    // Does not include the trailing \0.
    // We now do the appropriate wrapping to allow creating longer directories. Like MaxPath, this is a legacy restriction.
    public static readonly int MaxDirectory = 247;
    
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

    // These are the WhiteSpace characters we used to trim for paths:
    //
    //  (char)0x9,          // Horizontal tab     '\t'
    //  (char)0xA,          // Line feed          '\n'
    //  (char)0xB,          // Vertical tab       '\v'
    //  (char)0xC,          // Form feed          '\f'
    //  (char)0xD,          // Carriage return    '\r'
    //  (char)0x20,         // Space              ' '
    //  (char)0x85,         // Next line          '\u0085'
    //  (char)0xA0          // Non breaking space '\u00A0'

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
        // Add other control chars
        yield return "\v\f\r";
    }

    public static IEnumerable<string> GetSimpleWhiteSpace()
    {
        yield return " ";
        yield return "  ";
        yield return "   ";
        yield return "    ";
        yield return "     ";
    }

    /// <summary>
    /// Whitespace characters that arent in the traditional control set (e.g. less than 0x20)
    /// and aren't space (e.g. 0x20).
    /// </summary>
    public static IEnumerable<string> GetNonControlWhiteSpace()
    {
        yield return "\u0085"; // Next Line (.NET used to trim)
        yield return "\u00A0"; // Non breaking space (.NET used to trim)
        yield return "\u2028"; // Line separator
        yield return "\u2029"; // Paragraph separator
        yield return "\u2003"; // EM space
        yield return "\u2008"; // Punctuation space
    }

    /// <summary>
    /// Whitespace characters other than space (includes some Unicode whitespace characters we
    /// did not traditionally trim.
    /// </summary>
    public static IEnumerable<string> GetNonSpaceWhiteSpace()
    {
        return GetControlWhiteSpace().Concat(GetNonControlWhiteSpace());
    }

    /// <summary>
    /// This is the Whitespace we used to trim from paths
    /// </summary>
    public static IEnumerable<string> GetWhiteSpace()
    {
        return GetControlWhiteSpace().Concat(GetSimpleWhiteSpace());
    }

    public static IEnumerable<string> GetUncPathsWithoutShareName()
    {
        foreach (char slash in new[] { Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar })
        {
            if (!PlatformDetection.IsWindows && slash == '/') // Unc paths must start with '\' on Unix
            {
                continue;
            }
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

    public static IEnumerable<string> GetPathsWithComponentLongerThanMaxComponent()
    {
        // While paths themselves can be up to and including 32,000 characters, most volumes
        // limit each component of the path to a total of 255 characters.

        string component = new string('s', MaxComponent + 1);

        yield return string.Format(@"C:\{0}", component);
        yield return string.Format(@"C:\{0}\Filename.txt", component);
        yield return string.Format(@"C:\{0}\Filename.txt\", component);
        yield return string.Format(@"\\{0}\Share", component);
        yield return string.Format(@"\\LOCALHOST\{0}", component);
        yield return string.Format(@"\\LOCALHOST\{0}\FileName.txt", component);
        yield return string.Format(@"\\LOCALHOST\Share\{0}", component);
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
        yield return GetLongPath(rootPath, MaxExtendedPath + 1, useExtendedSyntax);
        yield return GetLongPath(rootPath, MaxExtendedPath + 2, useExtendedSyntax);
    }

    private static string GetLongPath(string rootPath, int characterCount, bool extended = false)
    {
        return IOServices.GetPath(rootPath, characterCount, extended);
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
