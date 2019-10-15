// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public class IUnknownConstantAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new IUnknownConstantAttribute();
#pragma warning disable 0618 // UnknownWrapper is marked as Obsolete.
            UnknownWrapper wrapper = Assert.IsType<UnknownWrapper>(attribute.Value);
#pragma warning restore 0618
            Assert.Null(wrapper.WrappedObject);
        }
    }
}
