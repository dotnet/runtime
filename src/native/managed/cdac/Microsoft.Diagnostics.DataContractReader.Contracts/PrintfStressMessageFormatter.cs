﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader;

public sealed class PrintfStressMessageFormatter
{
    public interface ISpecialPointerFormatter
    {
        string FormatMethodTable(TargetPointer pointer);
        string FormatMethodDesc(TargetPointer pointer);
        string FormatVTable(TargetPointer pointer);
        string FormatStackTrace(TargetPointer pointer);
    }

    private record struct PaddingFormat(int Width, char FormatChar, int Precision = 6);

    private readonly Target _target;
    private readonly ISpecialPointerFormatter _pointerFormatter;
    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _formatActions;
    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _alternateActions;

    public PrintfStressMessageFormatter(Target target, ISpecialPointerFormatter pointerFormatter)
    {
        _target = target;
        _pointerFormatter = pointerFormatter;
        _formatActions = new()
        {
            { "pM", FormatMethodDesc },
            { "pT", FormatMethodTable },
            { "pV", FormatVTable },
            { "pK", FormatStackTrace },
            { "s", FormatAsciiString },
            { "hs", FormatAsciiString },
            { "S", FormatUtf16String },
            { "ls", FormatUtf16String },
            { "p", FormatPointer },
            { "f", FormatFloatingPoint },
            { "d", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<int>(ptr, 'd', paddingFormat)) },
            { "i", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<int>(ptr, 'd', paddingFormat)) },
            { "u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "x", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "X", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'X', paddingFormat)) },
            { "lu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "lld", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "lli", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "llu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "llx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "llX", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'X', paddingFormat)) },
            { "zd", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "zi", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "zu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "zx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "zX", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'X', paddingFormat)) },
            { "I64u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "Id", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "Ix", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64p", FormatPointer },
        };

        _alternateActions = new()
        {
            { "X", FormatHexWithPrefix },
            { "x", FormatHexWithPrefix },
        };
    }

    private void FormatPointer(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        // Default formatting for pointers is to format as "padded to full byte width with 0s in hex".
        // Allow custom printf formatting to override this if desired.
        if (paddingFormat == new PaddingFormat(0, ' '))
        {
            builder.Append(ptr.Value.ToString($"X{_target.PointerSize * 2}"));
            return;
        }
        builder.Append(FormatInteger<ulong>(ptr, 'X', paddingFormat));
    }

    private void FormatMethodDesc(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append(_pointerFormatter.FormatMethodDesc(ptr));
    }

    private void FormatMethodTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append(_pointerFormatter.FormatMethodTable(ptr));
    }

    private void FormatVTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append(_pointerFormatter.FormatVTable(ptr));
    }

    private void FormatStackTrace(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        builder.Append(_pointerFormatter.FormatStackTrace(ptr));
    }

    private void FormatAsciiString(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(_target.ReadUtf8String(ptr).PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr.Value:x}#)");
        }
    }

    private void FormatUtf16String(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(_target.ReadUtf16String(ptr).PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr.Value:x}#)");
        }
    }

    private static void FormatHexWithPrefix(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        if (paddingFormat.FormatChar == '0')
        {
            // We need to subtract 2 from the width to account for the "0x" prefix.
            string format = $"x{Math.Max(paddingFormat.Width - 2, 0)}";
            ReadOnlySpan<char> value = ptr.Value.ToString(format);
            if (value.Length > paddingFormat.Width)
            {
                value = value[^paddingFormat.Width..];
            }
            builder.Append($"0x{value}");
        }
        else
        {
            builder.Append($"0x{ptr.Value:x}".PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
        }
    }

    private static string FormatInteger<T>(TargetPointer value, char format, PaddingFormat paddingFormat)
        where T : INumberBase<T>
    {
        if (paddingFormat.FormatChar == '0')
        {
            return T.CreateTruncating(value.Value).ToString($"{format}{paddingFormat.Width}", formatProvider: CultureInfo.InvariantCulture);
        }
        else
        {
            return T.CreateTruncating(value.Value).ToString($"{format}", formatProvider: CultureInfo.InvariantCulture).PadLeft(paddingFormat.Width, paddingFormat.FormatChar);
        }
    }

    private static void FormatFloatingPoint(TargetPointer valueAsBits, PaddingFormat paddingFormat, StringBuilder builder)
    {
        double value = BitConverter.UInt64BitsToDouble(valueAsBits.Value);
        if (paddingFormat.Precision == 0)
        {
            if (paddingFormat.FormatChar == '0')
            {
                builder.Append(value.ToString($"F{paddingFormat.Width}", provider: CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(value.ToString("F", provider: CultureInfo.InvariantCulture).PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
            }
        }
        else
        {
            if (paddingFormat.FormatChar == '0')
            {
                // Create a format string of 00000.### where there are Precision #s and Width - Precision - 1 0s.
                string formatString = string.Create(paddingFormat.Width, paddingFormat, (buffer, format) =>
                {
                    buffer.Fill('0');
                    buffer[^paddingFormat.Precision..].Fill('#');
                    buffer[buffer.Length - paddingFormat.Precision - 1] = '.';
                });
                builder.Append(value.ToString(formatString, provider: CultureInfo.InvariantCulture));
            }
            else
            {
                // Create a format string of #####.### where there are Precision #s after the dot, '0' before the dot, and #s until the string is Width long at the start.
                string formatString = string.Create(paddingFormat.Width, paddingFormat, (buffer, format) =>
                {
                    buffer.Fill('#');
                    buffer[buffer.Length - paddingFormat.Precision - 1] = '.';
                    buffer[buffer.Length - paddingFormat.Precision - 2] = '0';
                });
                builder.Append(value.ToString(formatString, provider: CultureInfo.InvariantCulture).PadLeft(paddingFormat.Width, paddingFormat.FormatChar));
            }
        }
    }

    public string GetFormattedMessage(StressMsgData stressMsg)
    {
        Debug.Assert(stressMsg.FormatString != TargetPointer.Null);
        string formatString = _target.ReadUtf8String(stressMsg.FormatString);
        int currentArg = 0;
        int startIndex = 0;
        StringBuilder sb = new();
        while (startIndex < formatString.Length)
        {
            int nextFormatter = formatString.IndexOf('%', startIndex);
            if (nextFormatter == -1)
            {
                sb.Append(formatString.AsSpan()[startIndex..]);
                break;
            }

            sb.Append(formatString.AsSpan()[startIndex..nextFormatter]);

            if (nextFormatter == formatString.Length - 1)
            {
                sb.Append('%');
            }
            else
            {
                startIndex = nextFormatter + 1;
                char operand = formatString[startIndex++];
                if (operand == '%')
                {
                    sb.Append('%');
                    continue;
                }

                var formatActions = _formatActions;

                if (operand == '#')
                {
                    formatActions = _alternateActions;
                    operand = formatString[startIndex++];
                }

                PaddingFormat paddingFormat = new PaddingFormat(0, ' ');

                if (operand == '0')
                {
                    paddingFormat = paddingFormat with { FormatChar = '0' };
                    operand = formatString[startIndex++];
                }

                while (operand is >= '0' and <= '9')
                {
                    paddingFormat = paddingFormat with { Width = paddingFormat.Width * 10 + (operand - '0') };
                    operand = formatString[startIndex++];
                }

                if (operand == '.')
                {
                    paddingFormat = paddingFormat with { Precision = 0 };
                    operand = formatString[startIndex++];
                    while (operand is >= '0' and <= '9')
                    {
                        paddingFormat = paddingFormat with { Precision = paddingFormat.Precision * 10 + (operand - '0') };
                        operand = formatString[startIndex++];
                    }
                }

                string specifier;

                // Check for width specifiers to form the format specifier we'll look up in the table.
                if (operand == 'l')
                {
                    char nextChar = formatString[startIndex++];
                    if (nextChar != 'l')
                    {
                        specifier = "l" + nextChar;
                    }
                    else
                    {
                        specifier = "ll" + formatString[startIndex++];
                    }
                }
                else if (operand == 'z')
                {
                    specifier = "z" + formatString[startIndex++];
                }
                else if (operand == 'p')
                {
                    if (startIndex < formatString.Length
                        && formatString[startIndex] is 'M' or 'T' or 'V' or 'K')
                    {
                        specifier = "p" + formatString[startIndex++];
                    }
                    else
                    {
                        specifier = "p";
                    }
                }
                else if (operand == 'I')
                {
                    if (formatString.Length - startIndex >= 3
                        && formatString.AsSpan()[startIndex..(startIndex + 2)].SequenceEqual("64"))
                    {
                        specifier = "I64" + formatString[startIndex + 2];
                        startIndex += 3;
                    }
                    else
                    {
                        specifier = "I" + formatString[startIndex++];
                    }
                }
                else
                {
                    specifier = operand.ToString();
                }

                if (!formatActions.TryGetValue(specifier, out var action))
                {
                    throw new InvalidOperationException($"Unknown format specifier '{specifier}' in string '{formatString}'");
                }

                action(stressMsg.Args[currentArg++], paddingFormat, sb);
            }
        }

        return sb.ToString();
    }
}
