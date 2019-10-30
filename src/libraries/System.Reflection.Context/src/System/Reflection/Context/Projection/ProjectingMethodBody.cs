// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given method body
    internal class ProjectingMethodBody : DelegatingMethodBody
    {
        private readonly Projector _projector;

        public ProjectingMethodBody(MethodBody body, Projector projector)
            : base(body)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public override IList<ExceptionHandlingClause> ExceptionHandlingClauses
        {
            get { return _projector.Project(base.ExceptionHandlingClauses, _projector.ProjectExceptionHandlingClause); }
        }

        public override IList<LocalVariableInfo> LocalVariables
        {
            get { return _projector.Project(base.LocalVariables, _projector.ProjectLocalVariable); }
        }
    }
}
