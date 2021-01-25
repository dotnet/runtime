// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class AddingNewEventArgsTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var args = new AddingNewEventArgs();
            Assert.Null(args.NewObject);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("newObject")]
        public void Ctor_NewObject(object newObject)
        {
            var args = new AddingNewEventArgs(newObject);
            Assert.Same(newObject, args.NewObject);
        }
    }
}
