// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Text.Unicode
{
    internal static class Resources
    {
        private const string Version = "12.1";

        public const string CaseFolding = "CaseFolding-" + Version + ".0.txt";
        public const string DerivedBidiClass = "DerivedBidiClass-" + Version + ".0.txt";
        public const string DerivedName = "DerivedName-" + Version + ".0.txt";
        public const string EmojiData = "emoji-data-" + Version + ".txt";
        public const string GraphemeBreakProperty = "GraphemeBreakProperty-" + Version + ".0.txt";
        public const string PropList = "PropList-" + Version + ".0.txt";
        public const string UnicodeData = "UnicodeData." + Version + ".txt";

        public static Stream OpenResource(string resourceName)
        {
            return typeof(Resources).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new ArgumentException(message: $"Resource {resourceName} not found.", paramName: nameof(resourceName));
        }
    }
}
