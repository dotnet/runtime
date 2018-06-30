// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Reflection;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionParameterImportDefinition : ReflectionImportDefinition
    {
        private Lazy<ParameterInfo> _importingLazyParameter;

        public ReflectionParameterImportDefinition(
            Lazy<ParameterInfo> importingLazyParameter,
            string contractName, 
            string requiredTypeIdentity,
            IEnumerable<KeyValuePair<string,Type>> requiredMetadata,
            ImportCardinality cardinality, 
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object> metadata,
            ICompositionElement origin)
            : base(contractName, requiredTypeIdentity, requiredMetadata, cardinality, false, true, requiredCreationPolicy, metadata, origin)
        {
            if (importingLazyParameter == null)
            {
                throw new ArgumentNullException(nameof(importingLazyParameter));
            }

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
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} (Parameter=\"{1}\", ContractName=\"{2}\")",  // NOLOC
                parameter.Member.GetDisplayName(),
                parameter.Name,                
                ContractName);
        }
    }
}
