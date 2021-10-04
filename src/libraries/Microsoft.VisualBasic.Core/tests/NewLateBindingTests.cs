// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class NewLateBindingTests
    {
        private sealed class OptionalValuesType : DynamicObject
        {
            public object F1<T>(T p1 = default)
            {
                return $"{typeof(T)}, {ToString(p1)}";
            }
            public object F2<T>(T p1 = default, int? p2 = 2)
            {
                return $"{typeof(T)}, {ToString(p1)}, {ToString(p2)}";
            }
            public object F3<T>(object p1, T p2 = default, int? p3 = 3)
            {
                return $"{typeof(T)}, {ToString(p2)}, {ToString(p3)}";
            }
            public object F4<T>(object p1, object p2, T p3 = default, int? p4 = 4)
            {
                return $"{typeof(T)}, {ToString(p3)}, {ToString(p4)}";
            }
            public object F5<T>(object p1, object p2, object p3, T p4 = default, int? p5 = 5)
            {
                return $"{typeof(T)}, {ToString(p4)}, {ToString(p5)}";
            }
            public object F6<T>(object p1, object p2, object p3, object p4, T p5 = default, int? p6 = 6)
            {
                return $"{typeof(T)}, {ToString(p5)}, {ToString(p6)}";
            }
            public object F7<T>(object p1, object p2, object p3, object p4, object p5, T p6 = default, int? p7 = 7)
            {
                return $"{typeof(T)}, {ToString(p6)}, {ToString(p7)}";
            }
            public object F8<T>(object p1, object p2, object p3, object p4, object p5, object p6, T p7 = default, int? p8 = 8)
            {
                return $"{typeof(T)}, {ToString(p7)}, {ToString(p8)}";
            }
            private static string ToString(object obj) => obj?.ToString() ?? "null";
        }

        public static IEnumerable<object[]> LateCall_OptionalValues_Data()
        {
            // If System.Type.Missing is used for a parameter with type parameter type,
            // System.Reflection.Missing is used in type inference. This matches .NET Framework behavior.

            yield return CreateData("F1", new object[] { -1 }, null, "System.Int32, -1");
            yield return CreateData("F1", new object[] { Type.Missing }, null, "System.Reflection.Missing, null");
            yield return CreateData("F1", new object[] { Type.Missing }, new[] { typeof(int) }, "System.Int32, 0");

            yield return CreateData("F2", new object[] { 1, -1 }, null, "System.Int32, 1, -1");
            yield return CreateData("F2", new object[] { 1, Type.Missing }, null, "System.Int32, 1, 2");
            yield return CreateData("F2", new object[] { Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 2");
            yield return CreateData("F2", new object[] { Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 2");

            yield return CreateData("F3", new object[] { 1, 2, -1 }, null, "System.Int32, 2, -1");
            yield return CreateData("F3", new object[] { 1, 2, Type.Missing }, null, "System.Int32, 2, 3");
            yield return CreateData("F3", new object[] { 1, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 3");
            yield return CreateData("F3", new object[] { 1, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 3");

            yield return CreateData("F4", new object[] { 1, 2, 3, -1 }, null, "System.Int32, 3, -1");
            yield return CreateData("F4", new object[] { 1, 2, 3, Type.Missing }, null, "System.Int32, 3, 4");
            yield return CreateData("F4", new object[] { 1, 2, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 4");
            yield return CreateData("F4", new object[] { 1, 2, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 4");

            yield return CreateData("F5", new object[] { 1, 2, 3, 4, -1 }, null, "System.Int32, 4, -1");
            yield return CreateData("F5", new object[] { 1, 2, 3, 4, Type.Missing }, null, "System.Int32, 4, 5");
            yield return CreateData("F5", new object[] { 1, 2, 3, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 5");
            yield return CreateData("F5", new object[] { 1, 2, 3, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 5");

            yield return CreateData("F6", new object[] { 1, 2, 3, 4, 5, -1 }, null, "System.Int32, 5, -1");
            yield return CreateData("F6", new object[] { 1, 2, 3, 4, 5, Type.Missing }, null, "System.Int32, 5, 6");
            yield return CreateData("F6", new object[] { 1, 2, 3, 4, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 6");
            yield return CreateData("F6", new object[] { 1, 2, 3, 4, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 6");

            yield return CreateData("F7", new object[] { 1, 2, 3, 4, 5, 6, -1 }, null, "System.Int32, 6, -1");
            yield return CreateData("F7", new object[] { 1, 2, 3, 4, 5, 6, Type.Missing }, null, "System.Int32, 6, 7");
            yield return CreateData("F7", new object[] { 1, 2, 3, 4, 5, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 7");
            yield return CreateData("F7", new object[] { 1, 2, 3, 4, 5, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 7");

            yield return CreateData("F8", new object[] { 1, 2, 3, 4, 5, 6, 7, -1 }, null, "System.Int32, 7, -1");
            yield return CreateData("F8", new object[] { 1, 2, 3, 4, 5, 6, 7, Type.Missing }, null, "System.Int32, 7, 8");
            yield return CreateData("F8", new object[] { 1, 2, 3, 4, 5, 6, Type.Missing, Type.Missing }, null, "System.Reflection.Missing, null, 8");
            yield return CreateData("F8", new object[] { 1, 2, 3, 4, 5, 6, Type.Missing, Type.Missing }, new[] { typeof(int) }, "System.Int32, 0, 8");

            static object[] CreateData(string memberName, object[] arguments, Type[] typeArguments, string expectedValue) => new object[] { memberName, arguments, typeArguments, expectedValue };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotLinqExpressionsBuiltWithIsInterpretingOnly))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/51834", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [MemberData(nameof(LateCall_OptionalValues_Data))]
        public void LateCall_OptionalValues(string memberName, object[] arguments, Type[] typeArguments, string expectedValue)
        {
            // NewLateBinding.LateCall() corresponds to a call to the member when using late binding:
            //   Dim instance = New OptionalValuesType()
            //   instance.Member(arguments)
            var actualValue = NewLateBinding.LateCall(
                Instance: new OptionalValuesType(),
                Type: null,
                MemberName: memberName,
                Arguments: arguments,
                ArgumentNames: null,
                TypeArguments: typeArguments,
                CopyBack: null,
                IgnoreReturn: true);
            Assert.Equal(expectedValue, actualValue);
        }

        private sealed class Properties_GetOnly
        {
            private object[] _values;
            public Properties_GetOnly(string p, int length)
            {
                P = p;
                _values = new object[length];
            }
            public string P { get; }
            public object this[int i]
            {
                get { return _values[i]; }
            }
        }

        [Fact]
        public void Properties_GetOnly_01()
        {
            // NewLateBinding.LateSet() corresponds to a setting a member with late binding:
            //   Dim instance = New Properties_GetOnly("A", 0)
            //   instance.P = "B"
            var instance = new Properties_GetOnly("A", 0);
            Assert.Throws<MissingMemberException>(() =>
                NewLateBinding.LateSet(
                    Instance: instance,
                    Type: null,
                    MemberName: "P",
                    Arguments: new object[] { "B" },
                    ArgumentNames: null,
                    TypeArguments: null));
            Assert.Equal("A", instance.P);
        }

        [Fact]
        public void Properties_GetOnly_02()
        {
            // NewLateBinding.LateSet() corresponds to a setting a member with late binding:
            //   Dim instance = New Properties_GetOnly(Nothing, 10)
            //   instance(3) = "3"
            var instance = new Properties_GetOnly(null, 10);
            Assert.Throws<MissingMemberException>(() =>
                NewLateBinding.LateSet(
                    Instance: instance,
                    Type: null,
                    MemberName: "Item",
                    Arguments: new object[] { 3, "3" },
                    ArgumentNames: null,
                    TypeArguments: null));
            Assert.Null(instance[3]);
        }

        [Fact]
        public void Properties_GetOnly_03()
        {
            // NewLateBinding.LateCall() corresponds to calling the set accessor with late binding:
            //   Dim instance = New Properties_GetOnly("A", 0)
            //   instance.Set_P("B")
            var instance = new Properties_GetOnly("A", 0);
            Assert.Throws<MissingMemberException>(() =>
                NewLateBinding.LateCall(
                    Instance: instance,
                    Type: null,
                    MemberName: "Set_P",
                    Arguments: new object[] { "B" },
                    ArgumentNames: null,
                    TypeArguments: null,
                    CopyBack: null,
                    IgnoreReturn: true));
            Assert.Equal("A", instance.P);
        }

        private sealed class Properties_GetAndSet
        {
            private object[] _values;
            public Properties_GetAndSet(string p, int length)
            {
                P = p;
                _values = new object[length];
            }
            public string P { get; set; }
            public object this[int i]
            {
                get { return _values[i]; }
                set { _values[i] = value; }
            }
        }

        [Fact]
        public void Properties_GetAndSet_01()
        {
            var instance = new Properties_GetAndSet("A", 0);
            NewLateBinding.LateSet(
                Instance: instance,
                Type: null,
                MemberName: "P",
                Arguments: new object[] { "B" },
                ArgumentNames: null,
                TypeArguments: null);
            Assert.Equal("B", instance.P);
        }

        [Fact]
        public void Properties_GetAndSet_02()
        {
            var instance = new Properties_GetAndSet(null, 10);
            NewLateBinding.LateSet(
                Instance: instance,
                Type: null,
                MemberName: "Item",
                Arguments: new object[] { 3, "3" },
                ArgumentNames: null,
                TypeArguments: null);
            Assert.Equal("3", instance[3]);
        }

        [Fact]
        public void Properties_GetAndSet_03()
        {
            var instance = new Properties_GetAndSet("A", 0);
            NewLateBinding.LateCall(
                Instance: instance,
                Type: null,
                MemberName: "Set_P",
                Arguments: new object[] { "B" },
                ArgumentNames: null,
                TypeArguments: null,
                CopyBack: null,
                IgnoreReturn: true);
            Assert.Equal("B", instance.P);
        }

        private sealed class Properties_GetAndInit
        {
            private object[] _values;
            public Properties_GetAndInit(string p, int length)
            {
                P = p;
                _values = new object[length];
            }
            public string P { get; init; }
            public object this[int i]
            {
                get { return _values[i]; }
                init { _values[i] = value; }
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Properties_GetAndInit_01()
        {
            var instance = new Properties_GetAndInit("A", 0);
            Assert.Throws<MissingMemberException>(() =>
                NewLateBinding.LateSet(
                    Instance: instance,
                    Type: null,
                    MemberName: "P",
                    Arguments: new object[] { "B" },
                    ArgumentNames: null,
                    TypeArguments: null));
            Assert.Equal("A", instance.P);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Properties_GetAndInit_02()
        {
            var instance = new Properties_GetAndInit(null, 10);
            Assert.Throws<MissingMemberException>(() =>
                NewLateBinding.LateSet(
                    Instance: instance,
                    Type: null,
                    MemberName: "Item",
                    Arguments: new object[] { 3, "3" },
                    ArgumentNames: null,
                    TypeArguments: null));
            Assert.Null(instance[3]);
        }

        // Not preventing direct call to property init accessor.
        [Fact]
        public void Properties_GetAndInit_03()
        {
            var instance = new Properties_GetAndInit("A", 0);
            NewLateBinding.LateCall(
                Instance: instance,
                Type: null,
                MemberName: "Set_P",
                Arguments: new object[] { "B" },
                ArgumentNames: null,
                TypeArguments: null,
                CopyBack: null,
                IgnoreReturn: true);
            Assert.Equal("B", instance.P);
        }
    }
}
