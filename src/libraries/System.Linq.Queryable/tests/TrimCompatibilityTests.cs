// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace System.Linq.Tests
{
    public class TrimCompatibilityTests
    {
        /// <summary>
        /// Verifies that all the Queryable methods contain a DynamicDependency
        /// to the corresponding Enumerable method. This ensures the ILLinker will
        /// preserve the corresponding Enumerable method when trimming.
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50712", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        public static void QueryableMethodsContainCorrectDynamicDependency()
        {
            IEnumerable<MethodInfo> dependentMethods =
                typeof(Queryable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.Name != "AsQueryable");

            foreach (MethodInfo method in dependentMethods)
            {
                DynamicDependencyAttribute dependency = method.GetCustomAttribute<DynamicDependencyAttribute>();
                Assert.NotNull(dependency);
                Assert.Equal(typeof(Enumerable), dependency.Type);

                int genericArgCount = 0;
                string methodName = dependency.MemberSignature;

                int genericSeparator = methodName.IndexOf('`');
                if (genericSeparator != -1)
                {
                    genericArgCount = int.Parse(methodName.Substring(genericSeparator + 1));
                    methodName = methodName.Substring(0, genericSeparator);
                }

                Assert.Equal(method.GetGenericArguments().Length, genericArgCount);
                Assert.Equal(method.Name, methodName);
            }
        }

        /// <summary>
        /// Verifies that all methods in CachedReflectionInfo that call MakeGenericMethod
        /// call it on a method that doesn't contain any trimming annotations (i.e. DynamicallyAccessedMembers).
        /// </summary>
        /// <remarks>
        /// This ensures it is safe to suppress IL2060:MakeGenericMethod warnings in the CachedReflectionInfo class.
        /// </remarks>
        [Fact]
        public static void CachedReflectionInfoMethodsNoAnnotations()
        {
            IEnumerable<MethodInfo> methods =
                typeof(Queryable).Assembly
                    .GetType("System.Linq.CachedReflectionInfo")
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                    .Where(m => m.GetParameters().Length > 0);

            // If you are adding a new method to this class, ensure the method meets these requirements
            Assert.Equal(131, methods.Count());
            foreach (MethodInfo method in methods)
            {
                ParameterInfo[] parameters = method.GetParameters();

                Type[] args = new Type[parameters.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = typeof(object);
                }

                MethodInfo resultMethodInfo = (MethodInfo)method.Invoke(null, args);
                Assert.True(resultMethodInfo.IsConstructedGenericMethod);
                MethodInfo originalGenericDefinition = resultMethodInfo.GetGenericMethodDefinition();

                EnsureNoTrimAnnotations(originalGenericDefinition);
            }
        }

        /// <summary>
        /// Verifies that all methods in Enumerable don't contain any trimming annotations (i.e. DynamicallyAccessedMembers).
        /// </summary>
        /// <remarks>
        /// This ensures it is safe to suppress IL2060:MakeGenericMethod warnings in EnumerableRewriter.FindEnumerableMethodForQueryable.
        /// </remarks>
        [Fact]
        public static void EnumerableMethodsNoAnnotations()
        {
            IEnumerable<MethodInfo> methods =
                typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m => m.IsGenericMethodDefinition);

            foreach (MethodInfo method in methods)
            {
                EnsureNoTrimAnnotations(method);
            }
        }

        private static void EnsureNoTrimAnnotations(MethodInfo method)
        {
            Type[] genericTypes = method.GetGenericArguments();
            foreach (Type genericType in genericTypes)
            {
                // The generic type should not have DynamicallyAccessedMembersAttribute on it.
                Assert.Null(genericType.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());

                // The generic type should not have a 'where new()' constraint since that will tell the trimmer to keep the ctor
                Assert.False(genericType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint));
            }
        }
    }
}
