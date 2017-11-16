// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal partial class CMemberLookupResults
    {
        public partial class CMethodIterator
        {
            private SymbolLoader _pSymbolLoader;
            private CSemanticChecker _pSemanticChecker;
            // Inputs.
            private AggregateType _pCurrentType;
            private MethodOrPropertySymbol _pCurrentSym;
            private AggregateDeclaration _pContext;
            private TypeArray _pContainingTypes;
            private CType _pQualifyingType;
            private Name _pName;
            private int _nArity;
            private symbmask_t _mask;
            private EXPRFLAG _flags;
            // Internal state.
            private int _nCurrentTypeCount;
            private bool _bIsCheckingInstanceMethods;
            private bool _bAtEnd;
            private bool _bAllowBogusAndInaccessible;
            // Flags for the current sym.
            private bool _bCurrentSymIsBogus;
            private bool _bCurrentSymIsInaccessible;
            // if Extension can be part of the results that are returned by the iterator
            // this may be false if an applicable instance method was found by bindgrptoArgs
            private bool _bcanIncludeExtensionsInResults;

            public CMethodIterator(CSemanticChecker checker, SymbolLoader symLoader, Name name, TypeArray containingTypes, CType @object, CType qualifyingType, AggregateDeclaration context, bool allowBogusAndInaccessible, bool allowExtensionMethods, int arity, EXPRFLAG flags, symbmask_t mask)
            {
                Debug.Assert(name != null);
                Debug.Assert(symLoader != null);
                Debug.Assert(checker != null);
                Debug.Assert(containingTypes != null);
                _pSemanticChecker = checker;
                _pSymbolLoader = symLoader;
                _pCurrentType = null;
                _pCurrentSym = null;
                _pName = name;
                _pContainingTypes = containingTypes;
                _pQualifyingType = qualifyingType;
                _pContext = context;
                _bAllowBogusAndInaccessible = allowBogusAndInaccessible;
                _nArity = arity;
                _flags = flags;
                _mask = mask;
                _nCurrentTypeCount = 0;
                _bIsCheckingInstanceMethods = true;
                _bAtEnd = false;
                _bCurrentSymIsBogus = false;
                _bCurrentSymIsInaccessible = false;
                _bcanIncludeExtensionsInResults = allowExtensionMethods;
            }
            public MethodOrPropertySymbol GetCurrentSymbol()
            {
                return _pCurrentSym;
            }
            public AggregateType GetCurrentType()
            {
                return _pCurrentType;
            }
            public bool IsCurrentSymbolInaccessible()
            {
                return _bCurrentSymIsInaccessible;
            }
            public bool IsCurrentSymbolBogus()
            {
                return _bCurrentSymIsBogus;
            }
            public bool MoveNext(bool canIncludeExtensionsInResults)
            {
                if (_bcanIncludeExtensionsInResults)
                {
                    _bcanIncludeExtensionsInResults = canIncludeExtensionsInResults;
                }

                if (_bAtEnd)
                {
                    return false;
                }

                if (_pCurrentType == null) // First guy.
                {
                    if (_pContainingTypes.Count == 0)
                    {
                        // No instance methods, only extensions.
                        _bIsCheckingInstanceMethods = false;
                        _bAtEnd = true;
                        return false;
                    }
                    else
                    {
                        if (!FindNextTypeForInstanceMethods())
                        {
                            // No instance or extensions.

                            _bAtEnd = true;
                            return false;
                        }
                    }
                }
                if (!FindNextMethod())
                {
                    _bAtEnd = true;
                    return false;
                }
                return true;
            }
            public bool AtEnd()
            {
                return _pCurrentSym == null;
            }
            private CSemanticChecker GetSemanticChecker()
            {
                return _pSemanticChecker;
            }
            private SymbolLoader GetSymbolLoader()
            {
                return _pSymbolLoader;
            }
            public bool CanUseCurrentSymbol()
            {
                _bCurrentSymIsInaccessible = false;
                _bCurrentSymIsBogus = false;

                // Make sure that whether we're seeing a ctor is consistent with the flag.
                // The only properties we handle are indexers.
                if (_mask == symbmask_t.MASK_MethodSymbol && (
                        0 == (_flags & EXPRFLAG.EXF_CTOR) != !((MethodSymbol)_pCurrentSym).IsConstructor() ||
                        0 == (_flags & EXPRFLAG.EXF_OPERATOR) != !((MethodSymbol)_pCurrentSym).isOperator) ||
                    _mask == symbmask_t.MASK_PropertySymbol && !(_pCurrentSym is IndexerSymbol))
                {
                    // Get the next symbol.
                    return false;
                }

                // If our arity is non-0, we must match arity with this symbol.
                if (_nArity > 0)
                {
                    if (_mask == symbmask_t.MASK_MethodSymbol && ((MethodSymbol)_pCurrentSym).typeVars.Count != _nArity)
                    {
                        return false;
                    }
                }

                // If this guy's not callable, no good.
                if (!ExpressionBinder.IsMethPropCallable(_pCurrentSym, (_flags & EXPRFLAG.EXF_USERCALLABLE) != 0))
                {
                    return false;
                }

                // Check access.
                if (!GetSemanticChecker().CheckAccess(_pCurrentSym, _pCurrentType, _pContext, _pQualifyingType))
                {
                    // Sym is not accessible. However, if we're allowing inaccessible, then let it through and mark it.
                    if (_bAllowBogusAndInaccessible)
                    {
                        _bCurrentSymIsInaccessible = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                // Check bogus.
                if (CSemanticChecker.CheckBogus(_pCurrentSym))
                {
                    // Sym is bogus, but if we're allow it, then let it through and mark it.
                    if (_bAllowBogusAndInaccessible)
                    {
                        _bCurrentSymIsBogus = true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return _bIsCheckingInstanceMethods;
            }

            private bool FindNextMethod()
            {
                while (true)
                {
                    if (_pCurrentSym == null)
                    {
                        _pCurrentSym = GetSymbolLoader().LookupAggMember(
                                _pName, _pCurrentType.getAggregate(), _mask) as MethodOrPropertySymbol;
                    }
                    else
                    {
                        _pCurrentSym = SymbolLoader.LookupNextSym(
                                _pCurrentSym, _pCurrentType.getAggregate(), _mask) as MethodOrPropertySymbol;
                    }

                    // If we couldn't find a sym, we look up the type chain and get the next type.
                    if (_pCurrentSym == null)
                    {
                        if (_bIsCheckingInstanceMethods)
                        {
                            if (!FindNextTypeForInstanceMethods() && _bcanIncludeExtensionsInResults)
                            {
                                // We didn't find any more instance methods, set us into extension mode.

                                _bIsCheckingInstanceMethods = false;
                            }
                            else if (_pCurrentType == null && !_bcanIncludeExtensionsInResults)
                            {
                                return false;
                            }
                            else
                            {
                                // Found an instance method.
                                continue;
                            }
                        }
                        continue;
                    }

                    // Note that we do not filter the current symbol for the user. They must do that themselves.
                    // This is because for instance, BindGrpToArgs wants to filter on arguments before filtering
                    // on bogosity.

                    // If we're here, we're good to go.

                    break;
                }
                return true;
            }

            private bool FindNextTypeForInstanceMethods()
            {
                // Otherwise, search through other types listed as well as our base class.
                if (_pContainingTypes.Count > 0)
                {
                    if (_nCurrentTypeCount >= _pContainingTypes.Count)
                    {
                        // No more types to check.
                        _pCurrentType = null;
                    }
                    else
                    {
                        _pCurrentType = _pContainingTypes[_nCurrentTypeCount++] as AggregateType;
                    }
                }
                else
                {
                    // We have no more types to consider, so check out the base class.

                    _pCurrentType = _pCurrentType.GetBaseClass();
                }
                return _pCurrentType != null;
            }
        }
    }
}
