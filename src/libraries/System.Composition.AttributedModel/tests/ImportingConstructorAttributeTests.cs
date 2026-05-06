// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Composition.AttributeModel.Tests
{
    public class ImportingConstructorAttributeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var attribute = new ImportingConstructorAttribute();
            Assert.Equal(typeof(ImportingConstructorAttribute), attribute.TypeId);
        }
    }
}
