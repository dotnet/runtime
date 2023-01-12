// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class SignatureHelperDynamicCodeNotSupported
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ThrowsWhenDynamicCodeNotSupported()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false.ToString());

            using RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                Assert.Throws<PlatformNotSupportedException>(() => SignatureHelper.GetFieldSigHelper(null));
                Assert.Throws<PlatformNotSupportedException>(() => SignatureHelper.GetLocalVarSigHelper());
                Assert.Throws<PlatformNotSupportedException>(() => SignatureHelper.GetMethodSigHelper(CallingConventions.Any, typeof(int)));

                // Mono always throws NotImplementedException - https://github.com/dotnet/runtime/issues/37794
                if (!PlatformDetection.IsMonoRuntime)
                {
                    Assert.Throws<PlatformNotSupportedException>(() => SignatureHelper.GetPropertySigHelper(null, typeof(string), new Type[] { typeof(string), typeof(int) }));
                }
            }, options);
        }
    }
}
