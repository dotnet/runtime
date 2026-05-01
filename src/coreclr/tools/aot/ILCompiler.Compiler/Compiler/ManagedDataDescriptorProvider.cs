// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    /// <summary>
    /// Compilation root provider that adds the managed cDAC data descriptor node.
    /// The node discovers [DataContract]-annotated types from MetadataManager.GetTypesWithEETypes()
    /// during object data emission, ensuring only types with MethodTables are included.
    /// </summary>
    public class ManagedDataDescriptorProvider : ICompilationRootProvider
    {
        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            var descriptorNode = new ManagedDataDescriptorNode();
            rootProvider.AddCompilationRoot(descriptorNode, "Managed type descriptors for cDAC");
        }
    }
}
