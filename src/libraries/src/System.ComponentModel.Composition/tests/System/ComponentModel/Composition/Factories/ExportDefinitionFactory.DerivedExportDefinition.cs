// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Factories
{
    partial class ExportDefinitionFactory
    {
        private class DerivedExportDefinition : ExportDefinition, ICompositionElement
        {
            private readonly string _contractName;
            private readonly IDictionary<string, object> _metadata;

            public DerivedExportDefinition(string contractName, IDictionary<string, object> metadata)
            {
                _contractName = contractName;
                _metadata = metadata ?? new Dictionary<string, object>();
            }

            public override string ContractName
            {
                get { return _contractName; }
            }

            public override IDictionary<string, object> Metadata
            {
                get { return _metadata; }
            }

            public string DisplayName
            {
                get { return base.ToString(); }
            }

            public ICompositionElement Origin
            {
                get { return null; }
            }
        }
    }
}
