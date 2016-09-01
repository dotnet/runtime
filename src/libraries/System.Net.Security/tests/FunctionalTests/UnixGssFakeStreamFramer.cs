// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security.Tests
{
    internal class UnixGssFakeStreamFramer
    {
        public const byte HandshakeDoneId = 20;
        public const byte HandshakeErrId = 21;
        public const byte DefaultMajorV = 1;
        public const byte DefaultMinorV = 0;

        private readonly Stream _innerStream;
        private readonly byte[] _header = new byte[5];
        private static readonly byte[] ErrorBuffer = new byte[] { 0, 0, 0, 0, 0x80, 0x09, 0x03, 0x0C }; // return LOGON_DENIED

        public UnixGssFakeStreamFramer(Stream innerStream)
        {
            _innerStream = innerStream;
        }

        public void WriteDataFrame(byte[] buffer, int offset, int count)
        {
            // data messages have the format of |pay-load-size|pay-load...|
            // where, pay-load-size = size of the payload as unsigned-int in little endian format
            // reference: https://msdn.microsoft.com/en-us/library/cc236740.aspx

            byte[] prefix = BitConverter.GetBytes(Convert.ToUInt32(count));
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(prefix);
            }

            _innerStream.Write(prefix, 0, prefix.Length);
            _innerStream.Write(buffer, offset, count);
        }

        public void WriteHandshakeFrame(byte[] buffer, int offset, int count)
        {
            WriteFrameHeader(count, isError:false);
            if (count > 0)
            {
                _innerStream.Write(buffer, offset, count);
            }
        }

        public void WriteHandshakeFrame(Interop.NetSecurityNative.GssApiException e)
        {
            WriteFrameHeader(ErrorBuffer.Length, isError:true);
            _innerStream.Write(ErrorBuffer, 0, ErrorBuffer.Length);
        }

        public byte[] ReadHandshakeFrame()
        {
            // A handshake header described at https://msdn.microsoft.com/en-us/library/cc236739.aspx
            // consists of 5 bytes:
            //   first byte is a message id (one of [HandshakeDoneId, HandshakeErrId, HandshakeInProgress])
            //   second byte is Major version of protocol (0x01)
            //   third byte is Minor version of protocol (0)
            //   fourth byte is the high order byte of the payload size (expressed as an unsigned number - ushort)
            //   fifth byte is the low order byte of the payload size (expressed as unsigned number - ushort)

            _innerStream.Read(_header, 0, _header.Length);
            byte[] inBuf = new byte[(_header[3] << 8) + _header[4]];
            _innerStream.Read(inBuf, 0, inBuf.Length);
            return inBuf;
        }

        public byte[] ReadDataFrame()
        {
            // data messages have the format of |pay-load-size|pay-load...|
            // where, pay-load-size = size of the payload as unsigned-int in little endian format
            // reference: https://msdn.microsoft.com/en-us/library/cc236740.aspx

            byte[] lenBytes = new byte[4];
            _innerStream.Read(lenBytes, 0, lenBytes.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lenBytes);
            }

            int length = Convert.ToInt32(BitConverter.ToUInt32(lenBytes, startIndex: 0));
            byte[] data = new byte[length];
            _innerStream.Read(data, 0, length);
            return data;
        }

        private void WriteFrameHeader(int count, bool isError)
        {
            // A handshake header described at https://msdn.microsoft.com/en-us/library/cc236739.aspx
            // consists of 5 bytes:
            //   first byte is a message id (one of [HandshakeDoneId, HandshakeErrId, HandshakeInProgress])
            //   second byte is Major version of protocol (0x01)
            //   third byte is Minor version of protocol (0)
            //   fourth byte is the high order byte of the payload size (expressed as unsigned number - ushort)
            //   fifth byte is the low order byte of the payload size (expressed as unsigned number - ushort)

            _header[0] = isError ? HandshakeErrId : HandshakeDoneId;
            _header[1] = DefaultMajorV;
            _header[2] = DefaultMinorV;
            _header[3] = (byte)((count >> 8) & 0xff);
            _header[4] = (byte)(count & 0xff);
            _innerStream.Write(_header, 0, _header.Length);
        }
    }
}
