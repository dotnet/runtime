// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;

#if NETCOREAPP
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics.Arm;
#endif

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// An abstraction representing various text encoders.
    /// </summary>
    /// <remarks>
    /// TextEncoder subclasses can be used to do HTML encoding, URI encoding, and JavaScript encoding.
    /// Instances of such subclasses can be accessed using <see cref="HtmlEncoder.Default"/>, <see cref="UrlEncoder.Default"/>, and <see cref="JavaScriptEncoder.Default"/>.
    /// </remarks>
    public abstract class TextEncoder
    {
        // Fast cache for Ascii
        private readonly byte[][] _asciiEscape = new byte[0x80][];

        private volatile bool _isAsciiCacheInitialized;
        private AsciiNeedsEscapingData _asciiNeedsEscaping;

#if NETCOREAPP
        private Vector128<sbyte> _bitMaskLookupAsciiNeedsEscaping;
#endif

        // Keep a reference to Array.Empty<byte> as this is used as a singleton for comparisons
        // and there is no guarantee that Array.Empty<byte>() will always be the same instance.
        private static readonly byte[] s_noEscape = Array.Empty<byte>();

        // The following pragma disables a warning complaining about non-CLS compliant members being abstract,
        // and wants me to mark the type as non-CLS compliant.
        // It is true that this type cannot be extended by all CLS compliant languages.
        // Having said that, if I marked the type as non-CLS all methods that take it as parameter will now have to be marked CLSCompliant(false),
        // yet consumption of concrete encoders is totally CLS compliant,
        // as it?s mainly to be done by calling helper methods in TextEncoderExtensions class,
        // and so I think the warning is a bit too aggressive.

        /// <summary>
        /// Encodes a Unicode scalar into a buffer.
        /// </summary>
        /// <param name="unicodeScalar">Unicode scalar.</param>
        /// <param name="buffer">The destination of the encoded text.</param>
        /// <param name="bufferLength">Length of the destination <paramref name="buffer"/> in chars.</param>
        /// <param name="numberOfCharactersWritten">Number of characters written to the <paramref name="buffer"/>.</param>
        /// <returns>Returns false if <paramref name="bufferLength"/> is too small to fit the encoded text, otherwise returns true.</returns>
        /// <remarks>This method is seldom called directly. One of the TextEncoder.Encode overloads should be used instead.
        /// Implementations of <see cref="TextEncoder"/> need to be thread safe and stateless.
        /// </remarks>
#pragma warning disable 3011
        [CLSCompliant(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe abstract bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten);

        // all subclasses have the same implementation of this method.
        // but this cannot be made virtual, because it will cause a virtual call to Encodes, and it destroys perf, i.e. makes common scenario 2x slower

        /// <summary>
        /// Finds index of the first character that needs to be encoded.
        /// </summary>
        /// <param name="text">The text buffer to search.</param>
        /// <param name="textLength">The number of characters in the <paramref name="text"/>.</param>
        /// <returns></returns>
        /// <remarks>This method is seldom called directly. It's used by higher level helper APIs.</remarks>
        [CLSCompliant(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public unsafe abstract int FindFirstCharacterToEncode(char* text, int textLength);
#pragma warning restore

        /// <summary>
        /// Determines if a given Unicode scalar will be encoded.
        /// </summary>
        /// <param name="unicodeScalar">Unicode scalar.</param>
        /// <returns>Returns true if the <paramref name="unicodeScalar"/> will be encoded by this encoder, otherwise returns false.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract bool WillEncode(int unicodeScalar);

        // this could be a field, but I am trying to make the abstraction pure.

        /// <summary>
        /// Maximum number of characters that this encoder can generate for each input character.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract int MaxOutputCharactersPerInputCharacter { get; }

        /// <summary>
        /// Encodes the supplied string and returns the encoded text as a new string.
        /// </summary>
        /// <param name="value">String to encode.</param>
        /// <returns>Encoded string.</returns>
        public virtual string Encode(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    int firstCharacterToEncode = FindFirstCharacterToEncode(valuePointer, value.Length);

                    if (firstCharacterToEncode == -1)
                    {
                        return value;
                    }

                    int bufferSize = MaxOutputCharactersPerInputCharacter * value.Length;

                    string result;
                    if (bufferSize < 1024)
                    {
                        char* wholebuffer = stackalloc char[bufferSize];
                        OperationStatus status = EncodeIntoBuffer(wholebuffer, bufferSize, valuePointer, value.Length, out int _, out int totalWritten, firstCharacterToEncode);
                        if (status != OperationStatus.Done)
                        {
                            ThrowArgumentException_MaxOutputCharsPerInputChar();
                        }

                        result = new string(wholebuffer, 0, totalWritten);
                    }
                    else
                    {
                        char[] wholebuffer = new char[bufferSize];
                        fixed (char* buffer = &wholebuffer[0])
                        {
                            OperationStatus status = EncodeIntoBuffer(buffer, bufferSize, valuePointer, value.Length, out int _, out int totalWritten, firstCharacterToEncode);
                            if (status != OperationStatus.Done)
                            {
                                ThrowArgumentException_MaxOutputCharsPerInputChar();
                            }

                            result = new string(wholebuffer, 0, totalWritten);
                        }
                    }

                    return result;
                }
            }
        }

        private unsafe OperationStatus EncodeIntoBuffer(
            char* buffer,
            int bufferLength,
            char* value,
            int valueLength,
            out int charsConsumed,
            out int charsWritten,
            int firstCharacterToEncode,
            bool isFinalBlock = true)
        {
            Debug.Assert(value != null);
            Debug.Assert(firstCharacterToEncode >= 0);

            char* originalBuffer = buffer;
            charsWritten = 0;

            if (firstCharacterToEncode > 0)
            {
                Debug.Assert(firstCharacterToEncode <= valueLength);
                Buffer.MemoryCopy(source: value,
                    destination: buffer,
                    destinationSizeInBytes: sizeof(char) * bufferLength,
                    sourceBytesToCopy: sizeof(char) * firstCharacterToEncode);

                charsWritten += firstCharacterToEncode;
                bufferLength -= firstCharacterToEncode;
                buffer += firstCharacterToEncode;
            }

            int valueIndex = firstCharacterToEncode;

            char firstChar = value[valueIndex];
            char secondChar = firstChar;
            bool wasSurrogatePair = false;

            // this loop processes character pairs (in case they are surrogates).
            // there is an if block below to process single last character.
            int secondCharIndex;
            for (secondCharIndex = valueIndex + 1; secondCharIndex < valueLength; secondCharIndex++)
            {
                if (!wasSurrogatePair)
                {
                    firstChar = secondChar;
                }
                else
                {
                    firstChar = value[secondCharIndex - 1];
                }

                secondChar = value[secondCharIndex];

                if (!WillEncode(firstChar))
                {
                    wasSurrogatePair = false;
                    *buffer = firstChar;
                    buffer++;
                    bufferLength--;
                    charsWritten++;
                }
                else
                {
                    int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, secondChar, out wasSurrogatePair, out bool _);
                    if (!TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out int charsWrittenThisTime))
                    {
                        charsConsumed = (int)(originalBuffer - buffer);
                        return OperationStatus.DestinationTooSmall;
                    }

                    if (wasSurrogatePair)
                    {
                        secondCharIndex++;
                    }

                    buffer += charsWrittenThisTime;
                    bufferLength -= charsWrittenThisTime;
                    charsWritten += charsWrittenThisTime;
                }
            }

            if (secondCharIndex == valueLength)
            {
                firstChar = value[valueLength - 1];
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, null, out wasSurrogatePair, out bool needMoreData);
                if (!isFinalBlock && needMoreData)
                {
                    Debug.Assert(wasSurrogatePair == false);
                    charsConsumed = (int)(buffer - originalBuffer);
                    return OperationStatus.NeedMoreData;
                }

                if (!TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out int charsWrittenThisTime))
                {
                    charsConsumed = (int)(buffer - originalBuffer);
                    return OperationStatus.DestinationTooSmall;
                }

                buffer += charsWrittenThisTime;
                bufferLength -= charsWrittenThisTime;
                charsWritten += charsWrittenThisTime;
            }

            charsConsumed = valueLength;
            return OperationStatus.Done;
        }

        /// <summary>
        /// Encodes the supplied string into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to this output.</param>
        /// <param name="value">String to be encoded.</param>
        public void Encode(TextWriter output, string value)
        {
            Encode(output, value, 0, value.Length);
        }

        /// <summary>
        ///  Encodes a substring into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to this output.</param>
        /// <param name="value">String whose substring is to be encoded.</param>
        /// <param name="startIndex">The index where the substring starts.</param>
        /// <param name="characterCount">Number of characters in the substring.</param>
        public virtual void Encode(TextWriter output, string value, int startIndex, int characterCount)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = FindFirstCharacterToEncode(substring, characterCount);

                    if (firstIndexToEncode == -1) // nothing to encode;
                    {
                        if (startIndex == 0 && characterCount == value.Length) // write whole string
                        {
                            output.Write(value);
                            return;
                        }
                        for (int i = 0; i < characterCount; i++) // write substring
                        {
                            output.Write(*substring);
                            substring++;
                        }
                        return;
                    }

                    // write prefix, then encode
                    for (int i = 0; i < firstIndexToEncode; i++)
                    {
                        output.Write(*substring);
                        substring++;
                    }

                    EncodeCore(output, substring, characterCount - firstIndexToEncode);
                }
            }
        }

        /// <summary>
        ///  Encodes characters from an array into a <see cref="TextWriter"/>.
        /// </summary>
        /// <param name="output">Encoded text is written to the output.</param>
        /// <param name="value">Array of characters to be encoded.</param>
        /// <param name="startIndex">The index where the substring starts.</param>
        /// <param name="characterCount">Number of characters in the substring.</param>
        public virtual void Encode(TextWriter output, char[] value, int startIndex, int characterCount)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }
            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = FindFirstCharacterToEncode(substring, characterCount);

                    if (firstIndexToEncode == -1) // nothing to encode;
                    {
                        if (startIndex == 0 && characterCount == value.Length) // write whole string
                        {
                            output.Write(value);
                            return;
                        }
                        for (int i = 0; i < characterCount; i++) // write substring
                        {
                            output.Write(*substring);
                            substring++;
                        }
                        return;
                    }

                    // write prefix, then encode
                    for (int i = 0; i < firstIndexToEncode; i++)
                    {
                        output.Write(*substring);
                        substring++;
                    }

                    EncodeCore(output, substring, characterCount - firstIndexToEncode);
                }
            }
        }

        /// <summary>
        /// Encodes the supplied UTF-8 text.
        /// </summary>
        /// <param name="utf8Source">A source buffer containing the UTF-8 text to encode.</param>
        /// <param name="utf8Destination">The destination buffer to which the encoded form of <paramref name="utf8Source"/>
        /// will be written.</param>
        /// <param name="bytesConsumed">The number of bytes consumed from the <paramref name="utf8Source"/> buffer.</param>
        /// <param name="bytesWritten">The number of bytes written to the <paramref name="utf8Destination"/> buffer.</param>
        /// <param name="isFinalBlock"><see langword="true"/> if there is further source data that needs to be encoded;
        /// <see langword="false"/> if there is no further source data that needs to be encoded.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the encoding operation.</returns>
        /// <remarks>The buffers <paramref name="utf8Source"/> and <paramref name="utf8Destination"/> must not overlap.</remarks>
        public unsafe virtual OperationStatus EncodeUtf8(
            ReadOnlySpan<byte> utf8Source,
            Span<byte> utf8Destination,
            out int bytesConsumed,
            out int bytesWritten,
            bool isFinalBlock = true)
        {
            int originalUtf8SourceLength = utf8Source.Length;
            int originalUtf8DestinationLength = utf8Destination.Length;

            const int TempUtf16CharBufferLength = 24; // arbitrarily chosen, but sufficient for any reasonable implementation
            char* pTempCharBuffer = stackalloc char[TempUtf16CharBufferLength];

            const int TempUtf8ByteBufferLength = TempUtf16CharBufferLength * 3 /* max UTF-8 output code units per UTF-16 input code unit */;
            byte* pTempUtf8Buffer = stackalloc byte[TempUtf8ByteBufferLength];

            uint nextScalarValue;
            int utf8BytesConsumedForScalar = 0;
            int nonEscapedByteCount = 0;
            OperationStatus opStatus = OperationStatus.Done;

            while (!utf8Source.IsEmpty)
            {
                // For performance, read until we require escaping.
                do
                {
                    nextScalarValue = utf8Source[nonEscapedByteCount];
                    if (UnicodeUtility.IsAsciiCodePoint(nextScalarValue))
                    {
                        // Check Ascii cache.
                        byte[]? encodedBytes = GetAsciiEncoding((byte)nextScalarValue);

                        if (ReferenceEquals(encodedBytes, s_noEscape))
                        {
                            if (++nonEscapedByteCount <= utf8Destination.Length)
                            {
                                // Source data can be copied as-is.
                                continue;
                            }

                            --nonEscapedByteCount;
                            opStatus = OperationStatus.DestinationTooSmall;
                            break;
                        }

                        if (encodedBytes == null)
                        {
                            // We need to escape and update the cache, so break out of this loop.
                            opStatus = OperationStatus.Done;
                            utf8BytesConsumedForScalar = 1;
                            break;
                        }

                        // For performance, handle the non-escaped bytes and encoding here instead of breaking out of the loop.
                        if (nonEscapedByteCount > 0)
                        {
                            // We previously verified the destination size.
                            Debug.Assert(nonEscapedByteCount <= utf8Destination.Length);

                            utf8Source.Slice(0, nonEscapedByteCount).CopyTo(utf8Destination);
                            utf8Source = utf8Source.Slice(nonEscapedByteCount);
                            utf8Destination = utf8Destination.Slice(nonEscapedByteCount);
                            nonEscapedByteCount = 0;
                        }

                        if (!((ReadOnlySpan<byte>)encodedBytes).TryCopyTo(utf8Destination))
                        {
                            opStatus = OperationStatus.DestinationTooSmall;
                            break;
                        }

                        utf8Destination = utf8Destination.Slice(encodedBytes.Length);
                        utf8Source = utf8Source.Slice(1);
                        continue;
                    }

                    // Code path for non-Ascii.
                    opStatus = UnicodeHelpers.DecodeScalarValueFromUtf8(utf8Source.Slice(nonEscapedByteCount), out nextScalarValue, out utf8BytesConsumedForScalar);
                    if (opStatus == OperationStatus.Done)
                    {
                        if (!WillEncode((int)nextScalarValue))
                        {
                            nonEscapedByteCount += utf8BytesConsumedForScalar;
                            if (nonEscapedByteCount <= utf8Destination.Length)
                            {
                                // Source data can be copied as-is.
                                continue;
                            }

                            nonEscapedByteCount -= utf8BytesConsumedForScalar;
                            opStatus = OperationStatus.DestinationTooSmall;
                        }
                    }

                    // We need to escape.
                    break;
                } while (nonEscapedByteCount < utf8Source.Length);

                if (nonEscapedByteCount > 0)
                {
                    // We previously verified the destination size.
                    Debug.Assert(nonEscapedByteCount <= utf8Destination.Length);

                    utf8Source.Slice(0, nonEscapedByteCount).CopyTo(utf8Destination);
                    utf8Source = utf8Source.Slice(nonEscapedByteCount);
                    utf8Destination = utf8Destination.Slice(nonEscapedByteCount);
                    nonEscapedByteCount = 0;
                }

                if (utf8Source.IsEmpty)
                {
                    goto Done;
                }

                // This code path is hit for ill-formed input data (where decoding has replaced it with U+FFFD)
                // and for well-formed input data that must be escaped.

                if (opStatus != OperationStatus.Done) // Optimize happy path.
                {
                    if (opStatus == OperationStatus.NeedMoreData)
                    {
                        if (!isFinalBlock)
                        {
                            bytesConsumed = originalUtf8SourceLength - utf8Source.Length;
                            bytesWritten = originalUtf8DestinationLength - utf8Destination.Length;
                            return OperationStatus.NeedMoreData;
                        }
                        // else treat this as a normal invalid subsequence.
                    }
                    else if (opStatus == OperationStatus.DestinationTooSmall)
                    {
                        goto ReturnDestinationTooSmall;
                    }
                }

                if (TryEncodeUnicodeScalar((int)nextScalarValue, pTempCharBuffer, TempUtf16CharBufferLength, out int charsWrittenJustNow))
                {
                    // Now that we have it as UTF-16, transcode it to UTF-8.
                    // Need to copy it to a temporary buffer first, otherwise GetBytes might throw an exception
                    // due to lack of output space.

                    int transcodedByteCountThisIteration = Encoding.UTF8.GetBytes(pTempCharBuffer, charsWrittenJustNow, pTempUtf8Buffer, TempUtf8ByteBufferLength);
                    ReadOnlySpan<byte> transcodedUtf8BytesThisIteration = new ReadOnlySpan<byte>(pTempUtf8Buffer, transcodedByteCountThisIteration);

                    // Update cache for Ascii
                    if (UnicodeUtility.IsAsciiCodePoint(nextScalarValue))
                    {
                        _asciiEscape[nextScalarValue] = transcodedUtf8BytesThisIteration.ToArray();
                    }

                    if (!transcodedUtf8BytesThisIteration.TryCopyTo(utf8Destination))
                    {
                        goto ReturnDestinationTooSmall;
                    }

                    utf8Destination = utf8Destination.Slice(transcodedByteCountThisIteration);
                }
                else
                {
                    // We really don't expect this to fail. If that happens we'll report an error to our caller.
                    bytesConsumed = originalUtf8SourceLength - utf8Source.Length;
                    bytesWritten = originalUtf8DestinationLength - utf8Destination.Length;
                    return OperationStatus.InvalidData;
                }

                utf8Source = utf8Source.Slice(utf8BytesConsumedForScalar);
            }

        Done:
            // Input buffer has been fully processed!
            bytesConsumed = originalUtf8SourceLength;
            bytesWritten = originalUtf8DestinationLength - utf8Destination.Length;
            return OperationStatus.Done;

        ReturnDestinationTooSmall:
            bytesConsumed = originalUtf8SourceLength - utf8Source.Length;
            bytesWritten = originalUtf8DestinationLength - utf8Destination.Length;
            return OperationStatus.DestinationTooSmall;
        }

        /// <summary>
        /// Encodes the supplied characters.
        /// </summary>
        /// <param name="source">A source buffer containing the characters to encode.</param>
        /// <param name="destination">The destination buffer to which the encoded form of <paramref name="source"/>
        /// will be written.</param>
        /// <param name="charsConsumed">The number of characters consumed from the <paramref name="source"/> buffer.</param>
        /// <param name="charsWritten">The number of characters written to the <paramref name="destination"/> buffer.</param>
        /// <param name="isFinalBlock"><see langword="true"/> if there is further source data that needs to be encoded;
        /// <see langword="false"/> if there is no further source data that needs to be encoded.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the encoding operation.</returns>
        /// <remarks>The buffers <paramref name="source"/> and <paramref name="destination"/> must not overlap.</remarks>
        public virtual OperationStatus Encode(
            ReadOnlySpan<char> source,
            Span<char> destination,
            out int charsConsumed,
            out int charsWritten,
            bool isFinalBlock = true)
        {
            unsafe
            {
                fixed (char* sourcePtr = source)
                {
                    int firstCharacterToEncode;
                    if (source.IsEmpty || (firstCharacterToEncode = FindFirstCharacterToEncode(sourcePtr, source.Length)) == -1)
                    {
                        if (source.TryCopyTo(destination))
                        {
                            charsConsumed = source.Length;
                            charsWritten = source.Length;
                            return OperationStatus.Done;
                        }

                        charsConsumed = 0;
                        charsWritten = 0;
                        return OperationStatus.DestinationTooSmall;
                    }
                    else if (destination.IsEmpty)
                    {
                        // Guards against passing a null destinationPtr to EncodeIntoBuffer (pinning an empty Span will return a null pointer).
                        charsConsumed = 0;
                        charsWritten = 0;
                        return OperationStatus.DestinationTooSmall;
                    }

                    fixed (char* destinationPtr = destination)
                    {
                        return EncodeIntoBuffer(destinationPtr, destination.Length, sourcePtr, source.Length, out charsConsumed, out charsWritten, firstCharacterToEncode, isFinalBlock);
                    }
                }
            }
        }

        private unsafe void EncodeCore(TextWriter output, char* value, int valueLength)
        {
            Debug.Assert(value != null && output != null);
            Debug.Assert(valueLength >= 0);

            int bufferLength = MaxOutputCharactersPerInputCharacter;
            char* buffer = stackalloc char[bufferLength];

            char firstChar = *value;
            char secondChar = firstChar;
            bool wasSurrogatePair = false;
            int charsWritten;

            // this loop processes character pairs (in case they are surrogates).
            // there is an if block below to process single last character.
            int secondCharIndex;
            for (secondCharIndex = 1; secondCharIndex < valueLength; secondCharIndex++)
            {
                if (!wasSurrogatePair)
                {
                    firstChar = secondChar;
                }
                else
                {
                    firstChar = value[secondCharIndex - 1];
                }
                secondChar = value[secondCharIndex];

                if (!WillEncode(firstChar))
                {
                    wasSurrogatePair = false;
                    output.Write(firstChar);
                }
                else
                {
                    int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, secondChar, out wasSurrogatePair, out bool _);
                    if (!TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out charsWritten))
                    {
                        ThrowArgumentException_MaxOutputCharsPerInputChar();
                    }
                    Write(output, buffer, charsWritten);

                    if (wasSurrogatePair)
                    {
                        secondCharIndex++;
                    }
                }
            }

            if (!wasSurrogatePair || (secondCharIndex == valueLength))
            {
                firstChar = value[valueLength - 1];
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, null, out wasSurrogatePair, out bool _);
                if (!TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out charsWritten))
                {
                    ThrowArgumentException_MaxOutputCharsPerInputChar();
                }
                Write(output, buffer, charsWritten);
            }
        }

        private unsafe int FindFirstCharacterToEncode(ReadOnlySpan<char> text)
        {
            fixed (char* pText = &MemoryMarshal.GetReference(text))
            {
                return FindFirstCharacterToEncode(pText, text.Length);
            }
        }

        /// <summary>
        /// Given a UTF-8 text input buffer, finds the first element in the input buffer which would be
        /// escaped by the current encoder instance.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 text input buffer to search.</param>
        /// <returns>
        /// The index of the first element in <paramref name="utf8Text"/> which would be escaped by the
        /// current encoder instance, or -1 if no data in <paramref name="utf8Text"/> requires escaping.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual unsafe int FindFirstCharacterToEncodeUtf8(ReadOnlySpan<byte> utf8Text)
        {
            if (!_isAsciiCacheInitialized)
            {
                InitializeAsciiCache();
            }

            // Loop through the input text, terminating when we see ill-formed UTF-8 or when we decode a scalar value
            // that must be encoded. If we see either of these things then we'll return its index in the original
            // input sequence. If we consume the entire text without seeing either of these, return -1 to indicate
            // that the text can be copied as-is without escaping.

            fixed (byte* ptr = utf8Text)
            {
                int idx = 0;

#if NETCOREAPP
                if ((Sse2.IsSupported || AdvSimd.Arm64.IsSupported) && utf8Text.Length - 16 >= idx)
                {
                    // Hoist these outside the loop, as the JIT won't do it.
                    Vector128<sbyte> bitMaskLookupAsciiNeedsEscaping = _bitMaskLookupAsciiNeedsEscaping;
                    Vector128<sbyte> bitPosLookup = Ssse3Helper.s_bitPosLookup;
                    Vector128<sbyte> nibbleMaskSByte = Ssse3Helper.s_nibbleMaskSByte;
                    Vector128<sbyte> nullMaskSByte = Ssse3Helper.s_nullMaskSByte;

                    sbyte* startingAddress = (sbyte*)ptr;
                    do
                    {
                        Debug.Assert(startingAddress >= ptr && startingAddress <= (ptr + utf8Text.Length - 16));

                        // Load the next 16 bytes.
                        Vector128<sbyte> sourceValue;
                        bool containsNonAsciiBytes;

                        // Check for ASCII text. Any byte that's not in the ASCII range will already be negative when
                        // casted to signed byte.
                        if (Sse2.IsSupported)
                        {
                            sourceValue = Sse2.LoadVector128(startingAddress);
                            containsNonAsciiBytes = Sse2Helper.ContainsNonAsciiByte(sourceValue);
                        }
                        else if (AdvSimd.Arm64.IsSupported)
                        {
                            sourceValue = AdvSimd.LoadVector128(startingAddress);
                            containsNonAsciiBytes = AdvSimdHelper.ContainsNonAsciiByte(sourceValue);
                        }
                        else
                        {
                            throw new PlatformNotSupportedException();
                        }

                        if (!containsNonAsciiBytes)
                        {
                            // All of the following 16 bytes is ASCII.
                            // TODO AdvSimd: optimization maybe achievable using VectorTableLookup and/or VectorTableLookupExtension

                            if (Ssse3.IsSupported)
                            {
                                Vector128<sbyte> mask = Ssse3Helper.CreateEscapingMask(sourceValue, bitMaskLookupAsciiNeedsEscaping, bitPosLookup, nibbleMaskSByte, nullMaskSByte);
                                int index = Sse2Helper.GetIndexOfFirstNonAsciiByte(mask.AsByte());

                                if (index < 16)
                                {
                                    idx += index;
                                    goto Return;
                                }
                            }
                            else
                            {
                                byte* p = (byte*)startingAddress;
                                if (DoesAsciiNeedEncoding(p[0])) goto Return;
                                if (DoesAsciiNeedEncoding(p[1])) goto Return1;
                                if (DoesAsciiNeedEncoding(p[2])) goto Return2;
                                if (DoesAsciiNeedEncoding(p[3])) goto Return3;
                                if (DoesAsciiNeedEncoding(p[4])) goto Return4;
                                if (DoesAsciiNeedEncoding(p[5])) goto Return5;
                                if (DoesAsciiNeedEncoding(p[6])) goto Return6;
                                if (DoesAsciiNeedEncoding(p[7])) goto Return7;
                                if (DoesAsciiNeedEncoding(p[8])) goto Return8;
                                if (DoesAsciiNeedEncoding(p[9])) goto Return9;
                                if (DoesAsciiNeedEncoding(p[10])) goto Return10;
                                if (DoesAsciiNeedEncoding(p[11])) goto Return11;
                                if (DoesAsciiNeedEncoding(p[12])) goto Return12;
                                if (DoesAsciiNeedEncoding(p[13])) goto Return13;
                                if (DoesAsciiNeedEncoding(p[14])) goto Return14;
                                if (DoesAsciiNeedEncoding(p[15])) goto Return15;
                            }

                            idx += 16;
                        }
                        else
                        {
                            // At least one of the following 16 bytes is non-ASCII.

                            int processNextSixteen = idx + 16;
                            Debug.Assert(processNextSixteen <= utf8Text.Length);

                            while (idx < processNextSixteen)
                            {
                                Debug.Assert((ptr + idx) <= (ptr + utf8Text.Length));

                                if (UnicodeUtility.IsAsciiCodePoint(ptr[idx]))
                                {
                                    if (DoesAsciiNeedEncoding(ptr[idx]))
                                    {
                                        goto Return;
                                    }
                                    idx++;
                                }
                                else
                                {
                                    OperationStatus opStatus = UnicodeHelpers.DecodeScalarValueFromUtf8(utf8Text.Slice(idx), out uint nextScalarValue, out int utf8BytesConsumedForScalar);

                                    Debug.Assert(nextScalarValue <= int.MaxValue);
                                    if (opStatus != OperationStatus.Done || WillEncode((int)nextScalarValue))
                                    {
                                        goto Return;
                                    }

                                    Debug.Assert(opStatus == OperationStatus.Done);
                                    idx += utf8BytesConsumedForScalar;
                                }
                            }
                        }
                        startingAddress = (sbyte*)ptr + idx;
                    }
                    while (utf8Text.Length - 16 >= idx);

                    // Process the remaining bytes.
                    Debug.Assert(utf8Text.Length - idx < 16);
                }
#endif

                while (idx < utf8Text.Length)
                {
                    Debug.Assert((ptr + idx) <= (ptr + utf8Text.Length));

                    if (UnicodeUtility.IsAsciiCodePoint(ptr[idx]))
                    {
                        if (DoesAsciiNeedEncoding(ptr[idx]))
                        {
                            goto Return;
                        }
                        idx++;
                    }
                    else
                    {
                        OperationStatus opStatus = UnicodeHelpers.DecodeScalarValueFromUtf8(utf8Text.Slice(idx), out uint nextScalarValue, out int utf8BytesConsumedForScalar);

                        Debug.Assert(nextScalarValue <= int.MaxValue);
                        if (opStatus != OperationStatus.Done || WillEncode((int)nextScalarValue))
                        {
                            goto Return;
                        }

                        Debug.Assert(opStatus == OperationStatus.Done);
                        idx += utf8BytesConsumedForScalar;
                    }
                }
                Debug.Assert(idx == utf8Text.Length);

                idx = -1; // All bytes are allowed.
                goto Return;

#if NETCOREAPP
            Return15:
                return idx + 15;
            Return14:
                return idx + 14;
            Return13:
                return idx + 13;
            Return12:
                return idx + 12;
            Return11:
                return idx + 11;
            Return10:
                return idx + 10;
            Return9:
                return idx + 9;
            Return8:
                return idx + 8;
            Return7:
                return idx + 7;
            Return6:
                return idx + 6;
            Return5:
                return idx + 5;
            Return4:
                return idx + 4;
            Return3:
                return idx + 3;
            Return2:
                return idx + 2;
            Return1:
                return idx + 1;
#endif
            Return:
                return idx;
            }
        }

        internal static unsafe bool TryCopyCharacters(char[] source, char* destination, int destinationLength, out int numberOfCharactersWritten)
        {
            Debug.Assert(source != null && destination != null && destinationLength >= 0);

            if (destinationLength < source.Length)
            {
                numberOfCharactersWritten = 0;
                return false;
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = source[i];
            }

            numberOfCharactersWritten = source.Length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool TryWriteScalarAsChar(int unicodeScalar, char* destination, int destinationLength, out int numberOfCharactersWritten)
        {
            Debug.Assert(destination != null && destinationLength >= 0);

            Debug.Assert(unicodeScalar < ushort.MaxValue);
            if (destinationLength < 1)
            {
                numberOfCharactersWritten = 0;
                return false;
            }
            *destination = (char)unicodeScalar;
            numberOfCharactersWritten = 1;
            return true;
        }

        private static void ValidateRanges(int startIndex, int characterCount, int actualInputLength)
        {
            if (startIndex < 0 || startIndex > actualInputLength)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }
            if (characterCount < 0 || characterCount > (actualInputLength - startIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(characterCount));
            }
        }

        private static unsafe void Write(TextWriter output, char* input, int inputLength)
        {
            Debug.Assert(output != null && input != null && inputLength >= 0);

            while (inputLength-- > 0)
            {
                output.Write(*input);
                input++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte[]? GetAsciiEncoding(byte value)
        {
            byte[] encoding = _asciiEscape[value];
            if (encoding == null)
            {
                if (!WillEncode(value))
                {
                    encoding = s_noEscape;
                    _asciiEscape[value] = encoding;
                }
            }
            return encoding;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe void InitializeAsciiCache()
        {
#if NETCOREAPP
            if (Ssse3.IsSupported)
            {
                Vector128<sbyte> vector = Vector128<sbyte>.Zero;
                sbyte* tmp = (sbyte*)&vector;

                for (int i = 0; i < 0x80; i++)
                {
                    bool willEncode = WillEncode(i);
                    _asciiNeedsEscaping.Data[i] = willEncode;

                    if (willEncode)
                    {
                        int highNibble = i >> 4;
                        int lowNibble = i & 0xF;

                        tmp[lowNibble] |= (sbyte)(1 << highNibble);
                    }
                }

                _bitMaskLookupAsciiNeedsEscaping = vector;
                return;
            }
#endif
            for (int i = 0; i < 0x80; i++)
            {
                _asciiNeedsEscaping.Data[i] = WillEncode(i);
            }

            _isAsciiCacheInitialized = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe bool DoesAsciiNeedEncoding(uint value)
        {
            Debug.Assert(value <= 0x7F);

            return _asciiNeedsEscaping.Data[value];
        }

        private unsafe struct AsciiNeedsEscapingData
        {
            public fixed bool Data[0x80];
        }

        private static void ThrowArgumentException_MaxOutputCharsPerInputChar()
        {
            throw new ArgumentException(SR.TextEncoderDoesNotImplementMaxOutputCharsPerInputChar);
        }
    }
}
