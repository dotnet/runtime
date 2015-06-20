// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderTest : Attribute
    {
        public string TestString
        {
            get
            {
                return TestStringField;
            }
            set
            {
                TestStringField = value;
            }
        }

        public int TestInt32
        {
            get
            {
                return TestInt;
            }
            set
            {
                TestInt = value;
            }
        }

        public string GetOnlyString
        {
            get
            {
                return GetString;
            }
        }

        public int GetOnlyInt32
        {
            get
            {
                return GetInt;
            }
        }

        public string TestStringField;
        public int TestInt;
        public string GetString;
        public int GetInt;

        public CustomAttributeBuilderTest()
        {
        }

        public CustomAttributeBuilderTest(string getOnlyString, int getOnlyInt32)
        {
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }

        public CustomAttributeBuilderTest(string testString, int testInt32, string getOnlyString, int getOnlyInt32)
        {
            TestStringField = testString;
            TestInt = testInt32;
            GetString = getOnlyString;
            GetInt = getOnlyInt32;
        }
    }

    public class MethodBuilderSetCustomAttribute1
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
        private const MethodAttributes TestMethodAttributes = MethodAttributes.Public | MethodAttributes.Static;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;
        private const int ByteArraySize = 128;

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);

                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);

                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] ctorParams = new Type[] { };
            byte[] binaryAttribute = new byte[ByteArraySize];

            TestLibrary.Generator.GetBytes(binaryAttribute);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            builder.SetCustomAttribute(typeof(CustomAttributeBuilderTest).GetConstructor(ctorParams), binaryAttribute);
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            byte[] binaryAttribute = new byte[ByteArraySize];

            TestLibrary.Generator.GetBytes(binaryAttribute);

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.Throws<ArgumentNullException>(() => { builder.SetCustomAttribute(null, binaryAttribute); });
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            Type[] ctorParams = new Type[] { };

            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.Throws<ArgumentNullException>(() => { builder.SetCustomAttribute(typeof(CustomAttributeBuilderTest).GetConstructor(ctorParams), null); });
        }
    }
}
