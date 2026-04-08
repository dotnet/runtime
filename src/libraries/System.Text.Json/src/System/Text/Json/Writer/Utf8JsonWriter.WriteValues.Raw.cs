// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the input as JSON content. It is expected that the input content is a single complete JSON value.
        /// </summary>
        /// <param name="json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to validate if the input is an RFC 8259-compliant JSON payload.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if the length of the input is zero or greater than 715,827,882 (<see cref="int.MaxValue"/> / 3).</exception>
        /// <exception cref="JsonException">
        /// Thrown if <paramref name="skipInputValidation"/> is <see langword="false"/>, and the input
        /// is not a valid, complete, single JSON value according to the JSON RFC (https://tools.ietf.org/html/rfc8259)
        /// or the input JSON exceeds a recursive depth of 64.
        /// </exception>
        /// <remarks>
        /// When writing untrused JSON values, do not set <paramref name="skipInputValidation"/> to <see langword="true"/> as this can result in invalid JSON
        /// being written, and/or the overall payload being written to the writer instance being invalid.
        ///
        /// When using this method, the input content will be written to the writer destination as-is, unless validation fails (when it is enabled).
        ///
        /// The <see cref="JsonWriterOptions.SkipValidation"/> value for the writer instance is honored when using this method.
        ///
        /// The <see cref="JsonWriterOptions.Indented"/> and <see cref="JsonWriterOptions.Encoder"/> values for the writer instance are not applied when using this method.
        /// </remarks>
        public void WriteRawValue([StringSyntax(StringSyntaxAttribute.Json)] string json, bool skipInputValidation = false)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            ArgumentNullException.ThrowIfNull(json);

            TranscodeAndWriteRawValue(json.AsSpan(), skipInputValidation);
        }

        /// <summary>
        /// Writes the input as JSON content. It is expected that the input content is a single complete JSON value.
        /// </summary>
        /// <param name="json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to validate if the input is an RFC 8259-compliant JSON payload.</param>
        /// <exception cref="ArgumentException">Thrown if the length of the input is zero or greater than 715,827,882 (<see cref="int.MaxValue"/> / 3).</exception>
        /// <exception cref="JsonException">
        /// Thrown if <paramref name="skipInputValidation"/> is <see langword="false"/>, and the input
        /// is not a valid, complete, single JSON value according to the JSON RFC (https://tools.ietf.org/html/rfc8259)
        /// or the input JSON exceeds a recursive depth of 64.
        /// </exception>
        /// <remarks>
        /// When writing untrused JSON values, do not set <paramref name="skipInputValidation"/> to <see langword="true"/> as this can result in invalid JSON
        /// being written, and/or the overall payload being written to the writer instance being invalid.
        ///
        /// When using this method, the input content will be written to the writer destination as-is, unless validation fails (when it is enabled).
        ///
        /// The <see cref="JsonWriterOptions.SkipValidation"/> value for the writer instance is honored when using this method.
        ///
        /// The <see cref="JsonWriterOptions.Indented"/> and <see cref="JsonWriterOptions.Encoder"/> values for the writer instance are not applied when using this method.
        /// </remarks>
        public void WriteRawValue([StringSyntax(StringSyntaxAttribute.Json)] ReadOnlySpan<char> json, bool skipInputValidation = false)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            TranscodeAndWriteRawValue(json, skipInputValidation);
        }

        /// <summary>
        /// Writes the input as JSON content. It is expected that the input content is a single complete JSON value.
        /// </summary>
        /// <param name="utf8Json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to validate if the input is an RFC 8259-compliant JSON payload.</param>
        /// <exception cref="ArgumentException">Thrown if the length of the input is zero or greater than or equal to <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="JsonException">
        /// Thrown if <paramref name="skipInputValidation"/> is <see langword="false"/>, and the input
        /// is not a valid, complete, single JSON value according to the JSON RFC (https://tools.ietf.org/html/rfc8259)
        /// or the input JSON exceeds a recursive depth of 64.
        /// </exception>
        /// <remarks>
        /// When writing untrused JSON values, do not set <paramref name="skipInputValidation"/> to <see langword="true"/> as this can result in invalid JSON
        /// being written, and/or the overall payload being written to the writer instance being invalid.
        ///
        /// When using this method, the input content will be written to the writer destination as-is, unless validation fails (when it is enabled).
        ///
        /// The <see cref="JsonWriterOptions.SkipValidation"/> value for the writer instance is honored when using this method.
        ///
        /// The <see cref="JsonWriterOptions.Indented"/> and <see cref="JsonWriterOptions.Encoder"/> values for the writer instance are not applied when using this method.
        /// </remarks>
        public void WriteRawValue(ReadOnlySpan<byte> utf8Json, bool skipInputValidation = false)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            if (utf8Json.Length == int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException_ValueTooLarge(int.MaxValue);
            }

            WriteRawValueCore(utf8Json, skipInputValidation);
        }

        /// <summary>
        /// Writes the input as JSON content. It is expected that the input content is a single complete JSON value.
        /// </summary>
        /// <param name="utf8Json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to validate if the input is an RFC 8259-compliant JSON payload.</param>
        /// <exception cref="ArgumentException">Thrown if the length of the input is zero or equal to <see cref="int.MaxValue"/>.</exception>
        /// <exception cref="JsonException">
        /// Thrown if <paramref name="skipInputValidation"/> is <see langword="false"/>, and the input
        /// is not a valid, complete, single JSON value according to the JSON RFC (https://tools.ietf.org/html/rfc8259)
        /// or the input JSON exceeds a recursive depth of 64.
        /// </exception>
        /// <remarks>
        /// When writing untrused JSON values, do not set <paramref name="skipInputValidation"/> to <see langword="true"/> as this can result in invalid JSON
        /// being written, and/or the overall payload being written to the writer instance being invalid.
        ///
        /// When using this method, the input content will be written to the writer destination as-is, unless validation fails (when it is enabled).
        ///
        /// The <see cref="JsonWriterOptions.SkipValidation"/> value for the writer instance is honored when using this method.
        ///
        /// The <see cref="JsonWriterOptions.Indented"/> and <see cref="JsonWriterOptions.Encoder"/> values for the writer instance are not applied when using this method.
        /// </remarks>
        public void WriteRawValue(ReadOnlySequence<byte> utf8Json, bool skipInputValidation = false)
        {
            if (!_options.SkipValidation)
            {
                ValidateWritingValue();
            }

            long utf8JsonLen = utf8Json.Length;

            if (utf8JsonLen == 0)
            {
                ThrowHelper.ThrowArgumentException(SR.ExpectedJsonTokens);
            }
            if (utf8JsonLen >= int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException_ValueTooLarge(utf8JsonLen);
            }

            if (skipInputValidation)
            {
                // Treat all unvalidated raw JSON value writes as string. If the payload is valid, this approach does
                // not affect structural validation since a string token is equivalent to a complete object, array,
                // or other complete JSON tokens when considering structural validation on subsequent writer calls.
                // If the payload is not valid, then we make no guarantees about the structural validation of the final payload.
                _tokenType = JsonTokenType.String;
            }
            else
            {
                // Utilize reader validation.
                Utf8JsonReader reader = new(utf8Json);
                while (reader.Read());
                _tokenType = reader.TokenType;
            }

            Debug.Assert(utf8JsonLen < int.MaxValue);
            int len = (int)utf8JsonLen;

            // TODO (https://github.com/dotnet/runtime/issues/29293):
            // investigate writing this in chunks, rather than requesting one potentially long, contiguous buffer.
            int maxRequired = len + 1; // Optionally, 1 list separator. We've guarded against integer overflow earlier in the call stack.

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            utf8Json.CopyTo(output.Slice(BytesPending));
            BytesPending += len;

            SetFlagToAddListSeparatorBeforeNextItem();
        }

        private void TranscodeAndWriteRawValue(ReadOnlySpan<char> json, bool skipInputValidation)
        {
            if (json.Length > JsonConstants.MaxUtf16RawValueLength)
            {
                ThrowHelper.ThrowArgumentException_ValueTooLarge(json.Length);
            }

            if (json.Length == 0)
            {
                ThrowHelper.ThrowArgumentException(SR.ExpectedJsonTokens);
            }

            // Reserve space up front so that no mid-operation reallocation is needed.
            // For typical inputs use worst-case sizing (3 bytes per char) to avoid an extra byte-counting scan.
            // For very large inputs compute the exact byte count to avoid over-requesting buffer space.
            int maxRequired = json.Length <= (JsonConstants.ArrayPoolMaxSizeBeforeUsingNormalAlloc / JsonConstants.MaxExpansionFactorWhileTranscoding)
                ? json.Length * JsonConstants.MaxExpansionFactorWhileTranscoding + 1
                : JsonReaderHelper.GetUtf8ByteCount(json) + 1;

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            // Use a speculative offset so that no writer state is mutated until validation succeeds.
            int offset = BytesPending;

            if (_currentDepth < 0)
            {
                output[offset++] = JsonConstants.ListSeparator;
            }

            // Transcode UTF-16 directly into the output buffer, avoiding a temporary buffer allocation and copy.
            int actualByteCount = JsonReaderHelper.GetUtf8FromText(json, output.Slice(offset));

            JsonTokenType tokenType;
            if (skipInputValidation)
            {
                tokenType = JsonTokenType.String;
            }
            else
            {
                // Validate the transcoded JSON in-place.
                // If this throws, no writer state (BytesPending, _tokenType, separator flag) has been changed.
                Utf8JsonReader reader = new(output.Slice(offset, actualByteCount));
                while (reader.Read());
                tokenType = reader.TokenType;
            }

            // Validation passed (or was skipped) — commit all state atomically.
            BytesPending = offset + actualByteCount;
            _tokenType = tokenType;
            SetFlagToAddListSeparatorBeforeNextItem();
        }

        private void WriteRawValueCore(ReadOnlySpan<byte> utf8Json, bool skipInputValidation)
        {
            int len = utf8Json.Length;

            if (len == 0)
            {
                ThrowHelper.ThrowArgumentException(SR.ExpectedJsonTokens);
            }

            // In the UTF-16-based entry point methods above, we validate that the payload length <= int.MaxValue /3.
            // The result of this division will be rounded down, so even if every input character needs to be transcoded
            // (with expansion factor of 3), the resulting payload would be less than int.MaxValue,
            // as (int.MaxValue/3) * 3 is less than int.MaxValue.
            Debug.Assert(len < int.MaxValue);

            if (skipInputValidation)
            {
                // Treat all unvalidated raw JSON value writes as string. If the payload is valid, this approach does
                // not affect structural validation since a string token is equivalent to a complete object, array,
                // or other complete JSON tokens when considering structural validation on subsequent writer calls.
                // If the payload is not valid, then we make no guarantees about the structural validation of the final payload.
                _tokenType = JsonTokenType.String;
            }
            else
            {
                // Utilize reader validation.
                Utf8JsonReader reader = new(utf8Json);
                while (reader.Read());
                _tokenType = reader.TokenType;
            }

            // TODO (https://github.com/dotnet/runtime/issues/29293):
            // investigate writing this in chunks, rather than requesting one potentially long, contiguous buffer.
            int maxRequired = len + 1; // Optionally, 1 list separator. We've guarded against integer overflow earlier in the call stack.

            if (_memory.Length - BytesPending < maxRequired)
            {
                Grow(maxRequired);
            }

            Span<byte> output = _memory.Span;

            if (_currentDepth < 0)
            {
                output[BytesPending++] = JsonConstants.ListSeparator;
            }

            utf8Json.CopyTo(output.Slice(BytesPending));
            BytesPending += len;

            SetFlagToAddListSeparatorBeforeNextItem();
        }
    }
}
