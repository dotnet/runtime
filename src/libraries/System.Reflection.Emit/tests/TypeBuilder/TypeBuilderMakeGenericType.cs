// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeGenericType
    {
        [Fact]
        public void TestWithTwoGenericParamsWithPredefinedTypes()
        {
            string[] genericParab = new string[] { "U", "T" };
            string mscorlibFullName = typeof(int).GetTypeInfo().Assembly.FullName;
            string expectedFullName = "testType[[System.String, " + mscorlibFullName + "],[System.Int32, " + mscorlibFullName + "]]";
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            GenericTypeParameterBuilder[] typeGenParam = testTyBuilder.DefineGenericParameters(genericParab);
            Type myType = testTyBuilder.MakeGenericType(new Type[] { typeof(string), typeof(int) });
            Assert.True(myType.FullName.Equals(expectedFullName));
        }

        [Fact]
        public void TestWithTwoGenericParamsWithCustomTypes()
        {
            string[] genericParab = new string[] { "U", "T" };
            string expectedFullName = "testType[[System.Reflection.Emit.Tests.TBGenericTypeTestClass, System.Reflection.Emit.Tests, Version=999.999.999.999, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a],[System.Reflection.Emit.Tests.TBGenericTypeTestInterface, System.Reflection.Emit.Tests, Version=999.999.999.999, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]";
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            GenericTypeParameterBuilder[] typeGenParam = testTyBuilder.DefineGenericParameters(genericParab);
            Type myType = testTyBuilder.MakeGenericType(new Type[] { typeof(TBGenericTypeTestClass), typeof(TBGenericTypeTestInterface) });
            Assert.True(myType.FullName.Equals(expectedFullName), string.Format("Actual string = {0}", myType.FullName));
        }

        [Fact]
        public void TestWithSingleGenericParam()
        {
            string[] genericParab = new string[] { "U" };
            string mscorlibFullName = typeof(int).GetTypeInfo().Assembly.FullName;
            string expectedFullName = "testType[[System.String, " + mscorlibFullName + "]]";
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            GenericTypeParameterBuilder[] typeGenParam = testTyBuilder.DefineGenericParameters(genericParab);
            Type myType = testTyBuilder.MakeGenericType(new Type[] { typeof(string) });
            Assert.True(myType.FullName.Equals(expectedFullName));
        }

        [Fact]
        public void TestThrowsExceptionOnEmptyTypeArray()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Assert.Throws<InvalidOperationException>(() => { Type myType = testTyBuilder.MakeGenericType(new Type[0]); });
        }

        [Fact]
        public void TestThrowsExceptionOnNullTypeArray()
        {
            string[] genericParab = new string[] { "U", "T" };
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            GenericTypeParameterBuilder[] typeGenParam = testTyBuilder.DefineGenericParameters(genericParab);
            Assert.Throws<ArgumentNullException>(() => { Type myType = testTyBuilder.MakeGenericType(null); });
        }

        [Fact]
        public void TestThrowsExceptionOnNullInMemberOfTypeArray()
        {
            string[] genericParab = new string[] { "U", "T" };
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            GenericTypeParameterBuilder[] typeGenParam = testTyBuilder.DefineGenericParameters(genericParab);
            Assert.Throws<ArgumentNullException>(() => { Type myType = testTyBuilder.MakeGenericType(new Type[] { null, null }); });
        }

        private ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "myAssembly.dll";
            AssemblyBuilder myAssemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModBuilder = myAssemBuilder.DefineDynamicModule("Module1");
            return myModBuilder;
        }
    }

    public class TBGenericTypeTestClass { }
    public interface TBGenericTypeTestInterface { }
}
