// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class BinaryFormatterTests
    {
        [Fact]
        public void Foo()
        {
            Assert.Equal(1, 1);
        }
    }

    [Serializable]
    internal class Serializable : ISerializable
    {
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}
