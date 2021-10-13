// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Represents an entry in the Custom Attribute metadata table.
    /// </summary>
    public sealed class CustomAttributeNode : TokenBasedNode
    {
        public CustomAttributeNode(EcmaModule module, CustomAttributeHandle handle)
            : base(module, handle)
        {
        }

        private CustomAttributeHandle Handle => (CustomAttributeHandle)_handle;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            CustomAttribute customAttribute = _module.MetadataReader.GetCustomAttribute(Handle);

            // We decided not to report parent as a dependency because we don't expect custom attributes to be needed outside of their parent references

            if (!customAttribute.Constructor.IsNil)
                yield return new DependencyListEntry(factory.GetNodeForToken(_module, customAttribute.Constructor), "Custom attribute constructor");

            // TODO: Parse custom attribute value and add dependencies from it
        }

        protected override EntityHandle WriteInternal(ModuleWritingContext writeContext)
        {
            MetadataReader reader = _module.MetadataReader;
            CustomAttribute customAttribute = reader.GetCustomAttribute(Handle);

            var builder = writeContext.MetadataBuilder;

            // TODO: the value blob might contain references to tokens we need to rewrite
            var valueBlob = reader.GetBlobBytes(customAttribute.Value);

            return builder.AddCustomAttribute(writeContext.TokenMap.MapToken(customAttribute.Parent),
                writeContext.TokenMap.MapToken(customAttribute.Constructor),
                builder.GetOrAddBlob(valueBlob));
        }

        public override string ToString()
        {
            // TODO: Need to write a helper to get the name of the type
            return "Custom Attribute";
        }
    }
}
