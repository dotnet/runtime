// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed partial class WebSocketExceptionTests
    {
        [Fact]
        public void ConstructorTests_DefaultConstructor_MatchesLastError()
        {
            int error = Marshal.GetLastWin32Error();
            var exc = new WebSocketException();
            Assert.Equal(error, exc.NativeErrorCode);
            Assert.Equal(error, exc.ErrorCode);
        }
    }
}
