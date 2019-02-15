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
    /// <summary>
    /// SNI Packet
    /// </summary>
    internal sealed partial class SNIPacket
    {
        [Flags]
        private enum SNIPacketFlags : uint
        {
            None = 0,
            ArrayFromPool = 1,
            MuxHeaderReserved = 2,
            MuxHeaderWritten = 4,
        }

        private int _length; // the length of the data in the data segment, advanced by Append-ing data, does not include smux header length
        private int _capacity; // the total capacity requested, if the array is rented this may be less than the _data.Length, does not include smux header length
        private int _offset; // the start point of the data in the data segment, advanced by Take-ing data
        private int _header; // the amount of space at the start of the array reserved for the smux header, this is zeroed in SetHeader
        private SNIPacketFlags _flags;
        private byte[] _data;
        private SNIAsyncCallback _completionCallback;

        public SNIPacket(int capacity, bool reserveMuxHeader=false)
		{
            Allocate(capacity, reserveMuxHeader);
        }

        /// <summary>
        /// Length of data left to process
        /// </summary>
        public int DataLeft => (_length - _offset);

        /// <summary>
        /// Length of data
        /// </summary>
        public int Length => _length;

        /// <summary>
        /// Packet validity
        /// </summary>
        public bool IsInvalid => _data is null;

        public bool MuxHeaderReserved => ((_flags & SNIPacketFlags.MuxHeaderReserved) == SNIPacketFlags.MuxHeaderReserved);

        public bool MuxHeaderWritten => ((_flags & SNIPacketFlags.MuxHeaderWritten) == SNIPacketFlags.MuxHeaderWritten);

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
        /// <param name="capacity">Length of byte array to be allocated</param>
        public void Allocate(int capacity, bool reserveMuxHeader)
        {
            SNIPacketFlags flags = reserveMuxHeader ? SNIPacketFlags.MuxHeaderReserved : SNIPacketFlags.None;
            int headerCapacity = reserveMuxHeader ? SNISMUXHeader.HEADER_LENGTH : 0;
            int totalCapacity = headerCapacity + capacity;
            if (_data != null)
            {
                if (_data.Length < totalCapacity)
                {
                    Array.Clear(_data, 0, _header + _length);
                    if ((_flags & SNIPacketFlags.ArrayFromPool) == SNIPacketFlags.ArrayFromPool)
                    {
                        ArrayPool<byte>.Shared.Return(_data, clearArray: false);
                        _flags &= ~SNIPacketFlags.ArrayFromPool;
                    }
                    _data = null;
                }
                else
                {
                    // if the current array is big enough and rented keep it
                    flags |= (_flags & SNIPacketFlags.ArrayFromPool); 
                }
            }

            if (_data == null)
            {
                _data = ArrayPool<byte>.Shared.Rent(totalCapacity);
                flags |= SNIPacketFlags.ArrayFromPool; // set local not instance because it will be assigned after this block
            }

            _flags = flags;
            _capacity = capacity;
            _length = 0;
            _offset = 0;
            _header = headerCapacity;
        }

        /// <summary>
        /// Read packet data into a buffer without removing it from the packet
        /// </summary>
        /// <param name="buffer">Buffer</param>
        /// <param name="dataSize">Number of bytes read from the packet into the buffer</param>
        public void GetData(byte[] buffer, ref int dataSize)
        {
            Buffer.BlockCopy(_data, _header, buffer, 0, _length); // read from 
            dataSize = _length;
        }

        /// <summary>
        /// Take data from another packet
        /// </summary>
        /// <param name="packet">Packet</param>
        /// <param name="size">Data to take</param>
        /// <returns>Amount of data taken</returns>
        public int TakeData(SNIPacket packet, int size)
        {
            int dataSize = TakeData(packet._data, packet._header + packet._length, size);
            packet._length += dataSize;
            return dataSize;
        }

        /// <summary>
        /// Append data
        /// </summary>
        /// <param name="data">Data</param>
        /// <param name="size">Size</param>
        public void AppendData(byte[] data, int size)
        {
            Buffer.BlockCopy(data, 0, _data, _header + _length, size);
            _length += size;
        }

        public void AppendData(ReadOnlySpan<byte> data)
        {
            data.CopyTo(_data.AsSpan(_length));
            _length += data.Length;
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
            if (_offset >= _length)
            {
                return 0;
            }

            if (_offset + size > _length)
            {
                size = _length - _offset;
            }

            Buffer.BlockCopy(_data, (_header + _offset), buffer, dataOffset, size);
            _offset += size;
            return size;
        }


        /// <summary>
        /// Set the MARS SMUX header information for this packet
        /// </summary>
        /// <param name="header">the header to write into the packet</param>
        public void SetHeader(SNISMUXHeader header)
        {
            Debug.Assert(header != null, "writing null mux header to packet");

            Debug.Assert(_offset == 0, "writing mux header to partially read packet");
            Debug.Assert(_header == SNISMUXHeader.HEADER_LENGTH, "writing mux header to partially incorrectly sized reserved region");
            Debug.Assert(((_flags & SNIPacketFlags.MuxHeaderReserved) == SNIPacketFlags.MuxHeaderReserved), "writing mux heaser to non-mux packet");

            header.Write(_data.AsSpan(0, SNISMUXHeader.HEADER_LENGTH));
            _capacity += _header;
            _length += _header;
            _header = 0;
            _flags |= SNIPacketFlags.MuxHeaderWritten;
        }

        /// <summary>
        /// Release packet
        /// </summary>
        public void Release()
        {
            if (_data != null)
            {
                Array.Clear(_data, 0, _header + _length);
                if ((_flags & SNIPacketFlags.ArrayFromPool) == SNIPacketFlags.ArrayFromPool)
                {
                    ArrayPool<byte>.Shared.Return(_data, clearArray: false);
                    _flags &= ~SNIPacketFlags.ArrayFromPool;
                }
                _data = null;
                _capacity = 0;
            }
            _length = 0;
            _offset = 0;
            _header = 0;
            _flags = SNIPacketFlags.None;
            _completionCallback = null;
        }

        /// <summary>
        /// Read data from a stream synchronously
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        public void ReadFromStream(Stream stream)
        {
            _length = stream.Read(_data, _header, _capacity);
        }

        /// <summary>
        /// Write data to a stream synchronously
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        public void WriteToStream(Stream stream)
        {
            stream.Write(_data, _header, _length);
        }

    }


}
