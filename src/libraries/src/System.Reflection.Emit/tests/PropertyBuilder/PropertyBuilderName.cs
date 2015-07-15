// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest9
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicNestedTypeName = "TestDynamicNestedType";
        private const string DynamicDerivedTypeName = "TestDynamicDerivedType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder GetTypeBuilder(string typeName, TypeAttributes typeAtt)
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = DynamicAssemblyName;
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                                                                        myAssemblyName,
                                                                        AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(
                                                                        myAssemblyBuilder,
                                                                        DynamicModuleName);

            return myModuleBuilder.DefineType(typeName, typeAtt);
        }

        [Fact]
        public void TestNameOfRandomString()
        {
            string expectedName = new string((char)(_generator.GetInt32() % ushort.MaxValue + 1), 1) +
                _generator.GetString(false, 1, 260);

            ExecutePosTest(expectedName);
        }

        [Fact]
        public void TestWithLanguageKeyword()
        {
            string expectedName = "class";
            ExecutePosTest(expectedName);
        }

        [Fact]
        public void TestWithLongString()
        {
            string expectedName = new string('a', Int16.MaxValue);
            ExecutePosTest(expectedName);
        }

        [Fact]
        public void TestWithSpecialCharacters()
        {
            string expectedName = "1A\0\t\v\r\n\n\uDC81\uDC91";
            ExecutePosTest(expectedName);
        }


        private void ExecutePosTest(string expectedName)
        {
            string actualName;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            myTypeBuilder = GetTypeBuilder(
                                    DynamicTypeName,
                                    TypeAttributes.Class |
                                    TypeAttributes.Public);

            myPropertyBuilder = myTypeBuilder.DefineProperty(
                                    expectedName,
                                    PropertyAttributes.None,
                                    typeof(int),
                                    null);
            actualName = myPropertyBuilder.Name;
            Assert.Equal(expectedName, actualName);
        }
    }
}
