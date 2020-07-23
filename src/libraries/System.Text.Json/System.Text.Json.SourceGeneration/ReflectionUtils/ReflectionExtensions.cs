// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;

namespace System.Reflection
{
    internal static class ReflectionExtensions
    {
        public static CustomAttributeData GetCustomAttributeData(this MemberInfo memberInfo, Type type)
        {
            return memberInfo.CustomAttributes.FirstOrDefault(a => type.IsAssignableFrom(a.AttributeType));
        }

        public static CustomAttributeData GetCustomAttributeData(this ParameterInfo paramterInfo, Type type)
        {
            return paramterInfo.CustomAttributes.FirstOrDefault(a => type.IsAssignableFrom(a.AttributeType));
        }

        public static TValue GetConstructorArgument<TValue>(this CustomAttributeData customAttributeData, int index)
        {
            return index < customAttributeData.ConstructorArguments.Count ? (TValue)customAttributeData.ConstructorArguments[index].Value! : default!;
        }

        public static TValue GetNamedArgument<TValue>(this CustomAttributeData customAttributeData, string name)
        {
            return (TValue)customAttributeData.NamedArguments.FirstOrDefault(a => a.MemberName == name).TypedValue.Value;
        }
    }
}
