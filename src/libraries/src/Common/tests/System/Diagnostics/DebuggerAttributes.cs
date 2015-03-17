// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System.Diagnostics
{
    internal static class DebuggerAttributes
    {
        internal static object GetFieldValue(object obj, string fieldName)
        {
            return GetField(obj, fieldName).GetValue(obj);
        }

        internal static void ValidateDebuggerTypeProxyProperties(object obj)
        {
            // Get the DebuggerTypeProxyAttibute for obj
            var attrs = 
                obj.GetType().GetTypeInfo().CustomAttributes
                .Where(a => a.AttributeType == typeof(DebuggerTypeProxyAttribute))
                .ToArray();
            if (attrs.Length != 1)
            {
                throw new InvalidOperationException(
                    string.Format("Expected one DebuggerTypeProxyAttribute on {0}.", obj));
            }
            var cad = (CustomAttributeData)attrs[0];

            // Get the proxy type.  As written, this only works if the proxy and the target type
            // have the same generic parameters, e.g. Dictionary<TKey,TValue> and Proxy<TKey,TValue>.
            // It will not work with, for example, Dictionary<TKey,TValue>.Keys and Proxy<TKey>,
            // as the former has two generic parameters and the latter only one.
            Type proxyType = cad.ConstructorArguments[0].ArgumentType == typeof(Type) ?
                (Type)cad.ConstructorArguments[0].Value :
                Type.GetType((string)cad.ConstructorArguments[0].Value);
            var genericArguments = obj.GetType().GenericTypeArguments;
            if (genericArguments.Length > 0)
            {
                proxyType = proxyType.MakeGenericType(genericArguments);
            }

            // Create an instance of the proxy type, and make sure we can access all of the instance properties 
            // on the type without exception
            object proxyInstance = Activator.CreateInstance(proxyType, obj);
            foreach (var pi in proxyInstance.GetType().GetTypeInfo().DeclaredProperties)
            {
                pi.GetValue(proxyInstance, null);
            }
        }

        internal static void ValidateDebuggerDisplayReferences(object obj)
        {
            // Get the DebuggerDisplayAttribute for obj
            var attrs = 
                obj.GetType().GetTypeInfo().CustomAttributes
                .Where(a => a.AttributeType == typeof(DebuggerDisplayAttribute))
                .ToArray();
            if (attrs.Length != 1)
            {
                throw new InvalidOperationException(
                    string.Format("Expected one DebuggerDisplayAttribute on {0}.", obj));
            }
            var cad = (CustomAttributeData)attrs[0];

            // Get the text of the DebuggerDisplayAttribute
            string attrText = (string)cad.ConstructorArguments[0].Value;

            // Parse the text for all expressions
            var references = new List<string>();
            int pos = 0;
            while (true)
            {
                int openBrace = attrText.IndexOf('{', pos);
                if (openBrace < pos) break;
                int closeBrace = attrText.IndexOf('}', openBrace);
                if (closeBrace < openBrace) break;

                string reference = attrText.Substring(openBrace + 1, closeBrace - openBrace - 1).Replace(",nq", "");
                pos = closeBrace + 1;

                references.Add(reference);
            }
            if (references.Count == 0)
            {
                throw new InvalidOperationException(
                    string.Format("The DebuggerDisplayAttribute for {0} doesn't reference any expressions.", obj));
            }

            // Make sure that each referenced expression is a simple field or property name, and that we can
            // invoke the property's get accessor or read from the field.
            foreach (var reference in references)
            {
                PropertyInfo pi = GetProperty(obj, reference);
                if (pi != null)
                {
                    object ignored = pi.GetValue(obj, null);
                    continue;
                }

                FieldInfo fi = GetField(obj, reference);
                if (fi != null)
                {
                    object ignored = fi.GetValue(obj);
                    continue;
                }

                throw new InvalidOperationException(
                    string.Format("The DebuggerDisplayAttribute for {0} contains the expression \"{1}\".", obj, reference)); 
            }
        }

        private static FieldInfo GetField(object obj, string fieldName)
        {
            for (Type t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                FieldInfo fi = t.GetTypeInfo().GetDeclaredField(fieldName);
                if (fi != null)
                {
                    return fi;
                }
            }
            return null;
        }

        private static PropertyInfo GetProperty(object obj, string propertyName)
        {
            for (Type t = obj.GetType(); t != null; t = t.GetTypeInfo().BaseType)
            {
                PropertyInfo pi = t.GetTypeInfo().GetDeclaredProperty(propertyName);
                if (pi != null)
                {
                    return pi;
                }
            }
            return null;
        }
    }
}
