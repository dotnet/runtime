// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class TrimCompatibilityTests
    {
        /// <summary>
        /// Verifies that the below Types don't have any DynamicallyAccessedMembers attributes,
        /// so we can safely call MakeGenericMethod on their methods.
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public static void VerifyMethodsCalledWithMakeGenericMethod()
        {
            Assembly linqExpressions = typeof(Expression).Assembly;
            Type[] types = new Type[]
            {
                linqExpressions.GetType("System.Dynamic.Utils.DelegateHelpers"),
                linqExpressions.GetType("System.Dynamic.UpdateDelegates"),
                linqExpressions.GetType("System.Runtime.CompilerServices.CallSiteOps"),
            };

            foreach (Type type in types)
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    Type[] genericTypes = method.GetGenericArguments();
                    if (genericTypes != null)
                    {
                        foreach (Type genericType in genericTypes)
                        {
                            Assert.Null(genericType.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
                        }
                    }
                }
            }
        }
    }
}
