// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed partial class CMemberLookupResults
    {
        public class CMethodIterator
        {
            // Inputs.
            private readonly AggregateSymbol _context;
            private readonly TypeArray _containingTypes;
            private readonly CType _qualifyingType;
            private readonly Name _name;
            private readonly int _arity;
            private readonly symbmask_t _mask;
            private readonly EXPRFLAG _flags;
            private readonly ArgInfos _nonTrailingNamedArguments;
            // Internal state.
            private int _currentTypeIndex;

            public CMethodIterator(Name name, TypeArray containingTypes, CType qualifyingType, AggregateSymbol context, int arity, EXPRFLAG flags, symbmask_t mask, ArgInfos nonTrailingNamedArguments)
            {
                Debug.Assert(name != null);
                Debug.Assert(containingTypes != null);
                Debug.Assert(containingTypes.Count != 0);

                _name = name;
                _containingTypes = containingTypes;
                _qualifyingType = qualifyingType;
                _context = context;
                _arity = arity;
                _flags = flags;
                _mask = mask;
                _nonTrailingNamedArguments = nonTrailingNamedArguments;
            }

            public MethodOrPropertySymbol CurrentSymbol { get; private set; }

            public AggregateType CurrentType { get; private set; }

            public bool IsCurrentSymbolInaccessible { get; private set; }

            public bool IsCurrentSymbolBogus { get; private set; }

            public bool IsCurrentSymbolMisnamed { get; private set; }

            public bool MoveNext() => (CurrentType != null || FindNextTypeForInstanceMethods()) && FindNextMethod();

            public bool AtEnd => CurrentSymbol == null;

            public bool CanUseCurrentSymbol
            {
                [RequiresUnreferencedCode(Binder.TrimmerWarning)]
                get
                {
                    // Make sure that whether we're seeing a ctor is consistent with the flag.
                    // The only properties we handle are indexers.
                    if (_mask == symbmask_t.MASK_MethodSymbol && (
                            0 == (_flags & EXPRFLAG.EXF_CTOR) != !((MethodSymbol)CurrentSymbol).IsConstructor() ||
                            0 == (_flags & EXPRFLAG.EXF_OPERATOR) != !((MethodSymbol)CurrentSymbol).isOperator) ||
                        _mask == symbmask_t.MASK_PropertySymbol && !(CurrentSymbol is IndexerSymbol))
                    {
                        // Get the next symbol.
                        return false;
                    }

                    // If our arity is non-0, we must match arity with this symbol.
                    if (_arity > 0 & _mask == symbmask_t.MASK_MethodSymbol && ((MethodSymbol)CurrentSymbol).typeVars.Count != _arity)
                    {
                        return false;
                    }

                    // If this symbol's not callable, no good.
                    if (!ExpressionBinder.IsMethPropCallable(CurrentSymbol, (_flags & EXPRFLAG.EXF_USERCALLABLE) != 0))
                    {
                        return false;
                    }

                    // Check access. If Sym is not accessible, then let it through and mark it.
                    IsCurrentSymbolInaccessible = !CSemanticChecker.CheckAccess(CurrentSymbol, CurrentType, _context, _qualifyingType);

                    // Check bogus. If Sym is bogus, then let it through and mark it.
                    IsCurrentSymbolBogus = CSemanticChecker.CheckBogus(CurrentSymbol);

                    IsCurrentSymbolMisnamed = CheckArgumentNames();

                    return true;
                }
            }

            private bool CheckArgumentNames()
            {
                ArgInfos args = _nonTrailingNamedArguments;
                if (args != null)
                {
                    List<Name> paramNames = ExpressionBinder.GroupToArgsBinder
                        .FindMostDerivedMethod(CurrentSymbol, _qualifyingType)
                        .ParameterNames;

                    List<Expr> argExpressions = args.prgexpr;
                    for (int i = 0; i < args.carg; i++)
                    {
                        if (argExpressions[i] is ExprNamedArgumentSpecification named)
                        {
                            // Either wrong name, or correct name but we have more params arguments to follow.
                            if (paramNames[i] != named.Name || i == paramNames.Count - 1 && i != args.carg - 1)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            private bool FindNextMethod()
            {
                while (true)
                {
                    CurrentSymbol = (CurrentSymbol == null
                        ? SymbolLoader.LookupAggMember(_name, CurrentType.OwningAggregate, _mask)
                        : CurrentSymbol.LookupNext(_mask)) as MethodOrPropertySymbol;

                    // If we couldn't find a sym, we look up the type chain and get the next type.
                    if (CurrentSymbol == null)
                    {
                        if (!FindNextTypeForInstanceMethods())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // Note that we do not filter the current symbol for the user. They must do that themselves.
                        // This is because for instance, BindGrpToArgs wants to filter on arguments before filtering
                        // on bogosity.

                        // If we're here, we're good to go.

                        return true;
                    }
                }
            }

            private bool FindNextTypeForInstanceMethods()
            {
                if (_currentTypeIndex >= _containingTypes.Count)
                {
                    // No more types to check.
                    CurrentType = null;
                    return false;
                }

                CurrentType = _containingTypes[_currentTypeIndex++] as AggregateType;
                return true;
            }
        }
    }
}
