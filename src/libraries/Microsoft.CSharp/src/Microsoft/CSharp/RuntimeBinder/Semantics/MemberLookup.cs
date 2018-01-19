// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    [Flags]
    internal enum MemLookFlags : uint
    {
        None = 0,

        Ctor = EXPRFLAG.EXF_CTOR,
        NewObj = EXPRFLAG.EXF_NEWOBJCALL,
        Operator = EXPRFLAG.EXF_OPERATOR,
        Indexer = EXPRFLAG.EXF_INDEXER,
        UserCallable = EXPRFLAG.EXF_USERCALLABLE,
        BaseCall = EXPRFLAG.EXF_BASECALL,

        // All EXF flags are < 0x01000000
        MustBeInvocable = 0x20000000,

        All = Ctor | NewObj | Operator | Indexer | UserCallable | BaseCall | MustBeInvocable
    }

    /////////////////////////////////////////////////////////////////////////////////
    // MemberLookup class handles looking for a member within a type and its
    // base types. This only handles AGGTYPESYMs and TYVARSYMs.
    // 
    // Lookup must be called before any other methods.

    internal sealed class MemberLookup
    {
        // The inputs to Lookup.
        private CSemanticChecker _pSemanticChecker;
        private SymbolLoader _pSymbolLoader;
        private CType _typeSrc;
        private Expr _obj;
        private CType _typeQual;
        private ParentSymbol _symWhere;
        private Name _name;
        private int _arity;
        private MemLookFlags _flags;

        // For maintaining the type array. We throw the first 8 or so here.
        private readonly List<AggregateType> _rgtypeStart;

        // Results of the lookup.
        private List<AggregateType> _prgtype;
        private int _csym;                 // Number of syms found.
        private readonly SymWithType _swtFirst;     // The first symbol found.
        private readonly List<MethPropWithType> _methPropWithTypeList; // When we look up methods, we want to keep the list of all candidate methods given a particular name.

        // These are for error reporting.
        private readonly SymWithType _swtAmbig;     // An ambiguous symbol.
        private readonly SymWithType _swtInaccess;  // An inaccessible symbol.
        private readonly SymWithType _swtBad;       // If we're looking for a constructor or indexer, this matched on name, but isn't the right thing.
        private readonly SymWithType _swtBogus;     // A bogus member - such as an indexed property.
        private readonly SymWithType _swtBadArity;  // An symbol with the wrong arity.
        private bool _fMulti;              // Whether symFirst is of a kind for which we collect multiples (methods and indexers).

        /***************************************************************************************************
            Another match was found. Increment the count of syms and add the type to our list if it's not
            already there.
        ***************************************************************************************************/
        private void RecordType(AggregateType type, Symbol sym)
        {
            Debug.Assert(type != null && sym != null);

            if (!_prgtype.Contains(type))
            {
                _prgtype.Add(type);
            }

            // Now record the sym....

            _csym++;

            // If it is first, record it.
            if (_swtFirst == null)
            {
                _swtFirst.Set(sym, type);
                Debug.Assert(_csym == 1);
                Debug.Assert(_prgtype[0] == type);
                _fMulti = sym is MethodSymbol || sym is IndexerSymbol;
            }
        }

        /******************************************************************************
            Search just the given type (not any bases). Returns true iff it finds
            something (which will have been recorded by RecordType).
         
            pfHideByName is set to true iff something was found that hides all
            members of base types (eg, a hidebyname method).
        ******************************************************************************/
        private bool SearchSingleType(AggregateType typeCur, out bool pfHideByName)
        {
            bool fFoundSome = false;

            pfHideByName = false;

            // Make sure this type is accessible. It may not be due to private inheritance
            // or friend assemblies.
            bool fInaccess = !GetSemanticChecker().CheckTypeAccess(typeCur, _symWhere);
            if (fInaccess && (_csym != 0 || _swtInaccess != null))
                return false;

            // Loop through symbols.
            Symbol symCur;
            for (symCur = GetSymbolLoader().LookupAggMember(_name, typeCur.getAggregate(), symbmask_t.MASK_ALL);
                 symCur != null;
                 symCur = SymbolLoader.LookupNextSym(symCur, typeCur.getAggregate(), symbmask_t.MASK_ALL))
            {
                // Check for arity.
                switch (symCur.getKind())
                {
                    case SYMKIND.SK_MethodSymbol:
                        // For non-zero arity, only methods of the correct arity are considered.
                        // For zero arity, don't filter out any methods since we do type argument
                        // inferencing.
                        if (_arity > 0 && ((MethodSymbol)symCur).typeVars.Count != _arity)
                        {
                            if (!_swtBadArity)
                                _swtBadArity.Set(symCur, typeCur);
                            continue;
                        }
                        break;

                    case SYMKIND.SK_AggregateSymbol:
                        // For types, always filter on arity.
                        if (((AggregateSymbol)symCur).GetTypeVars().Count != _arity)
                        {
                            if (!_swtBadArity)
                                _swtBadArity.Set(symCur, typeCur);
                            continue;
                        }
                        break;

                    default:
                        // All others are only considered when arity is zero.
                        if (_arity > 0)
                        {
                            if (!_swtBadArity)
                                _swtBadArity.Set(symCur, typeCur);
                            continue;
                        }
                        break;
                }

                // Check for user callability.
                if (symCur.IsOverride() && !symCur.IsHideByName())
                {
                    continue;
                }

                MethodOrPropertySymbol methProp = symCur as MethodOrPropertySymbol;
                MethodSymbol meth = symCur as MethodSymbol;
                if (methProp != null && (_flags & MemLookFlags.UserCallable) != 0 && !methProp.isUserCallable())
                {
                    // If its an indexed property method symbol, let it through.
                    if (meth != null &&
                        meth.isPropertyAccessor() &&
                        ((symCur.name.Text.StartsWith("set_", StringComparison.Ordinal) && meth.Params.Count > 1) ||
                        (symCur.name.Text.StartsWith("get_", StringComparison.Ordinal) && meth.Params.Count > 0)))
                    {
                        if (!_swtInaccess)
                        {
                            _swtInaccess.Set(symCur, typeCur);
                        }
                        continue;
                    }
                }

                if (fInaccess || !GetSemanticChecker().CheckAccess(symCur, typeCur, _symWhere, _typeQual))
                {
                    // Not accessible so get the next sym.
                    if (!_swtInaccess)
                    {
                        _swtInaccess.Set(symCur, typeCur);
                    }
                    if (fInaccess)
                    {
                        return false;
                    }
                    continue;
                }

                PropertySymbol prop = symCur as PropertySymbol;

                // Make sure that whether we're seeing a ctor, operator, or indexer is consistent with the flags.
                if (((_flags & MemLookFlags.Ctor) == 0) != (meth == null || !meth.IsConstructor()) ||
                    ((_flags & MemLookFlags.Operator) == 0) != (meth == null || !meth.isOperator) ||
                    ((_flags & MemLookFlags.Indexer) == 0) != !(prop is IndexerSymbol))
                {
                    if (!_swtBad)
                    {
                        _swtBad.Set(symCur, typeCur);
                    }
                    continue;
                }

                // We can't call CheckBogus on methods or indexers because if the method has the wrong
                // number of parameters people don't think they should have to /r the assemblies containing
                // the parameter types and they complain about the resulting CS0012 errors.
                if (!(symCur is MethodSymbol) && (_flags & MemLookFlags.Indexer) == 0 && CSemanticChecker.CheckBogus(symCur))
                {
                    // A bogus member - we can't use these, so only record them for error reporting.
                    if (!_swtBogus)
                    {
                        _swtBogus.Set(symCur, typeCur);
                    }
                    continue;
                }

                // if we are in a calling context then we should only find a property if it is delegate valued
                if ((_flags & MemLookFlags.MustBeInvocable) != 0)
                {
                    if ((symCur is FieldSymbol field && !IsDelegateType(field.GetType(), typeCur) && !IsDynamicMember(symCur)) ||
                        (prop != null && !IsDelegateType(prop.RetType, typeCur) && !IsDynamicMember(symCur)))
                    {
                        if (!_swtBad)
                        {
                            _swtBad.Set(symCur, typeCur);
                        }
                        continue;
                    }
                }

                if (methProp != null)
                {
                    MethPropWithType mwpInsert = new MethPropWithType(methProp, typeCur);
                    _methPropWithTypeList.Add(mwpInsert);
                }

                // We have a visible symbol.
                fFoundSome = true;

                if (_swtFirst)
                {
                    if (!typeCur.isInterfaceType())
                    {
                        // Non-interface case.
                        Debug.Assert(_fMulti || typeCur == _prgtype[0]);
                        if (!_fMulti)
                        {
                            if (_swtFirst.Sym is FieldSymbol && symCur is EventSymbol
                                // The isEvent bit is only set on symbols which come from source...
                                // This is not a problem for the compiler because the field is only
                                // accessible in the scope in which it is declared,
                                // but in the EE we ignore accessibility...
                                && _swtFirst.Field().isEvent
                        )
                            {
                                // m_swtFirst is just the field behind the event symCur so ignore symCur.
                                continue;
                            }
                            else if (_swtFirst.Sym is FieldSymbol && symCur is EventSymbol)
                            {
                                // symCur is the matching event.
                                continue;
                            }
                            goto LAmbig;
                        }
                        if (_swtFirst.Sym.getKind() != symCur.getKind())
                        {
                            if (typeCur == _prgtype[0])
                                goto LAmbig;
                            // This one is hidden by the first one. This one also hides any more in base types.
                            pfHideByName = true;
                            continue;
                        }
                    }
                    // Interface case.
                    // m_fMulti   : n n n y y y y y
                    // same-kind  : * * * y n n n n
                    // fDiffHidden: * * * * y n n n
                    // meth       : * * * * * y n *  can n happen? just in case, we better handle it....
                    // hack       : n * y * * y * n
                    // meth-2     : * n y * * * * *
                    // res        : A A S R H H A A
                    else if (!_fMulti)
                    {
                        // Give method groups priority.
                        if (!(symCur is MethodSymbol))
                            goto LAmbig;
                        // Erase previous results so we'll record this method as the first.
                        _prgtype = new List<AggregateType>();
                        _csym = 0;
                        _swtFirst.Clear();
                        _swtAmbig.Clear();
                    }
                    else if (_swtFirst.Sym.getKind() != symCur.getKind())
                    {
                        if (!typeCur.fDiffHidden)
                        {
                            // Give method groups priority.
                            if (!(_swtFirst.Sym is MethodSymbol))
                                goto LAmbig;
                        }
                        // This one is hidden by another. This one also hides any more in base types.
                        pfHideByName = true;
                        continue;
                    }
                }

                RecordType(typeCur, symCur);

                if (methProp != null && methProp.isHideByName)
                    pfHideByName = true;
                // We've found a symbol in this type but need to make sure there aren't any conflicting
                // syms here, so keep searching the type.
            }

            Debug.Assert(!fInaccess || !fFoundSome);

            return fFoundSome;

        LAmbig:
            // Ambiguous!
            if (!_swtAmbig)
                _swtAmbig.Set(symCur, typeCur);
            pfHideByName = true;
            return true;
        }

        private bool IsDynamicMember(Symbol sym)
        {
            System.Runtime.CompilerServices.DynamicAttribute da = null;
            if (sym is FieldSymbol field)
            {
                if (!field.getType().isPredefType(PredefinedType.PT_OBJECT))
                {
                    return false;
                }
                var o = field.AssociatedFieldInfo.GetCustomAttributes(typeof(System.Runtime.CompilerServices.DynamicAttribute), false).ToArray();
                if (o.Length == 1)
                {
                    da = o[0] as System.Runtime.CompilerServices.DynamicAttribute;
                }
            }
            else
            {
                Debug.Assert(sym is PropertySymbol);
                PropertySymbol prop = (PropertySymbol)sym;
                if (!prop.getType().isPredefType(PredefinedType.PT_OBJECT))
                {
                    return false;
                }
                var o = prop.AssociatedPropertyInfo.GetCustomAttributes(typeof(System.Runtime.CompilerServices.DynamicAttribute), false).ToArray();
                if (o.Length == 1)
                {
                    da = o[0] as System.Runtime.CompilerServices.DynamicAttribute;
                }
            }

            if (da == null)
            {
                return false;
            }
            return (da.TransformFlags.Count == 0 || (da.TransformFlags.Count == 1 && da.TransformFlags[0]));
        }

        /******************************************************************************
            Lookup in a class and its bases (until *ptypeEnd is hit).
            
            ptypeEnd [in/out] - *ptypeEnd should be either null or object. If we find
                something here that would hide members of object, this sets *ptypeEnd
                to null.
         
            Returns true when searching should continue to the interfaces.
        ******************************************************************************/
        private bool LookupInClass(AggregateType typeStart, ref AggregateType ptypeEnd)
        {
            Debug.Assert(!_swtFirst || _fMulti);
            Debug.Assert(typeStart != null && !typeStart.isInterfaceType() && (ptypeEnd == null || typeStart != ptypeEnd));

            AggregateType typeEnd = ptypeEnd;
            AggregateType typeCur;

            // Loop through types. Loop until we hit typeEnd (object or null).
            for (typeCur = typeStart; typeCur != typeEnd && typeCur != null; typeCur = typeCur.GetBaseClass())
            {
                Debug.Assert(!typeCur.isInterfaceType());

                SearchSingleType(typeCur, out bool fHideByName);

                if (_swtFirst && !_fMulti)
                {
                    // Everything below this type and in interfaces is hidden.
                    return false;
                }

                if (fHideByName)
                {
                    // This hides everything below it and in object, but not in the interfaces!
                    ptypeEnd = null;

                    // Return true to indicate that it's ok to search additional types.
                    return true;
                }

                if ((_flags & MemLookFlags.Ctor) != 0)
                {
                    // If we're looking for a constructor, don't check base classes or interfaces.
                    return false;
                }
            }

            Debug.Assert(typeCur == typeEnd);
            return true;
        }

        /******************************************************************************
            Returns true if searching should continue to object.
        ******************************************************************************/
        private bool LookupInInterfaces(AggregateType typeStart, TypeArray types)
        {
            Debug.Assert(!_swtFirst || _fMulti);
            Debug.Assert(typeStart == null || typeStart.isInterfaceType());
            Debug.Assert(typeStart != null || types.Count != 0);

            // Clear all the hidden flags. Anything found in a class hides any other
            // kind of member in all the interfaces.
            if (typeStart != null)
            {
                typeStart.fAllHidden = false;
                typeStart.fDiffHidden = (_swtFirst != null);
            }

            for (int i = 0; i < types.Count; i++)
            {
                AggregateType type = (AggregateType)types[i];
                Debug.Assert(type.isInterfaceType());
                type.fAllHidden = false;
                type.fDiffHidden = !!_swtFirst;
            }

            bool fHideObject = false;
            AggregateType typeCur = typeStart;
            int itypeNext = 0;

            if (typeCur == null)
            {
                typeCur = (AggregateType)types[itypeNext++];
            }
            Debug.Assert(typeCur != null);

            // Loop through the interfaces.
            for (; ;)
            {
                Debug.Assert(typeCur != null && typeCur.isInterfaceType());

                if (!typeCur.fAllHidden && SearchSingleType(typeCur, out bool fHideByName))
                {
                    fHideByName |= !_fMulti;

                    // Mark base interfaces appropriately.
                    TypeArray ifaces = typeCur.GetIfacesAll();
                    for (int i = 0; i < ifaces.Count; i++)
                    {
                        AggregateType type = (AggregateType)ifaces[i];
                        Debug.Assert(type.isInterfaceType());
                        if (fHideByName)
                            type.fAllHidden = true;
                        type.fDiffHidden = true;
                    }

                    // If we hide all base types, that includes object!
                    if (fHideByName)
                        fHideObject = true;
                }

                if (itypeNext >= types.Count)
                    return !fHideObject;

                // Substitution has already been done.
                typeCur = types[itypeNext++] as AggregateType;
            }
        }

        private SymbolLoader GetSymbolLoader() { return _pSymbolLoader; }
        private CSemanticChecker GetSemanticChecker() { return _pSemanticChecker; }
        private ErrorHandling GetErrorContext() { return GetSymbolLoader().GetErrorContext(); }

        private RuntimeBinderException ReportBogus(SymWithType swt)
        {
            Debug.Assert(CSemanticChecker.CheckBogus(swt.Sym));
            MethodSymbol meth1 = swt.Prop().GetterMethod;
            MethodSymbol meth2 = swt.Prop().SetterMethod;
            Debug.Assert((meth1 ?? meth2) != null);
            return meth1 == null | meth2 == null
                ? GetErrorContext().Error(
                    ErrorCode.ERR_BindToBogusProp1, swt.Sym.name, new SymWithType(meth1 ?? meth2, swt.GetType()),
                    new ErrArgRefOnly(swt.Sym))
                : GetErrorContext().Error(
                    ErrorCode.ERR_BindToBogusProp2, swt.Sym.name, new SymWithType(meth1, swt.GetType()),
                    new SymWithType(meth2, swt.GetType()), new ErrArgRefOnly(swt.Sym));
        }

        private bool IsDelegateType(CType pSrcType, AggregateType pAggType)
        {
            CType pInstantiatedType = GetSymbolLoader().GetTypeManager().SubstType(pSrcType, pAggType, pAggType.GetTypeArgsAll());
            return pInstantiatedType.isDelegateType();
        }

        /////////////////////////////////////////////////////////////////////////////////
        // Public methods.

        public MemberLookup()
        {
            _methPropWithTypeList = new List<MethPropWithType>();
            _rgtypeStart = new List<AggregateType>();
            _swtFirst = new SymWithType();
            _swtAmbig = new SymWithType();
            _swtInaccess = new SymWithType();
            _swtBad = new SymWithType();
            _swtBogus = new SymWithType();
            _swtBadArity = new SymWithType();
        }

        /***************************************************************************************************
            Lookup must be called before anything else can be called.
         
            typeSrc - Must be an AggregateType or TypeParameterType.
            obj - the expression through which the member is being accessed. This is used for accessibility
                of protected members and for constructing a MEMGRP from the results of the lookup.
                It is legal for obj to be an EK_CLASS, in which case it may be used for accessibility, but
                will not be used for MEMGRP construction.
            symWhere - the symbol from with the name is being accessed (for checking accessibility).
            name - the name to look for.
            arity - the number of type args specified. Only members that support this arity are found.
                Note that when arity is zero, all methods are considered since we do type argument
                inferencing.
         
            flags - See MemLookFlags.
                TypeVarsAllowed only applies to the most derived type (not base types).
        ***************************************************************************************************/
        public bool Lookup(CSemanticChecker checker, CType typeSrc, Expr obj, ParentSymbol symWhere, Name name, int arity, MemLookFlags flags)
        {
            Debug.Assert((flags & ~MemLookFlags.All) == 0);
            Debug.Assert(obj == null || obj.Type != null);
            Debug.Assert(typeSrc is AggregateType);
            Debug.Assert(checker != null);

            _prgtype = _rgtypeStart;

            // Save the inputs for error handling, etc.
            _pSemanticChecker = checker;
            _pSymbolLoader = checker.SymbolLoader;
            _typeSrc = typeSrc;
            _obj = obj is ExprClass ? null : obj;
            _symWhere = symWhere;
            _name = name;
            _arity = arity;
            _flags = flags;

            _typeQual = (_flags & MemLookFlags.Ctor) != 0 ? _typeSrc : obj?.Type;

            // Determine what to search.
            AggregateType typeCls1 = null;
            AggregateType typeIface = null;
            TypeArray ifaces = BSYMMGR.EmptyTypeArray();
            AggregateType typeCls2 = null;

            if (!typeSrc.isInterfaceType())
            {
                typeCls1 = (AggregateType)typeSrc;

                if (typeCls1.IsWindowsRuntimeType())
                {
                    ifaces = typeCls1.GetWinRTCollectionIfacesAll(GetSymbolLoader());
                }
            }
            else
            {
                Debug.Assert(typeSrc.isInterfaceType());
                Debug.Assert((_flags & (MemLookFlags.Ctor | MemLookFlags.NewObj | MemLookFlags.Operator | MemLookFlags.BaseCall)) == 0);
                typeIface = (AggregateType)typeSrc;
                ifaces = typeIface.GetIfacesAll();
            }

            if (typeIface != null || ifaces.Count > 0)
                typeCls2 = GetSymbolLoader().GetPredefindType(PredefinedType.PT_OBJECT);

            // Search the class first (except possibly object).
            if (typeCls1 == null || LookupInClass(typeCls1, ref typeCls2))
            {
                // Search the interfaces.
                if ((typeIface != null || ifaces.Count > 0) && LookupInInterfaces(typeIface, ifaces) && typeCls2 != null)
                {
                    // Search object last.
                    Debug.Assert(typeCls2 != null && typeCls2.isPredefType(PredefinedType.PT_OBJECT));

                    AggregateType result = null;
                    LookupInClass(typeCls2, ref result);
                }
            }

            return !FError();
        }

        // Whether there were errors.
        private bool FError()
        {
            return !_swtFirst || _swtAmbig;
        }

        // The first symbol found.
        public Symbol SymFirst()
        {
            return _swtFirst.Sym;
        }
        public SymWithType SwtFirst()
        {
            return _swtFirst;
        }

        public Expr GetObject()
        {
            return _obj;
        }

        public CType GetSourceType()
        {
            return _typeSrc;
        }

        public MemLookFlags GetFlags()
        {
            return _flags;
        }

        // Put all the types in a type array.
        private TypeArray GetAllTypes()
        {
            return GetSymbolLoader().getBSymmgr().AllocParams(_prgtype.Count, _prgtype.ToArray());
        }

        /******************************************************************************
            Reports errors. Only call this if FError() is true.
        ******************************************************************************/
        public Exception ReportErrors()
        {
            Debug.Assert(FError());

            // Report error.
            // NOTE: If the definition of FError changes, this code will need to change.
            Debug.Assert(!_swtFirst || _swtAmbig);

            if (_swtFirst)
            {
                // Ambiguous lookup.
                return GetErrorContext().Error(ErrorCode.ERR_AmbigMember, _swtFirst, _swtAmbig);
            }

            if (_swtInaccess)
            {
                return !_swtInaccess.Sym.isUserCallable() && ((_flags & MemLookFlags.UserCallable) != 0)
                    ? GetErrorContext().Error(ErrorCode.ERR_CantCallSpecialMethod, _swtInaccess)
                    : GetSemanticChecker().ReportAccessError(_swtInaccess, _symWhere, _typeQual);
            }

            if ((_flags & MemLookFlags.Ctor) != 0)
            {
                return _arity > 0
                    ? GetErrorContext().Error(ErrorCode.ERR_BadCtorArgCount, _typeSrc.getAggregate(), _arity)
                    : GetErrorContext().Error(ErrorCode.ERR_NoConstructors, _typeSrc.getAggregate());
            }

            if ((_flags & MemLookFlags.Operator) != 0)
            {
                return GetErrorContext().Error(ErrorCode.ERR_NoSuchMember, _typeSrc, _name);
            }

            if ((_flags & MemLookFlags.Indexer) != 0)
            {
                return GetErrorContext().Error(ErrorCode.ERR_BadIndexLHS, _typeSrc);
            }

            if (_swtBad)
            {
                return GetErrorContext().Error((_flags & MemLookFlags.MustBeInvocable) != 0 ? ErrorCode.ERR_NonInvocableMemberCalled : ErrorCode.ERR_CantCallSpecialMethod, _swtBad);
            }

            if (_swtBogus)
            {
                return ReportBogus(_swtBogus);
            }

            if (_swtBadArity)
            {
                int cvar;

                switch (_swtBadArity.Sym.getKind())
                {
                    case SYMKIND.SK_MethodSymbol:
                        Debug.Assert(_arity != 0);
                        cvar = ((MethodSymbol)_swtBadArity.Sym).typeVars.Count;
                        return GetErrorContext().Error(cvar > 0 ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, _swtBadArity, new ErrArgSymKind(_swtBadArity.Sym), cvar);

                    case SYMKIND.SK_AggregateSymbol:
                        cvar = ((AggregateSymbol)_swtBadArity.Sym).GetTypeVars().Count;
                        return GetErrorContext().Error(cvar > 0 ? ErrorCode.ERR_BadArity : ErrorCode.ERR_HasNoTypeVars, _swtBadArity, new ErrArgSymKind(_swtBadArity.Sym), cvar);

                    default:
                        Debug.Assert(_arity != 0);
                        return GetErrorContext().Error(ErrorCode.ERR_TypeArgsNotAllowed, _swtBadArity, new ErrArgSymKind(_swtBadArity.Sym));
                }
            }

            return GetErrorContext().Error(ErrorCode.ERR_NoSuchMember, _typeSrc, _name);
        }
    }
}
