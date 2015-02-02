// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal static class ErrorFacts
    {
        public static string GetMessage(ErrorCode code)
        {
            string codeStr = null;
            switch (code)
            {
                case ErrorCode.ERR_BadBinaryOps: codeStr = SR.BadBinaryOps; break;
                case ErrorCode.ERR_IntDivByZero: codeStr = SR.IntDivByZero; break;
                case ErrorCode.ERR_BadIndexLHS: codeStr = SR.BadIndexLHS; break;
                case ErrorCode.ERR_BadIndexCount: codeStr = SR.BadIndexCount; break;
                case ErrorCode.ERR_BadUnaryOp: codeStr = SR.BadUnaryOp; break;
                case ErrorCode.ERR_NoImplicitConv: codeStr = SR.NoImplicitConv; break;
                case ErrorCode.ERR_NoExplicitConv: codeStr = SR.NoExplicitConv; break;
                case ErrorCode.ERR_ConstOutOfRange: codeStr = SR.ConstOutOfRange; break;
                case ErrorCode.ERR_AmbigBinaryOps: codeStr = SR.AmbigBinaryOps; break;
                case ErrorCode.ERR_AmbigUnaryOp: codeStr = SR.AmbigUnaryOp; break;
                case ErrorCode.ERR_ValueCantBeNull: codeStr = SR.ValueCantBeNull; break;
                case ErrorCode.ERR_WrongNestedThis: codeStr = SR.WrongNestedThis; break;
                case ErrorCode.ERR_NoSuchMember: codeStr = SR.NoSuchMember; break;
                case ErrorCode.ERR_ObjectRequired: codeStr = SR.ObjectRequired; break;
                case ErrorCode.ERR_AmbigCall: codeStr = SR.AmbigCall; break;
                case ErrorCode.ERR_BadAccess: codeStr = SR.BadAccess; break;
                case ErrorCode.ERR_MethDelegateMismatch: codeStr = SR.MethDelegateMismatch; break;
                case ErrorCode.ERR_AssgLvalueExpected: codeStr = SR.AssgLvalueExpected; break;
                case ErrorCode.ERR_NoConstructors: codeStr = SR.NoConstructors; break;
                case ErrorCode.ERR_BadDelegateConstructor: codeStr = SR.BadDelegateConstructor; break;
                case ErrorCode.ERR_PropertyLacksGet: codeStr = SR.PropertyLacksGet; break;
                case ErrorCode.ERR_ObjectProhibited: codeStr = SR.ObjectProhibited; break;
                case ErrorCode.ERR_AssgReadonly: codeStr = SR.AssgReadonly; break;
                case ErrorCode.ERR_RefReadonly: codeStr = SR.RefReadonly; break;
                case ErrorCode.ERR_AssgReadonlyStatic: codeStr = SR.AssgReadonlyStatic; break;
                case ErrorCode.ERR_RefReadonlyStatic: codeStr = SR.RefReadonlyStatic; break;
                case ErrorCode.ERR_AssgReadonlyProp: codeStr = SR.AssgReadonlyProp; break;
                case ErrorCode.ERR_AbstractBaseCall: codeStr = SR.AbstractBaseCall; break;
                case ErrorCode.ERR_RefProperty: codeStr = SR.RefProperty; break;
                case ErrorCode.ERR_ManagedAddr: codeStr = SR.ManagedAddr; break;
                case ErrorCode.ERR_FixedNotNeeded: codeStr = SR.FixedNotNeeded; break;
                case ErrorCode.ERR_UnsafeNeeded: codeStr = SR.UnsafeNeeded; break;
                case ErrorCode.ERR_BadBoolOp: codeStr = SR.BadBoolOp; break;
                case ErrorCode.ERR_MustHaveOpTF: codeStr = SR.MustHaveOpTF; break;
                case ErrorCode.ERR_CheckedOverflow: codeStr = SR.CheckedOverflow; break;
                case ErrorCode.ERR_ConstOutOfRangeChecked: codeStr = SR.ConstOutOfRangeChecked; break;
                case ErrorCode.ERR_AmbigMember: codeStr = SR.AmbigMember; break;
                case ErrorCode.ERR_SizeofUnsafe: codeStr = SR.SizeofUnsafe; break;
                case ErrorCode.ERR_FieldInitRefNonstatic: codeStr = SR.FieldInitRefNonstatic; break;
                case ErrorCode.ERR_CallingFinalizeDepracated: codeStr = SR.CallingFinalizeDepracated; break;
                case ErrorCode.ERR_CallingBaseFinalizeDeprecated: codeStr = SR.CallingBaseFinalizeDeprecated; break;
                case ErrorCode.ERR_BadCastInFixed: codeStr = SR.BadCastInFixed; break;
                case ErrorCode.ERR_NoImplicitConvCast: codeStr = SR.NoImplicitConvCast; break;
                case ErrorCode.ERR_InaccessibleGetter: codeStr = SR.InaccessibleGetter; break;
                case ErrorCode.ERR_InaccessibleSetter: codeStr = SR.InaccessibleSetter; break;
                case ErrorCode.ERR_BadArity: codeStr = SR.BadArity; break;
                case ErrorCode.ERR_BadTypeArgument: codeStr = SR.BadTypeArgument; break;
                case ErrorCode.ERR_TypeArgsNotAllowed: codeStr = SR.TypeArgsNotAllowed; break;
                case ErrorCode.ERR_HasNoTypeVars: codeStr = SR.HasNoTypeVars; break;
                case ErrorCode.ERR_NewConstraintNotSatisfied: codeStr = SR.NewConstraintNotSatisfied; break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedRefType: codeStr = SR.GenericConstraintNotSatisfiedRefType; break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum: codeStr = SR.GenericConstraintNotSatisfiedNullableEnum; break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface: codeStr = SR.GenericConstraintNotSatisfiedNullableInterface; break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar: codeStr = SR.GenericConstraintNotSatisfiedTyVar; break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedValType: codeStr = SR.GenericConstraintNotSatisfiedValType; break;
                case ErrorCode.ERR_TypeVarCantBeNull: codeStr = SR.TypeVarCantBeNull; break;
                case ErrorCode.ERR_BadRetType: codeStr = SR.BadRetType; break;
                case ErrorCode.ERR_CantInferMethTypeArgs: codeStr = SR.CantInferMethTypeArgs; break;
                case ErrorCode.ERR_MethGrpToNonDel: codeStr = SR.MethGrpToNonDel; break;
                case ErrorCode.ERR_RefConstraintNotSatisfied: codeStr = SR.RefConstraintNotSatisfied; break;
                case ErrorCode.ERR_ValConstraintNotSatisfied: codeStr = SR.ValConstraintNotSatisfied; break;
                case ErrorCode.ERR_CircularConstraint: codeStr = SR.CircularConstraint; break;
                case ErrorCode.ERR_BaseConstraintConflict: codeStr = SR.BaseConstraintConflict; break;
                case ErrorCode.ERR_ConWithValCon: codeStr = SR.ConWithValCon; break;
                case ErrorCode.ERR_AmbigUDConv: codeStr = SR.AmbigUDConv; break;
                case ErrorCode.ERR_PredefinedTypeNotFound: codeStr = SR.PredefinedTypeNotFound; break;
                case ErrorCode.ERR_PredefinedTypeBadType: codeStr = SR.PredefinedTypeBadType; break;
                case ErrorCode.ERR_BindToBogus: codeStr = SR.BindToBogus; break;
                case ErrorCode.ERR_CantCallSpecialMethod: codeStr = SR.CantCallSpecialMethod; break;
                case ErrorCode.ERR_BogusType: codeStr = SR.BogusType; break;
                case ErrorCode.ERR_MissingPredefinedMember: codeStr = SR.MissingPredefinedMember; break;
                case ErrorCode.ERR_LiteralDoubleCast: codeStr = SR.LiteralDoubleCast; break;
                case ErrorCode.ERR_UnifyingInterfaceInstantiations: codeStr = SR.UnifyingInterfaceInstantiations; break;
                case ErrorCode.ERR_ConvertToStaticClass: codeStr = SR.ConvertToStaticClass; break;
                case ErrorCode.ERR_GenericArgIsStaticClass: codeStr = SR.GenericArgIsStaticClass; break;
                case ErrorCode.ERR_PartialMethodToDelegate: codeStr = SR.PartialMethodToDelegate; break;
                case ErrorCode.ERR_IncrementLvalueExpected: codeStr = SR.IncrementLvalueExpected; break;
                case ErrorCode.ERR_NoSuchMemberOrExtension: codeStr = SR.NoSuchMemberOrExtension; break;
                case ErrorCode.ERR_ValueTypeExtDelegate: codeStr = SR.ValueTypeExtDelegate; break;
                case ErrorCode.ERR_BadArgCount: codeStr = SR.BadArgCount; break;
                case ErrorCode.ERR_BadArgTypes: codeStr = SR.BadArgTypes; break;
                case ErrorCode.ERR_BadArgType: codeStr = SR.BadArgType; break;
                case ErrorCode.ERR_RefLvalueExpected: codeStr = SR.RefLvalueExpected; break;
                case ErrorCode.ERR_BadProtectedAccess: codeStr = SR.BadProtectedAccess; break;
                case ErrorCode.ERR_BindToBogusProp2: codeStr = SR.BindToBogusProp2; break;
                case ErrorCode.ERR_BindToBogusProp1: codeStr = SR.BindToBogusProp1; break;
                case ErrorCode.ERR_BadDelArgCount: codeStr = SR.BadDelArgCount; break;
                case ErrorCode.ERR_BadDelArgTypes: codeStr = SR.BadDelArgTypes; break;
                case ErrorCode.ERR_AssgReadonlyLocal: codeStr = SR.AssgReadonlyLocal; break;
                case ErrorCode.ERR_RefReadonlyLocal: codeStr = SR.RefReadonlyLocal; break;
                case ErrorCode.ERR_ReturnNotLValue: codeStr = SR.ReturnNotLValue; break;
                case ErrorCode.ERR_BadArgExtraRef: codeStr = SR.BadArgExtraRef; break;
                case ErrorCode.ERR_BadArgRef: codeStr = SR.BadArgRef; break;
                case ErrorCode.ERR_AssgReadonly2: codeStr = SR.AssgReadonly2; break;
                case ErrorCode.ERR_RefReadonly2: codeStr = SR.RefReadonly2; break;
                case ErrorCode.ERR_AssgReadonlyStatic2: codeStr = SR.AssgReadonlyStatic2; break;
                case ErrorCode.ERR_RefReadonlyStatic2: codeStr = SR.RefReadonlyStatic2; break;
                case ErrorCode.ERR_AssgReadonlyLocalCause: codeStr = SR.AssgReadonlyLocalCause; break;
                case ErrorCode.ERR_RefReadonlyLocalCause: codeStr = SR.RefReadonlyLocalCause; break;
                case ErrorCode.ERR_ThisStructNotInAnonMeth: codeStr = SR.ThisStructNotInAnonMeth; break;
                case ErrorCode.ERR_DelegateOnNullable: codeStr = SR.DelegateOnNullable; break;
                case ErrorCode.ERR_BadCtorArgCount: codeStr = SR.BadCtorArgCount; break;
                case ErrorCode.ERR_BadExtensionArgTypes: codeStr = SR.BadExtensionArgTypes; break;
                case ErrorCode.ERR_BadInstanceArgType: codeStr = SR.BadInstanceArgType; break;
                case ErrorCode.ERR_BadArgTypesForCollectionAdd: codeStr = SR.BadArgTypesForCollectionAdd; break;
                case ErrorCode.ERR_InitializerAddHasParamModifiers: codeStr = SR.InitializerAddHasParamModifiers; break;
                case ErrorCode.ERR_NonInvocableMemberCalled: codeStr = SR.NonInvocableMemberCalled; break;
                case ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument: codeStr = SR.NamedArgumentSpecificationBeforeFixedArgument; break;
                case ErrorCode.ERR_BadNamedArgument: codeStr = SR.BadNamedArgument; break;
                case ErrorCode.ERR_BadNamedArgumentForDelegateInvoke: codeStr = SR.BadNamedArgumentForDelegateInvoke; break;
                case ErrorCode.ERR_DuplicateNamedArgument: codeStr = SR.DuplicateNamedArgument; break;
                case ErrorCode.ERR_NamedArgumentUsedInPositional: codeStr = SR.NamedArgumentUsedInPositional; break;
                default:
                    Debug.Assert(false, "Missing resources for the error " + code.ToString()); // means missing resources match the code entry
                    break;
            }

            if (codeStr == null)
            {
                return null;
            }

            return codeStr;
        }

        public static string GetMessage(MessageID id)
        {
            return id.ToString();
        }
    }
}
