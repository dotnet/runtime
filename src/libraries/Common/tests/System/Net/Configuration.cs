// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        #pragma warning disable 414
        private static readonly string DefaultAzureServer = "corefx-net-http11.azurewebsites.net";
        #pragma warning restore 414

        private static string GetValue(string envName, string defaultValue=null)
        {
            string envValue = Environment.GetEnvironmentVariable(envName);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                return defaultValue;
            }

            return Environment.ExpandEnvironmentVariables(envValue);
        }

        private static int GetPortValue(string envName, int defaultValue)
        {
            string envValue = Environment.GetEnvironmentVariable(envName);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                return defaultValue;
            }
            envValue = Environment.ExpandEnvironmentVariables(envValue);

            var split = envValue.Split(':');
            if (split.Length<2)
            {
                return defaultValue;
            }
            
            return int.Parse(split[1]);
        }

        private static Uri GetUriValue(string envName, Uri defaultValue=null)
        {
            string envValue = GetValue(envName, null);

            if (envValue == null)
            {
                return defaultValue;
            }

            return new Uri(envValue);
        }
    }
}
