// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;

namespace System.ComponentModel.Composition.Factories
{
    partial class ImportDefinitionFactory
    {
        private class DerivedImportDefinition : ImportDefinition
        {
            private readonly Expression<Func<ExportDefinition, bool>> _constraint;
            private readonly ImportCardinality _cardinality;
            private readonly bool _isRecomposable;
            private readonly bool _isPrerequisite;

            public DerivedImportDefinition(Expression<Func<ExportDefinition, bool>> constraint, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
            {
                _constraint = constraint ?? (export => false);
                _cardinality = cardinality;
                _isRecomposable = isRecomposable;
                _isPrerequisite = isPrerequisite;
            }

            public override ImportCardinality Cardinality
            {
                get { return _cardinality; }
            }

            public override bool IsPrerequisite
            {
                get { return _isPrerequisite; }
            }

            public override bool IsRecomposable
            {
                get { return _isRecomposable; }
            }

            public override Expression<Func<ExportDefinition, bool>> Constraint
            {
                get { return _constraint; }
            }
        }
    }
}
