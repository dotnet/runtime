// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.Runtime;

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

    [EagerStaticClassConstruction]
    internal static class TypeCast
    {
#if DEBUG
        private const int InitialCacheSize = 8;    // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 512;  // make this lower than release to make it easier to reach this in tests.
#else
        private const int InitialCacheSize = 128;  // MUST BE A POWER OF TWO
        private const int MaximumCacheSize = 4096; // 4096 * sizeof(CastCacheEntry) is 98304 bytes on 64bit. We will rarely need this much though.
#endif // DEBUG

        private static CastCache s_castCache = new CastCache(InitialCacheSize, MaximumCacheSize);

        [Flags]
        internal enum AssignmentVariation
        {
            /// <summary>
            /// Conversion from an object. In the terminology of ECMA335 the relationship is called "compatible-with".
            /// This is the relation used by castclass and isinst (III.4.3).
            /// Value types are compatible with Object, ValueType and Enum (if applicable) and implemented interfaces.
            /// </summary>
            BoxedSource = 0,

            /// <summary>
            /// Type compatibility of unboxed types. Used for checking compatibility of type parameters.
            /// Value types are compatible only if equivalent.
            /// </summary>
            Unboxed = 1,

            /// <summary>
            /// Allow identically sized integral types and enums to be considered equivalent.
            /// Used when checking type compatibility of array element types.
            /// </summary>
            AllowSizeEquivalence = 2,
        }

        // IsInstanceOf test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the IsInstanceOfInterface and IsInstanceOfClass functions,
        // this test must deal with all kinds of type tests
        [RuntimeExport("RhTypeCast_IsInstanceOfAny")]
        public static unsafe object? IsInstanceOfAny(MethodTable* pTargetType, object? obj)
        {
            if (obj != null)
            {
                MethodTable* mt = obj.GetMethodTable();
                if (mt != pTargetType)
                {
                    CastResult result = s_castCache.TryGet((nuint)mt + (int)AssignmentVariation.BoxedSource, (nuint)pTargetType);
                    if (result == CastResult.CanCast)
                    {
                        // do nothing
                    }
                    else if (result == CastResult.CannotCast)
                    {
                        obj = null;
                    }
                    else
                    {
                        goto slowPath;
                    }
                }
            }

            return obj;

        slowPath:
            // fall through to the slow helper
            return IsInstanceOfAny_NoCacheLookup(pTargetType, obj);
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfInterface")]
        public static unsafe object? IsInstanceOfInterface(MethodTable* pTargetType, object? obj)
        {
            Debug.Assert(pTargetType->IsInterface);
            Debug.Assert(!pTargetType->HasGenericVariance);

            const int unrollSize = 4;

            if (obj != null)
            {
                MethodTable* mt = obj.GetMethodTable();
                nint interfaceCount = mt->NumInterfaces;
                if (interfaceCount != 0)
                {
                    MethodTable** interfaceMap = mt->InterfaceMap;
                    if (interfaceCount < unrollSize)
                    {
                        // If not enough for unrolled, jmp straight to small loop
                        // as we already know there is one or more interfaces so don't need to check again.
                        goto few;
                    }

                    do
                    {
                        if (interfaceMap[0] == pTargetType ||
                            interfaceMap[1] == pTargetType ||
                            interfaceMap[2] == pTargetType ||
                            interfaceMap[3] == pTargetType)
                        {
                            goto done;
                        }

                        interfaceMap += unrollSize;
                        interfaceCount -= unrollSize;
                    } while (interfaceCount >= unrollSize);

                    if (interfaceCount == 0)
                    {
                        // If none remaining, skip the short loop
                        goto extra;
                    }

                few:
                    do
                    {
                        if (interfaceMap[0] == pTargetType)
                        {
                            goto done;
                        }

                        // Assign next offset
                        interfaceMap++;
                        interfaceCount--;
                    } while (interfaceCount > 0);

                extra:
                    if (mt->IsIDynamicInterfaceCastable)
                    {
                        goto slowPath;
                    }
                }

                obj = null;
            }

        done:

            return obj;

        slowPath:

            return IsInstanceOfInterface_Helper(pTargetType, obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe object? IsInstanceOfInterface_Helper(MethodTable* pTargetType, object? obj)
        {
            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (!IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: false))
                obj = null;

            return obj;
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfClass")]
        public static unsafe object? IsInstanceOfClass(MethodTable* pTargetType, object? obj)
        {
            Debug.Assert(!pTargetType->IsParameterizedType, "IsInstanceOfClass called with parameterized MethodTable");
            Debug.Assert(!pTargetType->IsFunctionPointer, "IsInstanceOfClass called with function pointer MethodTable");
            Debug.Assert(!pTargetType->IsInterface, "IsInstanceOfClass called with interface MethodTable");
            Debug.Assert(!pTargetType->HasGenericVariance, "IsInstanceOfClass with variant MethodTable");

            if (obj == null || obj.GetMethodTable() == pTargetType)
                return obj;

            if (!obj.GetMethodTable()->IsCanonical)
            {
                // Arrays should be the only non-canonical types that can exist on GC heap
                Debug.Assert(obj.GetMethodTable()->IsArray);

                // arrays can be cast to System.Object or System.Array
                if (WellKnownEETypes.IsValidArrayBaseType(pTargetType))
                    goto done;

                // They don't cast to any other class
                goto fail;
            }

            MethodTable* mt = obj.GetMethodTable()->NonArrayBaseType;
            for (; ; )
            {
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
            }

        fail:
            obj = null;

        done:
            return obj;
        }

        [RuntimeExport("RhTypeCast_IsInstanceOfException")]
        public static unsafe bool IsInstanceOfException(MethodTable* pTargetType, object? obj)
        {
            // Based on IsInstanceOfClass

            if (obj == null)
                return false;

            MethodTable* pObjType = obj.GetMethodTable();

            if (pObjType == pTargetType)
                return true;

            // arrays can be cast to System.Object and System.Array
            if (pObjType->IsArray)
                return WellKnownEETypes.IsValidArrayBaseType(pTargetType);

            while (true)
            {
                pObjType = pObjType->NonArrayBaseType;
                if (pObjType == null)
                    return false;

                if (pObjType == pTargetType)
                    return true;
            }
        }

        // ChkCast test used for unusual cases (naked type parameters, variant generic types)
        // Unlike the ChkCastInterface and ChkCastClass functions,
        // this test must deal with all kinds of type tests
        [RuntimeExport("RhTypeCast_CheckCastAny")]
        public static unsafe object CheckCastAny(MethodTable* pTargetType, object obj)
        {
            CastResult result;

            if (obj != null)
            {
                MethodTable* mt = obj.GetMethodTable();
                if (mt != pTargetType)
                {
                    result = s_castCache.TryGet((nuint)mt, (nuint)pTargetType);
                    if (result != CastResult.CanCast)
                    {
                        goto slowPath;
                    }
                }
            }

            return obj;

        slowPath:
            // fall through to the slow helper
            object objRet = CheckCastAny_NoCacheLookup(pTargetType, obj);
            // Make sure that the fast helper have not lied
            Debug.Assert(result != CastResult.CannotCast);
            return objRet;
        }

        [RuntimeExport("RhTypeCast_CheckCastInterface")]
        public static unsafe object CheckCastInterface(MethodTable* pTargetType, object obj)
        {
            Debug.Assert(pTargetType->IsInterface);
            Debug.Assert(!pTargetType->HasGenericVariance);

            const int unrollSize = 4;

            if (obj != null)
            {
                MethodTable* mt = obj.GetMethodTable();
                nint interfaceCount = mt->NumInterfaces;
                if (interfaceCount == 0)
                {
                    goto slowPath;
                }

                MethodTable** interfaceMap = mt->InterfaceMap;
                if (interfaceCount < unrollSize)
                {
                    // If not enough for unrolled, jmp straight to small loop
                    // as we already know there is one or more interfaces so don't need to check again.
                    goto few;
                }

                do
                {
                    if (interfaceMap[0] == pTargetType ||
                        interfaceMap[1] == pTargetType ||
                        interfaceMap[2] == pTargetType ||
                        interfaceMap[3] == pTargetType)
                    {
                        goto done;
                    }

                    // Assign next offset
                    interfaceMap += unrollSize;
                    interfaceCount -= unrollSize;
                } while (interfaceCount >= unrollSize);

                if (interfaceCount == 0)
                {
                    // If none remaining, skip the short loop
                    goto slowPath;
                }

            few:
                do
                {
                    if (interfaceMap[0] == pTargetType)
                    {
                        goto done;
                    }

                    // Assign next offset
                    interfaceMap++;
                    interfaceCount--;
                } while (interfaceCount > 0);

                goto slowPath;
            }

        done:
            return obj;

        slowPath:

            return CheckCastInterface_Helper(pTargetType, obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe object CheckCastInterface_Helper(MethodTable* pTargetType, object obj)
        {
            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (obj.GetMethodTable()->IsIDynamicInterfaceCastable
                && IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: true))
            {
                return obj;
            }

            // Throw the invalid cast exception defined by the classlib, using the input MethodTable* to find the
            // correct classlib.
            return ThrowInvalidCastException(pTargetType);
        }

        [RuntimeExport("RhTypeCast_CheckCastClass")]
        public static unsafe object CheckCastClass(MethodTable* pTargetType, object obj)
        {
            Debug.Assert(!pTargetType->IsParameterizedType, "CheckCastClass called with parameterized MethodTable");
            Debug.Assert(!pTargetType->IsFunctionPointer, "CheckCastClass called with function pointer MethodTable");
            Debug.Assert(!pTargetType->IsInterface, "CheckCastClass called with interface MethodTable");
            Debug.Assert(!pTargetType->HasGenericVariance, "CheckCastClass with variant MethodTable");

            if (obj == null || obj.GetMethodTable() == pTargetType)
            {
                return obj;
            }

            return CheckCastClassSpecial(pTargetType, obj);
        }

        // Optimized helper for classes. Assumes that the trivial cases
        // has been taken care of by the inlined check
        [RuntimeExport("RhTypeCast_CheckCastClassSpecial")]
        private static unsafe object CheckCastClassSpecial(MethodTable* pTargetType, object obj)
        {
            Debug.Assert(!pTargetType->IsParameterizedType, "CheckCastClass called with parameterized MethodTable");
            Debug.Assert(!pTargetType->IsFunctionPointer, "CheckCastClass called with function pointer MethodTable");
            Debug.Assert(!pTargetType->IsInterface, "CheckCastClass called with interface MethodTable");
            Debug.Assert(!pTargetType->HasGenericVariance, "CheckCastClass with variant MethodTable");

            MethodTable* mt = obj.GetMethodTable();
            Debug.Assert(mt != pTargetType, "The check for the trivial cases should be inlined by the JIT");

            if (!mt->IsCanonical)
            {
                // Arrays should be the only non-canonical types that can exist on GC heap
                Debug.Assert(mt->IsArray);

                // arrays can be cast to System.Object or System.Array
                if (WellKnownEETypes.IsValidArrayBaseType(pTargetType))
                    goto done;

                // They don't cast to any other class
                goto fail;
            }

            for (; ; )
            {
                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;

                mt = mt->NonArrayBaseType;
                if (mt == pTargetType)
                    goto done;

                if (mt == null)
                    break;
            }

            goto fail;

        done:
            return obj;

        fail:
            return ThrowInvalidCastException(pTargetType);
        }

        private static unsafe bool IsInstanceOfInterfaceViaIDynamicInterfaceCastable(MethodTable* pTargetType, object obj, bool throwing)
        {
            var pfnIsInterfaceImplemented = (delegate*<object, MethodTable*, bool, bool>)
                pTargetType->GetClasslibFunction(ClassLibFunctionId.IDynamicCastableIsInterfaceImplemented);
            return pfnIsInterfaceImplemented(obj, pTargetType, throwing);
        }

        internal static unsafe bool IsDerived(MethodTable* pDerivedType, MethodTable* pBaseType)
        {
            Debug.Assert(!pDerivedType->IsArray, "did not expect array type");
            Debug.Assert(!pDerivedType->IsParameterizedType, "did not expect parameterType");
            Debug.Assert(!pDerivedType->IsFunctionPointer, "did not expect function pointer");
            Debug.Assert(!pBaseType->IsArray, "did not expect array type");
            Debug.Assert(!pBaseType->IsInterface, "did not expect interface type");
            Debug.Assert(!pBaseType->IsParameterizedType, "did not expect parameterType");
            Debug.Assert(!pBaseType->IsFunctionPointer, "did not expect function pointer");
            Debug.Assert(pBaseType->IsCanonical || pBaseType->IsGenericTypeDefinition, "unexpected MethodTable");
            Debug.Assert(pDerivedType->IsCanonical || pDerivedType->IsGenericTypeDefinition, "unexpected MethodTable");

            // If a generic type definition reaches this function, then the function should return false unless the types are equivalent.
            // This works as the NonArrayBaseType of a GenericTypeDefinition is always null.

            do
            {
                if (pDerivedType == pBaseType)
                    return true;

                pDerivedType = pDerivedType->NonArrayBaseType;
            }
            while (pDerivedType != null);

            return false;
        }

        private static unsafe bool ImplementsInterface(MethodTable* pObjType, MethodTable* pTargetType, EETypePairList* pVisited)
        {
            Debug.Assert(!pTargetType->IsParameterizedType, "did not expect parameterized type");
            Debug.Assert(!pTargetType->IsFunctionPointer, "did not expect function pointer type");
            Debug.Assert(pTargetType->IsInterface, "IsInstanceOfInterface called with non-interface MethodTable");

            int numInterfaces = pObjType->NumInterfaces;
            MethodTable** interfaceMap = pObjType->InterfaceMap;
            for (int i = 0; i < numInterfaces; i++)
            {
                MethodTable* pInterfaceType = interfaceMap[i];
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
                MethodTableList targetInstantiation = pTargetType->GenericArguments;
                int targetArity = (int)pTargetType->GenericArity;
                GenericVariance* pTargetVarianceInfo = pTargetType->GenericVariance;

                Debug.Assert(pTargetVarianceInfo != null, "did not expect empty variance info");


                for (int i = 0; i < numInterfaces; i++)
                {
                    MethodTable* pInterfaceType = interfaceMap[i];

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
                        MethodTableList interfaceInstantiation = pInterfaceType->GenericArguments;
                        int interfaceArity = (int)pInterfaceType->GenericArity;
                        GenericVariance* pInterfaceVarianceInfo = pInterfaceType->GenericVariance;

                        Debug.Assert(pInterfaceVarianceInfo != null, "did not expect empty variance info");

                        // The types represent different instantiations of the same generic type. The
                        // arity of both had better be the same.
                        Debug.Assert(targetArity == interfaceArity, "arity mismatch between generic instantiations");

                        // Compare the instantiations to see if they're compatible taking variance into account.
                        if (TypeParametersAreCompatible(targetArity,
                                                        interfaceInstantiation,
                                                        targetInstantiation,
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

                MethodTableList targetInstantiation = pTargetType->GenericArguments;
                int targetArity = (int)pTargetType->GenericArity;
                GenericVariance* pTargetVarianceInfo = pTargetType->GenericVariance;

                Debug.Assert(pTargetVarianceInfo != null, "did not expect empty variance info");

                MethodTableList sourceInstantiation = pSourceType->GenericArguments;
                int sourceArity = (int)pSourceType->GenericArity;
                GenericVariance* pSourceVarianceInfo = pSourceType->GenericVariance;

                Debug.Assert(pSourceVarianceInfo != null, "did not expect empty variance info");

                // The types represent different instantiations of the same generic type. The
                // arity of both had better be the same.
                Debug.Assert(targetArity == sourceArity, "arity mismatch between generic instantiations");

                // Compare the instantiations to see if they're compatible taking variance into account.
                if (TypeParametersAreCompatible(targetArity,
                                                sourceInstantiation,
                                                targetInstantiation,
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
                                                               MethodTableList sourceInstantiation,
                                                               MethodTableList targetInstantiation,
                                                               GenericVariance* pVarianceInfo,
                                                               bool fForceCovariance,
                                                               EETypePairList* pVisited)
        {
            // Walk through the instantiations comparing the cast compatibility of each pair
            // of type args.
            for (int i = 0; i < arity; i++)
            {
                MethodTable* pTargetArgType = targetInstantiation[i];
                MethodTable* pSourceArgType = sourceInstantiation[i];

                GenericVariance varType;
                if (fForceCovariance)
                    varType = GenericVariance.ArrayCovariant;
                else
                    varType = pVarianceInfo[i];

                switch (varType)
                {
                    case GenericVariance.NonVariant:
                        // Non-variant type params need to be identical.

                        if (pSourceArgType != pTargetArgType)
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

                        if (!AreTypesAssignableInternal(pSourceArgType, pTargetArgType, AssignmentVariation.Unboxed, pVisited))
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
                        if (!AreTypesAssignableInternal(pSourceArgType, pTargetArgType, AssignmentVariation.AllowSizeEquivalence, pVisited))
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

                        if (!AreTypesAssignableInternal(pTargetArgType, pSourceArgType, AssignmentVariation.Unboxed, pVisited))
                            return false;

                        break;

                    default:
                        Debug.Assert(false, "unknown generic variance type");
                        break;
                }
            }

            return true;
        }

        [RuntimeExport("RhTypeCast_CheckArrayStore")]
        public static unsafe void CheckArrayStore(object array, object obj)
        {
            if (array == null || obj == null)
            {
                return;
            }

            Debug.Assert(array.GetMethodTable()->IsArray, "first argument must be an array");

            MethodTable* arrayElemType = array.GetMethodTable()->RelatedParameterType;
            if (AreTypesAssignableInternal(obj.GetMethodTable(), arrayElemType, AssignmentVariation.BoxedSource, null))
                return;

            // If object type implements IDynamicInterfaceCastable then there's one more way to check whether it implements
            // the interface.
            if (obj.GetMethodTable()->IsIDynamicInterfaceCastable && IsInstanceOfInterfaceViaIDynamicInterfaceCastable(arrayElemType, obj, throwing: false))
                return;

            // Throw the array type mismatch exception defined by the classlib, using the input array's MethodTable*
            // to find the correct classlib.

            throw array.GetMethodTable()->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
        }

        internal struct ArrayElement
        {
            public object Value;
        }

        //
        // Array stelem/ldelema helpers with RyuJIT conventions
        //
        [RuntimeExport("RhpStelemRef")]
        public static unsafe void StelemRef(Array array, nint index, object obj)
        {
            // This is supported only on arrays
            Debug.Assert(array.GetMethodTable()->IsArray, "first argument must be an array");

#if INPLACE_RUNTIME
            // this will throw appropriate exceptions if array is null or access is out of range.
            ref object element = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
#else
            if (array is null)
            {
                // TODO: If both array and obj are null, we're likely going to throw Redhawk's NullReferenceException.
                //       This should blame the caller.
                throw obj.GetMethodTable()->GetClasslibException(ExceptionIDs.NullReference);
            }
            if ((uint)index >= (uint)array.Length)
            {
                throw array.GetMethodTable()->GetClasslibException(ExceptionIDs.IndexOutOfRange);
            }
            ref object rawData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(array).Data);
            ref object element = ref Unsafe.Add(ref rawData, index);
#endif

            MethodTable* elementType = array.GetMethodTable()->RelatedParameterType;

            if (obj == null)
                goto assigningNull;

            if (elementType != obj.GetMethodTable())
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
            if (array.GetMethodTable() == MethodTable.Of<object[]>())
                goto doWrite;
#endif

            StelemRef_Helper(ref element, elementType, obj);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe void StelemRef_Helper(ref object element, MethodTable* elementType, object obj)
        {
            CastResult result = s_castCache.TryGet((nuint)obj.GetMethodTable() + (int)AssignmentVariation.BoxedSource, (nuint)elementType);
            if (result == CastResult.CanCast)
            {
                InternalCalls.RhpAssignRef(ref element, obj);
                return;
            }

            StelemRef_Helper_NoCacheLookup(ref element, elementType, obj);
        }

        private static unsafe void StelemRef_Helper_NoCacheLookup(ref object element, MethodTable* elementType, object obj)
        {
            object? castedObj = IsInstanceOfAny_NoCacheLookup(elementType, obj);
            if (castedObj != null)
            {
                InternalCalls.RhpAssignRef(ref element, obj);
                return;
            }

            // Throw the array type mismatch exception defined by the classlib, using the input array's
            // MethodTable* to find the correct classlib.
            throw elementType->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
        }

        [RuntimeExport("RhpLdelemaRef")]
        public static unsafe ref object LdelemaRef(Array array, nint index, IntPtr elementType)
        {
            Debug.Assert(array is null || array.GetMethodTable()->IsArray, "first argument must be an array");

#if INPLACE_RUNTIME
            // this will throw appropriate exceptions if array is null or access is out of range.
            ref object element = ref Unsafe.As<ArrayElement[]>(array)[index].Value;
#else
            if (array is null)
            {
                throw ((MethodTable*)elementType)->GetClasslibException(ExceptionIDs.NullReference);
            }
            if ((uint)index >= (uint)array.Length)
            {
                throw ((MethodTable*)elementType)->GetClasslibException(ExceptionIDs.IndexOutOfRange);
            }
            ref object rawData = ref Unsafe.As<byte, object>(ref Unsafe.As<RawArrayData>(array).Data);
            ref object element = ref Unsafe.Add(ref rawData, index);
#endif

            MethodTable* elemType = (MethodTable*)elementType;
            MethodTable* arrayElemType = array.GetMethodTable()->RelatedParameterType;

            if (elemType == arrayElemType)
            {
                return ref element;
            }

            return ref ThrowArrayMismatchException(array);
        }

        // This weird structure is for parity with CoreCLR - allows potentially to be tailcalled
        private static unsafe ref object ThrowArrayMismatchException(Array array)
        {
            // Throw the array type mismatch exception defined by the classlib, using the input array's MethodTable*
            // to find the correct classlib.
            throw array.GetMethodTable()->GetClasslibException(ExceptionIDs.ArrayTypeMismatch);
        }

        private static unsafe object IsInstanceOfArray(MethodTable* pTargetType, object obj)
        {
            MethodTable* pObjType = obj.GetMethodTable();

            Debug.Assert(pTargetType->IsArray, "IsInstanceOfArray called with non-array MethodTable");

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

            // compare the array types structurally

            if (pObjType->ParameterizedTypeShape != pTargetType->ParameterizedTypeShape)
            {
                // If the shapes are different, there's one more case to check for: Casting SzArray to MdArray rank 1.
                if (!pObjType->IsSzArray || pTargetType->ArrayRank != 1)
                {
                    return null;
                }
            }

            if (AreTypesAssignableInternal(pObjType->RelatedParameterType, pTargetType->RelatedParameterType,
                AssignmentVariation.AllowSizeEquivalence, null))
            {
                return obj;
            }

            return null;
        }

        private static unsafe object CheckCastArray(MethodTable* pTargetEEType, object obj)
        {
            object result = IsInstanceOfArray(pTargetEEType, obj);

            if (result == null)
            {
                // Throw the invalid cast exception defined by the classlib, using the input MethodTable*
                // to find the correct classlib.

                return ThrowInvalidCastException(pTargetEEType);
            }

            return result;
        }

        private static unsafe object IsInstanceOfVariantType(MethodTable* pTargetType, object obj)
        {
            if (!AreTypesAssignableInternal(obj.GetMethodTable(), pTargetType, AssignmentVariation.BoxedSource, null)
                && (!obj.GetMethodTable()->IsIDynamicInterfaceCastable
                || !IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: false)))
            {
                return null;
            }

            return obj;
        }

        private static unsafe object CheckCastVariantType(MethodTable* pTargetType, object obj)
        {
            if (!AreTypesAssignableInternal(obj.GetMethodTable(), pTargetType, AssignmentVariation.BoxedSource, null)
                && (!obj.GetMethodTable()->IsIDynamicInterfaceCastable
                || !IsInstanceOfInterfaceViaIDynamicInterfaceCastable(pTargetType, obj, throwing: true)))
            {
                return ThrowInvalidCastException(pTargetType);
            }

            return obj;
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

        // Would not be inlined, but still need to mark NoInlining so that it doesn't throw off tail calls
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe object ThrowInvalidCastException(MethodTable* pMT)
        {
            throw pMT->GetClasslibException(ExceptionIDs.InvalidCast);
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

                return pSourceType == pNullableType;
            }

            return AreTypesAssignableInternal(pSourceType, pTargetType, AssignmentVariation.BoxedSource, null);
        }

        // Internal recursively callable version of the export method above.
        // It keeps track of visited type pairs and returns a failure if type assignability yields a self-dependent cycle.
        public static unsafe bool AreTypesAssignableInternal(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation, EETypePairList* pVisited)
        {
            // Important special case -- it breaks infinite recursion
            if (pSourceType == pTargetType)
                return true;

            nuint sourceAndVariation = (nuint)pSourceType + (uint)variation;
            CastResult result = s_castCache.TryGet(sourceAndVariation, (nuint)(pTargetType));
            if (result != CastResult.MaybeCast)
            {
                return result == CastResult.CanCast;
            }

            return CacheMiss(pSourceType, pTargetType, variation, pVisited);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe bool CacheMiss(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation, EETypePairList* pVisited)
        {
            //
            // First, check if we previously visited the input types pair, to avoid infinite recursions
            //
            if (EETypePairList.Exists(pVisited, pSourceType, pTargetType))
                return false;

            //
            // Call into the type cast code to calculate the result
            //
            EETypePairList newList = new EETypePairList(pSourceType, pTargetType, pVisited);
            bool result = TypeCast.AreTypesAssignableInternalUncached(pSourceType, pTargetType, variation, &newList);

            //
            // Update the cache
            //
            nuint sourceAndVariation = (nuint)pSourceType + (uint)variation;
            s_castCache.TrySet(sourceAndVariation, (nuint)pTargetType, result);

            return result;
        }

        internal static unsafe bool AreTypesAssignableInternalUncached(MethodTable* pSourceType, MethodTable* pTargetType, AssignmentVariation variation, EETypePairList* pVisited)
        {
            bool fBoxedSource = (variation == AssignmentVariation.BoxedSource);
            bool fAllowSizeEquivalence = ((variation & AssignmentVariation.AllowSizeEquivalence) == AssignmentVariation.AllowSizeEquivalence);

            //
            // Are the types identical?
            //
            if (pSourceType == pTargetType)
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
                    MethodTable* pSourceRelatedParameterType = pSourceType->RelatedParameterType;
                    // Source type is also a parameterized type. Are the parameter types compatible?
                    if (pSourceRelatedParameterType->IsPointer)
                    {
                        // If the parameter types are pointers, then only exact matches are correct.
                        // As we've already compared equality at the start of this function,
                        // return false as the exact match case has already been handled.
                        // int** is not compatible with uint**, nor is int*[] oompatible with uint*[].
                        return false;
                    }
                    else if (pSourceRelatedParameterType->IsByRef)
                    {
                        // Only allow exact matches for ByRef types - same as pointers above. This should
                        // be unreachable and it's only a defense in depth. ByRefs can't be parameters
                        // of any parameterized type.
                        return false;
                    }
                    else if (pSourceRelatedParameterType->IsFunctionPointer)
                    {
                        // If the parameter types are function pointers, then only exact matches are correct.
                        // As we've already compared equality at the start of this function,
                        // return false as the exact match case has already been handled.
                        return false;
                    }
                    else
                    {
                        // Note that using AreTypesAssignableInternal with AssignmentVariation.AllowSizeEquivalence
                        // here handles array covariance as well as IFoo[] -> Foo[] etc.  We are not using
                        // AssignmentVariation.BoxedSource because int[] is not assignable to object[].
                        return AreTypesAssignableInternal(pSourceType->RelatedParameterType,
                            pTargetType->RelatedParameterType, AssignmentVariation.AllowSizeEquivalence, pVisited);
                    }
                }

                // Can't cast a non-parameter type to a parameter type or a parameter type of different shape to a parameter type
                return false;
            }

            if (pTargetType->IsFunctionPointer)
            {
                // Function pointers only cast if source and target are equivalent types.
                return false;
            }

            if (pSourceType->IsArray)
            {
                // Target type is not an array. But we can still cast arrays to Object or System.Array.
                return WellKnownEETypes.IsValidArrayBaseType(pTargetType);
            }
            else if (pSourceType->IsParameterizedType)
            {
                return false;
            }
            else if (pSourceType->IsFunctionPointer)
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe object? IsInstanceOfAny_NoCacheLookup(MethodTable* pTargetType, object obj)
        {
            MethodTable* pSourceType = obj.GetMethodTable();
            object? retObj;
            if (pTargetType->IsArray)
            {
                retObj = IsInstanceOfArray(pTargetType, obj);
            }
            else if (pTargetType->HasGenericVariance)
            {
                retObj = IsInstanceOfVariantType(pTargetType, obj);
            }
            else if (pTargetType->IsInterface)
            {
                retObj = IsInstanceOfInterface(pTargetType, obj);
            }
            else if (pTargetType->IsParameterizedType || pTargetType->IsFunctionPointer)
            {
                // We handled arrays above so this is for pointers and byrefs only.
                retObj = null;
            }
            else
            {
                retObj = IsInstanceOfClass(pTargetType, obj);
            }

            //
            // Update the cache
            //
            if (!pSourceType->IsIDynamicInterfaceCastable)
            {
                //
                // Update the cache
                //
                nuint sourceAndVariation = (nuint)pSourceType + (uint)AssignmentVariation.BoxedSource;
                s_castCache.TrySet(sourceAndVariation, (nuint)pTargetType, retObj != null);
            }

            return retObj;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe object CheckCastAny_NoCacheLookup(MethodTable* pTargetType, object obj)
        {
            MethodTable* pSourceType = obj.GetMethodTable();
            if (pTargetType->IsArray)
            {
                obj = CheckCastArray(pTargetType, obj);
            }
            else if (pTargetType->HasGenericVariance)
            {
                obj = CheckCastVariantType(pTargetType, obj);
            }
            else if (pTargetType->IsInterface)
            {
                obj = CheckCastInterface(pTargetType, obj);
            }
            else if (pTargetType->IsParameterizedType || pTargetType->IsFunctionPointer)
            {
                // We handled arrays above so this is for pointers and byrefs only.
                // Nothing can be a boxed instance of these.
                return ThrowInvalidCastException(pTargetType);
            }
            else
            {
                obj = CheckCastClass(pTargetType, obj);
            }

            if (!pSourceType->IsIDynamicInterfaceCastable)
            {
                //
                // Update the cache
                //
                nuint sourceAndVariation = (nuint)pSourceType + (uint)AssignmentVariation.BoxedSource;
                s_castCache.TrySet(sourceAndVariation, (nuint)pTargetType, true);
            }

            return obj;
        }
    }
}
