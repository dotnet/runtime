using System;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.Cli.Build
{
    public class EnvVars
    {
        public static readonly bool Verbose = GetBool("DOTNET_BUILD_VERBOSE");

        public static readonly bool PublishRidAgnosticPackages = GetBool("PUBLISH_RID_AGNOSTIC_PACKAGES");

        private static bool GetBool(string name, bool defaultValue = false)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            switch (str.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "no":
                    return false;
                default:
                    return defaultValue;
            }
        }

        public static string EnsureVariable(string variableName)
        {
            string value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrEmpty(value))
            {
                throw new BuildFailureException($"'{variableName}' environment variable was not found.");
            }

            return value;
        }
    }
}
