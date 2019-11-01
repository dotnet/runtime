// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_netstandard17 : NamedPipeTestBase
    {
        [Fact]
        public void NamedPipeClientStream_InvalidHandleInerhitability()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeDirection.Out, 0, TokenImpersonationLevel.Delegation, HandleInheritability.None - 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("inheritability", () => new NamedPipeClientStream("a", "b", PipeDirection.Out, 0, TokenImpersonationLevel.Delegation, HandleInheritability.Inheritable + 1));
        }
    }
}
