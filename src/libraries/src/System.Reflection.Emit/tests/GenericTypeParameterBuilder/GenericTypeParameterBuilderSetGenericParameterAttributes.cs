// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderSetGenericParameterAttributes
    {
        [Theory]
        [InlineData(GenericParameterAttributes.Contravariant)]
        [InlineData(GenericParameterAttributes.Covariant)]
        [InlineData(GenericParameterAttributes.DefaultConstructorConstraint)]
        [InlineData(GenericParameterAttributes.None)]
        [InlineData(GenericParameterAttributes.NotNullableValueTypeConstraint)]
        [InlineData(GenericParameterAttributes.ReferenceTypeConstraint)]
        [InlineData(GenericParameterAttributes.SpecialConstraintMask)]
        [InlineData(GenericParameterAttributes.VarianceMask)]
        public void SetGenericParameterAttributes(GenericParameterAttributes genericParameterAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            GenericTypeParameterBuilder firstTypeParam = typeParams[0];

            firstTypeParam.SetGenericParameterAttributes(genericParameterAttributes);
            Assert.Equal(genericParameterAttributes, firstTypeParam.GenericParameterAttributes);
        }
    }
}
