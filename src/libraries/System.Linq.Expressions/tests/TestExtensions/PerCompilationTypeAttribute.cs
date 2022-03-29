// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                object[] withFalse = null;                
                if (PlatformDetection.IsNotLinqExpressionsBuiltWithIsInterpretingOnly)
                {
                    withFalse = new object[received.Length + 1];
                    withFalse[received.Length] = s_boxedFalse;
                }

                object[] withTrue = new object[received.Length + 1];
                withTrue[received.Length] = s_boxedTrue;

                for (int i = 0; i != received.Length; ++i)
                {
                    object arg = received[i];

                    if (withFalse != null)
                        withFalse[i] = arg;

                    withTrue[i] = arg;
                }

                if (withFalse != null)
                    yield return withFalse;

                yield return withTrue;
            }
        }
    }
}
