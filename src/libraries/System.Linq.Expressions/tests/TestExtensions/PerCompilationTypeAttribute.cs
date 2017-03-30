// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;
using Xunit.Sdk;

namespace System.Linq.Expressions.Tests
{
    /// <summary>
    /// Operates as per <see cref="MemberDataAttribute"/>, but adds a final boolean value to the list of arguments,
    /// permuted through both false and true.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    internal class PerCompilationTypeAttribute : DataAttribute
    {
        private static readonly object s_boxedFalse = false;
        private static readonly object s_boxedTrue = true;

        private readonly MemberDataAttribute delegatedTo;

        public PerCompilationTypeAttribute(string memberName, params object[] parameters)
        {
            delegatedTo = new MemberDataAttribute(memberName, parameters);
        }

        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // Re-using the arrays would be a nice optimization, and safe since this is internal and we could
            // just not do the sort of uses that would break that, but xUnit pre-loads GetData() results and
            // we'd therefore end up with multiple copies of the last result.
            foreach (object[] received in delegatedTo.GetData(testMethod))
            {
#if FEATURE_COMPILE
                object[] withFalse = new object[received.Length + 1];
#endif

#if FEATURE_INTERPRET
                object[] withTrue = new object[received.Length + 1];
#endif

#if FEATURE_COMPILE
                withFalse[received.Length] = s_boxedFalse;
#endif

#if FEATURE_INTERPRET
                withTrue[received.Length] = s_boxedTrue;
#endif

                for (int i = 0; i != received.Length; ++i)
                {
                    object arg = received[i];

#if FEATURE_COMPILE
                    withFalse[i] = arg;
#endif

#if FEATURE_INTERPRET
                    withTrue[i] = arg;
#endif
                }

#if FEATURE_COMPILE
                yield return withFalse;
#endif

#if  FEATURE_INTERPRET
                yield return withTrue;
#endif
            }
        }
    }
}
