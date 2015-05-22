// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodGetBaseDefinition
    {
        private const string c_DYNAMIC_METHOD_NAME = "TestDynamicMethodA";
        private readonly Type _DYNAMIC_METHOD_OWNER_TYPE = typeof(TestBaseDefinitionOwner);
        private const string c_FIELD_NAME = "_id";

        public Module CurrentModule
        {
            get
            {
                return typeof(DynamicMethodGetBaseDefinition).GetTypeInfo().Assembly.ManifestModule;
            }
        }

        [Fact]
        public void PosTest1()
        {
            DynamicMethod testDynMethod;

            bool actualResult;

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE, true);

            MethodInfo testDynMethodInfo = testDynMethod.GetBaseDefinition();

            actualResult = testDynMethodInfo == testDynMethod;
            Assert.True(actualResult, "Failed to get base definition of dynamic method.");
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod testDynMethod;

            bool actualResult;

            testDynMethod = this.CreateDynMethod(_DYNAMIC_METHOD_OWNER_TYPE, false);

            MethodInfo testDynMethodInfo = testDynMethod.GetBaseDefinition();

            actualResult = testDynMethodInfo == testDynMethod;
            Assert.True(actualResult, "Failed to get base definition of dynamic method.");
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod testDynMethod;

            bool actualResult;
            testDynMethod = this.CreateDynMethod(this.CurrentModule, true);

            MethodInfo testDynMethodInfo = testDynMethod.GetBaseDefinition();

            actualResult = testDynMethodInfo == testDynMethod;
            Assert.True(actualResult, "Failed to get base definition of dynamic method.");
        }

        [Fact]
        public void PosTest4()
        {
            DynamicMethod testDynMethod;

            bool actualResult;
            testDynMethod = this.CreateDynMethod(this.CurrentModule, false);

            MethodInfo testDynMethodInfo = testDynMethod.GetBaseDefinition();

            actualResult = testDynMethodInfo == testDynMethod;
            Assert.True(actualResult, "Failed to get base definition of dynamic method.");
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
    }

    internal class TestBaseDefinitionOwner
    {
        private int _id; //c_FIELD_NAME

        public TestBaseDefinitionOwner(int id)
        {
            _id = id;
        }
        public TestBaseDefinitionOwner() : this(0)
        {
        }

        public int ID
        {
            get { return _id; }
        }
    }
}