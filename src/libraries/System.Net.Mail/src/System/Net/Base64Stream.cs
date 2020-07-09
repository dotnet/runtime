// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net
{
    internal sealed class Base64Stream : DelegatedStream, IEncodableStream
    {
        private static ReadOnlySpan<byte> Base64DecodeMap => new byte[] // rely on C# compiler optimization to eliminate allocation
        {
            //0   1   2    3    4    5    6    7    8    9    A    B     C    D    E    F
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 0
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 1
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 62,   255, 255, 255,  63, // 2
             52,  53,  54,  55,  56,  57,  58,  59,  60,  61, 255, 255,  255, 255, 255, 255, // 3
            255,   0,   1,   2,   3,   4,   5,   6,   7,   8,   9,  10,   11,  12,  13,  14, // 4
             15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  255, 255, 255, 255, 255, // 5
            255,  26,  27,  28,  29,  30,  31,  32,  33,  34,  35,  36,   37,  38,  39,  40, // 6
             41,  42,  43,  44,  45,  46,  47,  48,  49,  50,  51, 255,  255, 255, 255, 255, // 7
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 8
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 9
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // A
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // B
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // C
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // D
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // E
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // F
        };

        private static ReadOnlySpan<byte> Base64EncodeMap => new byte[]
        {
             65,  66,  67,  68,   69, 70,  71,  72,  73,  74,  75,  76,  77,  78,   79,  80,
             81,  82,  83,  84,   85, 86,  87,  88,  89,  90,  97,  98,  99, 100,  101, 102,
            103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115,  116, 117, 118,
            119, 120, 121, 122,  48,  49,  50,  51,  52,  53,  54,  55,  56,  57,   43,  47,
            61
        };

        private readonly int _lineLength;
        private readonly Base64WriteStateInfo _writeState;
        private ReadStateInfo? _readState;

        //the number of bytes needed to encode three bytes (see algorithm description in Encode method below)
        private const int SizeOfBase64EncodedBlock = 4;

        //bytes with this value in the decode map are invalid
        private const byte InvalidBase64Value = 255;

        internal Base64Stream(Stream stream, Base64WriteStateInfo writeStateInfo) : base(stream)
        {
            _writeState = new Base64WriteStateInfo();
            _lineLength = writeStateInfo.MaxLineLength;
        }

        internal Base64Stream(Base64WriteStateInfo writeStateInfo) : base(new MemoryStream())
        {
            _lineLength = writeStateInfo.MaxLineLength;
            _writeState = writeStateInfo;
        }

        private ReadStateInfo ReadState => _readState ?? (_readState = new ReadStateInfo());

        internal Base64WriteStateInfo WriteState
        {
            get
            {
                Debug.Assert(_writeState != null, "_writeState was null");
                return _writeState;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var result = new ReadAsyncResult(this, buffer, offset, count, callback, state);
            result.Read();
            return result;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var result = new WriteAsyncResult(this, buffer, offset, count, callback, state);
            result.Write();
            return result;
        }


        public override void Close()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                switch (WriteState.Padding)
                {
                    case 1:
                        WriteState.Append(Base64EncodeMap[WriteState.LastBits], Base64EncodeMap[64]);
                        break;
                    case 2:
                        WriteState.Append(Base64EncodeMap[WriteState.LastBits], Base64EncodeMap[64], Base64EncodeMap[64]);
                        break;
                }
                WriteState.Padding = 0;
                FlushInternal();
            }

            base.Close();
        }

        public unsafe int DecodeBytes(byte[] buffer, int offset, int count)
        {
            fixed (byte* pBuffer = buffer)
            {
                byte* start = pBuffer + offset;
                byte* source = start;
                byte* dest = start;
                byte* end = start + count;

                while (source < end)
                {
                    //space and tab are ok because folding must include a whitespace char.
                    if (*source == '\r' || *source == '\n' || *source == '=' || *source == ' ' || *source == '\t')
                    {
                        source++;
                        continue;
                    }

                    byte s = Base64DecodeMap[*source];

                    if (s == InvalidBase64Value)
                    {
                        throw new FormatException(SR.MailBase64InvalidCharacter);
                    }

                    switch (ReadState.Pos)
                    {
                        case 0:
                            ReadState.Val = (byte)(s << 2);
                            ReadState.Pos++;
                            break;
                        case 1:
                            *dest++ = (byte)(ReadState.Val + (s >> 4));
                            ReadState.Val = unchecked((byte)(s << 4));
                            ReadState.Pos++;
                            break;
                        case 2:
                            *dest++ = (byte)(ReadState.Val + (s >> 2));
                            ReadState.Val = unchecked((byte)(s << 6));
                            ReadState.Pos++;
                            break;
                        case 3:
                            *dest++ = (byte)(ReadState.Val + s);
                            ReadState.Pos = 0;
                            break;
                    }
                    source++;
                }

                return (int)(dest - start);
            }
        }

        public int EncodeBytes(byte[] buffer, int offset, int count) =>
            EncodeBytes(buffer, offset, count, true, true);

        internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
        {
            Debug.Assert(buffer != null, "buffer was null");
            Debug.Assert(_writeState != null, "writestate was null");
            Debug.Assert(_writeState.Buffer != null, "writestate.buffer was null");

            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            if (_writeState.Encoding == null)
            {
                for (int i = 0; i < count; ++i)
                {
                    AppendEncodedCodepoint(buffer[offset + i], shouldAppendSpaceToCRLF);
                }
            }
            else
            {
                // unfortunately we need to transform bytes back to chars to obtain codepoints
                // TODO: find way not to transform bytes back and forth
                string value = _writeState.Encoding.GetString(buffer, offset, count);
                AppendEncodedString(value, _writeState.Encoding);
            }

            if (dontDeferFinalBytes)
            {
                AppendPadding();
            }

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();
            return count;
        }

        public int EncodeString(string value, Encoding encoding)
        {
            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            int bytesCount = AppendEncodedString(value, encoding);
            AppendPadding();

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();
            return bytesCount;
        }

        private int AppendEncodedString(string value, Encoding encoding)
        {
            int bytesCount = 0;
            for (int i = 0; i < value.Length; ++i)
            {
                byte[] bytes;
                if (!char.IsSurrogate(value[i]))
                {
                    bytes = encoding.GetBytes(value, i, 1);
                }
                else
                {
                    if (i + 1 < value.Length && char.IsSurrogatePair(value[i], value[i + 1]))
                    {
                        bytes = encoding.GetBytes(value, i, 2);
                        ++i; // transformed both chars, so shifting the index to account for that
                    }
                    else
                    {
                        // TODO: illegal state. throw exception?
                        // for now just encoding char as-is
                        bytes = encoding.GetBytes(value, i, 1);
                    }
                }
                AppendEncodedCodepoint(bytes, true);
                bytesCount += bytes.Length;
            }
            return bytesCount;
        }

        private int AppendEncodedCodepoint(byte[] bytes, bool shouldAppendSpaceToCRLF)
        {
            if (LineBreakNeeded(bytes.Length)) {
                AppendPadding();
                WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
            }
            for (int i = 0; i < bytes.Length; ++i)
            {
                ApppendEncodedByte(bytes[i]);
            }

            return bytes.Length;
        }

        private int AppendEncodedCodepoint(byte b, bool shouldAppendSpaceToCRLF)
        {
            if (LineBreakNeeded(1)) {
                AppendPadding();
                WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
            }
            ApppendEncodedByte(b);
            return 1;
        }

        private bool LineBreakNeeded(int numberOfBytesToAppend)
        {
            if (_lineLength == -1) {
                return false;
            }

            int bytesLeftInCurrentBlock = 0;
            int numberOfCharsLeftInCurrentBlock = 0;
            switch (WriteState.Padding)
            {
                case 2: // 1 byte was encoded from 3
                    bytesLeftInCurrentBlock = 2;
                    numberOfCharsLeftInCurrentBlock = 3;
                    break;
                case 1: // 2 bytes were encoded from 3
                    bytesLeftInCurrentBlock = 1;
                    numberOfCharsLeftInCurrentBlock = 2;
                    break;
            }

            int numberOfBytesInNewBlock = numberOfBytesToAppend - bytesLeftInCurrentBlock;
            if (numberOfBytesInNewBlock <= 0)
            {
                return false;
            }

            int numberOfBlocksToAppend = numberOfBytesInNewBlock / 3 + (numberOfBytesInNewBlock % 3 == 0 ? 0 : 1);
            int numberOfCharsToAppend = numberOfCharsLeftInCurrentBlock + numberOfBlocksToAppend * SizeOfBase64EncodedBlock;

            return WriteState.CurrentLineLength + numberOfCharsToAppend + _writeState.FooterLength > _lineLength;
        }

        private int ApppendEncodedByte(byte b)
        {
            switch (WriteState.Padding)
            {
                case 0: // add first byte of 3
                    WriteState.Append(Base64EncodeMap[(b & 0xfc) >> 2]);
                    WriteState.LastBits = (byte)((b & 0x03) << 4);
                    WriteState.Padding = 2;
                    return 1;
                case 2: // add second byte of 3
                    WriteState.Append(Base64EncodeMap[WriteState.LastBits | ((b & 0xf0) >> 4)]);
                    WriteState.LastBits = (byte)((b & 0x0f) << 2);
                    WriteState.Padding = 1;
                    return 1;
                case 1: // add third byte of 3
                    WriteState.Append(Base64EncodeMap[WriteState.LastBits | ((b & 0xc0) >> 6)]);
                    WriteState.Append(Base64EncodeMap[(b & 0x3f)]);
                    WriteState.Padding = 0;
                    return 1;
                default:
                    return 0; // TODO: illegal state. throw exception?
            }
        }

        private void AppendPadding()
        {
            switch (WriteState.Padding)
            {
                case 0: // no padding needed
                    break;
                case 2: // 2 character padding needed (1 byte was encoded instead of 3)
                    WriteState.Append(Base64EncodeMap[WriteState.LastBits]);
                    WriteState.Append(Base64EncodeMap[64]);
                    WriteState.Append(Base64EncodeMap[64]);
                    WriteState.Padding = 0;
                    break;
                case 1: // 1 character padding needed (2 bytes were encoded instead of 3)
                    WriteState.Append(Base64EncodeMap[WriteState.LastBits]);
                    WriteState.Append(Base64EncodeMap[64]);
                    WriteState.Padding = 0;
                    break;
                default:
                    break; // TODO: illegal state. throw exception?
            }
        }

        public string GetEncodedString() => Encoding.ASCII.GetString(WriteState.Buffer, 0, WriteState.Length);

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            return ReadAsyncResult.End(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            WriteAsyncResult.End(asyncResult);
        }

        public override void Flush()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                FlushInternal();
            }

            base.Flush();
        }

        private void FlushInternal()
        {
            base.Write(WriteState.Buffer, 0, WriteState.Length);
            WriteState.Reset();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            while (true)
            {
                // read data from the underlying stream
                int read = base.Read(buffer, offset, count);

                // if the underlying stream returns 0 then there
                // is no more data - ust return 0.
                if (read == 0)
                {
                    return 0;
                }

                // while decoding, we may end up not having
                // any bytes to return pending additional data
                // from the underlying stream.
                read = DecodeBytes(buffer, offset, read);
                if (read > 0)
                {
                    return read;
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int written = 0;

            // do not append a space when writing from a stream since this means
            // it's writing the email body
            while (true)
            {
                written += EncodeBytes(buffer, offset + written, count - written, false, false);
                if (written < count)
                {
                    FlushInternal();
                }
                else
                {
                    break;
                }
            }
        }

        private sealed class ReadAsyncResult : LazyAsyncResult
        {
            private readonly Base64Stream _parent;
            private readonly byte[] _buffer;
            private readonly int _offset;
            private readonly int _count;
            private int _read;

            private static readonly AsyncCallback s_onRead = OnRead;

            internal ReadAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) : base(null, state, callback)
            {
                _parent = parent;
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            private bool CompleteRead(IAsyncResult result)
            {
                _read = _parent.BaseStream.EndRead(result);

                // if the underlying stream returns 0 then there
                // is no more data - ust return 0.
                if (_read == 0)
                {
                    InvokeCallback();
                    return true;
                }

                // while decoding, we may end up not having
                // any bytes to return pending additional data
                // from the underlying stream.
                _read = _parent.DecodeBytes(_buffer, _offset, _read);
                if (_read > 0)
                {
                    InvokeCallback();
                    return true;
                }

                return false;
            }

            internal void Read()
            {
                while (true)
                {
                    IAsyncResult result = _parent.BaseStream.BeginRead(_buffer, _offset, _count, s_onRead, this);
                    if (!result.CompletedSynchronously || CompleteRead(result))
                    {
                        break;
                    }
                }
            }

            private static void OnRead(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    ReadAsyncResult thisPtr = (ReadAsyncResult)result.AsyncState!;
                    try
                    {
                        if (!thisPtr.CompleteRead(result))
                        {
                            thisPtr.Read();
                        }
                    }
                    catch (Exception e)
                    {
                        if (thisPtr.IsCompleted)
                        {
                            throw;
                        }
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            internal static int End(IAsyncResult result)
            {
                ReadAsyncResult thisPtr = (ReadAsyncResult)result;
                thisPtr.InternalWaitForCompletion();
                return thisPtr._read;
            }
        }

        private sealed class WriteAsyncResult : LazyAsyncResult
        {
            private static readonly AsyncCallback s_onWrite = OnWrite;

            private readonly Base64Stream _parent;
            private readonly byte[] _buffer;
            private readonly int _offset;
            private readonly int _count;
            private int _written;

            internal WriteAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) : base(null, state, callback)
            {
                _parent = parent;
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            internal void Write()
            {
                while (true)
                {
                    // do not append a space when writing from a stream since this means
                    // it's writing the email body
                    _written += _parent.EncodeBytes(_buffer, _offset + _written, _count - _written, false, false);
                    if (_written < _count)
                    {
                        IAsyncResult result = _parent.BaseStream.BeginWrite(_parent.WriteState.Buffer, 0, _parent.WriteState.Length, s_onWrite, this);
                        if (!result.CompletedSynchronously)
                        {
                            break;
                        }
                        CompleteWrite(result);
                    }
                    else
                    {
                        InvokeCallback();
                        break;
                    }
                }
            }

            private void CompleteWrite(IAsyncResult result)
            {
                _parent.BaseStream.EndWrite(result);
                _parent.WriteState.Reset();
            }

            private static void OnWrite(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    WriteAsyncResult thisPtr = (WriteAsyncResult)result.AsyncState!;
                    try
                    {
                        thisPtr.CompleteWrite(result);
                        thisPtr.Write();
                    }
                    catch (Exception e)
                    {
                        if (thisPtr.IsCompleted)
                        {
                            throw;
                        }
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            internal static void End(IAsyncResult result)
            {
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion();
                Debug.Assert(thisPtr._written == thisPtr._count);
            }
        }

        private sealed class ReadStateInfo
        {
            internal byte Val { get; set; }
            internal byte Pos { get; set; }
        }
    }
}
