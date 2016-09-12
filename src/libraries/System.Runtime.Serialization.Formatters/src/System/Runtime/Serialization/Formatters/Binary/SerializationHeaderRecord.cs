// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Runtime.Serialization.Formatters.Binary
{
    // The Following classes read and write the binary records
    internal sealed class SerializationHeaderRecord : IStreamable
    {
        internal const int BinaryFormatterMajorVersion = 1;
        internal const int BinaryFormatterMinorVersion = 0;

        internal BinaryHeaderEnum _binaryHeaderEnum;
        internal int _topId;
        internal int _headerId;
        internal int _majorVersion;
        internal int _minorVersion;

        internal SerializationHeaderRecord() { }

        internal SerializationHeaderRecord(BinaryHeaderEnum binaryHeaderEnum, int topId, int headerId, int majorVersion, int minorVersion)
        {
            _binaryHeaderEnum = binaryHeaderEnum;
            _topId = topId;
            _headerId = headerId;
            _majorVersion = majorVersion;
            _minorVersion = minorVersion;
        }

        public void Write(BinaryFormatterWriter output)
        {
            _majorVersion = BinaryFormatterMajorVersion;
            _minorVersion = BinaryFormatterMinorVersion;
            output.WriteByte((byte)_binaryHeaderEnum);
            output.WriteInt32(_topId);
            output.WriteInt32(_headerId);
            output.WriteInt32(BinaryFormatterMajorVersion);
            output.WriteInt32(BinaryFormatterMinorVersion);
        }

        private static int GetInt32(byte[] buffer, int index) =>
            buffer[index] | buffer[index + 1] << 8 | buffer[index + 2] << 16 | buffer[index + 3] << 24;

        public void Read(BinaryParser input)
        {
            byte[] headerBytes = input.ReadBytes(17);

            // Throw if we couldnt read header bytes
            if (headerBytes.Length < 17)
            {
                throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
            }

            _majorVersion = GetInt32(headerBytes, 9);
            if (_majorVersion > BinaryFormatterMajorVersion)
            {
                throw new SerializationException(SR.Format(SR.Serialization_InvalidFormat, BitConverter.ToString(headerBytes)));
            }

            // binaryHeaderEnum has already been read
            _binaryHeaderEnum = (BinaryHeaderEnum)headerBytes[0];
            _topId = GetInt32(headerBytes, 1);
            _headerId = GetInt32(headerBytes, 5);
            _minorVersion = GetInt32(headerBytes, 13);
        }
    }
}
