// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest13
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";

        private TypeBuilder GetTypeBuilder(TypeAttributes typeAtt)
        {
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = DynamicAssemblyName;
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName,
                                                                            AssemblyBuilderAccess.Run);

            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder,
                                                                                DynamicModuleName);

            return myModuleBuilder.DefineType(DynamicTypeName, typeAtt);
        }

        [Fact]
        public void TestSetCustomAttribute()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(int);
            Type[] ctorParamTypes = new Type[] { typeof(int) };
            int expectedValue = TestLibrary.Generator.GetInt32();
            byte[] binaryAttr = new byte[6];
            object[] actualCustomAttrs;
            ConstructorInfo con = typeof(PBMyAttribute2).GetConstructor(ctorParamTypes);
            binaryAttr[0] = 01;
            binaryAttr[1] = 00;
            for (int i = 0; i < binaryAttr.Length - 2; ++i)
            {
                binaryAttr[i + 2] = (byte)(expectedValue >> (8 * i) & 0xff);
            }
            actualCustomAttrs = ExecutePosTest(
                                        con,
                                        binaryAttr,
                                        getMethodAttr,
                                        returnType,
                                        new Type[0],
                                        BindingFlags.Public |
                                        BindingFlags.Instance |
                                        BindingFlags.NonPublic |
                                        BindingFlags.Static);
            Assert.Equal(1, actualCustomAttrs.Length);
            Assert.True(actualCustomAttrs[0] is PBMyAttribute2);
            Assert.Equal(expectedValue, (actualCustomAttrs[0] as PBMyAttribute2).Value);
        }

        private object[] ExecutePosTest(
                            ConstructorInfo con,
                            byte[] binaryAttribute,
                            MethodAttributes getMethodAttr,
                            Type returnType,
                            Type[] paramTypes,
                            BindingFlags bindingAttr)
        {
            TypeBuilder myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                                             PropertyAttributes.HasDefault,
                                                                             returnType, null);
            myPropertyBuilder.SetCustomAttribute(con, binaryAttribute);
            // Define the "get" accessor method for DynamicPropertyName
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                                                       getMethodAttr, returnType, paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ret);

            // Map the 'get' method created above to our PropertyBuilder
            myPropertyBuilder.SetGetMethod(myMethodBuilder);

            Type myType = myTypeBuilder.CreateTypeInfo().AsType();

            PropertyInfo myProperty = myType.GetProperty(DynamicPropertyName, bindingAttr);
            return myProperty.GetCustomAttributes(false).Select(a => (object)a).ToArray();
        }

        [Fact]
        public void TestThrowsExceptionOnNullConstructorInfo()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;

            Type returnType = typeof(int);
            Type[] paramTypes = new Type[0];

            Assert.Throws<ArgumentNullException>(() =>
            {
                ExecutePosTest(null as ConstructorInfo, new byte[] { 01, 00, 01, 02, 03, 04 }, getMethodAttr, returnType, paramTypes, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            });
        }


        [Fact]
        public void TestThrowsExceptionOnCreateTypeCalled()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(int);
            Type[] ctorParamTypes = new Type[] { typeof(int) };
            Type[] paramTypes = new Type[0];
            int expectedValue = TestLibrary.Generator.GetInt32();
            byte[] binaryAttr = new byte[6];
            ConstructorInfo con = typeof(PBMyAttribute2).GetConstructor(ctorParamTypes);
            binaryAttr[0] = 01;
            binaryAttr[1] = 00;
            for (int i = 0; i < binaryAttr.Length - 2; ++i)
            {
                binaryAttr[i + 2] = (byte)(expectedValue >> (8 * i) & 0xff);
            }
            TypeBuilder myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             returnType,
                                                             paramTypes);
            MethodBuilder myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                             getMethodAttr,
                                             typeof(int),
                                             paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);
            myPropertyBuilder.SetGetMethod(myMethodBuilder);
            myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myPropertyBuilder.SetCustomAttribute(con, binaryAttr); });
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class PBMyAttribute2 : Attribute
    {
        private int _value;

        public int Value
        {
            get { return _value; }
        }

        public PBMyAttribute2(int value)
        {
            _value = value;
        }

        public PBMyAttribute2() : this(0)
        { }
    }
}
