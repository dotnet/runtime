// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Lightweight.Tests
{
    public class DynamicMethodctor2
    {
        [Fact]
        public void PosTest1()
        {
            Type owner = typeof(MethodTestClass2);
            DynamicMethod dynamicMeth = new DynamicMethod("Pos1method", typeof(void), null, owner);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos1method");
        }

        [Fact]
        public void PosTest2()
        {
            Type owner = typeof(MethodTestClass2);
            DynamicMethod dynamicMeth = new DynamicMethod("Pos2method", typeof(void), new Type[] { typeof(int), typeof(string) }, owner);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos2method");
        }

        [Fact]
        public void PosTest3()
        {
            Type owner = typeof(MethodTestClass2);
            DynamicMethod dynamicMeth = new DynamicMethod("Pos3method", typeof(string), null, owner);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos3method");
        }

        [Fact]
        public void PosTest4()
        {
            Type owner = typeof(MethodTestClass2);
            DynamicMethod dynamicMeth = new DynamicMethod("Pos4method", typeof(string), new Type[] { typeof(int), typeof(string) }, owner);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != "Pos4method");
        }

        [Fact]
        public void PosTest5()
        {
            Type owner = typeof(MethodTestClass2);
            DynamicMethod dynamicMeth = new DynamicMethod(string.Empty, typeof(string), new Type[] { typeof(int), typeof(string) }, owner);
            Assert.False(dynamicMeth == null || dynamicMeth.Name != string.Empty);
        }

        [Fact]
        public void NegTest1()
        {
            Type owner = typeof(MethodTestClass2);
            Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg1method", typeof(void), new Type[] { null, typeof(string) }, owner); });
        }

        [Fact]
        public void NegTest2()
        {
            Type owner = typeof(Array);
            Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg2method", typeof(void), new Type[] { null, typeof(string) }, owner); });
        }

        [Fact]
        public void NegTest3()
        {
            Type owner = typeof(MethodTestInterface);
            Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg3method", typeof(void), new Type[] { null, typeof(string) }, owner); });
        }

        [Fact]
        public void NegTest4()
        {
            Type[] genericTypes = typeof(MethodMyClass<>).GetGenericArguments();
            Type owner = genericTypes[0];
            Assert.Throws<ArgumentException>(() => { DynamicMethod dynamicMeth = new DynamicMethod("Neg4method", typeof(void), new Type[] { null, typeof(string) }, owner); });
        }

        [Fact]
        public void NegTest5()
        {
            Type owner = typeof(MethodTestClass2);
            Assert.Throws<ArgumentNullException>(() => { DynamicMethod dynamicMeth = new DynamicMethod(null, typeof(void), new Type[] { typeof(string) }, owner); });
        }

        [Fact]
        public void NegTest6()
        {
            Type owner = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                DynamicMethod dynamicMeth = new DynamicMethod("Neg6method", typeof(void), new Type[] { typeof(string) }, owner);
            });
        }

        [Fact]
        public void NegTest7()
        {
            Type returnType = CreateType();
            Type owner = typeof(MethodTestClass2);
            Assert.Throws<NotSupportedException>(() =>
            {
                DynamicMethod dynamicMeth = new DynamicMethod("Neg7method", returnType, new Type[] { typeof(string) }, owner);
            });
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

    public class MethodTestClass2
    {
        private int _id = 0;
        public MethodTestClass2(int id)
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


    public interface MethodTestInterface { }


    public class MethodMyClass<T> { }
}