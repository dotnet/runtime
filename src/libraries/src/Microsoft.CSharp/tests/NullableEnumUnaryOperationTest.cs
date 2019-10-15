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
    public class NullableEnumUnaryOperationTest
    {
        public enum Int8Enum : byte
        {
            A,
            B,
            C,
            D,
            E
        }

        public enum Int32Enum
        {
            A,
            B,
            C,
            D,
            E
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
            E
        }

        public static IEnumerable<object[]> Int8Enums() => Enum.GetValues(typeof(Int8Enum))
            .Cast<Int8Enum>()
            .Select(x => new object[] { x });

        public static IEnumerable<object[]> Int8NullableEnums() => Int8Enums().Append(new object[] { null });

        public static IEnumerable<object[]> Int32Enums() => Enum.GetValues(typeof(Int32Enum))
            .Cast<Int32Enum>()
            .Select(x => new object[] { x });

        public static IEnumerable<object[]> Int32NullableEnums() => Int32Enums().Append(new object[] { null });

        public static IEnumerable<object[]> UInt32Enums() => Enum.GetValues(typeof(UInt32Enum))
            .Cast<UInt32Enum>()
            .Select(x => new object[] { x });

        public static IEnumerable<object[]> UInt32NullableEnums() => UInt32Enums().Append(new object[] { null });

        public static IEnumerable<object[]> Int64Enums() => Enum.GetValues(typeof(Int64Enum))
            .Cast<Int64Enum>()
            .Select(x => new object[] { x });

        public static IEnumerable<object[]> Int64NullableEnums() => Int64Enums().Append(new object[] { null });

        [Theory, MemberData(nameof(Int8NullableEnums)), SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "25067 is not fixed in NetFX")]
        public void ComplementInt8NullableEnum(Int8Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.OnesComplement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int8Enum?, object>> site = CallSite<Func<CallSite, Int8Enum?, object>>.Create(binder);
            Func<CallSite, Int8Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(~value, result);
        }

        [Theory, MemberData(nameof(Int32NullableEnums)), SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "25067 is not fixed in NetFX")]
        public void ComplementInt32NullableEnum(Int32Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.OnesComplement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int32Enum?, object>> site = CallSite<Func<CallSite, Int32Enum?, object>>.Create(binder);
            Func<CallSite, Int32Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(~value, result);
        }

        [Theory, MemberData(nameof(UInt32NullableEnums)), SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "25067 is not fixed in NetFX")]
        public void ComplementUInt32NullableEnum(UInt32Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.OnesComplement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, UInt32Enum?, object>> site = CallSite<Func<CallSite, UInt32Enum?, object>>.Create(binder);
            Func<CallSite, UInt32Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(~value, result);
        }

        [Theory, MemberData(nameof(Int64NullableEnums)), SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "25067 is not fixed in NetFX")]
        public void ComplementInt64NullableEnum(Int64Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.OnesComplement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int64Enum?, object>> site = CallSite<Func<CallSite, Int64Enum?, object>>.Create(binder);
            Func<CallSite, Int64Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(~value, result);
        }

        [Theory, MemberData(nameof(Int32Enums))]
        public void IncrementInt32NullableEnum(Int32Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.Increment,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int32Enum?, object>> site = CallSite<Func<CallSite, Int32Enum?, object>>.Create(binder);
            Func<CallSite, Int32Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(++value, result); // Note that there is no write-back to value.
        }

        [Theory, MemberData(nameof(Int32Enums))]
        public void DecrementInt32NullableEnum(Int32Enum? value)
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.Decrement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int32Enum?, object>> site = CallSite<Func<CallSite, Int32Enum?, object>>.Create(binder);
            Func<CallSite, Int32Enum?, object> targ = site.Target;
            object result = targ(site, value);
            Assert.Equal(--value, result); // Note that there is no write-back to value.
        }

        [Fact]
        public void CannotIncrementNullNullableEnum()
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.Increment,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int32Enum?, object>> site = CallSite<Func<CallSite, Int32Enum?, object>>.Create(binder);
            Func<CallSite, Int32Enum?, object> targ = site.Target;
            Assert.Throws<RuntimeBinderException>(() => targ(site, default));
        }

        [Fact]
        public void CannotDecrementNullNullableEnum()
        {
            CSharpArgumentInfo x = CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.UseCompileTimeType, null);
            CallSiteBinder binder =
                Binder.UnaryOperation(
                    CSharpBinderFlags.None, ExpressionType.Decrement,
                    GetType(), new[] { x });
            CallSite<Func<CallSite, Int32Enum?, object>> site = CallSite<Func<CallSite, Int32Enum?, object>>.Create(binder);
            Func<CallSite, Int32Enum?, object> targ = site.Target;
            Assert.Throws<RuntimeBinderException>(() => targ(site, default));
        }
    }
}
