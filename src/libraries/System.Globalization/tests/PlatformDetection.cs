// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

public static class PlatformDetection
{
    private static bool? s_isUbuntu1510;

    public static bool IsUbuntu1510
    {
        get
        {
            if (!s_isUbuntu1510.HasValue)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    IdVersionPair v = ParseOsReleaseFile();

                    if (v.Id == "ubuntu" && v.VersionId == "15.10")
                    {
                        s_isUbuntu1510 = true;
                        return true;
                    }
                }

                s_isUbuntu1510 = false;
            }

            return s_isUbuntu1510.Value;
        }
    }

    private static IdVersionPair ParseOsReleaseFile()
    {
        Debug.Assert(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));

        IdVersionPair ret = new IdVersionPair();
        ret.Id = "";
        ret.VersionId = "";

        if (File.Exists("/etc/os-release"))
        {
            foreach (string line in File.ReadLines("/etc/os-release"))
            {
                if (line.StartsWith("ID=", System.StringComparison.Ordinal))
                {
                    ret.Id = line.Substring("ID=".Length);
                }
                else if (line.StartsWith("VERSION_ID=", System.StringComparison.Ordinal))
                {
                    ret.VersionId = line.Substring("VERSION_ID=".Length);
                }
            }
        }

        string versionId = ret.VersionId;

        if (versionId.Length >= 2 && versionId[0] == '"' && versionId[versionId.Length - 1] =='"')
        {
            // Remove Quotes.
            ret.VersionId = versionId.Substring(1, versionId.Length - 2);
        }

        return ret;
    }

    private struct IdVersionPair
    {
        public string Id { get; set; }
        public string VersionId { get; set; }
    }        
}
