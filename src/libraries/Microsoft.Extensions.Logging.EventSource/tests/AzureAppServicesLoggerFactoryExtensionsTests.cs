// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test
{
    public class LoggerFactoryExtensionsTests
{
        [Fact]
        public void BuilderExtensionAddsSingleSetOfServicesWhenCalledTwice()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddEventSourceLogger());
            var count = serviceCollection.Count;
            serviceCollection.AddLogging(builder => builder.AddEventSourceLogger());

            Assert.Equal(count, serviceCollection.Count);
        }
    }
}
