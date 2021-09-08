// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given resource
    internal sealed class ProjectingManifestResourceInfo : DelegatingManifestResourceInfo
    {
        private readonly Projector _projector;

        public ProjectingManifestResourceInfo(ManifestResourceInfo resource, Projector projector)
            : base(resource)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public override Assembly? ReferencedAssembly
        {
            get { return _projector.ProjectAssembly(base.ReferencedAssembly); }
        }
    }
}
