// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Context.Delegation;

namespace System.Reflection.Context.Projection
{
    // Recursively 'projects' any assemblies, modules, types and members returned by a given exception handling clause
    internal sealed class ProjectingExceptionHandlingClause : DelegatingExceptionHandlingClause
    {
        private readonly Projector _projector;

        public ProjectingExceptionHandlingClause(ExceptionHandlingClause clause, Projector projector)
            : base(clause)
        {
            Debug.Assert(null != projector);

            _projector = projector;
        }

        public override Type CatchType
        {
            get { return _projector.ProjectType(base.CatchType); }
        }
    }
}
