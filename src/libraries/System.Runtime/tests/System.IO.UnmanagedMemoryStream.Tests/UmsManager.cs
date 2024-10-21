// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    // this class holds managed native memory used by UnmanagedMemoryStream
    internal class UmsManager : IDisposable
    {
        private readonly UnmanagedMemoryStream _stream;
        private IntPtr _memory;
        private bool _isDisposed;

        public unsafe UmsManager(FileAccess access, int capacity)
        {
            int memorySizeInBytes = capacity;
            _memory = Marshal.AllocHGlobal(memorySizeInBytes);
            byte* bytes = (byte*)_memory.ToPointer();
            byte* currentByte = bytes;
            for (int index = 0; index < memorySizeInBytes; index++)
            {
                *currentByte = 0;
                currentByte++;
            }
            _stream = new UnmanagedMemoryStream(bytes, memorySizeInBytes, memorySizeInBytes, access);
        }

        public unsafe UmsManager(FileAccess access, byte[] seedData)
        {
            int memorySizeInBytes = seedData.Length;
            _memory = Marshal.AllocHGlobal(memorySizeInBytes);
            byte* destination = (byte*)_memory.ToPointer();
            fixed (byte* source = seedData)
            {
                Buffer.MemoryCopy(source, destination, memorySizeInBytes, memorySizeInBytes);
            }
            _stream = new UnmanagedMemoryStream(destination, memorySizeInBytes, memorySizeInBytes, access);
        }

        public UnmanagedMemoryStream Stream
        {
            get
            {
                return _stream;
            }
        }

        public byte[] ToArray()
        {
            long count = _stream.Length;
            var array = new byte[count];
            unsafe
            {
                byte* source = (byte*)_memory.ToPointer();
                fixed (byte* destination = array)
                {
                    Buffer.MemoryCopy(source, destination, array.Length, count);
                }
            }

            return array;
        }

        #region IDisposable
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }
            Marshal.FreeHGlobal(_memory);
        }

        public void Dispose()
        {
            Dispose(true);
            _isDisposed = true;
        }

        ~UmsManager()
        {
            Assert.True(_isDisposed); // please dispose the context
        }
        #endregion
    }
}
