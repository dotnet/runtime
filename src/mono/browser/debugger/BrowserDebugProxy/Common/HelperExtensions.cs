// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.WebAssembly.Diagnostics;

internal static class HelperExtensions
{
    private const int MaxLogMessageLineLength = 65536;
    private static readonly bool TruncateLogMessages = string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WASM_DONT_TRUNCATE_LOG_MESSAGES"));

    public static string Truncate(this string message, int maxLen, string suffix = "")

            => string.Concat(message.Substring(0, Math.Min(message.Length, maxLen)).AsSpan(),
                                message.Length > maxLen ? suffix : "");

    public static string TruncateLogMessage(this string message)
            => TruncateLogMessages
                ? message.Truncate(MaxLogMessageLineLength, ".. truncated")
                : message;

    public static void AddRange(this JArray arr, JArray addedArr)
    {
        foreach (var item in addedArr)
            arr.Add(item);
    }

    public static bool IsNullValuedObject(this JObject obj)
        => obj != null && obj["type"]?.Value<string>() == "object" && obj["subtype"]?.Value<string>() == "null";
}
