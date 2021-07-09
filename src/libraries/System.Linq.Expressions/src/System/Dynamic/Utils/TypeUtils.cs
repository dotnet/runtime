// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Dynamic.Utils
{
    internal static class TypeUtils
    {
        private static readonly Type[] s_arrayAssignableInterfaces = typeof(int[]).GetInterfaces()
            .Where(i => i.IsGenericType)
            .Select(i => i.GetGenericTypeDefinition())
            .ToArray();

        private static readonly ConstructorInfo s_nullableConstructor = typeof(Nullable<>).GetConstructor(typeof(Nullable<>).GetGenericArguments())!;

        public static Type GetNonNullableType(this Type type) => IsNullableType(type) ? type.GetGenericArguments()[0] : type;

        public static Type GetNullableType(this Type type)
        {
            Debug.Assert(type != null, "type cannot be null");
            if (type.IsValueType && !IsNullableType(type))
            {
                return typeof(Nullable<>).MakeGenericType(type);
            }

            return type;
        }

        public static ConstructorInfo GetNullableConstructor(Type nullableType)
        {
            Debug.Assert(nullableType.IsNullableType());

            return (ConstructorInfo)nullableType.GetMemberWithSameMetadataDefinitionAs(s_nullableConstructor);
        }

        public static bool IsNullableType(this Type type) => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool IsNullableOrReferenceType(this Type type) => !type.IsValueType || IsNullableType(type);

        public static bool IsBool(this Type type) => GetNonNullableType(type) == typeof(bool);

        public static bool IsNumeric(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Char:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool IsInteger(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool IsInteger64(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool IsArithmetic(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool IsUnsignedInt(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool IsIntegerOrBool(this Type type)
        {
            type = GetNonNullableType(type);
            if (!type.IsEnum)
            {
                switch (type.GetTypeCode())
                {
                    case TypeCode.Int64:
                    case TypeCode.Int32:
                    case TypeCode.Int16:
                    case TypeCode.UInt64:
                    case TypeCode.UInt32:
                    case TypeCode.UInt16:
                    case TypeCode.Boolean:
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                        return true;
                }
            }

            return false;
        }

        public static bool IsNumericOrBool(this Type type) => IsNumeric(type) || IsBool(type);

        // Checks if the type is a valid target for an instance call
        public static bool IsValidInstanceType(MemberInfo member, Type instanceType)
        {
            Type? targetType = member.DeclaringType;
            if (targetType == null)
            {
                return false;
            }

            if (AreReferenceAssignable(targetType, instanceType))
            {
                return true;
            }

            if (instanceType.IsValueType)
            {
                if (AreReferenceAssignable(targetType, typeof(object)))
                {
                    return true;
                }

                if (AreReferenceAssignable(targetType, typeof(ValueType)))
                {
                    return true;
                }

                if (instanceType.IsEnum && AreReferenceAssignable(targetType, typeof(Enum)))
                {
                    return true;
                }

                // A call to an interface implemented by a struct is legal whether the struct has
                // been boxed or not.
                if (targetType.IsInterface)
                {
                    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
                        Justification = "The targetType must be preserved (since we have an instance of it here)," +
                            "So if it's an interface that interface will be preserved everywhere" +
                            "So if it was implemented by the instanceType, it will be kept even after trimming." +
                            "The fact that GetInterfaces may return fewer interfaces doesn't matter as long" +
                            "as it returns the one we're looking for.")]
                    static Type[] GetTypeInterfaces(Type instanceType) => instanceType.GetInterfaces();
                    foreach (Type interfaceType in GetTypeInterfaces(instanceType))
                    {
                        if (AreReferenceAssignable(targetType, interfaceType))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static bool HasIdentityPrimitiveOrNullableConversionTo(this Type source, Type dest)
        {
            Debug.Assert(source != null && dest != null);

            // Identity conversion
            if (AreEquivalent(source, dest))
            {
                return true;
            }

            // Nullable conversions
            if (IsNullableType(source) && AreEquivalent(dest, GetNonNullableType(source)))
            {
                return true;
            }

            if (IsNullableType(dest) && AreEquivalent(source, GetNonNullableType(dest)))
            {
                return true;
            }

            // Primitive runtime conversions
            // All conversions amongst enum, bool, char, integer and float types
            // (and their corresponding nullable types) are legal except for
            // nonbool==>bool and nonbool==>bool? which are only legal from
            // bool-backed enums.
            return IsConvertible(source) && IsConvertible(dest)
                   && (GetNonNullableType(dest) != typeof(bool)
                   || source.IsEnum && source.GetEnumUnderlyingType() == typeof(bool));
        }

        public static bool HasReferenceConversionTo(this Type source, Type dest)
        {
            Debug.Assert(source != null && dest != null);

            // void -> void conversion is handled elsewhere
            // (it's an identity conversion)
            // All other void conversions are disallowed.
            if (source == typeof(void) || dest == typeof(void))
            {
                return false;
            }

            Type nnSourceType = GetNonNullableType(source);
            Type nnDestType = GetNonNullableType(dest);

            // Down conversion
            if (nnSourceType.IsAssignableFrom(nnDestType))
            {
                return true;
            }

            // Up conversion
            if (nnDestType.IsAssignableFrom(nnSourceType))
            {
                return true;
            }

            // Interface conversion
            if (source.IsInterface || dest.IsInterface)
            {
                return true;
            }

            // Variant delegate conversion
            if (IsLegalExplicitVariantDelegateConversion(source, dest))
            {
                return true;
            }

            // Object conversion handled by assignable above.
            Debug.Assert(source != typeof(object) && dest != typeof(object));

            return (source.IsArray || dest.IsArray) && StrictHasReferenceConversionTo(source, dest, true);
        }

        private static bool StrictHasReferenceConversionTo(this Type source, Type dest, bool skipNonArray)
        {
            // HasReferenceConversionTo was both too strict and too lax. It was too strict in prohibiting
            // some valid conversions involving arrays, and too lax in allowing casts between interfaces
            // and sealed classes that don't implement them. Unfortunately fixing the lax cases would be
            // a breaking change, especially since such expressions will even work if only given null
            // arguments.
            // This method catches the cases that were incorrectly disallowed, but when it needs to
            // examine possible conversions of element or type parameters it applies stricter rules.

            while (true)
            {
                if (!skipNonArray) // Skip if we just came from HasReferenceConversionTo and have just tested these
                {
                    if (source.IsValueType | dest.IsValueType)
                    {
                        return false;
                    }

                    // Includes to case of either being typeof(object)
                    if (source.IsAssignableFrom(dest) || dest.IsAssignableFrom(source))
                    {
                        return true;
                    }

                    if (source.IsInterface)
                    {
                        if (dest.IsInterface || dest.IsClass && !dest.IsSealed)
                        {
                            return true;
                        }
                    }
                    else if (dest.IsInterface)
                    {
                        if (source.IsClass && !source.IsSealed)
                        {
                            return true;
                        }
                    }
                }

                if (source.IsArray)
                {
                    if (dest.IsArray)
                    {
                        if (source.GetArrayRank() != dest.GetArrayRank() || source.IsSZArray != dest.IsSZArray)
                        {
                            return false;
                        }

                        source = source.GetElementType()!;
                        dest = dest.GetElementType()!;
                        skipNonArray = false;
                    }
                    else
                    {
                        return HasArrayToInterfaceConversion(source, dest);
                    }
                }
                else if (dest.IsArray)
                {
                    if (HasInterfaceToArrayConversion(source, dest))
                    {
                        return true;
                    }

                    return IsImplicitReferenceConversion(typeof(Array), source);
                }
                else
                {
                    return IsLegalExplicitVariantDelegateConversion(source, dest);
                }
            }
        }

        private static bool HasArrayToInterfaceConversion(Type source, Type dest)
        {
            Debug.Assert(source.IsArray);
            if (!source.IsSZArray || !dest.IsInterface || !dest.IsGenericType)
            {
                return false;
            }

            Type[] destParams = dest.GetGenericArguments();
            if (destParams.Length != 1)
            {
                return false;
            }

            Type destGen = dest.GetGenericTypeDefinition();

            foreach (Type iface in s_arrayAssignableInterfaces)
            {
                if (AreEquivalent(destGen, iface))
                {
                    return StrictHasReferenceConversionTo(source.GetElementType()!, destParams[0], false);
                }
            }

            return false;
        }

        private static bool HasInterfaceToArrayConversion(Type source, Type dest)
        {
            Debug.Assert(dest.IsSZArray);
            if (!dest.IsSZArray || !source.IsInterface || !source.IsGenericType)
            {
                return false;
            }

            Type[] sourceParams = source.GetGenericArguments();
            if (sourceParams.Length != 1)
            {
                return false;
            }

            Type sourceGen = source.GetGenericTypeDefinition();

            foreach (Type iface in s_arrayAssignableInterfaces)
            {
                if (AreEquivalent(sourceGen, iface))
                {
                    return StrictHasReferenceConversionTo(sourceParams[0], dest.GetElementType()!, false);
                }
            }

            return false;
        }

        private static bool IsCovariant(Type t)
        {
            Debug.Assert(t != null);
            return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Covariant);
        }

        private static bool IsContravariant(Type t)
        {
            Debug.Assert(t != null);
            return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Contravariant);
        }

        private static bool IsInvariant(Type t)
        {
            Debug.Assert(t != null);
            return 0 == (t.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
        }

        private static bool IsDelegate(Type t)
        {
            Debug.Assert(t != null);
            return t.IsSubclassOf(typeof(MulticastDelegate));
        }

        public static bool IsLegalExplicitVariantDelegateConversion(Type source, Type dest)
        {
            Debug.Assert(source != null && dest != null);

            // There *might* be a legal conversion from a generic delegate type S to generic delegate type  T,
            // provided all of the follow are true:
            //   o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.
            //     That is, S = D<S1...>, T = D<T1...>.
            //   o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
            //   o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
            //     to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
            //   o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti,
            //     or Si and Ti must both be reference types.

            if (!IsDelegate(source) || !IsDelegate(dest) || !source.IsGenericType || !dest.IsGenericType)
            {
                return false;
            }

            Type genericDelegate = source.GetGenericTypeDefinition();

            if (dest.GetGenericTypeDefinition() != genericDelegate)
            {
                return false;
            }

            Type[] genericParameters = genericDelegate.GetGenericArguments();
            Type[] sourceArguments = source.GetGenericArguments();
            Type[] destArguments = dest.GetGenericArguments();

            Debug.Assert(genericParameters != null);
            Debug.Assert(sourceArguments != null);
            Debug.Assert(destArguments != null);
            Debug.Assert(genericParameters.Length == sourceArguments.Length);
            Debug.Assert(genericParameters.Length == destArguments.Length);

            for (int iParam = 0; iParam < genericParameters.Length; ++iParam)
            {
                Type sourceArgument = sourceArguments[iParam];
                Type destArgument = destArguments[iParam];

                Debug.Assert(sourceArgument != null && destArgument != null);

                // If the arguments are identical then this one is automatically good, so skip it.
                if (AreEquivalent(sourceArgument, destArgument))
                {
                    continue;
                }

                Type genericParameter = genericParameters[iParam];

                Debug.Assert(genericParameter != null);

                if (IsInvariant(genericParameter))
                {
                    return false;
                }

                if (IsCovariant(genericParameter))
                {
                    if (!sourceArgument.HasReferenceConversionTo(destArgument))
                    {
                        return false;
                    }
                }
                else if (IsContravariant(genericParameter) && (sourceArgument.IsValueType || destArgument.IsValueType))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsConvertible(this Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum)
            {
                return true;
            }

            switch (type.GetTypeCode())
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                    return true;

                default:
                    return false;
            }
        }

        public static bool HasReferenceEquality(Type left, Type right)
        {
            if (left.IsValueType || right.IsValueType)
            {
                return false;
            }

            // If we have an interface and a reference type then we can do
            // reference equality.

            // If we have two reference types and one is assignable to the
            // other then we can do reference equality.

            return left.IsInterface || right.IsInterface || AreReferenceAssignable(left, right)
                   || AreReferenceAssignable(right, left);
        }

        public static bool HasBuiltInEqualityOperator(Type left, Type right)
        {
            // If we have an interface and a reference type then we can do
            // reference equality.
            if (left.IsInterface && !right.IsValueType)
            {
                return true;
            }

            if (right.IsInterface && !left.IsValueType)
            {
                return true;
            }

            // If we have two reference types and one is assignable to the
            // other then we can do reference equality.
            if (!left.IsValueType && !right.IsValueType)
            {
                if (AreReferenceAssignable(left, right) || AreReferenceAssignable(right, left))
                {
                    return true;
                }
            }

            // Otherwise, if the types are not the same then we definitely
            // do not have a built-in equality operator.
            if (!AreEquivalent(left, right))
            {
                return false;
            }

            // We have two identical value types, modulo nullability.  (If they were both the
            // same reference type then we would have returned true earlier.)
            Debug.Assert(left.IsValueType);

            // Equality between struct types is only defined for numerics, bools, enums,
            // and their nullable equivalents.
            Type nnType = GetNonNullableType(left);
            return nnType == typeof(bool) || IsNumeric(nnType) || nnType.IsEnum;
        }

        public static bool IsImplicitlyConvertibleTo(this Type source, Type destination) =>
            AreEquivalent(source, destination) // identity conversion
            || IsImplicitNumericConversion(source, destination)
            || IsImplicitReferenceConversion(source, destination)
            || IsImplicitBoxingConversion(source, destination)
            || IsImplicitNullableConversion(source, destination);

        [RequiresUnreferencedCode(Expression.ExpressionRequiresUnreferencedCode)]
        public static MethodInfo? GetUserDefinedCoercionMethod(Type convertFrom, Type convertToType)
        {
            Type nnExprType = GetNonNullableType(convertFrom);
            Type nnConvType = GetNonNullableType(convertToType);

            // try exact match on types
            MethodInfo[] eMethods = nnExprType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            MethodInfo? method = FindConversionOperator(eMethods, convertFrom, convertToType);
            if (method != null)
            {
                return method;
            }

            MethodInfo[] cMethods = nnConvType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            method = FindConversionOperator(cMethods, convertFrom, convertToType);
            if (method != null)
            {
                return method;
            }

            if (AreEquivalent(nnExprType, convertFrom) && AreEquivalent(nnConvType, convertToType))
            {
                return null;
            }

            // try lifted conversion
            return FindConversionOperator(eMethods, nnExprType, nnConvType)
                   ?? FindConversionOperator(cMethods, nnExprType, nnConvType)
                   ?? FindConversionOperator(eMethods, nnExprType, convertToType)
                   ?? FindConversionOperator(cMethods, nnExprType, convertToType);
        }

        private static MethodInfo? FindConversionOperator(MethodInfo[] methods, Type? typeFrom, Type? typeTo)
        {
            foreach (MethodInfo mi in methods)
            {
                if ((mi.Name == "op_Implicit" || mi.Name == "op_Explicit") && AreEquivalent(mi.ReturnType, typeTo))
                {
                    ParameterInfo[] pis = mi.GetParametersCached();
                    if (pis.Length == 1 && AreEquivalent(pis[0].ParameterType, typeFrom))
                    {
                        return mi;
                    }
                }
            }

            return null;
        }

        private static bool IsImplicitNumericConversion(Type source, Type destination)
        {
            TypeCode tcSource = source.GetTypeCode();
            TypeCode tcDest = destination.GetTypeCode();

            switch (tcSource)
            {
                case TypeCode.SByte:
                    switch (tcDest)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Byte:
                    switch (tcDest)
                    {
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Int16:
                    switch (tcDest)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.UInt16:
                    switch (tcDest)
                    {
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Int32:
                    switch (tcDest)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.UInt32:
                    switch (tcDest)
                    {
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    switch (tcDest)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Char:
                    switch (tcDest)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }

                    break;
                case TypeCode.Single:
                    return tcDest == TypeCode.Double;
            }

            return false;
        }

        private static bool IsImplicitReferenceConversion(Type source, Type destination) =>
            destination.IsAssignableFrom(source);

        private static bool IsImplicitBoxingConversion(Type source, Type destination) =>
            source.IsValueType && (destination == typeof(object) || destination == typeof(ValueType)) || source.IsEnum && destination == typeof(Enum);

        private static bool IsImplicitNullableConversion(Type source, Type destination) =>
            IsNullableType(destination) && IsImplicitlyConvertibleTo(GetNonNullableType(source), GetNonNullableType(destination));

        public static Type? FindGenericType(Type definition, Type? type)
        {
            // For now this helper doesn't support interfaces
            Debug.Assert(!definition.IsInterface);

            while (type is not null && type != typeof(object))
            {
                if (type.IsConstructedGenericType && AreEquivalent(type.GetGenericTypeDefinition(), definition))
                {
                    return type;
                }

                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Searches for an operator method on the type. The method must have
        /// the specified signature, no generic arguments, and have the
        /// SpecialName bit set. Also searches inherited operator methods.
        ///
        /// NOTE: This was designed to satisfy the needs of op_True and
        /// op_False, because we have to do runtime lookup for those. It may
        /// not work right for unary operators in general.
        /// </summary>
        public static MethodInfo? GetBooleanOperator(Type type, string name)
        {
            do
            {
                MethodInfo? result = type.GetAnyStaticMethodValidated(name, new[] { type });
                if (result != null && result.IsSpecialName && !result.ContainsGenericParameters)
                {
                    return result;
                }

                type = type.BaseType!;
            } while (type != null);

            return null;
        }

        public static Type GetNonRefType(this Type type) => type.IsByRef ? type.GetElementType()! : type;

        public static bool AreEquivalent(Type? t1, Type? t2) => t1 != null && t1.IsEquivalentTo(t2);

        public static bool AreReferenceAssignable(Type dest, Type src)
        {
            // This actually implements "Is this identity assignable and/or reference assignable?"
            if (AreEquivalent(dest, src))
            {
                return true;
            }

            return !dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src);
        }

        public static bool IsSameOrSubclass(Type type, Type subType) =>
            AreEquivalent(type, subType) || subType.IsSubclassOf(type);

        public static void ValidateType(Type type, string? paramName) => ValidateType(type, paramName, false, false);

        public static void ValidateType(Type type, string? paramName, bool allowByRef, bool allowPointer)
        {
            if (ValidateType(type, paramName, -1))
            {
                if (!allowByRef && type.IsByRef)
                {
                    throw Error.TypeMustNotBeByRef(paramName);
                }

                if (!allowPointer && type.IsPointer)
                {
                    throw Error.TypeMustNotBePointer(paramName);
                }
            }
        }

        public static bool ValidateType(Type type, string? paramName, int index)
        {
            if (type == typeof(void))
            {
                return false; // Caller can skip further checks.
            }

            if (type.ContainsGenericParameters)
            {
                throw type.IsGenericTypeDefinition
                    ? Error.TypeIsGeneric(type, paramName, index)
                    : Error.TypeContainsGenericParameters(type, paramName, index);
            }

            return true;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The trimmer will never remove the Invoke method from delegates.")]
        public static MethodInfo GetInvokeMethod(this Type delegateType)
        {
            Debug.Assert(typeof(Delegate).IsAssignableFrom(delegateType));
            return delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        }

#if FEATURE_COMPILE

        internal static bool IsUnsigned(this Type type) => IsUnsigned(GetNonNullableType(type).GetTypeCode());

        internal static bool IsUnsigned(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Char:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }

        internal static bool IsFloatingPoint(this Type type) => IsFloatingPoint(GetNonNullableType(type).GetTypeCode());

        internal static bool IsFloatingPoint(this TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Single:
                case TypeCode.Double:
                    return true;

                default:
                    return false;
            }
        }

#endif

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The Array 'Get' method is dynamically constructed and is not included in IL. It is not subject to trimming.")]
        public static MethodInfo GetArrayGetMethod(Type arrayType)
        {
            Debug.Assert(arrayType.IsArray);
            return arrayType.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance)!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The Array 'Set' method is dynamically constructed and is not included in IL. It is not subject to trimming.")]
        public static MethodInfo GetArraySetMethod(Type arrayType)
        {
            Debug.Assert(arrayType.IsArray);
            return arrayType.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance)!;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "The Array 'Address' method is dynamically constructed and is not included in IL. It is not subject to trimming.")]
        public static MethodInfo GetArrayAddressMethod(Type arrayType)
        {
            Debug.Assert(arrayType.IsArray);
            return arrayType.GetMethod("Address", BindingFlags.Public | BindingFlags.Instance)!;
        }
    }
}
