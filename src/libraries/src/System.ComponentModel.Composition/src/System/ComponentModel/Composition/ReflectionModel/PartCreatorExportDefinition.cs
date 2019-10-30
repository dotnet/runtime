// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class PartCreatorExportDefinition : ExportDefinition
    {
        private readonly ExportDefinition _productDefinition;
        private IDictionary<string, object> _metadata;

        public PartCreatorExportDefinition(ExportDefinition productDefinition)
            : base()
        {
            _productDefinition = productDefinition;
        }

        public override string ContractName
        {
            get
            {
                return CompositionConstants.PartCreatorContractName;
            }
        }

        public override IDictionary<string, object> Metadata
        {
            get
            {
                if (_metadata == null)
                {
                    var metadata = new Dictionary<string, object>(_productDefinition.Metadata);
                    metadata[CompositionConstants.ExportTypeIdentityMetadataName] = CompositionConstants.PartCreatorTypeIdentity;
                    metadata[CompositionConstants.ProductDefinitionMetadataName] = _productDefinition;

                    _metadata = metadata.AsReadOnly();
                }
                return _metadata;
            }
        }

        internal static bool IsProductConstraintSatisfiedBy(ImportDefinition productImportDefinition, ExportDefinition exportDefinition)
        {
            object productValue = null;
            if (exportDefinition.Metadata.TryGetValue(CompositionConstants.ProductDefinitionMetadataName, out productValue))
            {
                ExportDefinition productDefinition = productValue as ExportDefinition;

                if (productDefinition != null)
                {
                    return productImportDefinition.IsConstraintSatisfiedBy(productDefinition);
                }
            }

            return false;
        }
    }
}
