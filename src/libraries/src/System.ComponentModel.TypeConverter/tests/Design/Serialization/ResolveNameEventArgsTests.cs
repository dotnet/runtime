// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Design.Serialization.Tests
{
    public class ResolveNameEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("Name")]
        public void Ctor_Name(string name)
        {
            var eventArgs = new ResolveNameEventArgs(name);
            Assert.Same(name, eventArgs.Name);
            Assert.Null(eventArgs.Value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("Value")]
        public void Value_Set_GetReturnsExpected(object value)
        {
            var eventArgs = new ResolveNameEventArgs("Name") { Value = value };
            Assert.Same(value, eventArgs.Value);
        }
    }
}
