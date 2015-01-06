// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    internal class FakeSafeBuffer : SafeBuffer
    {
        public FakeSafeBuffer(ulong size)
            : base(true)
        {
            Initialize(size);
        }

        protected override bool ReleaseHandle()
        {
            return true;
        }
    }

    internal unsafe class TestSafeBuffer : SafeBuffer
    {
        private bool _isDisposed;

        public unsafe TestSafeBuffer(int capacity) : base(true)
        {
            Assert.True(capacity >= 0);
            Initialize((ulong)capacity);
            unsafe
            {
                IntPtr memory = Marshal.AllocHGlobal(capacity);
                SetHandle(memory);
                byte* bytes = (byte*)memory.ToPointer();
                byte* currentByte = bytes;
                for (int index = 0; index < capacity; index++)
                {
                    *currentByte = 0;
                }
            }
        }

        public unsafe TestSafeBuffer(byte[] seedData) : base(true)
        {
            int capacity = seedData.Length;
            Initialize((ulong)capacity);
            unsafe
            {
                IntPtr memory = Marshal.AllocHGlobal(capacity);
                SetHandle(memory);
                byte* bytes = (byte*)memory.ToPointer();
                byte* currentByte = bytes;
                for (int index = 0; index < capacity; index++)
                {
                    *currentByte = seedData[index];
                }
            }
        }

        public byte[] ToArray()
        {
            Assert.True(ByteLength <= int.MaxValue);
            int count = (int)this.ByteLength;
            var array = new byte[count];
            ReadArray(0, array, 0, count);
            return array;
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _isDisposed = true;
        }

        ~TestSafeBuffer()
        {
            Assert.True(_isDisposed); // please dispose the object
        }
        #endregion

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }

    public class UmsSafeBufferTests
    {
        [Fact]
        public static void WriteSafeBuffer()
        {
            var length = 1000;
            using (var buffer = new TestSafeBuffer(length)) { 
                var stream = new UnmanagedMemoryStream(buffer, 0, (long)buffer.ByteLength, FileAccess.Write);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                var copy = bytes.Copy();
                stream.Write(copy, 0, length);

                var memory = buffer.ToArray();

                Assert.True(ArrayHelpers.Comparer<byte>().Equals(bytes, memory));

                stream.Write(new byte[0], 0, 0);
            }
        }

        [Fact]
        public static void ReadWriteByteSafeBuffer()
        {
            var length = 1000;
            using (var buffer = new TestSafeBuffer(length))
            {
                var stream = new UnmanagedMemoryStream(buffer, 0, (long)buffer.ByteLength, FileAccess.ReadWrite);
                Assert.Equal(stream.Length, length);

                var bytes = ArrayHelpers.CreateByteArray(length);
                for(int index = 0; index < length; index++)
                {
                    stream.WriteByte(bytes[index]);
                }

                stream.Position = 0;
                for (int index = 0; index < length; index++)
                {
                    int value = stream.ReadByte();
                    Assert.Equal(bytes[index], (byte)value);
                }

                var memory = buffer.ToArray();
                Assert.True(ArrayHelpers.Comparer<byte>().Equals(bytes, memory));
            }
        }
    }
}
