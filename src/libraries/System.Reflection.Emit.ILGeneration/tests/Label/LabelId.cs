// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class LabelId
    {
        [Fact]
        public void LabelId_DefaultConstuctor_ReturnsZero()
        {
            Label label1 = new Label();
            Label label2 = new Label();

            Assert.Equal(0, label1.Id);
            Assert.Equal(label2.Id, label1.Id);
        }

        [Fact]
        public void LabelId_CreatedByILGenerator_ReturnsId()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            for (int i = 0; i < 100; i++)
            {
                Label label = ilGenerator.DefineLabel();
                Assert.Equal(i, label.Id);
            }
        }
    }
}
