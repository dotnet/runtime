// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Linq.Tests
{
    public class ConsistencyTests
    {
        [Fact]
        public static void MatchSequencePattern()
        {
            MethodInfo enumerableNotInQueryable = GetMissingExtensionMethod(typeof(Enumerable), typeof(Queryable), GetExcludedMethods());

            Assert.True(enumerableNotInQueryable == null, string.Format("Enumerable method {0} not defined by Queryable", enumerableNotInQueryable));

            MethodInfo queryableNotInEnumerable = GetMissingExtensionMethod(
                typeof(Queryable),
                typeof(Enumerable),
                 new[] {
                     nameof(Queryable.AsQueryable)
                 }
                );

            Assert.True(queryableNotInEnumerable == null, string.Format("Queryable method {0} not defined by Enumerable", queryableNotInEnumerable));
        }

        // If a change to Enumerable has required a change to the exception list in this test
        // make the same change at src/System.Linq.Queryable/tests/Queryable.cs.
        private static IEnumerable<string> GetExcludedMethods()
        {
            IEnumerable<string> result = new[]
            {
                nameof(Enumerable.ToLookup),
                nameof(Enumerable.ToDictionary),
                nameof(Enumerable.ToArray),
                nameof(Enumerable.AsEnumerable),
                nameof(Enumerable.ToList),
                nameof(Enumerable.ToHashSet),
                nameof(Enumerable.TryGetNonEnumeratedCount),
                "Fold",
                "LeftJoin",
            };

            return result;
        }

        private static MethodInfo GetMissingExtensionMethod(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type a,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type b,
            IEnumerable<string> excludedMethods)
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
                    if (ta.GetTypeInfo().IsGenericType && tb.GetTypeInfo().IsGenericType)
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
                if (t.GetTypeInfo().IsGenericType)
                {
                    Type g = t;
                    if (!g.GetTypeInfo().IsGenericTypeDefinition)
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
