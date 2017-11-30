// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // ----------------------------------------------------------------------------
    //
    // AggregateType
    //
    // Represents a generic constructed (or instantiated) type. Parent is the AggregateSymbol.
    // ----------------------------------------------------------------------------

    internal sealed class AggregateType : CType
    {
        private TypeArray _pTypeArgsThis;
        private TypeArray _pTypeArgsAll;         // includes args from outer types
        private AggregateSymbol _pOwningAggregate;

        private AggregateType _baseType;  // This is the result of calling SubstTypeArray on the aggregate's baseClass.
        private TypeArray _ifacesAll;  // This is the result of calling SubstTypeArray on the aggregate's ifacesAll.
        private TypeArray _winrtifacesAll; //This is the list of collection interfaces implemented by a WinRT object.

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
            if (_baseType == null)
            {
                Type baseSysType = AssociatedSystemType.BaseType;
                if (baseSysType == null)
                {
                    return null;
                }

                // If we have a generic type definition, then we need to set the
                // base class to be our current base type, and use that to calculate 
                // our agg type and its base, then set it to be the generic version of the
                // base type. This is because:
                //
                // Suppose we have Foo<T> : IFoo<T>
                //
                // Initially, the BaseType will be IFoo<Foo.T>, which gives us the substitution
                // that we want to use for our agg type's base type. However, in the Symbol chain,
                // we want the base type to be IFoo<IFoo.T>. So we need to substitute.
                //
                // If we don't have a generic type definition, then we just need to set our base
                // class. This is so that if we have a base type that's generic, we'll be
                // getting the correctly instantiated base type.
                TypeManager manager = GetOwningAggregate().GetTypeManager();
                AggregateType baseClass = manager.SymbolTable.GetCTypeFromType(baseSysType) as AggregateType;
                Debug.Assert(baseClass != null);
                _baseType = manager.SubstType(baseClass, GetTypeArgsAll());
            }

            return _baseType;
        }

        public IEnumerable<AggregateType> TypeHierarchy
        {
            get
            {
                if (isInterfaceType())
                {
                    yield return this;
                    foreach (AggregateType iface in GetIfacesAll().Items)
                    {
                        yield return iface;
                    }

                    yield return getAggregate().GetTypeManager().ObjectAggregateType;
                }
                else
                {
                    for (AggregateType agg = this; agg != null; agg = agg.GetBaseClass())
                    {
                        yield return agg;
                    }
                }
            }
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
            _pTypeArgsAll = pTypeManager.ConcatenateTypeArrays(pCheckedOuterTypeArgs, _pTypeArgsThis);
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

                for (int i = 0; i < ifaces.Count; i++)
                {
                    AggregateType type = ifaces[i] as AggregateType;
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
    }
}

