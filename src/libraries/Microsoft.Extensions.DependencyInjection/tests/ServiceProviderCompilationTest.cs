﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ServiceProviderCompilationTest
    {
        [Theory]
        [InlineData(ServiceProviderMode.Dynamic, typeof(I999))]
        [InlineData(ServiceProviderMode.Runtime, typeof(I999))]
        [InlineData(ServiceProviderMode.ILEmit, typeof(I999))]
        [InlineData(ServiceProviderMode.Expressions, typeof(I999))]
        private async Task CompilesInLimitedStackSpace(ServiceProviderMode mode, Type serviceType)
        {
            // Arrange
            var stackSize = 256 * 1024;
            var serviceCollection = new ServiceCollection();
            CompilationTestDataProvider.Register(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider(new ServiceProviderOptions { Mode = mode });

            // Act + Assert

            var tsc = new TaskCompletionSource<object>();
            var thread = new Thread(() =>
                {
                    try
                    {
                        object service = null;
                        for (int i = 0; i < 10; i++)
                        {
                            service = serviceProvider.GetService(serviceType);
                        }
                        tsc.SetResult(service);
                    }
                    catch (Exception ex)
                    {
                        tsc.SetException(ex);
                    }
                }, stackSize);

            thread.Start();
            thread.Join();
            await tsc.Task;
        }
    }
}
