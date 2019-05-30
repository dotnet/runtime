// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static partial class RuntimeHelpersTests
    {
        [Fact]
        public static void GetHashCodeTest()
        {
            // Int32 RuntimeHelpers.GetHashCode(Object)
            object obj1 = new object();
            int h1 = RuntimeHelpers.GetHashCode(obj1);
            int h2 = RuntimeHelpers.GetHashCode(obj1);

            Assert.Equal(h1, h2);

            object obj2 = new object();
            int h3 = RuntimeHelpers.GetHashCode(obj2);
            Assert.NotEqual(h1, h3); // Could potentially clash but very unlikely

            int i123 = 123;
            int h4 = RuntimeHelpers.GetHashCode(i123);
            Assert.NotEqual(i123.GetHashCode(), h4);

            int h5 = RuntimeHelpers.GetHashCode(null);
            Assert.Equal(h5, 0);
        }

        public struct TestStruct
        {
            public int i1;
            public int i2;
            public override bool Equals(object obj)
            {
                if (!(obj is TestStruct))
                    return false;

                TestStruct that = (TestStruct)obj;

                return i1 == that.i1 && i2 == that.i2;
            }

            public override int GetHashCode() => i1 ^ i2;
        }

        [Fact]
        public static unsafe void GetObjectValue()
        {
            // Object RuntimeHelpers.GetObjectValue(Object)
            TestStruct t = new TestStruct() { i1 = 2, i2 = 4 };
            object tOV = RuntimeHelpers.GetObjectValue(t);
            Assert.Equal(t, (TestStruct)tOV);

            object o = new object();
            object oOV = RuntimeHelpers.GetObjectValue(o);
            Assert.Equal(o, oOV);

            int i = 3;
            object iOV = RuntimeHelpers.GetObjectValue(i);
            Assert.Equal(i, (int)iOV);
        }

        [Fact]
        public static unsafe void OffsetToStringData()
        {
            // RuntimeHelpers.OffsetToStringData
            char[] expectedValues = new char[] { 'a', 'b', 'c', 'd', 'e', 'f' };
            string s = "abcdef";

            fixed (char* values = s) // Compiler will use OffsetToStringData with fixed statements
            {
                for (int i = 0; i < expectedValues.Length; i++)
                {
                    Assert.Equal(expectedValues[i], values[i]);
                }
            }
        }

        [Fact]
        public static void InitializeArray()
        {
            // Void RuntimeHelpers.InitializeArray(Array, RuntimeFieldHandle)
            char[] expected = new char[] { 'a', 'b', 'c' }; // Compiler will use RuntimeHelpers.InitializeArrary these
        }

        [Fact]
        public static void RunClassConstructor()
        {
            RuntimeTypeHandle t = typeof(HasCctor).TypeHandle;
            RuntimeHelpers.RunClassConstructor(t);
            Assert.Equal(HasCctorReceiver.S, "Hello");
            return;
        }

        internal class HasCctor
        {
            static HasCctor()
            {
                HasCctorReceiver.S = "Hello" + (Guid.NewGuid().ToString().Substring(string.Empty.Length, 0));  // Make sure the preinitialization optimization doesn't eat this.
            }
        }

        internal class HasCctorReceiver
        {
            public static string S;
        }

        [Fact]
        public static void PrepareMethod()
        {
            foreach (MethodInfo m in typeof(RuntimeHelpersTests).GetMethods())
                RuntimeHelpers.PrepareMethod(m.MethodHandle);

            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(default(RuntimeMethodHandle)));
            Assert.ThrowsAny<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(IList).GetMethod("Add").MethodHandle));
        }

        [Fact]
        public static void PrepareGenericMethod()
        {
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(default(RuntimeMethodHandle), null));

            //
            // Type instantiations
            //

            // Generic definition with instantiation is valid
            RuntimeHelpers.PrepareMethod(typeof(List<>).GetMethod("Add").MethodHandle, 
                new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle });

            // Instantiated method without instantiation is valid
            RuntimeHelpers.PrepareMethod(typeof(List<int>).GetMethod("Add").MethodHandle,
                null);

            // Generic definition without instantiation is invalid
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(List<>).GetMethod("Add").MethodHandle,
                null));

            // Wrong instantiation
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(List<>).GetMethod("Add").MethodHandle,
                new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle, typeof(TestStruct).TypeHandle }));

            //
            // Method instantiations
            //

            // Generic definition with instantiation is valid
            RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize").MethodHandle,
                new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle });

            // Instantiated method without instantiation is valid
            RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize")
                    .MakeGenericMethod(new Type[] { typeof(TestStruct) }).MethodHandle,
                null);

            // Generic definition without instantiation is invalid
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize").MethodHandle,
                null));

            // Wrong instantiation
            Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize").MethodHandle,
                new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle, typeof(TestStruct).TypeHandle }));
        }

        [Fact]
        public static void PrepareDelegate()
        {
            RuntimeHelpers.PrepareDelegate((Action)(() => { }));
            RuntimeHelpers.PrepareDelegate((Func<int>)(() => 1) + (Func<int>)(() => 2));
            RuntimeHelpers.PrepareDelegate(null);
        }
    }

    public struct Age
    {
        public int years;
        public int months;
    }

    public class FixedClass
    {
        [FixedAddressValueType]
        public static Age FixedAge;

        public static unsafe IntPtr AddressOfFixedAge()
        {
            fixed (Age* pointer = &FixedAge)
            {
                return (IntPtr)pointer;
            }
        }
    }

    public static partial class RuntimeHelpersTests
    {
        [Fact]
        public static void FixedAddressValueTypeTest()
        {
            // Get addresses of static Age fields.
            IntPtr fixedPtr1 = FixedClass.AddressOfFixedAge();

            // Garbage collection.
            GC.Collect(3, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Get addresses of static Age fields after garbage collection.
            IntPtr fixedPtr2 = FixedClass.AddressOfFixedAge();

            Assert.Equal(fixedPtr1, fixedPtr2);
        }
    }
}
