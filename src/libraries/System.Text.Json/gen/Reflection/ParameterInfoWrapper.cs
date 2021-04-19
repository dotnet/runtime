// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration.Reflection
{
    internal class ParameterInfoWrapper : ParameterInfo
    {
        private readonly IParameterSymbol _parameter;

        private readonly MetadataLoadContextInternal _metadataLoadContext;

        public ParameterInfoWrapper(IParameterSymbol parameter, MetadataLoadContextInternal metadataLoadContext)
        {
            _parameter = parameter;
            _metadataLoadContext = metadataLoadContext;
        }

        public override Type ParameterType => _parameter.Type.AsType(_metadataLoadContext);

        public override string Name => _parameter.Name;

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _parameter.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }
    }
}
