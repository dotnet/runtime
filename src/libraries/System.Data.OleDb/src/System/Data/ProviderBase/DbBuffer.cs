// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.ProviderBase
{
    // DbBuffer is abstract to require derived class to exist
    // so that when debugging, we can tell the difference between one DbBuffer and another
    internal abstract class DbBuffer : SafeHandle
    {
        internal const int LMEM_FIXED = 0x0000;
        internal const int LMEM_MOVEABLE = 0x0002;
        internal const int LMEM_ZEROINIT = 0x0040;

        private readonly int _bufferLength;

        private DbBuffer(int initialSize, bool zeroBuffer) : base(IntPtr.Zero, true)
        {
            if (0 < initialSize)
            {
                int flags = ((zeroBuffer) ? LMEM_ZEROINIT : LMEM_FIXED);

                _bufferLength = initialSize;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    base.handle = SafeNativeMethods.LocalAlloc(flags, (IntPtr)initialSize);
                }
                if (IntPtr.Zero == base.handle)
                {
                    throw new OutOfMemoryException();
                }
            }
        }

        protected DbBuffer(int initialSize) : this(initialSize, true)
        {
        }

        protected DbBuffer(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
        {
        }

        private int BaseOffset { get { return 0; } }

        public override bool IsInvalid
        {
            get
            {
                return (IntPtr.Zero == base.handle);
            }
        }

        internal int Length
        {
            get
            {
                return _bufferLength;
            }
        }

        internal string PtrToStringUni(int offset, int length)
        {
            offset += BaseOffset;
            Validate(offset, 2 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");

            string? value = null;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                value = Marshal.PtrToStringUni(ptr, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal byte ReadByte(int offset)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 1);
            Debug.Assert(0 == offset % 4, "invalid alignment");

            byte value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                value = Marshal.ReadByte(ptr, offset);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal byte[] ReadBytes(int offset, int length)
        {
            byte[] value = new byte[length];
            return ReadBytes(offset, value, 0, length);
        }

        internal byte[] ReadBytes(int offset, byte[] destination, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != destination, "null destination");
            Debug.Assert(startIndex + length <= destination.Length, "destination too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(ptr, destination, startIndex, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return destination;
        }

        internal char ReadChar(int offset)
        {
            short value = ReadInt16(offset);
            return unchecked((char)value);
        }

        internal char[] ReadChars(int offset, char[] destination, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 2 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != destination, "null destination");
            Debug.Assert(startIndex + length <= destination.Length, "destination too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(ptr, destination, startIndex, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return destination;
        }

        internal double ReadDouble(int offset)
        {
            long value = ReadInt64(offset);
            return BitConverter.Int64BitsToDouble(value);
        }

        internal short ReadInt16(int offset)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 2);
            Debug.Assert(0 == offset % 2, "invalid alignment");

            short value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                value = Marshal.ReadInt16(ptr, offset);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal void ReadInt16Array(int offset, short[] destination, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 2 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != destination, "null destination");
            Debug.Assert(startIndex + length <= destination.Length, "destination too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(ptr, destination, startIndex, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal int ReadInt32(int offset)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 4);
            Debug.Assert(0 == offset % 4, "invalid alignment");

            int value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                value = Marshal.ReadInt32(ptr, offset);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal void ReadInt32Array(int offset, int[] destination, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 4 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != destination, "null destination");
            Debug.Assert(startIndex + length <= destination.Length, "destination too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(ptr, destination, startIndex, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal long ReadInt64(int offset)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 8);
            Debug.Assert(0 == offset % IntPtr.Size, "invalid alignment");

            long value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                value = Marshal.ReadInt64(ptr, offset);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal IntPtr ReadIntPtr(int offset)
        {
            offset += BaseOffset;
            ValidateCheck(offset, IntPtr.Size);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");

            IntPtr value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                value = Marshal.ReadIntPtr(ptr, offset);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        internal unsafe float ReadSingle(int offset)
        {
            int value = ReadInt32(offset);
            return *(float*)&value;
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                SafeNativeMethods.LocalFree(ptr);
            }
            return true;
        }

        private void StructureToPtr(int offset, object structure)
        {
            Debug.Assert(null != structure, "null structure");
            offset += BaseOffset;
            ValidateCheck(offset, Marshal.SizeOf(structure.GetType()));
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.StructureToPtr(structure, ptr, false/*fDeleteOld*/);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteByte(int offset, byte value)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 1);
            Debug.Assert(0 == offset % 4, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                Marshal.WriteByte(ptr, offset, value);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteBytes(int offset, byte[] source, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != source, "null source");
            Debug.Assert(startIndex + length <= source.Length, "source too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(source, startIndex, ptr, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteCharArray(int offset, char[] source, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 2 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != source, "null source");
            Debug.Assert(startIndex + length <= source.Length, "source too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(source, startIndex, ptr, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteDouble(int offset, double value)
        {
            WriteInt64(offset, BitConverter.DoubleToInt64Bits(value));
        }

        internal void WriteInt16(int offset, short value)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 2);
            Debug.Assert(0 == offset % 2, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                Marshal.WriteInt16(ptr, offset, value);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteInt16Array(int offset, short[] source, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 2 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != source, "null source");
            Debug.Assert(startIndex + length <= source.Length, "source too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(source, startIndex, ptr, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteInt32(int offset, int value)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 4);
            Debug.Assert(0 == offset % 4, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                Marshal.WriteInt32(ptr, offset, value);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteInt32Array(int offset, int[] source, int startIndex, int length)
        {
            offset += BaseOffset;
            Validate(offset, 4 * length);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            Debug.Assert(null != source, "null source");
            Debug.Assert(startIndex + length <= source.Length, "source too small");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                Marshal.Copy(source, startIndex, ptr, length);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteInt64(int offset, long value)
        {
            offset += BaseOffset;
            ValidateCheck(offset, 8);
            Debug.Assert(0 == offset % IntPtr.Size, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                Marshal.WriteInt64(ptr, offset, value);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void WriteIntPtr(int offset, IntPtr value)
        {
            offset += BaseOffset;
            ValidateCheck(offset, IntPtr.Size);
            Debug.Assert(0 == offset % IntPtr.Size, "invalid alignment");

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = DangerousGetHandle();
                Marshal.WriteIntPtr(ptr, offset, value);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal unsafe void WriteSingle(int offset, float value)
        {
            WriteInt32(offset, *(int*)&value);
        }

        internal Guid ReadGuid(int offset)
        {
            // faster than Marshal.PtrToStructure(offset, typeof(Guid))
            byte[] buffer = new byte[16];
            ReadBytes(offset, buffer, 0, 16);
            return new Guid(buffer);
        }
        internal void WriteGuid(int offset, Guid value)
        {
            // faster than Marshal.Copy(value.GetByteArray()
            StructureToPtr(offset, value);
        }

        internal DateTime ReadDate(int offset)
        {
            short[] buffer = new short[3];
            ReadInt16Array(offset, buffer, 0, 3);
            return new DateTime(
                unchecked((ushort)buffer[0]),   // Year
                unchecked((ushort)buffer[1]),   // Month
                unchecked((ushort)buffer[2]));  // Day
        }
        internal void WriteDate(int offset, DateTime value)
        {
            short[] buffer = new short[3] {
                unchecked((short)value.Year),
                unchecked((short)value.Month),
                unchecked((short)value.Day),
            };
            WriteInt16Array(offset, buffer, 0, 3);
        }

        internal TimeSpan ReadTime(int offset)
        {
            short[] buffer = new short[3];
            ReadInt16Array(offset, buffer, 0, 3);
            return new TimeSpan(
                unchecked((ushort)buffer[0]),   // Hours
                unchecked((ushort)buffer[1]),   // Minutes
                unchecked((ushort)buffer[2]));  // Seconds
        }
        internal void WriteTime(int offset, TimeSpan value)
        {
            short[] buffer = new short[3] {
                unchecked((short)value.Hours),
                unchecked((short)value.Minutes),
                unchecked((short)value.Seconds),
            };
            WriteInt16Array(offset, buffer, 0, 3);
        }

        internal DateTime ReadDateTime(int offset)
        {
            short[] buffer = new short[6];
            ReadInt16Array(offset, buffer, 0, 6);
            int ticks = ReadInt32(offset + 12);
            DateTime value = new DateTime(
                unchecked((ushort)buffer[0]),  // Year
                unchecked((ushort)buffer[1]),  // Month
                unchecked((ushort)buffer[2]),  // Day
                unchecked((ushort)buffer[3]),  // Hours
                unchecked((ushort)buffer[4]),  // Minutes
                unchecked((ushort)buffer[5])); // Seconds
            return value.AddTicks(ticks / 100);
        }
        internal void WriteDateTime(int offset, DateTime value)
        {
            int ticks = (int)(value.Ticks % 10000000L) * 100;
            short[] buffer = new short[6] {
                unchecked((short)value.Year),
                unchecked((short)value.Month),
                unchecked((short)value.Day),
                unchecked((short)value.Hour),
                unchecked((short)value.Minute),
                unchecked((short)value.Second),
            };
            WriteInt16Array(offset, buffer, 0, 6);
            WriteInt32(offset + 12, ticks);
        }

        internal decimal ReadNumeric(int offset)
        {
            byte[] bits = new byte[20];
            ReadBytes(offset, bits, 1, 19);

            int[] buffer = new int[4];
            buffer[3] = ((int)bits[2]) << 16; // scale
            if (0 == bits[3])
            {
                buffer[3] |= unchecked((int)0x80000000); //sign
            }
            buffer[0] = BitConverter.ToInt32(bits, 4);     // low
            buffer[1] = BitConverter.ToInt32(bits, 8);     // mid
            buffer[2] = BitConverter.ToInt32(bits, 12);     // high
            if (0 != BitConverter.ToInt32(bits, 16))
            {
                throw ADP.NumericToDecimalOverflow();
            }
            return new decimal(buffer);
        }

        internal void WriteNumeric(int offset, decimal value, byte precision)
        {
            int[] tmp = decimal.GetBits(value);
            byte[] buffer = new byte[20];

            buffer[1] = precision;
            Buffer.BlockCopy(tmp, 14, buffer, 2, 2); // copy sign and scale
            buffer[3] = (byte)((0 == buffer[3]) ? 1 : 0); // flip sign for native
            Buffer.BlockCopy(tmp, 0, buffer, 4, 12);
            buffer[16] = 0;
            buffer[17] = 0;
            buffer[18] = 0;
            buffer[19] = 0;
            WriteBytes(offset, buffer, 1, 19);
        }

        [Conditional("DEBUG")]
        protected void ValidateCheck(int offset, int count)
        {
            Validate(offset, count);
        }

        protected void Validate(int offset, int count)
        {
            if ((offset < 0) || (count < 0) || (Length < checked(offset + count)))
            {
                throw ADP.InternalError(ADP.InternalErrorCode.InvalidBuffer);
            }
        }
    }
}
