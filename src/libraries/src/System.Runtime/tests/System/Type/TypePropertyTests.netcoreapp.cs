// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Tests.Types
{
    public abstract partial class TypePropertyTestBase
    {
        [Fact]
        public void IsGenericMethodParameter_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericMethodParameter, CreateType().IsGenericMethodParameter);
        }

        [Fact]
        public void IsGenericTypeParameter_Get_ReturnsExpected()
        {
            Assert.Equal(IsGenericTypeParameter, CreateType().IsGenericTypeParameter);
        }

        [Fact]
        public void IsSignatureType_Get_ReturnsExpected()
        {
            Assert.Equal(IsSignatureType, CreateType().IsSignatureType);
        }

        [Fact]
        public void IsSZArray_Get_ReturnsExpected()
        {
            Assert.Equal(IsSZArray, CreateType().IsSZArray);
        }

        [Fact]
        public void IsTypeDefinition_Get_ReturnsExpected()
        {
            Assert.Equal(IsTypeDefinition, CreateType().IsTypeDefinition);
        }

        [Fact]
        public void IsVariableBoundArray_Get_ReturnsExpected()
        {
            Assert.Equal(IsVariableBoundArray, CreateType().IsVariableBoundArray);
        }
    }
}
