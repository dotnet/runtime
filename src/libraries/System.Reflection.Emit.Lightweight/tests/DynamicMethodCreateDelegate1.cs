// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodCreateDelegateTests
    {
        private const string c_DYNAMIC_METHOD_NAME = "TestDynamicMethodA";
        private readonly Type _DYNAMIC_METHOD_OWNER_TYPE = typeof(TestCreateDelegateOwner1);
        private readonly Type _DYNAMIC_METHOD_OWNER_DERIVED_TYPE = typeof(TestCreateDelegateOwner1Derived);
        private const string c_FIELD_NAME = "_id";

        public Module CurrentModule
        {
            get
            {
                return typeof(DynamicMethodCreateDelegateTests).GetTypeInfo().Assembly.ManifestModule;
            }
        }

        [Fact]
        public void PosTest1()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();
            int newId = 100;

            bool actualResult;
            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(
                                                                                           typeof(UseLikeInstance),
                                                                                           target);
            actualResult = target.ID == instanceCallBack(newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1Derived target = new TestCreateDelegateOwner1Derived();
            int newId = 100;

            bool actualResult;

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(
                                                                                           typeof(UseLikeInstance),
                                                                                           target);
            actualResult = target.ID == instanceCallBack(newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method");
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();
            int newId = 100;

            bool actualResult;

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  this.CurrentModule,
                                                                  true);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(
                                                                                           typeof(UseLikeInstance),
                                                                                           target);
            actualResult = target.ID == instanceCallBack(newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method");
        }

        [Fact]
        public void PosTest4()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1Derived target = new TestCreateDelegateOwner1Derived();
            int newId = 100;

            bool actualResult;
            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_DERIVED_TYPE,
                                                                  true);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(
                                                                                           typeof(UseLikeInstance),
                                                                                           target);
            actualResult = target.ID == instanceCallBack(newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void NegTest1()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();
            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            Assert.Throws<InvalidOperationException>(() =>
            {
                UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(typeof(UseLikeInstance), target);
            });
        }

        [Fact]
        public void NegTest2()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            string target = "foo";

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            Assert.Throws<ArgumentException>(() =>
            {
                UseLikeInstance instanceCallBack = (UseLikeInstance)testDynMethod.CreateDelegate(typeof(UseLikeInstance), target);
            });
        }

        [Fact]
        public void NegTest3()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);


            Assert.Throws<ArgumentException>(() => { InvalidRetType invalidCallBack = (InvalidRetType)testDynMethod.CreateDelegate(typeof(InvalidRetType), target); });
        }

        [Fact]
        public void NegTest4()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            Assert.Throws<ArgumentException>(() => { WrongParamNumber invalidCallBack = (WrongParamNumber)testDynMethod.CreateDelegate(typeof(WrongParamNumber), target); });
        }

        [Fact]
        public void NegTest5()
        {
            DynamicMethod testDynMethod;
            Type retType;
            Type[] paramTypes;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner1 target = new TestCreateDelegateOwner1();

            retType = typeof(int);
            paramTypes = new Type[]
            {
                _DYNAMIC_METHOD_OWNER_TYPE,
                typeof(int)
            };

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                                  retType,
                                                                  paramTypes,
                                                                  _DYNAMIC_METHOD_OWNER_TYPE);

            ILGenerator testDynMethodIL = testDynMethod.GetILGenerator();

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ldarg_0);
            testDynMethodIL.Emit(OpCodes.Ldarg_1);
            testDynMethodIL.Emit(OpCodes.Stfld, fieldInfo);

            testDynMethodIL.Emit(OpCodes.Ret);

            Assert.Throws<ArgumentException>(() => { InvalidParamType invalidCallBack = (InvalidParamType)testDynMethod.CreateDelegate(typeof(InvalidParamType), target); });
        }
    }

    internal class TestCreateDelegateOwner1
    {
        private int _id; //c_FIELD_NAME

        public TestCreateDelegateOwner1(int id)
        {
            _id = id;
        }
        public TestCreateDelegateOwner1() : this(0)
        {
        }

        public int ID
        {
            get { return _id; }
        }
    }

    internal class TestCreateDelegateOwner1Derived : TestCreateDelegateOwner1
    {
        public TestCreateDelegateOwner1Derived(int id)
            : base(id)
        {
        }

        public TestCreateDelegateOwner1Derived()
            : base()
        {
        }
    }

    internal delegate int UseLikeInstance(int id);
    internal delegate TestCreateDelegateOwner1 InvalidRetType(int id);
    internal delegate int WrongParamNumber(int id, int m);
    internal delegate int InvalidParamType(TestCreateDelegateOwner1 owner);
}