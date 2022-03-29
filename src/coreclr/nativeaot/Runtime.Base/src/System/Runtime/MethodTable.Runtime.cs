// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using Internal.Runtime.CompilerServices;

namespace Internal.Runtime
{
    // Extensions to MethodTable that are specific to the use in Runtime.Base.
    internal unsafe partial struct MethodTable
    {
        internal MethodTable* GetArrayEEType()
        {
#if INPLACE_RUNTIME
            return EETypePtr.EETypePtrOf<Array>().ToPointer();
#else
            fixed (MethodTable* pThis = &this)
            {
                void* pGetArrayEEType = InternalCalls.RhpGetClasslibFunctionFromEEType(new IntPtr(pThis), ClassLibFunctionId.GetSystemArrayEEType);
                return ((delegate* <MethodTable*>)pGetArrayEEType)();
            }
#endif
        }

        internal Exception GetClasslibException(ExceptionIDs id)
        {
#if INPLACE_RUNTIME
            return RuntimeExceptionHelpers.GetRuntimeException(id);
#else
            DynamicModule* dynamicModule = this.DynamicModule;
            if (dynamicModule != null)
            {
                delegate* <System.Runtime.ExceptionIDs, System.Exception> getRuntimeException = dynamicModule->GetRuntimeException;
                if (getRuntimeException != null)
                {
                    return getRuntimeException(id);
                }
            }
            if (IsParameterizedType)
            {
                return RelatedParameterType->GetClasslibException(id);
            }

            return EH.GetClasslibExceptionFromEEType(id, GetAssociatedModuleAddress());
#endif
        }

        internal IntPtr GetClasslibFunction(ClassLibFunctionId id)
        {
            return (IntPtr)InternalCalls.RhpGetClasslibFunctionFromEEType((MethodTable*)Unsafe.AsPointer(ref this), id);
        }

        internal void SetToCloneOf(MethodTable* pOrigType)
        {
            Debug.Assert((_usFlags & (ushort)EETypeFlags.EETypeKindMask) == 0, "should be a canonical type");
            _usFlags |= (ushort)EETypeKind.ClonedEEType;
            _relatedType._pCanonicalType = pOrigType;
        }

        // Returns an address in the module most closely associated with this MethodTable that can be handed to
        // EH.GetClasslibException and use to locate the compute the correct exception type. In most cases
        // this is just the MethodTable pointer itself, but when this type represents a generic that has been
        // unified at runtime (and thus the MethodTable pointer resides in the process heap rather than a specific
        // module) we need to do some work.
        internal unsafe MethodTable* GetAssociatedModuleAddress()
        {
            fixed (MethodTable* pThis = &this)
            {
                if (!IsDynamicType)
                    return pThis;

                // There are currently four types of runtime allocated EETypes, arrays, pointers, byrefs, and generic types.
                // Arrays/Pointers/ByRefs can be handled by looking at their element type.
                if (IsParameterizedType)
                    return pThis->RelatedParameterType->GetAssociatedModuleAddress();

                if (!IsGeneric)
                {
                    // No way to resolve module information for a non-generic dynamic type.
                    return null;
                }

                // Generic types are trickier. Often we could look at the parent type (since eventually it
                // would derive from the class library's System.Object which is definitely not runtime
                // allocated). But this breaks down for generic interfaces. Instead we fetch the generic
                // instantiation information and use the generic type definition, which will always be module
                // local. We know this lookup will succeed since we're dealing with a unified generic type
                // and the unification process requires this metadata.
                MethodTable* pGenericType = pThis->GenericDefinition;

                Debug.Assert(pGenericType != null, "Generic type expected");

                return pGenericType;
            }
        }

        /// <summary>
        /// Return true if type is good for simple casting : canonical, no related type via IAT, no generic variance
        /// </summary>
        internal bool SimpleCasting()
        {
            return (_usFlags & (ushort)EETypeFlags.ComplexCastingMask) == (ushort)EETypeKind.CanonicalEEType;
        }

        /// <summary>
        /// Return true if both types are good for simple casting: canonical, no related type via IAT, no generic variance
        /// </summary>
        internal static bool BothSimpleCasting(MethodTable* pThis, MethodTable* pOther)
        {
            return ((pThis->_usFlags | pOther->_usFlags) & (ushort)EETypeFlags.ComplexCastingMask) == (ushort)EETypeKind.CanonicalEEType;
        }

        internal bool IsEquivalentTo(MethodTable* pOtherEEType)
        {
            fixed (MethodTable* pThis = &this)
            {
                if (pThis == pOtherEEType)
                    return true;

                MethodTable* pThisEEType = pThis;

                if (pThisEEType->IsCloned)
                    pThisEEType = pThisEEType->CanonicalEEType;

                if (pOtherEEType->IsCloned)
                    pOtherEEType = pOtherEEType->CanonicalEEType;

                if (pThisEEType == pOtherEEType)
                    return true;

                if (pThisEEType->IsParameterizedType && pOtherEEType->IsParameterizedType)
                {
                    return pThisEEType->RelatedParameterType->IsEquivalentTo(pOtherEEType->RelatedParameterType) &&
                        pThisEEType->ParameterizedTypeShape == pOtherEEType->ParameterizedTypeShape;
                }
            }

            return false;
        }
    }

    internal static class WellKnownEETypes
    {
        // Returns true if the passed in MethodTable is the MethodTable for System.Object
        // This is recognized by the fact that System.Object and interfaces are the only ones without a base type
        internal static unsafe bool IsSystemObject(MethodTable* pEEType)
        {
            if (pEEType->IsArray)
                return false;
            return (pEEType->NonArrayBaseType == null) && !pEEType->IsInterface;
        }

        // Returns true if the passed in MethodTable is the MethodTable for System.Array.
        // The binder sets a special CorElementType for this well known type
        internal static unsafe bool IsSystemArray(MethodTable* pEEType)
        {
            return (pEEType->ElementType == EETypeElementType.SystemArray);
        }
    }
}
