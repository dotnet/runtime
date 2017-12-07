// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal abstract class ReflectionImportDefinition : ContractBasedImportDefinition, ICompositionElement
    {
        private readonly ICompositionElement _origin;

        public ReflectionImportDefinition(
            string contractName, 
            string requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
            ImportCardinality cardinality, 
            bool isRecomposable, 
            bool isPrerequisite, 
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object> metadata,
            ICompositionElement origin)
            : base(contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, isPrerequisite, requiredCreationPolicy, metadata)
        {
            _origin = origin;
        }

        string ICompositionElement.DisplayName
        {
            get { return GetDisplayName(); }
        }

        ICompositionElement ICompositionElement.Origin
        {
            get { return _origin; }
        }

        public abstract ImportingItem ToImportingItem();

        protected abstract string GetDisplayName();
    }
}
