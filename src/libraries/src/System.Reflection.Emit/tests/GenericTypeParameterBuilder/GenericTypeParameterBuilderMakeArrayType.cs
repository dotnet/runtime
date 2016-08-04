// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderMakeArrayType
    {
        [Fact]
        public void MakeArrayType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            Assert.Equal("TFirst[]", typeParams[0].MakeArrayType().Name);
        }

        [Fact]
        public void MakeArrayType_Int_RankOfOne()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            Assert.Equal("TFirst[*]", typeParams[0].MakeArrayType(1).Name);
        }

        [Fact]
        public void MakeArrayType_Int_RankOfTwo()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);

            Assert.Equal("TFirst[,]", typeParams[0].MakeArrayType(2).Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void MakeArrayType_Int_RankLessThanOne_ThrowsIndexOutOfRangeException(int rank)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            
            Assert.Throws<IndexOutOfRangeException>(() => typeParams[0].MakeArrayType(rank));
        }
    }
}
