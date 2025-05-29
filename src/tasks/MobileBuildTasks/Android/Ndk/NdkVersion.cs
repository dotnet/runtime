// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Android.Build.Ndk
{
    public class NdkVersion
    {
        public Version Main { get; }
        public string Tag { get; } = "";

        public NdkVersion() => Main = new Version(0, 0);

        public NdkVersion(string? version)
        {
            string? ver = version?.Trim();
            if (string.IsNullOrEmpty(ver))
            {
                throw new ArgumentException ("must be a non-empty string", nameof(version));
            }

            int tagIdx = ver!.IndexOf('-');
            if (tagIdx >= 0)
            {
                Tag = ver.Substring(tagIdx + 1);
                ver = ver.Substring(0, tagIdx - 1);
            }

            if (!Version.TryParse(ver, out Version? ndkVersion) || ndkVersion == null)
            {
                throw new InvalidOperationException ($"Failed to parse '{ver}' as a valid NDK version.");
            }

            Main = ndkVersion;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Tag))
            {
                return $"{Main}-{Tag}";
            }

            return Main.ToString();
        }
    }
}
