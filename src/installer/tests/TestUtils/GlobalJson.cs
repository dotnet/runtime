// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.TestUtils
{
    public static class GlobalJson
    {
        public class Sdk
        {
            [JsonPropertyName("version")]
            public string Version { get; set; }

            [JsonPropertyName("rollForward")]
            public string RollForward { get; set; }

            [JsonPropertyName("allowPrerelease")]
            public bool? AllowPrerelease { get; set; }

            [JsonPropertyName("paths")]
            public string[] Paths { get; set; }

            [JsonPropertyName("errorMessage")]
            public string ErrorMessage { get; set; }
        }

        public const string HostSdkPath = "$host$";

        public static string CreateEmpty(string directory)
            => Write(directory, "{}");

        public static string CreateWithVersion(string directory, string version)
            => Write(directory, new Sdk { Version = version });

        public static string CreateWithVersionSettings(string directory, string version = null, string policy = null, bool? allowPrerelease = null)
            => Write(directory, new Sdk { Version = version, RollForward = policy, AllowPrerelease = allowPrerelease });

        public static string FormatSettings(Sdk sdk)
            => JsonSerializer.Serialize(new { sdk = sdk }, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault });

        public static string Write(string directory, string contents)
        {
            string file = Path.Combine(directory, "global.json");
            File.WriteAllText(file, contents);
            return file;
        }

        public static string Write(string directory, Sdk sdk)
            => Write(directory, FormatSettings(sdk));
    }
}
