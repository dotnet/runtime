// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Composition.Convention;

namespace System.Composition.TypedParts.Util
{
    internal class DirectAttributeContext : AttributedModelProvider
    {
        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.MemberInfo member)
        {
            if (reflectedType is null) throw new ArgumentNullException(nameof(reflectedType));
            if (member is null) throw new ArgumentNullException(nameof(member));

            if (!(member is TypeInfo) && member.DeclaringType != reflectedType)
                return Array.Empty<Attribute>();

            return Attribute.GetCustomAttributes(member, false);
        }

        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.ParameterInfo parameter)
        {
            if (reflectedType is null) throw new ArgumentNullException(nameof(reflectedType));
            if (parameter is null) throw new ArgumentNullException(nameof(parameter));

            return Attribute.GetCustomAttributes(parameter, false);
        }
    }
}
