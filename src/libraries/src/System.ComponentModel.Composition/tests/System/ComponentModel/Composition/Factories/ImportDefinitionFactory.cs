// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;

namespace System.ComponentModel.Composition.Factories
{
    // This class deliberately does not create instances of ImportDefinition,
    // so as to test other derived classes from ImportDefinition.
    internal static partial class ImportDefinitionFactory
    {
        public static ImportDefinition Create(Type contractType, ImportCardinality cardinality)
        {
            return Create(AttributedModelServices.GetContractName(contractType), cardinality);
        }

        public static ImportDefinition Create(string contractName)
        {
            return Create(contractName, (IEnumerable<KeyValuePair<string, Type>>)null, ImportCardinality.ExactlyOne, true, false);
        }

        public static ImportDefinition Create(string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
        {
            return Create(contractName, requiredMetadata, ImportCardinality.ExactlyOne, true, false);
        }

        public static ImportDefinition Create(string contractName, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
        {
            return Create(contractName, (IEnumerable<KeyValuePair<string, Type>>)null, cardinality, isRecomposable, isPrerequisite);
        }

        public static ImportDefinition Create(string contractName, ImportCardinality cardinality)
        {
            return Create(contractName, (IEnumerable<KeyValuePair<string, Type>>)null, cardinality, false, false);
        }

        public static ImportDefinition Create(string contractName, bool isRecomposable)
        {
            return Create(contractName, (IEnumerable<KeyValuePair<string, Type>>)null, ImportCardinality.ExactlyOne, isRecomposable, false);
        }

        public static ImportDefinition Create(string contractName, bool isRecomposable, bool isPrerequisite)
        {
            return Create(contractName, (IEnumerable<KeyValuePair<string, Type>>)null, ImportCardinality.ExactlyOne, isRecomposable, isPrerequisite);
        }

        public static ImportDefinition Create(string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
        {
            return new DerivedContractBasedImportDefinition(contractName, requiredMetadata, cardinality, isRecomposable, isPrerequisite);
        }

        public static ImportDefinition Create()
        {
            return Create((Expression<Func<ExportDefinition, bool>>)null);
        }

        public static ImportDefinition Create(Expression<Func<ExportDefinition, bool>> constraint)
        {
            return Create(constraint, ImportCardinality.ExactlyOne, true, false);
        }

        public static ImportDefinition Create(Expression<Func<ExportDefinition, bool>> constraint, ImportCardinality cardinality)
        {
            return Create(constraint, cardinality, true, false);
        }

        public static ImportDefinition Create(Expression<Func<ExportDefinition, bool>> constraint, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
        {
            return new DerivedImportDefinition(constraint, cardinality, isRecomposable, isPrerequisite);
        }

        public static ImportDefinition CreateDefault(string contractName, ImportCardinality cardinality, bool isRecomposable, bool isPrerequisite)
        {
            return new ContractBasedImportDefinition(contractName, (string)null, (IEnumerable<KeyValuePair<string, Type>>)null, cardinality, isRecomposable, isPrerequisite, CreationPolicy.Any);
        }
    }
}
