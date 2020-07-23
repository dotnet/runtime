// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public class ParameterInfoWrapper : ParameterInfo
    {
        private readonly IParameterSymbol _parameter;
        private readonly MetadataLoadContext _metadataLoadContext;

        public ParameterInfoWrapper(IParameterSymbol parameter, MetadataLoadContext metadataLoadContext)
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
