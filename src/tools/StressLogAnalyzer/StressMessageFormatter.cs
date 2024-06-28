// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer;

internal sealed class StressMessageFormatter
{
    private record struct PaddingFormat(int Width, char FormatChar);

    private readonly Target _target;

    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _formatActions;
    private readonly Dictionary<string, Action<TargetPointer, PaddingFormat, StringBuilder>> _alternateActions;

    public StressMessageFormatter(Target target)
    {
        _target = target;

        _formatActions = new(StringComparer.OrdinalIgnoreCase)
        {
            { "pM", FormatMethodDesc },
            { "pT", FormatMethodTable },
            { "pV", FormatVTable },
            { "pK", FormatStackTrace },
            { "s", FormatAsciiString },
            { "hs", FormatAsciiString },
            // "S" is omitted because it is the only specifier that only differs in case from another specifier that we support.
            // We'll normalize it to "ls" before we look up in the table.
            { "ls", FormatUtf16String },
            { "p", FormatHexWithPrefix },
            { "d", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "i", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "x", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "lld", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "lli", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "llu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "llx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "zd", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "zi", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<long>(ptr, 'd', paddingFormat)) },
            { "zu", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "zx", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64u", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'd', paddingFormat)) },
            { "Ix", (ptr, paddingFormat, builder) => builder.Append(FormatInteger<ulong>(ptr, 'x', paddingFormat)) },
            { "I64p", FormatHexWithPrefix },
        };

        _alternateActions = new(StringComparer.OrdinalIgnoreCase)
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

    private string ReadZeroTerminatedString<T>(TargetPointer pointer, int maxLength)
        where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        StringBuilder sb = new();
        for (T ch = _target.Read<T>(pointer);
            ch != T.Zero;
            ch = _target.Read<T>(pointer = new TargetPointer((ulong)pointer + 1)))
        {
            if (sb.Length > maxLength)
            {
                break;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }

    public string GetFormattedMessage(StressMsgData stressMsg)
    {
        Debug.Assert(stressMsg.FormatString != TargetPointer.Null);
        uint pointerSize = _target.GetTypeInfo(DataType.pointer).Size!.Value;
        TargetPointer nextCharPtr = stressMsg.FormatString;
        string formatString = ReadZeroTerminatedString<byte>(stressMsg.FormatString, maxLength: 256);
        // Normalize '%S' to '%ls' to allow us to use case-insensitive compare for all of the other formats
        // we support.
        formatString = formatString.Replace("%S", "%ls", StringComparison.Ordinal);
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

                while (operand > '0' && operand <= '9')
                {
                    paddingFormat = paddingFormat with { Width = paddingFormat.Width * 10 + (operand - '0') };
                    operand = formatString[startIndex++];
                }

                string specifier;

                // Check for width specifiers to form the format specifier we'll look up in the table.
                if (operand == 'l')
                {
                    if (formatString[startIndex++] != 'l')
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
                    if (formatString[startIndex] is 'M' or 'T' or 'V' or 'K')
                    {
                        specifier = "p" + formatString[startIndex++];
                    }
                    else
                    {
                        specifier = "p";
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
