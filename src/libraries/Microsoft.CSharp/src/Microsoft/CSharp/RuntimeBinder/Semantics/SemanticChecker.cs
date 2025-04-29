// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Errors;

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
    internal static class CSemanticChecker
    {
        // Generate an error if CType is static.
        public static void CheckForStaticClass(CType type)
        {
            if (type.IsStaticClass)
            {
                throw ErrorHandling.Error(ErrorCode.ERR_ConvertToStaticClass, type);
            }
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static ACCESSERROR CheckAccess2(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
        {
            Debug.Assert(symCheck != null);
            Debug.Assert(atsCheck == null || symCheck.parent == atsCheck.OwningAggregate);
            Debug.Assert(typeThru == null ||
                   typeThru is AggregateType ||
                   typeThru is TypeParameterType ||
                   typeThru is ArrayType ||
                   typeThru is NullableType);

#if DEBUG

            switch (symCheck.getKind())
            {
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
            CType type = symCheck.getType();
            if (type == null)
            {
                return ACCESSERROR.ACCESSERROR_NOERROR;
            }

            // For members of AGGSYMs, atsCheck should always be specified!
            Debug.Assert(atsCheck != null);

            // Substitute on the CType.
            if (atsCheck.TypeArgsAll.Count > 0)
            {
                type = TypeManager.SubstType(type, atsCheck);
            }

            return CheckTypeAccess(type, symWhere) ? ACCESSERROR.ACCESSERROR_NOERROR : ACCESSERROR.ACCESSERROR_NOACCESS;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool CheckTypeAccess(CType type, Symbol symWhere)
        {
            Debug.Assert(type != null);

            // Array, Ptr, Nub, etc don't matter.
            type = type.GetNakedType(true);

            if (!(type is AggregateType ats))
            {
                Debug.Assert(type is VoidType || type is TypeParameterType);
                return true;
            }

            do
            {
                if (ACCESSERROR.ACCESSERROR_NOERROR != CheckAccessCore(ats.OwningAggregate, ats.OuterType, symWhere, null))
                {
                    return false;
                }

                ats = ats.OuterType;
            } while (ats != null);

            TypeArray typeArgs = ((AggregateType)type).TypeArgsAll;
            for (int i = 0; i < typeArgs.Count; i++)
            {
                if (!CheckTypeAccess(typeArgs[i], symWhere))
                    return false;
            }

            return true;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private static ACCESSERROR CheckAccessCore(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru)
        {
            Debug.Assert(symCheck != null);
            Debug.Assert(atsCheck == null || symCheck.parent == atsCheck.OwningAggregate);
            Debug.Assert(typeThru == null ||
                   typeThru is AggregateType ||
                   typeThru is TypeParameterType ||
                   typeThru is ArrayType ||
                   typeThru is NullableType);

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

                case ACCESS.ACC_INTERNAL_AND_PROTECTED:
                    if (symWhere == null || !symWhere.SameAssemOrFriend(symCheck))
                    {
                        return ACCESSERROR.ACCESSERROR_NOACCESS;
                    }

                    break;
            }

            // Find the inner-most enclosing AggregateSymbol.
            AggregateSymbol aggWhere = null;

            for (Symbol symT = symWhere; symT != null; symT = symT.parent)
            {
                if (symT is AggregateSymbol aggSym)
                {
                    aggWhere = aggSym;
                    break;
                }
            }

            if (aggWhere == null)
            {
                return ACCESSERROR.ACCESSERROR_NOACCESS;
            }

            // Should always have atsCheck for private and protected access check.
            // We currently don't need it since access doesn't respect instantiation.
            // We just use symWhere.parent as AggregateSymbol instead.
            AggregateSymbol aggCheck = symCheck.parent as AggregateSymbol;

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
            Debug.Assert(symCheck.GetAccess() == ACCESS.ACC_PROTECTED
                || symCheck.GetAccess() == ACCESS.ACC_INTERNALPROTECTED
                || symCheck.GetAccess() == ACCESS.ACC_INTERNAL_AND_PROTECTED);

            // Check if symCheck is in aggWhere or a base of aggWhere,
            // or in an outer agg of aggWhere or a base of an outer agg of aggWhere.

            AggregateType atsThru = null;

            if (typeThru != null && !symCheck.isStatic)
            {
                atsThru = typeThru.GetAts();
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
                    if (atsThru == null || atsThru.OwningAggregate.FindBaseAgg(agg))
                    {
                        return ACCESSERROR.ACCESSERROR_NOERROR;
                    }
                }
            }

            // the CType in which the method is being called has no relationship with the
            // CType on which the method is defined surely this is NOACCESS and not NOACCESSTHRU
            return found ? ACCESSERROR.ACCESSERROR_NOACCESSTHRU : ACCESSERROR.ACCESSERROR_NOACCESS;
        }

        public static bool CheckBogus(Symbol sym) => (sym as PropertySymbol)?.Bogus ?? false;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static RuntimeBinderException ReportAccessError(SymWithType swtBad, Symbol symWhere, CType typeQual)
        {
            Debug.Assert(!CheckAccess(swtBad.Sym, swtBad.GetType(), symWhere, typeQual) ||
                   !CheckTypeAccess(swtBad.GetType(), symWhere));

            return CheckAccess2(swtBad.Sym, swtBad.GetType(), symWhere, typeQual)
                   == ACCESSERROR.ACCESSERROR_NOACCESSTHRU
                ? ErrorHandling.Error(ErrorCode.ERR_BadProtectedAccess, swtBad, typeQual, symWhere)
                : ErrorHandling.Error(ErrorCode.ERR_BadAccess, swtBad);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public static bool CheckAccess(Symbol symCheck, AggregateType atsCheck, Symbol symWhere, CType typeThru) =>
            CheckAccess2(symCheck, atsCheck, symWhere, typeThru) == ACCESSERROR.ACCESSERROR_NOERROR;
    }
}
