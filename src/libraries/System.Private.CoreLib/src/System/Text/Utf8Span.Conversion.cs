// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8Span"/> instance
        /// normalized using the specified Unicode normalization form.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation.
        /// </remarks>
        public Utf8String Normalize(NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            // TODO_UTF8STRING: Reduce allocations in this code path.

            return new Utf8String(this.ToString().Normalize(normalizationForm));
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to the desired Unicode normalization form, writing the
        /// UTF-8 result to the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the normalization operation.
        /// </returns>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. Note that the the required
        /// length of <paramref name="destination"/> may be longer or shorter (in terms of UTF-8 byte count)
        /// than the input <see cref="Utf8Span"/>.
        /// </remarks>
        public int Normalize(Span<byte> destination, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            // TODO_UTF8STRING: Reduce allocations in this code path.

            ReadOnlySpan<char> normalized = this.ToString().Normalize(normalizationForm).AsSpan();
            OperationStatus status = Utf8.FromUtf16(normalized, destination, out int _, out int bytesWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "Normalize shouldn't have produced malformed Unicode string.");

            if (status != OperationStatus.Done)
            {
                bytesWritten = -1; // "destination too small"
            }

            return bytesWritten;
        }

        /// <summary>
        /// Returns the entire <see cref="Utf8String"/> as an array of UTF-8 bytes.
        /// </summary>
        public byte[] ToByteArray() => Bytes.ToArray();

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to a <see langword="char[]"/>.
        /// </summary>
        public unsafe char[] ToCharArray()
        {
            if (IsEmpty)
            {
                return Array.Empty<char>();
            }

            // TODO_UTF8STRING: Since we know the underlying data is immutable, well-formed UTF-8,
            // we can perform transcoding using an optimized code path that skips all safety checks.
            // We should also consider skipping the two-pass if possible.

            fixed (byte* pbUtf8 = &DangerousGetMutableReference())
            {
                byte* pbUtf8Invalid = Utf8Utility.GetPointerToFirstInvalidByte(pbUtf8, this.Length, out int utf16CodeUnitCountAdjustment, out _);
                Debug.Assert(pbUtf8Invalid == pbUtf8 + this.Length, "Invalid UTF-8 data seen in buffer.");

                char[] asUtf16 = new char[this.Length + utf16CodeUnitCountAdjustment];
                fixed (char* pbUtf16 = asUtf16)
                {
                    OperationStatus status = Utf8Utility.TranscodeToUtf16(pbUtf8, this.Length, pbUtf16, asUtf16.Length, out byte* pbUtf8End, out char* pchUtf16End);
                    Debug.Assert(status == OperationStatus.Done, "The buffer changed out from under us unexpectedly?");
                    Debug.Assert(pbUtf8End == pbUtf8 + this.Length, "The buffer changed out from under us unexpectedly?");
                    Debug.Assert(pchUtf16End == pbUtf16 + asUtf16.Length, "The buffer changed out from under us unexpectedly?");

                    return asUtf16;
                }
            }
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> instance to its UTF-16 equivalent, writing the result into
        /// the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the transcoding operation.
        /// </returns>
        public int ToChars(Span<char> destination)
        {
            OperationStatus status = Utf8.ToUtf16(Bytes, destination, out int _, out int charsWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "Utf8Spans shouldn't contain ill-formed UTF-8 data.");

            if (status != OperationStatus.Done)
            {
                charsWritten = -1; // "destination too small"
            }

            return charsWritten;
        }

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8Span"/> instance
        /// converted to lowercase using <paramref name="culture"/>.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8Span"/>.
        /// </remarks>
        public Utf8String ToLower(CultureInfo culture)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            return new Utf8String(this.ToString().ToLower(culture));
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to lowercase using <paramref name="culture"/>, writing the
        /// UTF-8 result to the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the case conversion operation.
        /// </returns>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. Note that the the required
        /// length of <paramref name="destination"/> may be longer or shorter (in terms of UTF-8 byte count)
        /// than the input <see cref="Utf8Span"/>.
        /// </remarks>
        public int ToLower(Span<byte> destination, CultureInfo culture)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            ReadOnlySpan<char> asLower = this.ToString().ToLower(culture).AsSpan();
            OperationStatus status = Utf8.FromUtf16(asLower, destination, out int _, out int bytesWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "ToLower shouldn't have produced malformed Unicode string.");

            if (status != OperationStatus.Done)
            {
                bytesWritten = -1; // "destination too small"
            }

            return bytesWritten;
        }

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8Span"/> instance
        /// converted to lowercase using the invariant culture.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8Span"/>.
        /// </remarks>
        public Utf8String ToLowerInvariant()
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            return new Utf8String(this.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to lowercase using the invariant culture, writing the
        /// UTF-8 result to the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the case conversion operation.
        /// </returns>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the the required
        /// length of <paramref name="destination"/> may be longer or shorter (in terms of UTF-8 byte count)
        /// than the input <see cref="Utf8Span"/>.
        /// </remarks>
        public int ToLowerInvariant(Span<byte> destination)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            ReadOnlySpan<char> asLowerInvariant = this.ToString().ToLowerInvariant().AsSpan();
            OperationStatus status = Utf8.FromUtf16(asLowerInvariant, destination, out int _, out int bytesWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "ToLowerInvariant shouldn't have produced malformed Unicode string.");

            if (status != OperationStatus.Done)
            {
                bytesWritten = -1; // "destination too small"
            }

            return bytesWritten;
        }

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8Span"/> instance
        /// converted to uppercase using <paramref name="culture"/>.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8Span"/>.
        /// </remarks>
        public Utf8String ToUpper(CultureInfo culture)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            return new Utf8String(this.ToString().ToUpper(culture));
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to uppercase using <paramref name="culture"/>, writing the
        /// UTF-8 result to the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the case conversion operation.
        /// </returns>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. Note that the the required
        /// length of <paramref name="destination"/> may be longer or shorter (in terms of UTF-8 byte count)
        /// than the input <see cref="Utf8Span"/>.
        /// </remarks>
        public int ToUpper(Span<byte> destination, CultureInfo culture)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            if (culture is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.culture);
            }

            ReadOnlySpan<char> asUpper = this.ToString().ToUpper(culture).AsSpan();
            OperationStatus status = Utf8.FromUtf16(asUpper, destination, out int _, out int bytesWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "ToUpper shouldn't have produced malformed Unicode string.");

            if (status != OperationStatus.Done)
            {
                bytesWritten = -1; // "destination too small"
            }

            return bytesWritten;
        }

        /// <summary>
        /// Returns a new <see cref="Utf8String"/> instance which represents this <see cref="Utf8Span"/> instance
        /// converted to uppercase using the invariant culture.
        /// </summary>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the returned
        /// <see cref="Utf8String"/> instance may be longer or shorter (in terms of UTF-8 byte count) than the
        /// input <see cref="Utf8Span"/>.
        /// </remarks>
        public Utf8String ToUpperInvariant()
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            return new Utf8String(this.ToString().ToUpperInvariant());
        }

        /// <summary>
        /// Converts this <see cref="Utf8Span"/> to uppercase using the invariant culture, writing the
        /// UTF-8 result to the buffer <paramref name="destination"/>.
        /// </summary>
        /// <returns>
        /// The number of bytes written to <paramref name="destination"/>, or -1 if <paramref name="destination"/>
        /// is not large enough to hold the result of the case conversion operation.
        /// </returns>
        /// <remarks>
        /// The original <see cref="Utf8Span"/> is left unchanged by this operation. For more information on the
        /// invariant culture, see the <see cref="CultureInfo.InvariantCulture"/> property. Note that the the required
        /// length of <paramref name="destination"/> may be longer or shorter (in terms of UTF-8 byte count)
        /// than the input <see cref="Utf8Span"/>.
        /// </remarks>
        public int ToUpperInvariant(Span<byte> destination)
        {
            // TODO_UTF8STRING: Avoid intermediate allocations.

            ReadOnlySpan<char> asUpperInvariant = this.ToString().ToUpperInvariant().AsSpan();
            OperationStatus status = Utf8.FromUtf16(asUpperInvariant, destination, out int _, out int bytesWritten, replaceInvalidSequences: false, isFinalBlock: true);

            Debug.Assert(status == OperationStatus.Done || status == OperationStatus.DestinationTooSmall, "ToUpperInvariant shouldn't have produced malformed Unicode string.");

            if (status != OperationStatus.Done)
            {
                bytesWritten = -1; // "destination too small"
            }

            return bytesWritten;
        }
    }
}
