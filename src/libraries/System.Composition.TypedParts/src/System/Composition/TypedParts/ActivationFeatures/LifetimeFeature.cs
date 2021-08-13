// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Reflection;

namespace System.Composition.TypedParts.ActivationFeatures
{
    /// <summary>
    /// Modifies the activators of parts so that they a) get associated with the correct
    /// scope, and b) obtain their dependencies from the correct scope.
    /// </summary>
    internal sealed class LifetimeFeature : ActivationFeature
    {
        public const string SharingBoundaryPartMetadataName = "SharingBoundary";

        public override CompositeActivator RewriteActivator(
            TypeInfo partType,
            CompositeActivator activatorBody,
            IDictionary<string, object> partMetadata,
            IEnumerable<CompositionDependency> dependencies)
        {
            if (!ContractHelpers.IsShared(partMetadata))
                return activatorBody;

            object sharingBoundaryMetadata;
            if (!partMetadata.TryGetValue(SharingBoundaryPartMetadataName, out sharingBoundaryMetadata))
                sharingBoundaryMetadata = null;

            var sharingBoundary = (string)sharingBoundaryMetadata;
            var sharingKey = LifetimeContext.AllocateSharingId();

            return (c, o) =>
            {
                var scope = c.FindContextWithin(sharingBoundary);
                if (object.ReferenceEquals(scope, c))
                    return scope.GetOrCreate(sharingKey, o, activatorBody);
                else
                    return CompositionOperation.Run(scope, (c1, o1) => c1.GetOrCreate(sharingKey, o1, activatorBody));
            };
        }
    }
}
