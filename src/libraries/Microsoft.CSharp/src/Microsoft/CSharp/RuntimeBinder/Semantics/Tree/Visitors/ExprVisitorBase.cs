// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal abstract class ExprVisitorBase
    {
        public Expr Visit(Expr pExpr)
        {
            if (pExpr == null)
            {
                return null;
            }

            Expr pResult;
            if (IsCachedExpr(pExpr, out pResult))
            {
                return pResult;
            }

            return CacheExprMapping(pExpr, Dispatch(pExpr));
        }

        private bool IsCachedExpr(Expr pExpr, out Expr pTransformedExpr)
        {
            pTransformedExpr = null;
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expr CacheExprMapping(Expr pExpr, Expr pTransformedExpr)
        {
            return pTransformedExpr;
        }

        protected virtual Expr Dispatch(Expr pExpr)
        {
            switch (pExpr.Kind)
            {
                case ExpressionKind.BinaryOp:
                    return VisitBINOP(pExpr as ExprBinOp);
                case ExpressionKind.UnaryOp:
                    return VisitUNARYOP(pExpr as ExprUnaryOp);
                case ExpressionKind.Assignment:
                    return VisitASSIGNMENT(pExpr as ExprAssignment);
                case ExpressionKind.List:
                    return VisitLIST(pExpr as ExprList);
                case ExpressionKind.ArrayIndex:
                    return VisitARRAYINDEX(pExpr as ExprArrayIndex);
                case ExpressionKind.Call:
                    return VisitCALL(pExpr as ExprCall);
                case ExpressionKind.Field:
                    return VisitFIELD(pExpr as ExprField);
                case ExpressionKind.Local:
                    return VisitLOCAL(pExpr as ExprLocal);
                case ExpressionKind.Constant:
                    return VisitCONSTANT(pExpr as ExprConstant);
                case ExpressionKind.Class:
                    return pExpr;
                case ExpressionKind.Property:
                    return VisitPROP(pExpr as ExprProperty);
                case ExpressionKind.Multi:
                    return VisitMULTI(pExpr as ExprMulti);
                case ExpressionKind.MultiGet:
                    return VisitMULTIGET(pExpr as ExprMultiGet);
                case ExpressionKind.Wrap:
                    return VisitWRAP(pExpr as ExprWrap);
                case ExpressionKind.Concat:
                    return VisitCONCAT(pExpr as ExprConcat);
                case ExpressionKind.ArrayInit:
                    return VisitARRINIT(pExpr as ExprArrayInit);
                case ExpressionKind.Cast:
                    return VisitCAST(pExpr as ExprCast);
                case ExpressionKind.UserDefinedConversion:
                    return VisitUSERDEFINEDCONVERSION(pExpr as ExprUserDefinedConversion);
                case ExpressionKind.TypeOf:
                    return VisitTYPEOF(pExpr as ExprTypeOf);
                case ExpressionKind.ZeroInit:
                    return VisitZEROINIT(pExpr as ExprZeroInit);
                case ExpressionKind.UserLogicalOp:
                    return VisitUSERLOGOP(pExpr as ExprUserLogicalOp);
                case ExpressionKind.MemberGroup:
                    return VisitMEMGRP(pExpr as ExprMemberGroup);
                case ExpressionKind.FieldInfo:
                    return VisitFIELDINFO(pExpr as ExprFieldInfo);
                case ExpressionKind.MethodInfo:
                    return VisitMETHODINFO(pExpr as ExprMethodInfo);

                // Binary operators
                case ExpressionKind.EqualsParam:
                    return VisitEQUALS(pExpr as ExprBinOp);
                case ExpressionKind.Compare:
                    return VisitCOMPARE(pExpr as ExprBinOp);
                case ExpressionKind.NotEq:
                    return VisitNE(pExpr as ExprBinOp);
                case ExpressionKind.LessThan:
                    return VisitLT(pExpr as ExprBinOp);
                case ExpressionKind.LessThanOrEqual:
                    return VisitLE(pExpr as ExprBinOp);
                case ExpressionKind.GreaterThan:
                    return VisitGT(pExpr as ExprBinOp);
                case ExpressionKind.GreaterThanOrEqual:
                    return VisitGE(pExpr as ExprBinOp);
                case ExpressionKind.Add:
                    return VisitADD(pExpr as ExprBinOp);
                case ExpressionKind.Subtract:
                    return VisitSUB(pExpr as ExprBinOp);
                case ExpressionKind.Multiply:
                    return VisitMUL(pExpr as ExprBinOp);
                case ExpressionKind.Divide:
                    return VisitDIV(pExpr as ExprBinOp);
                case ExpressionKind.Modulo:
                    return VisitMOD(pExpr as ExprBinOp);
                case ExpressionKind.BitwiseAnd:
                    return VisitBITAND(pExpr as ExprBinOp);
                case ExpressionKind.BitwiseOr:
                    return VisitBITOR(pExpr as ExprBinOp);
                case ExpressionKind.BitwiseExclusiveOr:
                    return VisitBITXOR(pExpr as ExprBinOp);
                case ExpressionKind.LeftShirt:
                    return VisitLSHIFT(pExpr as ExprBinOp);
                case ExpressionKind.RightShift:
                    return VisitRSHIFT(pExpr as ExprBinOp);
                case ExpressionKind.LogicalAnd:
                    return VisitLOGAND(pExpr as ExprBinOp);
                case ExpressionKind.LogicalOr:
                    return VisitLOGOR(pExpr as ExprBinOp);
                case ExpressionKind.Sequence:
                    return VisitSEQUENCE(pExpr as ExprBinOp);
                case ExpressionKind.Save:
                    return VisitSAVE(pExpr as ExprBinOp);
                case ExpressionKind.Swap:
                    return VisitSWAP(pExpr as ExprBinOp);
                case ExpressionKind.Indir:
                    return VisitINDIR(pExpr as ExprBinOp);
                case ExpressionKind.StringEq:
                    return VisitSTRINGEQ(pExpr as ExprBinOp);
                case ExpressionKind.StringNotEq:
                    return VisitSTRINGNE(pExpr as ExprBinOp);
                case ExpressionKind.DelegateEq:
                    return VisitDELEGATEEQ(pExpr as ExprBinOp);
                case ExpressionKind.DelegateNotEq:
                    return VisitDELEGATENE(pExpr as ExprBinOp);
                case ExpressionKind.DelegateAdd:
                    return VisitDELEGATEADD(pExpr as ExprBinOp);
                case ExpressionKind.DelegateSubtract:
                    return VisitDELEGATESUB(pExpr as ExprBinOp);
                case ExpressionKind.Eq:
                    return VisitEQ(pExpr as ExprBinOp);

                // Unary operators
                case ExpressionKind.True:
                    return VisitTRUE(pExpr as ExprUnaryOp);
                case ExpressionKind.False:
                    return VisitFALSE(pExpr as ExprUnaryOp);
                case ExpressionKind.Inc:
                    return VisitINC(pExpr as ExprUnaryOp);
                case ExpressionKind.Dec:
                    return VisitDEC(pExpr as ExprUnaryOp);
                case ExpressionKind.LogicalNot:
                    return VisitLOGNOT(pExpr as ExprUnaryOp);
                case ExpressionKind.Negate:
                    return VisitNEG(pExpr as ExprUnaryOp);
                case ExpressionKind.UnaryPlus:
                    return VisitUPLUS(pExpr as ExprUnaryOp);
                case ExpressionKind.BitwiseNot:
                    return VisitBITNOT(pExpr as ExprUnaryOp);
                case ExpressionKind.Addr:
                    return VisitADDR(pExpr as ExprUnaryOp);
                case ExpressionKind.DecimalNegate:
                    return VisitDECIMALNEG(pExpr as ExprUnaryOp);
                case ExpressionKind.DecimalInc:
                    return VisitDECIMALINC(pExpr as ExprUnaryOp);
                case ExpressionKind.DecimalDec:
                    return VisitDECIMALDEC(pExpr as ExprUnaryOp);
                default:
                    throw Error.InternalCompilerError();
            }
        }

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

        protected virtual Expr VisitEXPR(Expr pExpr)
        {
            VisitChildren(pExpr);
            return pExpr;
        }
        protected virtual Expr VisitBINOP(ExprBinOp pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitLIST(ExprList pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitASSIGNMENT(ExprAssignment pExpr)
        {
            return VisitEXPR(pExpr);
        }

        protected virtual Expr VisitARRAYINDEX(ExprArrayIndex pExpr)
        {
            return VisitEXPR(pExpr);
        }

        protected virtual Expr VisitUNARYOP(ExprUnaryOp pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitUSERLOGOP(ExprUserLogicalOp pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitTYPEOF(ExprTypeOf pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitCAST(ExprCast pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitUSERDEFINEDCONVERSION(ExprUserDefinedConversion pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitZEROINIT(ExprZeroInit pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitMEMGRP(ExprMemberGroup pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitCALL(ExprCall pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitPROP(ExprProperty pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitFIELD(ExprField pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitLOCAL(ExprLocal pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitCONSTANT(ExprConstant pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitMULTIGET(ExprMultiGet pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitMULTI(ExprMulti pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitWRAP(ExprWrap pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitCONCAT(ExprConcat pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitARRINIT(ExprArrayInit pExpr)
        {
            return VisitEXPR(pExpr);
        }

        protected virtual Expr VisitFIELDINFO(ExprFieldInfo pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitMETHODINFO(ExprMethodInfo pExpr)
        {
            return VisitEXPR(pExpr);
        }
        protected virtual Expr VisitEQUALS(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitCOMPARE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitNE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitLE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitGE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitADD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSUB(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitDIV(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitBITAND(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitBITOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitLSHIFT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitLOGAND(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSEQUENCE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSAVE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitINDIR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSTRINGEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitDELEGATEEQ(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitDELEGATEADD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }

        protected virtual Expr VisitLT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitMUL(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitBITXOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitRSHIFT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitLOGOR(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSTRINGNE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitDELEGATENE(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitGT(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitMOD(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitSWAP(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitDELEGATESUB(ExprBinOp pExpr)
        {
            return VisitBINOP(pExpr);
        }
        protected virtual Expr VisitTRUE(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitINC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitLOGNOT(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitNEG(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitBITNOT(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitADDR(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitDECIMALNEG(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitDECIMALDEC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitFALSE(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitDEC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitUPLUS(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
        protected virtual Expr VisitDECIMALINC(ExprUnaryOp pExpr)
        {
            return VisitUNARYOP(pExpr);
        }
    }
}
