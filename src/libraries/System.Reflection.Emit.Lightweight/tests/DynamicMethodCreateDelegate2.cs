// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodCreateDelegateStaticTests
    {
        private const string c_DYNAMIC_METHOD_NAME = "TestDynamicMethodA";
        private readonly Type _DYNAMIC_METHOD_OWNER_TYPE = typeof(TestCreateDelegateOwner2);
        private readonly Type _DYNAMIC_METHOD_OWNER_DERIVED_TYPE = typeof(TestCreateDelegateOwner2Derived);
        private const string c_FIELD_NAME = "_id";

        public Module CurrentModule
        {
            get
            {
                return typeof(DynamicMethodCreateDelegateStaticTests).GetTypeInfo().Assembly.ManifestModule;
            }
        }

        [Fact]
        public void PosTest1()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();
            int newId = 100;

            bool actualResult;

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            UseLikeStatic2 staticCallBack = (UseLikeStatic2)testDynMethod.CreateDelegate(typeof(UseLikeStatic2));
            actualResult = target.ID == staticCallBack(target, newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2Derived target = new TestCreateDelegateOwner2Derived();
            int newId = 100;

            bool actualResult;

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            UseLikeStatic2 staticCallBack = (UseLikeStatic2)testDynMethod.CreateDelegate(typeof(UseLikeStatic2));
            actualResult = target.ID == staticCallBack(target, newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();
            int newId = 100;

            bool actualResult;
            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(this.CurrentModule);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            UseLikeStatic2 staticCallBack = (UseLikeStatic2)testDynMethod.CreateDelegate(typeof(UseLikeStatic2));
            actualResult = target.ID == staticCallBack(target, newId);
            actualResult = (target.ID == newId) && actualResult;

            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void PosTest4()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2Derived target = new TestCreateDelegateOwner2Derived();
            int newId = 100;

            bool actualResult;

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(this.CurrentModule);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            UseLikeStatic2 staticCallBack = (UseLikeStatic2)testDynMethod.CreateDelegate(typeof(UseLikeStatic2));
            actualResult = target.ID == staticCallBack(target, newId);
            actualResult = (target.ID == newId) && actualResult;
            Assert.True(actualResult, "Failed to create delegate for dynamic method.");
        }

        [Fact]
        public void NegTest1()
        {
            DynamicMethod testDynMethod;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            Assert.Throws<InvalidOperationException>(() =>
            {
                UseLikeStatic2 staticCallBack = (UseLikeStatic2)testDynMethod.CreateDelegate(typeof(UseLikeStatic2));
            });
        }

        [Fact]
        public void NegTest2()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();


            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            Assert.Throws<ArgumentException>(() =>
            {
                InvalidRetType2 invalidCallBack = (InvalidRetType2)testDynMethod.CreateDelegate(typeof(InvalidRetType2));
            });
        }

        [Fact]
        public void NegTest3()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);

            Assert.Throws<ArgumentException>(() =>
            {
                WrongParamNumber2 invalidCallBack = (WrongParamNumber2)testDynMethod.CreateDelegate(typeof(WrongParamNumber2));
            });
        }

        [Fact]
        public void NegTest4()
        {
            DynamicMethod testDynMethod;
            FieldInfo fieldInfo;
            TestCreateDelegateOwner2 target = new TestCreateDelegateOwner2();

            fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                c_FIELD_NAME,
                                BindingFlags.NonPublic |
                                BindingFlags.Instance);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE);
            this.EmitDynMethodBody(testDynMethod, fieldInfo);
            Assert.Throws<ArgumentException>(() =>
            {
                InvalidParamType2 invalidCallBack = (InvalidParamType2)testDynMethod.CreateDelegate(typeof(InvalidParamType2));
            });
        }

        private DynamicMethod CreateDynMethod(Type dynMethodOwnerType)
        {
            Type retType = typeof(int);
            Type[] paramTypes = new Type[]
            {
            _DYNAMIC_METHOD_OWNER_TYPE,
            typeof(int)
            };

            return new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                  retType,
                                                  paramTypes,
                                                  dynMethodOwnerType);
        }

        private DynamicMethod CreateDynMethod(Module mod)
        {
            Type retType = typeof(int);
            Type[] paramTypes = new Type[]
            {
            _DYNAMIC_METHOD_OWNER_TYPE,
            typeof(int)
            };

            return new DynamicMethod(c_DYNAMIC_METHOD_NAME,
                                                  retType,
                                                  paramTypes,
                                                  mod,
                                                  true);
        }

        private void EmitDynMethodBody(DynamicMethod dynMethod, FieldInfo fld)
        {
            ILGenerator methodIL = dynMethod.GetILGenerator();

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, fld);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Stfld, fld);

            methodIL.Emit(OpCodes.Ret);
        }
    }

    internal class TestCreateDelegateOwner2
    {
        private int _id; //c_FIELD_NAME

        public TestCreateDelegateOwner2(int id)
        {
            _id = id;
        }
        public TestCreateDelegateOwner2() : this(0)
        {
        }

        public int ID
        {
            get { return _id; }
        }
    }


    internal class TestCreateDelegateOwner2Derived : TestCreateDelegateOwner2
    {
        public TestCreateDelegateOwner2Derived(int id)
            : base(id)
        {
        }

        public TestCreateDelegateOwner2Derived()
            : base()
        {
        }
    }

    internal delegate int UseLikeStatic2(TestCreateDelegateOwner2 owner, int id);
    internal delegate TestCreateDelegateOwner2 InvalidRetType2(TestCreateDelegateOwner2 owner, int id);
    internal delegate int WrongParamNumber2(TestCreateDelegateOwner2 owner, int id, int m);
    internal delegate int InvalidParamType2(int id, TestCreateDelegateOwner2 owner);
}