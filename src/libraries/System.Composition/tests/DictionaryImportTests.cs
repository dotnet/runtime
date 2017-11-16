// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.Convention;
using System.Composition.UnitTests.Util;
using System.Composition.Runtime;
using Microsoft.Composition.Demos.ExtendedCollectionImports;
using Microsoft.Composition.Demos.ExtendedCollectionImports.Dictionaries;
using Xunit;

namespace System.Composition.UnitTests
{
    public interface IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", "A")]
    public class ValueA : IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", "B")]
    public class ValueB : IValued { }

    [Export(typeof(IValued))]
    public class ValueMissing : IValued { }

    [Export(typeof(IValued)), ExportMetadata("Value", 1)]
    public class NonStringValue : IValued { }

    [Export]
    public class Consumer
    {
        [Import, KeyByMetadata("Value")]
        public IDictionary<string, IValued> Values { get; set; }
    }

    [Export]
    public class LazyConsumer
    {
        [Import, KeyByMetadata("Value")]
        public IDictionary<string, Lazy<IValued>> Values { get; set; }
    }

    public class ConventionConsumer
    {
        public IDictionary<string, IValued> Values;

        public ConventionConsumer(
            [KeyByMetadata("Value")] IDictionary<string, IValued> values)
        {
            Values = values;
        }
    }
    public class DictionaryImportTests
    {
        private CompositionContext CreateContainer(params Type[] types)
        {
            var configuration = new ContainerConfiguration()
                .WithParts(types)
                .WithProvider(new DictionaryExportDescriptorProvider());

            return configuration.CreateContainer();
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DictionaryImportsKeyedByMetadata()
        {
            var container = CreateContainer(new[] { typeof(ValueA), typeof(ValueB), typeof(Consumer) });

            var consumer = container.GetExport<Consumer>();

            Assert.IsAssignableFrom(typeof(ValueA), consumer.Values["A"]);
            Assert.IsAssignableFrom(typeof(ValueB), consumer.Values["B"]);
            Assert.Equal(2, consumer.Values.Count());
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DictionaryImportsRecieveMetadataFromNestedAdapters()
        {
            var container = CreateContainer(new[] { typeof(ValueA), typeof(ValueB), typeof(LazyConsumer) });

            var consumer = container.GetExport<LazyConsumer>();

            var a = (Lazy<IValued>)consumer.Values["A"];
            Assert.False(a.IsValueCreated);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void WhenAMetadataKeyIsDuplicatedAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(ValueA), typeof(Consumer));
            var x = Assert.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.Equal("The metadata 'Value' cannot be used as a dictionary import key because the value 'A' is associated with exports from parts 'ValueA' and 'ValueA'.", x.Message);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void WhenAMetadataKeyIsMissingAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(ValueMissing), typeof(Consumer));
            var x = Assert.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.Equal("The metadata 'Value' cannot be used as a dictionary import key because it is missing from exports on part(s) 'ValueMissing'.", x.Message);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void WhenAMetadataValueIsOfTheWrongTypeAnInformativeExceptionIsThrown()
        {
            var container = CreateContainer(typeof(ValueA), typeof(NonStringValue), typeof(Consumer));
            var x = Assert.Throws<CompositionFailedException>(() => container.GetExport<Consumer>());
            Assert.Equal("The metadata 'Value' cannot be used as a dictionary import key of type 'String' because the value(s) supplied by 'NonStringValue' are of the wrong type.", x.Message);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DictionaryImportsCompatibleWithConventionBuilder()
        {
            var rb = new ConventionBuilder();
            rb.ForType<ConventionConsumer>().Export();
            var container = new ContainerConfiguration()
                .WithPart<ConventionConsumer>(rb)
                .WithParts(typeof(ValueA), typeof(ValueB))
                .WithProvider(new DictionaryExportDescriptorProvider())
                .CreateContainer();

            var consumer = container.GetExport<ConventionConsumer>();

            Assert.Equal(2, consumer.Values.Count());
        }
    }
}
