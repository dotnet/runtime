// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class CloseTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void AfterDisposeThrows()
        {
            StreamWriter sw2;

            // [] Calling methods after disposing the stream should throw
            //-----------------------------------------------------------------
            sw2 = new StreamWriter(CreateStream());
            sw2.Dispose();
            ValidateDisposedExceptions(sw2);
        }

        [Fact]
        public void AfterCloseThrows()
        {
            StreamWriter sw2;

            // [] Calling methods after closing the stream should throw
            //-----------------------------------------------------------------
            sw2 = new StreamWriter(CreateStream());
            sw2.Close();
            ValidateDisposedExceptions(sw2);
        }

        private void ValidateDisposedExceptions(StreamWriter sw)
        {
            if (PlatformDetection.IsNetCore)
            {
                Assert.NotNull(sw.BaseStream);
            }
            Assert.Throws<ObjectDisposedException>(() => sw.Write('A'));
            Assert.Throws<ObjectDisposedException>(() => sw.Write("hello"));
            Assert.Throws<ObjectDisposedException>(() => sw.Flush());
            Assert.Throws<ObjectDisposedException>(() => sw.AutoFlush = true);
        }

        [Fact]
        public void CloseCausesFlush() {
            StreamWriter sw2;
            Stream memstr2;

            // [] Check that flush updates the underlying stream
            //-----------------------------------------------------------------
            memstr2 = CreateStream();
            sw2 = new StreamWriter(memstr2);

            var strTemp = "HelloWorld" ;
            sw2.Write( strTemp);
            Assert.Equal(0, memstr2.Length);

            sw2.Flush();
            Assert.Equal(strTemp.Length, memstr2.Length);
        }

        [Fact]
        public void CantFlushAfterDispose() {
            // [] Flushing disposed writer should throw
            //-----------------------------------------------------------------

            Stream memstr2 = CreateStream();
            StreamWriter sw2 = new StreamWriter(memstr2);
            
            sw2.Dispose();
            Assert.Throws<ObjectDisposedException>(() => sw2.Flush());
        }

        [Fact]
        public void CantFlushAfterClose()
        {
            // [] Flushing closed writer should throw
            //-----------------------------------------------------------------

            Stream memstr2 = CreateStream();
            StreamWriter sw2 = new StreamWriter(memstr2);

            sw2.Close();
            Assert.Throws<ObjectDisposedException>(() => sw2.Flush());
        }
    }
}
