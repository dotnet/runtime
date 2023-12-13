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
        internal const int DefaultMaxDepth = 1000;

        private int _maxDepth;
        private int _optionsMask;
        private int _indentSize;

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

        internal byte IndentByte { get; private set; }

        /// <summary>
        /// Defines the whitespace character to indent with when <see cref="Indented"/> is set to true, with the default (i.e. '\0') inidicating spaces.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when the indent character is set to a non-whitespace character.
        /// </exception>
        public char IndentCharacter
        {
            readonly get => (char)IndentByte;
            set
            {
                if (value is not ' ' and not '\t' and not '\0')
                {
                    throw new ArgumentException(SR.InvalidIndentCharacter);
                }

                IndentByte = (byte)value;
            }
        }

        /// <summary>
        /// Defines the number of whitespace characters to write per indentation level, with the default (i.e. 0) indicating an indent size of 2.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the indent size is set to a negative value or a value greater than 128.
        /// </exception>
        public int IndentSize
        {
            readonly get => _indentSize;
            set
            {
                if (value is < 0 or > 128)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), SR.InvalidIndentSize);
                }

                _indentSize = value;
            }
        }

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

        internal bool IndentedOrNotSkipValidation => _optionsMask != SkipValidationBit; // Equivalent to: Indented || !SkipValidation;

        private const int IndentBit = 1;
        private const int SkipValidationBit = 2;
    }
}
