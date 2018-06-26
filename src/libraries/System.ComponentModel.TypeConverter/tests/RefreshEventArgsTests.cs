// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class RefreshEventArgsTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData(typeof(int))]
        public void Ctor_Type(Type typeChanged)
        {
            var args = new RefreshEventArgs(typeChanged);
            Assert.Null(args.ComponentChanged);
            Assert.Same(typeChanged, args.TypeChanged);
        }

        [Fact]
        public void Ctor_Object()
        {
            object componentChanged = "componentChanged";
            var args = new RefreshEventArgs(componentChanged);
            Assert.Same(componentChanged, args.ComponentChanged);
            Assert.Equal(typeof(string), args.TypeChanged);
        }

        [Fact]
        public void Ctor_NullComponentChanged_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new RefreshEventArgs((object)null));
        }
    }
}
