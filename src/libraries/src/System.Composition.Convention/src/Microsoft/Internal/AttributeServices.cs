// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static class AttributeServices
    {
        // MemberInfo Attribute helpers
        public static IEnumerable<T> GetAttributes<T>(this MemberInfo memberInfo) where T : System.Attribute
        {
            return memberInfo.GetCustomAttributes<T>(false);
        }

        public static IEnumerable<T> GetAttributes<T>(this MemberInfo memberInfo, bool inherit) where T : System.Attribute
        {
            return memberInfo.GetCustomAttributes<T>(inherit);
        }

        public static T GetFirstAttribute<T>(this MemberInfo memberInfo) where T : System.Attribute
        {
            return GetAttributes<T>(memberInfo).FirstOrDefault();
        }

        public static T GetFirstAttribute<T>(this MemberInfo memberInfo, bool inherit) where T : System.Attribute
        {
            return GetAttributes<T>(memberInfo, inherit).FirstOrDefault();
        }

        public static bool IsAttributeDefined<T>(this MemberInfo memberInfo) where T : System.Attribute
        {
            return memberInfo.IsDefined(typeof(T), false);
        }

        public static bool IsAttributeDefined<T>(this MemberInfo memberInfo, bool inherit) where T : System.Attribute
        {
            return memberInfo.IsDefined(typeof(T), inherit);
        }


        // ParameterInfo Attribute helpers
        public static IEnumerable<T> GetAttributes<T>(this ParameterInfo parameterInfo) where T : System.Attribute
        {
            return parameterInfo.GetCustomAttributes<T>(false);
        }

        public static IEnumerable<T> GetAttributes<T>(this ParameterInfo parameterInfo, bool inherit) where T : System.Attribute
        {
            return parameterInfo.GetCustomAttributes<T>(inherit);
        }

        public static T GetFirstAttribute<T>(this ParameterInfo parameterInfo) where T : System.Attribute
        {
            return GetAttributes<T>(parameterInfo).FirstOrDefault();
        }

        public static T GetFirstAttribute<T>(this ParameterInfo parameterInfo, bool inherit) where T : System.Attribute
        {
            return GetAttributes<T>(parameterInfo, inherit).FirstOrDefault();
        }

        public static bool IsAttributeDefined<T>(this ParameterInfo parameterInfo) where T : System.Attribute
        {
            return parameterInfo.IsDefined(typeof(T), false);
        }

        public static bool IsAttributeDefined<T>(this ParameterInfo parameterInfo, bool inherit) where T : System.Attribute
        {
            return parameterInfo.IsDefined(typeof(T), inherit);
        }
    }
}
