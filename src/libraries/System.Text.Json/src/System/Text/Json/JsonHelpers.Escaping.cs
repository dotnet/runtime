// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace System.Text.Json
{
    internal static partial class JsonHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] GetEscapedPropertyNameSection(ReadOnlySpan<byte> utf8Value, JavaScriptEncoder? encoder)
        {
            int idx = JsonWriterHelper.NeedsEscaping(utf8Value, encoder);

            if (idx != -1)
            {
                return GetEscapedPropertyNameSection(utf8Value, idx, encoder);
            }
            else
            {
                return GetPropertyNameSection(utf8Value);
            }
        }

        public static byte[] EscapeValue(
            ReadOnlySpan<byte> utf8Value,
            int firstEscapeIndexVal,
            JavaScriptEncoder? encoder)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, encoder, out int written);

            byte[] escapedString = escapedValue.Slice(0, written).ToArray();

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }

            return escapedString;
        }

        private static byte[] GetEscapedPropertyNameSection(
            ReadOnlySpan<byte> utf8Value,
            int firstEscapeIndexVal,
            JavaScriptEncoder? encoder)
        {
            Debug.Assert(int.MaxValue / JsonConstants.MaxExpansionFactorWhileEscaping >= utf8Value.Length);
            Debug.Assert(firstEscapeIndexVal >= 0 && firstEscapeIndexVal < utf8Value.Length);

            byte[]? valueArray = null;

            int length = JsonWriterHelper.GetMaxEscapedLength(utf8Value.Length, firstEscapeIndexVal);

            Span<byte> escapedValue = length <= JsonConstants.StackallocByteThreshold ?
                stackalloc byte[JsonConstants.StackallocByteThreshold] :
                (valueArray = ArrayPool<byte>.Shared.Rent(length));

            JsonWriterHelper.EscapeString(utf8Value, escapedValue, firstEscapeIndexVal, encoder, out int written);

            byte[] propertySection = GetPropertyNameSection(escapedValue.Slice(0, written));

            if (valueArray != null)
            {
                ArrayPool<byte>.Shared.Return(valueArray);
            }

            return propertySection;
        }

        private static byte[] GetPropertyNameSection(ReadOnlySpan<byte> utf8Value)
        {
            int length = utf8Value.Length;
            byte[] propertySection = new byte[length + 3];

            propertySection[0] = JsonConstants.Quote;
            utf8Value.CopyTo(propertySection.AsSpan(1, length));
            propertySection[++length] = JsonConstants.Quote;
            propertySection[++length] = JsonConstants.KeyValueSeparator;

            return propertySection;
        }
    }
}
