// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Composition.Convention;
using Microsoft.Internal;

namespace System.Composition.TypedParts.Util
{
    internal class DirectAttributeContext : AttributedModelProvider
    {
        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.MemberInfo member)
        {
            if (reflectedType == null) throw new ArgumentNullException(nameof(reflectedType));
            if (member == null) throw new ArgumentNullException(nameof(member));

            if (!(member is TypeInfo) && member.DeclaringType != reflectedType)
                return EmptyArray<Attribute>.Value;
#if netstandard10
            return member.GetCustomAttributes(false);
#else
            return Attribute.GetCustomAttributes(member, false);
#endif
        }

        public override IEnumerable<Attribute> GetCustomAttributes(Type reflectedType, Reflection.ParameterInfo parameter)
        {
            if (reflectedType == null) throw new ArgumentNullException(nameof(reflectedType));
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
#if netstandard10
            return parameter.GetCustomAttributes(false);
#else
            return Attribute.GetCustomAttributes(parameter, false);
#endif
        }
    }
}
