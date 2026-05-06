// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;

namespace System.Composition.Hosting
{
    internal abstract class SinglePartExportDescriptorProvider : ExportDescriptorProvider
    {
        private readonly Type _contractType;
        private readonly string _contractName;

        protected SinglePartExportDescriptorProvider(Type contractType, string contractName, IDictionary<string, object> metadata)
        {
            _contractType = contractType;
            _contractName = contractName;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        protected bool IsSupportedContract(CompositionContract contract)
        {
            if (contract.ContractType != _contractType ||
                contract.ContractName != _contractName)
                return false;

            if (contract.MetadataConstraints != null)
            {
                var subsetOfConstraints = contract.MetadataConstraints.Where(c => Metadata.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => Metadata[c.Key]);
                var constrainedSubset = new CompositionContract(contract.ContractType, contract.ContractName,
                    subsetOfConstraints.Count == 0 ? null : subsetOfConstraints);

                if (!contract.Equals(constrainedSubset))
                    return false;
            }

            return true;
        }

        protected IDictionary<string, object> Metadata { get; }
    }
}
