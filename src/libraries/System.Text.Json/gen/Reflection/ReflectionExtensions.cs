// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal static partial class ReflectionExtensions
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

        private static bool HasJsonConstructorAttribute(ConstructorInfo constructorInfo)
        {
            IList<CustomAttributeData> attributeDataList = CustomAttributeData.GetCustomAttributes(constructorInfo);

            foreach (CustomAttributeData attributeData in attributeDataList)
            {
                if (attributeData.AttributeType.FullName == "System.Text.Json.Serialization.JsonConstructorAttribute")
                {
                    return true;
                }
            }

            return false;
        }

        public static Location? GetDiagnosticLocation(this Type type)
        {
            TypeWrapper? typeWrapper = type as TypeWrapper;
            Debug.Assert(typeWrapper != null);
            return typeWrapper.Location;
        }

        public static Location? GetDiagnosticLocation(this PropertyInfo propertyInfo)
        {
            PropertyInfoWrapper? propertyInfoWrapper = propertyInfo as PropertyInfoWrapper;
            Debug.Assert(propertyInfoWrapper != null);
            return propertyInfoWrapper.Location;
        }

        public static Location? GetDiagnosticLocation(this FieldInfo fieldInfo)
        {
            FieldInfoWrapper? fieldInfoWrapper = fieldInfo as FieldInfoWrapper;
            Debug.Assert(fieldInfoWrapper != null);
            return fieldInfoWrapper.Location;
        }
    }
}
