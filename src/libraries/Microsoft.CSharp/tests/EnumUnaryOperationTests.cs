// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class EnumUnaryOperationTests
    {
        public enum ByteEnum : byte
        {
            A,
            B,
            C,
            D,
            E
        }

        public enum SByteEnum : sbyte
        {
            A,
            B,
            C,
            D,
            E,
            Z = -1
        }

        public enum Int32Enum
        {
            A,
            B,
            C,
            D,
            E,
            Z = -1
        }

        public enum UInt32Enum : uint
        {
            A,
            B,
            C,
            D,
            E
        }

        public enum Int64Enum : long
        {
            A,
            B,
            C,
            D,
            E,
            Z = -1
        }

        public static IEnumerable<object[]> ByteEnums() => Enum.GetValues(typeof(ByteEnum))
            .Cast<ByteEnum>()
            .Select(x => new object[] { x, ~x });

        public static IEnumerable<object[]> SByteEnums() => Enum.GetValues(typeof(SByteEnum))
            .Cast<ByteEnum>()
            .Select(x => new object[] { x, ~x });

        public static IEnumerable<object[]> Int32Enums() => Enum.GetValues(typeof(Int32Enum))
            .Cast<Int32Enum>()
            .Select(x => new object[] { x, ~x });

        public static IEnumerable<object[]> UInt32Enums() => Enum.GetValues(typeof(UInt32Enum))
            .Cast<UInt32Enum>()
            .Select(x => new object[] { x, ~x });

        public static IEnumerable<object[]> Int64Enums() => Enum.GetValues(typeof(Int64Enum))
            .Cast<Int64Enum>()
            .Select(x => new object[] { x, ~x });

        [Theory]
        [MemberData(nameof(ByteEnums))]
        [MemberData(nameof(SByteEnums))]
        [MemberData(nameof(Int32Enums))]
        [MemberData(nameof(UInt32Enums))]
        [MemberData(nameof(Int64Enums))]
        public void EnumOnesComplement(dynamic operand, dynamic result)
        {
            unchecked
            {
                Assert.Equal(result, ~operand);
            }
        }

        [Theory]
        [MemberData(nameof(ByteEnums))]
        [MemberData(nameof(SByteEnums))]
        [MemberData(nameof(Int32Enums))]
        [MemberData(nameof(UInt32Enums))]
        [MemberData(nameof(Int64Enums))]
        public void CheckedEnumOnesComplement(dynamic operand, dynamic result)
        {
            checked
            {
                Assert.Equal(result, ~operand);
            }
        }

        [Theory]
        [MemberData(nameof(ByteEnums))]
        [MemberData(nameof(SByteEnums))]
        [MemberData(nameof(Int32Enums))]
        [MemberData(nameof(UInt32Enums))]
        [MemberData(nameof(Int64Enums))]
        public void ConstantEnumOnesComplement(object operand, object result)
        {
            CallSite<Func<CallSite, object, object>> cs = CallSite<Func<CallSite, object, object>>.Create(
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.OnesComplement, GetType(),
                    new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null) }));
            Assert.Equal(result, cs.Target(cs, operand));
        }

        [Theory]
        [MemberData(nameof(ByteEnums))]
        [MemberData(nameof(SByteEnums))]
        [MemberData(nameof(Int32Enums))]
        [MemberData(nameof(UInt32Enums))]
        [MemberData(nameof(Int64Enums))]
        public void ConstantCheckedEnumOnesComplement(object operand, object result)
        {
            CallSite<Func<CallSite, object, object>> cs = CallSite<Func<CallSite, object, object>>.Create(
                Binder.UnaryOperation(
                    CSharpBinderFlags.CheckedContext, ExpressionType.OnesComplement, GetType(),
                    new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null) }));
            Assert.Equal(result, cs.Target(cs, operand));
        }
    }
}
