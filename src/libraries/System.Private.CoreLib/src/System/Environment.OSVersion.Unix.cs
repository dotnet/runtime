// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion() => GetOperatingSystem(Interop.Sys.GetUnixRelease());

        // Tests exercise this method for corner cases via private reflection
        private static OperatingSystem GetOperatingSystem(string release)
        {
            int major = 0, minor = 0, build = 0, revision = 0;

            // Parse the uname's utsname.release for the first four numbers found.
            // This isn't perfect, but Version already doesn't map exactly to all possible release
            // formats, e.g. 2.6.19-1.2895.fc6
            if (release != null)
            {
                int i = 0;
                major = FindAndParseNextNumber(release, ref i);
                minor = FindAndParseNextNumber(release, ref i);
                build = FindAndParseNextNumber(release, ref i);
                revision = FindAndParseNextNumber(release, ref i);
            }

            return new OperatingSystem(PlatformID.Unix, new Version(major, minor, build, revision));
        }

        private static int FindAndParseNextNumber(string text, ref int pos)
        {
            // Move to the beginning of the number
            for (; (uint)pos < (uint)text.Length; pos++)
            {
                if (char.IsAsciiDigit(text[pos]))
                {
                    break;
                }
            }

            // Parse the number;
            int num = 0;
            for (; (uint)pos < (uint)text.Length; pos++)
            {
                char c = text[pos];
                if (!char.IsAsciiDigit(c))
                    break;

                try
                {
                    num = checked((num * 10) + (c - '0'));
                }
                // Integer overflow can occur for example with:
                //     Linux nelknet 4.15.0-24201807041620-generic
                // To form a valid Version, num must be positive.
                catch (OverflowException)
                {
                    return int.MaxValue;
                }
            }

            return num;
        }
    }
}
