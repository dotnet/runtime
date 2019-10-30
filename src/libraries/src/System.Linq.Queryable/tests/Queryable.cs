// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Linq.Tests
{
    public class QueryableTests
    {
        [Fact]
        public void AsQueryable()
        {
            Assert.NotNull(((IEnumerable)(new int[] { })).AsQueryable());
        }

        [Fact]
        public void AsQueryableT()
        {
            Assert.NotNull((new int[] { }).AsQueryable());
        }

        [Fact]
        public void NullAsQueryableT()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable<int>)null).AsQueryable());
        }

        [Fact]
        public void NullAsQueryable()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => ((IEnumerable)null).AsQueryable());
        }

        private class NonGenericEnumerableSoWeDontNeedADependencyOnTheAssemblyWithNonGeneric : IEnumerable
        {
            public IEnumerator GetEnumerator()
            {
                yield break;
            }
        }

        [Fact]
        public void NonGenericToQueryable()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new NonGenericEnumerableSoWeDontNeedADependencyOnTheAssemblyWithNonGeneric().AsQueryable());
        }

        [Fact]
        public void ReturnsSelfIfPossible()
        {
            IEnumerable<int> query = Enumerable.Repeat(1, 2).AsQueryable();
            Assert.Same(query, query.AsQueryable());
        }

        [Fact]
        public void ReturnsSelfIfPossibleNonGeneric()
        {
            IEnumerable query = Enumerable.Repeat(1, 2).AsQueryable();
            Assert.Same(query, query.AsQueryable());
        }

        [Fact]
        public static void QueryableOfQueryable()
        {
            IQueryable<int> queryable1 = new [] { 1, 2, 3 }.AsQueryable();
            IQueryable<int>[] queryableArray1 = { queryable1, queryable1 };
            IQueryable<IQueryable<int>> queryable2 = queryableArray1.AsQueryable();
            ParameterExpression expression1 = Expression.Parameter(typeof(IQueryable<int>), "i");
            ParameterExpression[] expressionArray1 = { expression1 };
            IQueryable<IQueryable<int>> queryable3 = queryable2.Select(Expression.Lambda<Func<IQueryable<int>, IQueryable<int>>>(expression1, expressionArray1));
            int i = queryable3.Count();
            Assert.Equal(2, i);
        }

        [Fact]
        public static void MatchSequencePattern()
        {
            // If a change to Queryable has required a change to the exception list in this test
            // make the same change at src/System.Linq/tests/ConsistencyTests.cs
            MethodInfo enumerableNotInQueryable = GetMissingExtensionMethod(
                typeof(Enumerable),
                typeof(Queryable),
                 new [] {
                     "ToLookup",
                     "ToDictionary",
                     "ToArray",
                     "AsEnumerable",
                     "ToList",
                     "Fold",
                     "LeftJoin",
                     "Append",
                     "Prepend",
                     "ToHashSet"
                 }
                );

            Assert.True(enumerableNotInQueryable == null, string.Format("Enumerable method {0} not defined by Queryable", enumerableNotInQueryable));

            MethodInfo queryableNotInEnumerable = GetMissingExtensionMethod(
                typeof(Queryable),
                typeof(Enumerable),
                 new [] {
                     "AsQueryable"
                 }
                );

            Assert.True(queryableNotInEnumerable == null, string.Format("Queryable method {0} not defined by Enumerable", queryableNotInEnumerable));
        }

        private static MethodInfo GetMissingExtensionMethod(Type a, Type b, IEnumerable<string> excludedMethods)
        {
            var dex = new HashSet<string>(excludedMethods);

            var aMethods =
                a.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.CustomAttributes.Any(c => c.AttributeType == typeof(ExtensionAttribute)))
                .ToLookup(m => m.Name);

            MethodComparer mc = new MethodComparer();
            var bMethods = b.GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(m => m.CustomAttributes.Any(c => c.AttributeType == typeof(ExtensionAttribute)))
                .ToLookup(m => m, mc);

            foreach (var group in aMethods.Where(g => !dex.Contains(g.Key)))
            {
                foreach (MethodInfo m in group)
                {
                    if (!bMethods.Contains(m))
                        return m;
                }
            }

            return null;
        }

        private class MethodComparer : IEqualityComparer<MethodInfo>
        {
            public int GetHashCode(MethodInfo m) => m.Name.GetHashCode();

            public bool Equals(MethodInfo a, MethodInfo b)
            {
                if (a.Name != b.Name)
                    return false;

                ParameterInfo[] pas = a.GetParameters();
                ParameterInfo[] pbs = b.GetParameters();
                if (pas.Length != pbs.Length)
                    return false;

                Type[] aArgs = a.GetGenericArguments();
                Type[] bArgs = b.GetGenericArguments();
                for (int i = 0, n = pas.Length; i < n; i++)
                {
                    ParameterInfo pa = pas[i];
                    ParameterInfo pb = pbs[i];
                    Type ta = Strip(pa.ParameterType);
                    Type tb = Strip(pb.ParameterType);
                    if (ta.IsGenericType && tb.IsGenericType)
                    {
                        if (ta.GetGenericTypeDefinition() != tb.GetGenericTypeDefinition())
                        {
                            return false;
                        }
                    }
                    else if (ta.IsGenericParameter && tb.IsGenericParameter)
                    {
                        return Array.IndexOf(aArgs, ta) == Array.IndexOf(bArgs, tb);
                    }
                    else if (ta != tb)
                    {
                        return false;
                    }
                }

                return true;
            }

            private Type Strip(Type t)
            {
                if (t.IsGenericType)
                {
                    Type g = t;
                    if (!g.IsGenericTypeDefinition)
                    {
                        g = t.GetGenericTypeDefinition();
                    }
                    if (g == typeof(IQueryable<>) || g == typeof(IEnumerable<>))
                    {
                        return typeof(IEnumerable);
                    }
                    if (g == typeof(Expression<>))
                    {
                        return t.GetGenericArguments()[0];
                    }
                    if (g == typeof(IOrderedEnumerable<>) || g == typeof(IOrderedQueryable<>))
                    {
                        return typeof(IOrderedQueryable);
                    }
                }
                else
                {
                    if (t == typeof(IQueryable))
                    {
                        return typeof(IEnumerable);
                    }
                }

                return t;
            }
        }
    }
}
