// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Tests;
using Xunit;

namespace System.Reflection.Tests
{
    public class StrongNameKeyPairTests : FileCleanupTestBase
    {
#pragma warning disable SYSLIB0017 // Strong name signing is not supported and throws PlatformNotSupportedException.
        [Fact]
        public void Ctor_ByteArray_ThrowsPlatformNotSupportedException()
        {
            AssertExtensions.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair(new byte[] { 7, 2, 0, 0 }));
        }

        [Fact]
        public void Ctor_NullKeyPairArray_ThrowsPlatformNotSupportedException()
        {
            AssertExtensions.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair((byte[])null));
        }

        [Fact]
        public void Ctor_FileStream_ThrowsPlatformNotSupportedException()
        {
            string tempPath = GetTestFilePath();
            File.WriteAllBytes(tempPath, new byte[] { 7, 2, 0, 0 });
            using (FileStream fileStream = File.OpenRead(tempPath))
            {
                AssertExtensions.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair(fileStream));
            }
        }

        [Fact]
        public void Ctor_NullKeyPairFile_ThrowsPlatformNotSupportedException()
        {
            AssertExtensions.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair((FileStream)null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("keyPairContainer")]
        public void Ctor_String_ThrowsPlatformNotSupportedException(string keyPairContainer)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair(keyPairContainer));
        }

        [Fact]
        public void Ctor_SerializationInfo_StreamingContext_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SubStrongNameKeyPair(null, new StreamingContext()));
        }

        private class SubStrongNameKeyPair : StrongNameKeyPair
        {
            public SubStrongNameKeyPair(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
#pragma warning restore SYSLIB0017
    }
}
