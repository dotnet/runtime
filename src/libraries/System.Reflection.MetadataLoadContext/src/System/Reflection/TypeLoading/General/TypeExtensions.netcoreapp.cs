// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file makes NetStandard Reflection's "subclassing" surface area look as much like NetCore as possible so the rest of the code can be written without #if's.

namespace System.Reflection.TypeLoading
{
    internal static class NetCoreApiEmulators
    {
        // On NetCore, call the real thing.

        public static bool IsSignatureType(this Type type) => type.IsSignatureType;
        public static bool IsSZArray(this Type type) => type.IsSZArray;
        public static bool IsVariableBoundArray(this Type type) => type.IsVariableBoundArray;
        public static bool IsGenericMethodParameter(this Type type) => type.IsGenericMethodParameter;
        public static Type MakeSignatureGenericType(this Type genericTypeDefinition, Type[] typeArguments) => Type.MakeGenericSignatureType(genericTypeDefinition, typeArguments);
    }

    /// <summary>
    /// Another layer of base types. For NetCore, these base types are empty. For NetStandard, these base types add the NetCore apis to NetStandard
    /// so code interacting with "RoTypes" and friends can happily code to the full NetCore surface area.
    /// </summary>
    internal abstract class LeveledTypeInfo : TypeInfo
    {
        protected LeveledTypeInfo() : base() { }
    }

    internal abstract class LeveledAssembly : Assembly
    {
    }

    internal abstract class LeveledConstructorInfo : ConstructorInfo
    {
    }

    internal abstract class LeveledMethodInfo : MethodInfo
    {
    }

    internal abstract class LeveledEventInfo : EventInfo
    {
    }

    internal abstract class LeveledFieldInfo : FieldInfo
    {
    }

    internal abstract class LeveledPropertyInfo : PropertyInfo
    {
    }

    internal abstract class LeveledCustomAttributeData : CustomAttributeData
    {
    }
}
