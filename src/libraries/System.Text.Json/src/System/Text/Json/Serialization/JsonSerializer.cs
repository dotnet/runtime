// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        internal static bool IsValidNumberHandlingValue(JsonNumberHandling handling)
        {
            return JsonHelpers.IsInRangeInclusive((int)handling, 0, 7);
        }
    }
}
