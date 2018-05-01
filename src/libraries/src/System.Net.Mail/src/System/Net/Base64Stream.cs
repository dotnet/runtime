// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net
{
    internal sealed class Base64Stream : DelegatedStream, IEncodableStream
    {
        private static readonly byte[] s_base64DecodeMap = new byte[] {
            //0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1
            255,255,255,255,255,255,255,255,255,255,255, 62,255,255,255, 63, // 2
             52, 53, 54, 55, 56, 57, 58, 59, 60, 61,255,255,255,255,255,255, // 3
            255,  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, // 4
             15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25,255,255,255,255,255, // 5
            255, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, // 6
             41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51,255,255,255,255,255, // 7
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E
            255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        };

        private static readonly byte[] s_base64EncodeMap = new byte[] {
             65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
             81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 97, 98, 99,100,101,102,
            103,104,105,106,107,108,109,110,111,112,113,114,115,116,117,118,
            119,120,121,122, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 43, 47,
            61
        };

        private readonly int _lineLength;
        private readonly Base64WriteStateInfo _writeState;
        private ReadStateInfo _readState;

        //the number of bytes needed to encode three bytes (see algorithm description in Encode method below)
        private const int SizeOfBase64EncodedChar = 4;

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

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
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

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
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
                        WriteState.Append(s_base64EncodeMap[WriteState.LastBits], s_base64EncodeMap[64]);
                        break;
                    case 2:
                        WriteState.Append(s_base64EncodeMap[WriteState.LastBits], s_base64EncodeMap[64], s_base64EncodeMap[64]);
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

                    byte s = s_base64DecodeMap[*source];

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

            int cur = offset;
            switch (WriteState.Padding)
            {
                case 2:
                    WriteState.Append(s_base64EncodeMap[WriteState.LastBits | ((buffer[cur] & 0xf0) >> 4)]);
                    if (count == 1)
                    {
                        WriteState.LastBits = (byte)((buffer[cur] & 0x0f) << 2);
                        WriteState.Padding = 1;
                        cur++;
                        return cur - offset;
                    }
                    WriteState.Append(s_base64EncodeMap[((buffer[cur] & 0x0f) << 2) | ((buffer[cur + 1] & 0xc0) >> 6)]);
                    WriteState.Append(s_base64EncodeMap[(buffer[cur + 1] & 0x3f)]);
                    cur += 2;
                    count -= 2;
                    WriteState.Padding = 0;
                    break;
                case 1:
                    WriteState.Append(s_base64EncodeMap[WriteState.LastBits | ((buffer[cur] & 0xc0) >> 6)]);
                    WriteState.Append(s_base64EncodeMap[(buffer[cur] & 0x3f)]);
                    cur++;
                    count--;
                    WriteState.Padding = 0;
                    break;
            }

            int calcLength = cur + (count - (count % 3));

            // Convert three bytes at a time to base64 notation.  This will output 4 chars.
            for (; cur < calcLength; cur += 3)
            {
                if ((_lineLength != -1) && (WriteState.CurrentLineLength + SizeOfBase64EncodedChar + _writeState.FooterLength > _lineLength))
                {
                    WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
                }

                //how we actually encode: get three bytes in the
                //buffer to be encoded.  Then, extract six bits at a time and encode each six bit chunk as a base-64 character.
                //this means that three bytes of data will be encoded as four base64 characters.  It also means that to encode
                //a character, we must have three bytes to encode so if the number of bytes is not divisible by three, we 
                //must pad the buffer (this happens below)
                WriteState.Append(s_base64EncodeMap[(buffer[cur] & 0xfc) >> 2]);
                WriteState.Append(s_base64EncodeMap[((buffer[cur] & 0x03) << 4) | ((buffer[cur + 1] & 0xf0) >> 4)]);
                WriteState.Append(s_base64EncodeMap[((buffer[cur + 1] & 0x0f) << 2) | ((buffer[cur + 2] & 0xc0) >> 6)]);
                WriteState.Append(s_base64EncodeMap[(buffer[cur + 2] & 0x3f)]);
            }

            cur = calcLength; //Where we left off before

            // See if we need to fold before writing the last section (with possible padding)
            if ((count % 3 != 0) && (_lineLength != -1) && (WriteState.CurrentLineLength + SizeOfBase64EncodedChar + _writeState.FooterLength >= _lineLength))
            {
                WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
            }

            //now pad this thing if we need to.  Since it must be a number of bytes that is evenly divisble by 3, 
            //if there are extra bytes, pad with '=' until we have a number of bytes divisible by 3
            switch (count % 3)
            {
                case 2: //One character padding needed
                    WriteState.Append(s_base64EncodeMap[(buffer[cur] & 0xFC) >> 2]);
                    WriteState.Append(s_base64EncodeMap[((buffer[cur] & 0x03) << 4) | ((buffer[cur + 1] & 0xf0) >> 4)]);
                    if (dontDeferFinalBytes)
                    {
                        WriteState.Append(s_base64EncodeMap[((buffer[cur + 1] & 0x0f) << 2)]);
                        WriteState.Append(s_base64EncodeMap[64]);
                        WriteState.Padding = 0;
                    }
                    else
                    {
                        WriteState.LastBits = (byte)((buffer[cur + 1] & 0x0F) << 2);
                        WriteState.Padding = 1;
                    }
                    cur += 2;
                    break;

                case 1: // Two character padding needed
                    WriteState.Append(s_base64EncodeMap[(buffer[cur] & 0xFC) >> 2]);
                    if (dontDeferFinalBytes)
                    {
                        WriteState.Append(s_base64EncodeMap[(byte)((buffer[cur] & 0x03) << 4)]);
                        WriteState.Append(s_base64EncodeMap[64]);
                        WriteState.Append(s_base64EncodeMap[64]);
                        WriteState.Padding = 0;
                    }
                    else
                    {
                        WriteState.LastBits = (byte)((buffer[cur] & 0x03) << 4);
                        WriteState.Padding = 2;
                    }
                    cur++;
                    break;
            }

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();
            return cur - offset;
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

            for (;;)
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
            for (;;)
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

            internal ReadAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback)
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
                for (;;)
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
                    ReadAsyncResult thisPtr = (ReadAsyncResult)result.AsyncState;
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

            internal WriteAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state) : base(null, state, callback)
            {
                _parent = parent;
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            internal void Write()
            {
                for (;;)
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
                    WriteAsyncResult thisPtr = (WriteAsyncResult)result.AsyncState;
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
