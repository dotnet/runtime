// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static class AttributeServices
    {
        public static T[] GetAttributes<T>(this ICustomAttributeProvider attributeProvider) where T : class
        {
            return (T[])attributeProvider.GetCustomAttributes(typeof(T), false);
        }

        public static T[] GetAttributes<T>(this ICustomAttributeProvider attributeProvider, bool inherit) where T : class
        {
            return (T[])attributeProvider.GetCustomAttributes(typeof(T), inherit);
        }

        public static T GetFirstAttribute<T>(this ICustomAttributeProvider attributeProvider) where T : class
        {
            return GetAttributes<T>(attributeProvider).FirstOrDefault();
        }

        public static T GetFirstAttribute<T>(this ICustomAttributeProvider attributeProvider, bool inherit) where T : class
        {
            return GetAttributes<T>(attributeProvider, inherit).FirstOrDefault();
        }

        public static bool IsAttributeDefined<T>(this ICustomAttributeProvider attributeProvider) where T : class
        {
            return attributeProvider.IsDefined(typeof(T), false);
        }

        public static bool IsAttributeDefined<T>(this ICustomAttributeProvider attributeProvider, bool inherit) where T : class
        {
            return attributeProvider.IsDefined(typeof(T), inherit);
        }
    }
}
