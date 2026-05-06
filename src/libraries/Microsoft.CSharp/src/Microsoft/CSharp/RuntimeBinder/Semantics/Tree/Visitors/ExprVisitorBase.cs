// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprVisitorBase
    {
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected Expr Visit(Expr pExpr) => pExpr == null ? null : Dispatch(pExpr);

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr Dispatch(Expr pExpr) =>
            pExpr.Kind switch
            {
                ExpressionKind.BinaryOp => VisitBINOP(pExpr as ExprBinOp),
                ExpressionKind.UnaryOp => VisitUNARYOP(pExpr as ExprUnaryOp),
                ExpressionKind.Assignment => VisitASSIGNMENT(pExpr as ExprAssignment),
                ExpressionKind.List => VisitLIST(pExpr as ExprList),
                ExpressionKind.ArrayIndex => VisitARRAYINDEX(pExpr as ExprArrayIndex),
                ExpressionKind.Call => VisitCALL(pExpr as ExprCall),
                ExpressionKind.Field => VisitFIELD(pExpr as ExprField),
                ExpressionKind.Local => VisitLOCAL(pExpr as ExprLocal),
                ExpressionKind.Constant => VisitCONSTANT(pExpr as ExprConstant),
                ExpressionKind.Class => pExpr,
                ExpressionKind.Property => VisitPROP(pExpr as ExprProperty),
                ExpressionKind.Multi => VisitMULTI(pExpr as ExprMulti),
                ExpressionKind.MultiGet => VisitMULTIGET(pExpr as ExprMultiGet),
                ExpressionKind.Wrap => VisitWRAP(pExpr as ExprWrap),
                ExpressionKind.Concat => VisitCONCAT(pExpr as ExprConcat),
                ExpressionKind.ArrayInit => VisitARRINIT(pExpr as ExprArrayInit),
                ExpressionKind.Cast => VisitCAST(pExpr as ExprCast),
                ExpressionKind.UserDefinedConversion => VisitUSERDEFINEDCONVERSION(pExpr as ExprUserDefinedConversion),
                ExpressionKind.TypeOf => VisitTYPEOF(pExpr as ExprTypeOf),
                ExpressionKind.ZeroInit => VisitZEROINIT(pExpr as ExprZeroInit),
                ExpressionKind.UserLogicalOp => VisitUSERLOGOP(pExpr as ExprUserLogicalOp),
                ExpressionKind.MemberGroup => VisitMEMGRP(pExpr as ExprMemberGroup),
                ExpressionKind.FieldInfo => VisitFIELDINFO(pExpr as ExprFieldInfo),
                ExpressionKind.MethodInfo => VisitMETHODINFO(pExpr as ExprMethodInfo),

                // Binary operators
                ExpressionKind.EqualsParam => VisitEQUALS(pExpr as ExprBinOp),
                ExpressionKind.Compare => VisitCOMPARE(pExpr as ExprBinOp),
                ExpressionKind.NotEq => VisitNE(pExpr as ExprBinOp),
                ExpressionKind.LessThan => VisitLT(pExpr as ExprBinOp),
                ExpressionKind.LessThanOrEqual => VisitLE(pExpr as ExprBinOp),
                ExpressionKind.GreaterThan => VisitGT(pExpr as ExprBinOp),
                ExpressionKind.GreaterThanOrEqual => VisitGE(pExpr as ExprBinOp),
                ExpressionKind.Add => VisitADD(pExpr as ExprBinOp),
                ExpressionKind.Subtract => VisitSUB(pExpr as ExprBinOp),
                ExpressionKind.Multiply => VisitMUL(pExpr as ExprBinOp),
                ExpressionKind.Divide => VisitDIV(pExpr as ExprBinOp),
                ExpressionKind.Modulo => VisitMOD(pExpr as ExprBinOp),
                ExpressionKind.BitwiseAnd => VisitBITAND(pExpr as ExprBinOp),
                ExpressionKind.BitwiseOr => VisitBITOR(pExpr as ExprBinOp),
                ExpressionKind.BitwiseExclusiveOr => VisitBITXOR(pExpr as ExprBinOp),
                ExpressionKind.LeftShirt => VisitLSHIFT(pExpr as ExprBinOp),
                ExpressionKind.RightShift => VisitRSHIFT(pExpr as ExprBinOp),
                ExpressionKind.LogicalAnd => VisitLOGAND(pExpr as ExprBinOp),
                ExpressionKind.LogicalOr => VisitLOGOR(pExpr as ExprBinOp),
                ExpressionKind.Sequence => VisitSEQUENCE(pExpr as ExprBinOp),
                ExpressionKind.Save => VisitSAVE(pExpr as ExprBinOp),
                ExpressionKind.Swap => VisitSWAP(pExpr as ExprBinOp),
                ExpressionKind.Indir => VisitINDIR(pExpr as ExprBinOp),
                ExpressionKind.StringEq => VisitSTRINGEQ(pExpr as ExprBinOp),
                ExpressionKind.StringNotEq => VisitSTRINGNE(pExpr as ExprBinOp),
                ExpressionKind.DelegateEq => VisitDELEGATEEQ(pExpr as ExprBinOp),
                ExpressionKind.DelegateNotEq => VisitDELEGATENE(pExpr as ExprBinOp),
                ExpressionKind.DelegateAdd => VisitDELEGATEADD(pExpr as ExprBinOp),
                ExpressionKind.DelegateSubtract => VisitDELEGATESUB(pExpr as ExprBinOp),
                ExpressionKind.Eq => VisitEQ(pExpr as ExprBinOp),

                // Unary operators
                ExpressionKind.True => VisitTRUE(pExpr as ExprUnaryOp),
                ExpressionKind.False => VisitFALSE(pExpr as ExprUnaryOp),
                ExpressionKind.Inc => VisitINC(pExpr as ExprUnaryOp),
                ExpressionKind.Dec => VisitDEC(pExpr as ExprUnaryOp),
                ExpressionKind.LogicalNot => VisitLOGNOT(pExpr as ExprUnaryOp),
                ExpressionKind.Negate => VisitNEG(pExpr as ExprUnaryOp),
                ExpressionKind.UnaryPlus => VisitUPLUS(pExpr as ExprUnaryOp),
                ExpressionKind.BitwiseNot => VisitBITNOT(pExpr as ExprUnaryOp),
                ExpressionKind.Addr => VisitADDR(pExpr as ExprUnaryOp),
                ExpressionKind.DecimalNegate => VisitDECIMALNEG(pExpr as ExprUnaryOp),
                ExpressionKind.DecimalInc => VisitDECIMALINC(pExpr as ExprUnaryOp),
                ExpressionKind.DecimalDec => VisitDECIMALDEC(pExpr as ExprUnaryOp),
                _ => throw Error.InternalCompilerError(),
            };

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        private void VisitChildren(Expr pExpr)
        {
            Debug.Assert(pExpr != null);

            Expr exprRet;

            switch (pExpr.Kind)
            {
                case ExpressionKind.List:

                    // Lists are a special case.  We treat a list not as a
                    // binary node but rather as a node with n children.
                    ExprList list = (ExprList)pExpr;
                    while (true)
                    {
                        list.OptionalElement = Visit(list.OptionalElement);
                        Expr nextNode = list.OptionalNextListNode;
                        if (nextNode == null)
                        {
                            return;
                        }

                        if (!(nextNode is ExprList next))
                        {
                            list.OptionalNextListNode = Visit(nextNode);
                            return;
                        }

                        list = next;
                    }

                case ExpressionKind.Assignment:
                    exprRet = Visit((pExpr as ExprAssignment).LHS);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprAssignment).LHS = exprRet;
                    exprRet = Visit((pExpr as ExprAssignment).RHS);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprAssignment).RHS = exprRet;
                    break;

                case ExpressionKind.ArrayIndex:
                    exprRet = Visit((pExpr as ExprArrayIndex).Array);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprArrayIndex).Array = exprRet;
                    exprRet = Visit((pExpr as ExprArrayIndex).Index);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprArrayIndex).Index = exprRet;
                    break;

                case ExpressionKind.UnaryOp:
                case ExpressionKind.True:
                case ExpressionKind.False:
                case ExpressionKind.Inc:
                case ExpressionKind.Dec:
                case ExpressionKind.LogicalNot:
                case ExpressionKind.Negate:
                case ExpressionKind.UnaryPlus:
                case ExpressionKind.BitwiseNot:
                case ExpressionKind.Addr:
                case ExpressionKind.DecimalNegate:
                case ExpressionKind.DecimalInc:
                case ExpressionKind.DecimalDec:
                    exprRet = Visit((pExpr as ExprUnaryOp).Child);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprUnaryOp).Child = exprRet;
                    break;

                case ExpressionKind.UserLogicalOp:
                    exprRet = Visit((pExpr as ExprUserLogicalOp).TrueFalseCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprUserLogicalOp).TrueFalseCall = exprRet;
                    exprRet = Visit((pExpr as ExprUserLogicalOp).OperatorCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprUserLogicalOp).OperatorCall = exprRet as ExprCall;
                    exprRet = Visit((pExpr as ExprUserLogicalOp).FirstOperandToExamine);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprUserLogicalOp).FirstOperandToExamine = exprRet;
                    break;

                case ExpressionKind.TypeOf:
                    break;

                case ExpressionKind.Cast:
                    exprRet = Visit((pExpr as ExprCast).Argument);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprCast).Argument = exprRet;
                    break;

                case ExpressionKind.UserDefinedConversion:
                    exprRet = Visit((pExpr as ExprUserDefinedConversion).UserDefinedCall);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprUserDefinedConversion).UserDefinedCall = exprRet;
                    break;

                case ExpressionKind.ZeroInit:
                    break;

                case ExpressionKind.MemberGroup:

                    // The object expression. NULL for a static invocation.
                    exprRet = Visit((pExpr as ExprMemberGroup).OptionalObject);
                    (pExpr as ExprMemberGroup).OptionalObject = exprRet;
                    break;

                case ExpressionKind.Call:
                    exprRet = Visit((pExpr as ExprCall).OptionalArguments);
                    (pExpr as ExprCall).OptionalArguments = exprRet;
                    exprRet = Visit((pExpr as ExprCall).MemberGroup);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprCall).MemberGroup = exprRet as ExprMemberGroup;
                    break;

                case ExpressionKind.Property:
                    exprRet = Visit((pExpr as ExprProperty).OptionalArguments);
                    (pExpr as ExprProperty).OptionalArguments = exprRet;
                    exprRet = Visit((pExpr as ExprProperty).MemberGroup);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprProperty).MemberGroup = exprRet as ExprMemberGroup;
                    break;

                case ExpressionKind.Field:
                    exprRet = Visit((pExpr as ExprField).OptionalObject);
                    (pExpr as ExprField).OptionalObject = exprRet;
                    break;

                case ExpressionKind.Constant:

                    // Used for when we zeroinit 0 parameter constructors for structs/enums.
                    exprRet = Visit((pExpr as ExprConstant).OptionalConstructorCall);
                    (pExpr as ExprConstant).OptionalConstructorCall = exprRet;
                    break;

                /*************************************************************************************************
                  TYPEEXPRs defined:

                  The following exprs are used to represent the results of type binding, and are defined as follows:

                  TYPEARGUMENTS - This wraps the type arguments for a class. It contains the TypeArray* which is
                    associated with the AggregateType for the instantiation of the class.

                  TYPEORNAMESPACE - This is the base class for this set of Exprs. When binding a type, the result
                    must be a type or a namespace. This Expr encapsulates that fact. The lhs member is the Expr
                    tree that was bound to resolve the type or namespace.

                  TYPEORNAMESPACEERROR - This is the error class for the type or namespace exprs when we don't know
                    what to bind it to.

                  The following two exprs all have a TYPEORNAMESPACE child, which is their fundamental type:
                    POINTERTYPE - This wraps the sym for the pointer type.
                    NULLABLETYPE - This wraps the sym for the nullable type.

                  CLASS - This represents an instantiation of a class.

                  NSPACE - This represents a namespace, which is the intermediate step when attempting to bind
                    a qualified name.

                  ALIAS - This represents an alias

                *************************************************************************************************/

                case ExpressionKind.Multi:
                    exprRet = Visit((pExpr as ExprMulti).Left);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprMulti).Left = exprRet;
                    exprRet = Visit((pExpr as ExprMulti).Operator);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprMulti).Operator = exprRet;
                    break;

                case ExpressionKind.Concat:
                    exprRet = Visit((pExpr as ExprConcat).FirstArgument);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprConcat).FirstArgument = exprRet;
                    exprRet = Visit((pExpr as ExprConcat).SecondArgument);
                    Debug.Assert(exprRet != null);
                    (pExpr as ExprConcat).SecondArgument = exprRet;
                    break;

                case ExpressionKind.ArrayInit:
                    exprRet = Visit((pExpr as ExprArrayInit).OptionalArguments);
                    (pExpr as ExprArrayInit).OptionalArguments = exprRet;
                    exprRet = Visit((pExpr as ExprArrayInit).OptionalArgumentDimensions);
                    (pExpr as ExprArrayInit).OptionalArgumentDimensions = exprRet;
                    break;

                case ExpressionKind.Local:
                case ExpressionKind.Class:
                case ExpressionKind.MultiGet:
                case ExpressionKind.Wrap:
                case ExpressionKind.NoOp:
                case ExpressionKind.FieldInfo:
                case ExpressionKind.MethodInfo:
                    break;

                default:
                    pExpr.AssertIsBin();
                    exprRet = Visit((pExpr as ExprBinOp).OptionalLeftChild);
                    (pExpr as ExprBinOp).OptionalLeftChild = exprRet;
                    exprRet = Visit((pExpr as ExprBinOp).OptionalRightChild);
                    (pExpr as ExprBinOp).OptionalRightChild = exprRet;
                    break;
            }
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitEXPR(Expr pExpr)
        {
            VisitChildren(pExpr);
            return pExpr;
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitBINOP(ExprBinOp pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLIST(ExprList pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitASSIGNMENT(ExprAssignment pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitARRAYINDEX(ExprArrayIndex pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitUNARYOP(ExprUnaryOp pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitUSERLOGOP(ExprUserLogicalOp pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitTYPEOF(ExprTypeOf pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitCAST(ExprCast pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitUSERDEFINEDCONVERSION(ExprUserDefinedConversion pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitZEROINIT(ExprZeroInit pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMEMGRP(ExprMemberGroup pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitCALL(ExprCall pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitPROP(ExprProperty pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitFIELD(ExprField pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLOCAL(ExprLocal pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitCONSTANT(ExprConstant pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMULTIGET(ExprMultiGet pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMULTI(ExprMulti pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitWRAP(ExprWrap pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitCONCAT(ExprConcat pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitARRINIT(ExprArrayInit pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitFIELDINFO(ExprFieldInfo pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMETHODINFO(ExprMethodInfo pExpr)
        {
            return VisitEXPR(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitEQUALS(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitCOMPARE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitNE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitGE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitADD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSUB(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDIV(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitBITAND(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitBITOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLSHIFT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLOGAND(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSEQUENCE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSAVE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitINDIR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSTRINGEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDELEGATEEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDELEGATEADD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMUL(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitBITXOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitRSHIFT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLOGOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSTRINGNE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDELEGATENE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitGT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitMOD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitSWAP(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDELEGATESUB(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitTRUE(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitINC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitLOGNOT(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitNEG(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitBITNOT(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitADDR(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDECIMALNEG(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDECIMALDEC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitFALSE(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDEC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitUPLUS(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        protected virtual Expr VisitDECIMALINC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
    }
}
