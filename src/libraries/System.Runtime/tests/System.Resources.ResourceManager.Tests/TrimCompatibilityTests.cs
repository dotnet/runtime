// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
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
            Type type = typeof(ResourceReader);
            MethodInfo mi = type.GetMethod("CreateUntypedDelegate", BindingFlags.NonPublic | BindingFlags.Static);

            Type[] genericTypes = mi.GetGenericArguments();
            if (genericTypes != null)
            {
                foreach(Type genericType in genericTypes)
                {
                    // The generic type should not have DynamicallyAccessedMembersAttribute on it.
                    Assert.Null(genericType.GetCustomAttribute<DynamicallyAccessedMembersAttribute>());

                    // The generic type should not have a 'where new()' constraint since that will tell the trimmer to keep the ctor
                    Assert.False(genericType.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint));
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void VerifyFeatureSwitchGeneratesTheRightException()
        {
            var remoteInvokeOptions = new RemoteInvokeOptions();
            remoteInvokeOptions.RuntimeConfigurationOptions.Add("System.Resources.ResourceManager.AllowCustomResourceTypes", false);

            using var handle = RemoteExecutor.Invoke(() =>
            {
                ResourceManager rm = new ResourceManager("System.Resources.Tests.Resources.CustomReader", typeof(TrimCompatibilityTests).Assembly);
                Assert.Throws<NotSupportedException>(() => rm.GetObject("myGuid"));
            }, remoteInvokeOptions);
        }
    }
}
