// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class RuntimeBinderInternalCompilerExceptionTests
    {
        [Fact]
        public void NullaryCtor()
        {
            RuntimeBinderInternalCompilerException rbe = new RuntimeBinderInternalCompilerException();
            Assert.Null(rbe.InnerException);
            Assert.Empty(rbe.Data);
            Assert.True((rbe.HResult & 0xFFFF0000) == 0x80130000); // Error from .NET
            Assert.Contains(rbe.GetType().FullName, rbe.Message); // Localized, but should contain type name.
        }

        [Fact]
        public void StringCtor()
        {
            string message = "This is a test message.";
            RuntimeBinderInternalCompilerException rbe = new RuntimeBinderInternalCompilerException(message);
            Assert.Null(rbe.InnerException);
            Assert.Empty(rbe.Data);
            Assert.True((rbe.HResult & 0xFFFF0000) == 0x80130000); // Error from .NET
            Assert.Same(message, rbe.Message);
            rbe = new RuntimeBinderInternalCompilerException(null);
            Assert.Equal(new RuntimeBinderInternalCompilerException().Message, rbe.Message);
        }


        [Fact]
        public void InnerExceptionCtor()
        {
            string message = "This is a test message.";
            Exception inner = new Exception("This is a test exception");
            RuntimeBinderInternalCompilerException rbe = new RuntimeBinderInternalCompilerException(message, inner);
            Assert.Same(inner, rbe.InnerException);
        }
    }
}
