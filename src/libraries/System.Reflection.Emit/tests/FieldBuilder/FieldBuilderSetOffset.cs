// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderSetOffset
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(8)]
        public void SetOffset(int offset)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("Field_PosTest2", typeof(int), FieldAttributes.Public);
            field.SetOffset(offset);
        }

        [Fact]
        public void SetOffset_DifferentForTwoProperties()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            FieldBuilder field1 = type.DefineField("TestField1", typeof(int), FieldAttributes.Public);
            field1.SetOffset(0);

            FieldBuilder field2 = type.DefineField("TestField2", typeof(int), FieldAttributes.Public);
            field2.SetOffset(4);

            FieldBuilder field3 = type.DefineField("TestField3", typeof(int), FieldAttributes.Public);
            field3.SetOffset(4);
        }

        [Fact]
        public void SetOffset_SameForTwoProperties()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            FieldBuilder field1 = type.DefineField("TestField1", typeof(int), FieldAttributes.Public);
            field1.SetOffset(0);

            FieldBuilder field2 = type.DefineField("TestField2", typeof(int), FieldAttributes.Public);
            field2.SetOffset(0);
        }

        [Fact]
        public void SetOffset_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Public);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => field.SetOffset(0));
        }

        [Fact]
        public void SetOffset_NegativeOffset_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);

            FieldBuilder field = type.DefineField("TestField", typeof(int), FieldAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(null, () => field.SetOffset(-1));
        }
    }
}
