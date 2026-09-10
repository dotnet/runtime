// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests use the shared tests from the base class with MethodInfo.Invoke.
    /// </summary>
    public sealed class MethodInfoTests : MethodCommonTests
    {
        public override object? Invoke(MethodInfo methodInfo, object? obj, object?[]? parameters)
        {
            return methodInfo.Invoke(obj, parameters);
        }

        protected override bool SupportsMissing => false;

        public static IEnumerable<object[]> Invoke_ReturnValueAcrossTiers_TestData()
        {
            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(byte), (byte)42 };
            yield return new object[] { typeof(sbyte), (sbyte)-42 };
            yield return new object[] { typeof(char), 'x' };
            yield return new object[] { typeof(short), (short)-1234 };
            yield return new object[] { typeof(ushort), (ushort)1234 };
            yield return new object[] { typeof(int), -12345 };
            yield return new object[] { typeof(uint), 12345u };
            yield return new object[] { typeof(long), -1234567890123L };
            yield return new object[] { typeof(ulong), 1234567890123UL };
            yield return new object[] { typeof(float), 12.5f };
            yield return new object[] { typeof(double), -25.5 };
            yield return new object[] { typeof(nint), (nint)12345 };
            yield return new object[] { typeof(nuint), (nuint)54321 };
            yield return new object[] { typeof(string), "returned value" };
            yield return new object[] { typeof(object), new object() };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR))]
        [MemberData(nameof(Invoke_ReturnValueAcrossTiers_TestData))]
        public void Invoke_ReturnValueAcrossTiers(Type returnType, object expected)
        {
            Type target = typeof(ReturnValueTarget<>).MakeGenericType(returnType);
            target.GetField(nameof(ReturnValueTarget<int>.Value)).SetValue(null, expected);
            MethodInfo method = target.GetMethod(nameof(ReturnValueTarget<int>.GetValue));
            MethodInvoker invoker = MethodInvoker.Create(method);

            for (int i = 0; i <= IntrinsicInvokeSelectionAssertions.SpecializationThreshold; i++)
            {
                Assert.Equal(expected, method.Invoke(null, null));
                Assert.Equal(expected, invoker.Invoke(null));
            }
        }

        public static class ReturnValueTarget<T>
        {
            public static T Value;

            public static T GetValue() => Value;
        }

        public static IEnumerable<object[]> Invoke_InstanceReferenceVoid_SharedThunk_TestData()
        {
            yield return new object[] { nameof(IntrinsicInvokeReferenceTarget.Void0), Array.Empty<object?>(), -1 };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Void1),
                new object?[] { new IntrinsicInvokeReference(1) },
                0
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Void2),
                new object?[] { new object[] { "array" }, (Action)(() => { }) },
                1
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Void3),
                new object?[] { Task.FromResult("task"), new IntrinsicInvokeReference(3), new string[] { "array" } },
                2
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Void4),
                new object?[] { new object(), new IntrinsicInvokeReference(4), (Action)(() => { }), Task.FromResult(4) },
                3
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_InstanceReferenceVoid_SharedThunk_TestData))]
        public void Invoke_InstanceReferenceVoid_SharedThunk(string methodName, object?[] arguments, int retainedArgument)
        {
            var target = new IntrinsicInvokeReferenceTarget();
            MethodInfo method = typeof(IntrinsicInvokeReferenceTarget).GetMethod(methodName)!;

            Assert.Null(method.Invoke(target, arguments));
            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Equal(1, target.CallCount);
            Assert.Same(retainedArgument < 0 ? target.Sentinel : arguments[retainedArgument], target.LastValue);
        }

        public static IEnumerable<object[]> Invoke_InstanceReferenceReturn_SharedThunk_TestData()
        {
            yield return new object[] { nameof(IntrinsicInvokeReferenceTarget.Return0), Array.Empty<object?>(), -1 };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Return1),
                new object?[] { new IntrinsicInvokeReference(1) },
                0
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Return2),
                new object?[] { new object[] { "array" }, (Action)(() => { }) },
                0
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Return3),
                new object?[] { Task.FromResult("task"), (Func<string>)(() => "delegate"), new IntrinsicInvokeReference(3) },
                1
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeReferenceTarget.Return4),
                new object?[] { Task.FromResult("task"), new object[] { "array" }, new IntrinsicInvokeReference(4), (Action)(() => { }) },
                0
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_InstanceReferenceReturn_SharedThunk_TestData))]
        public void Invoke_InstanceReferenceReturn_SharedThunk(string methodName, object?[] arguments, int returnedArgument)
        {
            var target = new IntrinsicInvokeReferenceTarget();
            MethodInfo method = typeof(IntrinsicInvokeReferenceTarget).GetMethod(methodName)!;

            object? result = method.Invoke(target, arguments);

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Same(returnedArgument < 0 ? target.Sentinel : arguments[returnedArgument], result);
        }

        public static IEnumerable<object[]> Invoke_PrimitiveValues_TestData() =>
            IntrinsicInvokeTestData.PrimitiveValues();

        [Theory]
        [MemberData(nameof(Invoke_PrimitiveValues_TestData))]
        public void Invoke_InstancePrimitiveReturn_SharedThunk(Type returnType, object expected)
        {
            Type targetType = typeof(IntrinsicInvokePrimitiveReturnTarget<>).MakeGenericType(returnType);
            object target = Activator.CreateInstance(targetType)!;
            targetType.GetProperty(nameof(IntrinsicInvokePrimitiveReturnTarget<int>.Value))!.SetValue(target, expected);
            MethodInfo method = targetType.GetMethod(nameof(IntrinsicInvokePrimitiveReturnTarget<int>.GetValue))!;

            object? result = method.Invoke(target, null);

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.NotNull(result);
            Assert.Equal(returnType, result.GetType());
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> Invoke_PrimitiveAndEnumValues_TestData()
        {
            foreach (object[] data in IntrinsicInvokeTestData.PrimitiveValues())
            {
                yield return data;
            }

            foreach (object[] data in IntrinsicInvokeTestData.EnumValues())
            {
                yield return data;
            }
        }

        [Theory]
        [MemberData(nameof(Invoke_PrimitiveAndEnumValues_TestData))]
        public void Invoke_InstancePrimitiveArgument_SharedThunk(Type argumentType, object value)
        {
            Type targetType = typeof(IntrinsicInvokePrimitiveArgumentTarget<>).MakeGenericType(argumentType);
            object target = Activator.CreateInstance(targetType)!;
            MethodInfo method = targetType.GetMethod(nameof(IntrinsicInvokePrimitiveArgumentTarget<int>.SetValue))!;

            Assert.Null(method.Invoke(target, new object?[] { value }));

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            object? actual = targetType.GetField(nameof(IntrinsicInvokePrimitiveArgumentTarget<int>.Value))!.GetValue(target);
            Assert.NotNull(actual);
            Assert.Equal(value.GetType(), actual.GetType());
            Assert.Equal(value, actual);
        }

        [Fact]
        public void Invoke_InstanceTwoReferencesReturningInt_SharedThunk()
        {
            var target = new IntrinsicInvokeInstancePatternTarget();
            var reference = new IntrinsicInvokeReference(40);
            object[] array = { "first", "second" };
            MethodInfo method = typeof(IntrinsicInvokeInstancePatternTarget).GetMethod(
                nameof(IntrinsicInvokeInstancePatternTarget.Sum))!;

            Assert.Equal(42, method.Invoke(target, new object?[] { reference, array }));
            IntrinsicInvokeSelectionAssertions.AssertShared(method);
        }

        [Fact]
        public void Invoke_InstanceFloatFloatFloatInt_SharedThunk()
        {
            var target = new IntrinsicInvokeInstancePatternTarget();
            MethodInfo method = typeof(IntrinsicInvokeInstancePatternTarget).GetMethod(
                nameof(IntrinsicInvokeInstancePatternTarget.SetVector))!;

            Assert.Null(method.Invoke(target, new object?[] { 1.25f, 2.5f, 3.75f, 4 }));

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Equal(1.25f, target.X);
            Assert.Equal(2.5f, target.Y);
            Assert.Equal(3.75f, target.Z);
            Assert.Equal(4, target.W);
        }

        public static IEnumerable<object[]> Invoke_StaticIntReturningReference_SharedThunk_TestData()
        {
            yield return new object[]
            {
                typeof(IntrinsicInvokeStaticIntReturnTarget),
                nameof(IntrinsicInvokeStaticIntReturnTarget.ReturnString),
                "42"
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeGenericStaticIntReturnTarget<int>),
                nameof(IntrinsicInvokeGenericStaticIntReturnTarget<int>.ReturnComparable),
                42
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeStaticIntReturnTarget),
                nameof(IntrinsicInvokeStaticIntReturnTarget.ReturnGeneric),
                typeof(int)
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_StaticIntReturningReference_SharedThunk_TestData))]
        public void Invoke_StaticIntReturningReference_SharedThunk(Type declaringType, string methodName, object expected)
        {
            MethodInfo method = declaringType.GetMethod(methodName)!;
            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(int));
            }

            object? result = method.Invoke(null, new object?[] { 42 });

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Equal(expected, result);
        }

        public static IEnumerable<object[]> Invoke_StaticReferenceOutReference_SharedThunk_TestData()
        {
            var falseInput = new IntrinsicInvokeReference(1);
            yield return new object[]
            {
                nameof(IntrinsicInvokeByRefTarget.ReturnFalse),
                new object?[] { falseInput, null },
                false,
                falseInput
            };

            yield return new object[]
            {
                nameof(IntrinsicInvokeByRefTarget.ReturnNull),
                new object?[] { new object[] { "input" }, (Action)(() => { }) },
                true,
                null
            };

            Task<string> trueInput = Task.FromResult("input");
            yield return new object[]
            {
                nameof(IntrinsicInvokeByRefTarget.ReturnTrue),
                new object?[] { trueInput, null },
                true,
                trueInput
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_StaticReferenceOutReference_SharedThunk_TestData))]
        public void Invoke_StaticReferenceOutReference_SharedThunk(
            string methodName,
            object?[] arguments,
            bool expectedResult,
            object? expectedOutput)
        {
            MethodInfo method = typeof(IntrinsicInvokeByRefTarget).GetMethod(methodName)!;

            Assert.Equal(expectedResult, method.Invoke(null, arguments));

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Same(expectedOutput, arguments[1]);
        }

        [Fact]
        public void Invoke_StaticReferenceOutReference_DoesNotCopyBackAfterException()
        {
            MethodInfo method = typeof(IntrinsicInvokeByRefTarget).GetMethod(nameof(IntrinsicInvokeByRefTarget.ThrowAfterWrite))!;
            var input = new IntrinsicInvokeReference(42);
            var originalOutput = new IntrinsicInvokeReference(-1);
            object?[] arguments = { input, originalOutput };

            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, arguments));

            Assert.IsType<InvalidOperationException>(exception.InnerException);
            Assert.Same(originalOutput, arguments[1]);
            IntrinsicInvokeSelectionAssertions.AssertShared(method);
        }

        public static IEnumerable<object[]> Invoke_StaticReferenceReturn_SharedThunk_TestData()
        {
            var reference2 = new IntrinsicInvokeReference(2);
            object[] array2 = { "two" };
            yield return new object[]
            {
                nameof(IntrinsicInvokeStaticReferenceTarget.Return2),
                new object?[] { reference2, array2 },
                1
            };

            Task<string> task3 = Task.FromResult("three");
            yield return new object[]
            {
                nameof(IntrinsicInvokeStaticReferenceTarget.Return3),
                new object?[] { task3, (Action)(() => { }), new IntrinsicInvokeReference(3) },
                0
            };

            Action callback4 = () => { };
            yield return new object[]
            {
                nameof(IntrinsicInvokeStaticReferenceTarget.Return4),
                new object?[] { callback4, new object[] { "four" }, new IntrinsicInvokeReference(4), Task.FromResult(4) },
                0
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_StaticReferenceReturn_SharedThunk_TestData))]
        public void Invoke_StaticReferenceReturn_SharedThunk(string methodName, object?[] arguments, int returnedArgument)
        {
            MethodInfo method = typeof(IntrinsicInvokeStaticReferenceTarget).GetMethod(methodName)!;

            object? result = method.Invoke(null, arguments);

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Same(arguments[returnedArgument], result);
        }

        public static IEnumerable<object[]> Invoke_StaticReferenceVoid_SharedThunk_TestData()
        {
            var tracker3 = new IntrinsicInvokeActionTracker();
            yield return new object[]
            {
                nameof(IntrinsicInvokeStaticReferenceTarget.Void3),
                new object?[] { Task.FromResult("three"), tracker3.Callback, new IntrinsicInvokeReference(3) },
                tracker3
            };

            var tracker4 = new IntrinsicInvokeActionTracker();
            yield return new object[]
            {
                nameof(IntrinsicInvokeStaticReferenceTarget.Void4),
                new object?[] { new object[] { "four" }, new IntrinsicInvokeReference(4), tracker4.Callback, Task.FromResult(4) },
                tracker4
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_StaticReferenceVoid_SharedThunk_TestData))]
        public void Invoke_StaticReferenceVoid_SharedThunk(
            string methodName,
            object?[] arguments,
            object trackerObject)
        {
            var tracker = (IntrinsicInvokeActionTracker)trackerObject;
            MethodInfo method = typeof(IntrinsicInvokeStaticReferenceTarget).GetMethod(methodName)!;

            Assert.Null(method.Invoke(null, arguments));

            IntrinsicInvokeSelectionAssertions.AssertShared(method);
            Assert.Equal(1, tracker.CallCount);
        }

        public static IEnumerable<object[]> Invoke_VirtualDispatch_SharedThunk_TestData()
        {
            yield return new object[]
            {
                typeof(IntrinsicInvokeVirtualDispatchBase),
                nameof(IntrinsicInvokeVirtualDispatchBase.Dispatch),
                new object[]
                {
                    new IntrinsicInvokeVirtualDispatchA(),
                    new IntrinsicInvokeVirtualDispatchB(),
                    new IntrinsicInvokeVirtualDispatchA(),
                    new IntrinsicInvokeVirtualDispatchB()
                },
                new string[] { "virtual-a", "virtual-b", "virtual-a", "virtual-b" }
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeAbstractDispatchBase),
                nameof(IntrinsicInvokeAbstractDispatchBase.Dispatch),
                new object[]
                {
                    new IntrinsicInvokeAbstractDispatchA(),
                    new IntrinsicInvokeAbstractDispatchB(),
                    new IntrinsicInvokeAbstractDispatchA(),
                    new IntrinsicInvokeAbstractDispatchB()
                },
                new string[] { "abstract-a", "abstract-b", "abstract-a", "abstract-b" }
            };
            yield return new object[]
            {
                typeof(IIntrinsicInvokeInterfaceDispatch),
                nameof(IIntrinsicInvokeInterfaceDispatch.Dispatch),
                new object[]
                {
                    new IntrinsicInvokeInterfaceDispatchA(),
                    new IntrinsicInvokeInterfaceDispatchB(),
                    new IntrinsicInvokeInterfaceDispatchA(),
                    new IntrinsicInvokeInterfaceDispatchB()
                },
                new string[] { "interface-a", "interface-b", "interface-a", "interface-b" }
            };
            yield return new object[]
            {
                typeof(IIntrinsicInvokeDefaultInterfaceDispatch),
                nameof(IIntrinsicInvokeDefaultInterfaceDispatch.Dispatch),
                new object[]
                {
                    new IntrinsicInvokeDefaultInterfaceDispatch(),
                    new IntrinsicInvokeDefaultInterfaceOverride(),
                    new IntrinsicInvokeDefaultInterfaceDispatch(),
                    new IntrinsicInvokeDefaultInterfaceOverride()
                },
                new string[] { "default", "override", "default", "override" }
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeGenericVirtualDispatch),
                nameof(IntrinsicInvokeGenericVirtualDispatch.Dispatch),
                new object[] { new IntrinsicInvokeGenericVirtualA(), new IntrinsicInvokeGenericVirtualB() },
                new string[] { "Int32-generic-a", "Int32-generic-b" }
            };
            yield return new object[]
            {
                typeof(IIntrinsicInvokeGenericInterfaceDispatch),
                nameof(IIntrinsicInvokeGenericInterfaceDispatch.Dispatch),
                new object[] { new IntrinsicInvokeGenericInterfaceA(), new IntrinsicInvokeGenericInterfaceB() },
                new string[] { "Int32-interface-a", "Int32-interface-b" }
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_VirtualDispatch_SharedThunk_TestData))]
        public void Invoke_VirtualDispatch_SharedThunk(
            Type declaringType,
            string methodName,
            object[] receivers,
            string[] expected)
        {
            MethodInfo method = declaringType.GetMethod(methodName)!;
            if (method.IsGenericMethodDefinition)
            {
                method = method.MakeGenericMethod(typeof(int));
            }
            object argument = new object();

            for (int i = 0; i < receivers.Length; i++)
            {
                Assert.Equal(expected[i], method.Invoke(receivers[i], new object?[] { argument }));
                if (i == 0)
                {
                    IntrinsicInvokeSelectionAssertions.AssertShared(method);
                }
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public void DynamicInvoke_DelegateInvokeMethod_SharedThunk(int callbackKind)
        {
            var payload = new IntrinsicInvokeDelegatePayload();
            Delegate callback;
            string[] expectedLog;

            switch (callbackKind)
            {
                case 0:
                    callback = new IntrinsicInvokeStaticDynamicCallback(IntrinsicInvokeDelegateCallbacks.Static);
                    expectedLog = new string[] { "static" };
                    break;
                case 1:
                    var instanceTarget = new IntrinsicInvokeDelegateCallbacks("instance");
                    callback = new IntrinsicInvokeInstanceDynamicCallback(instanceTarget.Instance);
                    expectedLog = new string[] { "instance" };
                    break;
                case 2:
                    var multicastTarget = new IntrinsicInvokeDelegateCallbacks("instance");
                    IntrinsicInvokeMulticastDynamicCallback first = IntrinsicInvokeDelegateCallbacks.MulticastStatic;
                    IntrinsicInvokeMulticastDynamicCallback second = multicastTarget.MulticastInstance;
                    callback = first + second;
                    expectedLog = new string[] { "static", "instance" };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(callbackKind));
            }

            MethodInfo invokeMethod = callback.GetType().GetMethod(nameof(Action.Invoke))!;
            object? result = callback.DynamicInvoke(new object?[] { payload });

            Assert.Same(payload, result);
            Assert.Equal(expectedLog, payload.Log.ToArray());
            IntrinsicInvokeSelectionAssertions.AssertShared(invokeMethod);
        }

        public static IEnumerable<object[]> Invoke_ExcludedShapes_Fallback_TestData()
        {
            DateTime date = new DateTime(2026, 9, 10);
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.DateTimeArgument),
                null,
                new object?[] { date },
                10,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.DateTimeResult),
                null,
                Array.Empty<object?>(),
                date,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.NullableArgument),
                null,
                new object?[] { (int?)42 },
                42,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.NullableResult),
                null,
                Array.Empty<object?>(),
                43,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.ValueTaskArgument),
                null,
                new object?[] { new ValueTask<int>(44) },
                44,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.ValueTaskResult),
                null,
                Array.Empty<object?>(),
                new ValueTask<int>(45),
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.CancellationTokenArgument),
                null,
                new object?[] { new CancellationToken(canceled: true) },
                true,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.CancellationTokenResult),
                null,
                Array.Empty<object?>(),
                new CancellationToken(canceled: true),
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.ByRefValue),
                null,
                new object?[] { 46 },
                true,
                0,
                47
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.TwoPrimitiveArguments),
                null,
                new object?[] { 1, 2 },
                3,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeExcludedMethodTarget),
                nameof(IntrinsicInvokeExcludedMethodTarget.FiveReferenceArguments),
                null,
                new object?[] { "one", "two", "three", "four", "five" },
                "five",
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeInstancePatternTarget),
                nameof(IntrinsicInvokeInstancePatternTarget.FiveReferenceArguments),
                new IntrinsicInvokeInstancePatternTarget(),
                new object?[] { "one", "two", "three", "four", "five" },
                "five",
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IntrinsicInvokeStructReceiver),
                nameof(IntrinsicInvokeStructReceiver.GetValue),
                new IntrinsicInvokeStructReceiver(48),
                Array.Empty<object?>(),
                48,
                -1,
                null
            };
            yield return new object[]
            {
                typeof(IIntrinsicInvokeStructReceiver),
                nameof(IIntrinsicInvokeStructReceiver.GetValue),
                new IntrinsicInvokeStructReceiver(49),
                Array.Empty<object?>(),
                49,
                -1,
                null
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_ExcludedShapes_Fallback_TestData))]
        public void Invoke_ExcludedShapes_Fallback(
            Type declaringType,
            string methodName,
            object? target,
            object?[] arguments,
            object expectedResult,
            int copyBackIndex,
            object? expectedCopyBack)
        {
            MethodInfo method = declaringType.GetMethod(methodName)!;

            object? result = method.Invoke(target, arguments);

            Assert.Equal(expectedResult, result);
            if (copyBackIndex >= 0)
            {
                Assert.Equal(expectedCopyBack, arguments[copyBackIndex]);
            }

            IntrinsicInvokeSelectionAssertions.AssertFallback(method);
        }

        public static IEnumerable<object[]> Invoke_EnumResult_Fallback_TestData()
        {
            yield return new object[]
            {
                nameof(IntrinsicInvokeEnumResultTarget.InstanceResult),
                new IntrinsicInvokeEnumResultTarget(),
                IntrinsicInvokeInt32Enum.Value
            };
            yield return new object[]
            {
                nameof(IntrinsicInvokeEnumResultTarget.StaticResult),
                null,
                IntrinsicInvokeInt64Enum.Value
            };
        }

        [Theory]
        [MemberData(nameof(Invoke_EnumResult_Fallback_TestData))]
        public void Invoke_EnumResult_FallbackPreservesDeclaredType(string methodName, object? target, object expected)
        {
            MethodInfo method = typeof(IntrinsicInvokeEnumResultTarget).GetMethod(methodName)!;

            object? result = method.Invoke(target, null);

            Assert.NotNull(result);
            Assert.Equal(method.ReturnType, result.GetType());
            Assert.Equal(expected, result);
            IntrinsicInvokeSelectionAssertions.AssertFallback(method);
        }

        [Fact]
        public void Invoke_SharedThunk_UsesNormalArgumentValidation()
        {
            var target = new IntrinsicInvokeArgumentValidationTarget();

            MethodInfo referenceMethod = typeof(IntrinsicInvokeArgumentValidationTarget).GetMethod(
                nameof(IntrinsicInvokeArgumentValidationTarget.Reference))!;
            Assert.Null(referenceMethod.Invoke(target, new object?[] { new IntrinsicInvokeReference(1) }));
            IntrinsicInvokeSelectionAssertions.AssertShared(referenceMethod);
            Assert.Throws<ArgumentException>(() => referenceMethod.Invoke(target, new object?[] { new object() }));

            MethodInfo primitiveMethod = typeof(IntrinsicInvokeArgumentValidationTarget).GetMethod(
                nameof(IntrinsicInvokeArgumentValidationTarget.Primitive))!;
            Assert.Null(primitiveMethod.Invoke(target, new object?[] { 1 }));
            IntrinsicInvokeSelectionAssertions.AssertShared(primitiveMethod);
            Assert.Throws<ArgumentException>(() => primitiveMethod.Invoke(target, new object?[] { 1L }));

            MethodInfo enumMethod = typeof(IntrinsicInvokeArgumentValidationTarget).GetMethod(
                nameof(IntrinsicInvokeArgumentValidationTarget.Enum))!;
            Assert.Null(enumMethod.Invoke(target, new object?[] { IntrinsicInvokeInt32Enum.Value }));
            IntrinsicInvokeSelectionAssertions.AssertShared(enumMethod);
            Assert.Throws<ArgumentException>(() => enumMethod.Invoke(target, new object?[] { IntrinsicInvokeInt64Enum.Value }));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsCoreCLR), nameof(PlatformDetection.IsReflectionEmitSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void Invoke_CollectibleMethodCanUnload(bool useMethodInvoker, bool referenceArgument)
        {
            WeakReference assembly = CreateAndInvokeCollectibleMethod(useMethodInvoker, referenceArgument);
            for (int i = 0; i < 10 && assembly.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Assert.False(assembly.IsAlive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateAndInvokeCollectibleMethod(bool useMethodInvoker, bool referenceArgument)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName(nameof(CreateAndInvokeCollectibleMethod)), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Module");
            TypeBuilder typeBuilder = module.DefineType("Target", TypeAttributes.Public);
            Type argumentType = referenceArgument ? typeof(object) : typeof(int);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                "Echo", MethodAttributes.Public | MethodAttributes.Static, argumentType, new[] { argumentType });
            ILGenerator il = methodBuilder.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ret);
            Type type = typeBuilder.CreateType();
            MethodInfo method = type.GetMethod("Echo");
            MethodInvoker? invoker = useMethodInvoker ? MethodInvoker.Create(method) : null;
            object expected = referenceArgument ? new object() : 42;
            object[] arguments = { expected };

            for (int i = 0; i <= IntrinsicInvokeSelectionAssertions.SpecializationThreshold; i++)
            {
                Assert.Equal(expected, invoker is not null ? invoker.Invoke(null, expected) : method.Invoke(null, arguments));
            }

            return new WeakReference(type.Assembly);
        }

        [Fact]
        public void CreateDelegate_PublicMethod()
        {
            Type typeTestClass = typeof(MI_BaseClass);

            MI_BaseClass baseClass = (MI_BaseClass)Activator.CreateInstance(typeTestClass);
            MethodInfo virtualMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.VirtualMethod));
            MethodInfo privateInstanceMethodInfo = GetMethod(typeTestClass, "PrivateInstanceMethod");
            MethodInfo publicStaticMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.PublicStaticMethod));

            Delegate methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            object returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { baseClass });
            Assert.Equal(baseClass.VirtualMethod(), returnValue);

            Delegate genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_TC_Int>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { baseClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = privateInstanceMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { baseClass });
            Assert.Equal(21, returnValue);

            genMethodDelegate = privateInstanceMethodInfo.CreateDelegate<Delegate_TC_Int>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { baseClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_Void_Int), baseClass);
            returnValue = ((Delegate_Void_Int)methodDelegate).DynamicInvoke(null);
            Assert.Equal(baseClass.VirtualMethod(), returnValue);

            genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_Void_Int>(baseClass);
            genReturnValue = genMethodDelegate.DynamicInvoke(null);
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = publicStaticMethodInfo.CreateDelegate(typeof(Delegate_Str_Str));
            returnValue = ((Delegate_Str_Str)methodDelegate).DynamicInvoke(new object[] { "85" });
            Assert.Equal("85", returnValue);

            genMethodDelegate = publicStaticMethodInfo.CreateDelegate<Delegate_Str_Str>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { "85" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = publicStaticMethodInfo.CreateDelegate(typeof(Delegate_Void_Str), "93");
            returnValue = ((Delegate_Void_Str)methodDelegate).DynamicInvoke(null);
            Assert.Equal("93", returnValue);

            genMethodDelegate = publicStaticMethodInfo.CreateDelegate<Delegate_Void_Str>("93");
            genReturnValue = genMethodDelegate.DynamicInvoke(null);
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_InheritedMethod()
        {
            Type typeTestClass = typeof(MI_BaseClass);
            Type TestSubClassType = typeof(MI_SubClass);

            MI_SubClass testSubClass = (MI_SubClass)Activator.CreateInstance(TestSubClassType);
            MI_BaseClass testClass = (MI_BaseClass)Activator.CreateInstance(typeTestClass);
            MethodInfo virtualMethodInfo = GetMethod(typeTestClass, nameof(MI_BaseClass.VirtualMethod));

            Delegate methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_TC_Int));
            object returnValue = ((Delegate_TC_Int)methodDelegate).DynamicInvoke(new object[] { testSubClass });
            Assert.Equal(testSubClass.VirtualMethod(), returnValue);

            Delegate genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_TC_Int>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { testSubClass });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = virtualMethodInfo.CreateDelegate(typeof(Delegate_Void_Int), testSubClass);
            returnValue = ((Delegate_Void_Int)methodDelegate).DynamicInvoke();
            Assert.Equal(testSubClass.VirtualMethod(), returnValue);

            genMethodDelegate = virtualMethodInfo.CreateDelegate<Delegate_Void_Int>(testSubClass);
            genReturnValue = genMethodDelegate.DynamicInvoke();
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_GenericMethod()
        {
            Type typeGenericClassString = typeof(MI_GenericClass<string>);

            MI_GenericClass<string> genericClass = (MI_GenericClass<string>)Activator.CreateInstance(typeGenericClassString);

            MethodInfo miMethod1String = GetMethod(typeGenericClassString, nameof(MI_GenericClass<string>.GenericMethod1));
            MethodInfo miMethod2String = GetMethod(typeGenericClassString, nameof(MI_GenericClass<string>.GenericMethod3));
            MethodInfo miMethod2IntGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(int) });
            MethodInfo miMethod2StringGeneric = miMethod2String.MakeGenericMethod(new Type[] { typeof(string) });

            Delegate methodDelegate = miMethod1String.CreateDelegate(typeof(Delegate_GC_T_T<string>));
            object returnValue = ((Delegate_GC_T_T<string>)methodDelegate).DynamicInvoke(new object[] { genericClass, "TestGeneric" });
            Assert.Equal(genericClass.GenericMethod1("TestGeneric"), returnValue);

            Delegate genMethodDelegate = miMethod1String.CreateDelegate<Delegate_GC_T_T<string>>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { genericClass, "TestGeneric" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod1String.CreateDelegate(typeof(Delegate_T_T<string>), genericClass);
            returnValue = ((Delegate_T_T<string>)methodDelegate).DynamicInvoke(new object[] { "TestGeneric" });
            Assert.Equal(genericClass.GenericMethod1("TestGeneric"), returnValue);

            genMethodDelegate = miMethod1String.CreateDelegate<Delegate_T_T<string>>(genericClass);
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { "TestGeneric" });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod2IntGeneric.CreateDelegate(typeof(Delegate_T_T<int>));
            returnValue = ((Delegate_T_T<int>)methodDelegate).DynamicInvoke(new object[] { 58 });
            Assert.Equal(58, returnValue);

            genMethodDelegate = miMethod2IntGeneric.CreateDelegate<Delegate_T_T<int>>();
            genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { 58 });
            Assert.Equal(returnValue, genReturnValue);

            methodDelegate = miMethod2StringGeneric.CreateDelegate(typeof(Delegate_Void_T<string>), "firstArg");
            returnValue = ((Delegate_Void_T<string>)methodDelegate).DynamicInvoke();
            Assert.Equal("firstArg", returnValue);

            genMethodDelegate = miMethod2StringGeneric.CreateDelegate<Delegate_Void_T<string>>("firstArg");
            genReturnValue = genMethodDelegate.DynamicInvoke();
            Assert.Equal(returnValue, genReturnValue);
        }

        [Fact]
        public void CreateDelegate_ValueTypeParameters()
        {
            MethodInfo miPublicStructMethod = GetMethod(typeof(MI_BaseClass), nameof(MI_BaseClass.PublicStructMethod));
            MI_BaseClass testClass = new MI_BaseClass();

            Delegate methodDelegate = miPublicStructMethod.CreateDelegate(typeof(Delegate_DateTime_Str));
            object returnValue = ((Delegate_DateTime_Str)methodDelegate).DynamicInvoke(new object[] { testClass, null });
            Assert.Equal(testClass.PublicStructMethod(new DateTime()), returnValue);

            Delegate genMethodDelegate = miPublicStructMethod.CreateDelegate<Delegate_DateTime_Str>();
            object genReturnValue = genMethodDelegate.DynamicInvoke(new object[] { testClass, null });
            Assert.Equal(returnValue, genReturnValue);
        }

        private interface IStaticInterface
        {
            public static virtual string? StaticVirtual(string? s) => s;
        }

        [Fact]
        public void CreateDelegate_StaticVirtual()
        {
            MethodInfo miStaticVirtual = GetMethod(typeof(IStaticInterface), nameof(IStaticInterface.StaticVirtual));
            const string testString = "test";

            Func<string?, string?> methodDelegate = miStaticVirtual.CreateDelegate<Func<string?, string?>>();
            string? returnValue = methodDelegate(testString);
            Assert.Equal(testString, returnValue);
        }

        [Theory]
        [InlineData(typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), null, typeof(ArgumentNullException))]
        [InlineData(typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Int), typeof(ArgumentException))]
        public void CreateDelegate_Invalid(Type type, string name, Type? delegateType, Type exceptionType)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Throws(exceptionType, () => methodInfo.CreateDelegate(delegateType));
        }

        public static IEnumerable<object[]> CreateDelegate_Target_Invalid_TestData()
        {
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), null, new MI_BaseClass(), typeof(ArgumentNullException) }; // DelegateType is null
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_TC_Int), new MI_BaseClass(), typeof(ArgumentException) }; // DelegateType is incorrect
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Int), new DummyClass(), typeof(ArgumentException) }; // Target is incorrect
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualMethod), typeof(Delegate_Void_Str), new DummyClass(), typeof(ArgumentException) }; // Target is incorrect
        }

        [Theory]
        [MemberData(nameof(CreateDelegate_Target_Invalid_TestData))]
        public void CreateDelegate_Target_Invalid(Type type, string name, Type delegateType, object target, Type exceptionType)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Throws(exceptionType, () => methodInfo.CreateDelegate(delegateType, target));
        }

        [Theory]
        [InlineData(typeof(Int32Attr), "[System.Reflection.Tests.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]")]
        [InlineData(typeof(Int64Attr), "[System.Reflection.Tests.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]")]
        [InlineData(typeof(StringAttr), "[System.Reflection.Tests.StringAttr(\"hello\", name = \"StringAttrSimple\")]")]
        [InlineData(typeof(EnumAttr), "[System.Reflection.Tests.EnumAttr((System.Reflection.Tests.PublicEnum)1, name = \"EnumAttrSimple\")]")]
        [InlineData(typeof(TypeAttr), "[System.Reflection.Tests.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]")]
        [InlineData(typeof(Attr), "[System.Reflection.Tests.Attr((Int32)77, name = \"AttrSimple\")]")]
        public void CustomAttributes(Type type, string expectedToString)
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "MethodWithAttributes");
            CustomAttributeData attributeData = methodInfo.CustomAttributes.First(attribute => attribute.AttributeType.Equals(type));
            Assert.Equal(expectedToString, attributeData.ToString());
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), true)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_GenericClass<int>.GenericMethod1), typeof(MI_GenericClass<>), nameof(MI_GenericClass<int>.GenericMethod1), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_GenericClass<int>.GenericMethod2), typeof(MI_GenericClass<string>), nameof(MI_GenericClass<int>.GenericMethod2), false)]
        public void EqualsTest(Type type1, string name1, Type type2, string name2, bool expected)
        {
            MethodInfo methodInfo1 = GetMethod(type1, name1);
            MethodInfo methodInfo2 = GetMethod(type2, name2);
            Assert.Equal(expected, methodInfo1.Equals(methodInfo2));
        }

        [Theory]
        //Verify two same MethodInfo objects are equal
        [InlineData("DummyMethod1", "DummyMethod1", true)]
        //Verify two different MethodInfo objects are not equal
        [InlineData("DummyMethod1", "DummyMethod2", false)]
        public void Equality1(string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(typeof(MethodInfoTests), str1);
            MethodInfo mi2 = GetMethod(typeof(MethodInfoTests), str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        public static IEnumerable<object[]> TestEqualityMethodData2()
        {
            //Verify two different MethodInfo objects with same name from two different classes are not equal
            yield return new object[] { typeof(Sample), typeof(SampleG<>), "Method1", "Method1", false };
            //Verify two different MethodInfo objects with same name from two different classes are not equal
            yield return new object[] { typeof(Sample), typeof(SampleG<string>), "Method2", "Method2", false };
        }

        [Theory]
        [MemberData(nameof(TestEqualityMethodData2))]
        public void Equality2(Type sample1, Type sample2, string str1, string str2, bool expected)
        {
            MethodInfo mi1 = GetMethod(sample1, str1);
            MethodInfo mi2 = GetMethod(sample2, str2);

            Assert.Equal(expected, mi1 == mi2);
            Assert.NotEqual(expected, mi1 != mi2);
        }

        [Theory]
        [InlineData(typeof(MethodInfoBaseDefinitionBaseClass), "InterfaceMethod1", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "InterfaceMethod1", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "BaseClassVirtualMethod", typeof(MethodInfoBaseDefinitionBaseClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "BaseClassMethod", typeof(MethodInfoBaseDefinitionSubClass))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "ToString", typeof(object))]
        [InlineData(typeof(MethodInfoBaseDefinitionSubClass), "DerivedClassMethod", typeof(MethodInfoBaseDefinitionSubClass))]
        public void GetBaseDefinition(Type type1, string name, Type type2)
        {
            MethodInfo method = GetMethod(type1, name).GetBaseDefinition();
            Assert.Equal(GetMethod(type2, name), method);
            Assert.Equal(MemberTypes.Method, method.MemberType);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), new string[] { "i", "l" })]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod), new string[] { "strArray" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment), new string[] { "location" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement), new string[] { "location" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange), new string[] { "location1", "value" })]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange), new string[] { "location1", "value", "comparand" })]
        public void GetParameters(Type type, string name, string[] expectedParameterNames)
        {
            MethodInfo method = GetMethod(type, name);
            ParameterInfo[] parameters = method.GetParameters();

            Assert.Equal(expectedParameterNames.Length, parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                Assert.Equal(parameters[i].Name, expectedParameterNames[i]);
            }
        }

        [Fact]
        public void GetParameters_IsDeepCopy()
        {
            MethodInfo method = GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong));
            ParameterInfo[] parameters = method.GetParameters();
            parameters[0] = null;

            // If GetParameters is a deep copy, then this change
            // should not affect another call to GetParameters()
            ParameterInfo[] parameters2 = method.GetParameters();
            for (int i = 0; i < parameters2.Length; i++)
            {
                Assert.NotNull(parameters2[i]);
            }
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), true)]
        public void ContainsGenericParameters(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).ContainsGenericParameters);
        }

        [Fact]
        public void InvokeUninstantiatedGenericMethod()
        {
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.StaticGenericMethod)).Invoke(null, [null]));
        }

        [Fact]
        public void InvokeUninstantiatedGenericType_GenericMethod()
        {
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_GenericClass<>), "GenericMethod4").Invoke(null, [null]));
        }

        [Fact]
        public void InvokeUninstantiatedGenericType_NonGenericMethod()
        {
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_GenericClass<>), "NonGenericMethod").Invoke(null, [null]));
        }

        [Fact]
        public void GetFunctionPointerFromUninstantiatedGenericMethod()
        {
            RuntimeMethodHandle handle = typeof(MI_SubClass).GetMethod(nameof(MI_SubClass.StaticGenericMethod))!.MethodHandle;
            Assert.Throws<InvalidOperationException>(() => handle.GetFunctionPointer());
        }

        [Fact]
        public void GetFunctionPointerOnUninstantiatedGenericType_GenericMethod()
        {
            RuntimeMethodHandle handle = typeof(MI_GenericClass<>).GetMethod("GenericMethod4")!.MethodHandle;
            Assert.Throws<InvalidOperationException>(() => handle.GetFunctionPointer());
        }

        [Fact]
        public void GetFunctionPointerOnUninstantiatedGenericType_NonGenericMethod()
        {
            RuntimeMethodHandle handle = typeof(MI_GenericClass<>).GetMethod("NonGenericMethod")!.MethodHandle;
            Assert.Throws<InvalidOperationException>(() => handle.GetFunctionPointer());
        }

        [Fact]
        public void GetHashCodeTest()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "VoidMethodReturningInt");
            Assert.NotEqual(0, methodInfo.GetHashCode());
        }

        [Fact]
        public void GetHashCode_MultipleSubClasses_ShouldBeUnique()
        {
            var numberOfCollisions = 0;
            var hashset = new HashSet<int>();

            foreach (var type in new Type[] { typeof(MI_BaseClass), typeof(MI_SubClassA), typeof(MI_SubClassB), typeof(MI_SubClassC) })
            {
                foreach (var methodInfo in type.GetMethods())
                {
                    if (!hashset.Add(methodInfo.GetHashCode()))
                    {
                        numberOfCollisions++;
                    }
                }
            }

            // If intermittent failures are observed, it's acceptable to relax the assertion to allow some collisions.
            Assert.Equal(0, numberOfCollisions);
        }

        public static IEnumerable<object[]> Invoke_TestData()
        {
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualReturnIntMethod), new MI_BaseClass(), null, 0 };
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_BaseClass.VirtualReturnIntMethod), new MethodInfoDummySubClass(), null, 1 }; // From parent class

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), new MI_SubClass(), new object[] { 42 }, "42" }; // Box primitive integer
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), new MI_SubClass(), null, 3 }; // No parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), new MI_SubClass(), null, long.MaxValue }; // No parameters

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), new MI_SubClass(), new object[] { 200, 10000 }, 10200L }; // Primitive parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), null, new object[] { 10, 100 }, 110 }; // Static primitive parameters
            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), new MI_SubClass(), new object[] { 10, 100 }, 110 }; // Static primitive parameters
            yield return new object[] { typeof(MI_BaseClass), nameof(MI_SubClass.StaticIntMethodReturningBool), new MI_SubClass(), new object[] { 10 }, true }; // Static from parent class

            yield return new object[] { typeof(MI_SubClass), nameof(MI_SubClass.EnumMethodReturningEnum), new MI_SubClass(), new object[] { PublicEnum.Case1 }, PublicEnum.Case2 }; // Enum
            yield return new object[] { typeof(MI_Interface), nameof(MI_Interface.IMethod), new MI_SubClass(), new object[0], 10 }; // Interface
            yield return new object[] { typeof(MI_Interface), nameof(MI_Interface.IMethodNew), new MI_SubClass(), new object[0], 20 }; // Interface

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Integer", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, 1 }; // Default int parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Integer", new MethodInfoDefaultParameters(), new object[] { 2 }, 2 }; // Default int parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), Enumerable.Repeat(Type.Missing, 13), "True, test, c, 2, -1, -3, 4, -5, 6, -7, 8, 9.1, 11.12" }; // Default parameters, all missing

            object[] allPrimitives = new object[] { false, "value", 'd', (byte)102, (sbyte)-101, (short)-103, (ushort)104, -105, (uint)106, (long)-107, (ulong)108, 109.1f, 111.12 };
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), allPrimitives, "False, value, d, 102, -101, -103, 104, -105, 106, -107, 108, 109.1, 111.12" }; // Default parameters, all present

            object[] somePrimitives = new object[] { false, Type.Missing, 'd', Type.Missing, (sbyte)-101, Type.Missing, (ushort)104, Type.Missing, (uint)106, Type.Missing, (ulong)108, Type.Missing, 111.12 };
            yield return new object[] { typeof(MethodInfoDefaultParameters), "AllPrimitives", new MethodInfoDefaultParameters(), somePrimitives, "False, test, d, 2, -101, -3, 104, -5, 106, -7, 108, 9.1, 111.12" }; // Default parameters, some present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, "test" }; // Default string parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new object[] { "value" }, "value" }; // Default string parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Reference", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, null }; // Default reference parameter, missing
            object referenceType = new MethodInfoDefaultParameters.CustomReferenceType();
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Reference", new MethodInfoDefaultParameters(), new object[] { referenceType }, referenceType }; // Default reference parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "ValueType", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new MethodInfoDefaultParameters.CustomValueType() { Id = 0 } }; // Default value type parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "ValueType", new MethodInfoDefaultParameters(), new object[] { new MethodInfoDefaultParameters.CustomValueType() { Id = 1 } }, new MethodInfoDefaultParameters.CustomValueType() { Id = 1 } }; // Default value type parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "DateTime", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new DateTime(42) }; // Default DateTime parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "DateTime", new MethodInfoDefaultParameters(), new object[] { new DateTime(43) }, new DateTime(43) }; // Default DateTime parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "DecimalWithAttribute", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, new decimal(4, 3, 2, true, 1) }; // Default decimal parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "DecimalWithAttribute", new MethodInfoDefaultParameters(), new object[] { new decimal(12, 13, 14, true, 1) }, new decimal(12, 13, 14, true, 1) }; // Default decimal parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Decimal", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, 3.14m }; // Default decimal parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "Decimal", new MethodInfoDefaultParameters(), new object[] { 103.14m }, 103.14m }; // Default decimal parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "NullableInt", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, null }; // Default nullable parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "NullableInt", new MethodInfoDefaultParameters(), new object[] { (int?)42 }, (int?)42 }; // Default nullable parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "Enum", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, PublicEnum.Case1 }; // Default enum parameter, missing

            yield return new object[] { typeof(MethodInfoDefaultParametersInterface), "InterfaceMethod", new MethodInfoDefaultParameters(), new object[] { Type.Missing, Type.Missing, Type.Missing }, "1, test, 3.14" }; // Default interface parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParametersInterface), "InterfaceMethod", new MethodInfoDefaultParameters(), new object[] { 101, "value", 103.14m }, "101, value, 103.14" }; // Default interface parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "StaticMethod", null, new object[] { Type.Missing, Type.Missing, Type.Missing }, "1, test, 3.14" }; // Default static parameter, missing
            yield return new object[] { typeof(MethodInfoDefaultParameters), "StaticMethod", null, new object[] { 101, "value", 103.14m }, "101, value, 103.14" }; // Default static parameter, present

            yield return new object[] { typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new object[] { "value" }, "value" }; // Default static parameter, present
            yield return new object[] { typeof(MethodInfoDefaultParameters), "String", new MethodInfoDefaultParameters(), new string[] { "value" }, "value" }; // String array
        }

        [Theory]
        [MemberData(nameof(Invoke_TestData))]
        public void InvokeWithTestData(Type methodDeclaringType, string methodName, object obj, object[] parameters, object result)
        {
            MethodInfo method = GetMethod(methodDeclaringType, methodName);
            Assert.Equal(result, method.Invoke(obj, parameters));
        }

        [Fact]
        public void Invoke_ParameterSpecification_ArrayOfMissing()
        {
            InvokeWithTestData(typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new object[] { Type.Missing }, Type.Missing);
            InvokeWithTestData(typeof(MethodInfoDefaultParameters), "OptionalObjectParameter", new MethodInfoDefaultParameters(), new Missing[] { Missing.Value }, Missing.Value);
        }

        [Fact]
        [ActiveIssue("https://github.com/mono/mono/issues/15025", TestRuntimes.Mono)]
        public static void Invoke_OptionalParameterUnassingableFromMissing_WithMissingValue_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MethodInfoDefaultParameters), "OptionalStringParameter").Invoke(new MethodInfoDefaultParameters(), new object[] { Type.Missing }));
        }

        [Fact]
        public void Invoke_TwoParameters_CustomBinder_IncorrectTypeArguments()
        {
            MethodInfo method = GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt));
            var args = new object[] { "10", "100" };
            Assert.Equal(110, method.Invoke(null, BindingFlags.Default, new ConvertStringToIntBinder(), args, null));
            Assert.True(args[0] is int);
            Assert.True(args[1] is int);
        }

        [Fact]
        public void Invoke_CustomBinder_ResultRequiringPrimitiveWidening_DoesNotCopyBackWidenedArgument()
        {
            MethodInfo method = GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt));
            object[] args = new object[] { "10", 100 };

            Assert.Equal(110, method.Invoke(null, BindingFlags.Default, new ConvertStringToInt16Binder(), args, null));
            Assert.Equal("10", Assert.IsType<string>(args[0]));
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), new Type[] { typeof(int) })]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), new Type[] { typeof(string), typeof(int) })]
        public void MakeGenericMethod(Type type, string name, Type[] typeArguments)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(typeArguments);
            Assert.True(genericMethodInfo.IsGenericMethod);
            Assert.False(genericMethodInfo.IsGenericMethodDefinition);

            MethodInfo genericMethodDefinition = genericMethodInfo.GetGenericMethodDefinition();
            Assert.Equal(methodInfo, genericMethodDefinition);
            Assert.True(genericMethodDefinition.IsGenericMethod);
            Assert.True(genericMethodDefinition.IsGenericMethodDefinition);
        }

        [Fact]
        public void MakeGenericMethod_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod(null)); // TypeArguments is null
            Assert.Throws<ArgumentNullException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2)).MakeGenericMethod(typeof(string), null)); // TypeArguments has null Type
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt)).MakeGenericMethod(typeof(int))); // Method is non generic

            // Number of typeArguments does not match
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod());
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1)).MakeGenericMethod(typeof(string), typeof(int)));
            AssertExtensions.Throws<ArgumentException>(null, () => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2)).MakeGenericMethod(typeof(int)));
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod1), 1)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), 2)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), 0)]
        public void GetGenericArguments(Type type, string name, int expectedCount)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Type[] genericArguments = methodInfo.GetGenericArguments();
            Assert.Equal(expectedCount, genericArguments.Length);
        }

        [Fact]
        public void GetGenericMethodDefinition_MethodNotGeneric_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => GetMethod(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt)).GetGenericMethodDefinition());
        }

        [Fact]
        public void Attributes()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "ReturnVoidMethod");
            MethodAttributes attributes = methodInfo.Attributes;
            Assert.True(attributes.HasFlag(MethodAttributes.Public));
        }

        [Fact]
        public void CallingConvention()
        {
            MethodInfo methodInfo = GetMethod(typeof(MI_SubClass), "ReturnVoidMethod");
            CallingConventions callingConvention = methodInfo.CallingConvention;
            Assert.True(callingConvention.HasFlag(CallingConventions.HasThis));
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_AbstractBaseClass), nameof(MI_AbstractBaseClass.AbstractMethod), true)]
        public void IsAbstract(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsAbstract);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsConstructor(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsConstructor);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamily(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamily);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamilyAndAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamilyAndAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsFamilyOrAssembly(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFamilyOrAssembly);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.VirtualMethod), true)]
        public void IsFinal(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsFinal);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), false)]
        public void IsGenericMethodDefinition(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsGenericMethodDefinition);
        }

        [Theory]
        [InlineData(typeof(MI_AbstractSubClass), nameof(MI_AbstractSubClass.AbstractMethod), true)]
        public void IsHideBySig(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsHideBySig);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsPrivate(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsPrivate);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), true)]
        public void IsPublic(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsPublic);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        public void IsSpecialName(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsSpecialName);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), true)]
        public void IsStatic(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsStatic);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), false)]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnBoolMethod), true)]
        public void IsVirtual(Type type, string name, bool expected)
        {
            Assert.Equal(expected, GetMethod(type, name).IsVirtual);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange))]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange))]
        public void Name(Type type, string name)
        {
            MethodInfo mi = GetMethod(type, name);
            Assert.Equal(name, mi.Name);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StaticIntIntMethodReturningInt), typeof(int))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), typeof(void))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningInt), typeof(int))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ObjectMethodReturningString), typeof(string))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnStringArrayMethod), typeof(string[]))]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VirtualReturnBoolMethod), typeof(bool))]
        public void ReturnType_ReturnParameter(Type type, string name, Type expected)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Equal(expected, methodInfo.ReturnType);

            Assert.Equal(methodInfo.ReturnType, methodInfo.ReturnParameter.ParameterType);
            Assert.Null(methodInfo.ReturnParameter.Name);
            Assert.Equal(-1, methodInfo.ReturnParameter.Position);
        }

        [Theory]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.VoidMethodReturningLong), "Int64 VoidMethodReturningLong()")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.IntLongMethodReturningLong), "Int64 IntLongMethodReturningLong(Int32, Int64)")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.StringArrayMethod), "Void StringArrayMethod(System.String[])")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.ReturnVoidMethod), "Void ReturnVoidMethod(System.DateTime)")]
        [InlineData(typeof(MI_SubClass), nameof(MI_SubClass.GenericMethod2), "Void GenericMethod2[T,U](T, U)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Increment), "Int32 Increment(Int32 ByRef)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Decrement), "Int32 Decrement(Int32 ByRef)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.Exchange), "Int32 Exchange(Int32 ByRef, Int32)")]
        [InlineData(typeof(MI_Interlocked), nameof(MI_Interlocked.CompareExchange), "Int32 CompareExchange(Int32 ByRef, Int32, Int32)")]
        [InlineData(typeof(MI_GenericClass<>), nameof(MI_GenericClass<string>.GenericMethod1), "T GenericMethod1(T)")]
        [InlineData(typeof(MI_GenericClass<>), nameof(MI_GenericClass<string>.GenericMethod2), "T GenericMethod2[S](S, T, System.String)")]
        [InlineData(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod1), "System.String GenericMethod1(System.String)")]
        [InlineData(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod2), "System.String GenericMethod2[S](S, System.String, System.String)")]
        public void ToStringTest(Type type, string name, string expected)
        {
            MethodInfo methodInfo = GetMethod(type, name);
            Assert.Equal(expected, methodInfo.ToString());
        }

        public static IEnumerable<object[]> ToString_TestData()
        {
            MethodInfo genericMethodInfo = GetMethod(typeof(MI_GenericClass<string>), nameof(MI_GenericClass<string>.GenericMethod2)).MakeGenericMethod(new Type[] { typeof(DateTime) });
            yield return new object[] { genericMethodInfo, "System.String GenericMethod2[DateTime](System.DateTime, System.String, System.String)" };
        }

        [Theory]
        [MemberData(nameof(ToString_TestData))]
        public void ToStringTest_ByMethodInfo(MethodInfo methodInfo, string expected)
        {
            Assert.Equal(expected, methodInfo.ToString());
        }

        public static IEnumerable<object[]> MethodNameAndArguments()
        {
            yield return new object[] { nameof(Sample.DefaultString), "Hello", "Hi" };
            yield return new object[] { nameof(Sample.DefaultNullString), null, "Hi" };
            yield return new object[] { nameof(Sample.DefaultNullableInt), 3, 5 };
            yield return new object[] { nameof(Sample.DefaultNullableEnum), YesNo.Yes, YesNo.No };
        }

        [Theory]
        [MemberData(nameof(MethodNameAndArguments))]
        public static void InvokeCopiesBackMissingArgument(string methodName, object defaultValue, object passingValue)
        {
            MethodInfo method = typeof(Sample).GetMethod(methodName);
            object[] args = new object[] { Missing.Value };

            Assert.Equal(defaultValue, method.Invoke(null, args));
            Assert.Equal(defaultValue, args[0]);

            args[0] = passingValue;

            Assert.Equal(passingValue, method.Invoke(null, args));
            Assert.Equal(passingValue, args[0]);

            args[0] = null;
            Assert.Null(method.Invoke(null, args));
            Assert.Null(args[0]);
        }

        [Fact]
        public static void InvokeCopiesBackMissingParameterAndArgument()
        {
            MethodInfo method = typeof(Sample).GetMethod(nameof(Sample.DefaultMissing));
            object[] args = new object[] { Missing.Value };

            Assert.Null(method.Invoke(null, args));
            Assert.Null(args[0]);

            args[0] = null;
            Assert.Null(method.Invoke(null, args));
            Assert.Null(args[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50957", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]
        public static void CallStackFrame_AggressiveInlining()
        {
            MethodInfo mi = typeof(System.Reflection.TestAssembly.ClassToInvoke).GetMethod(nameof(System.Reflection.TestAssembly.ClassToInvoke.CallMe_AggressiveInlining),
                BindingFlags.Public | BindingFlags.Static)!;

            // Although the target method has AggressiveInlining, currently reflection should not inline the target into any generated IL.
            FirstCall(mi);
            SecondCall(mi);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Separate non-inlineable method to aid any test failures
        private static void FirstCall(MethodInfo mi)
        {
            Assembly asm = (Assembly)mi.Invoke(null, null);
            Assert.Contains("TestAssembly", asm.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Separate non-inlineable method to aid any test failures
        private static void SecondCall(MethodInfo mi)
        {
            Assembly asm = (Assembly)mi.Invoke(null, null);
            Assert.Contains("TestAssembly", asm.ToString());
        }

        //Methods for Reflection Metadata
        private void DummyMethod1(string str, int iValue, long lValue)
        {
        }

        private void DummyMethod2()
        {
        }
    }

    internal static class IntrinsicInvokeSelectionAssertions
    {
        internal const int SpecializationThreshold = 10_000;
        private const string ForceEmitInvokeSwitch = "Switch.System.Reflection.ForceEmitInvoke";
        private const string ForceInterpretedInvokeSwitch = "Switch.System.Reflection.ForceInterpretedInvoke";
        private const string SharedThunkMethodName = "InvokeWithSharedThunk";
        private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void AssertShared(MethodBase method)
        {
            if (ShouldAssertSharedSelection)
            {
                AssertShared(GetCachedInvoker(method));
            }
        }

        internal static void AssertShared(object invoker)
        {
            if (!ShouldAssertSharedSelection)
            {
                return;
            }

            Assert.Equal(SharedThunkMethodName, GetRefArgsDelegate(invoker).Method.Name);
            Assert.NotEqual(IntPtr.Zero, GetInvokeStateField<IntPtr>(invoker, "Thunk"));
        }

        internal static void AssertFallback(MethodBase method)
        {
            if (!ShouldAssertSharedSelection)
            {
                return;
            }

            AssertFallback(GetCachedInvoker(method));
        }

        internal static void AssertFallback(object invoker)
        {
            if (!ShouldAssertSharedSelection)
            {
                return;
            }

            Assert.NotEqual(SharedThunkMethodName, GetRefArgsDelegate(invoker).Method.Name);
            Assert.Equal(IntPtr.Zero, GetInvokeStateField<IntPtr>(invoker, "Thunk"));
        }

        internal static void AssertPromoted(object invoker)
        {
            if (!ShouldAssertSharedSelection)
            {
                return;
            }

            if (!ShouldAssertPromotion)
            {
                AssertNotPromoted(invoker, 0);
                return;
            }

            Assert.Equal(SpecializationThreshold, GetInvokeStateField<int>(invoker, "InvocationCount"));
            Assert.True(
                GetRefArgsDelegate(invoker).Method.Name != SharedThunkMethodName ||
                GetOptionalDelegate(invoker, "_invokeFunc_Obj4Args") is not null ||
                GetOptionalDelegate(invoker, "_invokeFunc_ObjSpanArgs") is not null);
        }

        internal static void AssertNotPromoted(object invoker, int invocationCount)
        {
            if (!ShouldAssertSharedSelection)
            {
                return;
            }

            AssertShared(invoker);
            Assert.Null(GetOptionalDelegate(invoker, "_invokeFunc_Obj4Args"));
            Assert.Null(GetOptionalDelegate(invoker, "_invokeFunc_ObjSpanArgs"));
            Assert.Equal(ShouldAssertPromotion ? invocationCount : 0, GetInvokeStateField<int>(invoker, "InvocationCount"));
        }

        private static bool ShouldAssertSharedSelection =>
            PlatformDetection.IsCoreCLR && (!IsForceEmitOnly || !RuntimeFeature.IsDynamicCodeCompiled);

        private static bool ShouldAssertPromotion =>
            ShouldAssertSharedSelection &&
            RuntimeFeature.IsDynamicCodeCompiled &&
            !IsForceInterpretedOnly;

        private static bool IsForceEmitOnly =>
            IsSwitchEnabled(ForceEmitInvokeSwitch) && !IsSwitchEnabled(ForceInterpretedInvokeSwitch);

        private static bool IsForceInterpretedOnly =>
            IsSwitchEnabled(ForceInterpretedInvokeSwitch) && !IsSwitchEnabled(ForceEmitInvokeSwitch);

        private static bool IsSwitchEnabled(string name) =>
            AppContext.TryGetSwitch(name, out bool enabled) && enabled;

        private static object GetCachedInvoker(MethodBase method)
        {
            FieldInfo? field = method.GetType().GetField("m_invoker", InstanceFields);
            Assert.NotNull(field);
            object? invoker = field.GetValue(method);
            Assert.NotNull(invoker);
            return invoker;
        }

        private static Delegate GetRefArgsDelegate(object invoker)
        {
            FieldInfo? field = invoker.GetType().GetField("_invokeFunc_RefArgs", InstanceFields);
            Assert.NotNull(field);
            object? value = field.GetValue(invoker);
            Assert.NotNull(value);
            return Assert.IsAssignableFrom<Delegate>(value);
        }

        private static Delegate? GetOptionalDelegate(object invoker, string fieldName)
        {
            FieldInfo? field = invoker.GetType().GetField(fieldName, InstanceFields);
            return field?.GetValue(invoker) as Delegate;
        }

        private static T GetInvokeStateField<T>(object invoker, string fieldName)
        {
            FieldInfo? invokeStateField = invoker.GetType().GetField("_invokeState", InstanceFields);
            Assert.NotNull(invokeStateField);
            object? invokeState = invokeStateField.GetValue(invoker);
            Assert.NotNull(invokeState);
            FieldInfo? valueField = invokeState.GetType().GetField(fieldName, InstanceFields);
            Assert.NotNull(valueField);
            object? value = valueField.GetValue(invokeState);
            Assert.NotNull(value);
            return (T)value;
        }
    }

    internal static class IntrinsicInvokeTestData
    {
        internal static IEnumerable<object[]> PrimitiveValues()
        {
            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(byte), (byte)42 };
            yield return new object[] { typeof(sbyte), (sbyte)-42 };
            yield return new object[] { typeof(char), 'x' };
            yield return new object[] { typeof(short), (short)-1234 };
            yield return new object[] { typeof(ushort), (ushort)1234 };
            yield return new object[] { typeof(int), -12345 };
            yield return new object[] { typeof(uint), 12345u };
            yield return new object[] { typeof(long), -1234567890123L };
            yield return new object[] { typeof(ulong), 1234567890123UL };
            yield return new object[] { typeof(float), 12.5f };
            yield return new object[] { typeof(double), -25.5 };
            yield return new object[] { typeof(nint), (nint)12345 };
            yield return new object[] { typeof(nuint), (nuint)54321 };
        }

        internal static IEnumerable<object[]> EnumValues()
        {
            yield return new object[] { typeof(IntrinsicInvokeByteEnum), IntrinsicInvokeByteEnum.Value };
            yield return new object[] { typeof(IntrinsicInvokeSByteEnum), IntrinsicInvokeSByteEnum.Value };
            yield return new object[] { typeof(IntrinsicInvokeInt16Enum), IntrinsicInvokeInt16Enum.Value };
            yield return new object[] { typeof(IntrinsicInvokeUInt16Enum), IntrinsicInvokeUInt16Enum.Value };
            yield return new object[] { typeof(IntrinsicInvokeInt32Enum), IntrinsicInvokeInt32Enum.Value };
            yield return new object[] { typeof(IntrinsicInvokeUInt32Enum), IntrinsicInvokeUInt32Enum.Value };
            yield return new object[] { typeof(IntrinsicInvokeInt64Enum), IntrinsicInvokeInt64Enum.Value };
            yield return new object[] { typeof(IntrinsicInvokeUInt64Enum), IntrinsicInvokeUInt64Enum.Value };
        }
    }

    internal interface IIntrinsicInvokeReference
    {
        int Value { get; }
    }

    internal sealed class IntrinsicInvokeReference : IIntrinsicInvokeReference
    {
        internal IntrinsicInvokeReference(int value) => Value = value;

        public int Value { get; }
    }

    internal sealed class IntrinsicInvokeReferenceTarget
    {
        internal int CallCount { get; private set; }
        internal object? LastValue { get; private set; }
        internal object Sentinel { get; } = new object();

        public void Void0() => Record(Sentinel);

        public void Void1(IIntrinsicInvokeReference value) => Record(value);

        public void Void2(object[] values, Action callback) => Record(callback, values);

        public void Void3(Task<string> task, IIntrinsicInvokeReference value, string[] values) =>
            Record(values, task, value);

        public void Void4(object value, IIntrinsicInvokeReference reference, Action callback, Task<int> task) =>
            Record(task, value, reference, callback);

        public object Return0() => CollectAndReturn(Sentinel);

        public IIntrinsicInvokeReference Return1(IIntrinsicInvokeReference value) => CollectAndReturn(value);

        public object[] Return2(object[] values, Action callback) => CollectAndReturn(values, callback);

        public Func<string> Return3(Task<string> task, Func<string> callback, IIntrinsicInvokeReference value) =>
            CollectAndReturn(callback, task, value);

        public Task<string> Return4(
            Task<string> task,
            object[] values,
            IIntrinsicInvokeReference reference,
            Action callback) =>
            CollectAndReturn(task, values, reference, callback);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Record(object? value, params object?[] keepAlive)
        {
            GC.Collect();
            CallCount++;
            LastValue = value;
            GC.KeepAlive(keepAlive);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T CollectAndReturn<T>(T value, params object?[] keepAlive)
        {
            GC.Collect();
            GC.KeepAlive(keepAlive);
            return value;
        }
    }

    internal sealed class IntrinsicInvokePrimitiveReturnTarget<T>
    {
        public T Value { get; set; }

        public T GetValue()
        {
            GC.Collect();
            return Value;
        }
    }

    internal sealed class IntrinsicInvokePrimitiveArgumentTarget<T>
    {
        public object? Value;

        public void SetValue(T value)
        {
            GC.Collect();
            Value = value;
        }
    }

    internal sealed class IntrinsicInvokeInstancePatternTarget
    {
        internal float X { get; private set; }
        internal float Y { get; private set; }
        internal float Z { get; private set; }
        internal int W { get; private set; }

        public int Sum(IIntrinsicInvokeReference reference, object[] values)
        {
            GC.Collect();
            return reference.Value + values.Length;
        }

        public void SetVector(float x, float y, float z, int w)
        {
            GC.Collect();
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public object FiveReferenceArguments(object first, object second, object third, object fourth, object fifth)
        {
            GC.Collect();
            GC.KeepAlive(first);
            GC.KeepAlive(second);
            GC.KeepAlive(third);
            GC.KeepAlive(fourth);
            return fifth;
        }
    }

    internal static class IntrinsicInvokeStaticIntReturnTarget
    {
        public static string ReturnString(int value)
        {
            GC.Collect();
            return value.ToString();
        }

        public static Type? ReturnGeneric<T>(int value)
        {
            GC.Collect();
            return value == 42 ? typeof(T) : null;
        }
    }

    internal static class IntrinsicInvokeGenericStaticIntReturnTarget<T> where T : IComparable
    {
        public static IComparable ReturnComparable(int value)
        {
            T result = (T)(object)value;
            GC.Collect();
            return result;
        }
    }

    internal static class IntrinsicInvokeByRefTarget
    {
        public static bool ReturnFalse(IIntrinsicInvokeReference input, out IIntrinsicInvokeReference output)
        {
            GC.Collect();
            output = input;
            return false;
        }

        public static bool ReturnNull(object[] input, out Action? output)
        {
            GC.Collect();
            output = null;
            GC.KeepAlive(input);
            return true;
        }

        public static bool ReturnTrue(Task<string> input, out Task<string> output)
        {
            GC.Collect();
            output = input;
            return true;
        }

        public static bool ThrowAfterWrite(IIntrinsicInvokeReference input, out IIntrinsicInvokeReference output)
        {
            output = input;
            GC.Collect();
            throw new InvalidOperationException();
        }
    }

    internal static class IntrinsicInvokeStaticReferenceTarget
    {
        public static object[] Return2(IIntrinsicInvokeReference reference, object[] values)
        {
            GC.Collect();
            GC.KeepAlive(reference);
            return values;
        }

        public static Task<string> Return3(Task<string> task, Action callback, IIntrinsicInvokeReference reference)
        {
            GC.Collect();
            GC.KeepAlive(callback);
            GC.KeepAlive(reference);
            return task;
        }

        public static Action Return4(
            Action callback,
            object[] values,
            IIntrinsicInvokeReference reference,
            Task<int> task)
        {
            GC.Collect();
            GC.KeepAlive(values);
            GC.KeepAlive(reference);
            GC.KeepAlive(task);
            return callback;
        }

        public static void Void3(Task<string> task, Action callback, IIntrinsicInvokeReference reference)
        {
            GC.Collect();
            callback();
            GC.KeepAlive(task);
            GC.KeepAlive(reference);
        }

        public static void Void4(
            object[] values,
            IIntrinsicInvokeReference reference,
            Action callback,
            Task<int> task)
        {
            GC.Collect();
            callback();
            GC.KeepAlive(values);
            GC.KeepAlive(reference);
            GC.KeepAlive(task);
        }
    }

    internal sealed class IntrinsicInvokeActionTracker
    {
        internal int CallCount { get; private set; }
        internal Action Callback => Invoke;

        private void Invoke() => CallCount++;
    }

    internal class IntrinsicInvokeVirtualDispatchBase
    {
        public virtual object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "virtual-base";
        }
    }

    internal sealed class IntrinsicInvokeVirtualDispatchA : IntrinsicInvokeVirtualDispatchBase
    {
        public override object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "virtual-a";
        }
    }

    internal sealed class IntrinsicInvokeVirtualDispatchB : IntrinsicInvokeVirtualDispatchBase
    {
        public override object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "virtual-b";
        }
    }

    internal abstract class IntrinsicInvokeAbstractDispatchBase
    {
        public abstract object Dispatch(object value);
    }

    internal sealed class IntrinsicInvokeAbstractDispatchA : IntrinsicInvokeAbstractDispatchBase
    {
        public override object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "abstract-a";
        }
    }

    internal sealed class IntrinsicInvokeAbstractDispatchB : IntrinsicInvokeAbstractDispatchBase
    {
        public override object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "abstract-b";
        }
    }

    internal interface IIntrinsicInvokeInterfaceDispatch
    {
        object Dispatch(object value);
    }

    internal sealed class IntrinsicInvokeInterfaceDispatchA : IIntrinsicInvokeInterfaceDispatch
    {
        public object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "interface-a";
        }
    }

    internal sealed class IntrinsicInvokeInterfaceDispatchB : IIntrinsicInvokeInterfaceDispatch
    {
        public object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "interface-b";
        }
    }

    internal interface IIntrinsicInvokeDefaultInterfaceDispatch
    {
        object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "default";
        }
    }

    internal sealed class IntrinsicInvokeDefaultInterfaceDispatch : IIntrinsicInvokeDefaultInterfaceDispatch
    {
    }

    internal sealed class IntrinsicInvokeDefaultInterfaceOverride : IIntrinsicInvokeDefaultInterfaceDispatch
    {
        public object Dispatch(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return "override";
        }
    }

    internal delegate object? IntrinsicInvokeStaticDynamicCallback(object? value);
    internal delegate object? IntrinsicInvokeInstanceDynamicCallback(object? value);
    internal delegate object? IntrinsicInvokeMulticastDynamicCallback(object? value);

    internal abstract class IntrinsicInvokeGenericVirtualDispatch
    {
        public abstract object Dispatch<T>(object value);
    }

    internal sealed class IntrinsicInvokeGenericVirtualA : IntrinsicInvokeGenericVirtualDispatch
    {
        public override object Dispatch<T>(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return typeof(T).Name + "-generic-a";
        }
    }

    internal sealed class IntrinsicInvokeGenericVirtualB : IntrinsicInvokeGenericVirtualDispatch
    {
        public override object Dispatch<T>(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return typeof(T).Name + "-generic-b";
        }
    }

    internal interface IIntrinsicInvokeGenericInterfaceDispatch
    {
        object Dispatch<T>(object value);
    }

    internal sealed class IntrinsicInvokeGenericInterfaceA : IIntrinsicInvokeGenericInterfaceDispatch
    {
        public object Dispatch<T>(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return typeof(T).Name + "-interface-a";
        }
    }

    internal sealed class IntrinsicInvokeGenericInterfaceB : IIntrinsicInvokeGenericInterfaceDispatch
    {
        public object Dispatch<T>(object value)
        {
            GC.Collect();
            GC.KeepAlive(value);
            return typeof(T).Name + "-interface-b";
        }
    }

    internal sealed class IntrinsicInvokeDelegatePayload
    {
        internal List<string> Log { get; } = new List<string>();
    }

    internal sealed class IntrinsicInvokeDelegateCallbacks
    {
        private readonly string _label;

        internal IntrinsicInvokeDelegateCallbacks(string label) => _label = label;

        internal static object? Static(object? value) => Record(value, "static");

        internal object? Instance(object? value) => Record(value, _label);

        internal static object? MulticastStatic(object? value) => Record(value, "static");

        internal object? MulticastInstance(object? value) => Record(value, _label);

        private static object? Record(object? value, string label)
        {
            GC.Collect();
            ((IntrinsicInvokeDelegatePayload)value!).Log.Add(label);
            return value;
        }
    }

    internal static class IntrinsicInvokeExcludedMethodTarget
    {
        public static object DateTimeArgument(DateTime value)
        {
            GC.Collect();
            return value.Day;
        }

        public static DateTime DateTimeResult()
        {
            GC.Collect();
            return new DateTime(2026, 9, 10);
        }

        public static object NullableArgument(int? value)
        {
            GC.Collect();
            return value.GetValueOrDefault();
        }

        public static int? NullableResult()
        {
            GC.Collect();
            return 43;
        }

        public static object ValueTaskArgument(ValueTask<int> value)
        {
            GC.Collect();
            return value.Result;
        }

        public static ValueTask<int> ValueTaskResult()
        {
            GC.Collect();
            return new ValueTask<int>(45);
        }

        public static object CancellationTokenArgument(CancellationToken value)
        {
            GC.Collect();
            return value.IsCancellationRequested;
        }

        public static CancellationToken CancellationTokenResult()
        {
            GC.Collect();
            return new CancellationToken(canceled: true);
        }

        public static bool ByRefValue(ref int value)
        {
            GC.Collect();
            value++;
            return true;
        }

        public static object TwoPrimitiveArguments(int first, int second)
        {
            GC.Collect();
            return first + second;
        }

        public static object FiveReferenceArguments(
            object first,
            object second,
            object third,
            object fourth,
            object fifth)
        {
            GC.Collect();
            GC.KeepAlive(first);
            GC.KeepAlive(second);
            GC.KeepAlive(third);
            GC.KeepAlive(fourth);
            return fifth;
        }
    }

    internal interface IIntrinsicInvokeStructReceiver
    {
        object GetValue();
    }

    internal readonly struct IntrinsicInvokeStructReceiver : IIntrinsicInvokeStructReceiver
    {
        internal IntrinsicInvokeStructReceiver(int value) => Value = value;

        internal int Value { get; }

        public object GetValue()
        {
            GC.Collect();
            return Value;
        }

        public override string ToString()
        {
            GC.Collect();
            return Value.ToString();
        }
    }

    internal sealed class IntrinsicInvokeEnumResultTarget
    {
        public IntrinsicInvokeInt32Enum InstanceResult()
        {
            GC.Collect();
            return IntrinsicInvokeInt32Enum.Value;
        }

        public static IntrinsicInvokeInt64Enum StaticResult()
        {
            GC.Collect();
            return IntrinsicInvokeInt64Enum.Value;
        }
    }

    internal sealed class IntrinsicInvokeArgumentValidationTarget
    {
        public void Reference(IIntrinsicInvokeReference value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }

        public void Primitive(int value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }

        public void Enum(IntrinsicInvokeInt32Enum value)
        {
            GC.Collect();
            GC.KeepAlive(value);
        }
    }

    internal enum IntrinsicInvokeByteEnum : byte
    {
        Value = 211
    }

    internal enum IntrinsicInvokeSByteEnum : sbyte
    {
        Value = -91
    }

    internal enum IntrinsicInvokeInt16Enum : short
    {
        Value = -30001
    }

    internal enum IntrinsicInvokeUInt16Enum : ushort
    {
        Value = 60001
    }

    internal enum IntrinsicInvokeInt32Enum : int
    {
        Value = -123456789
    }

    internal enum IntrinsicInvokeUInt32Enum : uint
    {
        Value = 0xFEDCBA98
    }

    internal enum IntrinsicInvokeInt64Enum : long
    {
        Value = -1234567890123456789
    }

    internal enum IntrinsicInvokeUInt64Enum : ulong
    {
        Value = 0xFEDCBA9876543210
    }

