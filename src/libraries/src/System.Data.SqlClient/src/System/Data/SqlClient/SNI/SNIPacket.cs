// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.SqlClient.SNI
{
    internal sealed partial class SNIPacket
    {
        private int _dataLength; // the length of the data in the data segment, advanced by Append-ing data, does not include smux header length
        private int _dataCapacity; // the total capacity requested, if the array is rented this may be less than the _data.Length, does not include smux header length
        private int _dataOffset; // the start point of the data in the data segment, advanced by Take-ing data
        private int _headerLength; // the amount of space at the start of the array reserved for the smux header, this is zeroed in SetHeader
                                    // _headerOffset is not needed because it is always 0
        private byte[] _data;
        private SNIAsyncCallback _completionCallback;

        public SNIPacket(int headerSize, int dataSize)
        {
            Allocate(headerSize, dataSize);
        }

        /// <summary>
        /// Length of data left to process
        /// </summary>
        public int DataLeft => (_dataLength - _dataOffset);

        /// <summary>
        /// Length of data
        /// </summary>
        public int Length => _dataLength;

        /// <summary>
        /// Packet validity
        /// </summary>
        public bool IsInvalid => _data is null;

        public int ReservedHeaderSize => _headerLength;

        /// <summary>
        /// Set async completion callback
        /// </summary>
        /// <param name="completionCallback">Completion callback</param>
        public void SetCompletionCallback(SNIAsyncCallback completionCallback)
        {
            _completionCallback = completionCallback;
        }

        /// <summary>
        /// Invoke the completion callback 
        /// </summary>
        /// <param name="sniErrorCode">SNI error</param>
        public void InvokeCompletionCallback(uint sniErrorCode)
        {
            _completionCallback(this, sniErrorCode);
        }

        /// <summary>
        /// Allocate space for data
        /// </summary>
        /// <param name="dataLength">Length of byte array to be allocated</param>
        private void Allocate(int headerLength, int dataLength)
        {
            _data = ArrayPool<byte>.Shared.Rent(headerLength + dataLength);
            _dataCapacity = dataLength;
            _dataLength = 0;
            _dataOffset = 0;
            _headerLength = headerLength;
        }

        /// <summary>
        /// Read packet data into a buffer without removing it from the packet
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="dataSize">Number of bytes read from the packet into the buffer</param>
        public void GetData(byte[] buffer, ref int dataSize)
        {
            Buffer.BlockCopy(_data, _headerLength, buffer, 0, _dataLength); // read from 
            dataSize = _dataLength;
        }

        /// <summary>
        /// Take data from another packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="size">Data to take</param>
        /// <returns>Amount of data taken</returns>
        public int TakeData(SNIPacket packet, int size)
        {
            int dataSize = TakeData(packet._data, packet._headerLength + packet._dataLength, size);
            packet._dataLength += dataSize;
            return dataSize;
        }

        /// <summary>
        /// Append data
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="size">Size</param>
        public void AppendData(byte[] data, int size)
        {
            Buffer.BlockCopy(data, 0, _data, _headerLength + _dataLength, size);
            _dataLength += size;
        }

        /// <summary>
        /// Read data from the packet into the buffer at dataOffset for zize and then remove that data from the packet
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="dataOffset">Data offset to write data at</param>
        /// <param name="size">Number of bytes to read from the packet into the buffer</param>
        /// <returns></returns>
        public int TakeData(byte[] buffer, int dataOffset, int size)
        {
            if (_dataOffset >= _dataLength)
            {
                return 0;
            }

            if (_dataOffset + size > _dataLength)
            {
                size = _dataLength - _dataOffset;
            }

            Buffer.BlockCopy(_data, _headerLength + _dataOffset, buffer, dataOffset, size);
            _dataOffset += size;
            return size;
        }

        public Span<byte> GetHeaderBuffer(int headerSize)
        {
            Debug.Assert(_dataOffset == 0, "requested packet header buffer from partially consumed packet");
            Debug.Assert(headerSize > 0, "requested packet header buffer of 0 length");
            Debug.Assert(_headerLength == headerSize, "requested packet header of headerSize which is not equal to the _headerSize reservation");
            return _data.AsSpan(0, headerSize);
        }

        public void SetHeaderActive()
        {
            Debug.Assert(_headerLength > 0, "requested to set header active when it is not reserved or is already active");
            _dataCapacity += _headerLength;
            _dataLength += _headerLength;
            _headerLength = 0;
        }

        /// <summary>
        /// Release packet
        /// </summary>
        public void Release()
        {
            if (_data != null)
            {
                Array.Clear(_data, 0, _headerLength + _dataLength);
                ArrayPool<byte>.Shared.Return(_data, clearArray: false);
                _data = null;
                _dataCapacity = 0;
            }
            _dataLength = 0;
            _dataOffset = 0;
            _headerLength = 0;
            _completionCallback = null;
        }

        /// <summary>
        /// Read data from a stream synchronously
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        public void ReadFromStream(Stream stream)
        {
            _dataLength = stream.Read(_data, _headerLength, _dataCapacity);
        }

        /// <summary>
        /// Write data to a stream synchronously
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        public void WriteToStream(Stream stream)
        {
            stream.Write(_data, _headerLength, _dataLength);
        }

    }


}
