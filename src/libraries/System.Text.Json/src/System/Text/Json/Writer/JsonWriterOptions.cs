// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;

namespace System.Text.Json
{
    /// <summary>
    /// Provides the ability for the user to define custom behavior when writing JSON
    /// using the <see cref="Utf8JsonWriter"/>. By default, the JSON is written without
    /// any indentation or extra white space. Also, the <see cref="Utf8JsonWriter"/> will
    /// throw an exception if the user attempts to write structurally invalid JSON.
    /// </summary>
    public struct JsonWriterOptions
    {
        private static readonly string s_alternateNewLine = Environment.NewLine.Length == 2 ? JsonConstants.NewLineLineFeed : JsonConstants.NewLineCarriageReturnLineFeed;

        internal const int DefaultMaxDepth = 1000;

        private int _maxDepth;
        private int _optionsMask;

        /// <summary>
        /// The encoder to use when escaping strings, or <see langword="null" /> to use the default encoder.
        /// </summary>
        public JavaScriptEncoder? Encoder { get; set; }

        /// <summary>
        /// Defines whether the <see cref="Utf8JsonWriter"/> should pretty print the JSON which includes:
        /// indenting nested JSON tokens, adding new lines, and adding white space between property names and values.
        /// By default, the JSON is written without any extra white space.
        /// </summary>
        public bool Indented
        {
            get
            {
                return (_optionsMask & IndentBit) != 0;
            }
            set
            {
                if (value)
                    _optionsMask |= IndentBit;
                else
                    _optionsMask &= ~IndentBit;
            }
        }

        /// <summary>
        /// Defines the indentation character used by <see cref="Utf8JsonWriter"/> when <see cref="Indented"/> is enabled. Defaults to the space character.
        /// </summary>
        /// <remarks>Allowed characters are space and horizontal tab.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> contains an invalid character.</exception>
        public char IndentCharacter
        {
            readonly get => (_optionsMask & IndentCharacterBit) != 0 ? JsonConstants.TabIndentCharacter : JsonConstants.DefaultIndentCharacter;
            set
            {
                JsonWriterHelper.ValidateIndentCharacter(value);
                if (value is not JsonConstants.DefaultIndentCharacter)
                    _optionsMask |= IndentCharacterBit;
                else
                    _optionsMask &= ~IndentCharacterBit;
            }
        }

        /// <summary>
        /// Defines the indentation size used by <see cref="Utf8JsonWriter"/> when <see cref="Indented"/> is enabled. Defaults to two.
        /// </summary>
        /// <remarks>Allowed values are integers between 0 and 127, included.</remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is out of the allowed range.</exception>
        public int IndentSize
        {
            readonly get => EncodeIndentSize((_optionsMask & IndentSizeMask) >> OptionsBitCount);
            set
            {
                JsonWriterHelper.ValidateIndentSize(value);
                _optionsMask = (_optionsMask & ~IndentSizeMask) | (EncodeIndentSize(value) << OptionsBitCount);
            }
        }

        // Encoding is applied by swapping 0 with the default value to ensure default(JsonWriterOptions) instances are well-defined.
        // As this operation is symmetrical, it can also be used to decode.
        private static int EncodeIndentSize(int value) => value switch
        {
            0 => JsonConstants.DefaultIndentSize,
            JsonConstants.DefaultIndentSize => 0,
            _ => value
        };

        /// <summary>
        /// Gets or sets the maximum depth allowed when writing JSON, with the default (i.e. 0) indicating a max depth of 1000.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the max depth is set to a negative value.
        /// </exception>
        /// <remarks>
        /// Reading past this depth will throw a <exception cref="JsonException"/>.
        /// </remarks>
        public int MaxDepth
        {
            readonly get => _maxDepth;
            set
            {
                if (value < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_MaxDepthMustBePositive(nameof(value));
                }

                _maxDepth = value;
            }
        }

        /// <summary>
        /// Defines whether the <see cref="Utf8JsonWriter"/> should skip structural validation and allow
        /// the user to write invalid JSON, when set to true. If set to false, any attempts to write invalid JSON will result in
        /// a <exception cref="InvalidOperationException"/> to be thrown.
        /// </summary>
        /// <remarks>
        /// If the JSON being written is known to be correct,
        /// then skipping validation (by setting it to true) could improve performance.
        /// An example of invalid JSON where the writer will throw (when SkipValidation
        /// is set to false) is when you write a value within a JSON object
        /// without a property name.
        /// </remarks>
        public bool SkipValidation
        {
            get
            {
                return (_optionsMask & SkipValidationBit) != 0;
            }
            set
            {
                if (value)
                    _optionsMask |= SkipValidationBit;
                else
                    _optionsMask &= ~SkipValidationBit;
            }
        }

        /// <summary>
        /// Gets or sets the new line string to use when <see cref="Indented"/> is <see langword="true"/>.
        /// The default is the value of <see cref="Environment.NewLine"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the new line string is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the new line string is not <c>\n</c> or <c>\r\n</c>.
        /// </exception>
        public string NewLine
        {
            get => (_optionsMask & NewLineBit) != 0 ? s_alternateNewLine : Environment.NewLine;
            set
            {
                JsonWriterHelper.ValidateNewLine(value);
                if (value != Environment.NewLine)
                    _optionsMask |= NewLineBit;
                else
                    _optionsMask &= ~NewLineBit;
            }
        }

        internal bool IndentedOrNotSkipValidation => (_optionsMask & (IndentBit | SkipValidationBit)) != SkipValidationBit;  // Equivalent to: Indented || !SkipValidation;

        private const int OptionsBitCount = 4;
        private const int IndentBit = 1;
        private const int SkipValidationBit = 2;
        private const int NewLineBit = 4;
        private const int IndentCharacterBit = 8;
        private const int IndentSizeMask = JsonConstants.MaximumIndentSize << OptionsBitCount;
    }
}
