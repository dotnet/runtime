// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;

namespace System.ComponentModel.Composition.Primitives
{
    internal static class PrimitivesServices
    {
        public static bool IsGeneric(this ComposablePartDefinition part)
        {
            return part.Metadata.GetValue<bool>(CompositionConstants.IsGenericPartMetadataName);
        }

        public static ImportDefinition GetProductImportDefinition(this ImportDefinition import)
        {
            IPartCreatorImportDefinition partCreatorDefinition = import as IPartCreatorImportDefinition;

            if (partCreatorDefinition != null)
            {
                return partCreatorDefinition.ProductImportDefinition;
            }
            else
            {
                return import;
            }
        }

        internal static IEnumerable<string> GetCandidateContractNames(this ImportDefinition import, ComposablePartDefinition part)
        {
            import = import.GetProductImportDefinition();
            string contractName = import.ContractName;
            string genericContractName = import.Metadata.GetValue<string>(CompositionConstants.GenericContractMetadataName);
            int[] importParametersOrder = import.Metadata.GetValue<int[]>(CompositionConstants.GenericImportParametersOrderMetadataName);
            if (importParametersOrder != null)
            {
                int partArity = part.Metadata.GetValue<int>(CompositionConstants.GenericPartArityMetadataName);
                if (partArity > 0)
                {
                    contractName = GenericServices.GetGenericName(contractName, importParametersOrder, partArity);
                }
            }

            yield return contractName;
            if (!string.IsNullOrEmpty(genericContractName))
            {
                yield return genericContractName;
            }
        }

internal static bool IsImportDependentOnPart(this ImportDefinition import, ComposablePartDefinition part, ExportDefinition export, bool expandGenerics)
        {
            import = import.GetProductImportDefinition();
            if (expandGenerics)
            {
                Tuple<ComposablePartDefinition, ExportDefinition> singleMatch;
                IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> multipleMatches;
                return part.TryGetExports(import, out singleMatch, out multipleMatches);
            }
            else
            {
                return TranslateImport(import, part).IsConstraintSatisfiedBy(export);
            }
        }

        private static ImportDefinition TranslateImport(ImportDefinition import, ComposablePartDefinition part)
        {
            ContractBasedImportDefinition contractBasedImport = import as ContractBasedImportDefinition;
            if (contractBasedImport == null)
            {
                return import;
            }

            int[] importParametersOrder = contractBasedImport.Metadata.GetValue<int[]>(CompositionConstants.GenericImportParametersOrderMetadataName);
            if (importParametersOrder == null)
            {
                return import;
            }

            int partArity = part.Metadata.GetValue<int>(CompositionConstants.GenericPartArityMetadataName);
            if (partArity == 0)
            {
                return import;
            }

            string contractName = GenericServices.GetGenericName(contractBasedImport.ContractName, importParametersOrder, partArity);
            string requiredTypeIdentity = GenericServices.GetGenericName(contractBasedImport.RequiredTypeIdentity, importParametersOrder, partArity);
            return new ContractBasedImportDefinition(
                         contractName,
                         requiredTypeIdentity,
                         contractBasedImport.RequiredMetadata,
                         contractBasedImport.Cardinality,
                         contractBasedImport.IsRecomposable,
                         false,
                         contractBasedImport.RequiredCreationPolicy,
                         contractBasedImport.Metadata);
        }
    }
}
