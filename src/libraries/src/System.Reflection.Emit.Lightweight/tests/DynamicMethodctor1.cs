// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodctor1
    {
        [Fact]
        public void PosTest1()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos1method", typeof(void), null, mod);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos1method");
        }

        [Fact]
        public void PosTest2()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos2method", typeof(void), new Type[] { typeof(int), typeof(string) }, mod);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos2method");
        }

        [Fact]
        public void PosTest3()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos3method", typeof(string), null, mod);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos3method");
        }

        [Fact]
        public void PosTest4()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos4method", typeof(string), new Type[] { typeof(int), typeof(string) }, mod);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos4method");
        }

        [Fact]
        public void PosTest5()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            DynamicMethod dynamicMeth = new DynamicMethod(string.Empty, typeof(string), new Type[] { typeof(int), typeof(string) }, mod);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != string.Empty);
        }

        [Fact]
        public void NegTest1()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg1method", typeof(void), new Type[] { null, typeof(string) }, mod); });
        }

        [Fact]
        public void NegTest2()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod(null, typeof(void), new Type[] { typeof(string) }, mod); });
        }

        [Fact]
        public void NegTest3()
        {
            Module mod = null;
            Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg3method", typeof(void), new Type[] { typeof(string) }, mod); });
        }

        [Fact]
        public void NegTest4()
        {
            Module mod = typeof(MethodTestClass).GetTypeInfo().Module;
            Type tpA = CreateType();
            Assert.Throws<NotSupportedException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg4method", tpA, new Type[] { typeof(string) }, mod); });
        }

        private static Type CreateType()
        {
            AssemblyName assemName = new AssemblyName("assemName");
            AssemblyBuilder myAssemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModB = Utilities.GetModuleBuilder(myAssemBuilder, "Module1");
            TypeBuilder myTypeB = myModB.DefineType("testType");
            return myTypeB.MakeByRefType();
        }
    }

    public class MethodTestClass
    {
        public MethodTestClass() { }
    }
}