// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;


namespace System.Xml.Extensions
{
    internal static class ExtensionMethods
    {
        #region Contract compliance for System.Type

        internal static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, Type[] parameterTypes)
        {
            return type.GetConstructor(bindingFlags, null, parameterTypes, null);
        }

        internal static MethodInfo GetMethod(this Type type, string methodName, BindingFlags bindingFlags, Type[] parameterTypes)
        {
            return type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);
        }

        #endregion

        internal static Uri ToUri(string s)
        {
            if (s != null && s.Length > 0)
            { //string.Empty is a valid uri but not "   "
                s = s.Trim(new char[] { ' ', '\t', '\n', '\r' });
                if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
                {
                    throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
                }
            }
            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
            }
            return uri;
        }
    }
}
