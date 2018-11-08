// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection.TypeLoading
{
    //
    // These are the official entrypoints for building/retrieving the canonical instance of all constructed types.
    //
    internal static class TypeFactories
    {
        public static RoArrayType GetUniqueArrayType(this RoType elementType) => elementType.GetRoModule().GetUniqueArrayType(elementType);
        public static RoArrayType GetUniqueArrayType(this RoType elementType, int rank) => elementType.GetRoModule().GetUniqueArrayType(elementType, rank);
        public static RoByRefType GetUniqueByRefType(this RoType elementType) => elementType.GetRoModule().GetUniqueByRefType(elementType);
        public static RoPointerType GetUniquePointerType(this RoType elementType) => elementType.GetRoModule().GetUniquePointerType(elementType);
        public static RoConstructedGenericType GetUniqueConstructedGenericType(this RoDefinitionType genericTypeDefinition, RoType[] genericTypeArguments) => genericTypeDefinition.GetRoModule().GetUniqueConstructedGenericType(genericTypeDefinition, genericTypeArguments);
    }
}
