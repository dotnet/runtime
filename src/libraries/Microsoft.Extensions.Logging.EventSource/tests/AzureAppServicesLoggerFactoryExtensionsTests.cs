// // Copyright (c) .NET Foundation. All rights reserved.
// // Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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