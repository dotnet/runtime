// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderMakePointerType
    {
        [Fact]
        public void MakePointerType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            Type pointerType = typeParams[0].MakePointerType();
            Assert.Equal(typeof(Array), pointerType.GetTypeInfo().BaseType);
            Assert.Equal("TFirst*", pointerType.Name);
        }
    }
}
