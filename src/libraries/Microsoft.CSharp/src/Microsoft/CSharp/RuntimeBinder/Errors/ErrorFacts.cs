// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Errors
{
    internal static class ErrorFacts
    {
        public static string GetMessage(ErrorCode code)
        {
            string codeStr;
            switch (code)
            {
                case ErrorCode.ERR_BadBinaryOps:
                    codeStr = SR.BadBinaryOps;
                    break;
                case ErrorCode.ERR_BadIndexLHS:
                    codeStr = SR.BadIndexLHS;
                    break;
                case ErrorCode.ERR_BadIndexCount:
                    codeStr = SR.BadIndexCount;
                    break;
                case ErrorCode.ERR_BadUnaryOp:
                    codeStr = SR.BadUnaryOp;
                    break;
                case ErrorCode.ERR_NoImplicitConv:
                    codeStr = SR.NoImplicitConv;
                    break;
                case ErrorCode.ERR_NoExplicitConv:
                    codeStr = SR.NoExplicitConv;
                    break;
                case ErrorCode.ERR_ConstOutOfRange:
                    codeStr = SR.ConstOutOfRange;
                    break;
                case ErrorCode.ERR_AmbigBinaryOps:
                    codeStr = SR.AmbigBinaryOps;
                    break;
                case ErrorCode.ERR_AmbigUnaryOp:
                    codeStr = SR.AmbigUnaryOp;
                    break;
                case ErrorCode.ERR_ValueCantBeNull:
                    codeStr = SR.ValueCantBeNull;
                    break;
                case ErrorCode.ERR_WrongNestedThis:
                    codeStr = SR.WrongNestedThis;
                    break;
                case ErrorCode.ERR_NoSuchMember:
                    codeStr = SR.NoSuchMember;
                    break;
                case ErrorCode.ERR_ObjectRequired:
                    codeStr = SR.ObjectRequired;
                    break;
                case ErrorCode.ERR_AmbigCall:
                    codeStr = SR.AmbigCall;
                    break;
                case ErrorCode.ERR_BadAccess:
                    codeStr = SR.BadAccess;
                    break;
                case ErrorCode.ERR_AssgLvalueExpected:
                    codeStr = SR.AssgLvalueExpected;
                    break;
                case ErrorCode.ERR_NoConstructors:
                    codeStr = SR.NoConstructors;
                    break;
                case ErrorCode.ERR_PropertyLacksGet:
                    codeStr = SR.PropertyLacksGet;
                    break;
                case ErrorCode.ERR_ObjectProhibited:
                    codeStr = SR.ObjectProhibited;
                    break;
                case ErrorCode.ERR_AssgReadonly:
                    codeStr = SR.AssgReadonly;
                    break;
                case ErrorCode.ERR_AssgReadonlyStatic:
                    codeStr = SR.AssgReadonlyStatic;
                    break;
                case ErrorCode.ERR_AssgReadonlyProp:
                    codeStr = SR.AssgReadonlyProp;
                    break;
                case ErrorCode.ERR_UnsafeNeeded:
                    codeStr = SR.UnsafeNeeded;
                    break;
                case ErrorCode.ERR_BadBoolOp:
                    codeStr = SR.BadBoolOp;
                    break;
                case ErrorCode.ERR_MustHaveOpTF:
                    codeStr = SR.MustHaveOpTF;
                    break;
                case ErrorCode.ERR_ConstOutOfRangeChecked:
                    codeStr = SR.ConstOutOfRangeChecked;
                    break;
                case ErrorCode.ERR_AmbigMember:
                    codeStr = SR.AmbigMember;
                    break;
                case ErrorCode.ERR_NoImplicitConvCast:
                    codeStr = SR.NoImplicitConvCast;
                    break;
                case ErrorCode.ERR_InaccessibleGetter:
                    codeStr = SR.InaccessibleGetter;
                    break;
                case ErrorCode.ERR_InaccessibleSetter:
                    codeStr = SR.InaccessibleSetter;
                    break;
                case ErrorCode.ERR_BadArity:
                    codeStr = SR.BadArity;
                    break;
                case ErrorCode.ERR_TypeArgsNotAllowed:
                    codeStr = SR.TypeArgsNotAllowed;
                    break;
                case ErrorCode.ERR_HasNoTypeVars:
                    codeStr = SR.HasNoTypeVars;
                    break;
                case ErrorCode.ERR_NewConstraintNotSatisfied:
                    codeStr = SR.NewConstraintNotSatisfied;
                    break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedRefType:
                    codeStr = SR.GenericConstraintNotSatisfiedRefType;
                    break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum:
                    codeStr = SR.GenericConstraintNotSatisfiedNullableEnum;
                    break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface:
                    codeStr = SR.GenericConstraintNotSatisfiedNullableInterface;
                    break;
                case ErrorCode.ERR_GenericConstraintNotSatisfiedValType:
                    codeStr = SR.GenericConstraintNotSatisfiedValType;
                    break;
                case ErrorCode.ERR_CantInferMethTypeArgs:
                    codeStr = SR.CantInferMethTypeArgs;
                    break;
                case ErrorCode.ERR_RefConstraintNotSatisfied:
                    codeStr = SR.RefConstraintNotSatisfied;
                    break;
                case ErrorCode.ERR_ValConstraintNotSatisfied:
                    codeStr = SR.ValConstraintNotSatisfied;
                    break;
                case ErrorCode.ERR_AmbigUDConv:
                    codeStr = SR.AmbigUDConv;
                    break;
                case ErrorCode.ERR_BindToBogus:
                    codeStr = SR.BindToBogus;
                    break;
                case ErrorCode.ERR_CantCallSpecialMethod:
                    codeStr = SR.CantCallSpecialMethod;
                    break;
                case ErrorCode.ERR_ConvertToStaticClass:
                    codeStr = SR.ConvertToStaticClass;
                    break;
                case ErrorCode.ERR_IncrementLvalueExpected:
                    codeStr = SR.IncrementLvalueExpected;
                    break;
                case ErrorCode.ERR_BadArgCount:
                    codeStr = SR.BadArgCount;
                    break;
                case ErrorCode.ERR_BadArgTypes:
                    codeStr = SR.BadArgTypes;
                    break;
                case ErrorCode.ERR_BadProtectedAccess:
                    codeStr = SR.BadProtectedAccess;
                    break;
                case ErrorCode.ERR_BindToBogusProp2:
                    codeStr = SR.BindToBogusProp2;
                    break;
                case ErrorCode.ERR_BindToBogusProp1:
                    codeStr = SR.BindToBogusProp1;
                    break;
                case ErrorCode.ERR_BadDelArgCount:
                    codeStr = SR.BadDelArgCount;
                    break;
                case ErrorCode.ERR_BadDelArgTypes:
                    codeStr = SR.BadDelArgTypes;
                    break;
                case ErrorCode.ERR_ReturnNotLValue:
                    codeStr = SR.ReturnNotLValue;
                    break;
                case ErrorCode.ERR_AssgReadonly2:
                    codeStr = SR.AssgReadonly2;
                    break;
                case ErrorCode.ERR_AssgReadonlyStatic2:
                    codeStr = SR.AssgReadonlyStatic2;
                    break;
                case ErrorCode.ERR_BadCtorArgCount:
                    codeStr = SR.BadCtorArgCount;
                    break;
                case ErrorCode.ERR_NonInvocableMemberCalled:
                    codeStr = SR.NonInvocableMemberCalled;
                    break;
                case ErrorCode.ERR_BadNamedArgument:
                    codeStr = SR.BadNamedArgument;
                    break;
                case ErrorCode.ERR_BadNamedArgumentForDelegateInvoke:
                    codeStr = SR.BadNamedArgumentForDelegateInvoke;
                    break;
                case ErrorCode.ERR_DuplicateNamedArgument:
                    codeStr = SR.DuplicateNamedArgument;
                    break;
                case ErrorCode.ERR_NamedArgumentUsedInPositional:
                    codeStr = SR.NamedArgumentUsedInPositional;
                    break;
                case ErrorCode.ERR_BadNonTrailingNamedArgument:
                    codeStr = SR.BadNonTrailingNamedArgument;
                    break;

                default:
                    // means missing resources match the code entry
                    Debug.Assert(false, "Missing resources for the error " + code.ToString());
                    codeStr = null;
                    break;
            }

            return codeStr;
        }

        public static string GetMessage(MessageID id)
        {
            return id.ToString();
        }
    }
}
