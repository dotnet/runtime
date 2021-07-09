// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public struct MBTestStruct
    {
        public int IntVal;
        public string StrVal;
    }

    public class ModuleBuilderGetArrayMethod
    {
        public static IEnumerable<object[]> CallingConventions_TestData()
        {
            yield return new object[] { CallingConventions.Any };
            yield return new object[] { CallingConventions.ExplicitThis };
            yield return new object[] { CallingConventions.HasThis };
            yield return new object[] { CallingConventions.Standard };
            yield return new object[] { CallingConventions.VarArgs };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_ValidArrayValues_VoidReturnType(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[]), callingConvention.ToString(), callingConvention, typeof(void), new Type[0]);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_ValidArrayValues_ValueReturnType(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(int[]), callingConvention.ToString(), callingConvention, typeof(int), new Type[0]);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_ValidArrayValues_ReferenceReturnType(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(object[]), callingConvention.ToString(), callingConvention, typeof(object), new Type[0]);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_ValidArrayValues_ValueParameterType(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(object[]), callingConvention.ToString() + "1", callingConvention, typeof(int), new Type[] { typeof(int) });

            VerifyGetArrayMethod(module, typeof(object[]), callingConvention.ToString() + "2", callingConvention, typeof(int), new Type[] { typeof(int), typeof(MBTestStruct) });
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_ValidArrayValues_ReferenceParameterType(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[]), callingConvention.ToString() + "1", callingConvention, typeof(int), new Type[] { typeof(object) });

            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[]), callingConvention.ToString() + "2", callingConvention, typeof(int), new Type[] { typeof(object), typeof(string), typeof(ModuleBuilderGetArrayMethod) });
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_JaggedArray(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[][]), callingConvention.ToString() + "1", callingConvention, typeof(int), new Type[] { typeof(object) });

            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[][]), callingConvention.ToString() + "2", callingConvention, typeof(int), new Type[] { typeof(object), typeof(int), typeof(ModuleBuilderGetArrayMethod) });
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_MultiDimensionalArray(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[,]), callingConvention.ToString() + "1", callingConvention, typeof(int), new Type[] { typeof(object) });

            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[,]), callingConvention.ToString() + "2", callingConvention, typeof(int), new Type[] { typeof(object), typeof(int), typeof(ModuleBuilderGetArrayMethod) });
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(CallingConventions_TestData))]
        public void GetArrayMethod_NullParameters(CallingConventions callingConvention)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            VerifyGetArrayMethod(module, typeof(ModuleBuilderGetArrayMethod[]), callingConvention.ToString(), callingConvention, typeof(void), null);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [InlineData(typeof(ModuleBuilderGetArrayMethod))]
        [InlineData(typeof(int))]
        [InlineData(typeof(Array))]
        [InlineData(typeof(void))]
        public void GetArrayMethod_ArrayClassNotArray_ThrowsArgumentException(Type arrayClass)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentException>(null, () => module.GetArrayMethod(arrayClass, "TestMethod", CallingConventions.Standard, typeof(void), new Type[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void GetArrayMethod_InvalidArgument_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();

            AssertExtensions.Throws<ArgumentNullException>("arrayClass", () => module.GetArrayMethod(null, "TestMethod", CallingConventions.Standard, typeof(void), new Type[0]));

            AssertExtensions.Throws<ArgumentNullException>("methodName", () => module.GetArrayMethod(typeof(string[]), null, CallingConventions.Standard, typeof(void), new Type[0]));
            AssertExtensions.Throws<ArgumentException>("methodName", () => module.GetArrayMethod(typeof(string[]), "", CallingConventions.Standard, typeof(void), new Type[0]));

            AssertExtensions.Throws<ArgumentNullException>("argument", () => module.GetArrayMethod(typeof(string[]), "TestMethod", CallingConventions.Standard, typeof(void), new Type[] { null }));
        }

        private void VerifyGetArrayMethod(ModuleBuilder module, Type arrayClass, string methodName, CallingConventions callingConvention, Type returnType, Type[] parameterTypes)
        {
            MethodInfo method = module.GetArrayMethod(arrayClass, methodName, callingConvention, returnType, parameterTypes);

            Assert.Equal(arrayClass, method.DeclaringType);
            Assert.Equal(methodName, method.Name);
            Assert.Equal(callingConvention, method.CallingConvention);
            Assert.Equal(returnType, method.ReturnType);
            Assert.NotEqual(0, method.MetadataToken);
        }
    }
}
