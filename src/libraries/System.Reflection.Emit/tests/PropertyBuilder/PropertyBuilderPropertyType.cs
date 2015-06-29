// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest10
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicNestedTypeName = "TestDynamicNestedType";
        private const string DynamicDerivedTypeName = "TestDynamicDerivedType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";

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
        public void PosTest1()
        {
            Type expectedType = typeof(int);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest2()
        {
            Type expectedType = typeof(byte);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest3()
        {
            Type expectedType = typeof(DateTime);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest4()
        {
            Type expectedType = typeof(double);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest5()
        {
            Type expectedType = typeof(string);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest6()
        {
            Type expectedType = typeof(int[]);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest7()
        {
            Type expectedType = typeof(IFoo);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest8()
        {
            Type expectedType = typeof(Colors);
            ExecutePosTest(expectedType);
        }

        [Fact]
        public void PosTest9()
        {
            Type expectedType = typeof(MySelector);
            ExecutePosTest(expectedType);
        }

        private void ExecutePosTest(Type expectedType)
        {
            Type actualType;
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            myTypeBuilder = GetTypeBuilder(
                                    DynamicTypeName,
                                    TypeAttributes.Class |
                                    TypeAttributes.Public);

            myPropertyBuilder = myTypeBuilder.DefineProperty(
                                    DynamicPropertyName,
                                    PropertyAttributes.None,
                                    expectedType,
                                    null);
            actualType = myPropertyBuilder.PropertyType;
            Assert.Equal(expectedType, actualType);
        }
    }

    internal interface IFoo
    {
        void MethodA();
    }

    internal enum Colors
    {
        Red, Green, Blue
    }

    internal delegate bool MySelector(int value);
}
