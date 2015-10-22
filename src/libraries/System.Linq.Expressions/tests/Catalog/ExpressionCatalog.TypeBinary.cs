// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> TypeIs()
        {
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(int?));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(int));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(IComparable));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(IEquatable<int>));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(bool));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(ValueType));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int)), typeof(object));

            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(int?));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(int));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(IComparable));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(IEquatable<int>));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(bool));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(ValueType));
            yield return Expression.TypeIs(Expression.Constant(1, typeof(int?)), typeof(object));

            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(int?));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(int));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(IComparable));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(IEquatable<int>));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(bool));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(ValueType));
            yield return Expression.TypeIs(Expression.Constant(null, typeof(int?)), typeof(object));

            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(string));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(int));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(object));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IComparable));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IEquatable<string>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IEnumerable<char>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IEnumerable));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IEquatable<int>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(string)), typeof(IComparer<bool>));

            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(string));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(int));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(object));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IComparable));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEquatable<string>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEnumerable<char>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEnumerable));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEquatable<int>));
            yield return Expression.TypeIs(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IComparer<bool>));
        }

        private static IEnumerable<Expression> TypeEqual()
        {
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(int?));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(int));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(IComparable));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(IEquatable<int>));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(bool));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(ValueType));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int)), typeof(object));

            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(int?));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(int));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(IComparable));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(IEquatable<int>));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(bool));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(ValueType));
            yield return Expression.TypeEqual(Expression.Constant(1, typeof(int?)), typeof(object));

            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(int?));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(int));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(IComparable));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(IEquatable<int>));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(bool));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(ValueType));
            yield return Expression.TypeEqual(Expression.Constant(null, typeof(int?)), typeof(object));

            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(string));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(int));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(object));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IComparable));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IEquatable<string>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IEnumerable<char>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IEnumerable));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IEquatable<int>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(string)), typeof(IComparer<bool>));

            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(string));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(int));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(object));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IComparable));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEquatable<string>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEnumerable<char>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEnumerable));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IEquatable<int>));
            yield return Expression.TypeEqual(Expression.Constant("bar", typeof(IEnumerable<char>)), typeof(IComparer<bool>));
        }
    }
}