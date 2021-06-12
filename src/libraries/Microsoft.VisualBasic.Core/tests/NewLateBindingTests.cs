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

        [Theory]
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
    }
}
