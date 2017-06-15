// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class PermissiveModifyControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new PermissiveModifyControl();
            Assert.True(control.IsCritical);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1413", control.Type);
            
            Assert.Empty(control.GetValue());
        }
    }
}
