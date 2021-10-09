// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata
{
    public class MetadataUpdateHandlerAttributeTest
    {
        [Fact]
        public void Ctor_RoundtripType()
        {
            Type t = typeof(MetadataUpdateHandlerAttributeTest);
            var a = new MetadataUpdateHandlerAttribute(t);
            Assert.Same(t, a.HandlerType);
        }
    }
}