#pragma warning disable 0414
    public interface MI_Interface
    {
        int IMethod();
        int IMethodNew();
    }

    public class MI_BaseClass : MI_Interface
    {
        public int IMethod() => 10;
        public int IMethodNew() => 20;

        public static bool StaticIntMethodReturningBool(int int4a) => int4a % 2 == 0;
        public virtual int VirtualReturnIntMethod() => 0;

        public virtual int VirtualMethod() => 0;
        private int PrivateInstanceMethod() => 21;
        public static string PublicStaticMethod(string x) => x;
        public string PublicStructMethod(DateTime dt) => dt.ToString();
    }

    public class MI_SubClass : MI_BaseClass
    {
        public override int VirtualReturnIntMethod() => 2;

        public PublicEnum EnumMethodReturningEnum(PublicEnum myenum) => myenum == PublicEnum.Case1 ? PublicEnum.Case2 : PublicEnum.Case1;
        public string ObjectMethodReturningString(object obj) => obj.ToString();
        public int VoidMethodReturningInt() => 3;
        public long VoidMethodReturningLong() => long.MaxValue;
        public long IntLongMethodReturningLong(int i, long l) => i + l;
        public static int StaticIntIntMethodReturningInt(int i1, int i2) => i1 + i2;

        public static void StaticGenericMethod<T>(T t) { }

        public new int IMethodNew() => 200;

        public override int VirtualMethod() => 1;

        public void ReturnVoidMethod(DateTime dt) { }
        public virtual string[] VirtualReturnStringArrayMethod() => new string[0];
        public virtual bool VirtualReturnBoolMethod() => true;

        public string Method2<T, S>(string t2, T t1, S t3) => "";

        public IntPtr ReturnIntPtrMethod() => new IntPtr(200);
        public int[] ReturnArrayMethod() => new int[] { 2, 3, 5, 7, 11 };

        public void GenericMethod1<T>(T t) { }
        public void GenericMethod2<T, U>(T t, U u) { }

        public void StringArrayMethod(string[] strArray) { }

        [Attr(77, name = "AttrSimple"),
        Int32Attr(77, name = "Int32AttrSimple"),
        Int64Attr(77, name = "Int64AttrSimple"),
        StringAttr("hello", name = "StringAttrSimple"),
        EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
        TypeAttr(typeof(object), name = "TypeAttrSimple")]
        public void MethodWithAttributes() { }
    }

    public class MI_SubClassA : MI_BaseClass { }
    public class MI_SubClassB : MI_BaseClass { }
    public class MI_SubClassC : MI_BaseClass { }

    public class MethodInfoDummySubClass : MI_BaseClass
    {
        public override int VirtualReturnIntMethod() => 1;
    }

    public class MI_Interlocked
    {
        public static int Increment(ref int location) => 0;
        public static int Decrement(ref int location) => 0;
        public static int Exchange(ref int location1, int value) => 0;
        public static int CompareExchange(ref int location1, int value, int comparand) => 0;

        public static float Exchange(ref float location1, float value) => 0;
        public static float CompareExchange(ref float location1, float value, float comparand) => 0;

        public static object Exchange(ref object location1, object value) => null;
        public static object CompareExchange(ref object location1, object value, object comparand) => null;
    }

    public class MI_GenericClass<T>
    {
        public T GenericMethod1(T t) => t;
        public T GenericMethod2<S>(S s1, T t, string s2) => t;
        public static S GenericMethod3<S>(S s) => s;
        public static T GenericMethod4(T s) => s;
        public static void NonGenericMethod() { }
    }

    public interface MethodInfoBaseDefinitionInterface
    {
        void InterfaceMethod1();
        void InterfaceMethod2();
    }

    public class MethodInfoBaseDefinitionBaseClass : MethodInfoBaseDefinitionInterface
    {
        public void InterfaceMethod1() { }
        void MethodInfoBaseDefinitionInterface.InterfaceMethod2() { }

        public virtual void BaseClassVirtualMethod() { }
        public virtual void BaseClassMethod() { }

        public override string ToString() => base.ToString();
    }

    public class MethodInfoBaseDefinitionSubClass : MethodInfoBaseDefinitionBaseClass
    {
        public override void BaseClassVirtualMethod() => base.BaseClassVirtualMethod();
        public new void BaseClassMethod() { }
        public override string ToString() => base.ToString();

        public void DerivedClassMethod() { }
    }

    public abstract class MI_AbstractBaseClass
    {
        public abstract void AbstractMethod();
        public virtual void VirtualMethod() { }
    }

    public class MI_AbstractSubClass : MI_AbstractBaseClass
    {
        public sealed override void VirtualMethod() { }
        public override void AbstractMethod() { }
    }

    public interface MethodInfoDefaultParametersInterface
    {
        string InterfaceMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m);
    }

    public class MethodInfoDefaultParameters : MethodInfoDefaultParametersInterface
    {
        public int Integer(int parameter = 1)
        {
            return parameter;
        }

        public string AllPrimitives(
            bool boolean = true,
            string str = "test",
            char character = 'c',
            byte unsignedbyte = 2,
            sbyte signedbyte = -1,
            short int16 = -3,
            ushort uint16 = 4,
            int int32 = -5,
            uint uint32 = 6,
            long int64 = -7,
            ulong uint64 = 8,
            float single = 9.1f,
            double dbl = 11.12)
        {
            return FormattableString.Invariant($"{boolean}, {str}, {character}, {unsignedbyte}, {signedbyte}, {int16}, {uint16}, {int32}, {uint32}, {int64}, {uint64}, {single}, {dbl}");
        }

        public string String(string parameter = "test") => parameter;

        public class CustomReferenceType
        {
            public override bool Equals(object obj) => ReferenceEquals(this, obj);
            public override int GetHashCode() => 0;
        }

        public CustomReferenceType Reference(CustomReferenceType parameter = null) => parameter;

        public struct CustomValueType
        {
            public int Id;
            public override bool Equals(object obj) => Id == ((CustomValueType)obj).Id;
            public override int GetHashCode() => Id.GetHashCode();
        }

        public CustomValueType ValueType(CustomValueType parameter = default(CustomValueType)) => parameter;

        public DateTime DateTime([DateTimeConstant(42)] DateTime parameter) => parameter;

        public decimal DecimalWithAttribute([DecimalConstant(1, 1, 2, 3, 4)] decimal parameter) => parameter;

        public decimal Decimal(decimal parameter = 3.14m) => parameter;

        public int? NullableInt(int? parameter = null) => parameter;

        public PublicEnum Enum(PublicEnum parameter = PublicEnum.Case1) => parameter;

        string MethodInfoDefaultParametersInterface.InterfaceMethod(int p1, string p2, decimal p3)
        {
            return FormattableString.Invariant($"{p1}, {p2}, {p3}");
        }

        public static string StaticMethod(int p1 = 1, string p2 = "test", decimal p3 = 3.14m)
        {
            return FormattableString.Invariant($"{p1}, {p2}, {p3}");
        }

        public object OptionalObjectParameter([Optional] object parameter) => parameter;
        public string OptionalStringParameter([Optional] string parameter) => parameter;
    }

    public delegate int Delegate_TC_Int(MI_BaseClass tc);
    public delegate int Delegate_Void_Int();
    public delegate string Delegate_Str_Str(string x);
    public delegate string Delegate_Void_Str();
    public delegate string Delegate_DateTime_Str(MI_BaseClass tc, DateTime dt);

    public delegate T Delegate_GC_T_T<T>(MI_GenericClass<T> gc, T x);
    public delegate T Delegate_T_T<T>(T x);
    public delegate T Delegate_Void_T<T>();

    public class DummyClass { }

    public class Sample
    {
        public string Method1(DateTime t)
        {
            return "";
        }
        public string Method2<T, S>(string t2, T t1, S t3)
        {
            return "";
        }

        public static string DefaultString(string value = "Hello") => value;

        public static string DefaultNullString(string value = null) => value;

        public static YesNo? DefaultNullableEnum(YesNo? value = YesNo.Yes) => value;

        public static int? DefaultNullableInt(int? value = 3) => value;

        public static Missing DefaultMissing(Missing value = null) => value;
    }

    public class SampleG<T>
    {
        public T Method1(T t)
        {
            return t;
        }
        public T Method2<S>(S t1, T t2, string t3)
        {
            return t2;
        }
    }

    public static class NullableRefMethods
    {
        public static bool Null(ref int? i)
        {
            Assert.Null(i);
            return true;
        }

        public static bool NullBoxed(ref object? i)
        {
            Assert.Null(i);
            return true;
        }

        public static bool NullToValue(ref int? i, int value)
        {
            Assert.Null(i);
            i = value;
            return true;
        }

        public static bool NullToValueBoxed(ref object? i, int value)
        {
            Assert.Null(i);
            i = value;
            return true;
        }

        public static bool ValueToNull(ref int? i, int expected)
        {
            Assert.Equal(expected, i);
            i = null;
            return true;
        }

        public static bool ValueToNullBoxed(ref int? i, int expected)
        {
            Assert.Equal(expected, i);
            i = null;
            return true;
        }
    }

    public static class CopyBackMethods
    {
        public static void IncrementByRef(ref int i)
        {
            i++;
        }

        public static void IncrementByNullableRef(ref int? i)
        {
            i++;
        }

        public static void SetToNullByRef(ref object o)
        {
            o = null;
        }

        public static void SetToNonNullByRef(ref object o)
        {
            o = new object();
        }
    }

    public enum ColorsInt : int
    {
        Red = 1
    }

    public enum ColorsShort : short
    {
        Red = 1
    }

    public enum OtherColorsInt : int
    {
        Red = 1
    }

    public struct ValueTypeWithOverrides
    {
        public int Id;
        public override string ToString() => "Hello";
        public int GetId() => Id;
    }

    public struct ValueTypeWithoutOverrides
    {
        public int Id;
        public int GetId() => Id;
    }

    public enum YesNo
    {
        No = 0,
        Yes = 1,
    }

    public static class EnumMethods
    {
        public static bool PassColorsInt(ColorsInt color)
        {
            Assert.Equal(ColorsInt.Red, color);
            return true;
        }

        public static bool PassColorsShort(ColorsShort color)
        {
            Assert.Equal(ColorsShort.Red, color);
            return true;
        }

        static YesNo NonNullableEnumDefaultYes(YesNo yesNo = YesNo.Yes)
        {
            return yesNo;
        }

        static YesNo? NullableEnumDefaultNo(YesNo? yesNo = YesNo.No)
        {
            return yesNo;
        }

        static YesNo? NullableEnumDefaultYes(YesNo? yesNo = YesNo.Yes)
        {
            return yesNo;
        }

        static YesNo? NullableEnumDefaultNull(YesNo? yesNo = null)
        {
            return yesNo;
        }

        static YesNo? NullableEnumNoDefault(YesNo? yesNo)
        {
            return yesNo;
        }
    }

    public static class FunctionPointerMethods
    {
        public static bool CallMe(int i)
        {
            return i == 42;
        }

        public static unsafe bool CallFcnPtr_FP(delegate*<int, bool> fn, int value)
        {
            return fn(value);
        }

        public static unsafe bool CallFcnPtr_IntPtr(IntPtr fn, int value)
        {
            return ((delegate*<int, bool>)fn)(value);
        }

        public static unsafe bool CallFcnPtr_UIntPtr(UIntPtr fn, int value)
        {
            return ((delegate*<int, bool>)fn)(value);
        }

        public static unsafe bool CallFcnPtr_Void(void* fn, int value)
        {
            return ((delegate*<int, bool>)fn)(value);
        }

        public static unsafe delegate*<int, bool> GetFunctionPointer() => &CallMe;
    }
#pragma warning restore 0414
}
