// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection
{
    public static class CustomAttributeExtensions
    {
        #region APIs that return a single attribute
        public static Attribute? GetCustomAttribute(this Assembly element, Type attributeType)
        {
            return Attribute.GetCustomAttribute(element, attributeType);
        }
        public static Attribute? GetCustomAttribute(this Module element, Type attributeType)
        {
            return Attribute.GetCustomAttribute(element, attributeType);
        }
        public static Attribute? GetCustomAttribute(this MemberInfo element, Type attributeType)
        {
            return Attribute.GetCustomAttribute(element, attributeType);
        }
        public static Attribute? GetCustomAttribute(this ParameterInfo element, Type attributeType)
        {
            return Attribute.GetCustomAttribute(element, attributeType);
        }

        public static T? GetCustomAttribute<T>(this Assembly element) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T));
        }
        public static T? GetCustomAttribute<T>(this Module element) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T));
        }
        public static T? GetCustomAttribute<T>(this MemberInfo element) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T));
        }
        public static T? GetCustomAttribute<T>(this ParameterInfo element) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T));
        }

        public static Attribute? GetCustomAttribute(this MemberInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttribute(element, attributeType, inherit);
        }
        public static Attribute? GetCustomAttribute(this ParameterInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttribute(element, attributeType, inherit);
        }

        public static T? GetCustomAttribute<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T), inherit);
        }
        public static T? GetCustomAttribute<T>(this ParameterInfo element, bool inherit) where T : Attribute
        {
            return (T?)GetCustomAttribute(element, typeof(T), inherit);
        }
        #endregion

        #region APIs that return all attributes
        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element)
        {
            return Attribute.GetCustomAttributes(element);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this Module element)
        {
            return Attribute.GetCustomAttributes(element);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element)
        {
            return Attribute.GetCustomAttributes(element);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element)
        {
            return Attribute.GetCustomAttributes(element);
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, bool inherit)
        {
            return Attribute.GetCustomAttributes(element, inherit);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, bool inherit)
        {
            return Attribute.GetCustomAttributes(element, inherit);
        }
        #endregion

        #region APIs that return all attributes of a particular type
        public static IEnumerable<Attribute> GetCustomAttributes(this Assembly element, Type attributeType)
        {
            return Attribute.GetCustomAttributes(element, attributeType);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this Module element, Type attributeType)
        {
            return Attribute.GetCustomAttributes(element, attributeType);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType)
        {
            return Attribute.GetCustomAttributes(element, attributeType);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType)
        {
            return Attribute.GetCustomAttributes(element, attributeType);
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Assembly element) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T));
        }
        public static IEnumerable<T> GetCustomAttributes<T>(this Module element) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T));
        }
        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T));
        }
        public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T));
        }

        public static IEnumerable<Attribute> GetCustomAttributes(this MemberInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttributes(element, attributeType, inherit);
        }
        public static IEnumerable<Attribute> GetCustomAttributes(this ParameterInfo element, Type attributeType, bool inherit)
        {
            return Attribute.GetCustomAttributes(element, attributeType, inherit);
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T), inherit);
        }
        public static IEnumerable<T> GetCustomAttributes<T>(this ParameterInfo element, bool inherit) where T : Attribute
        {
            return (IEnumerable<T>)GetCustomAttributes(element, typeof(T), inherit);
        }
        #endregion

        #region IsDefined
        public static bool IsDefined(this Assembly element, Type attributeType)
        {
            return Attribute.IsDefined(element, attributeType);
        }
        public static bool IsDefined(this Module element, Type attributeType)
        {
            return Attribute.IsDefined(element, attributeType);
        }
        public static bool IsDefined(this MemberInfo element, Type attributeType)
        {
            return Attribute.IsDefined(element, attributeType);
        }
        public static bool IsDefined(this ParameterInfo element, Type attributeType)
        {
            return Attribute.IsDefined(element, attributeType);
        }

        public static bool IsDefined(this MemberInfo element, Type attributeType, bool inherit)
        {
            return Attribute.IsDefined(element, attributeType, inherit);
        }
        public static bool IsDefined(this ParameterInfo element, Type attributeType, bool inherit)
        {
            return Attribute.IsDefined(element, attributeType, inherit);
        }
        #endregion
    }
}
