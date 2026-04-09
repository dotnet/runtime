// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Collections.Generic;

namespace System.Buffers.Text.Tests
{
    // Test metadata that describes a standard format (e.g. 'G', 'D' and 'X') supported by a particular data type.
    public sealed class SupportedFormat
    {
        public SupportedFormat(char symbol, bool supportsPrecision)
        {
            Symbol = symbol;
            SupportsPrecision = supportsPrecision;
        }

        public char Symbol { get; }
        public bool SupportsPrecision { get; }
        public bool IsDefault { get; set; } = false;
        public bool NoRepresentation { get; set; } = false; // If true, can only be accessed by passing default(StandardFormat). (The weird DateTimeOffset case.)
        public char FormatSynonymFor { get; set; } = default;
        public char ParseSynonymFor { get; set; } = default;
    }

    internal static partial class TestData
    {
        public static bool IsParsingImplemented<T>(this SupportedFormat f) => f.IsParsingImplemented(typeof(T));

        //
        // Used to disable automatic generation of ParserTestData from FormatterTestData. Useful for bringing up new
        // formats as you can use this shutoff valve to bring up formatting without having to bring up parsing at the same time.
        //
        public static bool IsParsingImplemented(this SupportedFormat f, Type t)
        {
            return true;
        }

        public static IEnumerable<SupportedFormat> IntegerFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('g', supportsPrecision: false) { FormatSynonymFor = 'G', ParseSynonymFor = 'G' };
                yield return new SupportedFormat('R', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('r', supportsPrecision: false) { FormatSynonymFor = 'R', ParseSynonymFor = 'R' };
                yield return new SupportedFormat('D', supportsPrecision: true);
                yield return new SupportedFormat('d', supportsPrecision: true) { FormatSynonymFor = 'D', ParseSynonymFor = 'd' };
                yield return new SupportedFormat('N', supportsPrecision: true);
                yield return new SupportedFormat('n', supportsPrecision: true) { FormatSynonymFor = 'N', ParseSynonymFor = 'N' };
                yield return new SupportedFormat('X', supportsPrecision: true);
                yield return new SupportedFormat('x', supportsPrecision: true) { ParseSynonymFor = 'X' };
            }
        }

        public static IEnumerable<SupportedFormat> DecimalFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('g', supportsPrecision: false) { FormatSynonymFor = 'G', ParseSynonymFor = 'G' };
                yield return new SupportedFormat('E', supportsPrecision: true);
                yield return new SupportedFormat('e', supportsPrecision: true) { ParseSynonymFor = 'E' };
                yield return new SupportedFormat('F', supportsPrecision: true);
                yield return new SupportedFormat('f', supportsPrecision: true) { FormatSynonymFor = 'F', ParseSynonymFor = 'F' };
            }
        }

        public static IEnumerable<SupportedFormat> FloatingPointFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('g', supportsPrecision: false) { FormatSynonymFor = 'G', ParseSynonymFor = 'G' };
                yield return new SupportedFormat('E', supportsPrecision: true);
                yield return new SupportedFormat('e', supportsPrecision: true) { ParseSynonymFor = 'E' };
                yield return new SupportedFormat('F', supportsPrecision: true);
                yield return new SupportedFormat('f', supportsPrecision: true) { FormatSynonymFor = 'F', ParseSynonymFor = 'F' };
            }
        }

        public static IEnumerable<SupportedFormat> BooleanFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('l', supportsPrecision: false) { ParseSynonymFor = 'l' };
            }
        }

        public static IEnumerable<SupportedFormat> GuidFormats
        {
            get
            {
                yield return new SupportedFormat('D', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('N', supportsPrecision: false);
                yield return new SupportedFormat('P', supportsPrecision: false);
                yield return new SupportedFormat('B', supportsPrecision: false);
            }
        }

        public static IEnumerable<SupportedFormat> DateTimeFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('R', supportsPrecision: false);
                yield return new SupportedFormat('l', supportsPrecision: false);
                yield return new SupportedFormat('O', supportsPrecision: false);
            }
        }

        public static IEnumerable<SupportedFormat> DateTimeOffsetFormats
        {
            get
            {
                // The "default" format for DateTimeOffset is weird - it's like "G" but also suffixes an offset so it doesn't exactly match any of the explicit offsets.
                yield return new SupportedFormat(default, supportsPrecision: false) { IsDefault = true, NoRepresentation = true };
                yield return new SupportedFormat('G', supportsPrecision: false);
                yield return new SupportedFormat('R', supportsPrecision: false);
                yield return new SupportedFormat('l', supportsPrecision: false);
                yield return new SupportedFormat('O', supportsPrecision: false);
            }
        }

        public static IEnumerable<SupportedFormat> TimeSpanFormats
        {
            get
            {
                yield return new SupportedFormat('G', supportsPrecision: false);
                yield return new SupportedFormat('g', supportsPrecision: false);
                yield return new SupportedFormat('c', supportsPrecision: false) { IsDefault = true };
                yield return new SupportedFormat('t', supportsPrecision: false) { ParseSynonymFor = 'c', FormatSynonymFor = 'c' };
                yield return new SupportedFormat('T', supportsPrecision: false) { ParseSynonymFor = 'c', FormatSynonymFor = 'c' };
            }
        }
    }
}
