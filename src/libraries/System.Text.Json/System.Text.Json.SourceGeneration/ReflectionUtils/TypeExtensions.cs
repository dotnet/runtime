// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace System.Reflection
{
    public static class TypeExtensions 
    {
        public static bool IsIList(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(List<>)))
            {
                return true;
            }
            if (type is TypeWrapper typeWrapper)
            {
                foreach (Type t in typeWrapper.GetInterfaces())
                {
                    if (t.IsGenericType && (t.GetGenericTypeDefinition().Equals(typeof(IList<>))))
                    {
                        return true;
                    }
                }
                return false;
            }
            return type.IsAssignableFrom(typeof(List<>));
        }

        public static bool IsIEnumerable(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>)))
            {
                return true;
            }
            if (type is TypeWrapper typeWrapper)
            {
                foreach (Type t in typeWrapper.GetInterfaces())
                {
                    if (t.IsGenericType && (t.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))))
                    {
                        return true;
                    }    
                }
                return false;
            }
            return type.IsAssignableFrom(typeof(IEnumerable<>));
        }

        public static bool IsIDictionary(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Dictionary<,>)))
            {
                return true;
            }
            if (type is TypeWrapper typeWrapper)
            {
                foreach (Type t in typeWrapper.GetInterfaces())
                {
                    if (t.IsGenericType && (t.GetGenericTypeDefinition().Equals(typeof(IDictionary<,>))))
                    {
                        return true;
                    }
                }
                return false;
            }
            return type.IsAssignableFrom(typeof(Dictionary<,>));
        }

        public static string GetFullNamespace(this Type type)
        {
            if (type is TypeWrapper typeWrapper)
            {
                INamespaceSymbol root = typeWrapper.GetNamespaceSymbol;
                if (root == null)
                {
                    return "";
                }

                StringBuilder fullNamespace = new StringBuilder();
                GetFullNamespace(root);
                return fullNamespace.ToString();

                void GetFullNamespace(INamespaceSymbol current)
                {
                    if (current.IsGlobalNamespace || current.ContainingNamespace.IsGlobalNamespace)
                    {
                        fullNamespace.Append(current.Name);
                        return;
                    }

                    GetFullNamespace(current.ContainingNamespace);

                    fullNamespace.Append("." + current.Name);
                }
            }
            return type.Namespace;
        }

        public static string GetCompilableUniqueName(this Type type)
        {
            if (type is TypeWrapper typeWrapper)
            {
                return string.Join("", $"{type.Assembly.FullName}.{type.Name}".Split('.', '`'));
            }

            return type.Name;
        }
    }
}
