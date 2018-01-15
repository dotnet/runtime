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
        private TypeManager Types => _globalSymbolContext.GetTypes();

        private BSYMMGR GlobalSymbols => _globalSymbolContext.GetGlobalSymbols();

        public ExprCall CreateCall(EXPRFLAG flags, CType type, Expr arguments, ExprMemberGroup memberGroup, MethWithInst method) => 
            new ExprCall(type, flags, arguments, memberGroup, method);

        public ExprField CreateField(CType type, Expr optionalObject, FieldWithType field, bool isLValue) => 
            new ExprField(type, optionalObject, field, isLValue);

        public ExprArrayInit CreateArrayInit(CType type, Expr arguments, Expr argumentDimensions, int[] dimSizes, int dimSize) => 
            new ExprArrayInit(type, arguments, argumentDimensions, dimSizes, dimSize);

        public ExprProperty CreateProperty(CType type, Expr optionalObjectThrough, Expr arguments, ExprMemberGroup memberGroup, PropWithType property, MethWithType setMethod) => 
            new ExprProperty(type, optionalObjectThrough, arguments, memberGroup, property, setMethod);

        public ExprMemberGroup CreateMemGroup(EXPRFLAG flags, Name name, TypeArray typeArgs, SYMKIND symKind, CType parentType, MethodOrPropertySymbol memberSymbol, Expr obj, CMemberLookupResults memberLookupResults) => 
            new ExprMemberGroup(Types.GetMethGrpType(), flags, name, typeArgs, symKind, parentType, memberSymbol, obj, memberLookupResults);

        public ExprMemberGroup CreateMemGroup(Expr obj, MethPropWithInst method)
        {
            Name name = method.Sym?.name;
            MethodOrPropertySymbol methProp = method.MethProp();

            CType type = method.GetType();

            return CreateMemGroup(
                0, name, method.TypeArgs, methProp?.getKind() ?? SYMKIND.SK_MethodSymbol, method.GetType(), methProp,
                obj, new CMemberLookupResults(GlobalSymbols.AllocParams(1, new[] {type}), name));
        }

        public ExprUserDefinedConversion CreateUserDefinedConversion(Expr arg, Expr call, MethWithInst method) => 
            new ExprUserDefinedConversion(arg, call, method);

        public ExprCast CreateCast(CType type, Expr argument) => CreateCast(0, type, argument);

        public ExprCast CreateCast(EXPRFLAG flags, CType type, Expr argument) => new ExprCast(flags, type, argument);

        public ExprLocal CreateLocal(LocalVariableSymbol local) => new ExprLocal(local);

        public ExprBoundLambda CreateAnonymousMethod(AggregateType delegateType, Scope argumentScope, Expr expression) => 
            new ExprBoundLambda(delegateType, argumentScope, expression);

        public ExprMethodInfo CreateMethodInfo(MethPropWithInst mwi) => 
            CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);

        public ExprMethodInfo CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters)
        {
            return new ExprMethodInfo(
                Types.GetPredefAgg(method.IsConstructor() ? PredefinedType.PT_CONSTRUCTORINFO : PredefinedType.PT_METHODINFO).getThisType(),
                method, methodType, methodParameters);
        }

        public ExprPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType) => 
            new ExprPropertyInfo(Types.GetPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType(), prop, propertyType);

        public ExprFieldInfo CreateFieldInfo(FieldSymbol field, AggregateType fieldType) => 
            new ExprFieldInfo(field, fieldType, Types.GetPredefAgg(PredefinedType.PT_FIELDINFO).getThisType());

        public ExprTypeOf CreateTypeOf(CType sourceType) => 
            new ExprTypeOf(Types.GetPredefAgg(PredefinedType.PT_TYPE).getThisType(), sourceType);


        public ExprUserLogicalOp CreateUserLogOp(CType type, Expr trueFalseCall, ExprCall operatorCall) => 
            new ExprUserLogicalOp(type, trueFalseCall, operatorCall);

        public ExprConcat CreateConcat(Expr first, Expr second) => new ExprConcat(first, second);

        public ExprConstant CreateStringConstant(string str) => 
            CreateConstant(Types.GetPredefAgg(PredefinedType.PT_STRING).getThisType(), ConstVal.Get(str));

        public ExprMultiGet CreateMultiGet(EXPRFLAG flags, CType type, ExprMulti multi) => 
            new ExprMultiGet(type, flags, multi);

        public ExprMulti CreateMulti(EXPRFLAG flags, CType type, Expr left, Expr op) => 
            new ExprMulti(type, flags, left, op);

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Precondition:
        //
        // type - Non-null
        //
        // This returns a null for reference types and an EXPRZEROINIT for all others.

        public Expr CreateZeroInit(CType type)
        {
            Debug.Assert(type != null);

            if (type.isEnumType())
            {
                // For enum types, we create a constant that has the default value
                // as an object pointer.
                return CreateConstant(type, ConstVal.Get(Activator.CreateInstance(type.AssociatedSystemType)));
            }

            Debug.Assert(type.fundType() > FUNDTYPE.FT_NONE);
            Debug.Assert(type.fundType() < FUNDTYPE.FT_COUNT);
            switch (type.fundType())
            {
                case FUNDTYPE.FT_PTR:
                    {
                        // Just allocate a new node and fill it in.
                        return CreateCast(0, type, CreateNull());
                    }

                case FUNDTYPE.FT_STRUCT:
                    if (type.isPredefType(PredefinedType.PT_DECIMAL))
                    {
                        goto default;
                    }

                    goto case FUNDTYPE.FT_VAR;

                case FUNDTYPE.FT_VAR:
                    return new ExprZeroInit(type);

                default:
                    return CreateConstant(type, ConstVal.GetDefaultValue(type.constValKind()));
            }
        }

        public ExprConstant CreateConstant(CType type, ConstVal constVal) => new ExprConstant(type, constVal);

        public ExprConstant CreateIntegerConstant(int x) =>
            CreateConstant(Types.GetPredefAgg(PredefinedType.PT_INT).getThisType(), ConstVal.Get(x));

        public ExprConstant CreateBoolConstant(bool b) => 
            CreateConstant(Types.GetPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstVal.Get(b));

        public ExprArrayIndex CreateArrayIndex(CType type, Expr array, Expr index) =>
            new ExprArrayIndex(type, array, index);

        public ExprBinOp CreateBinop(ExpressionKind exprKind, CType type, Expr left, Expr right) => 
            new ExprBinOp(exprKind, type, left, right);

        public ExprUnaryOp CreateUnaryOp(ExpressionKind exprKind, CType type, Expr operand) => 
            new ExprUnaryOp(exprKind, type, operand);

        public ExprOperator CreateOperator(ExpressionKind exprKind, CType type, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(exprKind.IsUnaryOperator() == (arg2 == null));
            return exprKind.IsUnaryOperator()
                ? (ExprOperator)CreateUnaryOp(exprKind, type, arg1)
                : CreateBinop(exprKind, type, arg1, arg2);
        }


        public ExprBinOp CreateUserDefinedBinop(ExpressionKind exprKind, CType type, Expr left, Expr right, Expr call, MethPropWithInst userMethod) => 
            new ExprBinOp(exprKind, type, left, right, call, userMethod);

        // The call may be lifted, but we do not mark the outer binop as lifted.
        public ExprUnaryOp CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType type, Expr operand, ExprCall call, MethPropWithInst userMethod) => 
            new ExprUnaryOp(exprKind, type, operand, call, userMethod);

        public ExprUnaryOp CreateNeg(EXPRFLAG flags, Expr operand)
        {
            Debug.Assert(operand != null);
            ExprUnaryOp unary = CreateUnaryOp(ExpressionKind.Negate, operand.Type, operand);
            unary.Flags |= flags;
            return unary;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the second.

        public ExprBinOp CreateSequence(Expr first, Expr second) =>
            CreateBinop(ExpressionKind.Sequence, second.Type, first, second);

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the first.

        public ExprAssignment CreateAssignment(Expr left, Expr right) => new ExprAssignment(left, right);

        ////////////////////////////////////////////////////////////////////////////////

        public ExprNamedArgumentSpecification CreateNamedArgumentSpecification(Name name, Expr value) =>
            new ExprNamedArgumentSpecification(name, value);

        public ExprWrap CreateWrap(Expr expression) => new ExprWrap(expression);

        public ExprBinOp CreateSave(ExprWrap wrap)
        {
            Debug.Assert(wrap != null);
            ExprBinOp expr = CreateBinop(ExpressionKind.Save, wrap.Type, wrap.OptionalExpression, wrap);
            expr.SetAssignment();
            return expr;
        }

        public ExprConstant CreateNull() => CreateConstant(Types.GetNullType(), default(ConstVal));

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

        public ExprList CreateList(Expr op1, Expr op2) => new ExprList(op1, op2);

        public ExprList CreateList(Expr op1, Expr op2, Expr op3) => CreateList(op1, CreateList(op2, op3));

        public ExprList CreateList(Expr op1, Expr op2, Expr op3, Expr op4) =>
            CreateList(op1, CreateList(op2, CreateList(op3, op4)));

        public ExprClass CreateClass(CType type) => new ExprClass(type);
    }
}

