// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Writes the input as JSON content.
        /// </summary>
        /// <param name="json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to skip validation of the input JSON content.</param>
        public void WriteRawValue(string json, bool skipInputValidation = false)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            WriteRawValue(json.AsSpan(), skipInputValidation);
        }

        /// <summary>
        /// Writes the input as JSON content.
        /// </summary>
        /// <param name="json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to skip validation of the input JSON content.</param>
        public void WriteRawValue(ReadOnlySpan<char> json, bool skipInputValidation = false)
        {
            byte[]? tempArray = null;

            // For performance, avoid obtaining actual byte count unless memory usage is higher than the threshold.
            Span<byte> utf8Json = json.Length <= (JsonConstants.ArrayPoolMaxSizeBeforeUsingNormalAlloc / JsonConstants.MaxExpansionFactorWhileTranscoding) ?
                // Use a pooled alloc.
                tempArray = ArrayPool<byte>.Shared.Rent(json.Length * JsonConstants.MaxExpansionFactorWhileTranscoding) :
                // Use a normal alloc since the pool would create a normal alloc anyway based on the threshold (per current implementation)
                // and by using a normal alloc we can avoid the Clear().
                new byte[JsonReaderHelper.GetUtf8ByteCount(json)];

            try
            {
                int actualByteCount = JsonReaderHelper.GetUtf8FromText(json, utf8Json);
                utf8Json = utf8Json.Slice(0, actualByteCount);
                WriteRawValue(utf8Json, skipInputValidation);
            }
            finally
            {
                if (tempArray != null)
                {
                    utf8Json.Clear();
                    ArrayPool<byte>.Shared.Return(tempArray);
                }
            }
        }

        /// <summary>
        /// Writes the input as JSON content.
        /// </summary>
        /// <param name="utf8Json">The raw JSON content to write.</param>
        /// <param name="skipInputValidation">Whether to skip validation of the input JSON content.</param>
        public void WriteRawValue(ReadOnlySpan<byte> utf8Json, bool skipInputValidation = false)
        {
            if (utf8Json.Length == 0)
            {
                ThrowHelper.ThrowArgumentException(SR.ExpectedJsonTokens);
            }

            if (!skipInputValidation)
            {
                Utf8JsonReader reader = new Utf8JsonReader(utf8Json);

                try
                {
                    while (reader.Read());
                }
                catch (JsonReaderException ex)
                {
                    ThrowHelper.ThrowArgumentException(ex.Message);
                }
            }

            int maxRequired = utf8Json.Length + 1; // Optionally, 1 list separator

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
            BytesPending += utf8Json.Length;

            SetFlagToAddListSeparatorBeforeNextItem();

            // Treat all raw JSON value writes as string.
            _tokenType = JsonTokenType.String;
        }
    }
}
