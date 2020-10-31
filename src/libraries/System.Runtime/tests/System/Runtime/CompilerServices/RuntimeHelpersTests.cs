// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static class RuntimeHelpersTests
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
            Assert.Equal(0, h5);
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
            Assert.Equal("Hello", HasCctorReceiver.S);
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

        [Fact]
        public static void TryEnsureSufficientExecutionStack_SpaceAvailable_ReturnsTrue()
        {
            Assert.True(RuntimeHelpers.TryEnsureSufficientExecutionStack());
        }

        [Fact]
        public static void TryEnsureSufficientExecutionStack_NoSpaceAvailable_ReturnsFalse()
        {
            FillStack(depth: 0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void FillStack(int depth)
        {
            // This test will fail with a StackOverflowException if TryEnsureSufficientExecutionStack() doesn't
            // return false. No exception is thrown and the test finishes when TryEnsureSufficientExecutionStack()
            // returns true.
            if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
            {
                Assert.Throws<InsufficientExecutionStackException>(() => RuntimeHelpers.EnsureSufficientExecutionStack());
                return;
            }
            else if (depth < 2048)
            {
                FillStack(depth + 1);
            }
        }

        [Fact]
        public static void GetUninitializedObject_InvalidArguments_ThrowsException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => RuntimeHelpers.GetUninitializedObject(null));

            AssertExtensions.Throws<ArgumentException>(null, () => RuntimeHelpers.GetUninitializedObject(typeof(string))); // special type
            Assert.Throws<MemberAccessException>(() => RuntimeHelpers.GetUninitializedObject(typeof(System.IO.Stream))); // abstract type
            Assert.Throws<MemberAccessException>(() => RuntimeHelpers.GetUninitializedObject(typeof(System.Collections.IEnumerable))); // interface
            Assert.Throws<MemberAccessException>(() => RuntimeHelpers.GetUninitializedObject(typeof(System.Collections.Generic.List<>))); // generic definition
            Assert.Throws<NotSupportedException>(() => RuntimeHelpers.GetUninitializedObject(typeof(TypedReference))); // byref-like type
        }

        [Fact]
        public static void GetUninitializedObject_DoesNotRunConstructor()
        {
            Assert.Equal(42, new ObjectWithDefaultCtor().Value);
            Assert.Equal(0, ((ObjectWithDefaultCtor)RuntimeHelpers.GetUninitializedObject(typeof(ObjectWithDefaultCtor))).Value);
        }

        [Fact]
        public static void GetUninitializedObject_Nullable()
        {
            // Nullable returns the underlying type instead
            Assert.Equal(typeof(int), RuntimeHelpers.GetUninitializedObject(typeof(Nullable<int>)).GetType());
        }

        private class ObjectWithDefaultCtor
        {
            public int Value = 42;
        }

        [Fact]
        public static void IsReferenceOrContainsReferences()
        {
            Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<int>());
            Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<string>());
            Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<Guid>());
            Assert.False(RuntimeHelpers.IsReferenceOrContainsReferences<StructWithoutReferences>());
            Assert.True(RuntimeHelpers.IsReferenceOrContainsReferences<StructWithReferences>());
        }

        [Fact]
        public static void ArrayRangeHelperTest()
        {
            int[] a = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Range range = Range.All;
            Assert.Equal(a, RuntimeHelpers.GetSubArray(a, range));

            range = new Range(Index.FromStart(1), Index.FromEnd(5));
            Assert.Equal(new int [] { 2, 3, 4, 5}, RuntimeHelpers.GetSubArray(a, range));

            range = new Range(Index.FromStart(0), Index.FromStart(a.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => { int [] array = RuntimeHelpers.GetSubArray(a, range); });
        }

        [Fact]
        [SkipOnMono("Not presently implemented on Mono")]
        public static void AllocateTypeAssociatedMemoryInvalidArguments()
        {
            Assert.Throws<ArgumentException>(() => { RuntimeHelpers.AllocateTypeAssociatedMemory(null, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(RuntimeHelpersTests), -1); });
        }

        [Fact]
        [SkipOnMono("Not presently implemented on Mono")]
        public static void AllocateTypeAssociatedMemoryValidArguments()
        {
            IntPtr memory = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(RuntimeHelpersTests), 32);
            Assert.NotEqual(memory, IntPtr.Zero);
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct StructWithoutReferences
        {
            public int a, b, c;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct StructWithReferences
        {
            public int a, b, c;
            public object d;
        }

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
}
