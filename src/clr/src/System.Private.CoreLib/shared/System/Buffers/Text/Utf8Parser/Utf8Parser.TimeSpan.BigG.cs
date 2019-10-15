// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private static bool TryParseTimeSpanBigG(ReadOnlySpan<byte> source, out TimeSpan value, out int bytesConsumed)
        {
            int srcIndex = 0;
            byte c = default;
            while (srcIndex != source.Length)
            {
                c = source[srcIndex];
                if (!(c == ' ' || c == '\t'))
                    break;
                srcIndex++;
            }

            if (srcIndex == source.Length)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            bool isNegative = false;
            if (c == Utf8Constants.Minus)
            {
                isNegative = true;
                srcIndex++;
                if (srcIndex == source.Length)
                {
                    value = default;
                    bytesConsumed = 0;
                    return false;
                }
            }

            if (!TryParseUInt32D(source.Slice(srcIndex), out uint days, out int justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }
            srcIndex += justConsumed;

            if (srcIndex == source.Length || source[srcIndex++] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseUInt32D(source.Slice(srcIndex), out uint hours, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }
            srcIndex += justConsumed;

            if (srcIndex == source.Length || source[srcIndex++] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseUInt32D(source.Slice(srcIndex), out uint minutes, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }
            srcIndex += justConsumed;

            if (srcIndex == source.Length || source[srcIndex++] != Utf8Constants.Colon)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseUInt32D(source.Slice(srcIndex), out uint seconds, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }
            srcIndex += justConsumed;

            if (srcIndex == source.Length || source[srcIndex++] != Utf8Constants.Period)
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            if (!TryParseTimeSpanFraction(source.Slice(srcIndex), out uint fraction, out justConsumed))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            srcIndex += justConsumed;

            if (!TryCreateTimeSpan(isNegative: isNegative, days: days, hours: hours, minutes: minutes, seconds: seconds, fraction: fraction, out value))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            //
            // There cannot legally be a sixth number. If the next character is a period or colon, treat this as a error as it's likely
            // to indicate the start of a sixth number. Otherwise, treat as end of parse with data left over.
            //
            if (srcIndex != source.Length && (source[srcIndex] == Utf8Constants.Period || source[srcIndex] == Utf8Constants.Colon))
            {
                value = default;
                bytesConsumed = 0;
                return false;
            }

            bytesConsumed = srcIndex;
            return true;
        }
    }
}
