// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Provider of augmented reflection data in support of conventions.
    /// </summary>
    public abstract class AttributedModelProvider
    {
        /// <summary>
        /// Provide the list of attributes applied to the specified member.
        /// </summary>
        /// <param name="reflectedType">The reflectedType the type used to retrieve the memberInfo.</param>
        /// <param name="member">The member to supply attributes for.</param>
        /// <returns>The list of applied attributes.</returns>
        public abstract IEnumerable<Attribute> GetCustomAttributes(System.Type reflectedType, MemberInfo member);

        /// <summary>
        /// <param name="reflectedType">The reflectedType the type used to retrieve the parameterInfo.</param>
        /// <param name="parameter">The member to supply attributes for.</param>
        /// <returns>The list of applied attributes.</returns>
        /// </summary>
        public abstract IEnumerable<Attribute> GetCustomAttributes(System.Type reflectedType, ParameterInfo parameter);
    }
}
