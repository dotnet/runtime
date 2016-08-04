// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderSetBaseTypeConstraint
    {
        [Theory]
        [InlineData(typeof(string), typeof(string))]
        [InlineData(null, typeof(object))]
        public void SetBaseTypeConstraint(Type baseTypeConstraint, Type expectedBaseType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            GenericTypeParameterBuilder firstParam = typeParams[0];
            firstParam.SetBaseTypeConstraint(baseTypeConstraint);
            Assert.Equal(expectedBaseType, firstParam.BaseType);
        }
    }
}
