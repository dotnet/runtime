// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysis;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// Compilation root provider that emits managed type layout descriptors for the cDAC.
    /// Types annotated with [CdacType] will have their [CdacField]-annotated field offsets
    /// computed at compile time and embedded in the final binary as a ContractDescriptor
    /// that the cDAC reader merges as a sub-descriptor.
    /// </summary>
    /// <remarks>
    /// The descriptor is emitted as a symbol named "DotNetManagedContractDescriptor"
    /// that the NativeAOT runtime's C++ code references via extern and stores as a
    /// sub-descriptor pointer in the main contract descriptor.
    /// </remarks>
    public class ManagedDataDescriptorProvider : ICompilationRootProvider
    {
        private const string CdacTypeAttributeNamespace = "System.Runtime.CompilerServices";
        private const string CdacTypeAttributeName = "CdacTypeAttribute";
        private const string CdacFieldAttributeName = "CdacFieldAttribute";

        private readonly CompilerTypeSystemContext _context;

        public ManagedDataDescriptorProvider(CompilerTypeSystemContext context)
        {
            _context = context;
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            var descriptorNode = new ManagedDataDescriptorNode();

            DiscoverAnnotatedTypes(descriptorNode);
            descriptorNode.FinalizeDescriptor();

            rootProvider.AddCompilationRoot(descriptorNode, "Managed type descriptors for cDAC");
            rootProvider.AddCompilationRoot(descriptorNode.JsonBlobNode, "Managed descriptor JSON data");
        }

        private void DiscoverAnnotatedTypes(ManagedDataDescriptorNode descriptorNode)
        {
            if (_context.SystemModule is not EcmaModule systemModule)
                return;

            var seenDescriptorNames = new HashSet<string>();
            MetadataReader reader = systemModule.MetadataReader;

            foreach (TypeDefinitionHandle typeDefHandle in reader.TypeDefinitions)
            {
                EcmaType ecmaType = (EcmaType)systemModule.GetType(typeDefHandle);
                var typeAttr = ecmaType.GetDecodedCustomAttribute(CdacTypeAttributeNamespace, CdacTypeAttributeName);
                if (typeAttr is null)
                    continue;

                string descriptorTypeName = (string)typeAttr.Value.FixedArguments[0].Value;

                if (string.IsNullOrEmpty(descriptorTypeName))
                    throw new InvalidOperationException($"[CdacType] on '{ecmaType}' has a null or empty descriptor name.");

                if (ecmaType.HasInstantiation)
                    throw new InvalidOperationException($"[CdacType] is not supported on generic type '{ecmaType}'.");

                if (!seenDescriptorNames.Add(descriptorTypeName))
                    throw new InvalidOperationException($"Duplicate [CdacType] descriptor name '{descriptorTypeName}' on '{ecmaType}'.");

                var fieldMappings = new Dictionary<string, string>();
                foreach (FieldDesc field in ecmaType.GetFields())
                {
                    if (field.IsStatic || field is not EcmaField ecmaField)
                        continue;

                    var fieldAttr = ecmaField.GetDecodedCustomAttribute(CdacTypeAttributeNamespace, CdacFieldAttributeName);
                    if (fieldAttr is null)
                        continue;

                    string cdacFieldName = fieldAttr.Value.FixedArguments.Length > 0
                        && fieldAttr.Value.FixedArguments[0].Value is string name
                            ? name
                            : field.GetName();

                    if (!fieldMappings.TryAdd(cdacFieldName, field.GetName()))
                        throw new InvalidOperationException($"Duplicate [CdacField] name '{cdacFieldName}' on type '{ecmaType}'.");
                }

                if (fieldMappings.Count > 0)
                {
                    descriptorNode.AddType(descriptorTypeName, (MetadataType)ecmaType, fieldMappings);
                }
            }
        }
    }
}
