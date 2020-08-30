// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using static System.RuntimeTypeHandle;

namespace System.Collections.Generic
{
    /// <summary>
    /// Helper class for creating the default <see cref="Comparer{T}"/> and <see cref="EqualityComparer{T}"/>.
    /// </summary>
    /// <remarks>
    /// This class is intentionally type-unsafe and non-generic to minimize the generic instantiation overhead of creating
    /// the default comparer/equality comparer for a new type parameter. Efficiency of the methods in here does not matter too
    /// much since they will only be run once per type parameter, but generic code involved in creating the comparers needs to be
    /// kept to a minimum.
    /// </remarks>
    internal static class ComparerHelpers
    {
        /// <summary>
        /// Creates the default <see cref="Comparer{T}"/>.
        /// </summary>
        /// <param name="type">The type to create the default comparer for.</param>
        /// <remarks>
        /// The logic in this method is replicated in vm/compile.cpp to ensure that NGen saves the right instantiations,
        /// and in vm/jitinterface.cpp so the jit can model the behavior of this method.
        /// </remarks>
        internal static object CreateDefaultComparer(Type type)
        {
            Debug.Assert(type != null && type is RuntimeType);

            object? result = null;
            var runtimeType = (RuntimeType)type;

            // If T implements IComparable<T> return a GenericComparer<T>
            if (typeof(IComparable<>).MakeGenericType(type).IsAssignableFrom(type))
            {
                result = CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericComparer<int>), runtimeType);
            }
            // Nullable does not implement IComparable<T?> directly because that would add an extra interface call per comparison.
            // Instead, it relies on Comparer<T?>.Default to specialize for nullables and do the lifted comparisons if T implements IComparable.
            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    result = TryCreateNullableComparer(runtimeType);
                }
            }
            // The comparer for enums is specialized to avoid boxing.
            else if (type.IsEnum)
            {
                result = TryCreateEnumComparer(runtimeType);
            }

            return result ?? CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectComparer<object>), runtimeType);
        }

        /// <summary>
        /// Creates the default <see cref="Comparer{T}"/> for a nullable type.
        /// </summary>
        /// <param name="nullableType">The nullable type to create the default comparer for.</param>
        private static object? TryCreateNullableComparer(RuntimeType nullableType)
        {
            Debug.Assert(nullableType != null);
            Debug.Assert(nullableType.IsGenericType && nullableType.GetGenericTypeDefinition() == typeof(Nullable<>));

            var embeddedType = (RuntimeType)nullableType.GetGenericArguments()[0];

            if (typeof(IComparable<>).MakeGenericType(embeddedType).IsAssignableFrom(embeddedType))
            {
                return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableComparer<int>), embeddedType);
            }

            return null;
        }

        /// <summary>
        /// Creates the default <see cref="Comparer{T}"/> for an enum type.
        /// </summary>
        /// <param name="enumType">The enum type to create the default comparer for.</param>
        private static object? TryCreateEnumComparer(RuntimeType enumType)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);

            // Explicitly call Enum.GetUnderlyingType here. Although GetTypeCode
            // ends up doing this anyway, we end up avoiding an unnecessary P/Invoke
            // and virtual method call.
            TypeCode underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));

            // Depending on the enum type, we need to special case the comparers so that we avoid boxing.
            // Specialize differently for signed/unsigned types so we avoid problems with large numbers.
            switch (underlyingTypeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumComparer<>), enumType);
            }

            return null;
        }

        /// <summary>
        /// Creates the default <see cref="EqualityComparer{T}"/>.
        /// </summary>
        /// <param name="type">The type to create the default equality comparer for.</param>
        /// <remarks>
        /// The logic in this method is replicated in vm/compile.cpp to ensure that NGen saves the right instantiations.
        /// </remarks>
        internal static object CreateDefaultEqualityComparer(Type type)
        {
            Debug.Assert(type != null && type is RuntimeType);

            object? result = null;
            var runtimeType = (RuntimeType)type;

            // Specialize for byte so Array.IndexOf is faster.
            if (type == typeof(byte))
            {
                result = new ByteEqualityComparer();
            }
            // If T implements IEquatable<T> return a GenericEqualityComparer<T>
            else if (type.IsAssignableTo(typeof(IEquatable<>).MakeGenericType(type)))
            {
                result = CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(GenericEqualityComparer<int>), runtimeType);
            }
            // Nullable does not implement IEquatable<T?> directly because that would add an extra interface call per comparison.
            // Instead, it relies on EqualityComparer<T?>.Default to specialize for nullables and do the lifted comparisons if T implements IEquatable.
            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    result = TryCreateNullableEqualityComparer(runtimeType);
                }
            }
            // The equality comparer for enums is specialized to avoid boxing.
            else if (type.IsEnum)
            {
                result = TryCreateEnumEqualityComparer(runtimeType);
            }

            return result ?? CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(ObjectEqualityComparer<object>), runtimeType);
        }

        /// <summary>
        /// Creates the default <see cref="EqualityComparer{T}"/> for a nullable type.
        /// </summary>
        /// <param name="nullableType">The nullable type to create the default equality comparer for.</param>
        private static object? TryCreateNullableEqualityComparer(RuntimeType nullableType)
        {
            Debug.Assert(nullableType != null);
            Debug.Assert(nullableType.IsGenericType && nullableType.GetGenericTypeDefinition() == typeof(Nullable<>));

            var embeddedType = (RuntimeType)nullableType.GetGenericArguments()[0];

            if (typeof(IEquatable<>).MakeGenericType(embeddedType).IsAssignableFrom(embeddedType))
            {
                return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(NullableEqualityComparer<int>), embeddedType);
            }

            return null;
        }

        /// <summary>
        /// Creates the default <see cref="EqualityComparer{T}"/> for an enum type.
        /// </summary>
        /// <param name="enumType">The enum type to create the default equality comparer for.</param>
        private static object? TryCreateEnumEqualityComparer(RuntimeType enumType)
        {
            Debug.Assert(enumType != null);
            Debug.Assert(enumType.IsEnum);

            // See the METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST and METHOD__JIT_HELPERS__UNSAFE_ENUM_CAST_LONG cases in getILIntrinsicImplementation
            // for how we cast the enum types to integral values in the comparer without boxing.

            TypeCode underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(enumType));

            // Depending on the enum type, we need to special case the comparers so that we avoid boxing.
            switch (underlyingTypeCode)
            {
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                case TypeCode.UInt16:
                    return RuntimeTypeHandle.CreateInstanceForAnotherGenericParameter((RuntimeType)typeof(EnumEqualityComparer<>), enumType);
            }

            return null;
        }
    }
}
