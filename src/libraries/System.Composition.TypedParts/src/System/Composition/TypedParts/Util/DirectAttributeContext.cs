// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Convention;
using System.Reflection;

namespace System.Composition.TypedParts.Util
{
    internal sealed class DirectAttributeContext : AttributedModelProvider
    {
        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.MemberInfo member)
        {
            ArgumentNullException.ThrowIfNull(reflectedType);
            ArgumentNullException.ThrowIfNull(member);

            if (!(member is TypeInfo) && member.DeclaringType != reflectedType)
                return Array.Empty<Attribute>();

            return Attribute.GetCustomAttributes(member, false);
        }

        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.ParameterInfo parameter)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            ArgumentNullException.ThrowIfNull(reflectedType);

            return Attribute.GetCustomAttributes(parameter, false);
        }
    }
}
