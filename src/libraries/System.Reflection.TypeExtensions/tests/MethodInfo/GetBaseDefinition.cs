// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;


namespace System.Reflection.Compatibility.UnitTests.MethodInfoTests
{
    internal class Binding_Flags
    {
        internal static BindingFlags LookupAll = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
        internal static BindingFlags Default = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        internal static BindingFlags ConstructorLookupAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    }

    public class Co4632GetBaseDefinition
    {
        [Fact]
        public void Test1()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;

            Object var = null;
            Object obj = null;

            // [A] Vanila: Class does not extend (other than Object)/implement

            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_a3");
            Assert.NotNull(type);
            Assert.Equal("Co4611_a3", type.Name);

            mInfo = type.GetMethod("MethodA", new Type[1] { typeof(System.String) });
            Assert.NotNull(mInfo);

            obj = new Co4611_a3();
            var = mInfo.Invoke(obj, (Object[])(new string[] { "test string" }));
            Assert.Equal("test string", (string)var);

            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal(mInfo, mInfo2);

            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);

            types = new Type[2];
            types[0] = typeof(System.String);
            types[1] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);
            Assert.NotNull(mInfo);

            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal(mInfo2, mInfo);
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
            // Get method: public void MethodA (Int32 i32)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal(mInfo, mInfo2);
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        // [B1] Abstract clss: abstract method obtained via abstract class;
        // abstract class Ab_Co4611_a1
        [Fact]
        public void Test2()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;

            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Ab_Co4611_a1");
            Assert.NotNull(type);
            Assert.Equal("Ab_Co4611_a1", type.Name);

            //GetMethod: public abstract void MethodA ()
            mInfo = type.GetMethod("MethodA", new Type[0]);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal(mInfo, mInfo2);
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);

            //GetMethod: public Int32 MethodA (Int32 i)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);

            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal(mInfo, mInfo2);
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        // [B3] Class extends an Abstract class:
        //    Method obtained via the child
        [Fact]
        public void Test3()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;

            // Extended class Co4611_X1
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_X1");
            Assert.NotNull(type);
            Assert.Equal("Co4611_X1", type.Name);

            //GetMethod: public abstract void MethodA ()
            mInfo = type.GetMethod("MethodA", new Type[0]);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type.GetTypeInfo().BaseType, mInfo2.DeclaringType);

            // [B4] child class derives from abstract super class which has valid impl for MethodA
            // 	  Obtain method via child class which overrides the implementation
            //GetMethod: public Int32 MethodA (Int32 i)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);

            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type.GetTypeInfo().BaseType, mInfo2.DeclaringType);
        }


        // [C] Class A inherits Class B; Both has implementation of MethodA.....
        //    Now, MethodInfo obtained via Class A
        [Fact]
        public void Test4()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_a6");
            Assert.NotNull(type);
            Assert.Equal("Co4611_a6", type.Name);

            //GetMethod: public Int32 MethodA (Int32 i)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        // [D] 2 unrelated classed, each has the same method
        [Fact]
        public void Test5()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_a4");
            Assert.NotNull(type);
            Assert.Equal("Co4611_a4", type.Name);

            //GetMethod: public Int32 MethodA (Int32 i)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        [Fact]
        public void Test6()
        {
            Type type = null;
            Type[] types = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_a5");
            Assert.NotNull(type);
            Assert.Equal("Co4611_a5", type.Name);

            //GetMethod: public Int32 MethodA (Int32 i)
            types = new Type[1];
            types[0] = typeof(System.Int32);
            mInfo = type.GetMethod("MethodA", types);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        // [E1] Class A implements Interface B
        // MethodInfo obtained for MethodA via Interface B
        [Fact]
        public void Test7()
        {
            Type type = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;
            // interface I_Co4611_a2
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.I_Co4611_a2");
            Assert.NotNull(type);
            Assert.Equal("I_Co4611_a2", type.Name);

            //GetMethod: public void MethodA ()
            mInfo = type.GetMethod("MethodA", new Type[0]);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(type, mInfo2.DeclaringType);
        }

        // [E2] Class A implements Interface B
        //    MethodInfo obtained for MethodA via Class A
        // class Co4611_X2 implements I_Co4611_a2
        [Fact]
        public void Test8()
        {
            Type type = null;
            MethodInfo mInfo = null;
            MethodInfo mInfo2 = null;
            type = Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_X2");
            Assert.NotNull(type);
            Assert.Equal("Co4611_X2", type.Name);

            //GetMethod: public void MethodA ()
            mInfo = type.GetMethod("MethodA", new Type[0]);
            Assert.NotNull(mInfo);
            mInfo2 = mInfo.GetBaseDefinition();
            Assert.Equal("MethodA", mInfo2.Name);
            Assert.Equal(Type.GetType("System.Reflection.Compatibility.UnitTests.MethodInfoTests.Co4611_X2"), mInfo2.DeclaringType);
        }
    }


    // classes to refelct upon: 
    // Scenario [A]
    internal class Co4611_a3
    {
        public String name = null;
        public Int32 id = -1;
        public UInt32 uid = 1;

        private void MethodA() { }
        public String MethodA(String str) { name = str; return str; }
        public void MethodA(Int32 i32) { throw new NotSupportedException("NYI...(1)"); }
        private void MethodA(UInt32 ui32) { throw new NotSupportedException("NYI...(2)"); }
        public Int32 MethodA(String str, Int32 i32) { name = str; id = i32; return id; }
        public UInt32 MethodA(String str, UInt32 ui32) { name = str; uid = ui32; return uid; }
    }

    // Scenario [B]

    internal abstract class Ab_Co4611_a1
    {
        public abstract void MethodA();
        public virtual Int32 MethodA(Int32 i)
        {
            return i;
        }
    }

    internal class Co4611_X1 : Ab_Co4611_a1
    {
        public override void MethodA() { Debug.WriteLine("Inside extended class Co4611_X1.MethodA"); }

        public override Int32 MethodA(Int32 i)
        {
            return 0;
        }
    }

    // Scenario [C]

    internal class Co4611_a6
    {
        public virtual Int32 MethodA(Int32 i)
        {
            return i;
        }
    }

    internal class Co4611_X3 : Co4611_a6
    {
        public override Int32 MethodA(Int32 i)
        {
            return 0;
        }
    }

    // Scenario [D]
    internal class Co4611_a4
    {
        public Int32 MethodA(Int32 i) { return i; }
    }

    internal class Co4611_a5
    {
        public Int32 MethodA(Int32 i) { return 0; }
    }

    // Scenario [E]
    internal interface I_Co4611_a2
    {
        void MethodA();
    }

    internal class Co4611_X2 : I_Co4611_a2
    {
        public void MethodA()
        {
            Debug.WriteLine("Inside interface implementation Co4611_X2.MethodA");
        }
    }
}