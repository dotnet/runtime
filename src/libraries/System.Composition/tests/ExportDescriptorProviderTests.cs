// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.Lightweight.UnitTests
{
    public class ExportDescriptorProviderTests
    {
        public class DefaultObjectExportDescriptorProvider : ExportDescriptorProvider
        {
            private readonly CompositionContract _supportedContract = new CompositionContract(typeof(object));

            public static readonly object DefaultObject = "Hello, World!";

            public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor descriptorAccessor)
            {
                if (!contract.Equals(_supportedContract))
                    return NoExportDescriptors;

                var implementations = descriptorAccessor.ResolveDependencies("test for existing", contract, false);
                if (implementations.Any())
                    return NoExportDescriptors;

                return new[] { new ExportDescriptorPromise(contract, "test metadataProvider", false, NoDependencies, _ => ExportDescriptor.Create((c, o) => DefaultObject, NoMetadata)) };
            }
        }

        public class ExportsObject
        {
            [Export]
            public object AnObject { get { return "Not the default"; } }
        }

        [Fact]
        public void ProvidersCanLocateImplementationsOfAContractItSupports()
        {
            var container = new ContainerConfiguration()
                .WithProvider(new DefaultObjectExportDescriptorProvider())
                .WithPart<ExportsObject>()
                .CreateContainer();

            var o = container.GetExport<object>();
            Assert.NotEqual(DefaultObjectExportDescriptorProvider.DefaultObject, o);
        }

        [Fact]
        public void ProvidersCanDetectAbsenceOfAContractItSupports()
        {
            var container = new ContainerConfiguration()
                .WithProvider(new DefaultObjectExportDescriptorProvider())
                .CreateContainer();

            var o = container.GetExport<object>();
            Assert.Equal(DefaultObjectExportDescriptorProvider.DefaultObject, o);
        }
    }
}
