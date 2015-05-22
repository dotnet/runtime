// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodInitLocals
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, null, typeof(TestMethodInitLocalsClass).GetTypeInfo().Module, true);
            dynamicmethod.InitLocals = true;
            Assert.False(!dynamicmethod.InitLocals);
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, null, typeof(TestMethodInitLocalsClass).GetTypeInfo().Module, true);
            dynamicmethod.InitLocals = false;
            Assert.False(dynamicmethod.InitLocals);
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, null, null, typeof(TestMethodInitLocalsClass).GetTypeInfo().Module, true);
            Assert.False(!dynamicmethod.InitLocals);
        }
    }

    public class TestMethodInitLocalsClass
    {
    }
}