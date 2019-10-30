// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportTests
    {
        [Fact]
        public void Constructor1_ShouldNotThrow()
        {
            new NoOverridesExport();
        }

        [Fact]
        public void Constructor2_NullAsExportedValueGetterArgument_ShouldThrowArgumentNull()
        {
            var definition = ExportDefinitionFactory.Create();

            Assert.Throws<ArgumentNullException>("exportedValueGetter", () =>
            {
                new Export(definition, (Func<object>)null);
            });
        }

        [Fact]
        public void Constructor3_NullAsExportedValueGetterArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("exportedValueGetter", () =>
            {
                new Export("ContractName", (Func<object>)null);
            });
        }

        [Fact]
        public void Constructor4_NullAsExportedValueGetterArgument_ShouldThrowArgumentNull()
        {
            var metadata = new Dictionary<string, object>();

            Assert.Throws<ArgumentNullException>("exportedValueGetter", () =>
            {
                new Export("ContractName", metadata, (Func<object>)null);
            });
        }

        [Fact]
        public void Constructor2_NullAsDefinitionArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                new Export((ExportDefinition)null, () => null);
            });
        }

        [Fact]
        public void Constructor2_DefinitionAsDefinitionArgument_ShouldSetDefinitionProperty()
        {
            var definition = ExportDefinitionFactory.Create();

            var export = new Export(definition, () => null);

            Assert.Same(definition, export.Definition);
        }

        [Fact]
        public void Constructor3_NullAsContractNameArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                new Export((string)null, () => null);
            });
        }

        [Fact]
        public void Constructor4_NullAsContractNameArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                new Export((string)null, new Dictionary<string, object>(), () => null);
            });
        }

        [Fact]
        public void Constructor3_EmptyStringAsContractNameArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("contractName", () =>
            {
                new Export(string.Empty, () => null);
            });
        }

        [Fact]
        public void Constructor4_EmptyStringAsContractNameArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("contractName", () =>
            {
                new Export(string.Empty, new Dictionary<string, object>(), () => null);
            });
        }

        [Fact]
        public void Constructor3_ValueAsContractNameArgument_ShouldSetDefinitionContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var export = new Export(e, () => null);

                Assert.Equal(e, export.Definition.ContractName);
            }
        }

        [Fact]
        public void Constructor4_ValueAsContractNameArgument_ShouldSetDefinitionContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var export = new Export(e, new Dictionary<string, object>(), () => null);

                Assert.Equal(e, export.Definition.ContractName);
            }
        }

        [Fact]
        public void Constructor3_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var export = new Export("ContractName", () => null);

            Assert.Empty(export.Metadata);
        }

        [Fact]
        public void Constructor4_NullAsMetadataArgument_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var export = new Export("ContractName", (IDictionary<string, object>)null, () => null);

            Assert.Empty(export.Metadata);
        }

        [Fact]
        public void Constructor3_NullAsMetadataArgument_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_NullAsMetadataArgument_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", (IDictionary<string, object>)null, () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_WritableDictionaryAsMetadataArgument_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", new Dictionary<string, object>(), () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_DictionaryAsMetadataArgument_ShouldSetMetadataProperty()
        {
            var expectations = Expectations.GetMetadata();

            foreach (var e in expectations)
            {
                var export = new Export("ContractName", e, () => null);

                EnumerableAssert.AreEqual(e, export.Metadata);
            }
        }

        [Fact]
        public void Constructor3_ShouldSetDefinitionMetadataPropertyToEmptyDictionary()
        {
            var export = new Export("ContractName", () => null);

            Assert.Empty(export.Definition.Metadata);
        }

        [Fact]
        public void Constructor4_NullAsMetadataArgument_ShouldSetDefinitionMetadataPropertyToEmptyDictionary()
        {
            var export = new Export("ContractName", (IDictionary<string, object>)null, () => null);

            Assert.Empty(export.Definition.Metadata);
        }

        [Fact]
        public void Constructor3_ShouldSetDefinitionMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_NullAsMetadataArgument_ShouldSetDefinitionMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", (IDictionary<string, object>)null, () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_WritableDictionaryAsMetadataArgument_ShouldSetDefinitionMetadataPropertyToReadOnlyDictionary()
        {
            var export = new Export("ContractName", new Dictionary<string, object>(), () => null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                export.Definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor4_DictionaryAsMetadataArgument_ShouldSetDefinitionMetadataProperty()
        {
            var expectations = Expectations.GetMetadata();

            foreach (var e in expectations)
            {
                var export = new Export("ContractName", e, () => null);

                EnumerableAssert.AreEqual(e, export.Definition.Metadata);
            }
        }

        [Fact]
        public void Constructor2_FuncReturningAStringAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var definition = ExportDefinitionFactory.Create();

            var export = new Export(definition, () => "Value");

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void Constructor3_FuncReturningAStringAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var export = new Export("ContractName", () => "Value");

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void Constructor4_FuncReturningAStringAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var export = new Export("ContractName", new Dictionary<string, object>(), () => "Value");

            Assert.Equal("Value", export.Value);
        }

        [Fact]
        public void Constructor2_FuncReturningNullAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var definition = ExportDefinitionFactory.Create();

            var export = new Export(definition, () => null);

            Assert.Null(export.Value);
        }

        [Fact]
        public void Constructor3_FuncReturningNullAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var export = new Export("ContractName", () => null);

            Assert.Null(export.Value);
        }

        [Fact]
        public void Constructor4_FuncReturningNullAsExportedValueGetter_ShouldBeReturnedByGetExportedValue()
        {
            var export = new Export("ContractName", new Dictionary<string, object>(), () => null);

            Assert.Null(export.Value);
        }

        [Fact]
        public void Metadata_DerivedExportDefinition_ShouldReturnDefinitionMetadata()
        {
            var expectations = Expectations.GetMetadata();

            foreach (var e in expectations)
            {
                var definition = ExportDefinitionFactory.Create("ContractName", e);

                var export = new DerivedExport(definition);

                EnumerableAssert.AreEqual(e, export.Metadata);
            }
        }

        [Fact]
        public void Definition_WhenNotOverridden_ShouldThrowNotImplemented()
        {
            var export = new NoOverridesExport();

            ExceptionAssert.Throws<NotImplementedException>(() =>
            {
                var definition = export.Definition;
            });
        }

        [Fact]
        public void Metadata_WhenDefinitionNotOverridden_ShouldThrowNotImplemented()
        {
            var export = new NoOverridesExport();

            ExceptionAssert.Throws<NotImplementedException>(() =>
            {
                var definition = export.Metadata;
            });
        }

        [Fact]
        public void GetExportedValue_WhenGetExportedValueCoreNotOverridden_ShouldThrowNotImplemented()
        {
            var export = new NoOverridesExport();

            ExceptionAssert.Throws<NotImplementedException>(() =>
            {
                var value = export.Value;
            });
        }

        [Fact]
        public void GetExportedValue_ShouldCacheExportedValueGetter()
        {
            int count = 0;

            var export = new Export("ContractName", () =>
                {
                    count++;
                    return count;
                });

            Assert.Equal(1, export.Value);
            Assert.Equal(1, export.Value);
            Assert.Equal(1, export.Value);
        }

        [Fact]
        public void GetExportedValue_ShouldCacheOverrideGetExportedValueCore()
        {
            int count = 0;

            var export = new DerivedExport(() =>
            {
                count++;
                return count;
            });

            Assert.Equal(1, export.Value);
            Assert.Equal(1, export.Value);
            Assert.Equal(1, export.Value);
        }

        [Fact]
        public void GetExportedValue_ThrowingFuncAsObjectGetterArgument_ShouldThrow()
        {
            var exceptionToThrow = new Exception();

            var export = new Export("ContractName", new Dictionary<string, object>(), () =>
            {
                throw exceptionToThrow;
            });

            ExceptionAssert.Throws(exceptionToThrow, RetryMode.Retry, () =>
            {
                var value = export.Value;
            });
        }

        private class NoOverridesExport : Export
        {
        }

        private class DerivedExport : Export
        {
            private readonly Func<object> _exportedValueGetter;
            private readonly ExportDefinition _definition;

            public DerivedExport(ExportDefinition definition)
            {
                _definition = definition;
            }

            public DerivedExport(Func<object> exportedValueGetter)
            {
                _exportedValueGetter = exportedValueGetter;
            }

            public override ExportDefinition Definition
            {
                get { return _definition; }
            }

            protected override object GetExportedValueCore()
            {
                return _exportedValueGetter();
            }
        }
    }
}
