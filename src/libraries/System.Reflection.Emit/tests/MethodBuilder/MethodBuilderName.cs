// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderName
    {
        [Theory]
        [InlineData("TestMethod")]
        public void Name(string name)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method1 = type.DefineMethod(name, MethodAttributes.Public);
            Assert.Equal(name, method1.Name);

            MethodBuilder method2 = type.DefineMethod(name, MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), new Type[] { typeof(int) });
            Assert.Equal(name, method2.Name);
        }
    }
}
