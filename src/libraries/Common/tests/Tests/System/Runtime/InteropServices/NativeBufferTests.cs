// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Tests.System.Runtime.InteropServices
{
    public class NativeBufferTests
    {
        [Fact]
        public void EnsureZeroCapacityDoesNotFreeBuffer()
        {
            using (var buffer = new NativeBuffer(10))
            {
                Assert.NotEqual(buffer.GetHandle().DangerousGetHandle(), IntPtr.Zero);
                buffer.EnsureByteCapacity(0);
                Assert.NotEqual(buffer.GetHandle().DangerousGetHandle(), IntPtr.Zero);
            }
        }

        [Fact]
        public void GetOverIndexThrowsArgumentOutOfRange()
        {
            using (var buffer = new NativeBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { byte c = buffer[0]; });
            }
        }

        [Fact]
        public void SetOverIndexThrowsArgumentOutOfRange()
        {
            using (var buffer = new NativeBuffer())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => { buffer[0] = 0; });
            }
        }

        [Fact]
        public void CanGetSetBytes()
        {
            using (var buffer = new NativeBuffer(1))
            {
                buffer[0] = 0xA;
                Assert.Equal(buffer[0], 0xA);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void NullSafePointerInTest()
        {
            using (var buffer = new NativeBuffer(0))
            {
                Assert.True(buffer.GetHandle().IsInvalid);
                Assert.Equal((ulong)0, buffer.ByteCapacity);

                // This will throw if we don't put a stub SafeHandle in for the empty buffer
                GetCurrentDirectorySafe((uint)buffer.ByteCapacity, buffer.GetHandle());
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364934.aspx
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentDirectoryW", CharSet = CharSet.Unicode, SetLastError = true, ExactSpelling = true)]
        private static extern uint GetCurrentDirectorySafe(
            uint nBufferLength,
            SafeHandle lpBuffer);
    }
}
