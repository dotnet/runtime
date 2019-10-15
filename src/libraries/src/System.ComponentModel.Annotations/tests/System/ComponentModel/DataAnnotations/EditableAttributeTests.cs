// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class EditableAttributeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Ctor(bool value)
        {
            var attribute = new EditableAttribute(value);
            Assert.Equal(value, attribute.AllowEdit);
            Assert.Equal(value, attribute.AllowInitialValue);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Properties_ChangingOneProperty_DoesNotAffectTheOther(bool value)
        {
            var attribute = new EditableAttribute(value);
            Assert.Equal(value, attribute.AllowEdit);
            Assert.Equal(value, attribute.AllowInitialValue);

            attribute.AllowInitialValue = !value;
            Assert.Equal(value, attribute.AllowEdit);
            Assert.Equal(!value, attribute.AllowInitialValue);
        }
    }
}
