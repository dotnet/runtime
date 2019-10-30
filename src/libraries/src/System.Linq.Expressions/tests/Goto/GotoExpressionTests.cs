// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class GotoExpressionTests
    {
        protected static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        public static IEnumerable<Type> ObjectAssignableTypes()
        {
            yield return typeof(object);
            yield return typeof(string);
            yield return typeof(Return);
        }

        public static IEnumerable<Type> NonObjectAssignableTypes()
        {
            yield return typeof(int);
            yield return typeof(ExpressionType);
        }

        public static IEnumerable<Type> Types()
        {
            return ObjectAssignableTypes().Concat(NonObjectAssignableTypes());
        }

        public static IEnumerable<object[]> TypesData()
        {
            return Types().Select(i => new object[] { i });
        }

        public static IEnumerable<object> ObjectAssignableConstantValues()
        {
            yield return new object();
            yield return "Hello";
            yield return new Uri("http://example.net/");
        }

        public static IEnumerable<object> NonObjectAssignableConstantValues()
        {
            yield return 42;
            yield return 42L;
            yield return DateTime.MinValue;
        }

        public static IEnumerable<object> ConstantValues()
        {
            return NonObjectAssignableConstantValues().Concat(ObjectAssignableConstantValues());
        }

        public static IEnumerable<object[]> ConstantValueData()
        {
            return ConstantValues().Select(i => new object[] { i });
        }

        public static IEnumerable<object[]> NonObjectAssignableConstantValueData()
        {
            return NonObjectAssignableConstantValues().Select(i => new object[] { i });
        }

        public static IEnumerable<object[]> ObjectAssignableConstantValueData()
        {
            return ObjectAssignableConstantValues().Select(i => new object[] { i });
        }
    }
}
