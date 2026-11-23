// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Xml
{
    internal sealed partial class XmlBufferReader
    {
        private readonly XmlDictionaryReader _reader;
        private Stream? _stream;
        private byte[]? _streamBuffer;
        private byte[] _buffer = null!; // initialized by SetBuffer
        private int _offsetMin;
        private int _offsetMax;
        private IXmlDictionary? _dictionary;
        private XmlBinaryReaderSession? _session;
        private int _offset;
        private const int maxBytesPerChar = 3;
        private char[]? _chars;
        private int _windowOffset;
        private int _windowOffsetMax;
        private bool _streamAtEnd;
        private ValueHandle? _listValue;
        private static readonly XmlBufferReader s_empty = new XmlBufferReader(Array.Empty<byte>());

        public XmlBufferReader(XmlDictionaryReader reader)
        {
            _reader = reader;
        }

        public XmlBufferReader(byte[] buffer)
        {
            _reader = null!; // this ctor is only used in 2 places internally, which will never touch _reader
            _buffer = buffer;
        }

        public static XmlBufferReader Empty
        {
            get
            {
                return s_empty;
            }
        }

        public byte[] Buffer
        {
            get
            {
                return _buffer;
            }
        }

        public bool IsStreamed
        {
            get
            {
                return _stream != null;
            }
        }


        public void SetBuffer(Stream stream, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            _streamBuffer ??= new byte[128];
            SetBuffer(stream, _streamBuffer, 0, 0, dictionary, session);
            _windowOffset = 0;
            _windowOffsetMax = _streamBuffer.Length;
        }

        public void SetBuffer(byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            SetBuffer(null, buffer, offset, count, dictionary, session);
        }

        private void SetBuffer(Stream? stream, byte[] buffer, int offset, int count, IXmlDictionary? dictionary, XmlBinaryReaderSession? session)
        {
            _stream = stream;
            _buffer = buffer;
            _offsetMin = offset;
            _offset = offset;
            _offsetMax = offset + count;
            _dictionary = dictionary;
            _session = session;
            _streamAtEnd = false;
        }

        public void Close()
        {
            CloseCoreSync();
        }

        internal ValueTask CloseAsync() => CloseCore<AsyncXmlStreamIO>();

        private async ValueTask CloseCore<TMode>()
            where TMode : struct, IXmlStreamIO
        {
            if (_streamBuffer != null && _streamBuffer.Length > 4096)
            {
                _streamBuffer = null;
            }
            if (_stream != null)
            {
                Stream stream = _stream;
                _stream = null;
                if (typeof(TMode) == typeof(SyncXmlStreamIO))
                    stream.Dispose();
                else
                    await stream.DisposeAsync().ConfigureAwait(false);
            }
            _buffer = Array.Empty<byte>();
            _offset = 0;
            _offsetMax = 0;
            _windowOffset = 0;
            _windowOffsetMax = 0;
            _dictionary = null;
            _session = null;
            _streamAtEnd = false;
        }

        public bool EndOfFile
        {
            get
            {
                return _offset == _offsetMax && !TryEnsureByte();
            }
        }

        internal bool BufferedEndOfFile => _offset == _offsetMax && (_stream is null || _streamAtEnd);

        public byte GetByte()
        {
            int offset = _offset;
            if (offset < _offsetMax)
                return _buffer[offset];
            else
                return GetByteHard();
        }

        internal ValueTask<byte> GetByte<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset < _offsetMax)
            {
                return new ValueTask<byte>(_buffer[_offset]);
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                return new ValueTask<byte>(GetByte());
            }

            return GetByteAsync(cancellationToken);
        }

        internal ValueTask<int> TryGetByte<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset < _offsetMax)
                return new ValueTask<int>(_buffer[_offset]);
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                GetBuffer(1, out int offset, out int offsetMax);
                return new ValueTask<int>(offset < offsetMax ? _buffer[offset] : -1);
            }

            return TryGetByteAsync(cancellationToken);
        }

        private async ValueTask<int> TryGetByteAsync(CancellationToken cancellationToken)
        {
            int offsetMax = await GetBufferAsync(1, cancellationToken).ConfigureAwait(false);
            return _offset < offsetMax ? _buffer[_offset] : -1;
        }

        private async ValueTask<byte> GetByteAsync(CancellationToken cancellationToken)
        {
            await EnsureBytesAsync(1, cancellationToken).ConfigureAwait(false);
            return _buffer[_offset];
        }

        internal ValueTask<bool> EndOfFileCore<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset < _offsetMax)
                return new ValueTask<bool>(false);
            return typeof(TMode) == typeof(SyncXmlStreamIO) ?
                new ValueTask<bool>(EndOfFile) : EndOfFileAsync(cancellationToken);
        }

        public void SkipByte()
        {
            Advance(1);
        }

        private byte GetByteHard()
        {
            EnsureByte();
            return _buffer[_offset];
        }

        public byte[] GetBuffer(int count, out int offset)
        {
            offset = _offset;
            if (offset <= _offsetMax - count)
                return _buffer;
            return GetBufferHard(count, out offset);
        }

        public byte[] GetBuffer(int count, out int offset, out int offsetMax)
        {
            offset = _offset;
            if (offset <= _offsetMax - count)
            {
                offsetMax = _offset + count;
            }
            else
            {
                TryEnsureBytes(Math.Min(count, _windowOffsetMax - offset));
                offsetMax = _offsetMax;
            }
            return _buffer;
        }

        internal ValueTask<int> GetBuffer<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                GetBuffer(count, out _, out int offsetMax);
                return new ValueTask<int>(offsetMax);
            }
            if (_offset <= _offsetMax - count)
            {
                return new ValueTask<int>(_offset + count);
            }

            return GetBufferAsync(count, cancellationToken);
        }

        private async ValueTask<int> GetBufferAsync(int count, CancellationToken cancellationToken)
        {
            await TryEnsureBytesAsync(Math.Min(count, _windowOffsetMax - _offset), cancellationToken).ConfigureAwait(false);
            return _offsetMax;
        }

        public byte[] GetBuffer(out int offset, out int offsetMax)
        {
            offset = _offset;
            offsetMax = _offsetMax;
            return _buffer;
        }

        private byte[] GetBufferHard(int count, out int offset)
        {
            offset = _offset;
            EnsureBytes(count);
            return _buffer;
        }

        private void EnsureByte()
        {
            if (!TryEnsureByte())
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
        }

        private bool TryEnsureByte()
        {
            if (_stream == null)
                return false;
            Debug.Assert(_offsetMax < _windowOffsetMax);
            if (_offsetMax >= _buffer.Length)
                return TryEnsureBytes(1);
            int b = _stream.ReadByte();
            if (b == -1)
            {
                _streamAtEnd = true;
                return false;
            }
            _buffer[_offsetMax++] = (byte)b;
            return true;
        }

        private void EnsureBytes(int count)
        {
            if (!TryEnsureBytes(count))
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
        }

        internal ValueTask EnsureBytes<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_stream is not null && count > _windowOffsetMax - _offset)
            {
                XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(_reader, _reader.Quotas.MaxBytesPerRead);
            }
            if (_offset <= _offsetMax - count)
            {
                return ValueTask.CompletedTask;
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                EnsureBytes(count);
                return ValueTask.CompletedTask;
            }

            return EnsureBytesAsync(count, cancellationToken);
        }

        private bool TryEnsureBytes(int count)
        {
            if (_offset <= _offsetMax - count)
            {
                return true;
            }
            return TryEnsureBytesCoreSync(count);
        }

        internal ValueTask<bool> TryEnsureBytesAsync(int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset <= _offsetMax - count)
            {
                return new ValueTask<bool>(true);
            }

            return TryEnsureBytesCore<AsyncXmlStreamIO>(count, cancellationToken);
        }

        internal async ValueTask EnsureBytesAsync(int count, CancellationToken cancellationToken)
        {
            if (!await TryEnsureBytesAsync(count, cancellationToken).ConfigureAwait(false))
            {
                XmlExceptionHelper.ThrowUnexpectedEndOfFile(_reader);
            }
        }

        internal async ValueTask<bool> EndOfFileAsync(CancellationToken cancellationToken)
        {
            return _offset == _offsetMax && !await TryEnsureBytesAsync(1, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask<bool> TryEnsureBytesCore<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if (_stream is null || _streamAtEnd)
                return false;

            Debug.Assert(_offset <= int.MaxValue - count);
            if (typeof(TMode) == typeof(AsyncXmlStreamIO) && count > _windowOffsetMax - _offset)
            {
                XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(_reader, _reader.Quotas.MaxBytesPerRead);
            }
            if (typeof(TMode) == typeof(AsyncXmlStreamIO) && _buffer.Length < Math.Min(1024, _reader.Quotas.MaxBytesPerRead))
            {
                byte[] newBuffer = new byte[Math.Min(1024, _reader.Quotas.MaxBytesPerRead)];
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _offsetMax);
                _buffer = newBuffer;
                _streamBuffer = newBuffer;
            }

            // The data could be coming from an untrusted source, so we use a standard
            // "multiply by 2" growth algorithm to avoid overly large memory utilization.
            // Constant value of 256 comes from MemoryStream implementation.

            do
            {
                int newOffsetMax = _offset + count;
                if (newOffsetMax <= _offsetMax)
                    return true;
                Debug.Assert(newOffsetMax <= _windowOffsetMax);
                if (newOffsetMax > _buffer.Length)
                {
                    byte[] newBuffer = new byte[Math.Max(256, _buffer.Length * 2)];
                    System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _offsetMax);
                    newOffsetMax = Math.Min(newOffsetMax, newBuffer.Length);
                    _buffer = newBuffer;
                    _streamBuffer = newBuffer;
                }
                int needed = newOffsetMax - _offsetMax;
                Debug.Assert(needed > 0);
                int read;
                if (typeof(TMode) == typeof(SyncXmlStreamIO))
                {
                    read = await TMode.ReadAtLeast(_stream, _buffer.AsMemory(_offsetMax, needed), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Read ahead only within the current quota window and already allocated buffer.
                    int available = Math.Min(_buffer.Length, _windowOffsetMax) - _offsetMax;
                    read = await TMode.Read(_stream, _buffer.AsMemory(_offsetMax, available), cancellationToken).ConfigureAwait(false);
                }
                _offsetMax += read;

                if (typeof(TMode) == typeof(SyncXmlStreamIO) ? read < needed : read == 0)
                {
                    _streamAtEnd = true;
                    return false;
                }
            } while (true);
        }

        public void Advance(int count)
        {
            Debug.Assert(_offset + count <= _offsetMax);
            _offset += count;
        }

        public void InsertBytes(ReadOnlySpan<byte> bytes)
        {
            Debug.Assert(_stream != null);
            int count = bytes.Length;
            if (_offsetMax > _buffer.Length - count)
            {
                byte[] newBuffer = new byte[_offsetMax + count];
                System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _offsetMax);
                _buffer = newBuffer;
                _streamBuffer = newBuffer;
            }
            System.Buffer.BlockCopy(_buffer, _offset, _buffer, _offset + count, _offsetMax - _offset);
            _offsetMax += count;
            bytes.CopyTo(_buffer.AsSpan(_offset, count));
        }

        public void SetWindow(int windowOffset, int windowLength, bool deferCompaction = false)
        {
            // [0...elementOffset-1][elementOffset..offset][offset..offsetMax-1][offsetMax..buffer.Length]
            // ^--Elements, Attributes in scope
            //                      ^-- The node just consumed
            //                                             ^--Data buffered, not consumed
            //                                                                  ^--Unused space
            // Ancestor names pin the buffer before windowOffset; compact relative to the reusable region.
            if (deferCompaction && _offset - windowOffset < (_buffer.Length - windowOffset) / 2)
                windowOffset = _offset;

            if (windowOffset > int.MaxValue - windowLength)
                windowLength = int.MaxValue - windowOffset;

            if (_offset != windowOffset)
            {
                System.Buffer.BlockCopy(_buffer, _offset, _buffer, windowOffset, _offsetMax - _offset);
                _offsetMax = windowOffset + (_offsetMax - _offset);
                _offset = windowOffset;
            }
            _windowOffset = windowOffset;
            _windowOffsetMax = Math.Max(windowOffset + windowLength, _offsetMax);
        }

        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                Debug.Assert(value >= _offsetMin && value <= _offsetMax);
                _offset = value;
            }
        }

        public int ReadBytes(int count)
        {
            Debug.Assert(count >= 0);
            int offset = _offset;
            if (offset > _offsetMax - count)
                EnsureBytes(count);
            _offset += count;
            return offset;
        }

        internal bool TryReadBytes(int count, out int offset)
        {
            Debug.Assert(count >= 0);
            offset = _offset;
            if (offset > _offsetMax - count)
                return false;
            _offset += count;
            return true;
        }

        internal bool TryReadName(out int offset, out int length)
        {
            offset = _offset;
            length = 0;
            if (offset < _offsetMax)
            {
                int count = _buffer[offset];
                if (count < 0x80 && count < _offsetMax - offset)
                {
                    offset++;
                    length = count;
                    _offset = offset + count;
                    return true;
                }
            }

            return false;
        }

        internal ValueTask<int> ReadBytes<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset <= _offsetMax - count)
            {
                int offset = _offset;
                Advance(count);
                return new ValueTask<int>(offset);
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                if (_stream is not null && count > _windowOffsetMax - _offset)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(_reader, _reader.Quotas.MaxBytesPerRead);
                return new ValueTask<int>(ReadBytes(count));
            }

            return ReadBytesCore<TMode>(count, cancellationToken);
        }

        private async ValueTask<int> ReadBytesCore<TMode>(int count, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await EnsureBytes<TMode>(count, cancellationToken).ConfigureAwait(false);
            int offset = _offset;
            Advance(count);
            return offset;
        }

        public int ReadMultiByteUInt31()
        {
            return ReadMultiByteUInt31Sync();
        }

        internal ValueTask<int> ReadMultiByteUInt31<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) == typeof(SyncXmlStreamIO) && GetByte() < 0x80)
            {
                return new ValueTask<int>(_buffer[_offset++]);
            }
            if (_offset < _offsetMax && _buffer[_offset] < 0x80)
            {
                return new ValueTask<int>(_buffer[_offset++]);
            }

            return ReadMultiByteUInt31Core<TMode>(cancellationToken);
        }

        private async ValueTask<int> ReadMultiByteUInt31Core<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int i = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            if ((i & 0x80) == 0)
                return i;
            i &= 0x7F;

            int j = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            i |= ((j & 0x7F) << 7);
            if ((j & 0x80) == 0)
                return i;

            int k = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            i |= ((k & 0x7F) << 14);
            if ((k & 0x80) == 0)
                return i;

            int l = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            i |= ((l & 0x7F) << 21);
            if ((l & 0x80) == 0)
                return i;

            int m = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            i |= (m << 28);
            if ((m & 0xF8) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);

            return i;
        }

        public int ReadUInt8()
        {
            byte b = GetByte();
            Advance(1);
            return b;
        }

        internal ValueTask<int> ReadUInt8<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
                return new ValueTask<int>(ReadUInt8());
            if (_offset < _offsetMax)
                return new ValueTask<int>(_buffer[_offset++]);

            return ReadUInt8Core<TMode>(cancellationToken);
        }

        private async ValueTask<int> ReadUInt8Core<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            byte value = await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
            Advance(1);
            return value;
        }

        internal ValueTask<int> ReadUInt16<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset <= _offsetMax - sizeof(ushort))
                return new ValueTask<int>(ReadUInt16());
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                if (_stream is not null && sizeof(ushort) > _windowOffsetMax - _offset)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(_reader, _reader.Quotas.MaxBytesPerRead);
                return new ValueTask<int>(ReadUInt16());
            }

            return ReadUInt16Core<TMode>(cancellationToken);
        }

        private async ValueTask<int> ReadUInt16Core<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await EnsureBytes<TMode>(sizeof(ushort), cancellationToken).ConfigureAwait(false);
            return ReadUInt16();
        }

        internal ValueTask<int> ReadUInt31<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_offset <= _offsetMax - sizeof(int))
                return new ValueTask<int>(ReadUInt31());
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                if (_stream is not null && sizeof(int) > _windowOffsetMax - _offset)
                    XmlExceptionHelper.ThrowMaxBytesPerReadExceeded(_reader, _reader.Quotas.MaxBytesPerRead);
                return new ValueTask<int>(ReadUInt31());
            }

            return ReadUInt31Core<TMode>(cancellationToken);
        }

        private async ValueTask<int> ReadUInt31Core<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            await EnsureBytes<TMode>(sizeof(int), cancellationToken).ConfigureAwait(false);
            return ReadUInt31();
        }

        public int ReadInt8()
            => (sbyte)ReadUInt8();

        public int ReadUInt16()
            => BitConverter.IsLittleEndian ? ReadRawBytes<ushort>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<ushort>());

        public int ReadInt16()
            => (short)ReadUInt16();

        public int ReadInt32()
            => BitConverter.IsLittleEndian ? ReadRawBytes<int>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<int>());

        public int ReadUInt31()
        {
            int i = ReadInt32();
            if (i < 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            return i;
        }

        public long ReadInt64()
            => BitConverter.IsLittleEndian ? ReadRawBytes<long>() : BinaryPrimitives.ReverseEndianness(ReadRawBytes<long>());

        public float ReadSingle()
        {
            float f = BinaryPrimitives.ReadSingleLittleEndian(GetBuffer(sizeof(float), out int offset).AsSpan(offset, sizeof(float)));
            Advance(sizeof(float));
            return f;
        }

        public double ReadDouble()
        {
            double d = BinaryPrimitives.ReadDoubleLittleEndian(GetBuffer(sizeof(double), out int offset).AsSpan(offset, sizeof(double)));
            Advance(sizeof(double));
            return d;
        }

        public decimal ReadDecimal()
        {
            if (BitConverter.IsLittleEndian)
            {
                return ReadRawBytes<decimal>();
            }
            else
            {
                byte[] buffer = GetBuffer(ValueHandleLength.Decimal, out int offset);
                ReadOnlySpan<byte> bytes = buffer.AsSpan(offset, sizeof(decimal));
                ReadOnlySpan<int> span =
                [
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(12, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4))
                ];

                Advance(ValueHandleLength.Decimal);
                return new decimal(span);
            }
        }

        public UniqueId ReadUniqueId()
        {
            int offset;
            byte[] buffer = GetBuffer(ValueHandleLength.UniqueId, out offset);
            UniqueId uniqueId = new UniqueId(buffer, offset);
            Advance(ValueHandleLength.UniqueId);
            return uniqueId;
        }

        public DateTime ReadDateTime()
        {
            long value = 0;
            try
            {
                value = ReadInt64();
                return DateTime.FromBinary(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "DateTime", exception);
            }
        }

        public TimeSpan ReadTimeSpan()
        {
            long value = 0;
            try
            {
                value = ReadInt64();
                return TimeSpan.FromTicks(value);
            }
            catch (ArgumentException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
            catch (FormatException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
            catch (OverflowException exception)
            {
                throw XmlExceptionHelper.CreateConversionException(value.ToString(CultureInfo.InvariantCulture), "TimeSpan", exception);
            }
        }

        public Guid ReadGuid()
        {
            int offset;
            _ = GetBuffer(ValueHandleLength.Guid, out offset);
            Guid guid = GetGuid(offset);
            Advance(ValueHandleLength.Guid);
            return guid;
        }

        public string ReadUTF8String(int length)
        {
            int offset;
            _ = GetBuffer(length, out offset);
            string value = GetString(offset, length);
            Advance(length);
            return value;
        }

        public void ReadRawArrayBytes<T>(Span<T> dst)
            where T : unmanaged
        {
            ReadRawArrayBytes(MemoryMarshal.AsBytes(dst));
        }

        public void ReadRawArrayBytes(Span<byte> dst)
        {
            if (dst.Length > 0)
            {
                GetBuffer(dst.Length, out _offset)
                    .AsSpan(_offset, dst.Length)
                    .CopyTo(dst);

                Advance(dst.Length);
            }
        }

        private char[] GetCharBuffer(int count)
        {
            if (count > 1024)
                return new char[count];
            if (_chars == null || _chars.Length < count)
                _chars = new char[count];
            return _chars;
        }

        private int GetChars(int offset, int length, char[] chars)
        {
            byte[] buffer = _buffer;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b >= 0x80)
                    return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, i);
                chars[i] = (char)b;
            }
            return length;
        }

        private int GetChars(int offset, int length, char[] chars, int charOffset)
        {
            byte[] buffer = _buffer;
            for (int i = 0; i < length; i++)
            {
                byte b = buffer[offset + i];
                if (b >= 0x80)
                    return i + XmlConverter.ToChars(buffer, offset + i, length - i, chars, charOffset + i);
                chars[charOffset + i] = (char)b;
            }
            return length;
        }

        public string GetString(int offset, int length)
        {
            return XmlConverter.ToString(_buffer, offset, length);
        }

        public string GetUnicodeString(int offset, int length)
        {
            return XmlConverter.ToStringUnicode(_buffer, offset, length);
        }

        public string GetString(int offset, int length, XmlNameTable nameTable)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetChars(offset, length, chars);
            return nameTable.Add(chars, 0, charCount);
        }

        public int GetEscapedChars(int offset, int length, char[] chars)
        {
            byte[] buffer = _buffer;
            int charCount = 0;
            int textOffset = offset;
            int offsetMax = offset + length;
            while (true)
            {
                while (offset < offsetMax && IsAttrChar(buffer[offset]))
                    offset++;
                charCount += GetChars(textOffset, offset - textOffset, chars, charCount);
                if (offset == offsetMax)
                    break;
                textOffset = offset;
                if (buffer[offset] == '&')
                {
                    while (offset < offsetMax && buffer[offset] != ';')
                        offset++;
                    offset++;
                    int ch = GetCharEntity(textOffset, offset - textOffset);
                    textOffset = offset;
                    if (ch > char.MaxValue)
                    {
                        SurrogateChar surrogate = new SurrogateChar(ch);
                        chars[charCount++] = surrogate.HighChar;
                        chars[charCount++] = surrogate.LowChar;
                    }
                    else
                    {
                        chars[charCount++] = (char)ch;
                    }
                }
                else if (buffer[offset] == '\n' || buffer[offset] == '\t')
                {
                    chars[charCount++] = ' ';
                    offset++;
                    textOffset = offset;
                }
                else // '\r'
                {
                    chars[charCount++] = ' ';
                    offset++;

                    if (offset < offsetMax && buffer[offset] == '\n')
                        offset++;

                    textOffset = offset;
                }
            }
            return charCount;
        }

        private static bool IsAttrChar(int ch)
        {
            switch (ch)
            {
                case '&':
                case '\r':
                case '\t':
                case '\n':
                    return false;

                default:
                    return true;
            }
        }

        public string GetEscapedString(int offset, int length)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetEscapedChars(offset, length, chars);
            return new string(chars, 0, charCount);
        }

        public string GetEscapedString(int offset, int length, XmlNameTable nameTable)
        {
            char[] chars = GetCharBuffer(length);
            int charCount = GetEscapedChars(offset, length, chars);
            return nameTable.Add(chars, 0, charCount);
        }

        private int GetLessThanCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 4 ||
                buffer[offset + 1] != (byte)'l' ||
                buffer[offset + 2] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'<';
        }

        private int GetGreaterThanCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 4 ||
                buffer[offset + 1] != (byte)'g' ||
                buffer[offset + 2] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'>';
        }

        private int GetQuoteCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 6 ||
                buffer[offset + 1] != (byte)'q' ||
                buffer[offset + 2] != (byte)'u' ||
                buffer[offset + 3] != (byte)'o' ||
                buffer[offset + 4] != (byte)'t')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'"';
        }

        private int GetAmpersandCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 5 ||
                buffer[offset + 1] != (byte)'a' ||
                buffer[offset + 2] != (byte)'m' ||
                buffer[offset + 3] != (byte)'p')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'&';
        }

        private int GetApostropheCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            if (length != 6 ||
                buffer[offset + 1] != (byte)'a' ||
                buffer[offset + 2] != (byte)'p' ||
                buffer[offset + 3] != (byte)'o' ||
                buffer[offset + 4] != (byte)'s')
            {
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return (int)'\'';
        }

        private int GetDecimalCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset + 0] == '&');
            Debug.Assert(buffer[offset + 1] == '#');
            Debug.Assert(buffer[offset + length - 1] == ';');
            int value = 0;
            for (int i = 2; i < length - 1; i++)
            {
                byte ch = buffer[offset + i];
                if (ch < (byte)'0' || ch > (byte)'9')
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                value = value * 10 + (ch - '0');
                if (value > SurrogateChar.MaxValue)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return value;
        }

        private int GetHexCharEntity(int offset, int length)
        {
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset + 0] == '&');
            Debug.Assert(buffer[offset + 1] == '#');
            Debug.Assert(buffer[offset + 2] == 'x');
            Debug.Assert(buffer[offset + length - 1] == ';');
            int value = 0;
            for (int i = 3; i < length - 1; i++)
            {
                byte ch = buffer[offset + i];
                int digit = HexConverter.FromChar(ch);
                if (digit == 0xFF)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                Debug.Assert(digit >= 0 && digit < 16);
                value = value * 16 + digit;
                if (value > SurrogateChar.MaxValue)
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            }
            return value;
        }

        public int GetCharEntity(int offset, int length)
        {
            if (length < 3)
                XmlExceptionHelper.ThrowInvalidCharRef(_reader);
            byte[] buffer = _buffer;
            Debug.Assert(buffer[offset] == '&');
            Debug.Assert(buffer[offset + length - 1] == ';');
            switch (buffer[offset + 1])
            {
                case (byte)'l':
                    return GetLessThanCharEntity(offset, length);
                case (byte)'g':
                    return GetGreaterThanCharEntity(offset, length);
                case (byte)'a':
                    if (buffer[offset + 2] == (byte)'m')
                        return GetAmpersandCharEntity(offset, length);
                    else
                        return GetApostropheCharEntity(offset, length);
                case (byte)'q':
                    return GetQuoteCharEntity(offset, length);
                case (byte)'#':
                    if (buffer[offset + 2] == (byte)'x')
                        return GetHexCharEntity(offset, length);
                    else
                        return GetDecimalCharEntity(offset, length);
                default:
                    XmlExceptionHelper.ThrowInvalidCharRef(_reader);
                    return 0;
            }
        }


        public bool IsWhitespaceKey(int key)
        {
            string s = GetDictionaryString(key).Value;
            return XmlConverter.IsWhitespace(s);
        }

        public bool IsWhitespaceUTF8(int offset, int length)
        {
            return XmlConverter.IsWhitespace(_buffer.AsSpan(offset, length));
        }

        public bool IsWhitespaceUnicode(int offset, int length)
        {
            for (int i = 0; i < length; i += sizeof(char))
            {
                char ch = (char)GetInt16(offset + i);
                if (!XmlConverter.IsWhitespace(ch))
                    return false;
            }
            return true;
        }

        public bool Equals2(int key1, int key2, XmlBufferReader bufferReader2)
        {
            // If the keys aren't from the same dictionary, they still might be the same
            if (key1 == key2)
                return true;
            else
                return GetDictionaryString(key1).Value == bufferReader2.GetDictionaryString(key2).Value;
        }

        public bool Equals2(int key1, XmlDictionaryString xmlString2)
        {
            if ((key1 & 1) == 0 && xmlString2.Dictionary == _dictionary)
                return xmlString2.Key == (key1 >> 1);
            else
                return GetDictionaryString(key1).Value == xmlString2.Value;
        }

        public bool Equals2(int offset1, int length1, byte[] buffer2)
        {
            if (length1 != buffer2.Length)
                return false;

            return _buffer.AsSpan(offset1, length1).SequenceEqual(buffer2);
        }

        public bool Equals2(int offset1, int length1, XmlBufferReader bufferReader2, int offset2, int length2)
        {
            if (length1 != length2)
                return false;

            return _buffer.AsSpan(offset1, length1).SequenceEqual(bufferReader2._buffer.AsSpan(offset2, length2));
        }

        public bool Equals2(int offset1, int length1, int offset2, int length2)
        {
            if (length1 != length2)
                return false;
            if (offset1 == offset2)
                return true;

            return _buffer.AsSpan(offset1, length1).SequenceEqual(_buffer.AsSpan(offset2, length2));
        }

        public unsafe bool Equals2(int offset1, int length1, string s2)
        {
            int byteLength = length1;
            int charLength = s2.Length;

            // N Unicode chars will be represented in at least N bytes, but
            // no more than N * 3 bytes.  If the byte count falls outside of this
            // range, then the strings cannot be equal.
            if (byteLength < charLength || byteLength > charLength * maxBytesPerChar)
                return false;

            byte[] buffer = _buffer;
            if (length1 < 8)
            {
                int length = Math.Min(byteLength, charLength);
                int offset = offset1;
                for (int i = 0; i < length; i++)
                {
                    byte b = buffer[offset + i];
                    if (b >= 0x80)
                        return XmlConverter.ToString(buffer, offset1, length1) == s2;
                    if (s2[i] != (char)b)
                        return false;
                }
                return byteLength == charLength;
            }
            else
            {
                int length = Math.Min(byteLength, charLength);
                fixed (byte* _pb = &buffer[offset1])
                {
                    byte* pb = _pb;
                    byte* pbMax = pb + length;
                    fixed (char* _pch = s2)
                    {
                        char* pch = _pch;
                        // Try to do the fast comparison in ASCII space
                        int t = 0;
                        while (pb < pbMax && *pb < 0x80)
                        {
                            t = *pb - (byte)*pch;
                            // The code generated is better if we break out then return
                            if (t != 0)
                                break;
                            pb++;
                            pch++;
                        }
                        if (t != 0)
                            return false;
                        if (pb == pbMax)
                            return (byteLength == charLength);
                    }
                }
                return XmlConverter.ToString(buffer, offset1, length1) == s2;
            }
        }

        public int Compare(int offset1, int length1, int offset2, int length2)
        {
            byte[] buffer = _buffer;
            int length = Math.Min(length1, length2);
            for (int i = 0; i < length; i++)
            {
                int s = buffer[offset1 + i] - buffer[offset2 + i];
                if (s != 0)
                    return s;
            }
            return length1 - length2;
        }

        public byte GetByte(int offset)
        {
            return _buffer[offset];
        }

        public int GetInt8(int offset)
        {
            return (sbyte)GetByte(offset);
        }

        private unsafe T ReadRawBytes<T>() where T : unmanaged
        {
            ReadOnlySpan<byte> buffer = GetBuffer(sizeof(T), out int offset)
                .AsSpan(offset, sizeof(T));
            T value = MemoryMarshal.Read<T>(buffer);

            Advance(sizeof(T));
            return value;
        }

        private unsafe T ReadRawBytes<T>(int offset) where T : unmanaged
            => MemoryMarshal.Read<T>(_buffer.AsSpan(offset, sizeof(T)));

        public int GetInt16(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<short>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<short>(offset));

        public int GetInt32(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<int>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<int>(offset));

        public long GetInt64(int offset)
            => BitConverter.IsLittleEndian ? ReadRawBytes<long>(offset) : BinaryPrimitives.ReverseEndianness(ReadRawBytes<long>(offset));

        public ulong GetUInt64(int offset)
            => (ulong)GetInt64(offset);

        public float GetSingle(int offset)
            => BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(offset, sizeof(float)));

        public double GetDouble(int offset)
            => BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(offset, sizeof(double)));

        public decimal GetDecimal(int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return ReadRawBytes<decimal>(offset);
            }
            else
            {
                ReadOnlySpan<byte> bytes = _buffer.AsSpan(offset, sizeof(decimal));
                ReadOnlySpan<int> span =
                [
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(8, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(12, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(4, 4)),
                    BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(0, 4))
                ];

                return new decimal(span);
            }
        }

        public UniqueId GetUniqueId(int offset)
            => new UniqueId(_buffer, offset);

        public Guid GetGuid(int offset)
            => new Guid(_buffer.AsSpan(offset, ValueHandleLength.Guid));

        public void GetBase64(int srcOffset, byte[] buffer, int dstOffset, int count)
        {
            System.Buffer.BlockCopy(_buffer, srcOffset, buffer, dstOffset, count);
        }


        public XmlBinaryNodeType GetNodeType()
        {
            return (XmlBinaryNodeType)GetByte();
        }

        public void SkipNodeType()
        {
            SkipByte();
        }

        public object[] GetList(int offset, int count)
        {
            int bufferOffset = this.Offset;
            this.Offset = offset;
            try
            {
                object[] objects = new object[count];
                for (int i = 0; i < count; i++)
                {
                    XmlBinaryNodeType nodeType = GetNodeType();
                    SkipNodeType();
                    Debug.Assert(nodeType != XmlBinaryNodeType.StartListText);
                    ReadValue(nodeType, _listValue!);
                    objects[i] = _listValue!.ToObject();
                }
                return objects;
            }
            finally
            {
                this.Offset = bufferOffset;
            }
        }


        public XmlDictionaryString GetDictionaryString(int key)
        {
            IXmlDictionary keyDictionary;
            if ((key & 1) != 0)
            {
                keyDictionary = _session!;
            }
            else
            {
                keyDictionary = _dictionary!;
            }

            XmlDictionaryString? s;
            if (!keyDictionary.TryLookup(key >> 1, out s))
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);

            return s;
        }

        public int ReadDictionaryKey()
        {
            return ReadDictionaryKeySync();
        }

        internal async ValueTask<int> ReadDictionaryKey<TMode>(CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int key = await ReadMultiByteUInt31<TMode>(cancellationToken).ConfigureAwait(false);
            if ((key & 1) != 0)
            {
                if (_session == null)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                int sessionKey = (key >> 1);
                if (!_session.TryLookup(sessionKey, out _))
                {
                    if (sessionKey < XmlDictionaryString.MinKey || sessionKey > XmlDictionaryString.MaxKey)
                        XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
                    XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedSession(_reader, sessionKey);
                }
            }
            else
            {
                if (_dictionary == null)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                int staticKey = (key >> 1);
                if (!_dictionary.TryLookup(staticKey, out _))
                {
                    if (staticKey < XmlDictionaryString.MinKey || staticKey > XmlDictionaryString.MaxKey)
                        XmlExceptionHelper.ThrowXmlDictionaryStringIDOutOfRange(_reader);
                    XmlExceptionHelper.ThrowXmlDictionaryStringIDUndefinedStatic(_reader, staticKey);
                }
            }

            return key;
        }

        public void ReadValue(XmlBinaryNodeType nodeType, ValueHandle value)
        {
            ReadValueSync(nodeType, value);
        }

        internal async ValueTask ReadValue<TMode>(XmlBinaryNodeType nodeType, ValueHandle value, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            switch (nodeType)
            {
                case XmlBinaryNodeType.EmptyText:
                    value.SetValue(ValueHandleType.Empty);
                    break;
                case XmlBinaryNodeType.ZeroText:
                    value.SetValue(ValueHandleType.Zero);
                    break;
                case XmlBinaryNodeType.OneText:
                    value.SetValue(ValueHandleType.One);
                    break;
                case XmlBinaryNodeType.TrueText:
                    value.SetValue(ValueHandleType.True);
                    break;
                case XmlBinaryNodeType.FalseText:
                    value.SetValue(ValueHandleType.False);
                    break;
                case XmlBinaryNodeType.BoolText:
                    value.SetValue(await ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false) != 0 ? ValueHandleType.True : ValueHandleType.False);
                    break;
                case XmlBinaryNodeType.Chars8Text:
                    await ReadValue<TMode>(value, ValueHandleType.UTF8, await ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Chars16Text:
                    await ReadValue<TMode>(value, ValueHandleType.UTF8, await ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Chars32Text:
                    await ReadValue<TMode>(value, ValueHandleType.UTF8, await ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.UnicodeChars8Text:
                    await ReadUnicodeValue<TMode>(value, await ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.UnicodeChars16Text:
                    await ReadUnicodeValue<TMode>(value, await ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.UnicodeChars32Text:
                    await ReadUnicodeValue<TMode>(value, await ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Bytes8Text:
                    await ReadValue<TMode>(value, ValueHandleType.Base64, await ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Bytes16Text:
                    await ReadValue<TMode>(value, ValueHandleType.Base64, await ReadUInt16<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Bytes32Text:
                    await ReadValue<TMode>(value, ValueHandleType.Base64, await ReadUInt31<TMode>(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.DictionaryText:
                    value.SetDictionaryValue(await ReadDictionaryKey<TMode>(cancellationToken).ConfigureAwait(false));
                    break;
                case XmlBinaryNodeType.UniqueIdText:
                    await ReadValue<TMode>(value, ValueHandleType.UniqueId, ValueHandleLength.UniqueId, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.GuidText:
                    await ReadValue<TMode>(value, ValueHandleType.Guid, ValueHandleLength.Guid, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.DecimalText:
                    await ReadValue<TMode>(value, ValueHandleType.Decimal, ValueHandleLength.Decimal, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Int8Text:
                    await ReadValue<TMode>(value, ValueHandleType.Int8, ValueHandleLength.Int8, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Int16Text:
                    await ReadValue<TMode>(value, ValueHandleType.Int16, ValueHandleLength.Int16, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Int32Text:
                    await ReadValue<TMode>(value, ValueHandleType.Int32, ValueHandleLength.Int32, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.Int64Text:
                    await ReadValue<TMode>(value, ValueHandleType.Int64, ValueHandleLength.Int64, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.UInt64Text:
                    await ReadValue<TMode>(value, ValueHandleType.UInt64, ValueHandleLength.UInt64, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.FloatText:
                    await ReadValue<TMode>(value, ValueHandleType.Single, ValueHandleLength.Single, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.DoubleText:
                    await ReadValue<TMode>(value, ValueHandleType.Double, ValueHandleLength.Double, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.TimeSpanText:
                    await ReadValue<TMode>(value, ValueHandleType.TimeSpan, ValueHandleLength.TimeSpan, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.DateTimeText:
                    await ReadValue<TMode>(value, ValueHandleType.DateTime, ValueHandleLength.DateTime, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.StartListText:
                    await ReadList<TMode>(value, cancellationToken).ConfigureAwait(false);
                    break;
                case XmlBinaryNodeType.QNameDictionaryText:
                    await ReadQName<TMode>(value, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                    break;
            }
        }

        private ValueTask ReadValue<TMode>(ValueHandle value, ValueHandleType type, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (TryReadBytes(length, out int offset))
            {
                value.SetValue(type, offset, length);
                return ValueTask.CompletedTask;
            }
            if (typeof(TMode) == typeof(SyncXmlStreamIO))
            {
                offset = ReflectionSerialization.Complete(ReadBytes<TMode>(length, cancellationToken));
                value.SetValue(type, offset, length);
                return ValueTask.CompletedTask;
            }

            return ReadValueCore<TMode>(value, type, length, cancellationToken);
        }

        private async ValueTask ReadValueCore<TMode>(ValueHandle value, ValueHandleType type, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int offset = await ReadBytes<TMode>(length, cancellationToken).ConfigureAwait(false);
            value.SetValue(type, offset, length);
        }

        private async ValueTask ReadUnicodeValue<TMode>(ValueHandle value, int length, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            if ((length & 1) != 0)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            await ReadValue<TMode>(value, ValueHandleType.Unicode, length, cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask ReadList<TMode>(ValueHandle value, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            _listValue ??= new ValueHandle(this);
            int count = 0;
            int offset = this.Offset;
            while (true)
            {
                XmlBinaryNodeType nodeType = (XmlBinaryNodeType)await GetByte<TMode>(cancellationToken).ConfigureAwait(false);
                SkipNodeType();
                if (nodeType == XmlBinaryNodeType.StartListText)
                    XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
                if (nodeType == XmlBinaryNodeType.EndListText)
                    break;
                await ReadValue<TMode>(nodeType, _listValue, cancellationToken).ConfigureAwait(false);
                count++;
            }
            value.SetValue(ValueHandleType.List, offset, count);
        }


        public void ReadQName(ValueHandle value)
        {
            ReadQNameSync(value);
        }

        internal async ValueTask ReadQName<TMode>(ValueHandle value, CancellationToken cancellationToken)
            where TMode : struct, IXmlStreamIO
        {
            int prefix = await ReadUInt8<TMode>(cancellationToken).ConfigureAwait(false);
            if (prefix >= 26)
                XmlExceptionHelper.ThrowInvalidBinaryFormat(_reader);
            int key = await ReadDictionaryKey<TMode>(cancellationToken).ConfigureAwait(false);
            value.SetQNameValue(prefix, key);
        }

        public int[] GetRows()
        {
            if (_buffer == null)
            {
                return new int[1] { 0 };
            }

            List<int> list = new List<int>();
            list.Add(_offsetMin);
            for (int i = _offsetMin; i < _offsetMax; i++)
            {
                if (_buffer[i] == (byte)13 || _buffer[i] == (byte)10)
                {
                    if (i + 1 < _offsetMax && _buffer[i + 1] == (byte)10)
                        i++;
                    list.Add(i + 1);
                }
            }
            return list.ToArray();
        }
    }
}
