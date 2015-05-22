// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodctor3
    {
        [Fact]
        public void PosTest1()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool skipVisb = true;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos1method", typeof(int), new Type[] { typeof(int) }, mod, skipVisb);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos1method" || attributes != (MethodAttributes.Public | MethodAttributes.Static));
        }

        [Fact]
        public void PosTest2()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool skipVisb = false;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos2method", typeof(void), new Type[] { typeof(int), typeof(string) }, mod, skipVisb);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos2method" || attributes != (MethodAttributes.Public | MethodAttributes.Static));
        }

        [Fact]
        public void PosTest3()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool skipVisib = true;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos3method", typeof(string), new Type[] { typeof(int), typeof(string) }, mod, skipVisib);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos3method" || attributes != (MethodAttributes.Static | MethodAttributes.Public));
        }

        [Fact]
        public void NegTest1()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool[] skipVisibs = new bool[] { false, true };
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg1method", typeof(void), new Type[] { null, typeof(string) }, mod, skipVisibs[i]); });
            }
        }

        [Fact]
        public void NegTest2()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool[] skipVisibs = new bool[] { false, true };
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod(null, typeof(void), new Type[] { typeof(string) }, mod, skipVisibs[i]); });
            }
        }

        [Fact]
        public void NegTest3()
        {
            Module mod = null;
            bool[] skipVisibs = new bool[] { false, true };
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg3method", typeof(void), new Type[] { typeof(string) }, mod, skipVisibs[i]); });
            }
        }

        [Fact]
        public void NegTest4()
        {
            Module mod = typeof(MethodTestClass3).GetTypeInfo().Module;
            bool[] skipVisibs = new bool[] { false, true };
            Type tpA = CreateType();
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<NotSupportedException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg4method", tpA, new Type[] { typeof(string) }, mod, skipVisibs[i]); });
            }
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

    public class MethodTestClass3
    {
        private int _id = 0;
        public MethodTestClass3(int id)
        {
            _id = id;
        }
        public int ID
        {
            get
            {
                return _id;
            }
        }
    }
}