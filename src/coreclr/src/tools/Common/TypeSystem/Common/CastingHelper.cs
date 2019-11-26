// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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
                if (genericVariableFromParam.HasReferenceTypeConstraint)
                {
                    return genericVariableFromParam.CanCastToInternal(paramType, protect);
                }
            }
            else if (fromParamUnderlyingType.IsPrimitive)
            {
                TypeDesc toParamUnderlyingType = paramType.UnderlyingType;
                if (toParamUnderlyingType.IsPrimitive)
                {
                    if (toParamUnderlyingType == fromParamUnderlyingType)
                    {
                        return true;
                    }

                    if (ArePrimitveTypesEquivalentSize(fromParamUnderlyingType, toParamUnderlyingType))
                    {
                        return true;
                    }
                }
            }

            // Anything else is not a match
            return false;
        }

        // Returns true of the two types are equivalent primitive types. Used by array casts.
        private static bool ArePrimitveTypesEquivalentSize(TypeDesc type1, TypeDesc type2)
        {
            Debug.Assert(type1.IsPrimitive && type2.IsPrimitive);

            // Primitive types such as E_T_I4 and E_T_U4 are interchangeable
            // Enums with interchangeable underlying types are interchangable
            // BOOL is NOT interchangeable with I1/U1, neither CHAR -- with I2/U2
            // Float and double are not interchangable here.

            int sourcePrimitiveTypeEquivalenceSize = type1.GetIntegralTypeMatchSize();

            // Quick check to see if the first type can be matched.
            if (sourcePrimitiveTypeEquivalenceSize == 0)
            {
                return false;
            }

            int targetPrimitiveTypeEquivalenceSize = type2.GetIntegralTypeMatchSize();

            return sourcePrimitiveTypeEquivalenceSize == targetPrimitiveTypeEquivalenceSize;
        }

        private static int GetIntegralTypeMatchSize(this TypeDesc type)
        {
            Debug.Assert(type.IsPrimitive);

            switch (type.Category)
            {
                case TypeFlags.SByte:
                case TypeFlags.Byte:
                    return 1;
                case TypeFlags.UInt16:
                case TypeFlags.Int16:
                    return 2;
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                    return 4;
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                    return 8;
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                    return type.Context.Target.PointerSize;
                default:
                    return 0;
            }
        }

        public static bool IsArrayElementTypeCastableBySize(TypeDesc elementType)
        {
            TypeDesc underlyingType = elementType.UnderlyingType;
            return underlyingType.IsPrimitive && GetIntegralTypeMatchSize(underlyingType) != 0;
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
                return thisType.CanCastToNonVariantInterface(otherType,protect);
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
                if (genericVariableFromParam.HasReferenceTypeConstraint)
                {
                    return genericVariableFromParam.CanCastToInternal(otherType, protect);
                }
            }

            return false;
        }

        private class StackOverflowProtect
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
