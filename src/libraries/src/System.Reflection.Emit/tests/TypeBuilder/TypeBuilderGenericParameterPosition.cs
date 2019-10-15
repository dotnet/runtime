// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGenericParameterPosition
    {
        [Fact]
        public void GenericParameterPosition()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            GenericTypeParameterBuilder[] genericParameters = type.DefineGenericParameters("T0", "T1");
            TypeInfo genericParameter = genericParameters[1].DeclaringType.GetTypeInfo();
            Assert.Equal(0, genericParameter.GenericParameterPosition);
        }
    }
}
