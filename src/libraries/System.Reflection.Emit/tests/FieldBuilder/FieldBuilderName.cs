// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderName
    {
        [Theory]
        [InlineData("ABC")]
        [InlineData("A!?123C")]
        public void Name(string fieldName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField(fieldName, typeof(object), FieldAttributes.Public);
            Assert.Equal(fieldName, field.Name);
        }
    }
}
