// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    partial class ImportDefinitionFactory
    {
        private class DerivedContractBasedImportDefinition : ContractBasedImportDefinition
        {
            private readonly string _contractName;
            private readonly ImportCardinality _cardinality;
            private readonly bool _isRecomposable;
            private readonly bool _isPrerequisite;
            private readonly IEnumerable<KeyValuePair<string, Type>> _requiredMetadata;

            public DerivedContractBasedImportDefinition(string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
            {
                _contractName = contractName;
                _cardinality = cardinality;
                _isRecomposable = isRecomposable;
                _isPrerequisite = isPrerequisite;
                _requiredMetadata = requiredMetadata;
            }

            public override IEnumerable<KeyValuePair<string, Type>> RequiredMetadata
            {
                get { return _requiredMetadata ?? base.RequiredMetadata; }
            }

            public override ImportCardinality Cardinality
            {
                get { return _cardinality; }
            }

            public override bool IsPrerequisite
            {
                get { return _isPrerequisite; }
            }

            public override bool IsRecomposable
            {
                get { return _isRecomposable; }
            }

            public override string ContractName
            {
                get { return _contractName; }
            }
        }
    }
}
