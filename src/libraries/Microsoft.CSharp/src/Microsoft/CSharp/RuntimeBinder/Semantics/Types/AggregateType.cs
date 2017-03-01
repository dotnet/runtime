// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // AggregateType
    //
    // Represents a generic constructed (or instantiated) type. Parent is the AggregateSymbol.
    // ----------------------------------------------------------------------------

    internal partial class AggregateType : CType
    {
        private TypeArray _pTypeArgsThis;
        private TypeArray _pTypeArgsAll;         // includes args from outer types
        private AggregateSymbol _pOwningAggregate;

#if ! CSEE // The EE can't cache these since the AGGSYMs may change as things are imported.
        private AggregateType _baseType;  // This is the result of calling SubstTypeArray on the aggregate's baseClass.
        private TypeArray _ifacesAll;  // This is the result of calling SubstTypeArray on the aggregate's ifacesAll.
        private TypeArray _winrtifacesAll; //This is the list of collection interfaces implemented by a WinRT object.
#else // !CSEE

        public short proxyOID; // oid for the managed proxy in the host running inside the debugee
        public short typeConverterID;
#endif // !CSEE

        public bool fConstraintsChecked;    // Have the constraints been checked yet?
        public bool fConstraintError;       // Did the constraints check produce an error?

        // These two flags are used to track hiding within interfaces.
        // Their use and validity is always localized. See e.g. MemberLookup::LookupInInterfaces.
        public bool fAllHidden;             // All members are hidden by a derived interface member.
        public bool fDiffHidden;            // Members other than a specific kind are hidden by a derived interface member or class member.

        public AggregateType outerType;          // the outer type if this is a nested type

        public void SetOwningAggregate(AggregateSymbol agg)
        {
            _pOwningAggregate = agg;
        }

        public AggregateSymbol GetOwningAggregate()
        {
            return _pOwningAggregate;
        }

        public AggregateType GetBaseClass()
        {
#if CSEE
            AggregateType atsBase = getAggregate().GetBaseClass();
            if (!atsBase || GetTypeArgsAll().size == 0 || atsBase.GetTypeArgsAll().size == 0)
                return atsBase;

            return getAggregate().GetTypeManager().SubstType(atsBase, GetTypeArgsAll()).AsAggregateType();
#else // !CSEE

            if (_baseType == null)
            {
                _baseType = getAggregate().GetTypeManager().SubstType(getAggregate().GetBaseClass(), GetTypeArgsAll()) as AggregateType;
            }

            return _baseType;
#endif // !CSEE
        }

        public void SetTypeArgsThis(TypeArray pTypeArgsThis)
        {
            TypeArray pOuterTypeArgs;
            if (outerType != null)
            {
                Debug.Assert(outerType.GetTypeArgsThis() != null);
                Debug.Assert(outerType.GetTypeArgsAll() != null);

                pOuterTypeArgs = outerType.GetTypeArgsAll();
            }
            else
            {
                pOuterTypeArgs = BSYMMGR.EmptyTypeArray();
            }

            Debug.Assert(pTypeArgsThis != null);
            _pTypeArgsThis = pTypeArgsThis;
            SetTypeArgsAll(pOuterTypeArgs);
        }

        private void SetTypeArgsAll(TypeArray outerTypeArgs)
        {
            Debug.Assert(_pTypeArgsThis != null);

            // Here we need to check our current type args. If we have an open placeholder,
            // then we need to have all open placeholders, and we want to flush
            // our outer type args so that they're open placeholders. 
            //
            // This is because of the following scenario:
            //
            // class B<T>
            // {
            //     class C<U>
            //     {
            //     }
            //     class D
            //     {
            //         void Foo()
            //         {
            //             Type T = typeof(C<>);
            //         }
            //     }
            // }
            //
            // The outer type will be B<T>, but the inner type will be C<>. However,
            // this will eventually be represented in IL as B<>.C<>. As such, we should
            // keep our data structures clear - if we have one open type argument, then
            // all of them must be open type arguments.
            //
            // Ensure that invariant here.

            TypeArray pCheckedOuterTypeArgs = outerTypeArgs;
            TypeManager pTypeManager = getAggregate().GetTypeManager();

            if (_pTypeArgsThis.Size > 0 && AreAllTypeArgumentsUnitTypes(_pTypeArgsThis))
            {
                if (outerTypeArgs.Size > 0 && !AreAllTypeArgumentsUnitTypes(outerTypeArgs))
                {
                    // We have open placeholder types in our type, but not the parent.
                    pCheckedOuterTypeArgs = pTypeManager.CreateArrayOfUnitTypes(outerTypeArgs.Size);
                }
            }
            _pTypeArgsAll = pTypeManager.ConcatenateTypeArrays(pCheckedOuterTypeArgs, _pTypeArgsThis);
        }

        private bool AreAllTypeArgumentsUnitTypes(TypeArray typeArray)
        {
            if (typeArray.Size == 0)
            {
                return true;
            }

            for (int i = 0; i < typeArray.size; i++)
            {
                Debug.Assert(typeArray.Item(i) != null);
                if (!typeArray.Item(i).IsOpenTypePlaceholderType())
                {
                    return false;
                }
            }
            return true;
        }

        public TypeArray GetTypeArgsThis()
        {
            return _pTypeArgsThis;
        }

        public TypeArray GetTypeArgsAll()
        {
            return _pTypeArgsAll;
        }

        public TypeArray GetIfacesAll()
        {
            if (_ifacesAll == null)
            {
                _ifacesAll = getAggregate().GetTypeManager().SubstTypeArray(getAggregate().GetIfacesAll(), GetTypeArgsAll());
            }
            return _ifacesAll;
        }

        public TypeArray GetWinRTCollectionIfacesAll(SymbolLoader pSymbolLoader)
        {
            if (_winrtifacesAll == null)
            {
                TypeArray ifaces = GetIfacesAll();
                System.Collections.Generic.List<CType> typeList = new System.Collections.Generic.List<CType>();

                for (int i = 0; i < ifaces.size; i++)
                {
                    AggregateType type = ifaces.Item(i).AsAggregateType();
                    Debug.Assert(type.isInterfaceType());

                    if (type.IsCollectionType())
                    {
                        typeList.Add(type);
                    }
                }
                _winrtifacesAll = pSymbolLoader.getBSymmgr().AllocParams(typeList.Count, typeList.ToArray());
            }
            return _winrtifacesAll;
        }

        public TypeArray GetDelegateParameters(SymbolLoader pSymbolLoader)
        {
            Debug.Assert(isDelegateType());
            MethodSymbol invoke = pSymbolLoader.LookupInvokeMeth(getAggregate());
            if (invoke == null || !invoke.isInvoke())
            {
                // This can happen if the delegate is internal to another assembly. 
                return null;
            }
            return getAggregate().GetTypeManager().SubstTypeArray(invoke.Params, this);
        }

        public CType GetDelegateReturnType(SymbolLoader pSymbolLoader)
        {
            Debug.Assert(isDelegateType());
            MethodSymbol invoke = pSymbolLoader.LookupInvokeMeth(getAggregate());
            if (invoke == null || !invoke.isInvoke())
            {
                // This can happen if the delegate is internal to another assembly. 
                return null;
            }
            return getAggregate().GetTypeManager().SubstType(invoke.RetType, this);
        }
    }
}

