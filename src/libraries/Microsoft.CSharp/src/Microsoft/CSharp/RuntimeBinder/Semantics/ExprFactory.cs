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

            ExprCall rval = new ExprCall();
            rval.Kind = ExpressionKind.EK_CALL;
            rval.Type = pType;
            rval.Flags = nFlags;
            rval.OptionalArguments = pOptionalArguments;
            rval.MemberGroup = pMemberGroup;
            rval.NullableCallLiftKind = NullableCallLiftKind.NotLifted;
            rval.CastOfNonLiftedResultToLiftedType = null;

            rval.MethWithInst = MWI;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprField CreateField(EXPRFLAG nFlags, CType pType, Expr pOptionalObject, uint nOffset, FieldWithType FWT, Expr pOptionalLHS)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MEMBERSET | EXPRFLAG.EXF_MASK_ANY)));
            ExprField rval = new ExprField();
            rval.Kind = ExpressionKind.EK_FIELD;
            rval.Type = pType;
            rval.Flags = nFlags;
            rval.OptionalObject = pOptionalObject;
            if (FWT != null)
            {
                rval.FieldWithType = FWT;
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprFuncPtr CreateFunctionPointer(EXPRFLAG nFlags, CType pType, Expr pObject, MethWithInst MWI)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_BASECALL)));
            ExprFuncPtr rval = new ExprFuncPtr();
            rval.Kind = ExpressionKind.EK_FUNCPTR;
            rval.Type = pType;
            rval.Flags = nFlags;
            rval.OptionalObject = pObject;
            rval.MethWithInst = new MethWithInst(MWI);
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprArrayInit CreateArrayInit(EXPRFLAG nFlags, CType pType, Expr pOptionalArguments, Expr pOptionalArgumentDimensions, int[] pDimSizes)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_MASK_ANY | EXPRFLAG.EXF_ARRAYCONST | EXPRFLAG.EXF_ARRAYALLCONST)));
            ExprArrayInit rval = new ExprArrayInit();
            rval.Kind = ExpressionKind.EK_ARRINIT;
            rval.Type = pType;
            rval.OptionalArguments = pOptionalArguments;
            rval.OptionalArgumentDimensions = pOptionalArgumentDimensions;
            rval.DimensionSizes = pDimSizes;
            rval.DimensionSize = pDimSizes?.Length ?? 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprProperty CreateProperty(CType pType, Expr pOptionalObject)
        {
            MethPropWithInst mwi = new MethPropWithInst();
            ExprMemberGroup pMemGroup = CreateMemGroup(pOptionalObject, mwi);
            return CreateProperty(pType, null, null, pMemGroup, null, null, null);
        }

        public ExprProperty CreateProperty(CType pType, Expr pOptionalObjectThrough, Expr pOptionalArguments, ExprMemberGroup pMemberGroup, PropWithType pwtSlot, MethWithType mwtGet, MethWithType mwtSet)
        {
            ExprProperty rval = new ExprProperty();
            rval.Kind = ExpressionKind.EK_PROP;
            rval.Type = pType;
            rval.Flags = 0;
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
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprEvent CreateEvent(CType pType, Expr pOptionalObject, EventWithType EWT)
        {
            ExprEvent rval = new ExprEvent();
            rval.Kind = ExpressionKind.EK_EVENT;
            rval.Type = pType;
            rval.Flags = 0;
            rval.OptionalObject = pOptionalObject;
            if (EWT != null)
            {
                rval.EventWithType = EWT;
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprMemberGroup CreateMemGroup(EXPRFLAG nFlags, Name pName, TypeArray pTypeArgs, SYMKIND symKind, CType pTypePar, MethodOrPropertySymbol pMPS, Expr pObject, CMemberLookupResults memberLookupResults)
        {
            Debug.Assert(0 == (nFlags & ~(
                           EXPRFLAG.EXF_CTOR | EXPRFLAG.EXF_INDEXER | EXPRFLAG.EXF_OPERATOR | EXPRFLAG.EXF_NEWOBJCALL |
                           EXPRFLAG.EXF_BASECALL | EXPRFLAG.EXF_DELEGATE | EXPRFLAG.EXF_USERCALLABLE | EXPRFLAG.EXF_MASK_ANY
                       )
                      ));
            ExprMemberGroup rval = new ExprMemberGroup();
            rval.Kind = ExpressionKind.EK_MEMGRP;
            rval.Type = GetTypes().GetMethGrpType();
            rval.Flags = nFlags;
            rval.name = pName;
            rval.typeArgs = pTypeArgs;
            rval.sk = symKind;
            rval.ParentType = pTypePar;
            rval.OptionalObject = pObject;
            rval.MemberLookupResults = memberLookupResults;
            rval.OptionalLHS = null;
            if (rval.typeArgs == null)
            {
                rval.typeArgs = BSYMMGR.EmptyTypeArray();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprMemberGroup CreateMemGroup(
                Expr pObject,
                MethPropWithInst mwi)
        {
            Name pName = mwi.Sym?.name;
            MethodOrPropertySymbol methProp = mwi.MethProp();

            CType pType = mwi.GetType();
            if (pType == null)
            {
                pType = GetTypes().GetErrorSym();
            }

            return CreateMemGroup(0, pName, mwi.TypeArgs, methProp?.getKind() ?? SYMKIND.SK_MethodSymbol, mwi.GetType(), methProp, pObject, new CMemberLookupResults(GetGlobalSymbols().AllocParams(1, new CType[] { pType }), pName));
        }

        public ExprUserDefinedConversion CreateUserDefinedConversion(Expr arg, Expr call, MethWithInst mwi)
        {
            Debug.Assert(arg != null);
            Debug.Assert(call != null);
            ExprUserDefinedConversion rval = new ExprUserDefinedConversion();
            rval.Kind = ExpressionKind.EK_USERDEFINEDCONVERSION;
            rval.Type = call.Type;
            rval.Flags = 0;
            rval.Argument = arg;
            rval.UserDefinedCall = call;
            rval.UserDefinedCallMethod = mwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return rval;
        }

        public ExprCast CreateCast(EXPRFLAG nFlags, CType pType, Expr pArg)
        {
            return CreateCast(nFlags, CreateClass(pType, null, null), pArg);
        }

        public ExprCast CreateCast(EXPRFLAG nFlags, ExprTypeOrNamespace pType, Expr pArg)
        {
            Debug.Assert(pArg != null);
            Debug.Assert(pType != null);
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_CAST_ALL | EXPRFLAG.EXF_MASK_ANY)));
            ExprCast rval = new ExprCast();
            rval.Type = pType.TypeOrNamespace as CType;
            rval.Kind = ExpressionKind.EK_CAST;
            rval.Argument = pArg;
            rval.Flags = nFlags;
            rval.DestinationType = pType;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprReturn CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, Expr pOptionalObject)
        {
            return CreateReturn(nFlags, pCurrentScope, pOptionalObject, pOptionalObject);
        }

        private ExprReturn CreateReturn(EXPRFLAG nFlags, Scope pCurrentScope, Expr pOptionalObject, Expr pOptionalOriginalObject)
        {
            Debug.Assert(0 == (nFlags &
                       ~(EXPRFLAG.EXF_ASLEAVE | EXPRFLAG.EXF_FINALLYBLOCKED | EXPRFLAG.EXF_RETURNISYIELD |
                          EXPRFLAG.EXF_ASFINALLYLEAVE | EXPRFLAG.EXF_GENERATEDSTMT | EXPRFLAG.EXF_MARKING |
                          EXPRFLAG.EXF_MASK_ANY
                        )
                      ));
            ExprReturn rval = new ExprReturn();
            rval.Kind = ExpressionKind.EK_RETURN;
            rval.Type = null;
            rval.Flags = nFlags;
            rval.OptionalObject = pOptionalObject;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprLocal CreateLocal(EXPRFLAG nFlags, LocalVariableSymbol pLocal)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));

            CType type = null;
            if (pLocal != null)
            {
                type = pLocal.GetType();
            }

            ExprLocal rval = new ExprLocal();
            rval.Kind = ExpressionKind.EK_LOCAL;
            rval.Type = type;
            rval.Flags = nFlags;
            rval.Local = pLocal;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprThisPointer CreateThis(LocalVariableSymbol pLocal, bool fImplicit)
        {
            Debug.Assert(pLocal == null || pLocal.isThis);

            CType type = null;
            if (pLocal != null)
            {
                type = pLocal.GetType();
            }

            EXPRFLAG flags = EXPRFLAG.EXF_CANTBENULL;
            if (fImplicit)
            {
                flags |= EXPRFLAG.EXF_IMPLICITTHIS;
            }
            if (type != null && type.isStructType())
            {
                flags |= EXPRFLAG.EXF_LVALUE;
            }

            ExprThisPointer rval = new ExprThisPointer();
            rval.Kind = ExpressionKind.EK_THISPOINTER;
            rval.Type = type;
            rval.Flags = flags;
            rval.Local = pLocal;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprBoundLambda CreateAnonymousMethod(AggregateType delegateType)
        {
            Debug.Assert(delegateType == null || delegateType.isDelegateType());
            ExprBoundLambda rval = new ExprBoundLambda();
            rval.Kind = ExpressionKind.EK_BOUNDLAMBDA;
            rval.Type = delegateType;
            rval.Flags = 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprUnboundLambda CreateLambda()
        {
            CType type = GetTypes().GetAnonMethType();


            ExprUnboundLambda rval = new ExprUnboundLambda();
            rval.Kind = ExpressionKind.EK_UNBOUNDLAMBDA;
            rval.Type = type;
            rval.Flags = 0;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprHoistedLocalExpr CreateHoistedLocalInExpression(ExprLocal localToHoist)
        {
            Debug.Assert(localToHoist != null);
            ExprHoistedLocalExpr rval = new ExprHoistedLocalExpr();
            rval.Kind = ExpressionKind.EK_HOISTEDLOCALEXPR;
            rval.Type = GetTypes().GetOptPredefAgg(PredefinedType.PT_EXPRESSION).getThisType();
            rval.Flags = 0;
            return rval;
        }

        public ExprMethodInfo CreateMethodInfo(MethPropWithInst mwi)
        {
            return CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);
        }

        public ExprMethodInfo CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters)
        {
            Debug.Assert(method != null);
            Debug.Assert(methodType != null);
            ExprMethodInfo methodInfo = new ExprMethodInfo();
            CType type;
            if (method.IsConstructor())
            {
                type = GetTypes().GetOptPredefAgg(PredefinedType.PT_CONSTRUCTORINFO).getThisType();
            }
            else
            {
                type = GetTypes().GetOptPredefAgg(PredefinedType.PT_METHODINFO).getThisType();
            }

            methodInfo.Kind = ExpressionKind.EK_METHODINFO;
            methodInfo.Type = type;
            methodInfo.Flags = 0;
            methodInfo.Method = new MethWithInst(method, methodType, methodParameters);
            return methodInfo;
        }

        public ExprPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType)
        {
            Debug.Assert(prop != null);
            Debug.Assert(propertyType != null);
            ExprPropertyInfo propInfo = new ExprPropertyInfo();

            propInfo.Kind = ExpressionKind.EK_PROPERTYINFO;
            propInfo.Type = GetTypes().GetOptPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType();
            propInfo.Flags = 0;
            propInfo.Property = new PropWithType(prop, propertyType);

            return propInfo;
        }

        public ExprFieldInfo CreateFieldInfo(FieldSymbol field, AggregateType fieldType)
        {
            Debug.Assert(field != null);
            Debug.Assert(fieldType != null);
            ExprFieldInfo rval = new ExprFieldInfo(field, fieldType);
            rval.Kind = ExpressionKind.EK_FIELDINFO;
            rval.Type = GetTypes().GetOptPredefAgg(PredefinedType.PT_FIELDINFO).getThisType();
            rval.Flags = 0;
            return rval;
        }

        private ExprTypeOf CreateTypeOf(ExprTypeOrNamespace pSourceType)
        {
            ExprTypeOf rval = new ExprTypeOf();
            rval.Kind = ExpressionKind.EK_TYPEOF;
            rval.Type = GetTypes().GetReqPredefAgg(PredefinedType.PT_TYPE).getThisType();
            rval.Flags = EXPRFLAG.EXF_CANTBENULL;
            rval.SourceType = pSourceType;
            Debug.Assert(rval != null);
            return (rval);
        }
        public ExprTypeOf CreateTypeOf(CType pSourceType)
        {
            return CreateTypeOf(MakeClass(pSourceType));
        }

        public ExprUserLogicalOp CreateUserLogOp(CType pType, Expr pCallTF, ExprCall pCallOp)
        {
            Debug.Assert(pCallTF != null);
            Debug.Assert(pCallOp != null);
            Debug.Assert(pCallOp.OptionalArguments != null);
            Debug.Assert(pCallOp.OptionalArguments.isLIST());
            Debug.Assert(pCallOp.OptionalArguments.asLIST().OptionalElement != null);
            ExprUserLogicalOp rval = new ExprUserLogicalOp();
            Expr leftChild = pCallOp.OptionalArguments.asLIST().OptionalElement;
            Debug.Assert(leftChild != null);
            if (leftChild.isWRAP())
            {
                // In the EE case, we don't create WRAPEXPRs.
                leftChild = leftChild.asWRAP().OptionalExpression;
                Debug.Assert(leftChild != null);
            }
            rval.Kind = ExpressionKind.EK_USERLOGOP;
            rval.Type = pType;
            rval.Flags = EXPRFLAG.EXF_ASSGOP;
            rval.TrueFalseCall = pCallTF;
            rval.OperatorCall = pCallOp;
            rval.FirstOperandToExamine = leftChild;
            Debug.Assert(rval != null);
            return (rval);
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

            ExprConcat rval = new ExprConcat();
            rval.Kind = ExpressionKind.EK_CONCAT;
            rval.Type = type;
            rval.Flags = 0;
            rval.FirstArgument = op1;
            rval.SecondArgument = op2;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprConstant CreateStringConstant(string str)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_STRING).getThisType(), ConstVal.Get(str));
        }

        public ExprMultiGet CreateMultiGet(EXPRFLAG nFlags, CType pType, ExprMulti pOptionalMulti)
        {
            Debug.Assert(0 == (nFlags & ~(EXPRFLAG.EXF_MASK_ANY)));
            ExprMultiGet rval = new ExprMultiGet();

            rval.Kind = ExpressionKind.EK_MULTIGET;
            rval.Type = pType;
            rval.Flags = nFlags;
            rval.OptionalMulti = pOptionalMulti;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprMulti CreateMulti(EXPRFLAG nFlags, CType pType, Expr pLeft, Expr pOp)
        {
            Debug.Assert(pLeft != null);
            Debug.Assert(pOp != null);
            ExprMulti rval = new ExprMulti();

            rval.Kind = ExpressionKind.EK_MULTI;
            rval.Type = pType;
            rval.Flags = nFlags;
            rval.Left = pLeft;
            rval.Operator = pOp;
            Debug.Assert(rval != null);
            return (rval);
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

        private Expr CreateZeroInit(ExprTypeOrNamespace pTypeExpr)
        {
            return CreateZeroInit(pTypeExpr, null, false);
        }

        private Expr CreateZeroInit(ExprTypeOrNamespace pTypeExpr, Expr pOptionalOriginalConstructorCall, bool isConstructor)
        {
            Debug.Assert(pTypeExpr != null);
            CType pType = pTypeExpr.TypeOrNamespace.AsType();
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

            ExprZeroInit rval = new ExprZeroInit();
            rval.Kind = ExpressionKind.EK_ZEROINIT;
            rval.Type = pType;
            rval.Flags = 0;
            rval.OptionalConstructorCall = pOptionalOriginalConstructorCall;
            rval.IsConstructor = isConstructor;

            if (bIsError)
            {
                rval.SetError();
            }

            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprConstant CreateConstant(CType pType, ConstVal constVal)
        {
            return CreateConstant(pType, constVal, null);
        }

        private ExprConstant CreateConstant(CType pType, ConstVal constVal, Expr pOriginal)
        {
            ExprConstant rval = CreateConstant(pType);
            rval.Val = constVal;
            Debug.Assert(rval != null);
            return (rval);
        }

        private ExprConstant CreateConstant(CType pType)
        {
            ExprConstant rval = new ExprConstant();
            rval.Kind = ExpressionKind.EK_CONSTANT;
            rval.Type = pType;
            rval.Flags = 0;
            return rval;
        }

        public ExprConstant CreateIntegerConstant(int x)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType(), ConstVal.Get(x));
        }
        public ExprConstant CreateBoolConstant(bool b)
        {
            return CreateConstant(GetTypes().GetReqPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstVal.Get(b));
        }
        public ExprBlock CreateBlock(ExprBlock pOptionalCurrentBlock, ExprStatement pOptionalStatements, Scope pOptionalScope)
        {
            ExprBlock rval = new ExprBlock();
            rval.Kind = ExpressionKind.EK_BLOCK;
            rval.Type = null;
            rval.Flags = 0;
            rval.OptionalStatements = pOptionalStatements;
            rval.OptionalScopeSymbol = pOptionalScope;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprQuestionMark CreateQuestionMark(Expr pTestExpression, ExprBinOp pConsequence)
        {
            Debug.Assert(pTestExpression != null);
            Debug.Assert(pConsequence != null);

            CType pType = pConsequence.Type;
            if (pType == null)
            {
                Debug.Assert(pConsequence.OptionalLeftChild != null);
                pType = pConsequence.OptionalLeftChild.Type;
                Debug.Assert(pType != null);
            }
            ExprQuestionMark pResult = new ExprQuestionMark();
            pResult.Kind = ExpressionKind.EK_QUESTIONMARK;
            pResult.Type = pType;
            pResult.Flags = 0;
            pResult.TestExpression = pTestExpression;
            pResult.Consequence = pConsequence;
            Debug.Assert(pResult != null);
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
            ExprArrayIndex pResult = new ExprArrayIndex();
            pResult.Kind = ExpressionKind.EK_ARRAYINDEX;
            pResult.Type = pType;
            pResult.Flags = 0;
            pResult.Array = pArray;
            pResult.Index = pIndex;
            return pResult;
        }

        public ExprArrayLength CreateArrayLength(Expr pArray)
        {
            ExprArrayLength pResult = new ExprArrayLength();
            pResult.Kind = ExpressionKind.EK_ARRAYLENGTH;
            pResult.Type = GetTypes().GetReqPredefAgg(PredefinedType.PT_INT).getThisType();
            pResult.Flags = 0;
            pResult.Array = pArray;
            return pResult;
        }

        public ExprBinOp CreateBinop(ExpressionKind exprKind, CType pType, Expr p1, Expr p2)
        {
            //Debug.Assert(exprKind.isBinaryOperator());
            ExprBinOp rval = new ExprBinOp();
            rval.Kind = exprKind;
            rval.Type = pType;
            rval.Flags = EXPRFLAG.EXF_BINOP;
            rval.OptionalLeftChild = p1;
            rval.OptionalRightChild = p2;
            rval.IsLifted = false;
            rval.OptionalUserDefinedCall = null;
            rval.UserDefinedCallMethod = null;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprUnaryOp CreateUnaryOp(ExpressionKind exprKind, CType pType, Expr pOperand)
        {
            Debug.Assert(exprKind.isUnaryOperator());
            Debug.Assert(pOperand != null);
            ExprUnaryOp rval = new ExprUnaryOp();
            rval.Kind = exprKind;
            rval.Type = pType;
            rval.Flags = 0;
            rval.Child = pOperand;
            rval.OptionalUserDefinedCall = null;
            rval.UserDefinedCallMethod = null;
            Debug.Assert(rval != null);
            return (rval);
        }

        public Expr CreateOperator(ExpressionKind exprKind, CType pType, Expr pArg1, Expr pOptionalArg2)
        {
            Debug.Assert(pArg1 != null);
            Expr rval = null;
            if (exprKind.isUnaryOperator())
            {
                Debug.Assert(pOptionalArg2 == null);
                rval = CreateUnaryOp(exprKind, pType, pArg1);
            }
            else
                rval = CreateBinop(exprKind, pType, pArg1, pOptionalArg2);
            Debug.Assert(rval != null);
            return rval;
        }


        public ExprBinOp CreateUserDefinedBinop(ExpressionKind exprKind, CType pType, Expr p1, Expr p2, Expr call, MethPropWithInst pmpwi)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            Debug.Assert(call != null);
            ExprBinOp rval = new ExprBinOp();
            rval.Kind = exprKind;
            rval.Type = pType;
            rval.Flags = EXPRFLAG.EXF_BINOP;
            rval.OptionalLeftChild = p1;
            rval.OptionalRightChild = p2;
            // The call may be lifted, but we do not mark the outer binop as lifted.
            rval.IsLifted = false;
            rval.OptionalUserDefinedCall = call;
            rval.UserDefinedCallMethod = pmpwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprUnaryOp CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType pType, Expr pOperand, Expr call, MethPropWithInst pmpwi)
        {
            Debug.Assert(pType != null);
            Debug.Assert(pOperand != null);
            Debug.Assert(call != null);
            Debug.Assert(pmpwi != null);
            ExprUnaryOp rval = new ExprUnaryOp();
            rval.Kind = exprKind;
            rval.Type = pType;
            rval.Flags = 0;
            rval.Child = pOperand;
            // The call may be lifted, but we do not mark the outer binop as lifted.
            rval.OptionalUserDefinedCall = call;
            rval.UserDefinedCallMethod = pmpwi;
            if (call.HasError)
            {
                rval.SetError();
            }
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprUnaryOp CreateNeg(EXPRFLAG nFlags, Expr pOperand)
        {
            Debug.Assert(pOperand != null);
            ExprUnaryOp pUnaryOp = CreateUnaryOp(ExpressionKind.EK_NEG, pOperand.Type, pOperand);
            pUnaryOp.Flags |= nFlags;
            return pUnaryOp;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the second.

        public ExprBinOp CreateSequence(Expr p1, Expr p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.EK_SEQUENCE, p2.Type, p1, p2);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the first.

        public ExprBinOp CreateReverseSequence(Expr p1, Expr p2)
        {
            Debug.Assert(p1 != null);
            Debug.Assert(p2 != null);
            return CreateBinop(ExpressionKind.EK_SEQREV, p1.Type, p1, p2);
        }

        public ExprAssignment CreateAssignment(Expr pLHS, Expr pRHS)
        {
            ExprAssignment pAssignment = new ExprAssignment();
            pAssignment.Kind = ExpressionKind.EK_ASSIGNMENT;
            pAssignment.Type = pLHS.Type;
            pAssignment.Flags = EXPRFLAG.EXF_ASSGOP;
            pAssignment.LHS = pLHS;
            pAssignment.RHS = pRHS;
            return pAssignment;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public ExprNamedArgumentSpecification CreateNamedArgumentSpecification(Name pName, Expr pValue)
        {
            ExprNamedArgumentSpecification pResult = new ExprNamedArgumentSpecification();

            pResult.Kind = ExpressionKind.EK_NamedArgumentSpecification;
            pResult.Type = pValue.Type;
            pResult.Flags = 0;
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
            rval.Kind = ExpressionKind.EK_WRAP;
            rval.Type = null;
            rval.Flags = 0;
            rval.OptionalExpression = pOptionalExpression;
            if (pOptionalExpression != null)
            {
                rval.Type = pOptionalExpression.Type;
            }
            rval.Flags |= EXPRFLAG.EXF_LVALUE;

            Debug.Assert(rval != null);
            return (rval);
        }
        public ExprWrap CreateWrapNoAutoFree(Scope pCurrentScope, Expr pOptionalWrap)
        {
            ExprWrap rval = CreateWrap(pCurrentScope, pOptionalWrap);
            return rval;
        }
        public ExprBinOp CreateSave(ExprWrap wrap)
        {
            Debug.Assert(wrap != null);
            ExprBinOp expr = CreateBinop(ExpressionKind.EK_SAVE, wrap.Type, wrap.OptionalExpression, wrap);
            expr.SetAssignment();
            return expr;
        }

        public Expr CreateNull()
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
            if (first.Kind != ExpressionKind.EK_LIST)
            {
                Debug.Assert(last == first);
                first = CreateList(first, newItem);
                last = first;
                return;
            }
            Debug.Assert(last.Kind == ExpressionKind.EK_LIST);
            Debug.Assert(last.asLIST().OptionalNextListNode != null);
            Debug.Assert(last.asLIST().OptionalNextListNode.Kind != ExpressionKind.EK_LIST);
            last.asLIST().OptionalNextListNode = CreateList(last.asLIST().OptionalNextListNode, newItem);
            last = last.asLIST().OptionalNextListNode;
        }

        public ExprList CreateList(Expr op1, Expr op2)
        {
            ExprList rval = new ExprList();
            rval.Kind = ExpressionKind.EK_LIST;
            rval.Type = null;
            rval.Flags = 0;
            rval.OptionalElement = op1;
            rval.OptionalNextListNode = op2;
            Debug.Assert(rval != null);
            return (rval);
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
            rval.Kind = ExpressionKind.EK_TYPEARGUMENTS;
            rval.Type = null;
            rval.Flags = 0;
            rval.OptionalElements = pOptionalElements;
            return rval;
        }

        public ExprClass CreateClass(CType pType, Expr pOptionalLHS, ExprTypeArguments pOptionalTypeArguments)
        {
            Debug.Assert(pType != null);
            ExprClass rval = new ExprClass();
            rval.Kind = ExpressionKind.EK_CLASS;
            rval.Type = pType;
            rval.TypeOrNamespace = pType;
            Debug.Assert(rval != null);
            return (rval);
        }

        public ExprClass MakeClass(CType pType)
        {
            Debug.Assert(pType != null);
            return CreateClass(pType, null/* LHS */, null/* type arguments */);
        }
    }
}

