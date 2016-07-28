// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderGenericParameterPosition
    {
        [Fact]
        public void GenericParameterPosition()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst", "TSecond" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            Assert.Equal(0, typeParams[0].GenericParameterPosition);
            Assert.Equal(1, typeParams[1].GenericParameterPosition);
        }
    }
}
