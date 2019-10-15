// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class ReferenceEqualityTests
    {
        protected static IEnumerable<object[]> ReferenceObjectsData()
        {
            return ReferenceObjects().Select(i => new[] { i });
        }

        protected static IEnumerable<object[]> DifferentObjects()
        {
            return from x in ReferenceObjects()
                   from y in ReferenceObjects()
                   where !ReferenceEquals(x, y)
                   && (x.GetType().IsAssignableFrom(y.GetType()) || y.GetType().IsAssignableFrom(x.GetType()))
                   select new[] { x, y };
        }

        protected static IEnumerable<object[]> ComparableValuesData()
        {
            return ComparableValues().Select(i => new object[] { i });
        }

        protected static IEnumerable<object[]> DifferentComparableValues()
        {
            return from x in ComparableValues()
                   from y in ComparableValues()
                   where !(ReferenceEquals(x, y))
                   select new[] { x, y };
        }

        public static IEnumerable<object[]> ComparableReferenceTypesData()
        {
            return ComparableReferenceTypes().Select(i => new object[] { i });
        }

        public static IEnumerable<object[]> LeftValueType()
        {
            return from x in ReferenceObjects()
                   from y in ValueTypeObjects()
                   select new[] { y, x };
        }

        public static IEnumerable<object[]> RightValueType()
        {
            return from x in ReferenceObjects()
                   from y in ValueTypeObjects()
                   select new[] { x, y };
        }

        public static IEnumerable<object[]> BothValueType()
        {
            return from x in ValueTypeObjects()
                   from y in ValueTypeObjects()
                   select new[] { x, y };
        }

        public static IEnumerable<object[]> UnassignablePairs()
        {
            return from x in ReferenceObjects()
                   from y in ReferenceObjects()
                   where !x.GetType().IsAssignableFrom(y.GetType()) && !y.GetType().IsAssignableFrom(x.GetType())
                   select new[] { x, y };
        }

        public static IEnumerable<object> ReferenceObjects()
        {
            yield return new object();
            yield return "";
            yield return "Hello";
            yield return new Uri("http://example.net/");
            yield return new Uri("http://example.net/");
        }

        public static IEnumerable<IComparable> ComparableValues()
        {
            yield return 1;
            yield return DateTime.MinValue;
            foreach (IComparable value in ComparableReferenceTypes())
                yield return value;
        }

        public static IEnumerable<IComparable> ComparableReferenceTypes()
        {
            yield return "abc";
            yield return "";
            yield return "Hello";
        }

        public static IEnumerable<object> ValueTypeObjects()
        {
            yield return 0;
            yield return 0m;
            yield return DateTime.MinValue;
        }

        public static IEnumerable<object[]> ReferenceTypesData()
        {
            return ReferenceTypes().Select(i => new object[] { i });
        }

        public static IEnumerable<Type> ReferenceTypes()
        {
            yield return typeof(object);
            yield return typeof(string);
            yield return typeof(ReferenceEqual);
        }

        protected static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }
    }
}
