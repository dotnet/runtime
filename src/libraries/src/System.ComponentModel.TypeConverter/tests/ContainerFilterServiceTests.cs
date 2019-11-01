// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class ContainerFilterServiceTests
    {
        [Fact]
        public void FilterComponents_Invoke_ReturnsComponents()
        {
            var service = new SubContainerFilterService();
            Assert.Null(service.FilterComponents(null));

            var components = new ComponentCollection(new Component[] { new Component() });
            Assert.Same(components, service.FilterComponents(components));
        }

        private class SubContainerFilterService : ContainerFilterService
        {
        }
    }
}
