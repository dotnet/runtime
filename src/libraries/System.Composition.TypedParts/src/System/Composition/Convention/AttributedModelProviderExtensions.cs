// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;

namespace System.Composition.Convention
{
    /// <summary>
    /// Helper extension methods for retrieving attributes from objects implementing IAttributedModelConvention
    /// </summary>
    internal static class AttributedModelProviderExtensions
    {
        public static TAttribute GetDeclaredAttribute<TAttribute>(this AttributedModelProvider convention, Type reflectedType, MemberInfo member) where TAttribute : Attribute
        {
            return convention.GetCustomAttributes(reflectedType, member).OfType<TAttribute>().SingleOrDefault();
        }

        public static Attribute[] GetDeclaredAttributes(this AttributedModelProvider convention, Type reflectedType, MemberInfo member)
        {
            return convention.GetCustomAttributes(reflectedType, member).ToArray();
        }

        public static TAttribute[] GetDeclaredAttributes<TAttribute>(this AttributedModelProvider convention, Type reflectedType, MemberInfo member) where TAttribute : Attribute
        {
            return convention.GetCustomAttributes(reflectedType, member).OfType<TAttribute>().ToArray();
        }

        public static TAttribute GetDeclaredAttribute<TAttribute>(this AttributedModelProvider convention, Type reflectedType, ParameterInfo parameter) where TAttribute : Attribute
        {
            return convention.GetCustomAttributes(reflectedType, parameter).OfType<TAttribute>().SingleOrDefault();
        }

        public static Attribute[] GetDeclaredAttributes(this AttributedModelProvider convention, Type reflectedType, ParameterInfo parameter)
        {
            return convention.GetCustomAttributes(reflectedType, parameter).OfType<Attribute>().ToArray();
        }

        public static TAttribute[] GetDeclaredAttributes<TAttribute>(this AttributedModelProvider convention, Type reflectedType, ParameterInfo parameter) where TAttribute : Attribute
        {
            return convention.GetCustomAttributes(reflectedType, parameter).OfType<TAttribute>().ToArray();
        }
    }
}
