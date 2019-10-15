// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderToString
    {
        [Fact]
        public void ToString_NullRequiredOptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = new Type[] { typeof(int), typeof(double) };

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes, null, null);
            Assert.StartsWith("Name: .ctor", constructor.ToString());
        }

        [Fact]
        public void ToString_NoRequiredOptionalCustomModifiers()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Type[] parameterTypes = new Type[] { typeof(int), typeof(double) };

            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);
            Assert.StartsWith("Name: .ctor", constructor.ToString());
        }
    }
}
