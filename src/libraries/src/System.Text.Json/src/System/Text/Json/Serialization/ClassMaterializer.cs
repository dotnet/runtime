// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json
{
    internal abstract class ClassMaterializer
    {
        public abstract JsonClassInfo.ConstructorDelegate CreateConstructor(Type classType);

        public abstract object ImmutableCollectionCreateRange(Type constructingType, Type elementType);
        public abstract object ImmutableDictionaryCreateRange(Type constructingType, Type elementType);

        protected MethodInfo ImmutableCollectionCreateRangeMethod(Type constructingType, Type elementType)
        {
            MethodInfo createRangeMethod = FindImmutableCreateRangeMethod(constructingType);

            if (createRangeMethod == null)
            {
                return null;
            }

            return createRangeMethod.MakeGenericMethod(elementType);
        }

        protected MethodInfo ImmutableDictionaryCreateRangeMethod(Type constructingType, Type elementType)
        {
            MethodInfo createRangeMethod = FindImmutableCreateRangeMethod(constructingType);

            if (createRangeMethod == null)
            {
                return null;
            }

            return createRangeMethod.MakeGenericMethod(typeof(string), elementType);
        }

        private MethodInfo FindImmutableCreateRangeMethod(Type constructingType)
        {
            MethodInfo[] constructingTypeMethods = constructingType.GetMethods();

            foreach (MethodInfo method in constructingTypeMethods)
            {
                if (method.Name == "CreateRange" && method.GetParameters().Length == 1)
                {
                    return method;
                }
            }

            // This shouldn't happen because constructingType should be an immutable type with
            // a CreateRange method. `null` being returned here will cause a JsonException to be
            // thrown when the desired CreateRange delegate is about to be invoked.
            Debug.Fail("Could not create the appropriate CreateRange method.");
            return null;
        }
    }
}
