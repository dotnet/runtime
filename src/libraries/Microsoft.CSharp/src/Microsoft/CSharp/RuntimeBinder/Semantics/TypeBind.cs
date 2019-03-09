// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Errors;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Defines the structure used when binding types

    // CheckConstraints options.
    [Flags]
    internal enum CheckConstraintsFlags
    {
        None = 0x00,
        Outer = 0x01,
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
        public static bool CheckConstraints(CType type, CheckConstraintsFlags flags)
        {
            type = type.GetNakedType(false);

            if (!(type is AggregateType ats))
            {
                if (type is NullableType nub)
                {
                    ats = nub.GetAts();
                }
                else
                {
                    return true;
                }
            }

            if (ats.TypeArgsAll.Count == 0)
            {
                // Common case: there are no type vars, so there are no constraints.
                ats.ConstraintError = false;
                return true;
            }

            // Already checked.
            if (ats.ConstraintError.HasValue)
            {
                // No errors
                if (!ats.ConstraintError.GetValueOrDefault())
                {
                    return true;
                }

                // We want the result, not an exception.
                if ((flags & CheckConstraintsFlags.NoErrors) != 0)
                {
                    return false;
                }
            }

            TypeArray typeVars = ats.OwningAggregate.GetTypeVars();
            TypeArray typeArgsThis = ats.TypeArgsThis;
            TypeArray typeArgsAll = ats.TypeArgsAll;

            Debug.Assert(typeVars.Count == typeArgsThis.Count);

            // Check the outer type first. If CheckConstraintsFlags.Outer is not specified and the
            // outer type has already been checked then don't bother checking it.
            if (ats.OuterType != null && ((flags & CheckConstraintsFlags.Outer) != 0 || !ats.OuterType.ConstraintError.HasValue))
            {
                if (!CheckConstraints(ats.OuterType, flags))
                {
                    ats.ConstraintError = true;
                    return false;
                }
            }

            if (typeVars.Count > 0)
            {
                if (!CheckConstraintsCore(ats.OwningAggregate, typeVars, typeArgsThis, typeArgsAll, null, flags & CheckConstraintsFlags.NoErrors))
                {
                    ats.ConstraintError = true;
                    return false;
                }
            }

            // Now check type args themselves.
            for (int i = 0; i < typeArgsThis.Count; i++)
            {
                CType arg = typeArgsThis[i].GetNakedType(true);
                if (arg is AggregateType atArg && !atArg.ConstraintError.HasValue)
                {
                    CheckConstraints(atArg, flags | CheckConstraintsFlags.Outer);
                    if (atArg.ConstraintError.GetValueOrDefault())
                    {
                        ats.ConstraintError = true;
                        return false;
                    }
                }
            }

            ats.ConstraintError = false;
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Check the constraints on the method instantiation.
        public static void CheckMethConstraints(MethWithInst mwi)
        {
            Debug.Assert(mwi.Meth() != null && mwi.GetType() != null && mwi.TypeArgs != null);
            Debug.Assert(mwi.Meth().typeVars.Count == mwi.TypeArgs.Count);
            Debug.Assert(mwi.GetType().OwningAggregate == mwi.Meth().getClass());

            if (mwi.TypeArgs.Count > 0)
            {
                CheckConstraintsCore(mwi.Meth(), mwi.Meth().typeVars, mwi.TypeArgs, mwi.GetType().TypeArgsAll, mwi.TypeArgs, CheckConstraintsFlags.None);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Check whether typeArgs satisfies the constraints of typeVars. The 
        // typeArgsCls and typeArgsMeth are used for substitution on the bounds. The
        // tree and symErr are used for error reporting.
        private static bool CheckConstraintsCore(Symbol symErr, TypeArray typeVars, TypeArray typeArgs, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
        {
            Debug.Assert(typeVars.Count == typeArgs.Count);
            Debug.Assert(typeVars.Count > 0);
            Debug.Assert(flags == CheckConstraintsFlags.None || flags == CheckConstraintsFlags.NoErrors);

            for (int i = 0; i < typeVars.Count; i++)
            {
                // Empty bounds should be set to object.
                TypeParameterType var = (TypeParameterType)typeVars[i];
                CType arg = typeArgs[i];

                if (!CheckSingleConstraint(symErr, var, arg, typeArgsCls, typeArgsMeth, flags))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CheckSingleConstraint(Symbol symErr, TypeParameterType var, CType arg, TypeArray typeArgsCls, TypeArray typeArgsMeth, CheckConstraintsFlags flags)
        {
            Debug.Assert(!(arg is PointerType));
            Debug.Assert(!arg.IsStaticClass);

            bool fReportErrors = 0 == (flags & CheckConstraintsFlags.NoErrors);

            if (var.HasRefConstraint && !arg.IsReferenceType)
            {
                if (fReportErrors)
                {
                    throw ErrorHandling.Error(ErrorCode.ERR_RefConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
                }

                return false;
            }

            TypeArray bnds = TypeManager.SubstTypeArray(var.Bounds, typeArgsCls, typeArgsMeth);
            int itypeMin = 0;

            if (var.HasValConstraint)
            {
                // If we have a type variable that is constrained to a value type, then we
                // want to check if its a nullable type, so that we can report the 
                // constraint error below. In order to do this however, we need to check 
                // that either the type arg is not a value type, or it is a nullable type.
                //
                // To check whether or not its a nullable type, we need to get the resolved
                // bound from the type argument and check against that.

                if (!arg.IsNonNullableValueType)
                {
                    if (fReportErrors)
                    {
                        throw ErrorHandling.Error(ErrorCode.ERR_ValConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
                    }

                    return false;
                }

                // Since FValCon() is set it is redundant to check System.ValueType as well.
                if (bnds.Count != 0 && bnds[0].IsPredefType(PredefinedType.PT_VALUE))
                {
                    itypeMin = 1;
                }
            }

            for (int j = itypeMin; j < bnds.Count; j++)
            {
                CType typeBnd = bnds[j];
                if (!SatisfiesBound(arg, typeBnd))
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
                        if (arg.IsReferenceType)
                        {
                            // A reference type can only satisfy bounds to types 
                            // to which they have an implicit reference conversion
                            error = ErrorCode.ERR_GenericConstraintNotSatisfiedRefType;
                        }
                        else if (arg is NullableType nubArg && SymbolLoader.HasBaseConversion(nubArg.UnderlyingType, typeBnd))    // This is inlining FBoxingConv
                        {
                            // nullable types do not satisfy bounds to every type that they are boxable to
                            // They only satisfy bounds of object and ValueType
                            if (typeBnd.IsPredefType(PredefinedType.PT_ENUM) || nubArg.UnderlyingType == typeBnd)
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
                                Debug.Assert(typeBnd.IsInterfaceType);
                                error = ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface;
                            }
                        }
                        else
                        {
                            // Value types can only satisfy bounds through boxing conversions.
                            // Note that the exceptional case of Nullable types and boxing is handled above.
                            error = ErrorCode.ERR_GenericConstraintNotSatisfiedValType;
                        }

                        throw ErrorHandling.Error(error, new ErrArg(symErr), new ErrArg(typeBnd, ErrArgFlags.Unique), var, new ErrArg(arg, ErrArgFlags.Unique));
                    }

                    return false;
                }
            }

            // Check the newable constraint.
            if (!var.HasNewConstraint || arg.IsValueType)
            {
                return true;
            }

            if (arg.IsClassType)
            {
                AggregateSymbol agg = ((AggregateType)arg).OwningAggregate;

                // Due to late binding nature of IDE created symbols, the AggregateSymbol might not
                // have all the information necessary yet, if it is not fully bound.
                // by calling LookupAggMember, it will ensure that we will update all the
                // information necessary at least for the given method.
                SymbolLoader.LookupAggMember(NameManager.GetPredefinedName(PredefinedName.PN_CTOR), agg, symbmask_t.MASK_ALL);

                if (agg.HasPubNoArgCtor() && !agg.IsAbstract())
                {
                    return true;
                }
            }

            if (fReportErrors)
            {
                throw ErrorHandling.Error(ErrorCode.ERR_NewConstraintNotSatisfied, symErr, new ErrArgNoRef(var), arg);
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Determine whether the arg type satisfies the typeBnd constraint. Note that 
        // typeBnd could be just about any type (since we added naked type parameter
        // constraints).

        private static bool SatisfiesBound(CType arg, CType typeBnd)
        {
            if (typeBnd == arg)
                return true;

            switch (typeBnd.TypeKind)
            {
                default:
                    Debug.Fail("Unexpected type.");
                    return false;

                case TypeKind.TK_VoidType:
                case TypeKind.TK_PointerType:
                    return false;

                case TypeKind.TK_ArrayType:
                case TypeKind.TK_TypeParameterType:
                    break;

                case TypeKind.TK_NullableType:
                    typeBnd = ((NullableType)typeBnd).GetAts();
                    break;

                case TypeKind.TK_AggregateType:
                    break;
            }

            Debug.Assert(typeBnd is AggregateType || typeBnd is TypeParameterType || typeBnd is ArrayType);

            switch (arg.TypeKind)
            {
                default:
                    return false;
                case TypeKind.TK_PointerType:
                    return false;
                case TypeKind.TK_NullableType:
                    arg = ((NullableType)arg).GetAts();
                    // Fall through.
                    goto case TypeKind.TK_TypeParameterType;
                case TypeKind.TK_TypeParameterType:
                case TypeKind.TK_ArrayType:
                case TypeKind.TK_AggregateType:
                    return SymbolLoader.HasBaseConversion(arg, typeBnd);
            }
        }
    }
}
