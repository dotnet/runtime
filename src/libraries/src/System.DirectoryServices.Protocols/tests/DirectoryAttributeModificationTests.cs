// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class DirectoryAttributeModificationTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var modification = new DirectoryAttributeModification();
            Assert.Empty(modification.Name);
            Assert.Empty(modification);
            Assert.Equal(DirectoryAttributeOperation.Replace, modification.Operation);
        }

        [Fact]
        public void Operation_SetValid_GetReturnsExpected()
        {
            var modification = new DirectoryAttributeModification { Operation = DirectoryAttributeOperation.Delete };
            Assert.Equal(DirectoryAttributeOperation.Delete, modification.Operation);
        }

        [Theory]
        [InlineData(DirectoryAttributeOperation.Add - 1)]
        [InlineData(DirectoryAttributeOperation.Replace + 1)]
        public void Operation_SetInvalid_InvalidEnumArgumentException(DirectoryAttributeOperation operation)
        {
            var modification = new DirectoryAttributeModification();
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => modification.Operation = operation);
        }
    }
}
