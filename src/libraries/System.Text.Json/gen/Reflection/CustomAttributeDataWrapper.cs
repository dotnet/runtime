// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal class CustomAttributeDataWrapper : CustomAttributeData
    {
        public CustomAttributeDataWrapper(AttributeData a, MetadataLoadContextInternal metadataLoadContext)
        {
            Debug.Assert(a.AttributeConstructor != null);

            var namedArguments = new List<CustomAttributeNamedArgument>();
            foreach (KeyValuePair<string, TypedConstant> na in a.NamedArguments)
            {
                var member = a.AttributeClass.BaseTypes().SelectMany(t => t.GetMembers(na.Key)).First();

                MemberInfo memberInfo = member is IPropertySymbol
                    ? new PropertyInfoWrapper((IPropertySymbol)member, metadataLoadContext)
                    : new FieldInfoWrapper((IFieldSymbol)member, metadataLoadContext);

                namedArguments.Add(new CustomAttributeNamedArgument(memberInfo, na.Value.Value));
            }

            var constructorArguments = new List<CustomAttributeTypedArgument>();

            foreach (TypedConstant ca in a.ConstructorArguments)
            {
                if (ca.Kind == TypedConstantKind.Error)
                {
                    continue;
                }

                object value = ca.Kind == TypedConstantKind.Array ? ca.Values : ca.Value;
                constructorArguments.Add(new CustomAttributeTypedArgument(ca.Type.AsType(metadataLoadContext), value));
            }

            Constructor = new ConstructorInfoWrapper(a.AttributeConstructor, metadataLoadContext);
            NamedArguments = namedArguments;
            ConstructorArguments = constructorArguments;
        }

        public override ConstructorInfo Constructor { get; }

        public override IList<CustomAttributeNamedArgument> NamedArguments { get; }

        public override IList<CustomAttributeTypedArgument> ConstructorArguments { get; }
    }
}
