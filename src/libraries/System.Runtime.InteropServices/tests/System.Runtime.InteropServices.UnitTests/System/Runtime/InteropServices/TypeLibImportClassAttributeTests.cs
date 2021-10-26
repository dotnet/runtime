// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class TypeLibImportClassAttributeTests
    {
        [Fact]
        public void Ctor_ImportClass()
        {
            var attribute = new TypeLibImportClassAttribute(typeof(int));
            Assert.Equal(typeof(int).ToString(), attribute.Value);
        }

        [Fact]
        public void Ctor_NullImportClass_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new TypeLibImportClassAttribute(null));
        }
    }
}
