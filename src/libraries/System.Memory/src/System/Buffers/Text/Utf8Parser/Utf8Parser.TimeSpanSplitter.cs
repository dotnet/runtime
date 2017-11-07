// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Buffers.Text
{
    public static partial class Utf8Parser
    {
        private enum ComponentParseResult : byte
        {
            // Do not change or add values in this enum unless you review every use of the TimeSpanSplitter.Separators field. That field is an "array of four
            // ComponentParseResults" encoded as a 32-bit integer with each of its four bytes containing one of 0 (NoMoreData), 1 (Colon) or 2 (Period).
            // (So a value of 0x01010200 means the string parsed as "nn:nn:nn.nnnnnnn")
            NoMoreData = 0,
            Colon = 1,
            Period = 2,
            ParseFailure = 3,
        }

        private struct TimeSpanSplitter
        {
            public uint V1;
            public uint V2;
            public uint V3;
            public uint V4;
            public uint V5;

            public bool IsNegative;

            // Encodes an "array of four ComponentParseResults" as a 32-bit integer with each of its four bytes containing one of 0 (NoMoreData), 1 (Colon) or 2 (Period).
            // (So a value of 0x01010200 means the string parsed as "nn:nn:nn.nnnnnnn")
            public uint Separators;

            public bool TrySplitTimeSpan(ReadOnlySpan<byte> text, bool periodUsedToSeparateDay, out int bytesConsumed)
            {
                int srcIndex = 0;
                byte c = default;

                // Unlike many other data types, TimeSpan allow leading whitespace.
                while (srcIndex != text.Length)
                {
                    c = text[srcIndex];
                    if (!(c == ' ' || c == '\t'))
                        break;
                    srcIndex++;
                }

                if (srcIndex == text.Length)
                {
                    bytesConsumed = 0;
                    return false;
                }

                // Check for an option negative sign. ('+' is not allowed.)
                if (c == Utf8Constants.Minus)
                {
                    IsNegative = true;
                    srcIndex++;
                    if (srcIndex == text.Length)
                    {
                        bytesConsumed = 0;
                        return false;
                    }
                }

                // From here, we terminate on anything that's not a digit, ':' or '.' The '.' is only allowed after at least three components have
                // been specified. If we see it earlier, we'll assume that's an error and fail out rather than treating it as the end of data.

                //
                // Timespan has to start with a number - parse the first one.
                //
                if (!TryParseUInt32D(text.Slice(srcIndex), out V1, out int justConsumed))
                {
                    bytesConsumed = 0;
                    return false;
                }
                srcIndex += justConsumed;

                ComponentParseResult result;

                //
                // Split out the second number (if any) For the 'c' format, a period might validly appear here as it;s used both to separate the day and the fraction - however,
                // the fraction is always the fourth component at earliest, so if we do see a period at this stage, always parse the integer as a regular integer, not as
                // a fraction.
                //
                result = ParseComponent(text, neverParseAsFraction: periodUsedToSeparateDay, ref srcIndex, out V2);
                if (result == ComponentParseResult.ParseFailure)
                {
                    bytesConsumed = 0;
                    return false;
                }
                else if (result == ComponentParseResult.NoMoreData)
                {
                    bytesConsumed = srcIndex;
                    return true;
                }
                else
                {
                    Debug.Assert(result == ComponentParseResult.Colon || result == ComponentParseResult.Period);
                    Separators |= ((uint)result) << 24;
                }

                //
                // Split out the third number (if any)
                //
                result = ParseComponent(text, false, ref srcIndex, out V3);
                if (result == ComponentParseResult.ParseFailure)
                {
                    bytesConsumed = 0;
                    return false;
                }
                else if (result == ComponentParseResult.NoMoreData)
                {
                    bytesConsumed = srcIndex;
                    return true;
                }
                else
                {
                    Debug.Assert(result == ComponentParseResult.Colon || result == ComponentParseResult.Period);
                    Separators |= ((uint)result) << 16;
                }

                //
                // Split out the fourth number (if any)
                //
                result = ParseComponent(text, false, ref srcIndex, out V4);
                if (result == ComponentParseResult.ParseFailure)
                {
                    bytesConsumed = 0;
                    return false;
                }
                else if (result == ComponentParseResult.NoMoreData)
                {
                    bytesConsumed = srcIndex;
                    return true;
                }
                else
                {
                    Debug.Assert(result == ComponentParseResult.Colon || result == ComponentParseResult.Period);
                    Separators |= ((uint)result) << 8;
                }

                //
                // Split out the fifth number (if any)
                //
                result = ParseComponent(text, false, ref srcIndex, out V5);
                if (result == ComponentParseResult.ParseFailure)
                {
                    bytesConsumed = 0;
                    return false;
                }
                else if (result == ComponentParseResult.NoMoreData)
                {
                    bytesConsumed = srcIndex;
                    return true;
                }
                else
                {
                    Debug.Assert(result == ComponentParseResult.Colon || result == ComponentParseResult.Period);
                    Separators |= (uint)result;
                }

                //
                // There cannot legally be a sixth number. If the next character is a period or colon, treat this as a error as it's likely
                // to indicate the start of a sixth number. Otherwise, treat as end of parse with data left over.
                //
                if (srcIndex != text.Length && (text[srcIndex] == Utf8Constants.Period || text[srcIndex] == Utf8Constants.Colon))
                {
                    bytesConsumed = 0;
                    return false;
                }

                bytesConsumed = srcIndex;
                return true;
            }

            //
            // Look for a separator followed by an unsigned integer.
            //
            private static ComponentParseResult ParseComponent(ReadOnlySpan<byte> text, bool neverParseAsFraction, ref int srcIndex, out uint value)
            {
                if (srcIndex == text.Length)
                {
                    value = default;
                    return ComponentParseResult.NoMoreData;
                }

                byte c = text[srcIndex];
                if (c == Utf8Constants.Colon || (c == Utf8Constants.Period && neverParseAsFraction))
                {
                    srcIndex++;

                    if (!TryParseUInt32D(text.Slice(srcIndex), out value, out int bytesConsumed))
                    {
                        value = default;
                        return ComponentParseResult.ParseFailure;
                    }

                    srcIndex += bytesConsumed;
                    return c == Utf8Constants.Colon ? ComponentParseResult.Colon : ComponentParseResult.Period;
                }
                else if (c == Utf8Constants.Period)
                {
                    srcIndex++;

                    if (!TryParseTimeSpanFraction(text.Slice(srcIndex), out value, out int bytesConsumed))
                    {
                        value = default;
                        return ComponentParseResult.ParseFailure;
                    }

                    srcIndex += bytesConsumed;
                    return ComponentParseResult.Period;
                }
                else
                {
                    value = default;
                    return ComponentParseResult.NoMoreData;
                }
            }
        }
    }
}
