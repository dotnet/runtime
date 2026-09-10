// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Invoke_AllocatingAndExistingInstanceAcrossTiers(bool valueArgument)
        {
            Type argumentType = valueArgument ? typeof(int) : typeof(object);
            ConstructorInfo constructor = typeof(MutableConstructorTarget).GetConstructor(new[] { argumentType });
            var existing = new MutableConstructorTarget(null);
            object value = valueArgument ? 42 : new object();
            object[] arguments = { value };

            for (int i = 0; i <= IntrinsicInvokeSelectionAssertions.SpecializationThreshold; i++)
            {
                var allocated = (MutableConstructorTarget)constructor.Invoke(arguments);
                Assert.NotSame(existing, allocated);
                Assert.Equal(value, allocated.Value);
                Assert.Null(constructor.Invoke(existing, arguments));
                Assert.Equal(value, existing.Value);
            }
        }

        public sealed class MutableConstructorTarget
        {
            public object? Value;

            public MutableConstructorTarget(object? value) => Value = value;
            public MutableConstructorTarget(int value) => Value = value;
        }

        public static IEnumerable<object[]> Invoke_ReferenceConstructors_SharedThunk_TestData()
        {
            yield return new object[] { Type.EmptyTypes, Array.Empty<object?>() };
            yield return new object[]
            {
                new Type[] { typeof(IIntrinsicInvokeReference) },
                new object?[] { new IntrinsicInvokeReference(1) }
            };
            yield return new object[]
            {
                new Type[] { typeof(object[]), typeof(Action) },
                new object?[] { new object[] { "two" }, (Action)(() => { }) }
            };
            yield return new object[]
            {
                new Type[] { typeof(Task<string>), typeof(IIntrinsicInvokeReference), typeof(object[]) },
                new object?[] { Task.FromResult("three"), new IntrinsicInvokeReference(3), new object[] { "three" } }
            };
            yield return new object[]
            {
                new Type[] { typeof(object), typeof(IIntrinsicInvokeReference), typeof(Action), typeof(Task<int>) },
                new object?[] { new object(), new IntrinsicInvokeReference(4), (Action)(() => { }), Task.FromResult(4) }
            };
            yield return new object[]
            {
                new Type[] { typeof(Task<int>), typeof(object[]), typeof(IIntrinsicInvokeReference), typeof(Action), typeof(string) },
                new object?[] { Task.FromResult(5), new object[] { "five" }, new IntrinsicInvokeReference(5), (Action)(() => { }), "five" }
            };
            yield return new object[]
            {
                new Type[] { typeof(object), typeof(IIntrinsicInvokeReference), typeof(object[]), typeof(Action), typeof(Task<string>), typeof(Func<string>) },
                new object?[]
                {
                    new object(),
                    new IntrinsicInvokeReference(6),
                    new object[] { "six" },
                    (Action)(() => { }),
                    Task.FromResult("six"),
                    (Func<string>)(() => "six")
                }
            };
            yield return new object[]
            {
                new Type[]
                {
                    typeof(IIntrinsicInvokeReference),
                    typeof(object[]),
                    typeof(Action),
                    typeof(Task<int>),
                    typeof(string),
                    typeof(object),
                    typeof(Func<int>)
                },
                new object?[]
                {
                    new IntrinsicInvokeReference(7),
                    new object[] { "seven" },
                    (Action)(() => { }),
                    Task.FromResult(7),
                    "seven",
                    new object(),
                    (Func<int>)(() => 7)
                }
            };
            yield return new object[]
            {
                new Type[]
                {
                    typeof(object[]),
                    typeof(IIntrinsicInvokeReference),
                    typeof(Action),
                    typeof(Task<string>),
                    typeof(string),
                    typeof(object),
                    typeof(Func<int>),
                    typeof(Task<int>)
                },
                new object?[]
                {
                    new object[] { "eight" },
                    new IntrinsicInvokeReference(8),
                    (Action)(() => { }),
                    Task.FromResult("eight"),
                    "eight",
                    new object(),
                    (Func<int>)(() => 8),
                    Task.FromResult(8)
                }
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_ReferenceConstructors_SharedThunk_TestData))]
        public void Invoke_ReferenceConstructors_SharedThunk(Type[] parameterTypes, object?[] arguments)
        {
            ConstructorInfo constructor = typeof(IntrinsicInvokeReferenceConstructorTarget).GetConstructor(parameterTypes)!;

            var result = (IntrinsicInvokeReferenceConstructorTarget)constructor.Invoke(arguments);

            IntrinsicInvokeSelectionAssertions.AssertShared(constructor);
            Assert.Equal(arguments.Length, result.Values.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                Assert.Same(arguments[i], result.Values[i]);
            }
        }

        public static IEnumerable<object[]> Invoke_PrimitivePatternConstructors_SharedThunk_TestData()
        {
            yield return new object[] { new Type[] { typeof(bool) }, new object?[] { true } };
            yield return new object[] { new Type[] { typeof(bool) }, new object?[] { false } };
            yield return new object[] { new Type[] { typeof(int) }, new object?[] { int.MinValue + 12345 } };
            yield return new object[] { new Type[] { typeof(long) }, new object?[] { long.MaxValue - 12345 } };
            yield return new object[] { new Type[] { typeof(int), typeof(int) }, new object?[] { int.MinValue, int.MaxValue } };
            yield return new object[] { new Type[] { typeof(long), typeof(long) }, new object?[] { long.MinValue, long.MaxValue } };
            yield return new object[]
            {
                new Type[] { typeof(IIntrinsicInvokeReference), typeof(int) },
                new object?[] { new IntrinsicInvokeReference(7), 8 }
            };
            yield return new object[]
            {
                new Type[] { typeof(object[]), typeof(int), typeof(Action), typeof(Task<string>) },
                new object?[] { new object[] { "nine" }, 9, (Action)(() => { }), Task.FromResult("nine") }
            };
            yield return new object[]
            {
                new Type[] { typeof(Task<string>), typeof(IIntrinsicInvokeReference), typeof(bool), typeof(Action) },
                new object?[] { Task.FromResult("ten"), new IntrinsicInvokeReference(10), true, (Action)(() => { }) }
            };
            yield return new object[]
            {
                new Type[] { typeof(object[]), typeof(Action), typeof(Task<string>), typeof(bool), typeof(IIntrinsicInvokeReference) },
                new object?[]
                {
                    new object[] { "eleven" },
                    (Action)(() => { }),
                    Task.FromResult("eleven"),
                    false,
                    new IntrinsicInvokeReference(11)
                }
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_PrimitivePatternConstructors_SharedThunk_TestData))]
        public void Invoke_PrimitivePatternConstructors_SharedThunk(Type[] parameterTypes, object?[] arguments)
        {
            ConstructorInfo constructor = typeof(IntrinsicInvokePrimitiveConstructorTarget).GetConstructor(parameterTypes)!;

            var result = (IntrinsicInvokePrimitiveConstructorTarget)constructor.Invoke(arguments);

            IntrinsicInvokeSelectionAssertions.AssertShared(constructor);
            Assert.Equal(arguments.Length, result.Values.Length);
            for (int i = 0; i < arguments.Length; i++)
            {
                if (parameterTypes[i].IsValueType)
                {
                    Assert.Equal(arguments[i], result.Values[i]);
                }
                else
                {
                    Assert.Same(arguments[i], result.Values[i]);
                }
            }
        }

        public static IEnumerable<object[]> Invoke_EnumConstructors_TestData() =>
            IntrinsicInvokeTestData.EnumValues();

        [Theory]
        [MemberData(nameof(Invoke_EnumConstructors_TestData))]
        public void Invoke_EnumConstructors_MapActualUnderlyingType(Type enumType, object value)
        {
            Type targetType = typeof(IntrinsicInvokeEnumConstructorTarget<>).MakeGenericType(enumType);
            ConstructorInfo constructor = targetType.GetConstructor(new Type[] { enumType })!;

            object result = constructor.Invoke(new object?[] { value });

            if (enumType == typeof(IntrinsicInvokeInt32Enum) || enumType == typeof(IntrinsicInvokeInt64Enum))
            {
                IntrinsicInvokeSelectionAssertions.AssertShared(constructor);
            }
            else
            {
                IntrinsicInvokeSelectionAssertions.AssertFallback(constructor);
            }

            object? actual = targetType.GetField(nameof(IntrinsicInvokeEnumConstructorTarget<IntrinsicInvokeInt32Enum>.Value))!.GetValue(result);
            Assert.NotNull(actual);
            Assert.Equal(enumType, actual.GetType());
            Assert.Equal(value, actual);
        }

        public static IEnumerable<object[]> Invoke_ExcludedConstructors_Fallback_TestData()
        {
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[] { typeof(DateTime) },
                new object?[] { new DateTime(2026, 9, 10) },
                new DateTime(2026, 9, 10)
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[] { typeof(int?) },
                new object?[] { (int?)42 },
                42
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[] { typeof(ValueTask<int>) },
                new object?[] { new ValueTask<int>(43) },
                new ValueTask<int>(43)
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[] { typeof(CancellationToken) },
                new object?[] { new CancellationToken(canceled: true) },
                new CancellationToken(canceled: true)
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[] { typeof(int), typeof(int), typeof(int) },
                new object?[] { 1, 2, 3 },
                6
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeStructConstructorTarget),
                new Type[] { typeof(int) },
                new object?[] { 44 },
                44
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedConstructorTarget),
                new Type[]
                {
                    typeof(object), typeof(object), typeof(object), typeof(object), typeof(object),
                    typeof(object), typeof(object), typeof(object), typeof(object)
                },
                new object?[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                9
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_ExcludedConstructors_Fallback_TestData))]
        public void Invoke_ExcludedConstructors_Fallback(
            Type declaringType,
            Type[] parameterTypes,
            object?[] arguments,
            object expected)
        {
            ConstructorInfo constructor = declaringType.GetConstructor(parameterTypes)!;

            object result = constructor.Invoke(arguments);

            Assert.Equal(expected, declaringType.GetField(nameof(IntrinsicInvokeExcludedConstructorTarget.Value))!.GetValue(result));
            IntrinsicInvokeSelectionAssertions.AssertFallback(constructor);
        }

        [Fact]
        public void Invoke_SharedConstructorThunk_UsesNormalArgumentValidation()
        {
            ConstructorInfo referenceConstructor = typeof(IntrinsicInvokeConstructorValidationTarget).GetConstructor(
                new Type[] { typeof(IIntrinsicInvokeReference) })!;
            referenceConstructor.Invoke(new object?[] { new IntrinsicInvokeReference(1) });
            IntrinsicInvokeSelectionAssertions.AssertShared(referenceConstructor);
            Assert.Throws<ArgumentException>(() => referenceConstructor.Invoke(new object?[] { new object() }));

            ConstructorInfo primitiveConstructor = typeof(IntrinsicInvokeConstructorValidationTarget).GetConstructor(
                new Type[] { typeof(int) })!;
            primitiveConstructor.Invoke(new object?[] { 1 });
            IntrinsicInvokeSelectionAssertions.AssertShared(primitiveConstructor);
            Assert.Throws<ArgumentException>(() => primitiveConstructor.Invoke(new object?[] { 1L }));

            ConstructorInfo enumConstructor = typeof(IntrinsicInvokeConstructorValidationTarget).GetConstructor(
                new Type[] { typeof(IntrinsicInvokeInt32Enum) })!;
            enumConstructor.Invoke(new object?[] { IntrinsicInvokeInt32Enum.Value });
            IntrinsicInvokeSelectionAssertions.AssertShared(enumConstructor);
            Assert.Throws<ArgumentException>(() => enumConstructor.Invoke(new object?[] { IntrinsicInvokeInt64Enum.Value }));
        }

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

        [Fact]
        public void GetGenericArguments_ReturnsEmptyArray()
        {
            ConstructorInfo[] constructors = GetConstructors(typeof(ClassWith3Constructors));
            Assert.All(constructors, constructorInfo => Assert.Empty(constructorInfo.GetGenericArguments()));

            ConstructorInfo[] genericTypeConstructors = GetConstructors(typeof(GenericClassWithConstructor<int>));
            Assert.All(genericTypeConstructors, constructorInfo => Assert.Empty(constructorInfo.GetGenericArguments()));

            ConstructorInfo[] openGenericTypeConstructors = GetConstructors(typeof(GenericClassWithConstructor<>));
            Assert.All(openGenericTypeConstructors, constructorInfo => Assert.Empty(constructorInfo.GetGenericArguments()));
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

    public class GenericClassWithConstructor<T>
    {
        public GenericClassWithConstructor() { }
        public GenericClassWithConstructor(T value) { }
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

    internal sealed class IntrinsicInvokeReferenceConstructorTarget
    {
        internal object?[] Values { get; }

        public IntrinsicInvokeReferenceConstructorTarget() =>
            Values = Collect();

        public IntrinsicInvokeReferenceConstructorTarget(IIntrinsicInvokeReference value) =>
            Values = Collect(value);

        public IntrinsicInvokeReferenceConstructorTarget(object[] values, Action callback) =>
            Values = Collect(values, callback);

        public IntrinsicInvokeReferenceConstructorTarget(
            Task<string> task,
            IIntrinsicInvokeReference reference,
            object[] values) =>
            Values = Collect(task, reference, values);

        public IntrinsicInvokeReferenceConstructorTarget(
            object value,
            IIntrinsicInvokeReference reference,
            Action callback,
            Task<int> task) =>
            Values = Collect(value, reference, callback, task);

        public IntrinsicInvokeReferenceConstructorTarget(
            Task<int> task,
            object[] values,
            IIntrinsicInvokeReference reference,
            Action callback,
            string text) =>
            Values = Collect(task, values, reference, callback, text);

        public IntrinsicInvokeReferenceConstructorTarget(
            object value,
            IIntrinsicInvokeReference reference,
            object[] values,
            Action callback,
            Task<string> task,
            Func<string> factory) =>
            Values = Collect(value, reference, values, callback, task, factory);

        public IntrinsicInvokeReferenceConstructorTarget(
            IIntrinsicInvokeReference reference,
            object[] values,
            Action callback,
            Task<int> task,
            string text,
            object value,
            Func<int> factory) =>
            Values = Collect(reference, values, callback, task, text, value, factory);

        public IntrinsicInvokeReferenceConstructorTarget(
            object[] values,
            IIntrinsicInvokeReference reference,
            Action callback,
            Task<string> textTask,
            string text,
            object value,
            Func<int> factory,
            Task<int> valueTask) =>
            Values = Collect(values, reference, callback, textTask, text, value, factory, valueTask);

        private static object?[] Collect(params object?[] values)
        {
            GC.Collect();
            return values;
        }
    }

    internal sealed class IntrinsicInvokePrimitiveConstructorTarget
    {
        internal object?[] Values { get; }

        public IntrinsicInvokePrimitiveConstructorTarget(bool value) =>
            Values = Collect(value);

        public IntrinsicInvokePrimitiveConstructorTarget(int value) =>
            Values = Collect(value);

        public IntrinsicInvokePrimitiveConstructorTarget(long value) =>
            Values = Collect(value);

        public IntrinsicInvokePrimitiveConstructorTarget(int first, int second) =>
            Values = Collect(first, second);

        public IntrinsicInvokePrimitiveConstructorTarget(long first, long second) =>
            Values = Collect(first, second);

        public IntrinsicInvokePrimitiveConstructorTarget(IIntrinsicInvokeReference reference, int value) =>
            Values = Collect(reference, value);

        public IntrinsicInvokePrimitiveConstructorTarget(
            object[] values,
            int number,
            Action callback,
            Task<string> task) =>
            Values = Collect(values, number, callback, task);

        public IntrinsicInvokePrimitiveConstructorTarget(
            Task<string> task,
            IIntrinsicInvokeReference reference,
            bool value,
            Action callback) =>
            Values = Collect(task, reference, value, callback);

        public IntrinsicInvokePrimitiveConstructorTarget(
            object[] values,
            Action callback,
            Task<string> task,
            bool value,
            IIntrinsicInvokeReference reference) =>
            Values = Collect(values, callback, task, value, reference);

        private static object?[] Collect(params object?[] values)
        {
            GC.Collect();
            return values;
        }
    }

    internal sealed class IntrinsicInvokeEnumConstructorTarget<T> where T : struct, Enum
    {
        public T Value;

        public IntrinsicInvokeEnumConstructorTarget(T value)
        {
            GC.Collect();
            Value = value;
        }
    }

    internal sealed class IntrinsicInvokeExcludedConstructorTarget
    {
        public object? Value;

        public IntrinsicInvokeExcludedConstructorTarget(DateTime value) =>
            Value = Collect(value);

        public IntrinsicInvokeExcludedConstructorTarget(int? value) =>
            Value = Collect(value);

        public IntrinsicInvokeExcludedConstructorTarget(ValueTask<int> value) =>
            Value = Collect(value);

        public IntrinsicInvokeExcludedConstructorTarget(CancellationToken value) =>
            Value = Collect(value);

        public IntrinsicInvokeExcludedConstructorTarget(int first, int second, int third) =>
            Value = Collect(first + second + third);

        public IntrinsicInvokeExcludedConstructorTarget(object first, object second, object third, object fourth,
            object fifth, object sixth, object seventh, object eighth, object ninth) =>
            Value = Collect(ninth);

        private static T Collect<T>(T value)
        {
            GC.Collect();
            return value;
        }
    }

    internal struct IntrinsicInvokeStructConstructorTarget
    {
        public object Value;

        public IntrinsicInvokeStructConstructorTarget(int value)
        {
            Value = value;
            GC.Collect();
        }
    }

    internal sealed class IntrinsicInvokeConstructorValidationTarget
    {
        public IntrinsicInvokeConstructorValidationTarget(IIntrinsicInvokeReference value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }

        public IntrinsicInvokeConstructorValidationTarget(int value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }

        public IntrinsicInvokeConstructorValidationTarget(IntrinsicInvokeInt32Enum value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }
    }
}
