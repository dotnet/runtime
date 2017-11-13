// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Composition.Demos.ExtendedCollectionImports;
using Microsoft.Composition.Demos.ExtendedCollectionImports.OrderedCollections;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ImportOrderingTests
    {
        public interface IItem
        {
        }

        [Shared, Export(typeof(IItem)), ExportMetadata("Order", 1)]
        public class Item1 : IItem { }

        [Shared, Export(typeof(IItem)), ExportMetadata("Order", 4)]
        public class Item4 : IItem { }

        [Shared, Export(typeof(IItem)), ExportMetadata("Order", 2)]
        public class Item2 : IItem { }

        [Shared, Export(typeof(IItem)), ExportMetadata("Order", 3)]
        public class Item3 : IItem { }

        [Export(typeof(IItem))]
        public class ItemWithoutOrder : IItem { }

        [Export]
        public class HasImportedItems
        {
            [OrderedImportMany("Order")]
            public IItem[] OrderedItems { get; set; }

            [ImportMany]
            public IItem[] UnorderedItems { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void CollectionsImportedWithAnOrderingAttributeComeInOrder()
        {
            var container = CreateExtendedContainer(typeof(HasImportedItems), typeof(Item1), typeof(Item4), typeof(Item2), typeof(Item3));

            var hasImportedItems = container.GetExport<HasImportedItems>();

            var ordered = hasImportedItems.UnorderedItems.OrderBy(i => i.GetType().Name).ToArray();

            Assert.Equal(ordered, hasImportedItems.OrderedItems);
            Assert.NotEqual(ordered, hasImportedItems.UnorderedItems);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void IfAnItemIsMissingMetadataAnInformativeExceptionIsThrown()
        {
            var container = CreateExtendedContainer(typeof(HasImportedItems), typeof(Item1), typeof(ItemWithoutOrder));
            var x = Assert.Throws<CompositionFailedException>(() => container.GetExport<HasImportedItems>());
            Assert.Equal("The metadata 'Order' cannot be used for ordering because it is missing from exports on part(s) 'ItemWithoutOrder'.", x.Message);
        }

        private CompositionContext CreateExtendedContainer(params Type[] partTypes)
        {
            return new ContainerConfiguration()
                .WithParts(partTypes)
                .WithProvider(new OrderedImportManyExportDescriptorProvider())
                .CreateContainer();
        }
    }
}
