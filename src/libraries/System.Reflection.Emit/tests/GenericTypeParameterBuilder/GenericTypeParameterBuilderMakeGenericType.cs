// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderMakeGenericType
    {
        [Fact]
        public void MakeGenericType_NullTypeArguments_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            Assert.Throws<InvalidOperationException>(() => typeParams[0].MakeGenericType(null));
        }

        [Fact]
        public void MakeGenericType_SingleTypeArgument_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            Assert.Throws<InvalidOperationException>(() => typeParams[0].MakeGenericType(new Type[] { typeof(Type) }));
        }

        [Fact]
        public void MakeGenericType_TwoTypeArguments_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            Assert.Throws<InvalidOperationException>(() => typeParams[0].MakeGenericType(new Type[] { typeof(int), typeof(string) }));
        }
    }
}
