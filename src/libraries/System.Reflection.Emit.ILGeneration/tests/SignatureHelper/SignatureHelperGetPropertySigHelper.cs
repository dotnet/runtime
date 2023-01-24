// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperGetPropertySigHelper
    {
        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(typeof(string), new Type[] { typeof(Delegate), typeof(int) }, 6)]
        [InlineData(typeof(Type), new Type[] { typeof(char), typeof(object) }, 6)]
        public void GetPropertySigHelper_Module_Type_TypeArray(Type returnType, Type[] parameterTypes, int expectedLength)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            SignatureHelper helper = SignatureHelper.GetPropertySigHelper(module, returnType, parameterTypes);
            Assert.Equal(expectedLength, helper.GetSignature().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetPropertySigHelper_Module_Type_TypeArray_NullModule_DoesNotThrow()
        {
            SignatureHelper helper = SignatureHelper.GetPropertySigHelper(null, typeof(string), new Type[] { typeof(string), typeof(int) });
            Assert.Equal(5, helper.GetSignature().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetPropertySigHelper_Module_Type_TypeArray_NullObjectInParameterTypes_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            AssertExtensions.Throws<ArgumentNullException>("argument", () => SignatureHelper.GetPropertySigHelper(module, typeof(string), new Type[] { null, typeof(int) }));
        }

        public static IEnumerable<object[]> GetPropertySigHelper_TestData()
        {
            yield return new object[] { new Type[] { typeof(int), typeof(char) }, 29 };
            yield return new object[] { new Type[] { typeof(short), typeof(bool) }, 29 };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(GetPropertySigHelper_TestData))]
        public void GetPropertySigHelper_Module_Type_TypeArray_TypeArray_TypeArrayArray_TypeArrayArray(Type[] types, int expectedLength)
        {
            ModuleBuilder module = Helpers.DynamicModule();

            Type[][] customModifiers = new Type[][] { types, types };
            SignatureHelper helper = SignatureHelper.GetPropertySigHelper(module, typeof(string), types, types, types, customModifiers, customModifiers);
            Assert.Equal(expectedLength, helper.GetSignature().Length);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetPropertySigHelper_NullModule_ThrowsNullReferenceException()
        {
            Type[] types = new Type[] { typeof(short), typeof(bool) };
            Type[][] customModifiers = new Type[][] { types, types };

            Assert.Throws<NullReferenceException>(() => SignatureHelper.GetPropertySigHelper(null, typeof(string), types, types, types, customModifiers, customModifiers));
        }


        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void GetPropertySigHelper_NullTypeInParameterTypes_ThrowsArgumentNullException()
        {
            Type[] types = new Type[] { typeof(short), null };
            Type[][] customModifiers = new Type[][] { types, types };

            ModuleBuilder module = Helpers.DynamicModule();

            AssertExtensions.Throws<ArgumentNullException>("optionalCustomModifiers", () => SignatureHelper.GetPropertySigHelper(module, typeof(string), types, types, types, customModifiers, customModifiers));
        }
    }
}
