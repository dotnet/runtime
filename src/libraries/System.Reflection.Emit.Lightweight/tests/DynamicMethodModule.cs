// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodModule
    {
        public const string c_DYNAMICMETHODNAME = "MethodName";

        [Fact]
        public void PosTest1()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(String).GetTypeInfo().Module);
            Module module = dynamicmethod.Module;
            Assert.NotNull(module);
        }

        [Fact]
        public void PosTest2()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(DynamicMethodModule).GetTypeInfo().Module);
            Module module = dynamicmethod.Module;
            Assert.NotNull(module);
        }

        [Fact]
        public void PosTest3()
        {
            DynamicMethod dynamicmethod = new DynamicMethod(c_DYNAMICMETHODNAME, null, null, typeof(MethodModuleTestClass).GetTypeInfo().Module);
            Module module = dynamicmethod.Module;
            Assert.NotNull(module);
        }
    }

    public class MethodModuleTestClass
    {
    }
}
