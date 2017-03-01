// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Pipes.Tests
{
    public class TestPipeStream : PipeStream
    {
        public TestPipeStream() : base(PipeDirection.InOut, 512) // values don't matter for these tests.
        {
        }

        [Fact]
        public void TestInitializeHandle()
        {
            Assert.False(IsHandleExposed);
            InitializeHandle(null, true, false);
            Assert.True(IsHandleExposed);
            InitializeHandle(null, false, false);
            Assert.False(IsHandleExposed);
            Assert.Throws<InvalidOperationException>(() => SafePipeHandle);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Null handle throws on Windows only
        public void TestCheckPipePropertyOperations_Unix()
        {
            // handle is null - throws on Windows but not Unix.
            CheckPipePropertyOperations();
        }
            
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Null handle throws on Windows only
        public void TestCheckPipePropertyOperations_Windows()
        {
            // handle is null - throws on Windows but not Unix
            Assert.Throws<InvalidOperationException>(() => CheckPipePropertyOperations());
        }

        [Fact]
        public void TestCheckReadWrite()
        {
            Assert.Throws<InvalidOperationException>(() => CheckReadOperations());
            Assert.Throws<InvalidOperationException>(() => CheckWriteOperations());
        }
    }
}
