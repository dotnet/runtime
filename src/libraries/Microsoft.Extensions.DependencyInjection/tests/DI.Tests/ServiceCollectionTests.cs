// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionTests
    {
        [Fact]
        public void TestMakeReadOnly()
        {
            var serviceCollection = new ServiceCollection();
            var descriptor = new ServiceDescriptor(typeof(IFakeService), new FakeService());
            serviceCollection.Add(descriptor);

            serviceCollection.MakeReadOnly();

            var descriptor2 = new ServiceDescriptor(typeof(IFakeEveryService), new FakeService());

            Assert.Throws<InvalidOperationException>(() => serviceCollection[0] = descriptor2);
            Assert.Throws<InvalidOperationException>(() => serviceCollection.Clear());
            Assert.Throws<InvalidOperationException>(() => serviceCollection.Remove(descriptor));
            Assert.Throws<InvalidOperationException>(() => serviceCollection.Add(descriptor2));
            Assert.Throws<InvalidOperationException>(() => serviceCollection.Insert(0, descriptor2));
            Assert.Throws<InvalidOperationException>(() => serviceCollection.RemoveAt(0));

            Assert.True(serviceCollection.IsReadOnly);
            Assert.Equal(1, serviceCollection.Count);
            foreach (ServiceDescriptor d in serviceCollection)
            {
                Assert.Equal(descriptor, d);
            }
            Assert.Equal(descriptor, serviceCollection[0]);
            Assert.True(serviceCollection.Contains(descriptor));
            Assert.Equal(0, serviceCollection.IndexOf(descriptor));

            ServiceDescriptor[] copyArray = new ServiceDescriptor[1];
            serviceCollection.CopyTo(copyArray, 0);
            Assert.Equal(descriptor, copyArray[0]);

            // ensure MakeReadOnly can be called twice, and it is just ignored
            serviceCollection.MakeReadOnly();
            Assert.True(serviceCollection.IsReadOnly);
        }
    }
}
