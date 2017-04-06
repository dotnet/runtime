// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class ExprFactory
    {
        private readonly GlobalSymbolContext _globalSymbolContext;

        public ExprFactory(GlobalSymbolContext globalSymbolContext)
        {
            Debug.Assert(globalSymbolContext != null);
            _globalSymbolContext = globalSymbolContext;
        }
        private TypeManager GetTypes()
        {
            return _globalSymbolContext.GetTypes();
        }
        private BSYMMGR GetGlobalSymbols()
        {
            return _globalSymbolContext.GetGlobalSymbols();
        }

        public ExprCall CreateCall(EXPRFLAG nFlags, CType pType, Expr pOptionalArguments, ExprMemberGroup pMemberGroup, MethWithInst MWI)
        {
            Debug.Assert(0 == (nFlags &
               ~(
                   EXPRFLAG.EXF_NEWOBJCALL | EXPRFLAG.EXF_CONSTRAINED | EXPRFLAG.EXF_BASECALL |
                   EXPRFLAG.EXF_NEWSTRUCTASSG |
                   EXPRFLAG.EXF_IMPLICITSTRUCTASSG | EXPRFLAG.EXF_MASK_ANY
               )
              ));

            ExprCall rval = new ExprCall(pType);
            rval.Flags = nFlags;
            rval.OptionalArguments = pOptionalArguments;
            rval.MemberGroup = pMemberGroup;
            rval.NullableCallLiftKind = NullableCallLiftKind.NotLifted;

            rval.MethWithInst = MWI;
            return rval;
        }

        public ExprField CreateField(EXPRFLAG nFlags, CType pType, Expr pOptionalObject, FieldWithType FWT)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MEMBERSET | EXPRFLAG.EXF_MASK_ANY)));
            ExprField rval = new ExprField(pType);
            rval.Flags = nFlags;
            rval.OptionalObject = pOptionalObject;
            rval.FieldWithType = FWT;
            return rval;
        }

        public ExprFuncPtr CreateFunctionPointer(EXPRFLAG nFlags, CType pType, Expr pObject, MethWithInst MWI)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_BASECALL)));
            ExprFuncPtr rval = new ExprFuncPtr(pType);
            rval.Flags = nFlags;
            rval.OptionalObject = pObject;
            rval.MethWithInst = new MethWithInst(MWI);
            return rval;
        }

        public ExprArrayInit CreateArrayInit(EXPRFLAG nFlags, CType pType, Expr pOptionalArguments, Expr pOptionalArgumentDimensions, int[] pDimSizes)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_MASK_ANY | EXPRFLAG.EXF_ARRAYCONST | EXPRFLAG.EXF_ARRAYALLCONST)));
            ExprArrayInit rval = new ExprArrayInit(pType);
            rval.OptionalArguments = pOptionalArguments;
            rval.OptionalArgumentDimensions = pOptionalArgumentDimensions;
            rval.DimensionSizes = pDimSizes;
            rval.DimensionSize = pDimSizes?.Length ?? 0;
            return rval;
        }

        public ExprProperty CreateProperty(CType pType, Expr pOptionalObject)
        {
            MethPropWithInst mwi = new MethPropWithInst();
            ExprMemberGroup pMemGroup = CreateMemGroup(pOptionalObject, mwi);
            return CreateProperty(pType, null, null, pMemGroup, null, null, null);
        }

        public ExprProperty CreateProperty(CType pType, Expr pOptionalObjectThrough, Expr pOptionalArguments, ExprMemberGroup pMemberGroup, PropWithType pwtSlot, MethWithType mwtGet, MethWithType mwtSet)
        {
            ExprProperty rval = new ExprProperty(pType);
            rval.OptionalObjectThrough = pOptionalObjectThrough;
            rval.OptionalArguments = pOptionalArguments;
            rval.MemberGroup = pMemberGroup;

            if (pwtSlot != null)
            {
                rval.PropWithTypeSlot = pwtSlot;
            }
            if (mwtSet != null)
            {
                rval.MethWithTypeSet = mwtSet;
            }
            return rval;
        }

        public ExprEvent CreateEvent(CType pType, Expr pOptionalObject, EventWithType EWT)
        {
            ExprEvent rval = new ExprEvent(pType);
            rval.OptionalObject = pOptionalObject;
            rval.EventWithType = EWT;
            return rval;
        }

        public ExprMemberGroup CreateMemGroup(EXPRFLAG nFlags, Name pName, TypeArray pTypeArgs, SYMKIND symKind, CType pTypePar, MethodOrPropertySymbol pMPS, Expr pObject, CMemberLookupResults memberLookupResults)
        {
            Debug.Assert(0 == (nFlags & ~(
                           EXPRFLAG.EXF_CTOR | EXPRFLAG.EXF_INDEXER | EXPRFLAG.EXF_OPERATOR | EXPRFLAG.EXF_NEWOBJCALL |
                           EXPRFLAG.EXF_BASECALL | EXPRFLAG.EXF_DELEGATE | EXPRFLAG.EXF_USERCALLABLE | EXPRFLAG.EXF_MASK_ANY
                       )
                      ));
            ExprMemberGroup rval = new ExprMemberGroup(GetTypes().GetMethGrpType());
            rval.Flags = nFlags;
            rval.Name = pName;
            rval.TypeArgs = pTypeArgs ?? BSYMMGR.EmptyTypeArray();
            rval.SymKind = symKind;
            rval.ParentType = pTypePar;
            rval.OptionalObject = pObject;
            rval.MemberLookupResults = memberLookupResults;
            return rval;
        }

        public ExprMemberGroup CreateMemGroup(
                Expr pObject,
                MethPropWithInst mwi)
        {
            Name pName = mwi.Sym?.name;
            MethodOrPropertySymbol methProp = mwi.MethProp();

            CType pType = mwi.GetType() ?? (CType)GetTypes().GetErrorSym();

            return CreateMemGroup(0, pName, mwi.TypeArgs, methProp?.getKind() ?? SYMKIND.SK_MethodSymbol, mwi.GetType(), methProp, pObject, new CMemberLookupResults(GetGlobalSymbols().AllocParams(1, new CType[] { pType }), pName));
        }

        public ExprUserDefinedConversion CreateUserDefinedConversion(Expr arg, Expr call, MethWithInst mwi)
        {
            Debug.Assert(arg != null);
            Debug.Assert(call != null);
            ExprUserDefinedConversion rval = new ExprUserDefinedConversion();
            rval.Argument = arg;
            rval.UserDefinedCall = call;
            rval.UserDefinedCallMethod = mwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            return rval;
        }

        public ExprCast CreateCast(EXPRFLAG nFlags, CType pType, Expr pArg)
        {
            return CreateCast(nFlags, CreateClass(pType, null), pArg);
        }

        public ExprCast CreateCast(EXPRFLAG nFlags, ExprClass pType, Expr pArg)
        {
            Debug.Assert(pArg != null);
            Debug.Assert(pType != null);
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_CAST_ALL | EXPRFLAG.EXF_MASK_ANY)));
            ExprCast rval = new ExprCast();
            rval.Argument = pArg;
            rval.Flags = nFlags;
            rval.DestinationType = pType;
            return rval;
        }

        public ExprReturn CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, Expr pOptionalObject)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_ASLEAVE | EXPRFLAG.EXF_FINALLYBLOCKED | EXPRFLAG.EXF_RETURNISYIELD |
                          EXPRFLAG.EXF_ASFINALLYLEAVE | EXPRFLAG.EXF_GENERATEDSTMT | EXPRFLAG.EXF_MARKING |
                          EXPRFLAG.EXF_MASK_ANY
                        )
                      ));
            ExprReturn rval = new ExprReturn();
            rval.Flags = nFlags;
            rval.OptionalObject = pOptionalObject;
            return rval;
        }

        public ExprLocal CreateLocal(EXPRFLAG nFlags, LocalVariableSymbol pLocal)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));
            ExprLocal rval = new ExprLocal();
            rval.Flags = nFlags;
            rval.Local = pLocal;
            return rval;
        }

        public ExprThisPointer CreateThis(LocalVariableSymbol pLocal, bool fImplicit)
        {
            Debug.Assert(pLocal == null || pLocal.isThis);

            EXPRFLAG flags = EXPRFLAG.EXF_CANTBENULL;
            if (fImplicit)
            {
                flags |= EXPRFLAG.EXF_IMPLICITTHIS;
            }
            if (pLocal != null && pLocal.GetType().isStructType())
            {
                flags |= EXPRFLAG.EXF_LVALUE;
            }

            ExprThisPointer rval = new ExprThisPointer();
            rval.Flags = flags;
            rval.Local = pLocal;
            return (rval);
        }

        public ExprBoundLambda CreateAnonymousMethod(AggregateType delegateType)
        {
            Debug.Assert(delegateType == null || delegateType.isDelegateType());
            ExprBoundLambda rval = new ExprBoundLambda(delegateType);
            return rval;
        }

        public ExprUnboundLambda CreateLambda()
        {
            ExprUnboundLambda rval = new ExprUnboundLambda(GetTypes().GetAnonMethType());
            return rval;
        }

        public ExprHoistedLocalExpr CreateHoistedLocalInExpression(ExprLocal localToHoist)
        {
            Debug.Assert(localToHoist != null);
            return new ExprHoistedLocalExpr(GetTypes().GetOptPredefAgg(PredefinedType.PT_EXPRESSION).getThisType());
        }

        public ExprMethodInfo CreateMethodInfo(MethPropWithInst mwi)
        {
            return CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);
        }

        public ExprMethodInfo CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters)
        {
            Debug.Assert(method != null);
            Debug.Assert(methodType != null);
            ExprMethodInfo methodInfo = new ExprMethodInfo(
                GetTypes().GetOptPredefAgg(method.IsConstructor() ? PredefinedType.PT_CONSTRUCTORINFO : PredefinedType.PT_METHODINFO).getThisType());
            methodInfo.Method = new MethWithInst(method, methodType, methodParameters);
            return methodInfo;
        }

        public ExprPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType)
        {
            Debug.Assert(prop != null);
            Debug.Assert(propertyType != null);
            ExprPropertyInfo propInfo = new ExprPropertyInfo(GetTypes().GetOptPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType());
            propInfo.Property = new PropWithType(prop, propertyType);
            return propInfo;
        }

        public ExprFieldInfo CreateFieldInfo(FieldSymbol field, AggregateType fieldType)
        {
            Debug.Assert(field != null);
            Debug.Assert(fieldType != null);
            return new ExprFieldInfo(field, fieldType, GetTypes().GetOptPredefAgg(PredefinedType.PT_FIELDINFO).getThisType());
        }

        private ExprTypeOf CreateTypeOf(ExprClass pSourceType)
        {
            ExprTypeOf rval = new ExprTypeOf(GetTypes().GetReqPredefAgg(PredefinedType.PT_TYPE).getThisType());
            rval.Flags = EXPRFLAG.EXF_CANTBENULL;
            rval.SourceType = pSourceType;
            return rval;
        }
        public ExprTypeOf CreateTypeOf(CType pSourceType)
        {
            return CreateTypeOf(MakeClass(pSourceType));
        }

        public ExprUserLogicalOp CreateUserLogOp(CType pType, Expr pCallTF, ExprCall pCallOp)
        {
            Debug.Assert(pCallTF != null);
            Debug.Assert((pCallOp?.OptionalArguments as ExprList)?.OptionalElement != null);
            ExprUserLogicalOp rval = new ExprUserLogicalOp(pType);
            Expr leftChild = ((ExprList)pCallOp.OptionalArguments).OptionalElement;
            Debug.Assert(leftChild != null);
            if (leftChild is ExprWrap wrap)
            {
                // In the EE case, we don't create WRAPEXPRs.
                leftChild = wrap.OptionalExpression;
                Debug.Assert(leftChild != null);
            }
            rval.Flags = EXPRFLAG.EXF_ASSGOP;
            rval.TrueFalseCall = pCallTF;
            rval.OperatorCall = pCallOp;
            rval.FirstOperandToExamine = leftChild;
            return rval;
        }

        public ExprUserLogicalOp CreateUserLogOpError(CType pType, Expr pCallTF, ExprCall pCallOp)
        {
            ExprUserLogicalOp rval = CreateUserLogOp(pType, pCallTF, pCallOp);
            rval.SetError();
            return rval;
        }

        public ExprConcat CreateConcat(Expr op1, Expr op2)
        {
            Debug.Assert(op1?.Type != null);
            Debug.Assert(op2?.Type != null);
            Debug.Assert(op1.Type.isPredefType(PredefinedType.PT_STRING) || op2.Type.isPredefType(PredefinedType.PT_STRING));

            CType type = op1.Type;
            if (!type.isPredefType(PredefinedType.PT_STRING))
            {
                type = op2.Type;
            }

            Debug.Assert(type.isPredefType(PredefinedType.PT_STRING));

            ExprConcat rval = new ExprConcat(type);
            rval.FirstArgument = op1;
            rval.SecondArgument = op2;
            return rval;
        }

        public ExprConstant CreateStringConstant(string str)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_STRING).getThisType(), ConstVal.Get(str));
        }

        public ExprMultiGet CreateMultiGet(EXPRFLAG nFlags, CType pType, ExprMulti pOptionalMulti)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));
            ExprMultiGet rval = new ExprMultiGet(pType);
            rval.Flags = nFlags;
            rval.OptionalMulti = pOptionalMulti;
            return rval;
        }

        public ExprMulti CreateMulti(EXPRFLAG nFlags, CType pType, Expr pLeft, Expr pOp)
        {
            Debug.Assert(pLeft != null);
            Debug.Assert(pOp != null);
            ExprMulti rval = new ExprMulti(pType);
            rval.Flags = nFlags;
            rval.Left = pLeft;
            rval.Operator = pOp;
            return rval;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Precondition:
        //
        // pType - Non-null
        //
        // This returns a null for reference types and an EXPRZEROINIT for all others.

        public Expr CreateZeroInit(CType pType)
        {
            ExprClass exprClass = MakeClass(pType);
            return CreateZeroInit(exprClass);
        }

        private Expr CreateZeroInit(ExprClass pTypeExpr)
        {
            return CreateZeroInit(pTypeExpr, null, false);
        }

        private Expr CreateZeroInit(ExprClass pTypeExpr, Expr pOptionalOriginalConstructorCall, bool isConstructor)
        {
            Debug.Assert(pTypeExpr != null);
            CType pType = pTypeExpr.Type;
            bool bIsError = false;

            if (pType.isEnumType())
            {
                // For enum types, we create a constant that has the default value
                // as an object pointer.
                ExprConstant expr = CreateConstant(pType, ConstVal.Get(Activator.CreateInstance(pType.AssociatedSystemType)));
                return expr;
            }

            switch (pType.fundType())
            {
                default:
                    bIsError = true;
                    break;

                case FUNDTYPE.FT_PTR:
                    {
                        CType nullType = GetTypes().GetNullType();

                        // It looks like this if is always false ...
                        if (nullType.fundType() == pType.fundType())
                        {
                            // Create a constant here.

                            ExprConstant expr = CreateConstant(pType, ConstVal.GetDefaultValue(ConstValKind.IntPtr));
                            return (expr);
                        }

                        // Just allocate a new node and fill it in.

                        ExprCast cast = CreateCast(0, pTypeExpr, CreateNull());
                        return (cast);
                    }

                case FUNDTYPE.FT_REF:
                case FUNDTYPE.FT_I1:
                case FUNDTYPE.FT_U1:
                case FUNDTYPE.FT_I2:
                case FUNDTYPE.FT_U2:
                case FUNDTYPE.FT_I4:
                case FUNDTYPE.FT_U4:
                case FUNDTYPE.FT_I8:
                case FUNDTYPE.FT_U8:
                case FUNDTYPE.FT_R4:
                case FUNDTYPE.FT_R8:
                    {
                        ExprConstant expr = CreateConstant(pType, ConstVal.GetDefaultValue(pType.constValKind()));
                        ExprConstant exprInOriginal = CreateConstant(pType, ConstVal.GetDefaultValue(pType.constValKind()));
                        exprInOriginal.OptionalConstructorCall = pOptionalOriginalConstructorCall;
                        return expr;
                    }
                case FUNDTYPE.FT_STRUCT:
                    if (pType.isPredefType(PredefinedType.PT_DECIMAL))
                    {
                        ExprConstant expr = CreateConstant(pType, ConstVal.GetDefaultValue(pType.constValKind()));
                        ExprConstant exprOriginal = CreateConstant(pType, ConstVal.GetDefaultValue(pType.constValKind()));
                        exprOriginal.OptionalConstructorCall = pOptionalOriginalConstructorCall;
                        return expr;
                    }
                    break;

                case FUNDTYPE.FT_VAR:
                    break;
            }

            ExprZeroInit rval = new ExprZeroInit(pType);
            rval.OptionalConstructorCall = pOptionalOriginalConstructorCall;
            rval.IsConstructor = isConstructor;

            if (bIsError)
            {
                rval.SetError();
            }

            return rval;
        }

        public ExprConstant CreateConstant(CType pType, ConstVal constVal)
        {
            ExprConstant rval = CreateConstant(pType);
            rval.Val = constVal;
            return rval;
        }

        private ExprConstant CreateConstant(CType pType)
        {
            return new ExprConstant(pType);
        }

        public ExprConstant CreateIntegerConstant(int x)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType(), ConstVal.Get(x));
        }
        public ExprConstant CreateBoolConstant(bool b)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstVal.Get(b));
        }

        public ExprBlock CreateBlock(ExprStatement pOptionalStatements, Scope pOptionalScope)
        {
            ExprBlock rval = new ExprBlock();
            rval.OptionalStatements = pOptionalStatements;
            rval.OptionalScopeSymbol = pOptionalScope;
            return rval;
        }

        public ExprQuestionMark CreateQuestionMark(Expr pTestExpression, ExprBinOp pConsequence)
        {
            Debug.Assert(pTestExpression != null);
            Debug.Assert(pConsequence != null);

            ExprQuestionMark pResult = new ExprQuestionMark();
            pResult.TestExpression = pTestExpression;
            pResult.Consequence = pConsequence;
            return pResult;
        }

        public ExprArrayIndex CreateArrayIndex(Expr pArray, Expr pIndex)
        {
            CType pType = pArray.Type;

            if (pType != null && pType.IsArrayType())
            {
                pType = pType.AsArrayType().GetElementType();
            }
            else if (pType == null)
            {
                pType = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
            }
            ExprArrayIndex pResult = new ExprArrayIndex(pType);
            pResult.Array = pArray;
            pResult.Index = pIndex;
            return pResult;
        }

        public ExprArrayLength CreateArrayLength(Expr pArray)
        {
            ExprArrayLength pResult = new ExprArrayLength(GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType());
            pResult.Array = pArray;
            return pResult;
        }

        public ExprBinOp CreateBinop(ExpressionKind exprKind, CType pType, Expr p1, Expr p2)
        {
            //Debug.Assert(exprKind.isBinaryOperator());
            ExprBinOp rval = new ExprBinOp(exprKind, pType);
            rval.Flags = EXPRFLAG.EXF_BINOP;
            rval.OptionalLeftChild = p1;
            rval.OptionalRightChild = p2;
            return rval;
        }

        public ExprUnaryOp CreateUnaryOp(ExpressionKind exprKind, CType pType, Expr pOperand)
        {
            Debug.Assert(exprKind.IsUnaryOperator());
            Debug.Assert(pOperand != null);
            ExprUnaryOp rval = new ExprUnaryOp(exprKind, pType);
            rval.Child = pOperand;
            return rval;
        }

        public ExprOperator CreateOperator(ExpressionKind exprKind, CType pType, Expr pArg1, Expr pOptionalArg2)
        {
            Debug.Assert(pArg1 != null);
            Debug.Assert(exprKind.IsUnaryOperator() ? pOptionalArg2 == null : pOptionalArg2 != null);
            return exprKind.IsUnaryOperator()
                ? (ExprOperator)CreateUnaryOp(exprKind, pType, pArg1)
                : CreateBinop(exprKind, pType, pArg1, pOptionalArg2);
        }


        public ExprBinOp CreateUserDefinedBinop(ExpressionKind exprKind, CType pType, Expr p1, Expr p2, Expr call, MethPropWithInst pmpwi)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            Debug.Assert(call != null);
            ExprBinOp rval = new ExprBinOp(exprKind, pType);
            rval.Flags = EXPRFLAG.EXF_BINOP;
            rval.OptionalLeftChild = p1;
            rval.OptionalRightChild = p2;
            rval.OptionalUserDefinedCall = call;
            rval.UserDefinedCallMethod = pmpwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            return rval;
        }

        public ExprUnaryOp CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType pType, Expr pOperand, ExprCall call, MethPropWithInst pmpwi)
        {
            Debug.Assert(pType != null);
            Debug.Assert(pOperand != null);
            Debug.Assert(call != null);
            Debug.Assert(pmpwi != null);
            ExprUnaryOp rval = new ExprUnaryOp(exprKind, pType);
            rval.Child = pOperand;
            // The call may be lifted, but we do not mark the outer binop as lifted.
            rval.OptionalUserDefinedCall = call;
            rval.UserDefinedCallMethod = pmpwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            return rval;
        }

        public ExprUnaryOp CreateNeg(EXPRFLAG nFlags, Expr pOperand)
        {
            Debug.Assert(pOperand != null);
            ExprUnaryOp pUnaryOp = CreateUnaryOp(ExpressionKind.Negate, pOperand.Type, pOperand);
            pUnaryOp.Flags |= nFlags;
            return pUnaryOp;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the second.

        public ExprBinOp CreateSequence(Expr p1, Expr p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.Sequence, p2.Type, p1, p2);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the first.

        public ExprBinOp CreateReverseSequence(Expr p1, Expr p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.SequenceReverse, p1.Type, p1, p2);
        }

        public ExprAssignment CreateAssignment(Expr pLHS, Expr pRHS)
        {
            ExprAssignment pAssignment = new ExprAssignment();
            pAssignment.Flags = EXPRFLAG.EXF_ASSGOP;
            pAssignment.LHS = pLHS;
            pAssignment.RHS = pRHS;
            return pAssignment;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public ExprNamedArgumentSpecification CreateNamedArgumentSpecification(Name pName, Expr pValue)
        {
            ExprNamedArgumentSpecification pResult = new ExprNamedArgumentSpecification();
            pResult.Value = pValue;
            pResult.Name = pName;

            return pResult;
        }


        public ExprWrap CreateWrap(
            Scope pCurrentScope,
            Expr pOptionalExpression
        )
        {
            ExprWrap rval = new ExprWrap();
            rval.OptionalExpression = pOptionalExpression;
            rval.Flags = EXPRFLAG.EXF_LVALUE;

            return rval;
        }
        public ExprWrap CreateWrapNoAutoFree(Scope pCurrentScope, Expr pOptionalWrap)
        {
            ExprWrap rval = CreateWrap(pCurrentScope, pOptionalWrap);
            return rval;
        }
        public ExprBinOp CreateSave(ExprWrap wrap)
        {
            Debug.Assert(wrap != null);
            ExprBinOp expr = CreateBinop(ExpressionKind.Save, wrap.Type, wrap.OptionalExpression, wrap);
            expr.SetAssignment();
            return expr;
        }

        public ExprConstant CreateNull()
        {
            return CreateConstant(GetTypes().GetNullType(), default(ConstVal));
        }

        public void AppendItemToList(
            Expr newItem,
            ref Expr first,
            ref Expr last
        )
        {
            if (newItem == null)
            {
                // Nothing changes.
                return;
            }
            if (first == null)
            {
                Debug.Assert(last == first);
                first = newItem;
                last = newItem;
                return;
            }
            if (first.Kind != ExpressionKind.List)
            {
                Debug.Assert(last == first);
                first = CreateList(first, newItem);
                last = first;
                return;
            }
            Debug.Assert((last as ExprList)?.OptionalNextListNode != null);
            Debug.Assert((last as ExprList).OptionalNextListNode.Kind != ExpressionKind.List);
            ExprList list = (ExprList)last;
            list.OptionalNextListNode = CreateList(list.OptionalNextListNode, newItem);
            last = list.OptionalNextListNode;
        }

        public ExprList CreateList(Expr op1, Expr op2)
        {
            ExprList rval = new ExprList();
            rval.OptionalElement = op1;
            rval.OptionalNextListNode = op2;
            return rval;
        }
        public ExprList CreateList(Expr op1, Expr op2, Expr op3)
        {
            return CreateList(op1, CreateList(op2, op3));
        }
        public ExprList CreateList(Expr op1, Expr op2, Expr op3, Expr op4)
        {
            return CreateList(op1, CreateList(op2, CreateList(op3, op4)));
        }
        public ExprTypeArguments CreateTypeArguments(TypeArray pTypeArray, Expr pOptionalElements)
        {
            Debug.Assert(pTypeArray != null);
            ExprTypeArguments rval = new ExprTypeArguments();
            rval.OptionalElements = pOptionalElements;
            return rval;
        }

        public ExprClass CreateClass(CType pType, ExprTypeArguments pOptionalTypeArguments)
        {
            Debug.Assert(pType != null);
            ExprClass rval = new ExprClass(pType);
            return rval;
        }

        public ExprClass MakeClass(CType pType)
        {
            Debug.Assert(pType != null);
            return CreateClass(pType, null/* type arguments */);
        }
    }
}

