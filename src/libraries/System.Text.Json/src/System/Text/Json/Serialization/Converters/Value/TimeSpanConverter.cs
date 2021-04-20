// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        private readonly static JsonEncodedText Zero = JsonEncodedText.Encode("PT0S");
        private const ulong TicksPerYear = TimeSpan.TicksPerDay * 365;
        private const ulong TicksPerMonth = TimeSpan.TicksPerDay * 30;
        private const ulong TicksPerDay = (ulong)TimeSpan.TicksPerDay;
        private const ulong TicksPerHour = (ulong)TimeSpan.TicksPerHour;
        private const ulong TicksPerMinute = (ulong)TimeSpan.TicksPerMinute;
        private const ulong TicksPerSecond = (ulong)TimeSpan.TicksPerSecond;

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            static byte ReadByte(ref ReadOnlySpan<byte> span)
            {
                if (span.IsEmpty)
                {
                    ThrowFormatException();
                }

                var result = span[0];
                span = span[1..];

                return result;
            }

            static ulong ReadDigits(ref ReadOnlySpan<byte> span)
            {
                if (span.IsEmpty)
                {
                    ThrowFormatException();
                }

                ulong value = 0;
                do
                {
                    uint digit = (uint)(span[0] - '0');

                    if (digit > 9)
                    {
                        break;
                    }

                    value = value * 10 + digit;
                    span = span[1..];
                }
                while (!span.IsEmpty);
                return value;
            }

            var span = reader.ValueSpan;
            byte current = ReadByte(ref span);
            bool negative = current == '-';
            ulong value;

            if (negative)
            {
                current = ReadByte(ref span);
            }

            if (current != 'P')
            {
                ThrowFormatException();
            }

            checked
            {
                value = ReadDigits(ref span);
                current = ReadByte(ref span);

                ulong ticks = 0;
                if (current == 'Y')
                {
                    ticks += value * TicksPerYear;

                    if (span.IsEmpty)
                    {
                        return Result(ticks);
                    }

                    value = ReadDigits(ref span);
                    current = ReadByte(ref span);
                }

                if (current == 'M')
                {
                    ticks += value * TicksPerMonth;

                    if (span.IsEmpty)
                    {
                        return Result(ticks);
                    }

                    value = ReadDigits(ref span);
                    current = ReadByte(ref span);
                }

                if (current == 'D')
                {
                    ticks += value * TicksPerDay;

                    if (span.IsEmpty)
                    {
                        return Result(ticks);
                    }

                    current = ReadByte(ref span);
                }

                bool hasTime = current == 'T';
                if (hasTime)
                {
                    value = ReadDigits(ref span);
                    current = ReadByte(ref span);

                    if (current == 'H')
                    {
                        ticks += value * TicksPerHour;

                        if (span.IsEmpty)
                        {
                            return Result(ticks);
                        }

                        value = ReadDigits(ref span);
                        current = ReadByte(ref span);
                    }

                    if (current == 'M')
                    {
                        ticks += value * TicksPerMinute;

                        if (span.IsEmpty)
                        {
                            return Result(ticks);
                        }

                        value = ReadDigits(ref span);
                        current = ReadByte(ref span);
                    }

                    if (current == 'S')
                    {
                        ticks += value * TicksPerSecond;

                        if (span.IsEmpty)
                        {
                            return Result(ticks);
                        }

                        current = ReadByte(ref span);
                    }
                }

                if (current == '.' ||
                    current == ',')
                {
                    ulong valueFraction = ReadDigits(ref span);
                    (ulong ticksPerPart, ulong ticksPerFraction) = ReadByte(ref span) switch
                    {
                        (byte)'Y' => (TicksPerYear, TicksPerMonth),
                        (byte)'M' => hasTime
                            ? (TicksPerMinute, TicksPerSecond)
                            : (TicksPerMonth, TicksPerDay),
                        (byte)'D' => (TicksPerDay, TicksPerHour),
                        (byte)'H' => (TicksPerHour, TicksPerMinute),
                        (byte)'S' => (TicksPerSecond, 1U),
                        _ => (0U, 0U),
                    };

                    if (span.IsEmpty && ticksPerPart != 0)
                    {
                        ulong scale = 10;
                        while (valueFraction / scale != 0)
                        {
                            scale *= 10;
                        }

                        ulong ticksFraction = (ulong)(ticksPerPart * ((double)valueFraction / (double)scale));

                        ticks += (ulong)(ticksFraction / ticksPerFraction) * ticksPerFraction;
                        ticks += (ulong)(value * ticksPerPart);

                        return Result(ticks);
                    }
                }
            }

            ThrowFormatException();
            return default;

            TimeSpan Result(ulong ticks) => new TimeSpan(negative ? -(long)ticks : (long)ticks);
            static void ThrowFormatException() => throw ThrowHelper.GetFormatException(DataType.TimeSpan);
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            static int Write(Span<byte> span, char designator, ref ulong ticks, ulong ticksPerPart)
            {
                var value = ticks / ticksPerPart;
                int written = 0;

                if (value != 0)
                {
                    ticks -= value * ticksPerPart;
                    written = WriteDigits(span, value);
                    span[written++] = (byte)designator;

                    return written;
                }

                return written;
            }

            static int WriteDigits(Span<byte> span, ulong value)
            {
                int digits = value switch
                {
                    < 10 => 1,
                    < 100 => 2,
                    < 1000 => 3,
                    < 10000 => 4,
                    < 100000 => 5,
                    < 1000000 => 6,
                    < 10000000 => 7,
                    _ => 0,
                };

                int digit = digits;
                while (digit-- != 0)
                {
                    ulong temp = value / 10;

                    span[digit] = (byte)('0' + value - temp * 10);
                    value = temp;
                }

                return digits;
            }

            if (value == TimeSpan.Zero)
            {
                writer.WriteStringValue(Zero);
                return;
            }

            Span<byte> result = stackalloc byte[32];
            var ticks = (ulong)value.Ticks;
            var position = 0;

            if (ticks > long.MaxValue)
            {
                result[position++] = (byte)'-';
                ticks = (ulong)-value.Ticks;
            }

            result[position++] = (byte)'P';
            position += Write(result[position..], 'Y', ref ticks, TicksPerYear);
            position += Write(result[position..], 'M', ref ticks, TicksPerMonth);
            position += Write(result[position..], 'D', ref ticks, TicksPerDay);

            if (ticks != 0)
            {
                result[position++] = (byte)'T';
                position += Write(result[position..], 'H', ref ticks, TicksPerHour);
                position += Write(result[position..], 'M', ref ticks, TicksPerMinute);
                position += Write(result[position..], 'S', ref ticks, TicksPerSecond);

                if (ticks != 0)
                {
                    for (ulong temp = ticks;
                        ticks - (temp /= 10) * 10 == 0;
                        ticks = temp);

                    result[position - 1] = (byte)'.';
                    position += WriteDigits(result[position..], ticks);
                    result[position++] = (byte)'S';
                }
            }

            writer.WriteStringValue(result[0..position]);
        }
    }
}
