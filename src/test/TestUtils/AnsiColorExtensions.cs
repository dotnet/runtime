// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Cli.Build.Framework
{
    public static class AnsiColorExtensions
    {
        public static string Black(this string text)
        {
            return "\x1B[30m" + text + "\x1B[39m";
        }

        public static string Red(this string text)
        {
            return "\x1B[31m" + text + "\x1B[39m";
        }
        public static string Green(this string text)
        {
            return "\x1B[32m" + text + "\x1B[39m";
        }

        public static string Yellow(this string text)
        {
            return "\x1B[33m" + text + "\x1B[39m";
        }

        public static string Blue(this string text)
        {
            return "\x1B[34m" + text + "\x1B[39m";
        }

        public static string Magenta(this string text)
        {
            return "\x1B[35m" + text + "\x1B[39m";
        }

        public static string Cyan(this string text)
        {
            return "\x1B[36m" + text + "\x1B[39m";
        }

        public static string White(this string text)
        {
            return "\x1B[37m" + text + "\x1B[39m";
        }

        public static string Bold(this string text)
        {
            return "\x1B[1m" + text + "\x1B[22m";
        }
    }
}
