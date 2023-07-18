// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    internal static partial class FormattingHelpers
    {
        public static bool TryFormat<T>(T value, Span<byte> utf8Destination, out int bytesWritten, StandardFormat format) where T : IUtf8SpanFormattable
        {
            scoped Span<char> formatText = default;
            if (!format.IsDefault)
            {
                formatText = format.Format(stackalloc char[StandardFormat.FormatStringLength]);
            }

            return value.TryFormat(utf8Destination, out bytesWritten, formatText, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns the symbol contained within the standard format. If the standard format
        /// has not been initialized, returns the provided fallback symbol.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetSymbolOrDefault(in StandardFormat format, char defaultSymbol)
        {
            // This is equivalent to the line below, but it is written in such a way
            // that the JIT is able to perform more optimizations.
            //
            // return (format.IsDefault) ? defaultSymbol : format.Symbol;

            char symbol = format.Symbol;
            if (symbol == default && format.Precision == default)
            {
                symbol = defaultSymbol;
            }
            return symbol;
        }
    }
}
