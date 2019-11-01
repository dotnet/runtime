// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperGetMethodSigHelper
    {
        [Theory]
        [InlineData(CallingConventions.Any, typeof(int))]
        [InlineData(CallingConventions.ExplicitThis, typeof(string))]
        [InlineData(CallingConventions.HasThis, typeof(string))]
        [InlineData(CallingConventions.Standard, typeof(string))]
        [InlineData(CallingConventions.VarArgs, typeof(string))]
        public void GetMethodSigHelper_CallingConventions_Type_Length_ReturnsThree(CallingConventions callingConventions, Type type)
        {
            SignatureHelper helper = SignatureHelper.GetMethodSigHelper(callingConventions, type);
            Assert.Equal(3, helper.GetSignature().Length);
        }

        [Theory]
        [InlineData(CallingConventions.Any, typeof(int))]
        [InlineData(CallingConventions.ExplicitThis, typeof(string))]
        [InlineData(CallingConventions.HasThis, typeof(string))]
        [InlineData(CallingConventions.Standard, typeof(string))]
        [InlineData(CallingConventions.VarArgs, typeof(string))]
        public void GetMethodSigHelper_Module_CallingConventions_Type_Length_ReturnsThree(CallingConventions callingConventions, Type type)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetMethodSigHelper(module, callingConventions, type);
            Assert.Equal(3, helper1.GetSignature().Length);

            SignatureHelper helper2 = SignatureHelper.GetMethodSigHelper(null, callingConventions, type);
            Assert.Equal(3, helper2.GetSignature().Length);
        }

        [Fact]
        public void GetMethodSigHelper_Module_Type_TypeArray()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper1 = SignatureHelper.GetMethodSigHelper(module, typeof(string), new Type[] { typeof(char), typeof(int) });
            Assert.Equal(5, helper1.GetSignature().Length);

            SignatureHelper helper2 = SignatureHelper.GetMethodSigHelper(null, typeof(string), new Type[] { typeof(char), typeof(int) });
            Assert.Equal(5, helper2.GetSignature().Length);
        }

        [Fact]
        public void GetMethodSigHelper_Module_Type_TypeArray_NullObjectInParameterType_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("argument", () => SignatureHelper.GetMethodSigHelper(module, typeof(string), new Type[] { typeof(char), null }));
        }
    }
}
