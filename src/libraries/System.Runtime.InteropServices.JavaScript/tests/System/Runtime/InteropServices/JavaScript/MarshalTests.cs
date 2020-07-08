// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class MarshalTests
    {
        [Fact]
        public static void MarshalTypedArray()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                var uint8View = new Uint8Array(buffer);
                App.call_test_method (""MarshalByteBuffer"", ""o"", [ uint8View ]);		
            ");

            Assert.Equal(16, HelperMarshal.byteBuffer.Length);
        }

        [Fact]
        public static void MarshalArrayBuffer()
        {
            Runtime.InvokeJS(@"
                var buffer = new ArrayBuffer(16);
                App.call_test_method (""MarshalArrayBuffer"", ""o"", [ buffer ]);		
            ");

            Assert.Equal(16, HelperMarshal.byteBuffer.Length);
        }
    }
}
