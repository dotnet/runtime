// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Resources;

namespace HelixCache;

internal static class SR
{
    private static readonly ResourceManager s_resources = new("HelixCache.Strings", typeof(SR).Assembly);

    internal static string Get(string name, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture,
            s_resources.GetString(name) ?? throw new MissingManifestResourceException(name), arguments);
}
