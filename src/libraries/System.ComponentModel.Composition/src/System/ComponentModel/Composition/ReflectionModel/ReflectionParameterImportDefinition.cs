// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionParameterImportDefinition : ReflectionImportDefinition
    {
        private readonly Lazy<ParameterInfo> _importingLazyParameter;

        public ReflectionParameterImportDefinition(
            Lazy<ParameterInfo> importingLazyParameter,
            string contractName,
            string? requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>>? requiredMetadata,
            ImportCardinality cardinality,
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object?> metadata,
            ICompositionElement? origin)
            : base(contractName, requiredTypeIdentity, requiredMetadata, cardinality, false, true, requiredCreationPolicy, metadata, origin)
        {
            ArgumentNullException.ThrowIfNull(importingLazyParameter);

            _importingLazyParameter = importingLazyParameter;
        }

        public override ImportingItem ToImportingItem()
        {
            return new ImportingParameter(this, new ImportType(ImportingLazyParameter.GetNotNullValue("parameter").ParameterType, Cardinality));
        }

        public Lazy<ParameterInfo> ImportingLazyParameter
        {
            get { return _importingLazyParameter; }
        }

        protected override string GetDisplayName()
        {
            ParameterInfo parameter = ImportingLazyParameter.GetNotNullValue("parameter");
            return $"{parameter.Member.GetDisplayName()} (Parameter=\"{parameter.Name}\", ContractName=\"{ContractName}\")";  // NOLOC
        }
    }
}
