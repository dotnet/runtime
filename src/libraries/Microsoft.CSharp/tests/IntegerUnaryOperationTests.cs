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
    public class IntegerUnaryOperationTests
    {
        private static CallSite<Func<CallSite, object, object>> GetUnaryOperationCallSite(ExpressionType operation, bool checkedContext, bool constantArgument)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(constantArgument ? CSharpArgumentInfoFlags.Constant : CSharpArgumentInfoFlags.None, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    checkedContext ? CSharpBinderFlags.CheckedContext : CSharpBinderFlags.None, operation,
                    typeof(IntegerBinaryOperationTests), new[] { x });
            return CallSite<Func<CallSite, object, object>>.Create(binder);
        }

        private static readonly int[] TestInt32Values = { 0, 1, -1, 3, int.MinValue, int.MaxValue };

        public static IEnumerable<object[]> Int32TestNegations
            => from x in TestInt32Values select new object[] {x, ExpressionType.Negate, unchecked(-x), x != int.MinValue};

        public static IEnumerable<object[]> Int32TestUnaryPluses
            => from x in TestInt32Values select new object[] {x, ExpressionType.UnaryPlus, x, true};

        public static IEnumerable<object[]> Int32TestOnesComplements
            => from x in TestInt32Values select new object[] {x, ExpressionType.OnesComplement, ~x, true};

        private static readonly uint[] TestUInt32Values = { 0, 1, 3, int.MaxValue, uint.MaxValue };

        public static IEnumerable<object[]> UInt32TestNegations
            => from x in TestUInt32Values select new object[] {x, ExpressionType.Negate, -x, true};

        public static IEnumerable<object[]> UInt32TestUnaryPluses
            => from x in TestUInt32Values select new object[] {x, ExpressionType.UnaryPlus, x, true};

        public static IEnumerable<object[]> UInt32TestOnesComplements
            => from x in TestUInt32Values select new object[] {x, ExpressionType.OnesComplement, ~x, true};

        private static readonly long[] TestInt64Values = { 0, 1, -1, 3, long.MinValue, long.MaxValue };

        public static IEnumerable<object[]> Int64TestNegations
            => from x in TestInt64Values select new object[] {x, ExpressionType.Negate, unchecked(-x), x != long.MinValue};

        public static IEnumerable<object[]> Int64TestUnaryPluses
            => from x in TestInt64Values select new object[] {x, ExpressionType.UnaryPlus, x, true};

        public static IEnumerable<object[]> Int64TestOnesComplements
            => from x in TestInt64Values
               select new object[] { x, ExpressionType.OnesComplement, ~x, true };

        private static readonly ulong[] TestUInt64Values = { 0, 1, 3, long.MaxValue, ulong.MaxValue };

        public static IEnumerable<object[]> UInt64TestUnaryPluses
            => from x in TestUInt64Values select new object[] {x, ExpressionType.UnaryPlus, x, true};

        public static IEnumerable<object[]> UInt64TestOnesComplements
            => from x in TestUInt64Values select new object[] {x, ExpressionType.OnesComplement, ~x, true};

        [Theory]
        [MemberData(nameof(Int32TestNegations))]
        [MemberData(nameof(Int32TestUnaryPluses))]
        [MemberData(nameof(Int32TestOnesComplements))]
        [MemberData(nameof(UInt32TestNegations))]
        [MemberData(nameof(UInt32TestUnaryPluses))]
        [MemberData(nameof(UInt32TestOnesComplements))]
        [MemberData(nameof(Int64TestNegations))]
        [MemberData(nameof(Int64TestUnaryPluses))]
        [MemberData(nameof(Int64TestOnesComplements))]
        [MemberData(nameof(UInt64TestUnaryPluses))]
        [MemberData(nameof(UInt64TestOnesComplements))]
        public void RuntimeExpressions(object x, ExpressionType type, object result, bool shouldSucceedChecked)
        {
            var callsite = GetUnaryOperationCallSite(type, false, false);
            Assert.Equal(result, callsite.Target(callsite, x));
            callsite = GetUnaryOperationCallSite(type, true, false);
            if (shouldSucceedChecked)
            {
                Assert.Equal(result, callsite.Target(callsite, x));
            }
            else
            {
                Assert.Throws<OverflowException>(() => callsite.Target(callsite, x));
            }
        }

        [Theory]
        [MemberData(nameof(Int32TestNegations))]
        [MemberData(nameof(Int32TestUnaryPluses))]
        [MemberData(nameof(Int32TestOnesComplements))]
        [MemberData(nameof(UInt32TestNegations))]
        [MemberData(nameof(UInt32TestUnaryPluses))]
        [MemberData(nameof(UInt32TestOnesComplements))]
        [MemberData(nameof(Int64TestNegations))]
        [MemberData(nameof(Int64TestUnaryPluses))]
        [MemberData(nameof(Int64TestOnesComplements))]
        [MemberData(nameof(UInt64TestUnaryPluses))]
        [MemberData(nameof(UInt64TestOnesComplements))]
        public void ConstantExpressions(object x, ExpressionType type, object result, bool shouldSucceedChecked)
        {
            var callsite = GetUnaryOperationCallSite(type, false, true);
            Assert.Equal(result, callsite.Target(callsite, x));
            callsite = GetUnaryOperationCallSite(type, true, true);
            if (shouldSucceedChecked)
            {
                Assert.Equal(result, callsite.Target(callsite, x));
            }
            else
            {
                Assert.Throws<OverflowException>(() => callsite.Target(callsite, x));
            }
        }
    }
}
