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
            if (encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
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

                    int bufferSize = encoder.MaxOutputCharactersPerInputCharacter * value.Length;
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

        public static void Encode(this TextEncoder encoder, string value, int startIndex, int characterCount, TextWriter output)
        {
            if (encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = encoder.FindFirstCharacterToEncode(substring, characterCount);

                    if (firstIndexToEncode == -1) // nothing to encode; 
                    {
                        if(startIndex == 0 && characterCount == value.Length) // write whole string
                        {
                            output.Write(value);
                            return;
                        }
                        for(int i=0; i<characterCount; i++) // write substring
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

                    EncodeCore(encoder, substring, characterCount - firstIndexToEncode, output);
                }
            }
        }

        public static void Encode(this TextEncoder encoder, char[] value, int startIndex, int characterCount, TextWriter output)
        {
            if(encoder == null)
            {
                throw new ArgumentNullException("encoder");
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }
            ValidateRanges(startIndex, characterCount, actualInputLength: value.Length);

            unsafe
            {
                fixed (char* valuePointer = value)
                {
                    char* substring = valuePointer + startIndex;
                    int firstIndexToEncode = encoder.FindFirstCharacterToEncode(substring, characterCount);

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

                    EncodeCore(encoder, substring, characterCount - firstIndexToEncode, output);
                }
            }
        }

        private static unsafe void EncodeCore(this TextEncoder encoder, char* value, int valueLength, TextWriter output)
        {
            Debug.Assert(encoder != null && value != null & output != null);
            Debug.Assert(valueLength >= 0);

            int bufferLength = encoder.MaxOutputCharactersPerInputCharacter;
            char* buffer = stackalloc char[bufferLength]; 

            char firstChar = *value;
            char secondChar = firstChar;
            bool wasSurrogatePair = false;
            int charsWritten;

            // this loop processes character pairs (in case they are surrogates).
            // there is an if block below to process single last character.
            for (int secondCharIndex = 1; secondCharIndex < valueLength; secondCharIndex++)
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
                firstChar = value[valueLength - 1];
                int nextScalar = UnicodeHelpers.GetScalarValueFromUtf16(firstChar, null, out wasSurrogatePair);
                if (!encoder.TryEncodeUnicodeScalar(nextScalar, buffer, bufferLength, out charsWritten))
                {
                    throw new ArgumentException("Argument encoder does not implement MaxOutputCharsPerInputChar correctly."); 
                }
                output.Write(buffer, charsWritten);
            }
        }

        internal unsafe static bool TryCopyCharacters(this char[] source, char* destination, int destinationLength, out int numberOfCharactersWritten)
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
        internal unsafe static bool TryWriteScalarAsChar(this int unicodeScalar, char* destination, int destinationLength, out int numberOfCharactersWritten)
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
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if (characterCount < 0 || characterCount > (actualInputLength - startIndex))
            {
                throw new ArgumentOutOfRangeException("characterCount");
            }
        }

        private unsafe static void Write(this TextWriter output, char* input, int inputLength)
        {
            Debug.Assert(output != null && input != null && inputLength >= 0);

            while (inputLength-- > 0)
            {
                output.Write(*input);
                input++;
            }
        }
    }
}
