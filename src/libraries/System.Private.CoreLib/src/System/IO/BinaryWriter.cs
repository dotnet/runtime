// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
    // This abstract base class represents a writer that can write
    // primitives to an arbitrary stream. A subclass can override methods to
    // give unique encodings.
    //
    public class BinaryWriter : IDisposable, IAsyncDisposable
    {
        private const int MaxArrayPoolRentalSize = 64 * 1024; // try to keep rentals to a reasonable size

        public static readonly BinaryWriter Null = new BinaryWriter();

        protected Stream OutStream;
        private readonly Encoding _encoding;
        private readonly bool _leaveOpen;
        private readonly bool _useFastUtf8;

        // Protected default constructor that sets the output stream
        // to a null stream (a bit bucket).
        protected BinaryWriter()
        {
            OutStream = Stream.Null;
            _encoding = Encoding.UTF8;
            _useFastUtf8 = true;
        }

        // BinaryWriter never emits a BOM, so can use Encoding.UTF8 fast singleton
        public BinaryWriter(Stream output) : this(output, Encoding.UTF8, false)
        {
        }

        public BinaryWriter(Stream output, Encoding encoding) : this(output, encoding, false)
        {
        }

        public BinaryWriter(Stream output!!, Encoding encoding!!, bool leaveOpen)
        {
            if (!output.CanWrite)
                throw new ArgumentException(SR.Argument_StreamNotWritable);

            OutStream = output;
            _encoding = encoding;
            _leaveOpen = leaveOpen;
            _useFastUtf8 = encoding.IsUTF8CodePage && encoding.EncoderFallback.MaxCharCount <= 1;
        }

        // Closes this writer and releases any system resources associated with the
        // writer. Following a call to Close, any operations on the writer
        // may raise exceptions.
        public virtual void Close()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveOpen)
                    OutStream.Flush();
                else
                    OutStream.Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual ValueTask DisposeAsync()
        {
            try
            {
                if (GetType() == typeof(BinaryWriter))
                {
                    if (_leaveOpen)
                    {
                        return new ValueTask(OutStream.FlushAsync());
                    }

                    OutStream.Close();
                }
                else
                {
                    // Since this is a derived BinaryWriter, delegate to whatever logic
                    // the derived implementation already has in Dispose.
                    Dispose();
                }

                return default;
            }
            catch (Exception exc)
            {
                return ValueTask.FromException(exc);
            }
        }

        // Returns the stream associated with the writer. It flushes all pending
        // writes before returning. All subclasses should override Flush to
        // ensure that all buffered data is sent to the stream.
        public virtual Stream BaseStream
        {
            get
            {
                Flush();
                return OutStream;
            }
        }

        // Clears all buffers for this writer and causes any buffered data to be
        // written to the underlying device.
        public virtual void Flush()
        {
            OutStream.Flush();
        }

        public virtual long Seek(int offset, SeekOrigin origin)
        {
            return OutStream.Seek(offset, origin);
        }

        // Writes a boolean to this stream. A single byte is written to the stream
        // with the value 0 representing false or the value 1 representing true.
        //
        public virtual void Write(bool value) => OutStream.WriteByte((byte)(value ? 1 : 0));

        // Writes a byte to this stream. The current position of the stream is
        // advanced by one.
        //
        public virtual void Write(byte value) => OutStream.WriteByte(value);

        // Writes a signed byte to this stream. The current position of the stream
        // is advanced by one.
        //
        [CLSCompliant(false)]
        public virtual void Write(sbyte value) => OutStream.WriteByte((byte)value);

        // Writes a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer!!)
        {
            OutStream.Write(buffer, 0, buffer.Length);
        }

        // Writes a section of a byte array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the byte array.
        //
        public virtual void Write(byte[] buffer, int index, int count)
        {
            OutStream.Write(buffer, index, count);
        }

        // Writes a character to this stream. The current position of the stream is
        // advanced by two.
        // Note this method cannot handle surrogates properly in UTF-8.
        //
        public virtual void Write(char ch)
        {
            if (!Rune.TryCreate(ch, out Rune rune)) // optimistically assume UTF-8 code path (which uses Rune) will be hit
            {
                throw new ArgumentException(SR.Arg_SurrogatesNotAllowedAsSingleChar);
            }

            Span<byte> buffer = stackalloc byte[8]; // reasonable guess for worst-case expansion for any arbitrary encoding

            if (_useFastUtf8)
            {
                int utf8ByteCount = rune.EncodeToUtf8(buffer);
                OutStream.Write(buffer.Slice(0, utf8ByteCount));
            }
            else
            {
                byte[]? rented = null;
                int maxByteCount = _encoding.GetMaxByteCount(1);

                if (maxByteCount > buffer.Length)
                {
                    rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                    buffer = rented;
                }

                int actualByteCount = _encoding.GetBytes(new ReadOnlySpan<char>(in ch), buffer);
                OutStream.Write(buffer.Slice(0, actualByteCount));

                if (rented != null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        // Writes a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars!!)
        {
            WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: false);
        }

        // Writes a section of a character array to this stream.
        //
        // This default implementation calls the Write(Object, int, int)
        // method to write the character array.
        //
        public virtual void Write(char[] chars!!, int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (index > chars.Length - count)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_IndexCount);

            WriteCharsCommonWithoutLengthPrefix(chars.AsSpan(index, count), useThisWriteOverride: false);
        }

        // Writes a double to this stream. The current position of the stream is
        // advanced by eight.
        //
        public virtual void Write(double value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        public virtual void Write(decimal value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(decimal)];
            decimal.GetBytes(value, buffer);
            OutStream.Write(buffer);
        }

        // Writes a two-byte signed integer to this stream. The current position of
        // the stream is advanced by two.
        //
        public virtual void Write(short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        //
        [CLSCompliant(false)]
        public virtual void Write(ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        //
        public virtual void Write(int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        //
        [CLSCompliant(false)]
        public virtual void Write(uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes an eight-byte signed integer to this stream. The current position
        // of the stream is advanced by eight.
        //
        public virtual void Write(long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes an eight-byte unsigned integer to this stream. The current
        // position of the stream is advanced by eight.
        //
        [CLSCompliant(false)]
        public virtual void Write(ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        //
        public virtual void Write(float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a half to this stream. The current position of the stream is
        // advanced by two.
        //
        public virtual void Write(Half value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort) /* = sizeof(Half) */];
            BinaryPrimitives.WriteHalfLittleEndian(buffer, value);
            OutStream.Write(buffer);
        }

        // Writes a length-prefixed string to this stream in the BinaryWriter's
        // current Encoding. This method first writes the length of the string as
        // an encoded unsigned integer with variable length, and then writes that many characters
        // to the stream.
        //
        public virtual void Write(string value!!)
        {
            // Common: UTF-8, small string, avoid 2-pass calculation
            // Less common: UTF-8, large string, avoid 2-pass calculation
            // Uncommon: excessively large string or not UTF-8

            if (_useFastUtf8)
            {
                if (value.Length <= 127 / 3)
                {
                    // Max expansion: each char -> 3 bytes, so 127 bytes max of data, +1 for length prefix
                    Span<byte> buffer = stackalloc byte[128];
                    int actualByteCount = _encoding.GetBytes(value, buffer.Slice(1));
                    buffer[0] = (byte)actualByteCount; // bypass call to Write7BitEncodedInt
                    OutStream.Write(buffer.Slice(0, actualByteCount + 1 /* length prefix */));
                    return;
                }
                else if (value.Length <= MaxArrayPoolRentalSize / 3)
                {
                    byte[] rented = ArrayPool<byte>.Shared.Rent(value.Length * 3); // max expansion: each char -> 3 bytes
                    int actualByteCount = _encoding.GetBytes(value, rented);
                    Write7BitEncodedInt(actualByteCount);
                    OutStream.Write(rented, 0, actualByteCount);
                    ArrayPool<byte>.Shared.Return(rented);
                    return;
                }
            }

            // Slow path: not fast UTF-8, or data is very large. We need to fall back
            // to a 2-pass mechanism so that we're not renting absurdly large arrays.

            int actualBytecount = _encoding.GetByteCount(value);
            Write7BitEncodedInt(actualBytecount);
            WriteCharsCommonWithoutLengthPrefix(value, useThisWriteOverride: false);
        }

        public virtual void Write(ReadOnlySpan<byte> buffer)
        {
            if (GetType() == typeof(BinaryWriter))
            {
                OutStream.Write(buffer);
            }
            else
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
                try
                {
                    buffer.CopyTo(array);
                    Write(array, 0, buffer.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(array);
                }
            }
        }

        public virtual void Write(ReadOnlySpan<char> chars)
        {
            // When Write(ROS<char>) was first introduced, it dispatched to the this.Write(byte[], ...)
            // virtual method rather than write directly to the output stream. We maintain that same
            // double-indirection for compat purposes.
            WriteCharsCommonWithoutLengthPrefix(chars, useThisWriteOverride: true);
        }

        private void WriteCharsCommonWithoutLengthPrefix(ReadOnlySpan<char> chars, bool useThisWriteOverride)
        {
            // If our input is truly enormous, the call to GetMaxByteCount might overflow,
            // which we want to avoid. Theoretically, any Encoding could expand from chars -> bytes
            // at an enormous ratio and cause us problems anyway given small inputs, but this is so
            // unrealistic that we needn't worry about it.

            byte[] rented;

            if (chars.Length <= MaxArrayPoolRentalSize)
            {
                // GetByteCount may walk the buffer contents, resulting in 2 passes over the data.
                // We prefer GetMaxByteCount because it's a constant-time operation.

                int maxByteCount = _encoding.GetMaxByteCount(chars.Length);
                if (maxByteCount <= MaxArrayPoolRentalSize)
                {
                    rented = ArrayPool<byte>.Shared.Rent(maxByteCount);
                    int actualByteCount = _encoding.GetBytes(chars, rented);
                    WriteToOutStream(rented, 0, actualByteCount, useThisWriteOverride);
                    ArrayPool<byte>.Shared.Return(rented);
                    return;
                }
            }

            // We're dealing with an enormous amount of data, so acquire an Encoder.
            // It should be rare that callers pass sufficiently large inputs to hit
            // this code path, and the cost of the operation is dominated by the transcoding
            // step anyway, so it's ok for us to take the allocation here.

            rented = ArrayPool<byte>.Shared.Rent(MaxArrayPoolRentalSize);
            Encoder encoder = _encoding.GetEncoder();
            bool completed;

            do
            {
                encoder.Convert(chars, rented, flush: true, out int charsConsumed, out int bytesWritten, out completed);
                if (bytesWritten != 0)
                {
                    WriteToOutStream(rented, 0, bytesWritten, useThisWriteOverride);
                }

                chars = chars.Slice(charsConsumed);
            } while (!completed);

            ArrayPool<byte>.Shared.Return(rented);

            void WriteToOutStream(byte[] buffer, int offset, int count, bool useThisWriteOverride)
            {
                if (useThisWriteOverride)
                {
                    Write(buffer, offset, count); // bounce through this.Write(...) overridden logic
                }
                else
                {
                    OutStream.Write(buffer, offset, count); // ignore this.Write(...) override, go straight to inner stream
                }
            }
        }

        public void Write7BitEncodedInt(int value)
        {
            uint uValue = (uint)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)(uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }

        public void Write7BitEncodedInt64(long value)
        {
            ulong uValue = (ulong)value;

            // Write out an int 7 bits at a time. The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            //
            // Using the constants 0x7F and ~0x7F below offers smaller
            // codegen than using the constant 0x80.

            while (uValue > 0x7Fu)
            {
                Write((byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            Write((byte)uValue);
        }
    }
}
