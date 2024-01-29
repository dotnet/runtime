// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests are shared with ConstructorInfo.Invoke and ConstructorInvoker.Invoke by using
    /// the abstract Invoke(...) methods below.
    /// </summary>
    public abstract class ConstructorCommonTests
    {
        public abstract object Invoke(ConstructorInfo constructorInfo, object?[]? parameters);

        protected abstract bool IsExceptionWrapped { get; }

        /// <summary>
        /// Invoke constructor on an existing instance. Should return null.
        /// </summary>
        public abstract object? Invoke(ConstructorInfo constructorInfo, object obj, object?[]? parameters);

        public static ConstructorInfo[] GetConstructors(Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.ToArray();
        }

        [Fact]
        public void SimpleInvoke()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Equal(3, constructors.Length);
            ClassWith3Constructors obj = (ClassWith3Constructors)Invoke(constructors[0], null);
            Assert.NotNull(obj);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15024", TestRuntimes.Mono)]
        public void Invoke_StaticConstructor_ThrowsMemberAccessException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructor));
            Assert.Equal(1, constructors.Length);
            Assert.Throws<MemberAccessException>(() => Invoke(constructors[0], new object[0]));
        }

        [Fact]
        public void Invoke_OneDimensionalArray()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(object[]));
            int[] arraylength = { 1, 2, 99, 65535 };

            // Try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                // Create big Array with elements
                object[] arr = (object[])Invoke(constructors[0], new object[] { length });
                Assert.Equal(arr.Length, length);
            }
        }

        [Fact]
        public void Invoke_OneDimensionalArray_NegativeLengths_ThrowsOverflowException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(object[]));
            int[] arraylength = new int[] { -1, -2, -99 };
            // Try to invoke Array ctors with different lengths
            foreach (int length in arraylength)
            {
                // Create big Array with elements
                if (IsExceptionWrapped)
                {
                    Exception ex = Assert.Throws<TargetInvocationException>(() => Invoke(constructors[0], new object[] { length }));
                    Assert.IsType<OverflowException>(ex.InnerException);
                }
                else
                {
                    Assert.Throws<OverflowException>(() => Invoke(constructors[0], new object[] { length }));
                }
            }
        }

        [Fact]
        public void Invoke_OneParameter()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj = (ClassWith3Constructors)Invoke(constructors[1], new object[] { 100 });
            Assert.Equal(100, obj.intValue);
        }

        [Fact]
        public void Invoke_TwoParameters()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj = (ClassWith3Constructors)Invoke(constructors[2], new object[] { 101, "hello" });
            Assert.Equal(101, obj.intValue);
            Assert.Equal("hello", obj.stringValue);
        }

        [Fact]
        public void Invoke_NoParameters_ThowsTargetParameterCountException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Throws<TargetParameterCountException>(() => Invoke(constructors[2], new object[0]));
        }

        [Fact]
        public void Invoke_ParameterMismatch_ThrowsTargetParameterCountException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Throws<TargetParameterCountException>(() => (ClassWith3Constructors)Invoke(constructors[2], new object[] { 121 }));
        }

        [Fact]
        public void Invoke_ParameterWrongType_ThrowsArgumentException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            AssertExtensions.Throws<ArgumentException>(null, () => (ClassWith3Constructors)Invoke(constructors[1], new object[] { "hello" }));
        }

        [Fact]
        public void Invoke_ExistingInstance()
        {
            // Should not produce a second object.
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            ClassWith3Constructors obj1 = new ClassWith3Constructors(100, "hello");
            ClassWith3Constructors obj2 = (ClassWith3Constructors)Invoke(constructors[2], obj1, new object[] { 999, "initialized" });
            Assert.Null(obj2);
            Assert.Equal(999, obj1.intValue);
            Assert.Equal("initialized", obj1.stringValue);
        }

        [Fact]
        public void Invoke_NullForObj()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Throws<TargetException>(() => Invoke(constructors[2], obj: null, new object[] { 999, "initialized" }));
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15026", TestRuntimes.Mono)]
        public void Invoke_AbstractClass_ThrowsMemberAccessException()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ConstructorInfoAbstractBase));
            Assert.Throws<MemberAccessException>(() => (ConstructorInfoAbstractBase)Invoke(constructors[0], new object[0]));
        }

        [Fact]
        public void Invoke_SubClass()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ConstructorInfoDerived));
            ConstructorInfoDerived obj = null;
            obj = (ConstructorInfoDerived)Invoke(constructors[0], new object[] { });
            Assert.NotNull(obj);
        }

        [Fact]
        public void Invoke_Struct()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(StructWith1Constructor));
            StructWith1Constructor obj;
            obj = (StructWith1Constructor)Invoke(constructors[0], new object[] { 1, 2 });
            Assert.Equal(1, obj.x);
            Assert.Equal(2, obj.y);
        }
    }
}
