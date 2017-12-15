// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ContractBasedImportDefinitionTests
    {
        [Fact]
        public void Constructor1_ShouldSetRequiredMetadataPropertyToEmptyEnumerable()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.Empty(definition.RequiredMetadata);
        }

        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToEmptyEnumerable()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.Empty(definition.Metadata);
        }

        [Fact]
        public void Constructor1_ShouldSetCardinalityPropertyToExactlyOne()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.Equal(ImportCardinality.ExactlyOne, definition.Cardinality);
        }

        [Fact]
        public void Constructor1_ShouldSetIsPrerequisitePropertyToTrue()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.True(definition.IsPrerequisite);
        }

        [Fact]
        public void Constructor1_ShouldSetIsRecomposablePropertyToFalse()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.False(definition.IsRecomposable);
        }

        [Fact]
        public void Constructor1_ShouldSetRequiredCreationPolicyToAny()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.Equal(CreationPolicy.Any, definition.RequiredCreationPolicy);
        }

        [Fact]
        public void Constructor1_ShouldSetRequiredTypeIdentityToNull()
        {
            var definition = new NoOverridesContractBasedImportDefinition();

            Assert.Null(definition.RequiredTypeIdentity);
        }

        [Fact]
        public void Constructor2_NullAsContractNameArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                new ContractBasedImportDefinition((string)null, (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);
            });
        }

        [Fact]
        public void Constructor2_EmptyStringAsContractNameArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("contractName", () =>
            {
                new ContractBasedImportDefinition("", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);
            });
        }

        [Fact]
        public void RequiredMetadata_ArrayWithNullKeyAsRequiredMetadataArgument_ShouldThrowInvalidOperation()
        {
            var requiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>(null, typeof(object)) };

            var import = new ContractBasedImportDefinition("requiredMetadata", (string)null, requiredMetadata, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                var m = import.RequiredMetadata;
            });
        }

        [Fact]
        public void RequiredMetadata_ArrayWithNullValueAsRequiredMetadataArgument_ShouldThrowInvalidOperation()
        {
            var requiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("key", null) };
            var import = new ContractBasedImportDefinition("requiredMetadata", (string)null, requiredMetadata, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);

            ExceptionAssert.Throws<InvalidOperationException>(() =>
            {
                var m = import.RequiredMetadata;
            });
        }

        [Fact]
        public void Constructor2_NullAsMetadataArgument_ShouldSetRequiredMetadataToEmptyEnumerable()
        {
            var requiredMetadata = new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("key", null) };
            var definition = new ContractBasedImportDefinition("metadata", (string)null, requiredMetadata, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any, null);

            Assert.Empty(definition.Metadata);
        }

        [Fact]
        public void Constructor2_NullAsRequiredMetadataArgument_ShouldSetRequiredMetadataToEmptyEnumerable()
        {
            var definition = new ContractBasedImportDefinition("requiredMetadata", (string)null, (IEnumerable<KeyValuePair<string, Type>>)null, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);

            Assert.Empty(definition.RequiredMetadata);
        }

        [Fact]
        public void Constructor2_OutOfRangeValueAsCardinalityArgument_ShouldThrowArgument()
        {
            var expectations = Expectations.GetInvalidEnumValues<ImportCardinality>();

            foreach (var e in expectations)
            {
                Assert.Throws<ArgumentException>("cardinality", () =>
                {
                    new ContractBasedImportDefinition("ContractName", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), e, false, false, CreationPolicy.Any);
                });
            }
        }

        [Fact]
        public void Constructor2_ValueAsCardinalityArgument_ShouldSetCardinalityProperty()
        {
            var expectations = Expectations.GetEnumValues<ImportCardinality>();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), e, false, false, CreationPolicy.Any);

                Assert.Equal(e, definition.Cardinality);
            }
        }

        [Fact]
        public void Constructor2_ValueAsContractNameArgument_ShouldSetContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition(e, (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);

                Assert.Equal(e, definition.ContractName);
            }
        }

        [Fact]
        public void Constructor2_ValueAsRequiredMetadataArgument_ShouldSetRequiredMetadataProperty()
        {
            var expectations = Expectations.GetRequiredMetadataWithEmpty();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, e, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any);

                Assert.Equal(e, definition.RequiredMetadata);
            }
        }

        [Fact]
        public void Constructor2_ValueAsMetadataArgument_ShouldSetMetadataProperty()
        {
            var expectations = Expectations.GetMetadata();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, null, ImportCardinality.ExactlyOne, false, false, CreationPolicy.Any, e);

                Assert.Equal(e, definition.Metadata);
            }
        }

        [Fact]
        public void Constructor2_ValueAsIsRecomposableArgument_ShouldSetIsRecomposableProperty()
        {
            var expectations = Expectations.GetBooleans();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, e, false, CreationPolicy.Any);

                Assert.Equal(e, definition.IsRecomposable);
            }
        }

        [Fact]
        public void Constructor2_ValueAsIsPrerequisiteArgument_ShouldSetIsPrerequisiteProperty()
        {
            var expectations = Expectations.GetBooleans();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, false, e, CreationPolicy.Any);

                Assert.Equal(e, definition.IsPrerequisite);
            }
        }

        [Fact]
        public void Constructor2_ShouldSetRequiredCreationPolicyToAny()
        {
            var expectations = Expectations.GetEnumValues<CreationPolicy>();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, Enumerable.Empty<KeyValuePair<string, Type>>(), ImportCardinality.ExactlyOne, false, false, e);

                Assert.Equal(e, definition.RequiredCreationPolicy);
            }
        }

        [Fact]
        public void Constraint_ShouldIncludeContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition(e, (string)null, (IEnumerable<KeyValuePair<string, Type>>)null, ImportCardinality.ExactlyOne, true, false, CreationPolicy.Any);

                ConstraintAssert.Contains(definition.Constraint, e);
            }
        }

        [Fact]
        public void Constraint_ShouldIncludeRequiredMetadataProperty()
        {
            var expectations = Expectations.GetRequiredMetadataWithEmpty();

            foreach (var e in expectations)
            {
                var definition = new ContractBasedImportDefinition("ContractName", (string)null, e, ImportCardinality.ExactlyOne, true, false, CreationPolicy.Any);

                ConstraintAssert.Contains(definition.Constraint, "ContractName", e);
            }
        }

        [Fact]
        public void Constraint_ShouldIncludeOverriddenContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var definition = new DerivedContractBasedImportDefinition(e);

                ConstraintAssert.Contains(definition.Constraint, e);
            }
        }

        [Fact]
        public void Constraint_ShouldIncludeOverriddenRequiredMetadata()
        {
            var expectations = Expectations.GetRequiredMetadataWithEmpty();

            foreach (var e in expectations)
            {
                var definition = new DerivedContractBasedImportDefinition("ContractName", e);

                ConstraintAssert.Contains(definition.Constraint, "ContractName", e);
            }
        }

        [Fact]
        public void IsConstraintSatisfiedBy_ContractNameMatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("ContractName", "ContractName", new string[0], new Type[0]);
            Assert.True(import.IsConstraintSatisfiedBy(export));
        }

        [Fact]
        public void IsConstraintSatisfiedBy_ContractNameMismatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("NonContractName", "ContractName", new string[0], new Type[0]);
            Assert.False(import.IsConstraintSatisfiedBy(export));
        }

        [Fact]
        public void IsConstraintSatisfiedBy_TypeIdentityMismatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("ContractName", "NonContractName", new string[0], new Type[0]);
            Assert.False(import.IsConstraintSatisfiedBy(export));
        }

        [Fact]
        public void IsConstraintSatisfiedBy_MetadataMatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("ContractName", "ContractName", new string[]{"Int", "String", "Type"}, new Type[]{typeof(int), typeof(string), typeof(Type)});
            Assert.True(import.IsConstraintSatisfiedBy(export));
        }

        [Fact]
        public void IsConstraintSatisfiedBy_MetadataKeyMismatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("ContractName", "ContractName", new string[] { "Int", "String1", "Type" }, new Type[] { typeof(int), typeof(string), typeof(Type) });
            Assert.False(import.IsConstraintSatisfiedBy(export));
        }

        [Fact]
        public void IsConstraintSatisfiedBy_MetadataTypeMatch()
        {
            var export = CreateSimpleExport();
            var import = CreateSimpleImport("ContractName", "ContractName", new string[] { "Int", "String", "Type" }, new Type[] { typeof(int), typeof(string), typeof(int) });
            Assert.False(import.IsConstraintSatisfiedBy(export));
        }

        private static ExportDefinition CreateSimpleExport()
        {
            var metadata = new Dictionary<string, object>();
            metadata.Add("Int", 42);
            metadata.Add("String", "42");
            metadata.Add("Type", typeof(string));
            metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, "ContractName");
            return new ExportDefinition("ContractName", metadata);
        }

        private static ContractBasedImportDefinition CreateSimpleImport(string contractName, string typeIdentity, string[] metadataKeys, Type[] metadataTypes)
        {
            Dictionary<string, Type> requiredMetadata = new Dictionary<string, Type>();
            Assert.Equal(metadataKeys.Length, metadataTypes.Length);
            for(int i=0; i< metadataKeys.Length; i++)
            {
                requiredMetadata[metadataKeys[i]] = metadataTypes[i];
            }
            return new ContractBasedImportDefinition(contractName, typeIdentity, requiredMetadata, ImportCardinality.ZeroOrMore, false, false, CreationPolicy.Any);
            
        }

        private class NoOverridesContractBasedImportDefinition : ContractBasedImportDefinition
        {
            public NoOverridesContractBasedImportDefinition()
            {
            }
        }

        private class DerivedContractBasedImportDefinition : ContractBasedImportDefinition
        {
            private readonly string _contractName;
            private readonly IEnumerable<KeyValuePair<string, Type>> _requiredMetadata;

            public DerivedContractBasedImportDefinition(string contractName)
            {
                _contractName = contractName;
            }

            public DerivedContractBasedImportDefinition(string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
            {
                _contractName = contractName;
                _requiredMetadata = requiredMetadata;
            }

            public override string ContractName
            {
                get { return _contractName; }
            }

            public override IEnumerable<KeyValuePair<string, Type>> RequiredMetadata
            {
                get { return _requiredMetadata; }
            }
        }
    }
}

