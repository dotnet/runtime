// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Serialization
{
    public static partial class JsonSerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool WriteValue(
            JsonSerializerOptions options,
            ref Utf8JsonWriter writer,
            ref WriteStackFrame current)
        {
            Debug.Assert(current.JsonPropertyInfo.ClassType == ClassType.Value);

            current.JsonPropertyInfo.Write(options, ref current, ref writer);
            return true;
        }
    }
}
