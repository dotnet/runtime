// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace System.Linq
{
    internal static class TypeHelper
    {
        internal static Type FindGenericType(Type definition, Type type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.GetTypeInfo().IsGenericType && type.GetTypeInfo().GetGenericTypeDefinition() == definition)
                    return type;
                if (definition.GetTypeInfo().IsInterface)
                {
                    foreach (Type itype in type.GetTypeInfo().ImplementedInterfaces)
                    {
                        Type found = FindGenericType(definition, itype);
                        if (found != null)
                            return found;
                    }
                }
                type = type.GetTypeInfo().BaseType;
            }
            return null;
        }

        internal static bool IsAssignableFrom(this Type source, Type destination)
        {
            return source.GetTypeInfo().IsAssignableFrom(destination.GetTypeInfo());
        }

        internal static Type[] GetGenericArguments(this Type type)
        {
            // Note that TypeInfo distinguishes between the type parameters of definitions 
            // and the type arguments of instantiations, but we want to mimic the behavior
            // of the old Type.GetGenericArguments() here.
            TypeInfo t = type.GetTypeInfo();
            return t.IsGenericTypeDefinition ? t.GenericTypeParameters : t.GenericTypeArguments;
        }

        internal static MethodInfo[] GetStaticMethods(this Type type)
        {
            var list = new List<MethodInfo>();
            foreach (var method in type.GetRuntimeMethods())
            {
                if (method.IsStatic)
                {
                    list.Add(method);
                }
            }
            return list.ToArray();
        }
    }
}
