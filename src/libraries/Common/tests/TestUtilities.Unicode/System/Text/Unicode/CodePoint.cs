// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.Unicode
{
    /// <summary>
    /// Represents a Unicode code point (U+0000..U+10FFFF).
    /// </summary>
    public sealed class CodePoint : IEquatable<CodePoint>
    {
        internal CodePoint(int value, ParsedUnicodeData parsedData)
        {
            if ((uint)value > 0x10_FFFF)
            {
                throw new ArgumentOutOfRangeException(
                    message: FormattableString.Invariant($"Value U+{(uint)value:X4} is not a valid code point."),
                    paramName: nameof(value));
            }

            Assert.NotNull(parsedData);

            Value = value;

            if (parsedData.DerivedBidiClassData.TryGetValue(value, out BidiClass bidiClass))
            {
                BidiClass = bidiClass;
            }

            if (parsedData.PropListData.TryGetValue(value, out CodePointFlags flags))
            {
                Flags = flags;
            }

            // All code points by default case convert to themselves.

            SimpleLowercaseMapping = value;
            SimpleTitlecaseMapping = value;
            SimpleUppercaseMapping = value;

            if (parsedData.UnicodeDataData.TryGetValue(value, out UnicodeDataFileEntry entry))
            {
                GeneralCategory = entry.GeneralCategory;
                DecimalDigitValue = entry.DecimalDigitValue;
                DigitValue = entry.DigitValue;
                Name = entry.Name;
                NumericValue = entry.NumericValue;
                SimpleLowercaseMapping = entry.SimpleLowercaseMapping;
                SimpleTitlecaseMapping = entry.SimpleTitlecaseMapping;
                SimpleUppercaseMapping = entry.SimpleUppercaseMapping;
            }

            // All code points by default case fold to themselves.

            SimpleCaseFoldMapping = value;

            if (parsedData.CaseFoldingData.TryGetValue(value, out int caseFoldsTo))
            {
                SimpleCaseFoldMapping = caseFoldsTo;
            }

            // Can we get a better name for this code point?

            if (parsedData.DerivedNameData.TryGetValue(value, out string preferredName))
            {
                Name = preferredName;
            }

            // Finally, get the grapheme cluster break value.

            if (parsedData.GraphemeBreakPropertyData.TryGetValue(value, out GraphemeClusterBreakProperty graphemeProperty))
            {
                GraphemeClusterBreakProperty = graphemeProperty;
            }
        }

        /// <summary>
        /// The bidi class of this code point. Note that even unassigned code points can
        /// have a non-default bidi class.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Bidi_Class.
        /// </remarks>
        public BidiClass BidiClass { get; } = BidiClass.Left_To_Right; // default is "L" (strong left-to-right)

        /// <summary>
        /// The decimal digit value (0..9) of this code point, or -1 if this code point
        /// does not have a decimal digit value.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Numeric_Value, field (6).
        /// </remarks>
        public int DecimalDigitValue { get; } = -1; // default is "not a decimal digit"

        /// <summary>
        /// The digit value (0..9) of this code point, or -1 if this code point
        /// does not have a digit value.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Numeric_Value, field (7).
        /// </remarks>
        public int DigitValue { get; } = -1; // default is "not a digit"

        /// <summary>
        /// Any flags associated with this code point, such as "is white space?"
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#PropList.txt.
        /// </remarks>
        public CodePointFlags Flags { get; } // default is "no flags"

        /// <summary>
        /// The general Unicode category of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#UnicodeData.txt.
        /// </remarks>
        public UnicodeCategory GeneralCategory { get; } = UnicodeCategory.OtherNotAssigned; // default is "Unassigned"

        /// <summary>
        /// The grapheme cluster break property of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr29/#Grapheme_Cluster_Break_Property_Values.
        /// </remarks>
        public GraphemeClusterBreakProperty GraphemeClusterBreakProperty { get; } = GraphemeClusterBreakProperty.Other; // default is "Other"

        /// <summary>
        /// The name of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/Public/UCD/latest/ucd/extracted/DerivedName.txt.
        /// </remarks>
        public string Name { get; } = "<Unassigned>";

        /// <summary>
        /// The numeric value of this code point, or -1 if this code point
        /// does not have a numeric value.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Numeric_Value, field (8).
        /// </remarks>
        public double NumericValue { get; } = -1; // default is "not a numeric value"

        /// <summary>
        /// The code point that results from performing a simple case fold mapping of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#CaseFolding.txt
        /// and https://www.unicode.org/Public/UCD/latest/ucd/CaseFolding.txt.
        /// </remarks>
        public int SimpleCaseFoldMapping { get; }

        /// <summary>
        /// The code point that results from performing a simple lowercase mapping of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Simple_Lowercase_Mapping.
        /// </remarks>
        public int SimpleLowercaseMapping { get; }

        /// <summary>
        /// The code point that results from performing a simple titlecase mapping of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Simple_Titlecase_Mapping.
        /// </remarks>
        public int SimpleTitlecaseMapping { get; }

        /// <summary>
        /// The code point that results from performing a simple uppercase mapping of this code point.
        /// </summary>
        /// <remarks>
        /// See https://www.unicode.org/reports/tr44/#Simple_Uppercase_Mapping.
        /// </remarks>
        public int SimpleUppercaseMapping { get; }

        /// <summary>
        /// The value (0000..10FFFF) of this code point.
        /// </summary>
        public int Value { get; }

        public override bool Equals(object obj) => Equals(obj as CodePoint);

        public bool Equals(CodePoint obj)
        {
            if (obj is null)
            {
                return false;
            }

            return this.Value == obj.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public override string ToString()
        {
            return FormattableString.Invariant($"U+{(uint)Value:X4} {Name}");
        }
    }
}
