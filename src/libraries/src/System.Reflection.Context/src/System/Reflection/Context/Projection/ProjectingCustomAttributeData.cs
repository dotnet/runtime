// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given custom attribute data
    internal class ProjectingCustomAttributeData : DelegatingCustomAttributeData
    {
        private readonly Projector _projector;

        public ProjectingCustomAttributeData(CustomAttributeData attribute, Projector projector)
            : base(attribute)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public override ConstructorInfo Constructor
        {
            get { return _projector.ProjectConstructor(base.Constructor); }
        }

        public override IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get { return _projector.Project(base.ConstructorArguments, _projector.ProjectTypedArgument); }
        }

        public override IList<CustomAttributeNamedArgument> NamedArguments
        {
            get { return _projector.Project(base.NamedArguments, _projector.ProjectNamedArgument); }
        }
    }
}
