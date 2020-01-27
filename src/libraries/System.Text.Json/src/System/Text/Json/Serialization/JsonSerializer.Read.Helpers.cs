// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<byte> GetSpan(ref Utf8JsonReader reader)
        {
            ReadOnlySpan<byte> propertyName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
            return propertyName;
        }

        private static object? ReadCore(
            Type returnType,
            JsonSerializerOptions options,
            ref Utf8JsonReader reader)
        {
            ReadStack state = default;
            state.InitializeRoot(returnType, options);

            ReadCore(options, ref reader, ref state);

            return state.Current.ReturnValue;
        }
    }
}
