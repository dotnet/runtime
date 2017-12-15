// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ExportDefinitionTests
    {
        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var definition = new DerivedExportDefinition();

            Assert.Empty(definition.Metadata);
        }

        [Fact]
        public void Constructor1_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var definition = new DerivedExportDefinition();

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor2_NullAsContractNameArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("contractName", () =>
            {
                new ExportDefinition((string)null, new Dictionary<string, object>());
            });
        }

        [Fact]
        public void Constructor2_EmptyStringAsContractNameArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>("contractName", () =>
            {
                new ExportDefinition(string.Empty, new Dictionary<string, object>());
            });
        }

        [Fact]
        public void Constructor2_ValueAsContractNameArgument_ShouldSetContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var definition = new ExportDefinition(e, new Dictionary<string, object>());

                Assert.Equal(e, definition.ContractName);
            }
        }

        [Fact]
        public void Constructor2_NullAsMetadataArgument_ShouldSetMetadataPropertyToEmptyDictionary()
        {
            var definition = new ExportDefinition("Contract", (IDictionary<string, object>)null); ;

            Assert.Empty(definition.Metadata);
        }

        [Fact]
        public void Constructor2_NullAsMetadataArgument_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var definition = new ExportDefinition("Contract", (IDictionary<string, object>)null);

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor2_WritableDictionaryAsMetadataArgument_ShouldSetMetadataPropertyToReadOnlyDictionary()
        {
            var definition = new ExportDefinition("Contract", new Dictionary<string, object>());

            ExceptionAssert.Throws<NotSupportedException>(() =>
            {
                definition.Metadata["Value"] = "Value";
            });
        }

        [Fact]
        public void Constructor2_DictionaryAsMetadataArgument_ShouldSetMetadataProperty()
        {
            var expectations = Expectations.GetMetadata();

            foreach (var e in expectations)
            {
                var definition = new ExportDefinition("Contract", e);

                EnumerableAssert.AreEqual(e, definition.Metadata);
            }
        }

        [Fact]
        public void ContractName_WhenNotOverridden_ShouldThrowNotImplemented()
        {
            var definition = new DerivedExportDefinition();

            ExceptionAssert.Throws<NotImplementedException>(() =>
            {
                var contractName = definition.ContractName;
            });
        }

        [Fact]
        public void ToString_WhenContractNameNotOverridden_ShouldThrowNotImplemented()
        {
            var definition = new DerivedExportDefinition();

            ExceptionAssert.Throws<NotImplementedException>(() =>
            {
                definition.ToString();
            });
        }

        [Fact]
        public void ToString_ShouldReturnContractNameProperty()
        {
            var expectations = Expectations.GetContractNames();

            foreach (var e in expectations)
            {
                var definition = new ExportDefinition(e, new Dictionary<string, object>());

                Assert.Equal(e, definition.ToString());
            }
        }

        [Fact]
        public void ToString_ShouldReturnOverriddenContractNameProperty()
        {
            var expectations = Expectations.GetContractNamesWithEmpty();
            
            foreach (var e in expectations)
            {
                var definition = new DerivedExportDefinition(() => e);

                Assert.Equal(e, definition.ToString());
            }
        }

        private class DerivedExportDefinition : ExportDefinition
        {
            private readonly Func<string> _contractNameGetter;

            public DerivedExportDefinition()
            {
            }

            public DerivedExportDefinition(Func<string> contractNameGetter)
            {
                _contractNameGetter = contractNameGetter;
            }

            public override string ContractName
            {
                get 
                {
                    if (_contractNameGetter != null)
                    {
                        return _contractNameGetter();
                    }

                    return base.ContractName;
                }
            }
        }
    }
}

