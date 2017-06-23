// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public static partial class RuntimeHelpersTests
    {
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
    }
}
