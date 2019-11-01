// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using Xunit;

namespace System.ComponentModel.Composition.Hosting
{
    public class ScopeExtensionsTests
    {
        [Fact]
        public void Exports_Throws_OnNullPart()
        {
            ComposablePartDefinition part = null;
            string contractName = "Contract1";
            Assert.Throws<ArgumentNullException>("part", () =>
                {
                    part.Exports(contractName);
                });
        }

        [Fact]
        public void Exports_Throws_OnNullContractName()
        {
            ComposablePartDefinition part = typeof(PartExportingContract1).AsPart();
            string contractName = null;
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                part.Exports(contractName);
            });
        }

        [Fact]
        public void Exports()
        {
            ComposablePartDefinition part1 = typeof(PartExportingContract1).AsPart();
            ComposablePartDefinition part2 = typeof(PartExportingContract2).AsPart();

            Assert.True(part1.Exports("Contract1"));
            Assert.True(part2.Exports("Contract2"));

            Assert.False(part2.Exports("Contract1"));
            Assert.False(part1.Exports("Contract2"));
        }

        [Fact]
        public void Imports_Throws_OnNullPart()
        {
            ComposablePartDefinition part = null;
            string contractName = "Contract1";
            Assert.Throws<ArgumentNullException>("part", () =>
            {
                part.Imports(contractName);
            });
        }

        [Fact]
        public void Imports_Throws_OnNullContractName()
        {
            ComposablePartDefinition part = typeof(PartImportingContract1).AsPart();
            string contractName = null;
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                part.Imports(contractName);
            });
        }

        [Fact]
        public void Imports()
        {
            ComposablePartDefinition part1 = typeof(PartImportingContract1).AsPart();
            ComposablePartDefinition part2 = typeof(PartImportingContract2).AsPart();

            Assert.True(part1.Imports("Contract1"));
            Assert.True(part2.Imports("Contract2"));

            Assert.False(part2.Imports("Contract1"));
            Assert.False(part1.Imports("Contract2"));
        }

        [Fact]
        public void Imports_CardinalityIgnored_WhenNotSpecified()
        {
            ComposablePartDefinition part1 = typeof(PartImportingContract1).AsPart();
            ComposablePartDefinition part1Multiple = typeof(PartImportingContract1Multiple).AsPart();
            ComposablePartDefinition part1Optional = typeof(PartImportingContract1Optionally).AsPart();

            Assert.True(part1.Imports("Contract1"));
            Assert.True(part1Optional.Imports("Contract1"));
            Assert.True(part1Multiple.Imports("Contract1"));
        }

        [Fact]
        public void Imports_CardinalityNotIgnored_WhenSpecified()
        {
            ComposablePartDefinition part1 = typeof(PartImportingContract1).AsPart();
            ComposablePartDefinition part1Multiple = typeof(PartImportingContract1Multiple).AsPart();
            ComposablePartDefinition part1Optional = typeof(PartImportingContract1Optionally).AsPart();

            Assert.True(part1.Imports("Contract1", ImportCardinality.ExactlyOne));
            Assert.False(part1.Imports("Contract1", ImportCardinality.ZeroOrMore));
            Assert.False(part1.Imports("Contract1", ImportCardinality.ZeroOrOne));

            Assert.False(part1Multiple.Imports("Contract1", ImportCardinality.ExactlyOne));
            Assert.True(part1Multiple.Imports("Contract1", ImportCardinality.ZeroOrMore));
            Assert.False(part1Multiple.Imports("Contract1", ImportCardinality.ZeroOrOne));

            Assert.False(part1Optional.Imports("Contract1", ImportCardinality.ExactlyOne));
            Assert.False(part1Optional.Imports("Contract1", ImportCardinality.ZeroOrMore));
            Assert.True(part1Optional.Imports("Contract1", ImportCardinality.ZeroOrOne));

        }

        [Fact]
        public void ContainsMetadataWithKey_Throws_OnNullPart()
        {
            ComposablePartDefinition part = null;
            Assert.Throws<ArgumentNullException>("part", () =>
            {
                part.ContainsPartMetadataWithKey("Name");
            });
        }

        [Fact]
        public void ContainsMetadataWithKey_Throws_OnNullKey()
        {
            ComposablePartDefinition part = typeof(PartImportingContract1).AsPart();
            Assert.Throws<ArgumentNullException>("key", () =>
            {
                part.ContainsPartMetadataWithKey(null);
            });
        }

        [Fact]
        public void ContainsMetadataWithKey()
        {
            ComposablePartDefinition part1 = typeof(PartWithMetadada).AsPart();

            Assert.True(part1.ContainsPartMetadataWithKey("Name"));
            Assert.True(part1.ContainsPartMetadataWithKey("Spores"));
            Assert.True(part1.ContainsPartMetadataWithKey("Adds"));

            Assert.False(part1.ContainsPartMetadataWithKey("Description"));
        }

        [Fact]
        public void ContainsMetadata_Throws_OnNullPart()
        {
            ComposablePartDefinition part = null;
            Assert.Throws<ArgumentNullException>("part", () =>
            {
                part.ContainsPartMetadata("Name", "Festergut");
            });
        }

        [Fact]
        public void ContainsMetadata_Throws_OnNullKey()
        {
            ComposablePartDefinition part = typeof(PartImportingContract1).AsPart();
            Assert.Throws<ArgumentNullException>("key", () =>
            {
                part.ContainsPartMetadata(null, "Festergut");
            });
        }

        [Fact]
        public void ContainsMetadata()
        {
            ComposablePartDefinition part1 = typeof(PartWithMetadada).AsPart();

            Assert.True(part1.ContainsPartMetadata("Name", "Festergut"));
            Assert.False(part1.ContainsPartMetadata("Name", "Rotface"));
            Assert.False(part1.ContainsPartMetadata<string>("Name", null));

            Assert.True(part1.ContainsPartMetadata("Spores", 3));
            Assert.False(part1.ContainsPartMetadata("Spores", 3L));
            Assert.False(part1.ContainsPartMetadata("Spores", 5));
            Assert.False(part1.ContainsPartMetadata<string>("Spores", null));

            Assert.True(part1.ContainsPartMetadata<string>("Adds", null));
            Assert.False(part1.ContainsPartMetadata("Adds", "Ooze"));

            Assert.False(part1.ContainsPartMetadata("Ability", "Pungent Blight"));
        }

        [Export("Contract1")]
        public class PartExportingContract1
        {
        }

        [Export("Contract2")]
        public class PartExportingContract2
        {
        }

        public class PartImportingContract1
        {
            [Import("Contract1")]
            public object import;
        }

        public class PartImportingContract2
        {
            [Import("Contract2")]
            public object import;
        }

        public class PartImportingContract1Optionally
        {
            [Import("Contract1", AllowDefault = true)]
            public object import;
        }

        public class PartImportingContract1Multiple
        {
            [ImportMany("Contract1")]
            public object import;
        }

        [PartMetadata("Name", "Festergut")]
        [PartMetadata("Spores", 3)]
        [PartMetadata("Adds", null)]
        [Export("Contract1")]
        public class PartWithMetadada
        {
        }
    }
}
