// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Encodings.Web
{
    public static class TextEncoderExtensions
    {
        public static string Encode(this TextEncoder encoder, string value)
        {
            if (value == null)
            {
                return value;
            }

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    int firstCharacterToEncode = encoder.FindFirstCharacterToEncode(valuePointer, value.Length);

                    if (firstCharacterToEncode == -1)
                    {
                        return value;
                    }

                    int bufferSize = encoder.MaxOutputCharsPerInputChar * value.Length;
                    char* wholebuffer = stackalloc char[bufferSize];
                    char* buffer = wholebuffer;
                    int totalWritten = 0;

                    if (firstCharacterToEncode > 0)
                    {
                        int bytesToCopy = firstCharacterToEncode + firstCharacterToEncode;
                        Buffer.MemoryCopy(valuePointer, buffer, bytesToCopy, bytesToCopy);
                        totalWritten += firstCharacterToEncode;
                        bufferSize -= firstCharacterToEncode;
                        buffer += firstCharacterToEncode;
                    }

                    int valueIndex = firstCharacterToEncode;

                    char firstChar = value[valueIndex];
                    char secondChar = firstChar;
                    bool wasSurrogatePair = false;
                    int charsWritten;

                    // this loop processes character pairs (in case they are surrogates).
                    // there is an if block below to process single last character.
                    for (int secondCharIndex = valueIndex + 1; secondCharIndex < value.Length; secondCharIndex++)
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

                        if (!encoder.Encodes(firstChar))
                        {
                            wasSurrogatePair = false;
                            *buffer = firstChar;
                            buffer++;
                            bufferSize--;
                            totalWritten++;
                        }
                        else
                        {
                            int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, secondChar, out wasSurrogatePair);
                            if (!encoder.TryEncodeUnicodeScalar(nextScalar, buffer, bufferSize, out charsWritten))
                            {
                                throw new ArgumentException("Argument encoder does not implement MaxOutputCharsPerInputChar correctly."); 
                            }

                            buffer += charsWritten;
                            bufferSize -= charsWritten;
                            totalWritten += charsWritten;
                            if (wasSurrogatePair)
                            {
                                secondCharIndex++;
                            }
                        }
                    }

                    if (!wasSurrogatePair)
                    {
                        firstChar = value[value.Length - 1];
                        int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, null, out wasSurrogatePair);
                        if (!encoder.TryEncodeUnicodeScalar(nextScalar, buffer, bufferSize, out charsWritten))
                        {
                            throw new ArgumentException("Argument encoder does not implement MaxOutputCharsPerInputChar correctly."); 
                        }

                        buffer += charsWritten;
                        bufferSize -= charsWritten;
                        totalWritten += charsWritten;
                    }

                    var result = new String(wholebuffer, 0, totalWritten);
                    return result;
                }
            }
        }

        public static void Encode(this TextEncoder encoder, string value, TextWriter output)
        {
            Encode(encoder, value, 0, value.Length, output);
        }

        public static void Encode(this TextEncoder encoder, string value, int startIndex, int charCount, TextWriter output)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            ValidateRanges(startIndex, charCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = encoder.FindFirstCharacterToEncode(substring, charCount);

                    if (firstIndexToEncode == -1) // nothing to encode; 
                    {
                        if(startIndex == 0 && charCount == value.Length) // write whole string
                        {
                            output.Write(value);
                            return;
                        }
                        for(int i=0; i<charCount; i++) // write substring
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

                    EncodeCore(encoder, substring, charCount - firstIndexToEncode, output);
                }
            }
        }

        public static void Encode(this TextEncoder encoder, char[] value, int startIndex, int charCount, TextWriter output)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            ValidateRanges(startIndex, charCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = encoder.FindFirstCharacterToEncode(substring, charCount);

                    if (firstIndexToEncode == -1) // nothing to encode; 
                    {
                        if (startIndex == 0 && charCount == value.Length) // write whole string
                        {
                            output.Write(value);
                            return;
                        }
                        for (int i = 0; i < charCount; i++) // write substring
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

                    EncodeCore(encoder, substring, charCount - firstIndexToEncode, output);
                }
            }
        }

        private static unsafe void EncodeCore(this TextEncoder encoder, char* value, int charCount, TextWriter output)
        {
            int bufferLength = encoder.MaxOutputCharsPerInputChar;
            char* buffer = stackalloc char[bufferLength]; 

            char firstChar = *value;
            char secondChar = firstChar;
            bool wasSurrogatePair = false;
            int charsWritten;

            // this loop processes character pairs (in case they are surrogates).
            // there is an if block below to process single last character.
            for (int secondCharIndex = 1; secondCharIndex < charCount; secondCharIndex++)
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

                if (!encoder.Encodes(firstChar))
                {
                    wasSurrogatePair = false;
                    output.Write(firstChar); 
                }
                else
                {
                    int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, secondChar, out wasSurrogatePair);
                    if (!encoder.TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out charsWritten))
                    {
                        throw new ArgumentException("Argument encoder does not implement MaxOutputCharsPerInputChar correctly."); 
                    }
                    output.Write(buffer, charsWritten);

                    if (wasSurrogatePair)
                    {
                        secondCharIndex++;
                    }
                }
            }

            if (!wasSurrogatePair)
            {
                firstChar = value[charCount - 1];
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, null, out wasSurrogatePair);
                if (!encoder.TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out charsWritten))
                {
                    throw new ArgumentException("Argument encoder does not implement MaxOutputCharsPerInputChar correctly."); 
                }
                output.Write(buffer, charsWritten);
            }
        }

        internal unsafe static bool TryCopyCharacters(this char[] source, char* destination, int destinationLength, out int writtenChars)
        {
            if (destinationLength < source.Length)
            {
                writtenChars = 0;
                return false;
            }

            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = source[i];
            }

            writtenChars = source.Length;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static bool TryWriteScalarAsChar(this int unicodeScalar, char* destination, int destinationLength, out int writtenChars)
        {
            Debug.Assert(unicodeScalar < ushort.MaxValue);
            if (destinationLength < 1)
            {
                writtenChars = 0;
                return false;
            }
            *destination = (char)unicodeScalar;
            writtenChars = 1;
            return true;
        }

        private static void ValidateRanges(int startIndex, int charCount, int actualInputLength)
        {
            if (startIndex < 0 || startIndex > actualInputLength)
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (charCount < 0 || charCount > (actualInputLength - startIndex))
            {
                throw new ArgumentOutOfRangeException("charCount");
            }
        }

        private unsafe static void Write(this TextWriter output, char* input, int inputLength)
        {
            while (inputLength-- > 0)
            {
                output.Write(*input);
                input++;
            }
        }
    }
}
