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
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>An UID. Only found in binary property lists that are keyed archives.</summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class UID : NSObject
    {
        readonly ulong value;

        /// <summary>Initializes a new instance of the <see cref="Claunia.PropertyList.UID" /> class.</summary>
        /// <param name="bytes">Bytes.</param>
        public UID(ReadOnlySpan<byte> bytes)
        {
            if(bytes.Length != 1 &&
               bytes.Length != 2 &&
               bytes.Length != 4 &&
               bytes.Length != 8)
                throw new ArgumentException("Type argument is not valid.");

            value = (ulong)BinaryPropertyListParser.ParseLong(bytes);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Claunia.PropertyList.UID" /> class using an unsigned 8-bit
        ///     number.
        /// </summary>
        /// <param name="number">Unsigned 8-bit number.</param>
        public UID(byte number) => value = number;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Claunia.PropertyList.UID" /> class using an unsigned 16-bit
        ///     number.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="number">Unsigned 16-bit number.</param>
        public UID(ushort number) => value = number;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Claunia.PropertyList.UID" /> class using an unsigned 32-bit
        ///     number.
        /// </summary>
        /// <param name="number">Unsigned 32-bit number.</param>
        public UID(uint number) => value = number;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Claunia.PropertyList.UID" /> class using an unsigned 64-bit
        ///     number.
        /// </summary>
        /// <param name="number">Unsigned 64-bit number.</param>
        public UID(ulong number) => value = number;

        /// <summary>Gets the bytes.</summary>
        /// <value>The bytes.</value>
        public byte[] Bytes
        {
            get
            {
                byte[] bytes = new byte[ByteCount];
                GetBytes(bytes);

                return bytes;
            }
        }

        /// <summary>Gets the number of bytes required to represent this <see cref="UID" />.</summary>
        public int ByteCount => value switch
        {
            <= byte.MaxValue   => 1,
            <= ushort.MaxValue => 2,
            <= uint.MaxValue   => 4,
            _                  => 8
        };

        /// <summary>Writes the bytes required to represent this <see cref="UID" /> to a byte span.</summary>
        /// <param name="bytes">The byte span to which to write the byte representation of this UID.</param>
        public void GetBytes(Span<byte> bytes)
        {
            switch(ByteCount)
            {
                case 1:
                    bytes[0] = (byte)value;

                    break;

                case 2:
                    BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)value);

                    break;

                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)value);

                    break;

                case 8:
                    BinaryPrimitives.WriteUInt64BigEndian(bytes, value);

                    break;

                default: throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     UIDs are represented as dictionaries in XML property lists, where the key is always <c>CF$UID</c> and the
        ///     value is the integer representation of the UID.
        /// </summary>
        /// <param name="xml">The xml StringBuilder</param>
        /// <param name="level">The indentation level</param>
        internal override void ToXml(StringBuilder xml, int level)
        {
            Indent(xml, level);
            xml.Append("<dict>");
            xml.AppendLine();

            Indent(xml, level + 1);
            xml.Append("<key>CF$UID</key>");
            xml.AppendLine();

            Indent(xml, level + 1);
            xml.Append($"<integer>{value}</integer>");
            xml.AppendLine();

            Indent(xml, level);
            xml.Append("</dict>");
        }

        internal override void ToBinary(BinaryPropertyListWriter outPlist)
        {
            outPlist.Write(0x80 + ByteCount - 1);
            Span<byte> bytes = stackalloc byte[ByteCount];
            GetBytes(bytes);
            outPlist.Write(bytes);
        }

        internal override void ToASCII(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append("\"");
            Span<byte> bytes = stackalloc byte[ByteCount];
            GetBytes(bytes);

            foreach(byte b in bytes)
                ascii.Append($"{b:x2}");

            ascii.Append("\"");
        }

        internal override void ToASCIIGnuStep(StringBuilder ascii, int level) => ToASCII(ascii, level);

        /// <summary>
        ///     Determines whether the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.UID" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="Claunia.PropertyList.NSObject" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.UID" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.UID" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(NSObject obj) => Equals((object)obj);

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if(obj is not UID uid)
                return false;

            return uid.value == value;
        }

        /// <inheritdoc />
        public override int GetHashCode() => value.GetHashCode();

        /// <inheritdoc />
        public override string ToString() => $"{value} (UID)";

        /// <summary>Gets a <see cref="ulong" /> which represents this <see cref="UID" />.</summary>
        /// <returns>A <see cref="ulong" /> which represents this <see cref="UID" />.</returns>
        public ulong ToUInt64() => value;
    }
}