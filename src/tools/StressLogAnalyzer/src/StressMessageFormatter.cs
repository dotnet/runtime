// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer;

public sealed class StressMessageFormatter
{
    private record struct PaddingFormat(int Width, char FormatChar);

    private readonly Target _target;

    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _formatActions;
    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _alternateActions;

    public StressMessageFormatter(Target target)
    {
        _target = target;

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
            { "p", FormatHexWithPrefix },
            { "d", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "i", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "x", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "X", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'X', paddingFormat)) },
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
            { "Ix", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64p", FormatHexWithPrefix },
        };

        _alternateActions = new()
        {
            { "X", FormatHexWithPrefix },
            { "x", FormatHexWithPrefix },
        };
    }

    private static void FormatMethodDesc(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        // TODO: Implement MethodDesc formatting
        FormatHexWithPrefix(ptr, paddingFormat, builder);
    }

    private static void FormatMethodTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        // TODO: Implement MethodTable formatting
        FormatHexWithPrefix(ptr, paddingFormat, builder);
    }

    private static void FormatVTable(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        // TODO: Implement VTable formatting
        FormatHexWithPrefix(ptr, paddingFormat, builder);
    }

    private static void FormatStackTrace(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        // TODO: Implement StackTrace formatting
        FormatHexWithPrefix(ptr, paddingFormat, builder);
    }

    private void FormatAsciiString(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(ReadZeroTerminatedString<byte>(ptr, maxLength: 256));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr:x}#)");
        }
    }

    private void FormatUtf16String(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        try
        {
            builder.Append(ReadZeroTerminatedString<char>(ptr, maxLength: 256));
        }
        catch (InvalidOperationException)
        {
            builder.Append($"(#Could not read address of string at 0x{ptr:x}#)");
        }
    }

    private static void FormatHexWithPrefix(TargetPointer ptr, PaddingFormat paddingFormat, StringBuilder builder)
    {
        if (paddingFormat.FormatChar == '0')
        {
            // We need to subtract 2 from the width to account for the "0x" prefix.
            string format = $"x{Math.Max(paddingFormat.Width - 2, 0)}";
            builder.Append($"0x{ptr.Value.ToString(format)}");
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

    private unsafe string ReadZeroTerminatedString<T>(TargetPointer pointer, int maxLength)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        StringBuilder sb = new();
        for (T ch = _target.Read<T>(pointer);
            ch != T.Zero;
            ch = _target.Read<T>(pointer = new TargetPointer((ulong)pointer + (ulong)sizeof(T))))
        {
            if (sb.Length > maxLength)
            {
                break;
            }

            // char implements INumberBase<char> explicitly, so we need to call the helper method to use CreateChecked.
            sb.Append(MakeTruncatingHelper<char>(ch));
        }
        return sb.ToString();

        static U MakeTruncatingHelper<U>(T value) where U : INumberBase<U> => U.CreateChecked(value);
    }

    public string GetFormattedMessage(StressMsgData stressMsg)
    {
        Debug.Assert(stressMsg.FormatString != TargetPointer.Null);
        TargetPointer nextCharPtr = stressMsg.FormatString;
        string formatString = ReadZeroTerminatedString<byte>(stressMsg.FormatString, maxLength: 256);
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

                while (operand >= '0' && operand <= '9')
                {
                    paddingFormat = paddingFormat with { Width = paddingFormat.Width * 10 + (operand - '0') };
                    operand = formatString[startIndex++];
                }

                string specifier;

                // Check for width specifiers to form the format specifier we'll look up in the table.
                if (operand == 'l')
                {
                    char nextChar = formatString[startIndex++];
                    if (nextChar == 's')
                    {
                        specifier = "ls";
                    }
                    else if (nextChar != 'l')
                    {
                        throw new InvalidOperationException("Unsupported format width specifier 'l'");
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
                    throw new InvalidOperationException($"Unknown format specifier '{specifier}'");
                }

                action(stressMsg.Args[currentArg++], paddingFormat, sb);
            }
        }

        return sb.ToString();
    }
}
