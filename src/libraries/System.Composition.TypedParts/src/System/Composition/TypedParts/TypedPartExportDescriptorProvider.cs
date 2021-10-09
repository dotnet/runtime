// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Convention;
using System.Composition.Hosting.Core;
using System.Composition.TypedParts.ActivationFeatures;
using System.Composition.TypedParts.Discovery;
using System.Linq;
using System.Reflection;

namespace System.Composition.TypedParts
{
    internal sealed class TypedPartExportDescriptorProvider : ExportDescriptorProvider
    {
        private readonly IDictionary<CompositionContract, ICollection<DiscoveredExport>> _discoveredParts = new Dictionary<CompositionContract, ICollection<DiscoveredExport>>();

        public TypedPartExportDescriptorProvider(IEnumerable<Type> types, AttributedModelProvider attributeContext)
        {
            var activationFeatures = CreateActivationFeatures(attributeContext);
            var typeInspector = new TypeInspector(attributeContext, activationFeatures);

            foreach (var type in types)
            {
                DiscoveredPart part;
                if (typeInspector.InspectTypeForPart(type.GetTypeInfo(), out part))
                {
                    AddDiscoveredPart(part);
                }
            }
        }

        private void AddDiscoveredPart(DiscoveredPart part)
        {
            foreach (var export in part.DiscoveredExports)
            {
                AddDiscoveredExport(export);
            }
        }

        private void AddDiscoveredExport(DiscoveredExport export, CompositionContract contract = null)
        {
            var actualContract = contract ?? export.Contract;

            ICollection<DiscoveredExport> forKey;
            if (!_discoveredParts.TryGetValue(actualContract, out forKey))
            {
                forKey = new List<DiscoveredExport>();
                _discoveredParts.Add(actualContract, forKey);
            }

            forKey.Add(export);
        }

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            DiscoverGenericParts(contract);
            DiscoverConstrainedParts(contract);

            ICollection<DiscoveredExport> forKey;
            if (!_discoveredParts.TryGetValue(contract, out forKey))
                return NoExportDescriptors;

            // Exports with metadata may be matched via metadata constraints.
            // It should be possible to do this more aggressively by changing the way
            // exports are stored.
            if (!forKey.Any(x => x.Metadata.Any()))
            {
                // Allow some garbage to be collected
                _discoveredParts.Remove(contract);
            }

            return forKey.Select(de => de.GetExportDescriptorPromise(contract, definitionAccessor)).ToArray();
        }

        // If the contract has metadata constraints, look for exports with matching metadata.
        private void DiscoverConstrainedParts(CompositionContract contract)
        {
            if (contract.MetadataConstraints != null)
            {
                var unconstrained = new CompositionContract(contract.ContractType, contract.ContractName);
                DiscoverGenericParts(unconstrained);

                ICollection<DiscoveredExport> forKey;
                if (_discoveredParts.TryGetValue(unconstrained, out forKey))
                {
                    foreach (var export in forKey)
                    {
                        var subsettedConstraints = contract.MetadataConstraints.Where(c => export.Metadata.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => export.Metadata[c.Key]);
                        if (subsettedConstraints.Count != 0)
                        {
                            var constrainedSubset = new CompositionContract(unconstrained.ContractType, unconstrained.ContractName, subsettedConstraints);

                            if (constrainedSubset.Equals(contract))
                                AddDiscoveredExport(export, contract);
                        }
                    }
                }
            }
        }

        // If the contract is a closed generic, look for open generics
        // that close it.
        private void DiscoverGenericParts(CompositionContract contract)
        {
            if (!contract.ContractType.IsConstructedGenericType)
                return;

            var gtd = contract.ContractType.GetGenericTypeDefinition();
            var openGenericContract = contract.ChangeType(gtd);
            ICollection<DiscoveredExport> openGenericParts;
            if (!_discoveredParts.TryGetValue(openGenericContract, out openGenericParts))
                return;

            var typeArguments = contract.ContractType.GenericTypeArguments;
            foreach (var open in openGenericParts)
            {
                DiscoveredPart closed;
                if (open.Part.TryCloseGenericPart(typeArguments, out closed))
                    AddDiscoveredPart(closed);
            }
        }

        private static ActivationFeature[] CreateActivationFeatures(AttributedModelProvider attributeContext)
        {
            return new ActivationFeature[] {
                new DisposalFeature(),
                new PropertyInjectionFeature(attributeContext),
                new OnImportsSatisfiedFeature(attributeContext),
                new LifetimeFeature(),
            };
        }

        internal static ActivationFeature[] DebugGetActivationFeatures(AttributedModelProvider attributeContext)
        {
            return CreateActivationFeatures(attributeContext);
        }
    }
}
