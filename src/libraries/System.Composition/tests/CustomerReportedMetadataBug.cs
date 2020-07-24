// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.Lightweight.UnitTests
{
    public class CustomerReportedMetadataBug
    {
        public class ServiceMetadata
        {
            public string Name { get; set; }
        }

        public interface IService
        {
        }

        [Export(typeof(IService)), ExportMetadata("Name", "1")]
        public class SampleService1 : IService
        {
            public SampleService1()
            {
            }
        }

        public class LooseImporter
        {
            [ImportMany]
            public IList<Lazy<IService, ServiceMetadata>> Services { get; set; }
        }

        [Fact]
        public void SampleServicesCorrectlyImported()
        {
            var container = new ContainerConfiguration()
                .WithPart<SampleService1>()
                .CreateContainer();

            var importer = new LooseImporter();
            container.SatisfyImports(importer);

            Assert.Equal(1, importer.Services.Count);
        }
    }
}
