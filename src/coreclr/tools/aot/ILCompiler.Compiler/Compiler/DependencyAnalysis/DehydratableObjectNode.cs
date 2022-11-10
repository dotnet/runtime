// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public abstract class DehydratableObjectNode : ObjectNode
    {
        public sealed override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return factory.MetadataManager.IsDataDehydrated ? ObjectNodeSection.HydrationTargetSection : GetDehydratedSection(factory);
        }

        public sealed override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectData result = GetDehydratableData(factory, relocsOnly);

            // If we're not generating full data yet, or dehydration is not active,
            // return the ObjectData as is.
            if (relocsOnly || !factory.MetadataManager.IsDataDehydrated)
                return result;

            // Otherwise return the dehydrated data
            return factory.MetadataManager.PrepareForDehydration(this, result);
        }

        protected abstract ObjectNodeSection GetDehydratedSection(NodeFactory factory);
        protected abstract ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false);
    }
}
