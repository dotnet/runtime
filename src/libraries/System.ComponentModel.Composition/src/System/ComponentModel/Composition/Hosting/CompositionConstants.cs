// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.Hosting
{
    public static class CompositionConstants
    {
        private const string CompositionNamespace = "System.ComponentModel.Composition";

        public const string PartCreationPolicyMetadataName = CompositionNamespace + ".CreationPolicy";
        public const string ImportSourceMetadataName = CompositionNamespace + ".ImportSource";
        public const string IsGenericPartMetadataName = CompositionNamespace + ".IsGenericPart";
        public const string GenericContractMetadataName = CompositionNamespace + ".GenericContractName";
        public const string GenericParametersMetadataName = CompositionNamespace + ".GenericParameters";
        public const string ExportTypeIdentityMetadataName = "ExportTypeIdentity";

        internal const string GenericImportParametersOrderMetadataName = CompositionNamespace + ".GenericImportParametersOrderMetadataName";
        internal const string GenericExportParametersOrderMetadataName = CompositionNamespace + ".GenericExportParametersOrderMetadataName";
        internal const string GenericPartArityMetadataName = CompositionNamespace + ".GenericPartArity";
        internal const string GenericParameterConstraintsMetadataName = CompositionNamespace + ".GenericParameterConstraints";
        internal const string GenericParameterAttributesMetadataName = CompositionNamespace + ".GenericParameterAttributes";

        internal const string ProductDefinitionMetadataName = "ProductDefinition";

        internal const string PartCreatorContractName = CompositionNamespace + ".Contracts.ExportFactory";
        internal static readonly string PartCreatorTypeIdentity = AttributedModelServices.GetTypeIdentity(typeof(ComposablePartDefinition));
    }
}
