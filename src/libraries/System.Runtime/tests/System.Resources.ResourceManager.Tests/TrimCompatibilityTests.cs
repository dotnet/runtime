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
