// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Reflection;
using System.Linq;


namespace System.Xml.Extensions
{
    internal static class ExtensionMethods
    {
        #region Contract compliance for System.Type

        private static bool TypeSequenceEqual(Type[] seq1, Type[] seq2)
        {
            if (seq1 == null || seq2 == null || seq1.Length != seq2.Length)
                return false;
            for (int i = 0; i < seq1.Length; i++)
            {
                if (!seq1[i].Equals(seq2[i]) && !seq1[i].IsAssignableFrom(seq2[i]))
                    return false;
            }
            return true;
        }

        private static MethodBase FilterMethodBases(MethodBase[] methodBases, Type[] parameterTypes, string methodName)
        {
            if (methodBases == null || string.IsNullOrEmpty(methodName))
                return null;

            var matchedMethods = methodBases.Where(method => method.Name.Equals(methodName));
            matchedMethods = matchedMethods.Where(method => TypeSequenceEqual(method.GetParameters().Select(param => param.ParameterType).ToArray(), parameterTypes));
            return matchedMethods.FirstOrDefault();
        }

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
            Uri uri;
            if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out uri))
            {
                throw new FormatException(SR.Format(SR.XmlConvert_BadFormat, s, "Uri"));
            }
            return uri;
        }
    }
}
