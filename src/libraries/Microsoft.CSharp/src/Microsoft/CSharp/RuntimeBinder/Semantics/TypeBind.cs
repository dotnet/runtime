// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Defines the structure used when binding types

    // CheckConstraints options.

    internal enum CheckConstraintsFlags
    {
        None = 0x00,
        Outer = 0x01,
        NoDupErrors = 0x02,
        NoErrors = 0x04,
    }

    /////////////////////////////////////////////////////////////////////////////////
    // TypeBind has static methods to accomplish most tasks.
    // 
    // For some of these tasks there are also instance methods. The instance
    // method versions don't report not found errors (they may report others) but
    // instead record error information in the TypeBind instance. Call the
    // ReportErrors method to report recorded errors.

    internal static class TypeBind
    {
        // Check the constraints of any type arguments in the given Type.
        public static bool CheckConstraints(CSemanticChecker checker, ErrorHandling errHandling, CType type, CheckConstraintsFlags flags)
        {
            type = type.GetNakedType(false);

            if (type.IsNullableType())
            {
                CType typeT = type.AsNullableType().GetAts(checker.GetErrorContext());
                if (typeT != null)
                    type = typeT;
                else
                    type = type.GetNakedType(true);
            }

            if (!type.IsAggregateType())
                return true;

            AggregateType ats = type.AsAggregateType();

            if (ats.GetTypeArgsAll().Count == 0)
            {
                // Common case: there are no type vars, so there are no constraints.
                ats.fConstraintsChecked = true;
                ats.fConstraintError = false;
                return true;
            }

            if (ats.fConstraintsChecked)
            {
                // Already checked.
                if (!ats.fConstraintError || (flags & CheckConstraintsFlags.NoDupErrors) != 0)
                {
                    // No errors or no need to report errors again.
                    return !ats.fConstraintError;
                }
            }

            TypeArray typeVars = ats.getAggregate().GetTypeVars();
            TypeArray typeArgsThis = ats.GetTypeArgsThis();
            TypeArray typeArgsAll = ats.GetTypeArgsAll();

            Debug.Assert(typeVars.Count == typeArgsThis.Count);

            if (!ats.fConstraintsChecked)
            {
                ats.fConstraintsChecked = true;
                ats.fConstraintError = false;
            }

            // Check the outer type first. If CheckConstraintsFlags.Outer is not specified and the
            // outer type has already been checked then don't bother checking it.
            if (ats.outerType != null && ((flags & CheckConstraintsFlags.Outer) != 0 || !ats.outerType.fConstraintsChecked))
            {
                CheckConstraints(checker, errHandling, ats.outerType, flags);
                ats.fConstraintError |= ats.outerType.fConstraintError;
            }

            if (typeVars.Count > 0)
                ats.fConstraintError |= !CheckConstraintsCore(checker, errHandling, ats.getAggregate(), typeVars, typeArgsThis, typeArgsAll, null, (flags & CheckConstraintsFlags.NoErrors));

            // Now check type args themselves.
            for (int i = 0; i < typeArgsThis.Count; i++)
            {
                CType arg = typeArgsThis[i].GetNakedType(true);
                if (arg.IsAggregateType() && !arg.AsAggregateType().fConstraintsChecked)
                {
                    CheckConstraints(checker, errHandling, arg.AsAggregateType(), flags | CheckConstraintsFlags.Outer);
                    if (arg.AsAggregateType().fConstraintError)
                        ats.fConstraintError = true;
                }
            }
            return !ats.fConstraintError;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Check the constraints on the method instantiation.
        public static void CheckMethConstraints(CSemanticChecker checker, ErrorHandling errCtx, MethWithInst mwi)
        {
            Debug.Assert(mwi.Meth() != null && mwi.GetType() != null && mwi.TypeArgs != null);
            Debug.Assert(mwi.Meth().typeVars.Count == mwi.TypeArgs.Count);
            Debug.Assert(mwi.GetType().getAggregate() == mwi.Meth().getClass());

            if (mwi.TypeArgs.Count > 0)
            {
                CheckConstraintsCore(checker, errCtx, mwi.Meth(), mwi.Meth().typeVars, mwi.TypeArgs, mwi.GetType().GetTypeArgsAll(), mwi.TypeArgs, CheckConstraintsFlags.None);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////
        // Check whether typeArgs satisfies the constraints of typeVars. The 
        // typeArgsCls and typeArgsMeth are used for substitution on the bounds. The
        // tree and symErr are used for error reporting.

        private static bool CheckConstraintsCore(CSemanticChecker checker, ErrorHandling errHandling, Symbol symErr, TypeArray typeVars, TypeArray typeArgs, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
        {
            Debug.Assert(typeVars.Count == typeArgs.Count);
            Debug.Assert(typeVars.Count > 0);
            Debug.Assert(flags == CheckConstraintsFlags.None || flags == CheckConstraintsFlags.NoErrors);

            bool fError = false;

            for (int i = 0; i < typeVars.Count; i++)
            {
                // Empty bounds should be set to object.
                TypeParameterType var = typeVars.ItemAsTypeParameterType(i);
                CType arg = typeArgs[i];

                bool fOK = CheckSingleConstraint(checker, errHandling, symErr, var, arg, typeArgsCls, typeArgsMeth, flags);
                fError |= !fOK;
            }

            return !fError;
        }

        private static bool CheckSingleConstraint(CSemanticChecker checker, ErrorHandling errHandling, Symbol symErr, TypeParameterType var, CType arg, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
        {
            bool fReportErrors = 0 == (flags & CheckConstraintsFlags.NoErrors);

            if (arg.IsOpenTypePlaceholderType())
            {
                return true;
            }

            if (arg.IsErrorType())
            {
                // Error should have been reported previously.
                return false;
            }

            if (checker.CheckBogus(arg))
            {
                if (fReportErrors)
                {
                    errHandling.ErrorRef(ErrorCode.ERR_BogusType, arg);
                }

                return false;
            }

            if (arg.IsPointerType() || arg.isSpecialByRefType())
            {
                if (fReportErrors)
                {
                    errHandling.Error(ErrorCode.ERR_BadTypeArgument, arg);
                }

                return false;
            }

            if (arg.isStaticClass())
            {
                if (fReportErrors)
                {
                    checker.ReportStaticClassError(null, arg, ErrorCode.ERR_GenericArgIsStaticClass);
                }

                return false;
            }

            bool fError = false;
            if (var.HasRefConstraint() && !arg.IsRefType())
            {
                if (fReportErrors)
                {
                    errHandling.ErrorRef(ErrorCode.ERR_RefConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
                }

                fError = true;
            }

            TypeArray bnds = checker.GetSymbolLoader().GetTypeManager().SubstTypeArray(var.GetBounds(), typeArgsCls, typeArgsMeth);
            int itypeMin = 0;

            if (var.HasValConstraint())
            {
                // If we have a type variable that is constrained to a value type, then we
                // want to check if its a nullable type, so that we can report the 
                // constraint error below. In order to do this however, we need to check 
                // that either the type arg is not a value type, or it is a nullable type.
                //
                // To check whether or not its a nullable type, we need to get the resolved
                // bound from the type argument and check against that.

                bool bIsValueType = arg.IsValType();
                bool bIsNullable = arg.IsNullableType();
                if (bIsValueType && arg.IsTypeParameterType())
                {
                    TypeArray pArgBnds = arg.AsTypeParameterType().GetBounds();
                    if (pArgBnds.Count > 0)
                    {
                        bIsNullable = pArgBnds[0].IsNullableType();
                    }
                }

                if (!bIsValueType || bIsNullable)
                {
                    if (fReportErrors)
                    {
                        errHandling.ErrorRef(ErrorCode.ERR_ValConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
                    }

                    fError = true;
                }

                // Since FValCon() is set it is redundant to check System.ValueType as well.
                if (bnds.Count != 0 && bnds[0].isPredefType(PredefinedType.PT_VALUE))
                {
                    itypeMin = 1;
                }
            }

            for (int j = itypeMin; j < bnds.Count; j++)
            {
                CType typeBnd = bnds[j];
                if (!SatisfiesBound(checker, arg, typeBnd))
                {
                    if (fReportErrors)
                    {
                        // The bound isn't satisfied because of a constraint type. Explain to the user why not.
                        // There are 4 main cases, based on the type of the supplied type argument:
                        //  - reference type, or type parameter known to be a reference type
                        //  - nullable type, from which there is a boxing conversion to the constraint type(see below for details)
                        //  - type variable
                        //  - value type
                        // These cases are broken out because: a) The sets of conversions which can be used 
                        // for constraint satisfaction is different based on the type argument supplied, 
                        // and b) Nullable is one funky type, and user's can use all the help they can get
                        // when using it.
                        ErrorCode error;
                        if (arg.IsRefType())
                        {
                            // A reference type can only satisfy bounds to types 
                            // to which they have an implicit reference conversion
                            error = ErrorCode.ERR_GenericConstraintNotSatisfiedRefType;
                        }
                        else if (arg.IsNullableType() && checker.GetSymbolLoader().HasBaseConversion(arg.AsNullableType().GetUnderlyingType(), typeBnd))    // This is inlining FBoxingConv
                        {
                            // nullable types do not satisfy bounds to every type that they are boxable to
                            // They only satisfy bounds of object and ValueType
                            if (typeBnd.isPredefType(PredefinedType.PT_ENUM) || arg.AsNullableType().GetUnderlyingType() == typeBnd)
                            {
                                // Nullable types don't satisfy bounds of EnumType, or the underlying type of the enum
                                // even though the conversion from Nullable to these types is a boxing conversion
                                // This is a rare case, because these bounds can never be directly stated ...
                                // These bounds can only occur when one type paramter is constrained to a second type parameter
                                // and the second type parameter is instantiated with Enum or the underlying type of the first type
                                // parameter
                                error = ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum;
                            }
                            else
                            {
                                // Nullable types don't satisfy the bounds of any interface type
                                // even when there is a boxing conversion from the Nullable type to 
                                // the interface type. This will be a relatively common scenario
                                // so we cal it out separately from the previous case.
                                Debug.Assert(typeBnd.isInterfaceType());
                                error = ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface;
                            }
                        }
                        else if (arg.IsTypeParameterType())
                        {
                            // Type variables can satisfy bounds through boxing and type variable conversions
                            Debug.Assert(!arg.IsRefType());
                            error = ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar;
                        }
                        else
                        {
                            // Value types can only satisfy bounds through boxing conversions.
                            // Note that the exceptional case of Nullable types and boxing is handled above.
                            error = ErrorCode.ERR_GenericConstraintNotSatisfiedValType;
                        }
                        errHandling.Error(error, new ErrArgRef(symErr), new ErrArg(typeBnd, ErrArgFlags.Unique), var, new ErrArgRef(arg, ErrArgFlags.Unique));
                    }
                    fError = true;
                }
            }

            // Check the newable constraint.
            if (!var.HasNewConstraint() || arg.IsValType())
            {
                return !fError;
            }

            if (arg.isClassType())
            {
                AggregateSymbol agg = arg.AsAggregateType().getAggregate();

                // Due to late binding nature of IDE created symbols, the AggregateSymbol might not
                // have all the information necessary yet, if it is not fully bound.
                // by calling LookupAggMember, it will ensure that we will update all the
                // information necessary at least for the given method.
                checker.GetSymbolLoader().LookupAggMember(NameManager.GetPredefinedName(PredefinedName.PN_CTOR), agg, symbmask_t.MASK_ALL);

                if (agg.HasPubNoArgCtor() && !agg.IsAbstract())
                {
                    return !fError;
                }
            }
            else if (arg.IsTypeParameterType() && arg.AsTypeParameterType().HasNewConstraint())
            {
                return !fError;
            }

            if (fReportErrors)
            {
                errHandling.ErrorRef(ErrorCode.ERR_NewConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Determine whether the arg type satisfies the typeBnd constraint. Note that 
        // typeBnd could be just about any type (since we added naked type parameter
        // constraints).

        private static bool SatisfiesBound(CSemanticChecker checker, CType arg, CType typeBnd)
        {
            if (typeBnd == arg)
                return true;

            switch (typeBnd.GetTypeKind())
            {
                default:
                    Debug.Assert(false, "Unexpected type.");
                    return false;

                case TypeKind.TK_VoidType:
                case TypeKind.TK_PointerType:
                case TypeKind.TK_ErrorType:
                    return false;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_TypeParameterType:
                    break;

                case TypeKind.TK_NullableType:
                    typeBnd = typeBnd.AsNullableType().GetAts(checker.GetErrorContext());
                    if (null == typeBnd)
                        return true;
                    break;

                case TypeKind.TK_AggregateType:
                    break;
            }

            Debug.Assert(typeBnd.IsAggregateType() || typeBnd.IsTypeParameterType() || typeBnd.IsArrayType());

            switch (arg.GetTypeKind())
            {
                default:
                    return false;
                case TypeKind.TK_ErrorType:
                case TypeKind.TK_PointerType:
                    return false;
                case TypeKind.TK_NullableType:
                    arg = arg.AsNullableType().GetAts(checker.GetErrorContext());
                    if (null == arg)
                        return true;
                    // Fall through.
                    goto case TypeKind.TK_TypeParameterType;
                case TypeKind.TK_TypeParameterType:
                case TypeKind.TK_ArrayType:
                case TypeKind.TK_AggregateType:
                    return checker.GetSymbolLoader().HasBaseConversion(arg, typeBnd);
            }
        }
    }
}
