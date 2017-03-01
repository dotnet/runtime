// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum ACCESSERROR
    {
        ACCESSERROR_NOACCESS,
        ACCESSERROR_NOACCESSTHRU,
        ACCESSERROR_NOERROR
    };


    //
    // Semantic check methods on SymbolLoader
    //
    internal abstract class CSemanticChecker
    {
        // Generate an error if CType is static.
        public bool CheckForStaticClass(Symbol symCtx, CType CType, ErrorCode err)
        {
            if (!CType.isStaticClass())
                return false;
            ReportStaticClassError(symCtx, CType, err);
            return true;
        }

        public virtual ACCESSERROR CheckAccess2(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
        {
            Debug.Assert(symCheck != null);
            Debug.Assert(atsCheck == null || symCheck.parent == atsCheck.getAggregate());
            Debug.Assert(typeThru == null ||
                   typeThru.IsAggregateType() ||
                   typeThru.IsTypeParameterType() ||
                   typeThru.IsArrayType() ||
                   typeThru.IsNullableType() ||
                   typeThru.IsErrorType());

#if DEBUG

            switch (symCheck.getKind())
            {
                default:
                    break;
                case SYMKIND.SK_MethodSymbol:
                case SYMKIND.SK_PropertySymbol:
                case SYMKIND.SK_FieldSymbol:
                case SYMKIND.SK_EventSymbol:
                    Debug.Assert(atsCheck != null);
                    break;
            }

#endif // DEBUG

            ACCESSERROR error = CheckAccessCore(symCheck, atsCheck, symWhere, typeThru);
            if (ACCESSERROR.ACCESSERROR_NOERROR != error)
            {
                return error;
            }

            // Check the accessibility of the return CType.
            CType CType = symCheck.getType();
            if (CType == null)
            {
                return ACCESSERROR.ACCESSERROR_NOERROR;
            }

            // For members of AGGSYMs, atsCheck should always be specified!
            Debug.Assert(atsCheck != null);

            if (atsCheck.getAggregate().IsSource())
            {
                // We already check the "at least as accessible as" rules.
                // Does this always work for generics?
                // Could we get a bad CType argument in typeThru?
                // Maybe call CheckTypeAccess on typeThru?
                return ACCESSERROR.ACCESSERROR_NOERROR;
            }

            // Substitute on the CType.
            if (atsCheck.GetTypeArgsAll().size > 0)
            {
                CType = SymbolLoader.GetTypeManager().SubstType(CType, atsCheck);
            }

            return CheckTypeAccess(CType, symWhere) ? ACCESSERROR.ACCESSERROR_NOERROR : ACCESSERROR.ACCESSERROR_NOACCESS;
        }
        public virtual bool CheckTypeAccess(CType type, Symbol symWhere)
        {
            Debug.Assert(type != null);

            // Array, Ptr, Nub, etc don't matter.
            type = type.GetNakedType(true);

            if (!type.IsAggregateType())
            {
                Debug.Assert(type.IsVoidType() || type.IsErrorType() || type.IsTypeParameterType());
                return true;
            }

            for (AggregateType ats = type.AsAggregateType(); ats != null; ats = ats.outerType)
            {
                if (ACCESSERROR.ACCESSERROR_NOERROR != CheckAccessCore(ats.GetOwningAggregate(), ats.outerType, symWhere, null))
                {
                    return false;
                }
            }

            TypeArray typeArgs = type.AsAggregateType().GetTypeArgsAll();
            for (int i = 0; i < typeArgs.size; i++)
            {
                if (!CheckTypeAccess(typeArgs.Item(i), symWhere))
                    return false;
            }

            return true;
        }

        // Generates an error for static classes
        public void ReportStaticClassError(Symbol symCtx, CType CType, ErrorCode err)
        {
            if (symCtx != null)
                ErrorContext.Error(err, CType, new ErrArgRef(symCtx));
            else
                ErrorContext.Error(err, CType);
        }

        public abstract SymbolLoader SymbolLoader { get; }
        public abstract SymbolLoader GetSymbolLoader();

        /////////////////////////////////////////////////////////////////////////////////
        // SymbolLoader forwarders (begin)
        //

        private ErrorHandling ErrorContext
        {
            get
            {
                return SymbolLoader.ErrorContext;
            }
        }
        public ErrorHandling GetErrorContext() { return ErrorContext; }
        public NameManager GetNameManager() { return SymbolLoader.GetNameManager(); }
        public TypeManager GetTypeManager() { return SymbolLoader.GetTypeManager(); }
        public BSYMMGR getBSymmgr() { return SymbolLoader.getBSymmgr(); }
        public SymFactory GetGlobalSymbolFactory() { return SymbolLoader.GetGlobalSymbolFactory(); }
        public MiscSymFactory GetGlobalMiscSymFactory() { return SymbolLoader.GetGlobalMiscSymFactory(); }

        //protected CompilerPhase GetCompPhase() { return SymbolLoader.CompPhase(); }
        //protected void SetCompPhase(CompilerPhase compPhase) { SymbolLoader.compPhase = compPhase; }
        public PredefinedTypes getPredefTypes() { return SymbolLoader.getPredefTypes(); }
        //
        // SymbolLoader forwarders (end)
        /////////////////////////////////////////////////////////////////////////////////

        //
        // Utility methods
        //
        private ACCESSERROR CheckAccessCore(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
        {
            Debug.Assert(symCheck != null);
            Debug.Assert(atsCheck == null || symCheck.parent == atsCheck.getAggregate());
            Debug.Assert(typeThru == null ||
                   typeThru.IsAggregateType() ||
                   typeThru.IsTypeParameterType() ||
                   typeThru.IsArrayType() ||
                   typeThru.IsNullableType() ||
                   typeThru.IsErrorType());

            switch (symCheck.GetAccess())
            {
                default:
                    throw Error.InternalCompilerError();
                //return ACCESSERROR.ACCESSERROR_NOACCESS;

                case ACCESS.ACC_UNKNOWN:
                    return ACCESSERROR.ACCESSERROR_NOACCESS;

                case ACCESS.ACC_PUBLIC:
                    return ACCESSERROR.ACCESSERROR_NOERROR;

                case ACCESS.ACC_PRIVATE:
                case ACCESS.ACC_PROTECTED:
                    if (symWhere == null)
                    {
                        return ACCESSERROR.ACCESSERROR_NOACCESS;
                    }
                    break;

                case ACCESS.ACC_INTERNAL:
                case ACCESS.ACC_INTERNALPROTECTED:   // Check internal, then protected.

                    if (symWhere == null)
                    {
                        return ACCESSERROR.ACCESSERROR_NOACCESS;
                    }
                    if (symWhere.SameAssemOrFriend(symCheck))
                    {
                        return ACCESSERROR.ACCESSERROR_NOERROR;
                    }
                    if (symCheck.GetAccess() == ACCESS.ACC_INTERNAL)
                    {
                        return ACCESSERROR.ACCESSERROR_NOACCESS;
                    }
                    break;
            }

            // Should always have atsCheck for private and protected access check.
            // We currently don't need it since access doesn't respect instantiation.
            // We just use symWhere.parent.AsAggregateSymbol() instead.
            AggregateSymbol aggCheck = symCheck.parent.AsAggregateSymbol();

            // Find the inner-most enclosing AggregateSymbol.
            AggregateSymbol aggWhere = null;

            for (Symbol symT = symWhere; symT != null; symT = symT.parent)
            {
                if (symT.IsAggregateSymbol())
                {
                    aggWhere = symT.AsAggregateSymbol();
                    break;
                }
                if (symT.IsAggregateDeclaration())
                {
                    aggWhere = symT.AsAggregateDeclaration().Agg();
                    break;
                }
            }

            if (aggWhere == null)
            {
                return ACCESSERROR.ACCESSERROR_NOACCESS;
            }

            // First check for private access.
            for (AggregateSymbol agg = aggWhere; agg != null; agg = agg.GetOuterAgg())
            {
                if (agg == aggCheck)
                {
                    return ACCESSERROR.ACCESSERROR_NOERROR;
                }
            }

            if (symCheck.GetAccess() == ACCESS.ACC_PRIVATE)
            {
                return ACCESSERROR.ACCESSERROR_NOACCESS;
            }

            // Handle the protected case - which is the only real complicated one.
            Debug.Assert(symCheck.GetAccess() == ACCESS.ACC_PROTECTED || symCheck.GetAccess() == ACCESS.ACC_INTERNALPROTECTED);

            // Check if symCheck is in aggWhere or a base of aggWhere,
            // or in an outer agg of aggWhere or a base of an outer agg of aggWhere.

            AggregateType atsThru = null;

            if (typeThru != null && !symCheck.isStatic)
            {
                atsThru = SymbolLoader.GetAggTypeSym(typeThru);
            }

            // Look for aggCheck among the base classes of aggWhere and outer aggs.
            bool found = false;
            for (AggregateSymbol agg = aggWhere; agg != null; agg = agg.GetOuterAgg())
            {
                Debug.Assert(agg != aggCheck); // We checked for this above.

                // Look for aggCheck among the base classes of agg.
                if (agg.FindBaseAgg(aggCheck))
                {
                    found = true;
                    // aggCheck is a base class of agg. Check atsThru.
                    // For non-static protected access to be legal, atsThru must be an instantiation of
                    // agg or a CType derived from an instantiation of agg. In this case
                    // all that matters is that agg is in the base AggregateSymbol chain of atsThru. The
                    // actual AGGTYPESYMs involved don't matter.
                    if (atsThru == null || atsThru.getAggregate().FindBaseAgg(agg))
                    {
                        return ACCESSERROR.ACCESSERROR_NOERROR;
                    }
                }
            }

            // the CType in which the method is being called has no relationship with the 
            // CType on which the method is defined surely this is NOACCESS and not NOACCESSTHRU
            if (found == false)
                return ACCESSERROR.ACCESSERROR_NOACCESS;

            return (atsThru == null) ? ACCESSERROR.ACCESSERROR_NOACCESS : ACCESSERROR.ACCESSERROR_NOACCESSTHRU;
        }

        public bool CheckBogus(Symbol sym)
        {
            if (sym == null)
            {
                return false;
            }

            if (!sym.hasBogus())
            {
                bool fBogus = sym.computeCurrentBogusState();

                if (fBogus)
                {
                    // Only set this if everything is declared or
                    // at least 1 declared thing is bogus
                    sym.setBogus(fBogus);
                }
            }

            return sym.hasBogus() && sym.checkBogus();
        }

        public bool CheckBogus(CType pType)
        {
            if (pType == null)
            {
                return false;
            }

            if (!pType.hasBogus())
            {
                bool fBogus = pType.computeCurrentBogusState();

                if (fBogus)
                {
                    // Only set this if everything is declared or
                    // at least 1 declared thing is bogus
                    pType.setBogus(fBogus);
                }
            }

            return pType.hasBogus() && pType.checkBogus();
        }

        public void ReportAccessError(SymWithType swtBad, Symbol symWhere, CType typeQual)
        {
            Debug.Assert(!CheckAccess(swtBad.Sym, swtBad.GetType(), symWhere, typeQual) ||
                   !CheckTypeAccess(swtBad.GetType(), symWhere));

            if (CheckAccess2(swtBad.Sym, swtBad.GetType(), symWhere, typeQual) == ACCESSERROR.ACCESSERROR_NOACCESSTHRU)
            {
                ErrorContext.Error(ErrorCode.ERR_BadProtectedAccess, swtBad, typeQual, symWhere);
            }
            else
            {
                ErrorContext.ErrorRef(ErrorCode.ERR_BadAccess, swtBad);
            }
        }

        public bool CheckAccess(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
        {
            return CheckAccess2(symCheck, atsCheck, symWhere, typeThru) == ACCESSERROR.ACCESSERROR_NOERROR;
        }
    }
}
