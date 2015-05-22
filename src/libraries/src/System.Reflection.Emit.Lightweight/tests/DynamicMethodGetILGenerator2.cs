// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodGetILGenerator2
    {
        private const string c_DYNAMIC_METHOD_NAME = "TestDynamicMethodA";
        private readonly Type _DYNAMIC_METHOD_OWNER_TYPE = typeof(TestILGeneratorOwner2);
        private const string c_FIELD_NAME = "_id";

        public Module CurrentModule
        {
            get
            {
                return typeof(DynamicMethodGetILGenerator2).GetTypeInfo().Assembly.ManifestModule;
            }
        }

        public void PosTest1()
        {
            DynamicMethod testDynMethod;
            TestILGeneratorOwner2 target = new TestILGeneratorOwner2();
            int newId = 100;

            bool actualResult = false;

            FieldInfo fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                                c_FIELD_NAME,
                                                BindingFlags.Instance |
                                                BindingFlags.NonPublic);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE, true);

            ILGenerator dynMethodIL = testDynMethod.GetILGenerator();
            this.EmitDynMethodBody(dynMethodIL, fieldInfo);

            ILUseLikeInstance2 instanceCallBack = (ILUseLikeInstance2)testDynMethod.CreateDelegate(
                                                                                                typeof(ILUseLikeInstance2),
                                                                                                target);

            actualResult = this.VerifyILGenerator(instanceCallBack, target, newId);
            Assert.True(actualResult, "Failed to get IL Generator for dynamic method.");
        }

        public void PosTest2()
        {
            DynamicMethod testDynMethod;
            TestILGeneratorOwner2 target = new TestILGeneratorOwner2();
            int newId = 100;

            bool actualResult;

            FieldInfo fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                                c_FIELD_NAME,
                                                BindingFlags.Instance |
                                                BindingFlags.NonPublic);

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE, false);

            ILGenerator dynMethodIL = testDynMethod.GetILGenerator();
            this.EmitDynMethodBody(dynMethodIL, fieldInfo);

            ILUseLikeInstance2 instanceCallBack = (ILUseLikeInstance2)testDynMethod.CreateDelegate(
                                                                                                typeof(ILUseLikeInstance2),
                                                                                                target);

            actualResult = this.VerifyILGenerator(instanceCallBack, target, newId);
            Assert.True(actualResult, "Failed to get IL Generator for dynamic method.");
        }

        public void PosTest3()
        {
            DynamicMethod testDynMethod;
            TestILGeneratorOwner2 target = new TestILGeneratorOwner2();
            int newId = 100;

            bool actualResult;


            FieldInfo fieldInfo = _DYNAMIC_METHOD_OWNER_TYPE.GetField(
                                                c_FIELD_NAME,
                                                BindingFlags.Instance |
                                                BindingFlags.NonPublic);

            testDynMethod = this.CreateDynMethod(this.CurrentModule, true);

            ILGenerator dynMethodIL = testDynMethod.GetILGenerator();
            this.EmitDynMethodBody(dynMethodIL, fieldInfo);

            ILUseLikeInstance2 instanceCallBack = (ILUseLikeInstance2)testDynMethod.CreateDelegate(
                                                                                                typeof(ILUseLikeInstance2),
                                                                                                target);

            actualResult = this.VerifyILGenerator(instanceCallBack, target, newId);
            Assert.True(actualResult, "Failed to get IL Generator for dynamic method.");
        }

        private DynamicMethod CreateDynMethod(Type dynMethodOwnerType, bool skipVisibility)
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
                                                  dynMethodOwnerType,
                                                  skipVisibility);
        }

        private DynamicMethod CreateDynMethod(Module mod, bool skipVisibility)
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
                                                  skipVisibility);
        }

        private void EmitDynMethodBody(ILGenerator methodIL, FieldInfo fld)
        {
            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldfld, fld);

            methodIL.Emit(OpCodes.Ldarg_0);
            methodIL.Emit(OpCodes.Ldarg_1);
            methodIL.Emit(OpCodes.Stfld, fld);

            methodIL.Emit(OpCodes.Ret);
        }

        private bool VerifyILGenerator(ILUseLikeInstance2 instanceCallBack,
                                                  TestILGeneratorOwner2 target,
                                                  int newId)
        {
            bool retVal = false;
            retVal = target.ID == instanceCallBack(newId);
            retVal = (target.ID == newId) && retVal;
            return retVal;
        }
    }

    internal class TestILGeneratorOwner2
    {
        private int _id; //c_FIELD_NAME

        public TestILGeneratorOwner2(int id)
        {
            _id = id;
        }
        public TestILGeneratorOwner2() : this(0)
        {
        }

        public int ID
        {
            get { return _id; }
        }
    }

    internal delegate int ILUseLikeInstance2(int id);
}