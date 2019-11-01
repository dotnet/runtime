// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Composition.Hosting.Core.Tests
{
    public class ExportDescriptorProviderTests
    {
        [Fact]
        public void NoExportDescriptor_Get_ReturnsExpected()
        {
            var subProvider = new SubProvider();
            Assert.Empty(subProvider.GetNoExportDescriptors());
        }

        [Fact]
        public void NoMetadata_Get_ReturnsExpected()
        {
            var subProvider = new SubProvider();
            Assert.Empty(subProvider.GetNoMetadata());
        }

        [Fact]
        public void NoDependencies_Get_ReturnsExpected()
        {
            var subProvider = new SubProvider();
            Assert.Empty(subProvider.GetNoDependencies()());
        }

        private class SubProvider : ExportDescriptorProvider
        {
            public IEnumerable<ExportDescriptorPromise> GetNoExportDescriptors() => NoExportDescriptors;
            public IDictionary<string, object> GetNoMetadata() => NoMetadata;
            public Func<IEnumerable<CompositionDependency>> GetNoDependencies() => NoDependencies;

            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                throw new NotImplementedException();
            }
        }
    }
}
