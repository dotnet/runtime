// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public abstract class DehydratableObjectNode : ObjectNode
    {
        public sealed override ObjectNodeSection GetSection(NodeFactory factory)
        {
            ObjectNodeSection desiredSection = GetDehydratedSection(factory);

            return factory.MetadataManager.IsDataDehydrated
                && desiredSection.Type != SectionType.Uninitialized
                ? ObjectNodeSection.HydrationTargetSection : desiredSection;
        }

        public sealed override ObjectData GetData(NodeFactory factory, bool relocsOnly = false)
        {
            ObjectData result = GetDehydratableData(factory, relocsOnly);

            // If we're not actually generating data yet, don't dehydrate
            bool skipDehydrating = relocsOnly;

            // If dehydration is not active, don't dehydrate
            skipDehydrating |= !factory.MetadataManager.IsDataDehydrated;

            // If the data would be placed into an uninitialized section, that's better
            // than dehydrating a bunch of zeros.
            skipDehydrating |= GetDehydratedSection(factory).Type == SectionType.Uninitialized;

            if (skipDehydrating)
                return result;

            // Otherwise return the dehydrated data
            return factory.MetadataManager.PrepareForDehydration(this, result);
        }

        protected abstract ObjectNodeSection GetDehydratedSection(NodeFactory factory);
        protected abstract ObjectData GetDehydratableData(NodeFactory factory, bool relocsOnly = false);
    }
}
