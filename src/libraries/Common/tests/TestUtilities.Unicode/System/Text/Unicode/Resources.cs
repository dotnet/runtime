// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Text.Unicode
{
    internal static class Resources
    {
        public const string CaseFolding = "CaseFolding.txt";
        public const string DerivedBidiClass = "DerivedBidiClass.txt";
        public const string DerivedName = "DerivedName.txt";
        public const string EmojiData = "emoji-data.txt";
        public const string GraphemeBreakProperty = "GraphemeBreakProperty.txt";
        public const string PropList = "PropList.txt";
        public const string UnicodeData = "UnicodeData.txt";

        public static Stream OpenResource(string resourceName)
        {
            return typeof(Resources).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new ArgumentException(message: $"Resource {resourceName} not found.", paramName: nameof(resourceName));
        }
    }
}
