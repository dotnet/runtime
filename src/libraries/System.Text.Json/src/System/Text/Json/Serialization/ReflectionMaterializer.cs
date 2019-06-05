// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json
{
    internal sealed class ReflectionMaterializer : ClassMaterializer
    {
        public override JsonClassInfo.ConstructorDelegate CreateConstructor(Type type)
        {
            Debug.Assert(type != null);
            ConstructorInfo realMethod = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, binder: null, Type.EmptyTypes, modifiers: null);

            if (realMethod == null && !type.IsValueType)
            {
                return null;
            }

            return () => Activator.CreateInstance(type);
        }

        public override object ImmutableCollectionCreateRange(Type constructingType, Type elementType)
        {
            MethodInfo createRange = ImmutableCollectionCreateRangeMethod(constructingType, elementType);

            if (createRange == null)
            {
                return null;
            }

            return createRange.CreateDelegate(
                typeof(DefaultImmutableConverter.ImmutableCreateRangeDelegate<>).MakeGenericType(elementType), null);
        }

        public override object ImmutableDictionaryCreateRange(Type constructingType, Type elementType)
        {
            MethodInfo createRange = ImmutableDictionaryCreateRangeMethod(constructingType, elementType);

            if (createRange == null)
            {
                return null;
            }

            return createRange.CreateDelegate(
                typeof(DefaultImmutableConverter.ImmutableDictCreateRangeDelegate<,>).MakeGenericType(typeof(string), elementType), null);
        }
    }
}
