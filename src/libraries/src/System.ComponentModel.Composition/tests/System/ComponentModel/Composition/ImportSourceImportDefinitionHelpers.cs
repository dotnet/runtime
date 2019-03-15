// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using Microsoft.Internal;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;

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
                Assumes.NotNull(sourceDefinition);
                this._sourceDefinition = sourceDefinition;
                this._metadata = null;
            }

            public override string ContractName
            {
                get { return this._sourceDefinition.ContractName; }
            }

            public override IDictionary<string, object> Metadata
            {
                get
                {
                    var reply = this._metadata;
                    if(reply == null)
                    {
                        reply = new Dictionary<string, object> (this._sourceDefinition.Metadata);
                        reply.Remove(CompositionConstants.ImportSourceMetadataName);
                        this._metadata = reply;
                    }

                    return reply;
                }
            }

            public override ImportCardinality Cardinality
            {
                get { return this._sourceDefinition.Cardinality; }
            }

            public override Expression<Func<ExportDefinition, bool>> Constraint
            {
                get { return this._sourceDefinition.Constraint; }
            }

            public override bool IsPrerequisite
            {
                get { return this._sourceDefinition.IsPrerequisite; }
            }

            public override bool IsRecomposable
            {
                get { return this._sourceDefinition.IsRecomposable; }
            }

            public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
            {
                Requires.NotNull(exportDefinition, "exportDefinition");

                return this._sourceDefinition.IsConstraintSatisfiedBy(exportDefinition);
            }

            public override string ToString()
            {
                return this._sourceDefinition.ToString();
            }

            public override string RequiredTypeIdentity
            {
                get { return this._sourceDefinition.RequiredTypeIdentity; }
            }

            public override IEnumerable<KeyValuePair<string, Type>> RequiredMetadata
            {
                get
                {
                    return this._sourceDefinition.RequiredMetadata;
                }
            }

            public override CreationPolicy RequiredCreationPolicy
            {
                get { return this._sourceDefinition.RequiredCreationPolicy; }
            }
        }
    }
}
