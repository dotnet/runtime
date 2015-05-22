// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodctor6
    {
        [Fact]
        public void PosTest1()
        {
            Type owner = typeof(MethodTestClass6);
            bool skipVisb = true;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos1method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), null, owner, skipVisb);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos1method" || attributes != (MethodAttributes.Public | MethodAttributes.Static) || dynamicMeth.CallingConvention != CallingConventions.Standard);
        }

        [Fact]
        public void PosTest2()
        {
            Type owner = typeof(MethodTestClass6);
            bool skipVisb = false;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos2method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { typeof(int), typeof(string) }, owner, skipVisb);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos2method" || attributes != (MethodAttributes.Public | MethodAttributes.Static) || dynamicMeth.CallingConvention != CallingConventions.Standard);
        }

        [Fact]
        public void PosTest3()
        {
            Type owner = typeof(MethodTestClass6);
            bool skipVisib = true;
            DynamicMethod dynamicMeth = new DynamicMethod("Pos3method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(string), new Type[] { typeof(int), typeof(string) }, owner, skipVisib);
            MethodAttributes attributes = dynamicMeth.Attributes;
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos3method" || attributes != (MethodAttributes.Static | MethodAttributes.Public) || dynamicMeth.CallingConvention != CallingConventions.Standard);
        }

        [Fact]
        public void NegTest1()
        {
            Type owner = typeof(MethodTestClass6);
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg1method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest2()
        {
            Type owner = typeof(Array);
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg2method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest3()
        {
            Type owner = typeof(MethodTestInterface6);
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg3method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest4()
        {
            Type[] genericTypes = typeof(MethodMyClass6<>).GetGenericArguments();
            Type owner = genericTypes[0];
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg4method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest5()
        {
            Type owner = typeof(MethodTestClass6);
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentNullException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod(null, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest6()
        {
            Type owner = null;
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg6method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, owner, skipVisbs[i]); });
            }
        }

        [Fact]
        public void NegTest7()
        {
            Type returnType = CreateType();
            Type owner = typeof(MethodTestClass6);
            bool[] skipVisbs = new bool[] { false, true };
            for (int i = 0; i < skipVisbs.Length; i++)
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg7method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, new Type[] { typeof(string) }, owner, skipVisbs[i]);
                });
            }
        }

        [Fact]
        public void NegTest8()
        {
            Type owner = typeof(MethodTestClass6);
            bool[] skipVisibs = new bool[] { false, true };
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg6method", MethodAttributes.Private | MethodAttributes.Virtual, CallingConventions.Standard, typeof(void), new Type[] { typeof(string) }, owner, skipVisibs[i]);
                });
            }
        }

        [Fact]
        public void NegTest9()
        {
            Type owner = typeof(MethodTestClass6);
            bool[] skipVisibs = new bool[] { false, true };
            for (int i = 0; i < skipVisibs.Length; i++)
            {
                Assert.Throws<NotSupportedException>(() =>
                {
                    DynamicMethod dynamicMeth = new DynamicMethod("Neg7method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.VarArgs, typeof(void), new Type[] { typeof(string) }, owner, skipVisibs[i]);
                });
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

    public class MethodTestClass6
    {
        private int _id = 0;
        public MethodTestClass6(int id)
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


    public interface MethodTestInterface6 { }


    public class MethodMyClass6<T> { }
}