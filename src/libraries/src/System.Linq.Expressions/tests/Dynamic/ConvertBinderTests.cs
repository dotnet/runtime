// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Xunit;

namespace System.Dynamic.Test
{
    public class ConvertBinderTests
    {
        private class MinimumOverrideConvertBinder : ConvertBinder
        {
            public MinimumOverrideConvertBinder(Type type, bool @explicit) : base(type, @explicit)
            {
            }

            public override DynamicMetaObject FallbackConvert(
                DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotSupportedException();
            }
        }

        private static readonly int[] SomeInt32 = {0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1};

        private static readonly long[] SomeInt64 = {0L, 1L, 2L, -1L, long.MinValue, long.MaxValue, long.MaxValue - 1};

        private static Type[] SomeTypes = { typeof(ConvertBinderTests), typeof(ConvertBinder), typeof(string), typeof(int), typeof(void) };

        private static IEnumerable<object[]> Int32Args() => SomeInt32.Select(i => new object[] {i});

        private static IEnumerable<object[]> Int64Arges() => SomeInt64.Select(i => new object[] {i});

        private static IEnumerable<object[]> TypesAndBools() =>
            SomeTypes.SelectMany(t => new[] {false, true}, (t, b) => new object[] {t, b});


        [Theory, MemberData(nameof(Int32Args))]
        public void ConvertImplicit(int x)
        {
            dynamic d = x;
            long xl = d;
            Assert.Equal(x, xl);
        }

        [Theory, MemberData(nameof(Int64Arges))]
        public void ConvertExplicit(long x)
        {
            dynamic d = x;
            int xi = (int)d;
            Assert.Equal((int)x, xi);
        }

        [Theory, MemberData(nameof(Int64Arges))]
        public void ConvertExplicitOvf(long x)
        {
            dynamic d = x;
            if (x < int.MinValue | x > int.MaxValue)
            {
                Assert.Throws<OverflowException>(() => checked((int)d));
            }
            else
            {
                Assert.Equal(x, checked((int)d));
            }
        }

        [Fact]
        public void ImpossibleConversion()
        {
            dynamic d = 42;
            Assert.Throws<RuntimeBinderException>(() => (bool)d);
        }

        [Fact]
        public void NullType()
        {
            Assert.Throws<ArgumentNullException>("type", () => new MinimumOverrideConvertBinder(null, true));
            Assert.Throws<ArgumentNullException>("type", () => new MinimumOverrideConvertBinder(null, false));
        }

        [Theory, MemberData(nameof(TypesAndBools))]
        public void Properties(Type type, bool @explicit)
        {
            var binder = new MinimumOverrideConvertBinder(type, @explicit);
            Assert.Equal(type, binder.Type);
            Assert.Equal(type, binder.ReturnType);
            Assert.Equal(@explicit, binder.Explicit);
        }

        [Fact]
        public void NullTarget()
        {
            var binder = new MinimumOverrideConvertBinder(typeof(int), false);
            Assert.Throws<ArgumentNullException>("target", () => binder.Bind(null, null));
        }

        [Fact]
        public void ArgumentPassed()
        {
            var target = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var arg = new DynamicMetaObject(Expression.Parameter(typeof(object), null), BindingRestrictions.Empty);
            var binder = new MinimumOverrideConvertBinder(typeof(int), false);
            Assert.Throws<ArgumentException>("args", () => binder.Bind(target, new[] { arg }));
        }
    }
}
