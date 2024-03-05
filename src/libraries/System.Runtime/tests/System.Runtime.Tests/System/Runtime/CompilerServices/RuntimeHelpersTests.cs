// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        public static void EqualsTest()
        {
            // Boolean RuntimeHelpers.Equals(Object, Object)

            Assert.True(RuntimeHelpers.Equals(Guid.Empty, Guid.Empty));
            Assert.False(RuntimeHelpers.Equals(Guid.Empty, Guid.NewGuid()));

            // Reference equal
            object o = new object();
            Assert.True(RuntimeHelpers.Equals(o, o));

            // Type mismatch
            Assert.False(RuntimeHelpers.Equals(Guid.Empty, string.Empty));

            // Non value types
            Assert.False(RuntimeHelpers.Equals(new object(), new object()));
            Assert.False(RuntimeHelpers.Equals(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }));
        }

        [Fact]
        public static void InitializeArray()
        {
            // Void RuntimeHelpers.InitializeArray(Array, RuntimeFieldHandle)
            char[] expected = new char[] { 'a', 'b', 'c' }; // Compiler will use RuntimeHelpers.InitializeArray these
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

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                Assert.ThrowsAny<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(IList).GetMethod("Add").MethodHandle));
            }
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

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                // Generic definition without instantiation is invalid
                Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(List<>).GetMethod("Add").MethodHandle,
                    null));

                // Wrong instantiation
                Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(List<>).GetMethod("Add").MethodHandle,
                    new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle, typeof(TestStruct).TypeHandle }));
            }

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

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                // Generic definition without instantiation is invalid
                Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize").MethodHandle,
                    null));

                // Wrong instantiation
                Assert.Throws<ArgumentException>(() => RuntimeHelpers.PrepareMethod(typeof(Array).GetMethod("Resize").MethodHandle,
                    new RuntimeTypeHandle[] { typeof(TestStruct).TypeHandle, typeof(TestStruct).TypeHandle }));
            }
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

        public static IEnumerable<object[]> GetUninitializedObject_NegativeTestCases()
        {
            yield return new[] { typeof(string), typeof(ArgumentException) }; // variable-length type
            yield return new[] { typeof(int[]), typeof(ArgumentException) }; // variable-length type
            yield return new[] { typeof(int[,]), typeof(ArgumentException) }; // variable-length type

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                yield return new[] { Array.CreateInstance(typeof(int), new[] { 1 }, new[] { 1 }).GetType(), typeof(ArgumentException) }; // variable-length type (non-szarray)
            }

            yield return new[] { typeof(Array), typeof(MemberAccessException) }; // abstract type
            yield return new[] { typeof(Enum), typeof(MemberAccessException) }; // abstract type

            yield return new[] { typeof(Stream), typeof(MemberAccessException) }; // abstract type
            yield return new[] { typeof(Buffer), typeof(MemberAccessException) }; // static type (runtime sees it as abstract)
            yield return new[] { typeof(IDisposable), typeof(MemberAccessException) }; // interface type

            yield return new[] { typeof(List<>), typeof(MemberAccessException) }; // open generic type
            yield return new[] { typeof(List<>).GetGenericArguments()[0], PlatformDetection.IsMonoRuntime ? typeof(MemberAccessException) : typeof(ArgumentException) }; // 'T' placeholder typedesc

            yield return new[] { typeof(Delegate), typeof(MemberAccessException) }; // abstract type

            yield return new[] { typeof(void), typeof(ArgumentException) }; // explicit block in place
            yield return new[] { typeof(int).MakePointerType(), typeof(ArgumentException) }; // pointer
            yield return new[] { typeof(int).MakeByRefType(), typeof(ArgumentException) }; // byref

            yield return new[] { FunctionPointerType(), typeof(ArgumentException) }; // function pointer
            static unsafe Type FunctionPointerType() => typeof(delegate*<void>);

            yield return new[] { typeof(ReadOnlySpan<int>), typeof(NotSupportedException) }; // byref-like type
            yield return new[] { typeof(ArgIterator), typeof(NotSupportedException) }; // byref-like type

            Type canonType = typeof(object).Assembly.GetType("System.__Canon", throwOnError: false);
            if (canonType != null)
            {
                yield return new[] { typeof(List<>).MakeGenericType(canonType), typeof(NotSupportedException) }; // shared by generic instantiations
            }

            Type comObjType = typeof(object).Assembly.GetType("System.__ComObject", throwOnError: false);
            if (comObjType != null)
            {
                yield return new[] { comObjType, typeof(NotSupportedException) }; // COM type
            }

            if (PlatformDetection.SupportsComInterop)
            {
                yield return new[] { typeof(WbemContext), typeof(NotSupportedException) }; // COM type
            }
        }

        // This type definition is lifted from System.Management, just for testing purposes
        [ClassInterface((short)0x0000)]
        [Guid("674B6698-EE92-11D0-AD71-00C04FD8FDFF")]
        [ComImport]
        internal class WbemContext
        {
        }

        internal class ClassWithBeforeFieldInitCctor
        {
            private static readonly int _theInt = GetInt();

            private static int GetInt()
            {
                AppDomain.CurrentDomain.SetData("ClassWithBeforeFieldInitCctor_CctorRan", true);
                return 0;
            }
        }

        internal class ClassWithNormalCctor
        {
#pragma warning disable CS0414 // unused private field
            private static readonly int _theInt;
#pragma warning restore CS0414

            static ClassWithNormalCctor()
            {
                AppDomain.CurrentDomain.SetData("ClassWithNormalCctor_CctorRan", true);
                _theInt = 0;
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/69919", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        [Fact]
        public static void GetUninitializedObject_DoesNotRunBeforeFieldInitCctors()
        {
            object o = RuntimeHelpers.GetUninitializedObject(typeof(ClassWithBeforeFieldInitCctor));
            Assert.IsType<ClassWithBeforeFieldInitCctor>(o);

            Assert.Null(AppDomain.CurrentDomain.GetData("ClassWithBeforeFieldInitCctor_CctorRan"));
        }

        [Fact]
        public static void GetUninitializedObject_RunsNormalStaticCtors()
        {
            object o = RuntimeHelpers.GetUninitializedObject(typeof(ClassWithNormalCctor));
            Assert.IsType<ClassWithNormalCctor>(o);

            Assert.Equal(true, AppDomain.CurrentDomain.GetData("ClassWithNormalCctor_CctorRan"));
        }

        [Theory]
        [MemberData(nameof(GetUninitializedObject_NegativeTestCases))]
        public static void GetUninitializedObject_InvalidArguments_ThrowsException(Type typeToInstantiate, Type expectedExceptionType)
        {
            Assert.Throws(expectedExceptionType, () => RuntimeHelpers.GetUninitializedObject(typeToInstantiate));
        }

        [Fact]
        public static void GetUninitializedObject_DoesNotRunConstructor()
        {
            Assert.Equal(42, new ObjectWithDefaultCtor().Value);
            Assert.Equal(0, ((ObjectWithDefaultCtor)RuntimeHelpers.GetUninitializedObject(typeof(ObjectWithDefaultCtor))).Value);
        }

        [Fact]
        public static void GetUninitializedObject_Struct()
        {
            object o = RuntimeHelpers.GetUninitializedObject(typeof(Guid));
            Assert.Equal(Guid.Empty, Assert.IsType<Guid>(o));
        }

        [Fact]
        public static void GetUninitializedObject_Nullable()
        {
            // Nullable returns the underlying type instead
            object o = RuntimeHelpers.GetUninitializedObject(typeof(int?));
            Assert.Equal(0, Assert.IsType<int>(o));
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
        public static void ArrayGetSubArrayTest()
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
        public static void ArrayGetSubArrayCoVarianceTest()
        {
            object[] arr = new string[10];
            object[] slice = RuntimeHelpers.GetSubArray<object>(arr, new Range(Index.FromStart(1), Index.FromEnd(2)));
            Assert.IsType<string[]>(slice);

            uint[] arr2 = (uint[])(object)new int[10];
            uint[] slice2 = RuntimeHelpers.GetSubArray<uint>(arr2, new Range(Index.FromStart(1), Index.FromEnd(2)));
            Assert.IsType<int[]>(slice2);
        }

        [Fact]
        public static void AllocateTypeAssociatedMemoryInvalidArguments()
        {
            Assert.Throws<ArgumentException>(() => { RuntimeHelpers.AllocateTypeAssociatedMemory(null, 10); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(RuntimeHelpersTests), -1); });
        }

        [Fact]
        public static unsafe void AllocateTypeAssociatedMemoryValidArguments()
        {
            IntPtr memory = RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(RuntimeHelpersTests), 32);
            Assert.NotEqual(memory, IntPtr.Zero);
            // Validate that the memory is zeroed out
            Assert.True(new Span<byte>((void*)memory, 32).SequenceEqual(new byte[32]));
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
