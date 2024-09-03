// plist-cil - An open source library to parse and generate property lists for .NET
// Copyright (C) 2015 Natalia Portillo
//
// This code is based on:
// plist - An open source library to parse and generate property lists
// Copyright (C) 2014 Daniel Dreibrodt
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>
    ///     <para>
    ///         Parses property lists that are in Apple's binary format. Use this class when you are sure about the format of
    ///         the property list. Otherwise use the PropertyListParser class.
    ///     </para>
    ///     <para>
    ///         Parsing is done by calling the static <see cref="Parse(byte[])" />, <see cref="Parse(FileInfo)" /> and
    ///         <see cref="Parse(Stream)" /> methods.
    ///     </para>
    /// </summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class BinaryPropertyListParser
    {
        static readonly Encoding utf16BigEndian = Encoding.GetEncoding("UTF-16BE");

        /// <summary>Major version of the property list format</summary>
        int majorVersion;

        /// <summary>Minor version of the property list format</summary>
        int minorVersion;

        /// <summary>Length of an object reference in bytes</summary>
        int objectRefSize;

        /// <summary>The table holding the information at which offset each object is found</summary>
        int[] offsetTable;

        /// <summary>Protected constructor so that instantiation is fully controlled by the static parse methods.</summary>
        /// <see cref="Parse(byte[])" />
        protected BinaryPropertyListParser() {}

        /// <summary>Parses a binary property list from a byte array.</summary>
        /// <param name="data">The binary property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        public static NSObject Parse(byte[] data) => Parse(data.AsSpan());

        /// <summary>Parses a binary property list from a byte array.</summary>
        /// <param name="data">The binary property list's data.</param>
        /// <param name="offset">The length of the property list.</param>
        /// <param name="count">The offset at which to start reading the property list.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        public static NSObject Parse(byte[] data, int offset, int length) => Parse(data.AsSpan(offset, length));

        /// <summary>Parses a binary property list from a byte span.</summary>
        /// <param name="data">The binary property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        public static NSObject Parse(ReadOnlySpan<byte> data)
        {
            var parser = new BinaryPropertyListParser();

            return parser.DoParse(data);
        }

        /// <summary>Parses a binary property list from a byte array.</summary>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <param name="bytes">The binary property list's data.</param>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        protected NSObject DoParse(ReadOnlySpan<byte> bytes)
        {
            if(bytes.Length < 8    ||
               bytes[0]     != 'b' ||
               bytes[1]     != 'p' ||
               bytes[2]     != 'l' ||
               bytes[3]     != 'i' ||
               bytes[4]     != 's' ||
               bytes[5]     != 't')
            {
                string magic = Encoding.ASCII.GetString(bytes.Slice(0, 8).ToArray());

                throw new PropertyListFormatException("The given data is no binary property list. Wrong magic bytes: " +
                                                      magic);
            }

            majorVersion = bytes[6] - 0x30; //ASCII number
            minorVersion = bytes[7] - 0x30; //ASCII number

            // 0.0 - OS X Tiger and earlier
            // 0.1 - Leopard
            // 0.? - Snow Leopard
            // 1.5 - Lion
            // 2.0 - Snow Lion

            if(majorVersion > 0)
                throw new PropertyListFormatException("Unsupported binary property list format: v" + majorVersion +
                                                      "." + minorVersion + ". " +
                                                      "Version 1.0 and later are not yet supported.");

            /*
             * Handle trailer, last 32 bytes of the file
             */
            ReadOnlySpan<byte> trailer = bytes.Slice(bytes.Length - 32, 32);

            //6 null bytes (index 0 to 5)
            int offsetSize = trailer[6];
            objectRefSize = trailer[7];
            int numObjects        = (int)BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(8, 8));
            int topObject         = (int)BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(16, 8));
            int offsetTableOffset = (int)BinaryPrimitives.ReadUInt64BigEndian(trailer.Slice(24, 8));

            /*
             * Handle offset table
             */
            offsetTable = new int[numObjects];

            for(int i = 0; i < numObjects; i++)
            {
                ReadOnlySpan<byte> offsetBytes = bytes.Slice(offsetTableOffset + (i * offsetSize), offsetSize);
                offsetTable[i] = (int)ParseUnsignedInt(offsetBytes);
            }

            return ParseObject(bytes, topObject);
        }

        /// <summary>Parses a binary property list from an input stream.</summary>
        /// <param name="fs">The input stream that points to the property list's data.</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        public static NSObject Parse(Stream fs)
        {
            //Read all bytes into a list
            byte[] buf = PropertyListParser.ReadAll(fs);

            // Don't close the stream - that would be the responisibility of code that class
            // Parse
            return Parse(buf);
        }

        /// <summary>Parses a binary property list file.</summary>
        /// <param name="f">The binary property list file</param>
        /// <returns>The root object of the property list. This is usually a NSDictionary but can also be a NSArray.</returns>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        public static NSObject Parse(FileInfo f) => Parse(f.OpenRead());

        protected int GetOffset(int obj) => offsetTable[obj];

        /// <summary>
        ///     Parses an object inside the currently parsed binary property list. For the format specification check
        ///     <a href="http://www.opensource.apple.com/source/CF/CF-855.17/CFBinaryPList.c">
        ///         Apple's binary property list parser
        ///         implementation
        ///     </a>
        ///     .
        /// </summary>
        /// <returns>The parsed object.</returns>
        /// <param name="obj">The object ID.</param>
        /// <exception cref="PropertyListFormatException">When the property list's format could not be parsed.</exception>
        protected virtual NSObject ParseObject(ReadOnlySpan<byte> bytes, int obj)
        {
            int  offset  = offsetTable[obj];
            byte type    = bytes[offset];
            int  objType = (type & 0xF0) >> 4; //First  4 bits
            int  objInfo = type & 0x0F;        //Second 4 bits

            switch(objType)
            {
                case 0x0:
                {
                    //Simple
                    switch(objInfo)
                    {
                        case 0x0:
                        {
                            //null object (v1.0 and later)
                            return null;
                        }
                        case 0x8:
                        {
                            //false
                            return new NSNumber(false);
                        }
                        case 0x9:
                        {
                            //true
                            return new NSNumber(true);
                        }
                        case 0xC:
                        {
                            //URL with no base URL (v1.0 and later)
                            //TODO Implement binary URL parsing (not yet even implemented in Core Foundation as of revision 855.17)
                            break;
                        }
                        case 0xD:
                        {
                            //URL with base URL (v1.0 and later)
                            //TODO Implement binary URL parsing (not yet even implemented in Core Foundation as of revision 855.17)
                            break;
                        }
                        case 0xE:
                        {
                            //16-byte UUID (v1.0 and later)
                            //TODO Implement binary UUID parsing (not yet even implemented in Core Foundation as of revision 855.17)
                            break;
                        }
                        case 0xF:
                        {
                            //filler byte
                            return null;
                        }
                    }

                    break;
                }
                case 0x1:
                {
                    //integer
                    int length = 1 << objInfo;

                    return new NSNumber(bytes.Slice(offset + 1, length), NSNumber.INTEGER);
                }
                case 0x2:
                {
                    //real
                    int length = 1 << objInfo;

                    return new NSNumber(bytes.Slice(offset + 1, length), NSNumber.REAL);
                }
                case 0x3:
                {
                    //Date
                    if(objInfo != 0x3)
                        throw new
                            PropertyListFormatException("The given binary property list contains a date object of an unknown type (" +
                                                        objInfo + ")");

                    return new NSDate(bytes.Slice(offset + 1, 8));
                }
                case 0x4:
                {
                    //Data
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int dataoffset);

                    return new NSData(CopyOfRange(bytes, offset + dataoffset, offset + dataoffset + length));
                }
                case 0x5:
                {
                    //ASCII String, each character is 1 byte
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int stroffset);

                    return new NSString(bytes.Slice(offset + stroffset, length), Encoding.ASCII);
                }
                case 0x6:
                {
                    //UTF-16-BE String
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int stroffset);

                    //UTF-16 characters can have variable length, but the Core Foundation reference implementation
                    //assumes 2 byte characters, thus only covering the Basic Multilingual Plane
                    length *= 2;

                    return new NSString(bytes.Slice(offset + stroffset, length), utf16BigEndian);
                }
                case 0x7:
                {
                    //UTF-8 string (v1.0 and later)
                    ReadLengthAndOffset(bytes, objInfo, offset, out int strOffset, out int characters);

                    //UTF-8 characters can have variable length, so we need to calculate the byte length dynamically
                    //by reading the UTF-8 characters one by one
                    int length = CalculateUtf8StringLength(bytes, offset + strOffset, characters);

                    return new NSString(bytes.Slice(offset + strOffset, length), Encoding.UTF8);
                }
                case 0x8:
                {
                    //UID (v1.0 and later)
                    int length = objInfo + 1;

                    return new UID(bytes.Slice(offset + 1, length));
                }
                case 0xA:
                {
                    //Array
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int arrayOffset);

                    var array = new NSArray(length);

                    for(int i = 0; i < length; i++)
                    {
                        int objRef =
                            (int)ParseUnsignedInt(bytes.Slice(offset + arrayOffset + (i * objectRefSize),
                                                              objectRefSize));

                        array.Add(ParseObject(bytes, objRef));
                    }

                    return array;
                }
                case 0xB:
                {
                    //Ordered set (v1.0 and later)
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int contentOffset);

                    var set = new NSSet(true);

                    for(int i = 0; i < length; i++)
                    {
                        int objRef =
                            (int)ParseUnsignedInt(bytes.Slice(offset + contentOffset + (i * objectRefSize),
                                                              objectRefSize));

                        set.AddObject(ParseObject(bytes, objRef));
                    }

                    return set;
                }
                case 0xC:
                {
                    //Set (v1.0 and later)
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int contentOffset);

                    var set = new NSSet();

                    for(int i = 0; i < length; i++)
                    {
                        int objRef =
                            (int)ParseUnsignedInt(bytes.Slice(offset + contentOffset + (i * objectRefSize),
                                                              objectRefSize));

                        set.AddObject(ParseObject(bytes, objRef));
                    }

                    return set;
                }
                case 0xD:
                {
                    //Dictionary
                    ReadLengthAndOffset(bytes, objInfo, offset, out int length, out int contentOffset);

                    //System.out.println("Parsing dictionary #"+obj);
                    var dict = new NSDictionary(length);

                    for(int i = 0; i < length; i++)
                    {
                        int keyRef =
                            (int)ParseUnsignedInt(bytes.Slice(offset + contentOffset + (i * objectRefSize),
                                                              objectRefSize));

                        int valRef =
                            (int)
                            ParseUnsignedInt(bytes.Slice(offset + contentOffset + (length * objectRefSize) + (i * objectRefSize),
                                                         objectRefSize));

                        NSObject key = ParseObject(bytes, keyRef);
                        NSObject val = ParseObject(bytes, valRef);
                        dict.Add(key.ToString(), val);
                    }

                    return dict;
                }
                default:
                {
                    Debug.WriteLine("WARNING: The given binary property list contains an object of unknown type (" +
                                    objType + ")");

                    break;
                }
            }

            return null;
        }

        /// <summary>Reads the length for arrays, sets and dictionaries.</summary>
        /// <returns>An array with the length two. First entry is the length, second entry the offset at which the content starts.</returns>
        /// <param name="objInfo">Object information byte.</param>
        /// <param name="offset">Offset in the byte array at which the object is located.</param>
        void ReadLengthAndOffset(ReadOnlySpan<byte> bytes, int objInfo, int offset, out int lengthValue,
                                 out int offsetValue)
        {
            lengthValue = objInfo;
            offsetValue = 1;

            if(objInfo == 0xF)
            {
                int int_type = bytes[offset + 1];
                int intType  = (int_type & 0xF0) >> 4;

                if(intType != 0x1)
                    Debug.WriteLine("BinaryPropertyListParser: Length integer has an unexpected type" + intType +
                                    ". Attempting to parse anyway...");

                int intInfo   = int_type & 0x0F;
                int intLength = 1 << intInfo;
                offsetValue = 2 + intLength;

                if(intLength < 3)
                    lengthValue = (int)ParseUnsignedInt(bytes.Slice(offset + 2, intLength));
                else
                {
                    // BigInteger is Little-Endian in .NET, swap the thing.
                    // Also BigInteger is of .NET 4.0, maybe there's a better way to do it.
                    byte[] bigEBigInteger = bytes.Slice(offset + 2, intLength).ToArray();
                    Array.Reverse(bigEBigInteger);
                    bigEBigInteger[bigEBigInteger.Length - 1] = 0x00; // Be sure to get unsigned BigInteger

                    lengthValue = (int)new BigInteger(bigEBigInteger);
                }
            }
        }

        /// <summary>Calculates the length in bytes of the UTF-8 string.</summary>
        /// <returns>The UTF-8 string length.</returns>
        /// <param name="bytes">Array containing the UTF-8 string.</param>
        /// <param name="offset">Offset in the array where the UTF-8 string resides.</param>
        /// <param name="numCharacters">How many UTF-8 characters are in the string.</param>
        int CalculateUtf8StringLength(ReadOnlySpan<byte> bytes, int offset, int numCharacters)
        {
            int length = 0;

            for(int i = 0; i < numCharacters; i++)
            {
                int tempOffset = offset + length;

                if(bytes.Length <= tempOffset)
                    return numCharacters;

                if(bytes[tempOffset] < 0x80)
                    length++;

                if(bytes[tempOffset] < 0xC2)
                    return numCharacters;

                if(bytes[tempOffset] < 0xE0)
                {
                    if((bytes[tempOffset + 1] & 0xC0) != 0x80)
                        return numCharacters;

                    length += 2;
                }
                else if(bytes[tempOffset] < 0xF0)
                {
                    if((bytes[tempOffset + 1] & 0xC0) != 0x80 ||
                       (bytes[tempOffset + 2] & 0xC0) != 0x80)
                        return numCharacters;

                    length += 3;
                }
                else if(bytes[tempOffset] < 0xF5)
                {
                    if((bytes[tempOffset + 1] & 0xC0) != 0x80 ||
                       (bytes[tempOffset + 2] & 0xC0) != 0x80 ||
                       (bytes[tempOffset + 3] & 0xC0) != 0x80)
                        return numCharacters;

                    length += 4;
                }
            }

            return length;
        }

        /// <summary>Parses an unsigned integer from a span.</summary>
        /// <returns>The byte array containing the unsigned integer.</returns>
        /// <param name="bytes">The unsigned integer represented by the given bytes.</param>
        public static long ParseUnsignedInt(ReadOnlySpan<byte> bytes)
        {
            if(bytes.Length <= 4)
                return ParseLong(bytes);

            return ParseLong(bytes) & 0xFFFFFFFFL;
        }

        /// <summary>Parses an unsigned integers from a byte array.</summary>
        /// <returns>The byte array containing the unsigned integer.</returns>
        /// <param name="bytes">The unsigned integer represented by the given bytes.</param>
        public static long ParseUnsignedInt(byte[] bytes) => ParseUnsignedInt(bytes.AsSpan());

        /// <summary>Parses a long from a (big-endian) byte array.</summary>
        /// <returns>The long integer represented by the given bytes.</returns>
        /// <param name="bytes">The bytes representing the long integer.</param>
        public static long ParseLong(ReadOnlySpan<byte> bytes)
        {
            if(bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if(bytes.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytes));
            }

            // https://opensource.apple.com/source/CF/CF-1153.18/CFBinaryPList.c,
            // __CFBinaryPlistCreateObjectFiltered, case kCFBinaryPlistMarkerInt:
            //
            // in format version '00', 1, 2, and 4-byte integers have to be interpreted as unsigned,
            // whereas 8-byte integers are signed (and 16-byte when available)
            // negative 1, 2, 4-byte integers are always emitted as 8 bytes in format '00'
            // integers are not required to be in the most compact possible representation,
            // but only the last 64 bits are significant currently
            switch(bytes.Length)
            {
                case 1: return bytes[0];

                case 2: return BinaryPrimitives.ReadUInt16BigEndian(bytes);

                case 4: return BinaryPrimitives.ReadUInt32BigEndian(bytes);

                // Transition from unsigned to signed
                case 8: return BinaryPrimitives.ReadInt64BigEndian(bytes);

                // Only the last 64 bits are significant currently
                case 16: return BinaryPrimitives.ReadInt64BigEndian(bytes.Slice(8));
            }

            if(bytes.Length >= 8)
                throw new ArgumentOutOfRangeException(nameof(bytes),
                                                      $"Cannot read a byte span of length {bytes.Length}");

            // Compatibility with existing archives, including anything with a non-power-of-2
            // size and 16-byte values, and architectures that don't support unaligned access
            long value = 0;

            for(int i = 0; i < bytes.Length; i++)
            {
                value = (value << 8) + bytes[i];
            }

            return value;

            // Theoretically we could handle non-power-of-2 byte arrays larger than 8, with the code
            // above, and it appears the reference implementation does exactly that. But it seems to
            // be an extreme edge case.
        }

        /// <summary>Parses a double from a (big-endian) byte array.</summary>
        /// <returns>The double represented by the given bytes.</returns>
        /// <param name="bytes">The bytes representing the double.</param>
        public static double ParseDouble(ReadOnlySpan<byte> bytes)
        {
            if(bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            return bytes.Length switch
            {
                8 => BitConverter.Int64BitsToDouble(ParseLong(bytes)),
                4 => BitConverter.ToSingle(BitConverter.GetBytes(ParseLong(bytes)), 0),
                _ => throw new ArgumentException("bad byte array length " + bytes.Length)
            };
        }

        /// <summary>Copies a part of a byte array into a new array.</summary>
        /// <returns>The copied array.</returns>
        /// <param name="src">The source array.</param>
        /// <param name="startIndex">The index from which to start copying.</param>
        /// <param name="endIndex">The index until which to copy.</param>
        public static byte[] CopyOfRange(ReadOnlySpan<byte> src, int startIndex, int endIndex)
        {
            int length = endIndex - startIndex;

            if(length < 0)
                throw new ArgumentOutOfRangeException("startIndex (" + startIndex + ")" + " > endIndex (" + endIndex +
                                                      ")");

            return src.Slice(startIndex, endIndex - startIndex).ToArray();
        }
    }
}