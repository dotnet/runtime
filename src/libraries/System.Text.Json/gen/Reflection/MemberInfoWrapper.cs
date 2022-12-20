// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal sealed class MemberInfoWrapper : MemberInfo
    {
        private readonly ISymbol _member;
        private readonly MetadataLoadContextInternal _metadataLoadContext;

        public MemberInfoWrapper(ISymbol member, MetadataLoadContextInternal metadataLoadContext)
        {
            _member = member;
            _metadataLoadContext = metadataLoadContext;
        }

        public override Type DeclaringType => _member.ContainingType.AsType(_metadataLoadContext);

        public override MemberTypes MemberType => throw new NotImplementedException();

        public override string Name => _member.Name;

        public override Type ReflectedType => throw new NotImplementedException();

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            var attributes = new List<CustomAttributeData>();
            foreach (AttributeData a in _member.GetAttributes())
            {
                attributes.Add(new CustomAttributeDataWrapper(a, _metadataLoadContext));
            }
            return attributes;
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new NotSupportedException();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotSupportedException();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}
