// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Text.Json;

namespace Microsoft.WebAssembly.AppHost;

internal static class JsonExtensions
{
    public static bool TryGetPropertyByPath(this JsonElement element, string path, out JsonElement outElement)
    {
        outElement = default;
        JsonElement cur = element;
        string[] parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (string part in parts)
        {
            if (!cur.TryGetProperty(part, out JsonElement foundElement))
                return false;

            cur = foundElement;
        }

        outElement = cur;
        return true;
    }
}
