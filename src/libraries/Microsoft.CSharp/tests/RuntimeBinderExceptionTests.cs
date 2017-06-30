// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class RuntimeBinderExceptionTests
    {
        [Fact]
        public void NullaryCtor()
        {
            RuntimeBinderException rbe = new RuntimeBinderException();
            Assert.Null(rbe.InnerException);
            Assert.Empty(rbe.Data);
            Assert.True((rbe.HResult & 0xFFFF0000) == 0x80130000); // Error from .NET
            Assert.Contains(rbe.GetType().FullName, rbe.Message); // Localized, but should contain type name.
        }

        [Fact]
        public void StringCtor()
        {
            string message = "This is a test message.";
            RuntimeBinderException rbe = new RuntimeBinderException(message);
            Assert.Null(rbe.InnerException);
            Assert.Empty(rbe.Data);
            Assert.True((rbe.HResult & 0xFFFF0000) == 0x80130000); // Error from .NET
            Assert.Same(message, rbe.Message);
            rbe = new RuntimeBinderException(null);
            Assert.Equal(new RuntimeBinderException().Message, rbe.Message);
        }


        [Fact]
        public void InnerExceptionCtor()
        {
            string message = "This is a test message.";
            Exception inner = new Exception("This is a test exception");
            RuntimeBinderException rbe = new RuntimeBinderException(message, inner);
            Assert.Same(inner, rbe.InnerException);
        }
    }
}
