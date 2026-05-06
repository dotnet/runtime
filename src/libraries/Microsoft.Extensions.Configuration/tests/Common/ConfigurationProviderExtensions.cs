// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Test
{
    public static class ConfigurationProviderExtensions
    {
        public static string Get(this IConfigurationProvider provider, string key)
        {
            string value;

            if (!provider.TryGet(key, out value))
            {
                throw new InvalidOperationException("Key not found");
            }

            return value;
        }
    }
}
