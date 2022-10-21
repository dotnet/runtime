// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public static partial class CastingHelper
    {
        /// <summary>
        /// Returns true if '<paramref name="thisType"/>' can be cast to '<paramref name="otherType"/>'.
        /// Assumes '<paramref name="thisType"/>' is in it's boxed form if it's a value type (i.e.
        /// [System.Int32].CanCastTo([System.Object]) will return true).
        /// </summary>
        public static bool CanCastTo(this TypeDesc thisType, TypeDesc otherType)
        {
            return thisType.CanCastToInternal(otherType, null);
        }

        /// <summary>
        /// Get TypeFlags of the reduced type of a type.
        /// The reduced type concept is described in ECMA 335 chapter I.8.7
        /// </summary>
        private static TypeFlags GetReducedTypeElementType(TypeDesc type)
        {
            TypeFlags elemType = type.GetTypeFlags(TypeFlags.CategoryMask);
            switch (elemType)
            {
                case TypeFlags.Byte:
                    return TypeFlags.SByte;
                case TypeFlags.UInt16:
                    return TypeFlags.Int16;
                case TypeFlags.UInt32:
                    return TypeFlags.Int32;
                case TypeFlags.UInt64:
                    return TypeFlags.Int64;
                case TypeFlags.UIntPtr:
                    return TypeFlags.IntPtr;
            }

            return elemType;
        }

        /// <summary>
        /// Get CorElementType of the verification type of a type.
        /// The verification type concepts is described in ECMA 335 chapter I.8.7
        /// </summary>
        private static TypeFlags GetVerificationTypeElementType(TypeDesc type)
        {
            TypeFlags reducedTypeElementType = GetReducedTypeElementType(type);

            switch (reducedTypeElementType)
            {
                case TypeFlags.Boolean:
                    return TypeFlags.SByte;
                case TypeFlags.Char:
                    return TypeFlags.Int16;
            }

            return reducedTypeElementType;
        }

        /// <summary>
        /// Check if verification types of two types are equal
        /// </summary>
        private static bool AreVerificationTypesEqual(TypeDesc type1, TypeDesc type2)
        {
            if (type1 == type2)
            {
                return true;
            }

            if (type1.IsPrimitive && type2.IsPrimitive)
            {
                TypeFlags e1 = GetVerificationTypeElementType(type1);
                TypeFlags e2 = GetVerificationTypeElementType(type2);

                return e1 == e2;
            }

            return false;
        }

        /// <summary>
        /// Check if signatures of two function pointers are compatible
        /// Note - this is a simplified version of what's described in the ECMA spec and it considers
        /// pointers to be method-signature-compatible-with only if the signatures are the same.
        /// </summary>
        private static bool IsMethodSignatureCompatibleWith(TypeDesc fn1Ttype, TypeDesc fn2Type)
        {
            Debug.Assert(fn1Ttype.IsFunctionPointer && fn2Type.IsFunctionPointer);
            return fn1Ttype == fn2Type;
        }

        /// <summary>
        /// Checks if two types are compatible according to compatible-with as described in ECMA 335 I.8.7.1
        /// Most of the checks are performed by the CanCastTo, but some cases are pre-filtered out.
        /// </summary>
        public static bool IsCompatibleWith(this TypeDesc thisType, TypeDesc otherType)
        {
            // Structs can be cast to the interfaces they implement, but they are not compatible according to ECMA I.8.7.1
            bool isCastFromValueTypeToReferenceType = otherType.IsValueType && !thisType.IsValueType;
            if (isCastFromValueTypeToReferenceType)
            {
                return false;
            }

            // Managed pointers are compatible only if they are pointer-element-compatible-with as described in ECMA I.8.7.2
            if (thisType.IsByRef && otherType.IsByRef)
            {
                return AreVerificationTypesEqual(thisType.GetParameterType(), otherType.GetParameterType());
            }

            // Unmanaged pointers are handled the same way as managed pointers
            if (thisType.IsPointer && otherType.IsPointer)
            {
                return AreVerificationTypesEqual(thisType.GetParameterType(), otherType.GetParameterType());
            }

            // Function pointers are compatible only if they are method-signature-compatible-with as described in ECMA I.8.7.1
            if (thisType.IsFunctionPointer && otherType.IsFunctionPointer)
            {
                return IsMethodSignatureCompatibleWith(thisType, otherType);
            }

            // None of the types can be a managed pointer, a pointer or a function pointer here,
            // all the valid cases were handled above.
            if (thisType.IsByRef || otherType.IsByRef ||
                thisType.IsPointer || otherType.IsPointer ||
                thisType.IsFunctionPointer || otherType.IsFunctionPointer)
            {
                return false;
            }

            // Nullable<T> can be cast to T, but this is not compatible according to ECMA I.8.7.1
            bool isCastFromNullableOfTtoT = thisType.IsNullable && otherType.IsEquivalentTo(thisType.Instantiation[0]);
            if (isCastFromNullableOfTtoT)
            {
                return false;
            }

            return otherType.CanCastTo(thisType);
        }

        private static bool IsEquivalentTo(this TypeDesc thisType, TypeDesc otherType)
        {
            // TODO: Once type equivalence is implemented, this implementation needs to be enhanced to honor it.
            return thisType == otherType;
        }

        private static bool CanCastToInternal(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            if (thisType == otherType)
            {
                return true;
            }

            switch (thisType.Category)
            {
                case TypeFlags.GenericParameter:
                    return ((GenericParameterDesc)thisType).CanCastGenericParameterTo(otherType, protect);

                case TypeFlags.Array:
                case TypeFlags.SzArray:
                    return ((ArrayType)thisType).CanCastArrayTo(otherType, protect);

                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                    if (otherType.Category == thisType.Category)
                    {
                        return ((ParameterizedType)thisType).CanCastParamTo(((ParameterizedType)otherType).ParameterType, protect);
                    }
                    return false;

                case TypeFlags.FunctionPointer:
                    return false;

                default:
                    Debug.Assert(thisType.IsDefType);
                    return thisType.CanCastToClassOrInterface(otherType, protect);
            }
        }

        private static bool CanCastGenericParameterTo(this GenericParameterDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            // A boxed variable type can be cast to any of its constraints, or object, if none are specified
            if (otherType.IsObject)
            {
                return true;
            }

            if (thisType.HasNotNullableValueTypeConstraint &&
                otherType.IsWellKnownType(WellKnownType.ValueType))
            {
                return true;
            }

            foreach (var typeConstraint in thisType.TypeConstraints)
            {
                if (typeConstraint.CanCastToInternal(otherType, protect))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanCastArrayTo(this ArrayType thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            // Casting the array to one of the base types or interfaces?
            if (otherType.IsDefType)
            {
                return thisType.CanCastToClassOrInterface(otherType, protect);
            }

            // Casting array to something else (between SzArray and Array, for example)?
            if (thisType.Category != otherType.Category)
            {
                // An SzArray is castable to MdArray rank 1. We follow the same casting rules as SzArray to SzArray.
                if (thisType.Category == TypeFlags.SzArray
                    && otherType.Category == TypeFlags.Array
                    && ((ArrayType)otherType).Rank == 1)
                {
                    return thisType.CanCastParamTo(((ArrayType)otherType).ParameterType, protect);
                }

                return false;
            }

            ArrayType otherArrayType = (ArrayType)otherType;

            // Check ranks if we're casting multidim arrays
            if (!thisType.IsSzArray && thisType.Rank != otherArrayType.Rank)
            {
                return false;
            }

            return thisType.CanCastParamTo(otherArrayType.ParameterType, protect);
        }

        private static bool CanCastParamTo(this ParameterizedType thisType, TypeDesc paramType, StackOverflowProtect protect)
        {
            // While boxed value classes inherit from object their
            // unboxed versions do not.  Parameterized types have the
            // unboxed version, thus, if the from type parameter is value
            // class then only an exact match/equivalence works.
            if (thisType.ParameterType == paramType)
            {
                return true;
            }

            TypeDesc curTypesParm = thisType.ParameterType;

            // Object parameters don't need an exact match but only inheritance, check for that
            TypeDesc fromParamUnderlyingType = curTypesParm.UnderlyingType;
            if (fromParamUnderlyingType.IsGCPointer)
            {
                return curTypesParm.CanCastToInternal(paramType, protect);
            }
            else if (curTypesParm.IsGenericParameter)
            {
                var genericVariableFromParam = (GenericParameterDesc)curTypesParm;
                if (genericVariableFromParam.HasReferenceTypeConstraint || IsConstrainedAsGCPointer(genericVariableFromParam))
                {
                    return genericVariableFromParam.CanCastToInternal(paramType, protect);
                }
            }
            else if (fromParamUnderlyingType.IsPrimitive)
            {
                TypeDesc toParamUnderlyingType = paramType.UnderlyingType;
                if (GetNormalizedIntegralArrayElementType(fromParamUnderlyingType) == GetNormalizedIntegralArrayElementType(toParamUnderlyingType))
                {
                    return true;
                }
            }

            // Anything else is not a match
            return false;
        }

        private static bool IsConstrainedAsGCPointer(GenericParameterDesc type)
        {
            foreach (var typeConstraint in type.TypeConstraints)
            {
                if (typeConstraint.IsGenericParameter)
                {
                    if (IsConstrainedAsGCPointer((GenericParameterDesc)typeConstraint))
                        return true;
                }

                if (!typeConstraint.IsInterface && typeConstraint.IsGCPointer)
                {
                    // Object, ValueType, and Enum are GCPointers but they do not constrain the type to GCPointer!
                    if (!typeConstraint.IsWellKnownType(WellKnownType.Object) &&
                        !typeConstraint.IsWellKnownType(WellKnownType.ValueType) &&
                        !typeConstraint.IsWellKnownType(WellKnownType.Enum))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static TypeFlags GetNormalizedIntegralArrayElementType(TypeDesc type)
        {
            Debug.Assert(!type.IsEnum);

            // Primitive types such as E_T_I4 and E_T_U4 are interchangeable
            // Enums with interchangeable underlying types are interchangeable
            // BOOL is NOT interchangeable with I1/U1, neither CHAR -- with I2/U2
            // Float and double are not interchangeable here.

            TypeFlags elementType = type.Category;
            switch (elementType)
            {
                case TypeFlags.Byte:
                case TypeFlags.UInt16:
                case TypeFlags.UInt32:
                case TypeFlags.UInt64:
                case TypeFlags.UIntPtr:
                    return elementType - 1;
            }

            return elementType;
        }


        public static bool IsArrayElementTypeCastableBySize(TypeDesc elementType)
        {
            switch (elementType.UnderlyingType.Category)
            {
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                case TypeFlags.UInt32:
                case TypeFlags.Int32:
                case TypeFlags.UInt64:
                case TypeFlags.Int64:
                case TypeFlags.UIntPtr:
                case TypeFlags.IntPtr:
                    return true;
            }

            return false;
        }

        private static bool CanCastToClassOrInterface(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            if (otherType.IsInterface)
            {
                return thisType.CanCastToInterface(otherType, protect);
            }
            else
            {
                return thisType.CanCastToClass(otherType, protect);
            }
        }

        private static bool CanCastToInterface(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            if (!otherType.HasVariance)
            {
                return thisType.CanCastToNonVariantInterface(otherType, protect);
            }
            else
            {
                if (thisType.CanCastByVarianceToInterfaceOrDelegate(otherType, protect))
                {
                    return true;
                }

                foreach (var interfaceType in thisType.RuntimeInterfaces)
                {
                    if (interfaceType.CanCastByVarianceToInterfaceOrDelegate(otherType, protect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CanCastToNonVariantInterface(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            if (otherType == thisType)
            {
                return true;
            }

            foreach (var interfaceType in thisType.RuntimeInterfaces)
            {
                if (interfaceType == otherType)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CanCastByVarianceToInterfaceOrDelegate(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protectInput)
        {
            if (!thisType.HasSameTypeDefinition(otherType))
            {
                return false;
            }

            var stackOverflowProtectKey = new CastingPair(thisType, otherType);
            if (protectInput != null)
            {
                if (protectInput.Contains(stackOverflowProtectKey))
                    return false;
            }

            StackOverflowProtect protect = new StackOverflowProtect(stackOverflowProtectKey, protectInput);

            Instantiation instantiationThis = thisType.Instantiation;
            Instantiation instantiationTarget = otherType.Instantiation;
            Instantiation instantiationOpen = thisType.GetTypeDefinition().Instantiation;

            Debug.Assert(instantiationThis.Length == instantiationTarget.Length &&
                instantiationThis.Length == instantiationOpen.Length);

            for (int i = 0; i < instantiationThis.Length; i++)
            {
                TypeDesc arg = instantiationThis[i];
                TypeDesc targetArg = instantiationTarget[i];

                if (arg != targetArg)
                {
                    GenericParameterDesc openArgType = (GenericParameterDesc)instantiationOpen[i];

                    switch (openArgType.Variance)
                    {
                        case GenericVariance.Covariant:
                            if (!arg.IsBoxedAndCanCastTo(targetArg, protect))
                                return false;
                            break;

                        case GenericVariance.Contravariant:
                            if (!targetArg.IsBoxedAndCanCastTo(arg, protect))
                                return false;
                            break;

                        default:
                            // non-variant
                            Debug.Assert(openArgType.Variance == GenericVariance.None);
                            return false;
                    }
                }
            }

            return true;
        }

        private static bool CanCastToClass(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            TypeDesc curType = thisType;

            if (curType.IsInterface && otherType.IsObject)
            {
                return true;
            }

            // If the target type has variant type parameters, we take a slower path
            if (curType.HasVariance)
            {
                // First chase inheritance hierarchy until we hit a class that only differs in its instantiation
                do
                {
                    if (curType == otherType)
                    {
                        return true;
                    }

                    if (curType.CanCastByVarianceToInterfaceOrDelegate(otherType, protect))
                    {
                        return true;
                    }

                    curType = curType.BaseType;
                }
                while (curType != null);
            }
            else
            {
                // If there are no variant type parameters, just chase the hierarchy

                // Allow curType to be nullable, which means this method
                // will additionally return true if curType is Nullable<T> && (
                //    currType == otherType
                // OR otherType is System.ValueType or System.Object)

                // Always strip Nullable from the otherType, if present
                if (otherType.IsNullable && !curType.IsNullable)
                {
                    return thisType.CanCastTo(otherType.Instantiation[0]);
                }

                do
                {
                    if (curType == otherType)
                        return true;

                    curType = curType.BaseType;
                } while (curType != null);
            }

            return false;
        }

        private static bool IsBoxedAndCanCastTo(this TypeDesc thisType, TypeDesc otherType, StackOverflowProtect protect)
        {
            TypeDesc fromUnderlyingType = thisType.UnderlyingType;

            if (fromUnderlyingType.IsGCPointer)
            {
                return thisType.CanCastToInternal(otherType, protect);
            }
            else if (thisType.IsGenericParameter)
            {
                var genericVariableFromParam = (GenericParameterDesc)thisType;
                if (genericVariableFromParam.HasReferenceTypeConstraint || IsConstrainedAsGCPointer(genericVariableFromParam))
                {
                    return genericVariableFromParam.CanCastToInternal(otherType, protect);
                }
            }

            return false;
        }

        private sealed class StackOverflowProtect
        {
            private CastingPair _value;
            private StackOverflowProtect _previous;

            public StackOverflowProtect(CastingPair value, StackOverflowProtect previous)
            {
                _value = value;
                _previous = previous;
            }

            public bool Contains(CastingPair value)
            {
                for (var current = this; current != null; current = current._previous)
                    if (current._value.Equals(value))
                        return true;
                return false;
            }
        }

        private struct CastingPair
        {
            public readonly TypeDesc FromType;
            public readonly TypeDesc ToType;

            public CastingPair(TypeDesc fromType, TypeDesc toType)
            {
                FromType = fromType;
                ToType = toType;
            }

            public bool Equals(CastingPair other) => FromType == other.FromType && ToType == other.ToType;
        }
    }
}
