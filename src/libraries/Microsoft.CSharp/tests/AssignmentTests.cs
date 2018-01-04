// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class AssignmentTests
    {
        public static object[][] Additions =
        {
            new object[]{1, 2, 3},
            new object[]{1L, 2, 3L},
            new object[]{1, 2L, 3L},
            new object[]{1U, 2U, 3U},
            new object[]{1, 2U, 3L},
            new object[]{1, 2M, 3M},
            new object[]{1.0, 2, 3.0},
            new object[]{1, 2.0, 3.0}
        };

        private sealed class HasProperty<T>
        {
            public T Prop { get; set; }
        }

        private static HasProperty<T> GetHasProperty<T>(T initial) => new HasProperty<T>{ Prop = initial };

        public static object[][] PropertyAdditions =
        {
            new object[]{GetHasProperty(1), 2, 3},
            new object[]{GetHasProperty(1L), 2, 3L},
            new object[]{GetHasProperty(1), 2L, 3},
            new object[]{GetHasProperty(1U), 2U, 3U},
            new object[]{GetHasProperty(1), 2U, 3},
            new object[]{GetHasProperty(1), 2M, 3},
            new object[]{GetHasProperty(1.0), 2, 3.0},
            new object[]{GetHasProperty(1), 2.0, 3}
        };

        public static object[][] PropertyAssigments =
        {
            new object[]{GetHasProperty(1), 2, 2},
            new object[]{GetHasProperty(1L), 2, 2L},
            new object[]{GetHasProperty(1), 2L, 2},
            new object[]{GetHasProperty(1U), 2U, 2U},
            new object[]{GetHasProperty(1), 2U, 2},
            new object[]{GetHasProperty(1), 2M, 2},
            new object[]{GetHasProperty(1.0), 2, 2.0},
            new object[]{GetHasProperty(1), 2.0, 2}
        };

        public static object[][] PropertyBadAssigments =
        {
            new object[]{GetHasProperty(1), decimal.MaxValue, true, false},
            new object[]{GetHasProperty(1), DateTime.MinValue, false, false},
            new object[]{GetHasProperty(2), "hello", false, false},
            new object[]{GetHasProperty(1), null, false, false},
            new object[]{GetHasProperty(true), 2, false, false},
            new object[]{GetHasProperty(2.0), false, false, false},
            new object[]{GetHasProperty("abc"), 2.0, false, false},
            new object[]{GetHasProperty("abc"), new object(), false, true},
            new object[]{GetHasProperty(2), new object(), false, true}
        };

        public static IEnumerable<object[]> PropertyBadCheckedAssigments =
            new[]
            {
                new object[] {GetHasProperty(1), long.MaxValue, true, false},
                new object[] {GetHasProperty(1), (double)long.MaxValue, true, false},
                new object[] {GetHasProperty(1U), -1, true, false},
                new object[] {GetHasProperty(1), uint.MaxValue, true, false},
                new object[] {GetHasProperty(default(short)), int.MaxValue, true, false},
                new object[]{GetHasProperty(2UL), -1, true, false}
            }.Concat(PropertyBadAssigments);

        [Theory, MemberData(nameof(Additions))]
        public void AddAssignment(dynamic lhs, dynamic rhs, object expected)
        {
            lhs += rhs;
            Assert.Equal(expected, lhs);
            Assert.IsType(expected.GetType(), lhs);
        }

        [Theory, MemberData(nameof(PropertyAdditions))]
        public void PropertyAddAssignment(dynamic lhs, dynamic rhs, object expected)
        {
            lhs.Prop += rhs;
            Assert.Equal(expected, lhs.Prop);
        }

        [Theory, MemberData(nameof(PropertyAssigments))]
        public void PropertyAddResultAssigment(object lhs, object rhs, object expected)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment, "Prop", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            object result = callSite.Target(callSite, lhs, rhs);
            Assert.Equal(expected, result);
            Assert.Equal(expected, ((dynamic)lhs).Prop);
        }

        [Theory, MemberData(nameof(PropertyAssigments))]
        public void PropertyAddConstantResultAssigment(object lhs, object rhs, object expected)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment, "Prop", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
                        }));
            object result = callSite.Target(callSite, lhs, rhs);
            Assert.Equal(expected, result);
            Assert.Equal(expected, ((dynamic)lhs).Prop);
        }

        [Theory, MemberData(nameof(PropertyBadAssigments))]
        public void PropertyIncompatibleAssigment(object lhs, object rhs, bool overflow, bool invalidCast)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment, "Prop", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            if (invalidCast) // Invalid cast at runtime
            {
                Assert.Throws<InvalidCastException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else if (overflow) // Overflows at runtime, rather than fail at bind time
            {
                Assert.Throws<OverflowException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else
            {
                Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, lhs, rhs));
            }
        }

        [Theory, MemberData(nameof(PropertyBadAssigments))]
        public void PropertyIncompatibleConstantAssigment(object lhs, object rhs, bool _, bool invalidCast)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment, "Prop", GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
                        }));
            if (invalidCast)
            {
                Assert.Throws<InvalidCastException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else
            {
                Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, lhs, rhs));
            }
        }

        [Theory, MemberData(nameof(PropertyBadCheckedAssigments))]
        public void PropertyIncompatibleCheckedAssigment(object lhs, object rhs, bool overflow, bool invalidCast)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment | CSharpBinderFlags.CheckedContext, "Prop",
                        GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }));
            if (invalidCast)
            {
                Assert.Throws<InvalidCastException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else if (overflow) // Overflows at runtime, rather than fail at bind time
            {
                Assert.Throws<OverflowException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else
            {
                Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, lhs, rhs));
            }
        }

        [Theory, MemberData(nameof(PropertyBadCheckedAssigments))]
        public void PropertyIncompatibleConstantCheckedAssigment(object lhs, object rhs, bool _, bool invalidCast)
        {
            CallSite<Func<CallSite, object, object, object>> callSite =
                CallSite<Func<CallSite, object, object, object>>.Create(
                    Binder.SetMember(
                        CSharpBinderFlags.ValueFromCompoundAssignment | CSharpBinderFlags.CheckedContext, "Prop",
                        GetType(),
                        new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.Constant, null)
                        }));
            if (invalidCast) // Invalid cast at runtime
            {
                Assert.Throws<InvalidCastException>(() => callSite.Target(callSite, lhs, rhs));
            }
            else
            {
                Assert.Throws<RuntimeBinderException>(() => callSite.Target(callSite, lhs, rhs));
            }
        }
    }
}
