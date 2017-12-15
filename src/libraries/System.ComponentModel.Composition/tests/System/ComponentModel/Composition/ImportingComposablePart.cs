// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.ComponentModel.Composition
{
    internal class ImportingComposablePart : ComposablePart
    {
        private readonly List<ImportDefinition> _importDefinitions = new List<ImportDefinition>();
        private readonly List<ExportDefinition> _exportDefinitions = new List<ExportDefinition>();
        private Dictionary<ImportDefinition, object> _importValues = new Dictionary<ImportDefinition, object>();

        public ImportingComposablePart(ImportCardinality cardinality, bool isRecomposable, params string[] contractNames)
            : this((string)null, cardinality, isRecomposable, contractNames)
        {
        }

        public ImportingComposablePart(string exportContractName, ImportCardinality cardinality, bool isRecomposable, params string[] contractNames)
        {
            if (exportContractName != null)
            {
                var definition = ExportDefinitionFactory.Create(exportContractName);

                _exportDefinitions.Add(definition);
            }

            foreach (string contractName in contractNames)
            {
                var definition = ImportDefinitionFactory.Create(contractName, 
                                                                cardinality,
                                                                isRecomposable,
                                                                false);

                _importDefinitions.Add(definition);
            }
        }

        public ImportingComposablePart(params ImportDefinition[] importDefintions)
        {
            _importDefinitions.AddRange(importDefintions);
        }

        public override IEnumerable<ExportDefinition> ExportDefinitions
        {
            get { return this._exportDefinitions; }
        }

        public override IEnumerable<ImportDefinition> ImportDefinitions
        {
            get { return this._importDefinitions; }
        }

        public override IDictionary<string, object> Metadata
        {
            get { return new Dictionary<string, object>(); }
        }

        public int ImportSatisfiedCount
        {
            get;
            private set;
        }

        public void ResetImportSatisfiedCount()
        {
            ImportSatisfiedCount = 0;
        }

        public object Value
        {
            get 
            {
                Assert.Equal(1, _importValues.Count);

                return _importValues.Values.First();
            }
        }

        public object GetImport(string contractName)
        {
            foreach (var pair in _importValues)
            {
                var definition = (ContractBasedImportDefinition)pair.Key;
                if (definition.ContractName == contractName)
                {
                    return pair.Value;
                }                
            }

            return null;
        }

        public object GetImport(ImportDefinition definition)
        {
            Assert.True(_importValues.ContainsKey(definition));
            return _importValues[definition];
        }

        public override object GetExportedValue(ExportDefinition definition)
        {
            throw new NotImplementedException();
        }

        public override void SetImport(ImportDefinition definition, IEnumerable<Export> exports)
        {
            Assert.True(_importDefinitions.Contains(definition));

            ImportSatisfiedCount++;

            _importValues[definition] = GetExportValue(exports);
        }

        public void ResetImport(ImportDefinition definition)
        {
            Assert.True(_importDefinitions.Contains(definition));
            _importValues[definition] = null;
        }

        private object GetExportValue(IEnumerable<Export> exports)
        {
            var exportedValues = exports.Select(export => export.Value);

            int count = exportedValues.Count();
            if (count == 0)
            {
                return null;
            }
            else if (count == 1)
            {
                return exportedValues.First();
            }

            return exportedValues.ToArray();
        }
    }

}
