// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
