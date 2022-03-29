// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System.Runtime
{
    /////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //                                    **** WARNING ****
    //
    // A large portion of the logic present in this file is duplicated
    // in src\System.Private.Reflection.Execution\Internal\Reflection\Execution\TypeLoader\TypeCast.cs
    // (for dynamic type builder). If you make changes here make sure they are reflected there.
    //
    //                                    **** WARNING ****
    //
    /////////////////////////////////////////////////////////////////////////////////////////////////////

    internal static class TypeCast
    {
        [Flags]
        internal enum AssignmentVariation
        {
            Normal = 0,

            /// <summary>
            /// Assume the source type is boxed so that value types and enums are compatible with Object, ValueType
            /// and Enum (if applicable)
            /// </summary>
            BoxedSource = 1,

            /// <summary>
            /// Allow identically sized integral types and enums to be considered equivalent (currently used only for
            /// array element types)
            /// </summary>
            AllowSizeEquivalence = 2,
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfClass")]
        public static unsafe object IsInstanceOfClass(MethodTable* pTargetType, object obj)
        {
            if (obj == null || obj.MethodTable == pTargetType)
            {
                return obj;
            }

            MethodTable* pObjType = obj.MethodTable;

            Debug.Assert(!pTargetType->IsParameterizedType, "IsInstanceOfClass called with parameterized MethodTable");
            Debug.Assert(!pTargetType->IsInterface, "IsInstanceOfClass called with interface MethodTable");

            // Quick check if both types are good for simple casting: canonical, no related type via IAT, no generic variance
            if (Internal.Runtime.MethodTable.BothSimpleCasting(pObjType, pTargetType))
            {
                // walk the type hierarchy looking for a match
                do
                {
                    pObjType = pObjType->RawBaseType;

                    if (pObjType == null)
                    {
                        return null;
                    }

                    if (pObjType == pTargetType)
                    {
                        return obj;
                    }
                }
                while (pObjType->SimpleCasting());
            }

            return IsInstanceOfClass_Helper(pTargetType, obj, pObjType);
        }

        private static unsafe object IsInstanceOfClass_Helper(MethodTable* pTargetType, object obj, MethodTable* pObjType)
        {
            if (pTargetType->IsCloned)
            {
                pTargetType = pTargetType->CanonicalEEType;
            }

            if (pObjType->IsCloned)
            {
                pObjType = pObjType->CanonicalEEType;
            }

            // if the EETypes pointers match, we're done
            if (pObjType == pTargetType)
            {
                return obj;
            }

            if (pTargetType->HasGenericVariance && pObjType->HasGenericVariance)
            {
                // Only generic interfaces and delegates can have generic variance and we shouldn't see
                // interfaces for either input here. So if the canonical types are marked as having variance
                // we know we've hit the delegate case. We've dealt with the identical case just above. And
                // the regular path below will handle casting to Object, Delegate and MulticastDelegate. Since
                // we don't support deriving from user delegate classes any further all we have to check here
                // is that the uninstantiated generic delegate definitions are the same and the type
                // parameters are compatible.

                // NOTE: using general assignable path for the cache because of the cost of the variance checks
                if (CastCache.AreTypesAssignableInternal(pObjType, pTargetType, AssignmentVariation.BoxedSource, null))
                    return obj;
                return null;
            }

            if (pObjType->IsArray)
            {
                // arrays can be cast to System.Object
                if (WellKnownEETypes.IsSystemObject(pTargetType))
                {
                    return obj;
                }

                // arrays can be cast to System.Array
                if (WellKnownEETypes.IsSystemArray(pTargetType))
                {
                    return obj;
                }

                return null;
            }


            // walk the type hierarchy looking for a match
            while (true)
            {
                pObjType = pObjType->NonClonedNonArrayBaseType;
                if (pObjType == null)
                {
                    return null;
                }

                if (pObjType->IsCloned)
                    pObjType = pObjType->CanonicalEEType;

                if (pObjType == pTargetType)
                {
                    return obj;
                }
            }
        }

        [RuntimeExport("RhTypeCast_CheckCastClass")]
        public static unsafe object CheckCastClass(MethodTable* pTargetEEType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
                return null;

            object result = IsInstanceOfClass(pTargetEEType, obj);

            if (result == null)
            {
                // Throw the invalid cast exception defined by the classlib, using the input MethodTable*
                // to find the correct classlib.

                throw pTargetEEType->GetClasslibException(ExceptionIDs.InvalidCast);
            }

            return result;
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfArray")]
        public static unsafe object IsInstanceOfArray(MethodTable* pTargetType, object obj)
        {
            if (obj == null)
            {
                return null;
            }

            MethodTable* pObjType = obj.MethodTable;

            Debug.Assert(pTargetType->IsArray, "IsInstanceOfArray called with non-array MethodTable");
            Debug.Assert(!pTargetType->IsCloned, "cloned array types are disallowed");

            // if the types match, we are done
            if (pObjType == pTargetType)
            {
                return obj;
            }

            // if the object is not an array, we're done
            if (!pObjType->IsArray)
            {
                return null;
            }

            Debug.Assert(!pObjType->IsCloned, "cloned array types are disallowed");

            // compare the array types structurally

            if (pObjType->ParameterizedTypeShape != pTargetType->ParameterizedTypeShape)
            {
                // If the shapes are different, there's one more case to check for: Casting SzArray to MdArray rank 1.
                if (!pObjType->IsSzArray || pTargetType->ArrayRank != 1)
                {
                    return null;
                }
            }

            if (CastCache.AreTypesAssignableInternal(pObjType->RelatedParameterType, pTargetType->RelatedParameterType,
                AssignmentVariation.AllowSizeEquivalence, null))
            {
                return obj;
            }

            return null;
        }

        [RuntimeExport("RhTypeCast_CheckCastArray")]
        public static unsafe object CheckCastArray(MethodTable* pTargetEEType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
                return null;

            object result = IsInstanceOfArray(pTargetEEType, obj);

            if (result == null)
            {
                // Throw the invalid cast exception defined by the classlib, using the input MethodTable*
                // to find the correct classlib.

                throw pTargetEEType->GetClasslibException(ExceptionIDs.InvalidCast);
            }

            return result;
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfInterface")]
        public static unsafe object IsInstanceOfInterface(MethodTable* pTargetType, object obj)
        {
            if (obj == null)
            {
                return null;
            }

            MethodTable* pObjType = obj.MethodTable;

            if (CastCache.AreTypesAssignableInternal_SourceNotTarget_BoxedSource(pObjType, pTargetType, null))
                return obj;

            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (pObjType->IsIDynamicInterfaceCastable && IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: false))
                return obj;

            return null;
        }

        private static unsafe bool IsInstanceOfInterfaceViaIDynamicInterfaceCastable(MethodTable* pTargetType, object obj, bool throwing)
        {
            var pfnIsInterfaceImplemented = (delegate*<object, MethodTable*, bool, bool>)
                pTargetType->GetClasslibFunction(ClassLibFunctionId.IDynamicCastableIsInterfaceImplemented);
            return pfnIsInterfaceImplemented(obj, pTargetType, throwing);
        }

        internal static unsafe bool ImplementsInterface(MethodTable* pObjType, MethodTable* pTargetType, EETypePairList* pVisited)
        {
            Debug.Assert(!pTargetType->IsParameterizedType, "did not expect paramterized type");
            Debug.Assert(pTargetType->IsInterface, "IsInstanceOfInterface called with non-interface MethodTable");

            // This can happen with generic interface types
            // Debug.Assert(!pTargetType->IsCloned, "cloned interface types are disallowed");

            // canonicalize target type
            if (pTargetType->IsCloned)
                pTargetType = pTargetType->CanonicalEEType;

            int numInterfaces = pObjType->NumInterfaces;
            EEInterfaceInfo* interfaceMap = pObjType->InterfaceMap;
            for (int i = 0; i < numInterfaces; i++)
            {
                MethodTable* pInterfaceType = interfaceMap[i].InterfaceType;

                // canonicalize the interface type
                if (pInterfaceType->IsCloned)
                    pInterfaceType = pInterfaceType->CanonicalEEType;

                if (pInterfaceType == pTargetType)
                {
                    return true;
                }
            }

            // We did not find the interface type in the list of supported interfaces. There's still one
            // chance left: if the target interface is generic and one or more of its type parameters is co or
            // contra variant then the object can still match if it implements a different instantiation of
            // the interface with type compatible generic arguments.
            //
            // Interfaces which are only variant for arrays have the HasGenericVariance flag set even if they
            // are not variant.
            bool fArrayCovariance = pObjType->IsArray;
            if (pTargetType->HasGenericVariance)
            {
                // Grab details about the instantiation of the target generic interface.
                MethodTable* pTargetGenericType = pTargetType->GenericDefinition;
                EETypeRef* pTargetInstantiation = pTargetType->GenericArguments;
                int targetArity = (int)pTargetType->GenericArity;
                GenericVariance* pTargetVarianceInfo = pTargetType->GenericVariance;

                Debug.Assert(pTargetVarianceInfo != null, "did not expect empty variance info");


                for (int i = 0; i < numInterfaces; i++)
                {
                    MethodTable* pInterfaceType = interfaceMap[i].InterfaceType;

                    // We can ignore interfaces which are not also marked as having generic variance
                    // unless we're dealing with array covariance.
                    //
                    // Interfaces which are only variant for arrays have the HasGenericVariance flag set even if they
                    // are not variant.
                    if (pInterfaceType->HasGenericVariance)
                    {
                        MethodTable* pInterfaceGenericType = pInterfaceType->GenericDefinition;

                        // If the generic types aren't the same then the types aren't compatible.
                        if (pInterfaceGenericType != pTargetGenericType)
                            continue;

                        // Grab instantiation details for the candidate interface.
                        EETypeRef* pInterfaceInstantiation = pInterfaceType->GenericArguments;
                        int interfaceArity = (int)pInterfaceType->GenericArity;
                        GenericVariance* pInterfaceVarianceInfo = pInterfaceType->GenericVariance;

                        Debug.Assert(pInterfaceVarianceInfo != null, "did not expect empty variance info");

                        // The types represent different instantiations of the same generic type. The
                        // arity of both had better be the same.
                        Debug.Assert(targetArity == interfaceArity, "arity mismatch betweeen generic instantiations");

                        // Compare the instantiations to see if they're compatible taking variance into account.
                        if (TypeParametersAreCompatible(targetArity,
                                                        pInterfaceInstantiation,
                                                        pTargetInstantiation,
                                                        pTargetVarianceInfo,
                                                        fArrayCovariance,
                                                        pVisited))
                            return true;
                    }
                }
            }

            return false;
        }

        // Compare two types to see if they are compatible via generic variance.
        private static unsafe bool TypesAreCompatibleViaGenericVariance(MethodTable* pSourceType, MethodTable* pTargetType, EETypePairList* pVisited)
        {
            MethodTable* pTargetGenericType = pTargetType->GenericDefinition;
            MethodTable* pSourceGenericType = pSourceType->GenericDefinition;

            // If the generic types aren't the same then the types aren't compatible.
            if (pSourceGenericType == pTargetGenericType)
            {
                // Get generic instantiation metadata for both types.

                EETypeRef* pTargetInstantiation = pTargetType->GenericArguments;
                int targetArity = (int)pTargetType->GenericArity;
                GenericVariance* pTargetVarianceInfo = pTargetType->GenericVariance;

                Debug.Assert(pTargetVarianceInfo != null, "did not expect empty variance info");

                EETypeRef* pSourceInstantiation = pSourceType->GenericArguments;
                int sourceArity = (int)pSourceType->GenericArity;
                GenericVariance* pSourceVarianceInfo = pSourceType->GenericVariance;

                Debug.Assert(pSourceVarianceInfo != null, "did not expect empty variance info");

                // The types represent different instantiations of the same generic type. The
                // arity of both had better be the same.
                Debug.Assert(targetArity == sourceArity, "arity mismatch betweeen generic instantiations");

                // Compare the instantiations to see if they're compatible taking variance into account.
                if (TypeParametersAreCompatible(targetArity,
                                                pSourceInstantiation,
                                                pTargetInstantiation,
                                                pTargetVarianceInfo,
                                                false,
                                                pVisited))
                {
                    return true;
                }
            }

            return false;
        }

        // Compare two sets of generic type parameters to see if they're assignment compatible taking generic
        // variance into account. It's assumed they've already had their type definition matched (which
        // implies their arities are the same as well). The fForceCovariance argument tells the method to
        // override the defined variance of each parameter and instead assume it is covariant. This is used to
        // implement covariant array interfaces.
        internal static unsafe bool TypeParametersAreCompatible(int arity,
                                                               EETypeRef* pSourceInstantiation,
                                                               EETypeRef* pTargetInstantiation,
                                                               GenericVariance* pVarianceInfo,
                                                               bool fForceCovariance,
                                                               EETypePairList* pVisited)
        {
            // Walk through the instantiations comparing the cast compatibility of each pair
            // of type args.
            for (int i = 0; i < arity; i++)
            {
                MethodTable* pTargetArgType = pTargetInstantiation[i].Value;
                MethodTable* pSourceArgType = pSourceInstantiation[i].Value;

                GenericVariance varType;
                if (fForceCovariance)
                    varType = GenericVariance.ArrayCovariant;
                else
                    varType = pVarianceInfo[i];

                switch (varType)
                {
                    case GenericVariance.NonVariant:
                        // Non-variant type params need to be identical.

                        if (!AreTypesEquivalent(pSourceArgType, pTargetArgType))
                            return false;

                        break;

                    case GenericVariance.Covariant:
                        // For covariance (or out type params in C#) the object must implement an
                        // interface with a more derived type arg than the target interface. Or
                        // the object interface can have a type arg that is an interface
                        // implemented by the target type arg.
                        // For instance:
                        //   class Foo : ICovariant<String> is ICovariant<Object>
                        //   class Foo : ICovariant<Bar> is ICovariant<IBar>
                        //   class Foo : ICovariant<IBar> is ICovariant<Object>

                        if (!CastCache.AreTypesAssignableInternal(pSourceArgType, pTargetArgType, AssignmentVariation.Normal, pVisited))
                            return false;

                        break;

                    case GenericVariance.ArrayCovariant:
                        // For array covariance the object must be an array with a type arg
                        // that is more derived than that the target interface, or be a primitive
                        // (or enum) with the same size.
                        // For instance:
                        //   string[,,] is object[,,]
                        //   int[,,] is uint[,,]

                        // This call is just like the call for Covariance above except true is passed
                        // to the fAllowSizeEquivalence parameter to allow the int/uint matching to work
                        if (!CastCache.AreTypesAssignableInternal(pSourceArgType, pTargetArgType, AssignmentVariation.AllowSizeEquivalence, pVisited))
                            return false;

                        break;

                    case GenericVariance.Contravariant:
                        // For contravariance (or in type params in C#) the object must implement
                        // an interface with a less derived type arg than the target interface. Or
                        // the object interface can have a type arg that is a class implementing
                        // the interface that is the target type arg.
                        // For instance:
                        //   class Foo : IContravariant<Object> is IContravariant<String>
                        //   class Foo : IContravariant<IBar> is IContravariant<Bar>
                        //   class Foo : IContravariant<Object> is IContravariant<IBar>

                        if (!CastCache.AreTypesAssignableInternal(pTargetArgType, pSourceArgType, AssignmentVariation.Normal, pVisited))
                            return false;

                        break;

                    default:
                        Debug.Assert(false, "unknown generic variance type");
                        break;
                }
            }

            return true;
        }

        //
        // Determines if a value of the source type can be assigned to a location of the target type.
        // It does not handle IDynamicInterfaceCastable, and cannot since we do not have an actual object instance here.
        // This routine assumes that the source type is boxed, i.e. a value type source is presumed to be
        // compatible with Object and ValueType and an enum source is additionally compatible with Enum.
        //
        [RuntimeExport("RhTypeCast_AreTypesAssignable")]
        public static unsafe bool AreTypesAssignable(MethodTable* pSourceType, MethodTable* pTargetType)
        {
            // Special case: Generic Type definitions are not assignable in a mrt sense
            // in any way. Assignability of those types is handled by reflection logic.
            // Call this case out first and here so that these only somewhat filled in
            // types do not leak into the rest of the type casting logic.
            if (pTargetType->IsGenericTypeDefinition || pSourceType->IsGenericTypeDefinition)
            {
                return false;
            }

            // Special case: T can be cast to Nullable<T> (where T is a value type). Call this case out here
            // since this is only applicable if T is boxed, which is not true for any other callers of
            // AreTypesAssignableInternal, so no sense making all the other paths pay the cost of the check.
            if (pTargetType->IsNullable && pSourceType->IsValueType && !pSourceType->IsNullable)
            {
                MethodTable* pNullableType = pTargetType->NullableType;

                return AreTypesEquivalent(pSourceType, pNullableType);
            }

            return CastCache.AreTypesAssignableInternal(pSourceType, pTargetType, AssignmentVariation.BoxedSource, null);
        }

        // Internally callable version of the export method above. Has two additional flags:
        //  fBoxedSource            : assume the source type is boxed so that value types and enums are
        //                            compatible with Object, ValueType and Enum (if applicable)
        //  fAllowSizeEquivalence   : allow identically sized integral types and enums to be considered
        //                            equivalent (currently used only for array element types)
        internal static unsafe bool AreTypesAssignableInternal(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation, EETypePairList* pVisited)
        {
            bool fBoxedSource = ((variation & AssignmentVariation.BoxedSource) == AssignmentVariation.BoxedSource);
            bool fAllowSizeEquivalence = ((variation & AssignmentVariation.AllowSizeEquivalence) == AssignmentVariation.AllowSizeEquivalence);

            //
            // Are the types identical?
            //
            if (AreTypesEquivalent(pSourceType, pTargetType))
                return true;

            //
            // Handle cast to interface cases.
            //
            if (pTargetType->IsInterface)
            {
                // Value types can only be cast to interfaces if they're boxed.
                if (!fBoxedSource && pSourceType->IsValueType)
                    return false;

                if (ImplementsInterface(pSourceType, pTargetType, pVisited))
                    return true;

                // Are the types compatible due to generic variance?
                if (pTargetType->HasGenericVariance && pSourceType->HasGenericVariance)
                    return TypesAreCompatibleViaGenericVariance(pSourceType, pTargetType, pVisited);

                return false;
            }
            if (pSourceType->IsInterface)
            {
                // The only non-interface type an interface can be cast to is Object.
                return WellKnownEETypes.IsSystemObject(pTargetType);
            }

            //
            // Handle cast to array or pointer cases.
            //
            if (pTargetType->IsParameterizedType)
            {
                if (pSourceType->IsParameterizedType
                    && (pTargetType->ParameterizedTypeShape == pSourceType->ParameterizedTypeShape))
                {
                    // Source type is also a parameterized type. Are the parameter types compatible?
                    if (pSourceType->RelatedParameterType->IsPointerType)
                    {
                        // If the parameter types are pointers, then only exact matches are correct.
                        // As we've already called AreTypesEquivalent at the start of this function,
                        // return false as the exact match case has already been handled.
                        // int** is not compatible with uint**, nor is int*[] oompatible with uint*[].
                        return false;
                    }
                    else if (pSourceType->RelatedParameterType->IsByRefType)
                    {
                        // Only allow exact matches for ByRef types - same as pointers above. This should
                        // be unreachable and it's only a defense in depth. ByRefs can't be parameters
                        // of any parameterized type.
                        return false;
                    }
                    else
                    {
                        // Note that using AreTypesAssignableInternal with AssignmentVariation.AllowSizeEquivalence
                        // here handles array covariance as well as IFoo[] -> Foo[] etc.  We are not using
                        // AssignmentVariation.BoxedSource because int[] is not assignable to object[].
                        return CastCache.AreTypesAssignableInternal(pSourceType->RelatedParameterType,
                            pTargetType->RelatedParameterType, AssignmentVariation.AllowSizeEquivalence, pVisited);
                    }
                }

                // Can't cast a non-parameter type to a parameter type or a parameter type of different shape to a parameter type
                return false;
            }
            if (pSourceType->IsArray)
            {
                // Target type is not an array. But we can still cast arrays to Object or System.Array.
                return WellKnownEETypes.IsSystemObject(pTargetType) || WellKnownEETypes.IsSystemArray(pTargetType);
            }
            else if (pSourceType->IsParameterizedType)
            {
                return false;
            }

            //
            // Handle cast to other (non-interface, non-array) cases.
            //

            if (pSourceType->IsValueType)
            {
                // Certain value types of the same size are treated as equivalent when the comparison is
                // between array element types (indicated by fAllowSizeEquivalence). These are integer types
                // of the same size (e.g. int and uint) and the base type of enums vs all integer types of the
                // same size.
                if (fAllowSizeEquivalence && pTargetType->IsPrimitive)
                {
                    if (GetNormalizedIntegralArrayElementType(pSourceType) == GetNormalizedIntegralArrayElementType(pTargetType))
                        return true;

                    // Non-identical value types aren't equivalent in any other case (since value types are
                    // sealed).
                    return false;
                }

                // If the source type is a value type but it's not boxed then we've run out of options: the types
                // are not identical, the target type isn't an interface and we're not allowed to check whether
                // the target type is a parent of this one since value types are sealed and thus the only matches
                // would be against Object, ValueType or Enum, all of which are reference types and not compatible
                // with non-boxed value types.
                if (!fBoxedSource)
                    return false;
            }

            // Sub case of casting between two instantiations of the same delegate type where one or more of
            // the type parameters have variance. Only interfaces and delegate types can have variance over
            // their type parameters and we know that neither type is an interface due to checks above.
            if (pTargetType->HasGenericVariance && pSourceType->HasGenericVariance)
            {
                // We've dealt with the identical case at the start of this method. And the regular path below
                // will handle casting to Object, Delegate and MulticastDelegate. Since we don't support
                // deriving from user delegate classes any further all we have to check here is that the
                // uninstantiated generic delegate definitions are the same and the type parameters are
                // compatible.
                return TypesAreCompatibleViaGenericVariance(pSourceType, pTargetType, pVisited);
            }

            // Is the source type derived from the target type?
            if (IsDerived(pSourceType, pTargetType))
                return true;

            return false;
        }

        [RuntimeExport("RhTypeCast_CheckCastInterface")]
        public static unsafe object CheckCastInterface(MethodTable* pTargetType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
            {
                return null;
            }

            MethodTable* pObjType = obj.MethodTable;

            if (CastCache.AreTypesAssignableInternal_SourceNotTarget_BoxedSource(pObjType, pTargetType, null))
                return obj;

            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (pObjType->IsIDynamicInterfaceCastable
                && IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: true))
            {
                return obj;
            }

            // Throw the invalid cast exception defined by the classlib, using the input MethodTable* to find the
            // correct classlib.
            throw pTargetType->GetClasslibException(ExceptionIDs.InvalidCast);
        }

        [RuntimeExport("RhTypeCast_CheckArrayStore")]
        public static unsafe void CheckArrayStore(object array, object obj)
        {
            if (array == null || obj == null)
            {
                return;
            }

            Debug.Assert(array.MethodTable->IsArray, "first argument must be an array");

            MethodTable* arrayElemType = array.MethodTable->RelatedParameterType;
            if (CastCache.AreTypesAssignableInternal(obj.MethodTable, arrayElemType, AssignmentVariation.BoxedSource, null))
                return;

            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (obj.MethodTable->IsIDynamicInterfaceCastable && IsInstanceOfInterfaceViaIDynamicInterfaceCastable(arrayElemType, obj, throwing: false))
                return;

            // Throw the array type mismatch exception defined by the classlib, using the input array's MethodTable*
            // to find the correct classlib.

            throw array.MethodTable->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
        }

        [RuntimeExport("RhTypeCast_CheckVectorElemAddr")]
        public static unsafe void CheckVectorElemAddr(MethodTable* elemType, object array)
        {
            if (array == null)
            {
                return;
            }

            Debug.Assert(array.MethodTable->IsArray, "second argument must be an array");

            MethodTable* arrayElemType = array.MethodTable->RelatedParameterType;

            if (!AreTypesEquivalent(elemType, arrayElemType)
            // In addition to the exactness check, add another check to allow non-exact matches through
            // if the element type is a ValueType. The issue here is Universal Generics. The Universal
            // Generic codegen will generate a call to this helper for all ldelema opcodes if the exact
            // type is not known, and this can include ValueTypes. For ValueTypes, the exact check is not
            // desireable as enum's are allowed to pass through this code if they are size matched.
            // While this check is overly broad and allows non-enum valuetypes to also skip the check
            // that is OK, because in the non-enum case the casting operations are sufficient to ensure
            // type safety.
                && !elemType->IsValueType)
            {
                // Throw the array type mismatch exception defined by the classlib, using the input array's MethodTable*
                // to find the correct classlib.

                throw array.MethodTable->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
            }
        }

        internal struct ArrayElement
        {
            public object Value;
        }

        //
        // Array stelem/ldelema helpers with RyuJIT conventions
        //
        [RuntimeExport("RhpStelemRef")]
        public static unsafe void StelemRef(Array array, int index, object obj)
        {
            // This is supported only on arrays
            Debug.Assert(array.MethodTable->IsArray, "first argument must be an array");

#if INPLACE_RUNTIME
            // this will throw appropriate exceptions if array is null or access is out of range.
            ref object element = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
#else
            if (array is null)
            {
                // TODO: If both array and obj are null, we're likely going to throw Redhawk's NullReferenceException.
                //       This should blame the caller.
                throw obj.MethodTable->GetClasslibException(ExceptionIDs.NullReference);
            }
            if ((uint)index >= (uint)array.Length)
            {
                throw array.MethodTable->GetClasslibException(ExceptionIDs.IndexOutOfRange);
            }
            ref object rawData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(array).Data);
            ref object element = ref Unsafe.Add(ref rawData, index);
#endif

            MethodTable* elementType = array.MethodTable->RelatedParameterType;

            if (obj == null)
                goto assigningNull;

            if (elementType != obj.MethodTable)
                goto notExactMatch;

doWrite:
            InternalCalls.RhpAssignRef(ref element, obj);
            return;

assigningNull:
            element = null;
            return;

        notExactMatch:
#if INPLACE_RUNTIME
            // This optimization only makes sense for inplace runtime where there's only one System.Object.
            if (array.MethodTable == MethodTableOf<object[]>())
                goto doWrite;
#endif

            StelemRef_Helper(ref element, elementType, obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void StelemRef_Helper(ref object element, MethodTable* elementType, object obj)
        {
            if (CastCache.AreTypesAssignableInternal(obj.MethodTable, elementType, AssignmentVariation.BoxedSource, null))
            {
                InternalCalls.RhpAssignRef(ref element, obj);
            }
            else
            {
                // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
                // the interface.
                if (!obj.MethodTable->IsIDynamicInterfaceCastable || !IsInstanceOfInterfaceViaIDynamicInterfaceCastable(elementType, obj, throwing: false))
                {
                    // Throw the array type mismatch exception defined by the classlib, using the input array's
                    // MethodTable* to find the correct classlib.
                    throw elementType->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
                }
                InternalCalls.RhpAssignRef(ref element, obj);
            }
        }

        [RuntimeExport("RhpLdelemaRef")]
        public static unsafe ref object LdelemaRef(Array array, int index, IntPtr elementType)
        {
            Debug.Assert(array.MethodTable->IsArray, "first argument must be an array");

            MethodTable* elemType = (MethodTable*)elementType;
            MethodTable* arrayElemType = array.MethodTable->RelatedParameterType;

            if (!AreTypesEquivalent(elemType, arrayElemType))
            {
                // Throw the array type mismatch exception defined by the classlib, using the input array's MethodTable*
                // to find the correct classlib.

                throw array.MethodTable->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
            }

            ref object rawData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(array).Data);
            return ref Unsafe.Add(ref rawData, index);
        }

        internal static unsafe bool IsDerived(MethodTable* pDerivedType, MethodTable* pBaseType)
        {
            Debug.Assert(!pDerivedType->IsArray, "did not expect array type");
            Debug.Assert(!pDerivedType->IsParameterizedType, "did not expect parameterType");
            Debug.Assert(!pBaseType->IsArray, "did not expect array type");
            Debug.Assert(!pBaseType->IsInterface, "did not expect interface type");
            Debug.Assert(!pBaseType->IsParameterizedType, "did not expect parameterType");
            Debug.Assert(pBaseType->IsCanonical || pBaseType->IsCloned || pBaseType->IsGenericTypeDefinition, "unexpected MethodTable");
            Debug.Assert(pDerivedType->IsCanonical || pDerivedType->IsCloned || pDerivedType->IsGenericTypeDefinition, "unexpected MethodTable");

            // If a generic type definition reaches this function, then the function should return false unless the types are equivalent.
            // This works as the NonClonedNonArrayBaseType of a GenericTypeDefinition is always null.

            if (pBaseType->IsCloned)
                pBaseType = pBaseType->CanonicalEEType;

            do
            {
                if (pDerivedType->IsCloned)
                    pDerivedType = pDerivedType->CanonicalEEType;

                if (pDerivedType == pBaseType)
                    return true;

                pDerivedType = pDerivedType->NonClonedNonArrayBaseType;
            }
            while (pDerivedType != null);

            return false;
        }

        // Method to compare two types pointers for type equality
        // We cannot just compare the pointers as there can be duplicate type instances
        // for cloned and constructed types.
        // There are three separate cases here
        //   1. The pointers are Equal => true
        //   2. Either one or both the types are CLONED, follow to the canonical MethodTable and check
        //   3. For Arrays/Pointers, we have to further check for rank and element type equality
        [RuntimeExport("RhTypeCast_AreTypesEquivalent")]
        public static unsafe bool AreTypesEquivalent(MethodTable* pType1, MethodTable* pType2)
        {
            if (pType1 == pType2)
                return true;

            if (pType1->IsCloned)
                pType1 = pType1->CanonicalEEType;

            if (pType2->IsCloned)
                pType2 = pType2->CanonicalEEType;

            if (pType1 == pType2)
                return true;

            if (pType1->IsParameterizedType && pType2->IsParameterizedType)
                return AreTypesEquivalent(pType1->RelatedParameterType, pType2->RelatedParameterType) && pType1->ParameterizedTypeShape == pType2->ParameterizedTypeShape;

            return false;
        }

        // this is necessary for shared generic code - Foo<T> may be executing
        // for T being an interface, an array or a class
        [RuntimeExport("RhTypeCast_IsInstanceOf")]
        public static unsafe object IsInstanceOf(MethodTable* pTargetType, object obj)
        {
            // @TODO: consider using the cache directly, but beware of IDynamicInterfaceCastable in the interface case
            if (pTargetType->IsArray)
                return IsInstanceOfArray(pTargetType, obj);
            else if (pTargetType->IsInterface)
                return IsInstanceOfInterface(pTargetType, obj);
            else if (pTargetType->IsParameterizedType)
                return null; // We handled arrays above so this is for pointers and byrefs only.
            else
                return IsInstanceOfClass(pTargetType, obj);
        }

        [RuntimeExport("RhTypeCast_CheckCast")]
        public static unsafe object CheckCast(MethodTable* pTargetType, object obj)
        {
            // @TODO: consider using the cache directly, but beware of IDynamicInterfaceCastable in the interface case
            if (pTargetType->IsArray)
                return CheckCastArray(pTargetType, obj);
            else if (pTargetType->IsInterface)
                return CheckCastInterface(pTargetType, obj);
            else if (pTargetType->IsParameterizedType)
                return CheckCastNonArrayParameterizedType(pTargetType, obj);
            else
                return CheckCastClass(pTargetType, obj);
        }

        private static unsafe object CheckCastNonArrayParameterizedType(MethodTable* pTargetType, object obj)
        {
            // a null value can be cast to anything
            if (obj == null)
            {
                return null;
            }

            // Parameterized types are not boxable, so nothing can be an instance of these.
            throw pTargetType->GetClasslibException(ExceptionIDs.InvalidCast);
        }

        private static unsafe EETypeElementType GetNormalizedIntegralArrayElementType(MethodTable* type)
        {
            EETypeElementType elementType = type->ElementType;
            switch (elementType)
            {
                case EETypeElementType.Byte:
                case EETypeElementType.UInt16:
                case EETypeElementType.UInt32:
                case EETypeElementType.UInt64:
                case EETypeElementType.UIntPtr:
                    return elementType - 1;
            }

            return elementType;
        }

        internal unsafe struct EETypePairList
        {
            private MethodTable* _eetype1;
            private MethodTable* _eetype2;
            private EETypePairList* _next;

            public EETypePairList(MethodTable* pEEType1, MethodTable* pEEType2, EETypePairList* pNext)
            {
                _eetype1 = pEEType1;
                _eetype2 = pEEType2;
                _next = pNext;
            }

            public static bool Exists(EETypePairList* pList, MethodTable* pEEType1, MethodTable* pEEType2)
            {
                while (pList != null)
                {
                    if (pList->_eetype1 == pEEType1 && pList->_eetype2 == pEEType2)
                        return true;
                    if (pList->_eetype1 == pEEType2 && pList->_eetype2 == pEEType1)
                        return true;
                    pList = pList->_next;
                }
                return false;
            }
        }

        // source type + target type + assignment variation -> true/false
        [System.Runtime.CompilerServices.EagerStaticClassConstructionAttribute]
        private static class CastCache
        {
            //
            // Cache size parameters
            //

            // Start with small cache size so that the cache entries used by startup one-time only initialization
            // will get flushed soon
            private const int InitialCacheSize = 128; // MUST BE A POWER OF TWO
            private const int DefaultCacheSize = 1024;
            private const int MaximumCacheSize = 128 * 1024;

            //
            // Cache state
            //
            private static Entry[] s_cache = new Entry[InitialCacheSize];   // Initialize the cache eagerly to avoid null checks.
            private static UnsafeGCHandle s_previousCache;
            private static ulong s_tickCountOfLastOverflow = InternalCalls.RhpGetTickCount64();
            private static int s_entries;
            private static bool s_roundRobinFlushing;


            private sealed class Entry
            {
                public Entry Next;
                public Key Key;
                public bool Result;     // @TODO: consider storing this bit in the Key -- there is room
            }

            private unsafe struct Key
            {
                private IntPtr _sourceTypeAndVariation;
                private IntPtr _targetType;

                public Key(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation)
                {
                    Debug.Assert((((long)pSourceType) & 3) == 0, "misaligned MethodTable!");
                    Debug.Assert(((uint)variation) <= 3, "variation enum has an unexpectedly large value!");

                    _sourceTypeAndVariation = (IntPtr)(((byte*)pSourceType) + ((int)variation));
                    _targetType = (IntPtr)pTargetType;
                }

                private static int GetHashCode(IntPtr intptr)
                {
                    return unchecked((int)((long)intptr));
                }

                public int CalculateHashCode()
                {
                    return ((GetHashCode(_targetType) >> 4) ^ GetHashCode(_sourceTypeAndVariation));
                }

                public bool Equals(ref Key other)
                {
                    return (_sourceTypeAndVariation == other._sourceTypeAndVariation) && (_targetType == other._targetType);
                }

                public AssignmentVariation Variation
                {
                    get { return (AssignmentVariation)(unchecked((int)(long)_sourceTypeAndVariation) & 3); }
                }

                public MethodTable* SourceType { get { return (MethodTable*)(((long)_sourceTypeAndVariation) & ~3L); } }
                public MethodTable* TargetType { get { return (MethodTable*)_targetType; } }
            }

            public static unsafe bool AreTypesAssignableInternal(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation, EETypePairList* pVisited)
            {
                // Important special case -- it breaks infinite recursion in CastCache itself!
                if (pSourceType == pTargetType)
                    return true;

                Key key = new Key(pSourceType, pTargetType, variation);
                Entry entry = LookupInCache(s_cache, ref key);
                if (entry == null)
                    return CacheMiss(ref key, pVisited);

                return entry.Result;
            }

            // This method is an optimized and customized version of AreTypesAssignable that achieves better performance
            // than AreTypesAssignableInternal through 2 significant changes
            // 1. Removal of sourceType to targetType check (This propery must be known before calling this function. At time
            //    of writing, this is true as its is only used if sourceType is from an object, and targetType is an interface.)
            // 2. Force inlining (This particular variant is only used in a small number of dispatch scenarios that are particularly
            //    high in performance impact.)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe bool AreTypesAssignableInternal_SourceNotTarget_BoxedSource(MethodTable* pSourceType, MethodTable* pTargetType, EETypePairList* pVisited)
            {
                Debug.Assert(pSourceType != pTargetType, "target is source");
                Key key = new Key(pSourceType, pTargetType, AssignmentVariation.BoxedSource);
                Entry entry = LookupInCache(s_cache, ref key);
                if (entry == null)
                    return CacheMiss(ref key, pVisited);

                return entry.Result;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static Entry? LookupInCache(Entry[] cache, ref Key key)
            {
                int entryIndex = key.CalculateHashCode() & (cache.Length - 1);
                Entry entry = cache[entryIndex];
                while (entry != null)
                {
                    if (entry.Key.Equals(ref key))
                        break;
                    entry = entry.Next;
                }
                return entry;
            }

            private static unsafe bool CacheMiss(ref Key key, EETypePairList* pVisited)
            {
                //
                // First, check if we previously visited the input types pair, to avoid infinite recursions
                //
                if (EETypePairList.Exists(pVisited, key.SourceType, key.TargetType))
                    return false;

                bool result = false;
                bool previouslyCached = false;

                //
                // Try to find the entry in the previous version of the cache that is kept alive by weak reference
                //
                if (s_previousCache.IsAllocated)
                {
                    // Unchecked cast to avoid recursive dependency on array casting
                    Entry[] previousCache = Unsafe.As<Entry[]>(s_previousCache.Target);
                    if (previousCache != null)
                    {
                        Entry previousEntry = LookupInCache(previousCache, ref key);
                        if (previousEntry != null)
                        {
                            result = previousEntry.Result;
                            previouslyCached = true;
                        }
                    }
                }

                //
                // Call into the type cast code to calculate the result
                //
                if (!previouslyCached)
                {
                    EETypePairList newList = new EETypePairList(key.SourceType, key.TargetType, pVisited);
                    result = TypeCast.AreTypesAssignableInternal(key.SourceType, key.TargetType, key.Variation, &newList);
                }

                //
                // Update the cache under the lock
                //
                InternalCalls.RhpAcquireCastCacheLock();
                try
                {
                    try
                    {
                        // Avoid duplicate entries
                        Entry existingEntry = LookupInCache(s_cache, ref key);
                        if (existingEntry != null)
                            return existingEntry.Result;

                        // Resize cache as necessary
                        Entry[] cache = ResizeCacheForNewEntryAsNecessary();

                        int entryIndex = key.CalculateHashCode() & (cache.Length - 1);

                        Entry newEntry = new Entry() { Key = key, Result = result, Next = cache[entryIndex] };

                        // BEWARE: Array store check can lead to infinite recursion. We avoid this by making certain
                        // that the cache trivially answers the case of equivalent types without triggering the cache
                        // miss path. (See CastCache.AreTypesAssignableInternal)
                        cache[entryIndex] = newEntry;
                        return newEntry.Result;
                    }
                    catch (OutOfMemoryException)
                    {
                        // Entry allocation failed -- but we can still return the correct cast result.
                        return result;
                    }
                }
                finally
                {
                    InternalCalls.RhpReleaseCastCacheLock();
                }
            }

            private static Entry[] ResizeCacheForNewEntryAsNecessary()
            {
                Entry[] cache = s_cache;

                int entries = s_entries++;

                // If the cache has spare space, we are done
                if (2 * entries < cache.Length)
                {
                    if (s_roundRobinFlushing)
                    {
                        cache[2 * entries] = null;
                        cache[2 * entries + 1] = null;
                    }
                    return cache;
                }

                //
                // Now, we have cache that is overflowing with results. We need to decide whether to resize it or start
                // flushing the old entries instead
                //

                // Start over counting the entries
                s_entries = 0;

                // See how long it has been since the last time the cache was overflowing
                ulong tickCount = InternalCalls.RhpGetTickCount64();
                int tickCountSinceLastOverflow = (int)(tickCount - s_tickCountOfLastOverflow);
                s_tickCountOfLastOverflow = tickCount;

                bool shrinkCache = false;
                bool growCache = false;

                if (cache.Length < DefaultCacheSize)
                {
                    // If the cache have not reached the default size, just grow it without thinking about it much
                    growCache = true;
                }
                else
                {
                    if (tickCountSinceLastOverflow < cache.Length)
                    {
                        // We 'overflow' when 2*entries == cache.Length, so we have cache.Length / 2 entries that were
                        // filled in tickCountSinceLastOverflow ms, which is 2ms/entry

                        // If the fill rate of the cache is faster than ~2ms per entry, grow it
                        if (cache.Length < MaximumCacheSize)
                            growCache = true;
                    }
                    else
                    if (tickCountSinceLastOverflow > cache.Length * 16)
                    {
                        // We 'overflow' when 2*entries == cache.Length, so we have ((cache.Length*16) / 2) entries that
                        // were filled in tickCountSinceLastOverflow ms, which is 32ms/entry

                        // If the fill rate of the cache is slower than 32ms per entry, shrink it
                        if (cache.Length > DefaultCacheSize)
                            shrinkCache = true;
                    }
                    // Otherwise, keep the current size and just keep flushing the entries round robin
                }

                Entry[]? newCache = null;
                if (growCache || shrinkCache)
                {
                    try
                    {
                        newCache = new Entry[shrinkCache ? (cache.Length / 2) : (cache.Length * 2)];
                    }
                    catch (OutOfMemoryException)
                    {
                        // Failed to allocate a bigger/smaller cache.  That is fine, keep the old one.
                    }
                }

                if (newCache != null)
                {
                    s_roundRobinFlushing = false;

                    // Keep the reference to the old cache in a weak handle. We will try to use it to avoid hitting the
                    // cache miss path until the GC collects it.
                    if (s_previousCache.IsAllocated)
                    {
                        s_previousCache.Target = cache;
                    }
                    else
                    {
                        try
                        {
                            s_previousCache = UnsafeGCHandle.Alloc(cache, GCHandleType.Weak);
                        }
                        catch (OutOfMemoryException)
                        {
                            // Failed to allocate the handle to utilize the old cache, that is fine, we will just miss
                            // out on repopulating the new cache from the old cache.
                            s_previousCache = default(UnsafeGCHandle);
                        }
                    }

                    return s_cache = newCache;
                }
                else
                {
                    s_roundRobinFlushing = true;
                    return cache;
                }
            }
        }
    }
}
