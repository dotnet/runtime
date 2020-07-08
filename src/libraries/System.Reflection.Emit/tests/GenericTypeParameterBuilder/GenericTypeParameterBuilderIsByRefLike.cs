// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderIsByRefLike
    {
        [Fact]
        public void IsByRefLikeReturnsFalse()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            var typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            Assert.False(typeParams[0].IsByRefLike);
        }
    }
}
