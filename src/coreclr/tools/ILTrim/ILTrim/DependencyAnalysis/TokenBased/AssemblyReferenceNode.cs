// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

namespace ILTrim.DependencyAnalysis
{
    // Used for HandleKey<AssemblyReferenceValue> in NodeFactory,
    // to compare assemblies by simple name.
    public struct AssemblyReferenceValue : IEquatable<AssemblyReferenceValue>
    {
        public readonly EcmaAssembly Reference;
        public AssemblyReferenceValue(EcmaAssembly reference) => Reference = reference;

        public override bool Equals(object obj) => obj is AssemblyReferenceValue asmRef && Equals(asmRef);
        public bool Equals(AssemblyReferenceValue other)
        {
            return Reference.GetName().Name == other.Reference.GetName().Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Reference.GetName().Name);
        }
    }

    /// <summary>
    /// Represents an assembly referenced by the module. This doesn't necessarily
    /// have a source token, because references to type forwarders may introduce
    /// new assembly references when we resolve through the type forwarders.
    /// </summary>
    public sealed class AssemblyReferenceNode : TokenWriterNode
    {
        private readonly EcmaAssembly _reference;

        public override TableIndex TableIndex => TableIndex.AssemblyRef;

        public AssemblyReferenceHandle? TargetToken = null;

        public AssemblyReferenceNode(EcmaModule module, EcmaAssembly reference)
            : base(module)
        {
            _reference = reference;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            yield break;
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            var builder = writeContext.MetadataBuilder;
            AssemblyName referenceName = _reference.GetName();

            return builder.AddAssemblyReference(
                builder.GetOrAddString(referenceName.Name),
                referenceName.Version,
                builder.GetOrAddString(referenceName.CultureName),
                builder.GetOrAddBlob(referenceName.GetPublicKeyToken()),
                default(AssemblyFlags),
                default(BlobHandle));
        }

        public override void BuildTokens(TokenMap.Builder builder)
        {
            TargetToken = (AssemblyReferenceHandle)builder.AddToken(TableIndex);
        }

        public override int CompareTo(TokenWriterNode other) {
            if (other is AssemblyReferenceNode otherAssemblyReferenceNode)
            {
                // All AssemblyReferenceNodes should have the same table index.
                Debug.Assert(base.CompareToHelper(other) == 0);
                // Sort by simple assembly name.
                int result = _reference.GetName().Name.CompareTo(otherAssemblyReferenceNode._reference.GetName().Name);
                // It's only valid to compare these within the same module
                Debug.Assert(result != 0 || this == other);
                return result;
            }
            else
            {
                int baseResult = base.CompareToHelper(other);
                // Different node types should have different table indices.
                Debug.Assert(baseResult != 0);
                return baseResult;
            }
        }

        protected override string GetName(NodeFactory context)
        {
            MetadataReader reader = _module.MetadataReader;
            string moduleName = reader.GetString(reader.GetModuleDefinition().Name);
            return $"{this.ToString()} ({moduleName}:AssemblyRef)";
        }

        public override string ToString()
        {
            return _reference.GetName().ToString();
        }
    }
}
