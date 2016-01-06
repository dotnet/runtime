// Copyright (c) Jon Hanna. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

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

        private static IEnumerable<object[]> TypesData()
        {
            return Types().Select(i => new object[] { i });
        }

        private static IEnumerable<object> ObjectAssignableConstantValues()
        {
            yield return new object();
            yield return "Hello";
            yield return new Uri("http://example.net/");
        }

        private static IEnumerable<object> NonObjectAssignableConstantValues()
        {
            yield return 42;
            yield return 42L;
            yield return DateTime.MinValue;
        }

        private static IEnumerable<object> ConstantValues()
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
