// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    // this class holds managed native memory used by UnmanagedMemoryStream
    internal class UmsManager : IDisposable
    {
        private UnmanagedMemoryStream _stream;
        private IntPtr _memory;
        private int _memorySizeInBytes;
        private bool _isDisposed;

        public unsafe UmsManager(FileAccess access, int capacity)
        {
            _memorySizeInBytes = capacity;
            unsafe
            {
                _memory = Marshal.AllocHGlobal(_memorySizeInBytes);
                byte* bytes = (byte*)_memory.ToPointer();
                byte* currentByte = bytes;
                for (int index = 0; index < _memorySizeInBytes; index++)
                {
                    *currentByte = 0;
                    currentByte++;
                }
                _stream = new UnmanagedMemoryStream(bytes, _memorySizeInBytes, _memorySizeInBytes, access);
            }
        }

        public unsafe UmsManager(FileAccess access, byte[] seedData)
        {
            _memorySizeInBytes = seedData.Length;
            unsafe
            {
                _memory = Marshal.AllocHGlobal(_memorySizeInBytes);
                byte* bytes = (byte*)_memory.ToPointer();
                byte* currentByte = bytes;
                for (int index = 0; index < _memorySizeInBytes; index++)
                {
                    *currentByte = seedData[index];
                }
                _stream = new UnmanagedMemoryStream(bytes, _memorySizeInBytes, _memorySizeInBytes, access);
            }
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
                fixed (byte* parray = array)
                {
                    byte* source = (byte*)_memory.ToPointer();
                    byte* destination = parray;
                    for (int i = 0; i < count; i++)
                    {
                        *destination = *source;
                        destination++;
                        source++;
                    }
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
