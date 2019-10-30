// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // ComEventInterfaceAttribute is marked as Obsolete.
    public class ComEventInterfaceAttributeTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData(typeof(int), typeof(string))]
        public void Ctor_DefaultInterface(Type sourceInterface, Type eventProvider)
        {
            var attribute = new ComEventInterfaceAttribute(sourceInterface, eventProvider);
            Assert.Equal(sourceInterface, attribute.SourceInterface);
            Assert.Equal(eventProvider, attribute.EventProvider);
        }
    }
#pragma warning restore 0618
}
