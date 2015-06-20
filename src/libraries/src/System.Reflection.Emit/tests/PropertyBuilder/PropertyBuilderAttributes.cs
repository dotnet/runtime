// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest3
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicPropertyName = "TestDynamicProperty";

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
            PropertyAttributes propertyAttr = PropertyAttributes.HasDefault;
            ExecutePosTest(propertyAttr);
        }

        [Fact]
        public void PosTest2()
        {
            PropertyAttributes propertyAttr = PropertyAttributes.None;
            ExecutePosTest(propertyAttr);
        }

        [Fact]
        public void PosTest8()
        {
            PropertyAttributes propertyAttr = PropertyAttributes.RTSpecialName;
            ExecutePosTest(propertyAttr);
        }

        [Fact]
        public void PosTest9()
        {
            PropertyAttributes propertyAttr = PropertyAttributes.SpecialName;
            ExecutePosTest(propertyAttr);
        }

        [Fact]
        public void PosTest10()
        {
            PropertyAttributes propertyAttr =
                                    PropertyAttributes.SpecialName |
                                    PropertyAttributes.RTSpecialName |
                                    PropertyAttributes.None |
                                    PropertyAttributes.HasDefault;
            ExecutePosTest(propertyAttr);
        }

        private void ExecutePosTest(PropertyAttributes propertyAttr)
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            PropertyAttributes actualAttributes;
            myTypeBuilder = GetTypeBuilder(
                                    DynamicTypeName,
                                    TypeAttributes.Class |
                                    TypeAttributes.Public);

            myPropertyBuilder = myTypeBuilder.DefineProperty(
                                    DynamicPropertyName,
                                    propertyAttr,
                                    typeof(int),
                                    null);
            actualAttributes = myPropertyBuilder.Attributes;
            Assert.Equal(propertyAttr, actualAttributes);
        }
    }
}
