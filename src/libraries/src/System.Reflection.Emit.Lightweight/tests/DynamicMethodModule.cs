// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
