// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace System.Text.Json.SourceGeneration.Reflection
{
    internal static class ReflectionExtensions
    {
        public static CustomAttributeData GetCustomAttributeData(this MemberInfo memberInfo, Type type)
        {
            return memberInfo.CustomAttributes.FirstOrDefault(a => type.IsAssignableFrom(a.AttributeType));
        }

        public static TValue GetConstructorArgument<TValue>(this CustomAttributeData customAttributeData, int index)
        {
            return index < customAttributeData.ConstructorArguments.Count ? (TValue)customAttributeData.ConstructorArguments[index].Value! : default!;
        }

        public static bool IsInitOnly(this MethodInfo method)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            MethodInfoWrapper methodInfoWrapper = (MethodInfoWrapper)method;
            return methodInfoWrapper.IsInitOnly;
        }
    }
}
