// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.Hosting
{

    internal static class ImportSourceImportDefinitionHelpers
    {
        public static ImportDefinition RemoveImportSource(this ImportDefinition definition)
        {
            var contractBasedDefinition = definition as ContractBasedImportDefinition;
            if(contractBasedDefinition == null)
            {
                return definition;
            }
            else
            {
                return new NonImportSourceImportDefinition(contractBasedDefinition);
            }
        }

        internal class NonImportSourceImportDefinition : ContractBasedImportDefinition
        {
            private ContractBasedImportDefinition _sourceDefinition;
            private IDictionary<string, object> _metadata;

            public NonImportSourceImportDefinition(ContractBasedImportDefinition sourceDefinition)
            {
                if (sourceDefinition == null)
                {
                    throw new ArgumentNullException(nameof(sourceDefinition));
                }
                _sourceDefinition = sourceDefinition;
                _metadata = null;
            }

            public override string ContractName
            {
                get { return _sourceDefinition.ContractName; }
            }

            public override IDictionary<string, object> Metadata
            {
                get
                {
                    var reply = _metadata;
                    if(reply == null)
                    {
                        reply = new Dictionary<string, object> (_sourceDefinition.Metadata);
                        reply.Remove(CompositionConstants.ImportSourceMetadataName);
                        _metadata = reply;
                    }

                    Debug.Assert(reply != null);
                    return reply;
                }
            }

            public override ImportCardinality Cardinality
            {
                get { return _sourceDefinition.Cardinality; }
            }

            public override Expression<Func<ExportDefinition, bool>> Constraint
            {
                get { return _sourceDefinition.Constraint; }
            }

            public override bool IsPrerequisite
            {
                get { return _sourceDefinition.IsPrerequisite; }
            }

            public override bool IsRecomposable
            {
                get { return _sourceDefinition.IsRecomposable; }
            }

            public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
            {
                Requires.NotNull(exportDefinition, nameof(exportDefinition));

                return _sourceDefinition.IsConstraintSatisfiedBy(exportDefinition);
            }

            public override string ToString()
            {
                return _sourceDefinition.ToString();
            }

            public override string RequiredTypeIdentity
            {
                get { return _sourceDefinition.RequiredTypeIdentity; }
            }

            public override IEnumerable<KeyValuePair<string, Type>> RequiredMetadata
            {
                get
                {
                    return _sourceDefinition.RequiredMetadata;
                }
            }

            public override CreationPolicy RequiredCreationPolicy
            {
                get { return _sourceDefinition.RequiredCreationPolicy; }
            }
        }
    }
}
