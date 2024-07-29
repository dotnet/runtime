// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

#pragma warning disable 0414

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests use the shared tests from the base class with ConstructorInfo.Invoke.
    /// </summary>
    public sealed class ConstructorInfoTests : ConstructorCommonTests
    {
        public override object Invoke(ConstructorInfo constructorInfo, object?[]? parameters)
        {
            return constructorInfo.Invoke(parameters);
        }

        public override object? Invoke(ConstructorInfo constructorInfo, object obj, object?[]? parameters)
        {
            return constructorInfo.Invoke(obj, parameters);
        }

        protected override bool IsExceptionWrapped => true;

        [Fact]
        public void ConstructorName()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.Equal(3, constructors.Length);
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                Assert.Equal(ConstructorInfo.ConstructorName, constructorInfo.Name);
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            ConstructorInfo[] methodSampleConstructors1 = GetConstructors(typeof(ClassWith3Constructors));
            ConstructorInfo[] methodSampleConstructors2 = GetConstructors(typeof(ClassWith3Constructors));
            yield return new object[] { methodSampleConstructors1[0], methodSampleConstructors2[0], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[1], true };
            yield return new object[] { methodSampleConstructors1[2], methodSampleConstructors2[2], true };
            yield return new object[] { methodSampleConstructors1[1], methodSampleConstructors2[2], false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(ConstructorInfo constructorInfo1, ConstructorInfo constructorInfo2, bool expected)
        {
            Assert.Equal(expected, constructorInfo1.Equals(constructorInfo2));
            Assert.NotEqual(expected, constructorInfo1 != constructorInfo2);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                Assert.NotEqual(0, constructorInfo.GetHashCode());
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingStaticConstructorsSupported))]
        public void Invoke_StaticConstructor_NullObject_NullParameters()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructor));
            Assert.Equal(1, constructors.Length);
            object obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsInvokingStaticConstructorsSupported))]
        public void Invoke_StaticConstructorMultipleTimes()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection));
            Assert.Equal(1, constructors.Length);
            // The first time the static cctor is called, it should run the cctor twice
            // Once to initialize run the cctor as a cctor
            // The second to run it as a method which is invoked.
            Assert.Equal(0, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);
            object obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
            Assert.Equal(1, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);

            // Subsequent invocations of the static cctor should not run the cctor at all, as it has already executed
            // and running multiple times opens up the possibility of modifying read only static data
            obj = constructors[0].Invoke(null, new object[] { });
            Assert.Null(obj);
            Assert.Equal(1, ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection.VisibleStatics.s_cctorCallCount);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67531", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Invoke_TwoDimensionalArray_CustomBinder_IncorrectTypeArguments()
        {
            var ctor = typeof(int[,]).GetConstructor(new[] { typeof(int), typeof(int) });
            var args = new object[] { "1", "2" };
            var arr = (int[,])ctor.Invoke(BindingFlags.Default, new ConvertStringToIntBinder(), args, null);
            Assert.Equal(2, arr.Length);
            Assert.True(args[0] is int);
            Assert.True(args[1] is int);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67531", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Invoke_TwoParameters_CustomBinder_IncorrectTypeArgument()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));

            var args = new object[] { "101", "hello" };
            ClassWith3Constructors obj = (ClassWith3Constructors)constructors[2].Invoke(BindingFlags.Default, new ConvertStringToIntBinder(), args, null);
            Assert.Equal(101, obj.intValue);
            Assert.Equal("hello", obj.stringValue);
            Assert.True(args[0] is int);
            Assert.True(args[1] is string);
        }

        [Fact]
        public void IsConstructor_ReturnsTrue()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.All(constructors, constructorInfo => Assert.True(constructorInfo.IsConstructor));
        }

        [Fact]
        public void IsPublic()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.True(constructors[0].IsPublic);
        }

        // Use this class only from the Invoke_StaticConstructorMultipleTimes method
        public static class ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection
        {
            public static class VisibleStatics
            {
                public static int s_cctorCallCount;
            }

            static ClassWithStaticConstructorThatIsCalledMultipleTimesViaReflection()
            {
                VisibleStatics.s_cctorCallCount++;
            }
        }
    }

    // Metadata for Reflection
    public abstract class ConstructorInfoAbstractBase
    {
        public ConstructorInfoAbstractBase() { }
    }

    public class ConstructorInfoDerived : ConstructorInfoAbstractBase
    {
        public ConstructorInfoDerived() { }
    }

    public class ClassWith3Constructors
    {
        public int intValue = 0;
        public string stringValue = "";

        public ClassWith3Constructors() { }

        public ClassWith3Constructors(int intValue) { this.intValue = intValue; }

        public ClassWith3Constructors(int intValue, string stringValue)
        {
            this.intValue = intValue;
            this.stringValue = stringValue;
        }
    }

    public static class ClassWithStaticConstructor
    {
        static ClassWithStaticConstructor() { }
    }

    public struct StructWith1Constructor
    {
        public int x;
        public int y;

        public StructWith1Constructor(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
