// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    internal static partial class JsonWriterHelper
    {
        public static bool TryWriteIndentation(Span<byte> buffer, int indent, out int bytesWritten)
        {
            Debug.Assert(indent % JsonConstants.SpacesPerIndent == 0);

            if (buffer.Length >= indent)
            {
                // Based on perf tests, the break-even point where vectorized Fill is faster
                // than explicitly writing the space in a loop is 8.
                if (indent < 8)
                {
                    int i = 0;
                    while (i < indent)
                    {
                        buffer[i++] = JsonConstants.Space;
                        buffer[i++] = JsonConstants.Space;
                    }
                }
                else
                {
                    buffer.Slice(0, indent).Fill(JsonConstants.Space);
                }
                bytesWritten = indent;
                return true;
            }
            else
            {
                int i = 0;
                while (i < buffer.Length - 1)
                {
                    buffer[i++] = JsonConstants.Space;
                    buffer[i++] = JsonConstants.Space;
                }
                bytesWritten = i;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateProperty(ReadOnlySpan<byte> propertyName)
        {
            if (propertyName.Length > JsonConstants.MaxTokenSize)
                ThrowHelper.ThrowArgumentException_PropertyNameTooLarge(propertyName.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateValue(ReadOnlySpan<byte> value)
        {
            if (value.Length > JsonConstants.MaxTokenSize)
                ThrowHelper.ThrowArgumentException_ValueTooLarge(value.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateDouble(double value)
        {
#if BUILDING_INBOX_LIBRARY
            if (!double.IsFinite(value))
#else
            if (double.IsNaN(value) || double.IsInfinity(value))
#endif
            {
                ThrowHelper.ThrowArgumentException_ValueNotSupported();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateSingle(float value)
        {
#if BUILDING_INBOX_LIBRARY
            if (!float.IsFinite(value))
#else
            if (float.IsNaN(value) || float.IsInfinity(value))
#endif
            {
                ThrowHelper.ThrowArgumentException_ValueNotSupported();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateProperty(ReadOnlySpan<char> propertyName)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize)
                ThrowHelper.ThrowArgumentException_PropertyNameTooLarge(propertyName.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateValue(ReadOnlySpan<char> value)
        {
            if (value.Length > JsonConstants.MaxCharacterTokenSize)
                ThrowHelper.ThrowArgumentException_ValueTooLarge(value.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidatePropertyAndValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize || value.Length > JsonConstants.MaxTokenSize)
                ThrowHelper.ThrowArgumentException(propertyName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidatePropertyAndValue(ReadOnlySpan<byte> propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName.Length > JsonConstants.MaxTokenSize || value.Length > JsonConstants.MaxCharacterTokenSize)
                ThrowHelper.ThrowArgumentException(propertyName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidatePropertyAndValue(ReadOnlySpan<byte> propertyName, ReadOnlySpan<byte> value)
        {
            if (propertyName.Length > JsonConstants.MaxTokenSize || value.Length > JsonConstants.MaxTokenSize)
                ThrowHelper.ThrowArgumentException(propertyName, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidatePropertyAndValue(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> value)
        {
            if (propertyName.Length > JsonConstants.MaxCharacterTokenSize || value.Length > JsonConstants.MaxCharacterTokenSize)
                ThrowHelper.ThrowArgumentException(propertyName, value);
        }
    }
}
