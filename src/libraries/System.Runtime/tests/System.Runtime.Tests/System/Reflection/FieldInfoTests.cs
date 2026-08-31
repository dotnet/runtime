// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;
using TestAttributes;

namespace System.Reflection.Tests
{
    public class FieldInfoTests
    {
        public int int_field;

        [Fact]
        public void ToStringFieldType()
        {
            Assert.Equal("Int32 int_field", typeof(FieldInfoTests).GetField("int_field").ToString());
        }
    }
}
