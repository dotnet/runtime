// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class LabelGetHashCode
    {
        [Fact]
        public void GetHashCode_NewInstance_ReturnsZero()
        {
            Label label1 = new Label();
            Label label2 = new Label();

            Assert.Equal(0, label1.GetHashCode());
            Assert.Equal(label2.GetHashCode(), label1.GetHashCode());
        }

        [Fact]
        public void GetHashCode_CreatedByILGenerator_ReturnsIndex()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            MethodBuilder method = type.DefineMethod("Method", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            for (int i = 0; i < 1000; i++)
            {
                Label label = ilGenerator.DefineLabel();
                Assert.Equal(i, label.GetHashCode());
            }
        }
    }
}
