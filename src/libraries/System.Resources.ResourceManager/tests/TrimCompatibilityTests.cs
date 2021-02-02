// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit;

namespace System.Resources.Tests
{
    public static class TrimCompatibilityTests
    {
        /// <summary>
        /// Verifies that ResourceReader.CreateUntypedDelegate doesn't have any DynamicallyAccessedMembers attributes,
        /// so we can safely call MakeGenericMethod on its methods.
        /// </summary>
        [Fact]
        public static void VerifyMethodsCalledWithMakeGenericMethod()
        {
            Assembly assembly = typeof(ResourceManager).Assembly;
            Type type = assembly.GetType("System.Resources.ResourceReader");
            MethodInfo mi = type.GetMethod("CreateUntypedDelegate", BindingFlags.NonPublic | BindingFlags.Static);

            Type[] genericTypes = mi.GetGenericArguments();
            if (genericTypes != null)
            {
                foreach(Type genericType in genericTypes)
                {
                    Assert.Null(genericType.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());
                }
            }
        }
    }
}
