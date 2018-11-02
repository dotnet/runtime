// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public static class MultiServiceHelpers
    {
        public static IEnumerable GetMultiService(Type collectionType, Func<Type, IEnumerable> getAllServices)
        {
            if (IsGenericIEnumerable(collectionType))
            {
                Type serviceType = FirstGenericArgument(collectionType);
                return Cast(getAllServices(serviceType), serviceType);
            }

            return null;
        }

        private static IEnumerable Cast(IEnumerable collection, Type castItemsTo)
        {
            IList castedCollection = CreateEmptyList(castItemsTo);

            foreach (object item in collection)
            {
                castedCollection.Add(item);
            }

            return castedCollection;
        }

        private static bool IsGenericIEnumerable(Type type)
        {
            return type.GetTypeInfo().IsGenericType &&
                   type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        }

        private static Type FirstGenericArgument(Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments.Single();
        }

        private static IList CreateEmptyList(Type innerType)
        {
            Type listType = typeof(List<>).MakeGenericType(innerType);
            return (IList)Activator.CreateInstance(listType);
        }
    }
}
