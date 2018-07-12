// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class ImplicitConversionTests
    {
        private static void AssertImplicitConvert<TSource, TTarget>(TSource argument, TTarget expected)
        {
            CallSiteBinder binder = Binder.Convert(CSharpBinderFlags.None, typeof(TTarget), typeof(ImplicitConversionTests));
            CallSite<Func<CallSite, TSource, TTarget>> callSite =
                CallSite<Func<CallSite, TSource, TTarget>>.Create(binder);
            Func<CallSite, TSource, TTarget> func = callSite.Target;
            TTarget result = func(callSite, argument);
            Assert.Equal(expected, result);

            if (typeof(TSource) != typeof(object))
            {
                AssertImplicitConvert<object, TTarget>(argument, expected);
            }
        }

        private static void AssertBadImplicitConvert<TSource, TTarget>(TSource argument)
        {
            CallSiteBinder binder = Binder.Convert(CSharpBinderFlags.None, typeof(TTarget), typeof(ImplicitConversionTests));
            CallSite<Func<CallSite, TSource, TTarget>> callSite =
                CallSite<Func<CallSite, TSource, TTarget>>.Create(binder);
            Func<CallSite, TSource, TTarget> func = callSite.Target;
            Assert.Throws<RuntimeBinderException>(() => func(callSite, argument));

            if (typeof(TSource) != typeof(object))
            {
                AssertBadImplicitConvert<object, TTarget>(argument);
            }
        }

        private static void TestTypeAsArgument<TTarget>(TTarget argument)
        {
            // Do nothing. Just test whether something can be accepted as an argument.
        }

        private static void TestBadTypeAsArgument<TTarget>(dynamic argument)
        {
            Assert.Throws<RuntimeBinderException>(() => TestTypeAsArgument<TTarget>(argument));
        }

        [Fact]
        public void NullablesToValueTypeUp()
        {
            AssertImplicitConvert<int?, ValueType>(2, 2);
            AssertImplicitConvert<int?, ValueType>(null, null);
            AssertImplicitConvert<int?, object>(2, 2);
        }


        [Fact]
        public void NullablesToBaseTypesInterfaces()
        {
            AssertImplicitConvert<int?, IEquatable<int>>(2, 2);
            AssertImplicitConvert<int?, IEquatable<int>>(null, null);
            AssertImplicitConvert<int?, IComparable<int>>(2, 2);
            AssertImplicitConvert<int?, IComparable>(2, 2);
            AssertImplicitConvert<int?, IConvertible>(2, 2);
            AssertImplicitConvert<int?, IFormattable>(2, 2);
        }

        [Fact]
        public void NumericConversions()
        {
            AssertImplicitConvert<sbyte, short>(1, 1);
            AssertImplicitConvert<sbyte, int>(1, 1);
            AssertImplicitConvert<sbyte, long>(1, 1);
            AssertImplicitConvert<sbyte, float>(1, 1);
            AssertImplicitConvert<sbyte, double>(1, 1);
            AssertImplicitConvert<sbyte, decimal>(1, 1);

            AssertImplicitConvert<byte, short>(1, 1);
            AssertImplicitConvert<byte, ushort>(1, 1);
            AssertImplicitConvert<byte, int>(1, 1);
            AssertImplicitConvert<byte, uint>(1, 1);
            AssertImplicitConvert<byte, long>(1, 1);
            AssertImplicitConvert<byte, ulong>(1, 1);
            AssertImplicitConvert<byte, float>(1, 1);
            AssertImplicitConvert<byte, double>(1, 1);
            AssertImplicitConvert<byte, decimal>(1, 1);

            AssertImplicitConvert<short, int>(1, 1);
            AssertImplicitConvert<short, long>(1, 1);
            AssertImplicitConvert<short, float>(1, 1);
            AssertImplicitConvert<short, double>(1, 1);
            AssertImplicitConvert<short, decimal>(1, 1);

            AssertImplicitConvert<ushort, int>(1, 1);
            AssertImplicitConvert<ushort, uint>(1, 1);
            AssertImplicitConvert<ushort, long>(1, 1);
            AssertImplicitConvert<ushort, ulong>(1, 1);
            AssertImplicitConvert<ushort, float>(1, 1);
            AssertImplicitConvert<ushort, double>(1, 1);
            AssertImplicitConvert<ushort, decimal>(1, 1);

            AssertImplicitConvert<int, long>(1, 1);
            AssertImplicitConvert<int, float>(1, 1);
            AssertImplicitConvert<int, double>(1, 1);
            AssertImplicitConvert<int, decimal>(1, 1);

            AssertImplicitConvert<uint, long>(1, 1);
            AssertImplicitConvert<uint, ulong>(1, 1);
            AssertImplicitConvert<uint, float>(1, 1);
            AssertImplicitConvert<uint, double>(1, 1);
            AssertImplicitConvert<uint, decimal>(1, 1);

            AssertImplicitConvert<long, float>(1, 1);
            AssertImplicitConvert<long, double>(1, 1);
            AssertImplicitConvert<long, decimal>(1, 1);

            AssertImplicitConvert<ulong, float>(1, 1);
            AssertImplicitConvert<ulong, double>(1, 1);
            AssertImplicitConvert<ulong, decimal>(1, 1);

            AssertImplicitConvert<float, double>(1, 1);

            AssertImplicitConvert<char, ushort>('a', 'a');
            AssertImplicitConvert<char, int>('a', 'a');
            AssertImplicitConvert<char, uint>('a', 'a');
            AssertImplicitConvert<char, long>('a', 'a');
            AssertImplicitConvert<char, ulong>('a', 'a');
            AssertImplicitConvert<char, float>('a', 'a');
            AssertImplicitConvert<char, double>('a', 'a');
            AssertImplicitConvert<char, decimal>('a', 'a');
        }

        [Fact]
        public void NumericNullableConversions()
        {
            AssertImplicitConvert<sbyte, short?>(1, 1);
            AssertImplicitConvert<sbyte, int?>(1, 1);
            AssertImplicitConvert<sbyte, long?>(1, 1);
            AssertImplicitConvert<sbyte, float?>(1, 1);
            AssertImplicitConvert<sbyte, double?>(1, 1);
            AssertImplicitConvert<sbyte, decimal?>(1, 1);

            AssertImplicitConvert<byte, short?>(1, 1);
            AssertImplicitConvert<byte, ushort?>(1, 1);
            AssertImplicitConvert<byte, int?>(1, 1);
            AssertImplicitConvert<byte, uint?>(1, 1);
            AssertImplicitConvert<byte, long?>(1, 1);
            AssertImplicitConvert<byte, ulong?>(1, 1);
            AssertImplicitConvert<byte, float?>(1, 1);
            AssertImplicitConvert<byte, double?>(1, 1);
            AssertImplicitConvert<byte, decimal?>(1, 1);

            AssertImplicitConvert<short, int?>(1, 1);
            AssertImplicitConvert<short, long?>(1, 1);
            AssertImplicitConvert<short, float?>(1, 1);
            AssertImplicitConvert<short, double?>(1, 1);
            AssertImplicitConvert<short, decimal?>(1, 1);

            AssertImplicitConvert<ushort, int?>(1, 1);
            AssertImplicitConvert<ushort, uint?>(1, 1);
            AssertImplicitConvert<ushort, long?>(1, 1);
            AssertImplicitConvert<ushort, ulong?>(1, 1);
            AssertImplicitConvert<ushort, float?>(1, 1);
            AssertImplicitConvert<ushort, double?>(1, 1);
            AssertImplicitConvert<ushort, decimal?>(1, 1);

            AssertImplicitConvert<int, long?>(1, 1);
            AssertImplicitConvert<int, float?>(1, 1);
            AssertImplicitConvert<int, double?>(1, 1);
            AssertImplicitConvert<int, decimal?>(1, 1);

            AssertImplicitConvert<uint, long?>(1, 1);
            AssertImplicitConvert<uint, ulong?>(1, 1);
            AssertImplicitConvert<uint, float?>(1, 1);
            AssertImplicitConvert<uint, double?>(1, 1);
            AssertImplicitConvert<uint, decimal?>(1, 1);

            AssertImplicitConvert<long, float?>(1, 1);
            AssertImplicitConvert<long, double?>(1, 1);
            AssertImplicitConvert<long, decimal?>(1, 1);

            AssertImplicitConvert<ulong, float?>(1, 1);
            AssertImplicitConvert<ulong, double?>(1, 1);
            AssertImplicitConvert<ulong, decimal?>(1, 1);

            AssertImplicitConvert<float, double?>(1, 1);

            AssertImplicitConvert<char, ushort?>('a', 'a');
            AssertImplicitConvert<char, int?>('a', 'a');
            AssertImplicitConvert<char, uint?>('a', 'a');
            AssertImplicitConvert<char, long?>('a', 'a');
            AssertImplicitConvert<char, ulong?>('a', 'a');
            AssertImplicitConvert<char, float?>('a', 'a');
            AssertImplicitConvert<char, double?>('a', 'a');
            AssertImplicitConvert<char, decimal?>('a', 'a');
        }

        [Fact]
        public void NumericNullableToNullableConversions()
        {
            AssertImplicitConvert<sbyte?, short?>(1, 1);
            AssertImplicitConvert<sbyte?, int?>(1, 1);
            AssertImplicitConvert<sbyte?, long?>(1, 1);
            AssertImplicitConvert<sbyte?, float?>(1, 1);
            AssertImplicitConvert<sbyte?, double?>(1, 1);
            AssertImplicitConvert<sbyte?, decimal?>(1, 1);

            AssertImplicitConvert<byte?, short?>(1, 1);
            AssertImplicitConvert<byte?, ushort?>(1, 1);
            AssertImplicitConvert<byte?, int?>(1, 1);
            AssertImplicitConvert<byte?, uint?>(1, 1);
            AssertImplicitConvert<byte?, long?>(1, 1);
            AssertImplicitConvert<byte?, ulong?>(1, 1);
            AssertImplicitConvert<byte?, float?>(1, 1);
            AssertImplicitConvert<byte?, double?>(1, 1);
            AssertImplicitConvert<byte?, decimal?>(1, 1);

            AssertImplicitConvert<short?, int?>(1, 1);
            AssertImplicitConvert<short?, long?>(1, 1);
            AssertImplicitConvert<short?, float?>(1, 1);
            AssertImplicitConvert<short?, double?>(1, 1);
            AssertImplicitConvert<short?, decimal?>(1, 1);

            AssertImplicitConvert<ushort?, int?>(1, 1);
            AssertImplicitConvert<ushort?, uint?>(1, 1);
            AssertImplicitConvert<ushort?, long?>(1, 1);
            AssertImplicitConvert<ushort?, ulong?>(1, 1);
            AssertImplicitConvert<ushort?, float?>(1, 1);
            AssertImplicitConvert<ushort?, double?>(1, 1);
            AssertImplicitConvert<ushort?, decimal?>(1, 1);

            AssertImplicitConvert<int?, long?>(1, 1);
            AssertImplicitConvert<int?, float?>(1, 1);
            AssertImplicitConvert<int?, double?>(1, 1);
            AssertImplicitConvert<int?, decimal?>(1, 1);

            AssertImplicitConvert<uint?, long?>(1, 1);
            AssertImplicitConvert<uint?, ulong?>(1, 1);
            AssertImplicitConvert<uint?, float?>(1, 1);
            AssertImplicitConvert<uint?, double?>(1, 1);
            AssertImplicitConvert<uint?, decimal?>(1, 1);

            AssertImplicitConvert<long?, float?>(1, 1);
            AssertImplicitConvert<long?, double?>(1, 1);
            AssertImplicitConvert<long?, decimal?>(1, 1);

            AssertImplicitConvert<ulong?, float?>(1, 1);
            AssertImplicitConvert<ulong?, double?>(1, 1);
            AssertImplicitConvert<ulong?, decimal?>(1, 1);

            AssertImplicitConvert<float?, double?>(1, 1);

            AssertImplicitConvert<char?, ushort?>('a', 'a');
            AssertImplicitConvert<char?, int?>('a', 'a');
            AssertImplicitConvert<char?, uint?>('a', 'a');
            AssertImplicitConvert<char?, long?>('a', 'a');
            AssertImplicitConvert<char?, ulong?>('a', 'a');
            AssertImplicitConvert<char?, float?>('a', 'a');
            AssertImplicitConvert<char?, double?>('a', 'a');
            AssertImplicitConvert<char?, decimal?>('a', 'a');

            AssertImplicitConvert<sbyte?, short?>(null, null);
            AssertImplicitConvert<sbyte?, int?>(null, null);
            AssertImplicitConvert<sbyte?, long?>(null, null);
            AssertImplicitConvert<sbyte?, float?>(null, null);
            AssertImplicitConvert<sbyte?, double?>(null, null);
            AssertImplicitConvert<sbyte?, decimal?>(null, null);

            AssertImplicitConvert<byte?, short?>(null, null);
            AssertImplicitConvert<byte?, ushort?>(null, null);
            AssertImplicitConvert<byte?, int?>(null, null);
            AssertImplicitConvert<byte?, uint?>(null, null);
            AssertImplicitConvert<byte?, long?>(null, null);
            AssertImplicitConvert<byte?, ulong?>(null, null);
            AssertImplicitConvert<byte?, float?>(null, null);
            AssertImplicitConvert<byte?, double?>(null, null);
            AssertImplicitConvert<byte?, decimal?>(null, null);

            AssertImplicitConvert<short?, int?>(null, null);
            AssertImplicitConvert<short?, long?>(null, null);
            AssertImplicitConvert<short?, float?>(null, null);
            AssertImplicitConvert<short?, double?>(null, null);
            AssertImplicitConvert<short?, decimal?>(null, null);

            AssertImplicitConvert<ushort?, int?>(null, null);
            AssertImplicitConvert<ushort?, uint?>(null, null);
            AssertImplicitConvert<ushort?, long?>(null, null);
            AssertImplicitConvert<ushort?, ulong?>(null, null);
            AssertImplicitConvert<ushort?, float?>(null, null);
            AssertImplicitConvert<ushort?, double?>(null, null);
            AssertImplicitConvert<ushort?, decimal?>(null, null);

            AssertImplicitConvert<int?, long?>(null, null);
            AssertImplicitConvert<int?, float?>(null, null);
            AssertImplicitConvert<int?, double?>(null, null);
            AssertImplicitConvert<int?, decimal?>(null, null);

            AssertImplicitConvert<uint?, long?>(null, null);
            AssertImplicitConvert<uint?, ulong?>(null, null);
            AssertImplicitConvert<uint?, float?>(null, null);
            AssertImplicitConvert<uint?, double?>(null, null);
            AssertImplicitConvert<uint?, decimal?>(null, null);

            AssertImplicitConvert<long?, float?>(null, null);
            AssertImplicitConvert<long?, double?>(null, null);
            AssertImplicitConvert<long?, decimal?>(null, null);

            AssertImplicitConvert<ulong?, float?>(null, null);
            AssertImplicitConvert<ulong?, double?>(null, null);
            AssertImplicitConvert<ulong?, decimal?>(null, null);

            AssertImplicitConvert<float?, double?>(null, null);

            AssertImplicitConvert<char?, ushort?>(null, null);
            AssertImplicitConvert<char?, int?>(null, null);
            AssertImplicitConvert<char?, uint?>(null, null);
            AssertImplicitConvert<char?, long?>(null, null);
            AssertImplicitConvert<char?, ulong?>(null, null);
            AssertImplicitConvert<char?, float?>(null, null);
            AssertImplicitConvert<char?, double?>(null, null);
            AssertImplicitConvert<char?, decimal?>(null, null);
        }

        [Fact]
        public void StructToInterfacesAndBase()
        {
            DateTime now = DateTime.Now;
            AssertImplicitConvert<DateTime, IEquatable<DateTime>>(now, now);
            AssertImplicitConvert<DateTime, IComparable<DateTime>>(now, now);
            AssertImplicitConvert<DateTime, ISerializable>(now, now);
            AssertImplicitConvert<DateTime, IFormattable>(now, now);
        }

        [Fact]
        [ActiveIssue(31032, TargetFrameworkMonikers.NetFramework)]
        public void ArraysToInterfaces()
        {
            int[] intArray = {1, 2, 3};
            AssertImplicitConvert<int[], IEnumerable>(intArray, intArray);
            AssertImplicitConvert<int[], IEnumerable<int>>(intArray, intArray);
            AssertImplicitConvert<int[], IList<int>>(intArray, intArray);
            AssertImplicitConvert<int[], ICollection<int>>(intArray, intArray);
            AssertImplicitConvert<int[], IReadOnlyList<int>>(intArray, intArray);
            AssertImplicitConvert<int[], IReadOnlyCollection<int>>(intArray, intArray);
        }

        [Fact]
        public void ArraysToInterfacesAsArguments()
        {
            dynamic intArray = new[] {1, 2, 3};
            TestTypeAsArgument<IEnumerable>(intArray);
            TestTypeAsArgument<IEnumerable<int>>(intArray);
            TestTypeAsArgument<IList<int>>(intArray);
            TestTypeAsArgument<ICollection<int>>(intArray);
            TestTypeAsArgument<IReadOnlyList<int>>(intArray);
            TestTypeAsArgument<IReadOnlyCollection<int>>(intArray);
        }

        [Fact]
        public void ArraysToIncompatibleInterfaces()
        {
            int[] intArray = { 1, 2, 3 };
            AssertBadImplicitConvert<int[], IEnumerable<string>>(intArray);
            AssertBadImplicitConvert<int[], IList<int?>>(intArray);
            AssertBadImplicitConvert<int[], ICollection<Uri>>(intArray);
            AssertBadImplicitConvert<int[], IReadOnlyList<DateTime>>(intArray);
            AssertBadImplicitConvert<int[], IReadOnlyCollection<long>>(intArray);
            AssertBadImplicitConvert<int[], IOrderedEnumerable<int>>(intArray);
        }

        [Fact]
        public void ArraysToIncompatibleInterfacesAsArgument()
        {
            int[] intArray = { 1, 2, 3 };
            TestBadTypeAsArgument<IEnumerable<string>>(intArray);
            TestBadTypeAsArgument<IList<int?>>(intArray);
            TestBadTypeAsArgument<ICollection<Uri>>(intArray);
            TestBadTypeAsArgument<IReadOnlyList<DateTime>>(intArray);
            TestBadTypeAsArgument<IReadOnlyCollection<long>>(intArray);
            TestBadTypeAsArgument<IOrderedEnumerable<int>>(intArray);
        }
    }
}
