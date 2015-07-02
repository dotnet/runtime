// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest11
    {
        private const string DynamicAssemblyName = "TestDynamicAssembly";
        private const string DynamicModuleName = "TestDynamicModule";
        private const string DynamicTypeName = "TestDynamicType";
        private const string DynamicFieldName = "TestDynamicFieldA";
        private const string DynamicPropertyName = "TestDynamicProperty";
        private const string DynamicMethodName = "DynamicMethodA";

        private enum Colors
        {
            Red = 0,
            Green = 1,
            Blue = 2
        }

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
        public void TestWithIntegerType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(int);
            Type[] paramTypes = new Type[0];
            int defaultValue = TestLibrary.Generator.GetInt32();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is int);
            Assert.Equal((int)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithBoolType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(bool);
            Type[] paramTypes = new Type[0];
            bool defaultValue = 0 == (TestLibrary.Generator.GetInt32() & 1);
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is bool);
            Assert.Equal((bool)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithSByteType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(SByte);
            Type[] paramTypes = new Type[0];
            SByte defaultValue = (SByte)(TestLibrary.Generator.GetInt32() % (SByte.MaxValue + 1));
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is sbyte);
            Assert.Equal((sbyte)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithShortType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(short);
            Type[] paramTypes = new Type[0];
            Int16 defaultValue = TestLibrary.Generator.GetInt16();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is short);
            Assert.Equal((short)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithLongType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(long);
            Type[] paramTypes = new Type[0];
            long defaultValue = TestLibrary.Generator.GetInt64();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is long);
            Assert.Equal((long)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithByteType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(byte);
            Type[] paramTypes = new Type[0];
            byte defaultValue = TestLibrary.Generator.GetByte();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is byte);
            Assert.Equal((byte)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithUShortType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(ushort);
            Type[] paramTypes = new Type[0];
            ushort defaultValue = (ushort)(TestLibrary.Generator.GetInt32() % (ushort.MaxValue + 1));
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is ushort);
            Assert.Equal((ushort)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithUIntType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(uint);
            Type[] paramTypes = new Type[0];
            uint defaultValue = (ushort)(TestLibrary.Generator.GetInt64() % ((long)uint.MaxValue + 1));
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is uint);
            Assert.Equal((uint)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithULongType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(ulong);
            Type[] paramTypes = new Type[0];
            ulong defaultValue = (ulong)long.MaxValue + (ulong)TestLibrary.Generator.GetInt64();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is ulong);
            Assert.Equal((ulong)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithFloatType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(float);
            Type[] paramTypes = new Type[0];
            float defaultValue = TestLibrary.Generator.GetSingle();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is float);
            Assert.Equal((float)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithDoubleType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(double);
            Type[] paramTypes = new Type[0];
            double defaultValue = TestLibrary.Generator.GetDouble();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is double);
            Assert.Equal((double)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithDateTimeType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(DateTime);
            Type[] paramTypes = new Type[0];
            DateTime defaultValue = DateTime.Now;
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is DateTime);
            Assert.Equal((DateTime)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithCharType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(char);
            Type[] paramTypes = new Type[0];
            char defaultValue = TestLibrary.Generator.GetChar();
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is char);
            Assert.Equal((char)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithStringType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;
            Type returnType = typeof(string);
            Type[] paramTypes = new Type[0];
            string defaultValue = TestLibrary.Generator.GetString(true, 0, 260);
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is string);
            Assert.Equal((string)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithEnumType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;

            Type returnType = typeof(Colors);
            Type[] paramTypes = new Type[0];
            Colors defaultValue = (Colors)(TestLibrary.Generator.GetInt32() % 3);
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.True(actualValue is Colors);
            Assert.Equal((Colors)actualValue, defaultValue);
        }

        [Fact]
        public void TestWithObjectType()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;

            Type returnType = typeof(object);
            Type[] paramTypes = new Type[0];
            object defaultValue = null;
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
            Assert.Equal(defaultValue, (object)actualValue);
        }

        private object ExecutePosTest(object defaultValue, MethodAttributes getMethodAttr, Type returnType, Type[] paramTypes, BindingFlags bindingAttr)
        {
            TypeBuilder myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                                             PropertyAttributes.HasDefault,
                                                                             returnType, null);
            myPropertyBuilder.SetConstant(defaultValue);

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
            return myProperty.GetConstantValue();
        }

        [Fact]
        public void NegTest1()
        {
            TypeBuilder myTypeBuilder;
            PropertyBuilder myPropertyBuilder;
            MethodBuilder myMethodBuilder;
            Type[] paramTypes = new Type[]
            {
               typeof(int)
            };

            myTypeBuilder = GetTypeBuilder(TypeAttributes.Class | TypeAttributes.Public);
            myPropertyBuilder = myTypeBuilder.DefineProperty(DynamicPropertyName,
                                                             PropertyAttributes.None,
                                                             typeof(int),
                                                             new Type[0]);
            myMethodBuilder = myTypeBuilder.DefineMethod(DynamicMethodName,
                                             MethodAttributes.Public,
                                             CallingConventions.HasThis,
                                             typeof(int),
                                             paramTypes);
            ILGenerator methodILGenerator = myMethodBuilder.GetILGenerator();

            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);
            Type myType = myTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { myPropertyBuilder.SetConstant(1); });
        }

        [Fact]
        public void PosTest18()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;

            Type returnType = typeof(object);
            Type[] paramTypes = new Type[0];
            object defaultValue = "TestCase";
            object actualValue;

            actualValue = ExecutePosTest(
                                    defaultValue,
                                    getMethodAttr,
                                    returnType,
                                    paramTypes,
                                    BindingFlags.Public |
                                    BindingFlags.Instance |
                                    BindingFlags.NonPublic |
                                    BindingFlags.Static);
        }

        [Fact]
        public void NegTest3()
        {
            MethodAttributes getMethodAttr = MethodAttributes.Public |
                                             MethodAttributes.SpecialName |
                                             MethodAttributes.HideBySig;

            Type returnType = typeof(decimal);
            Type[] paramTypes = new Type[0];
            decimal defaultValue = (decimal)TestLibrary.Generator.GetSingle();
            object actualValue;

            Assert.Throws<ArgumentException>(() =>
            {
                actualValue = ExecutePosTest(defaultValue, getMethodAttr, returnType, paramTypes, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);
            });
        }
    }
}
