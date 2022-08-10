// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics;

internal static class HelperExtensions
{
    private const int s_maxLogMessageLineLength = 65536;

    public static string Truncate(this string s, int maxLen, string suffix = "")

        => string.Concat(s.Substring(0, Math.Min(s.Length, maxLen)).AsSpan(),
                            s.Length > maxLen ? suffix : "");

    public static string TruncateLogMessage(this string s)
        => s.Truncate(s_maxLogMessageLineLength, ".. truncated");

    public static void AddRange(this JArray arr, JArray addedArr)
    {
        foreach (var item in addedArr)
            arr.Add(item);
    }

    public static bool IsNullValuedObject(this JObject obj)
        => obj != null && obj["type"]?.Value<string>() == "object" && obj["subtype"]?.Value<string>() == "null";
}
