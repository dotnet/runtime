// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given variable
    internal sealed class ProjectingLocalVariableInfo : DelegatingLocalVariableInfo
    {
        private readonly Projector _projector;

        public ProjectingLocalVariableInfo(LocalVariableInfo variable, Projector projector)
            : base(variable)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public override Type LocalType
        {
            get { return _projector.ProjectType(base.LocalType); }
        }
    }
}
