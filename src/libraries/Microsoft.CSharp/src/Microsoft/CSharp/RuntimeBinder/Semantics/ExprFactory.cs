// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class ExprFactory
    {
        public static ExprCall CreateCall(EXPRFLAG flags, CType type, Expr arguments, ExprMemberGroup memberGroup, MethWithInst method) =>
            new ExprCall(type, flags, arguments, memberGroup, method);

        public static ExprField CreateField(CType type, Expr optionalObject, FieldWithType field) =>
            new ExprField(type, optionalObject, field);

        public static ExprArrayInit CreateArrayInit(CType type, Expr arguments, Expr argumentDimensions, int[] dimSizes) =>
            new ExprArrayInit(type, arguments, argumentDimensions, dimSizes);

        public static ExprProperty CreateProperty(CType type, Expr optionalObjectThrough, Expr arguments, ExprMemberGroup memberGroup, PropWithType property, MethWithType setMethod) =>
            new ExprProperty(type, optionalObjectThrough, arguments, memberGroup, property, setMethod);

        public static ExprMemberGroup CreateMemGroup(EXPRFLAG flags, Name name, TypeArray typeArgs, SYMKIND symKind, CType parentType, Expr obj, CMemberLookupResults memberLookupResults) =>
            new ExprMemberGroup(flags, name, typeArgs, symKind, parentType, obj, memberLookupResults);

        public static ExprMemberGroup CreateMemGroup(Expr obj, MethPropWithInst method)
        {
            Name name = method.Sym?.name;
            return CreateMemGroup(
                0, name, method.TypeArgs, method.MethProp()?.getKind() ?? SYMKIND.SK_MethodSymbol, method.GetType(),
                obj, new CMemberLookupResults(TypeArray.Allocate((CType)method.GetType()), name));
        }

        public static ExprUserDefinedConversion CreateUserDefinedConversion(Expr arg, Expr call, MethWithInst method) =>
            new ExprUserDefinedConversion(arg, call, method);

        public static ExprCast CreateCast(CType type, Expr argument) => CreateCast(0, type, argument);

        public static ExprCast CreateCast(EXPRFLAG flags, CType type, Expr argument) => new ExprCast(flags, type, argument);

        public static ExprLocal CreateLocal(LocalVariableSymbol local) => new ExprLocal(local);

        public static ExprBoundLambda CreateAnonymousMethod(AggregateType delegateType, Scope argumentScope, Expr expression) =>
            new ExprBoundLambda(delegateType, argumentScope, expression);

        public static ExprMethodInfo CreateMethodInfo(MethPropWithInst mwi) =>
            CreateMethodInfo(mwi.Meth(), mwi.GetType(), mwi.TypeArgs);

        public static ExprMethodInfo CreateMethodInfo(MethodSymbol method, AggregateType methodType, TypeArray methodParameters) =>
            new ExprMethodInfo(
                TypeManager.GetPredefAgg(method.IsConstructor() ? PredefinedType.PT_CONSTRUCTORINFO : PredefinedType.PT_METHODINFO).getThisType(),
                method, methodType, methodParameters);

        public static ExprPropertyInfo CreatePropertyInfo(PropertySymbol prop, AggregateType propertyType) =>
            new ExprPropertyInfo(TypeManager.GetPredefAgg(PredefinedType.PT_PROPERTYINFO).getThisType(), prop, propertyType);

        public static ExprFieldInfo CreateFieldInfo(FieldSymbol field, AggregateType fieldType) =>
            new ExprFieldInfo(field, fieldType, TypeManager.GetPredefAgg(PredefinedType.PT_FIELDINFO).getThisType());

        public static ExprTypeOf CreateTypeOf(CType sourceType) =>
            new ExprTypeOf(TypeManager.GetPredefAgg(PredefinedType.PT_TYPE).getThisType(), sourceType);

        public static ExprUserLogicalOp CreateUserLogOp(CType type, Expr trueFalseCall, ExprCall operatorCall) =>
            new ExprUserLogicalOp(type, trueFalseCall, operatorCall);

        public static ExprConcat CreateConcat(Expr first, Expr second) => new ExprConcat(first, second);

        public static ExprConstant CreateStringConstant(string str) =>
            CreateConstant(TypeManager.GetPredefAgg(PredefinedType.PT_STRING).getThisType(), ConstVal.Get(str));

        public static ExprMultiGet CreateMultiGet(EXPRFLAG flags, CType type, ExprMulti multi) =>
            new ExprMultiGet(type, flags, multi);

        public static ExprMulti CreateMulti(EXPRFLAG flags, CType type, Expr left, Expr op) =>
            new ExprMulti(type, flags, left, op);

        ////////////////////////////////////////////////////////////////////////////////
        //
        // Precondition:
        //
        // type - Non-null
        //
        // This returns a null for reference types and an EXPRZEROINIT for all others.

        public static Expr CreateZeroInit(CType type)
        {
            Debug.Assert(type != null);

            if (type.IsEnumType)
            {
                // For enum types, we create a constant that has the default value
                // as an object pointer.
                return CreateConstant(type, ConstVal.Get(Activator.CreateInstance(type.AssociatedSystemType)));
            }

            Debug.Assert(type.FundamentalType > FUNDTYPE.FT_NONE);
            Debug.Assert(type.FundamentalType < FUNDTYPE.FT_COUNT);
            switch (type.FundamentalType)
            {
                case FUNDTYPE.FT_PTR:
                    {
                        // Just allocate a new node and fill it in.
                        return CreateCast(0, type, CreateNull());
                    }

                case FUNDTYPE.FT_STRUCT:
                    if (type.IsPredefType(PredefinedType.PT_DECIMAL))
                    {
                        goto default;
                    }

                    goto case FUNDTYPE.FT_VAR;

                case FUNDTYPE.FT_VAR:
                    return new ExprZeroInit(type);

                default:
                    return CreateConstant(type, ConstVal.GetDefaultValue(type.ConstValKind));
            }
        }

        public static ExprConstant CreateConstant(CType type, ConstVal constVal) => new ExprConstant(type, constVal);

        public static ExprConstant CreateIntegerConstant(int x) =>
            CreateConstant(TypeManager.GetPredefAgg(PredefinedType.PT_INT).getThisType(), ConstVal.Get(x));

        public static ExprConstant CreateBoolConstant(bool b) =>
            CreateConstant(TypeManager.GetPredefAgg(PredefinedType.PT_BOOL).getThisType(), ConstVal.Get(b));

        public static ExprArrayIndex CreateArrayIndex(CType type, Expr array, Expr index) =>
            new ExprArrayIndex(type, array, index);

        public static ExprBinOp CreateBinop(ExpressionKind exprKind, CType type, Expr left, Expr right) =>
            new ExprBinOp(exprKind, type, left, right);

        public static ExprUnaryOp CreateUnaryOp(ExpressionKind exprKind, CType type, Expr operand) =>
            new ExprUnaryOp(exprKind, type, operand);

        public static ExprOperator CreateOperator(ExpressionKind exprKind, CType type, Expr arg1, Expr arg2)
        {
            Debug.Assert(arg1 != null);
            Debug.Assert(exprKind.IsUnaryOperator() == (arg2 == null));
            return exprKind.IsUnaryOperator()
                ? (ExprOperator)CreateUnaryOp(exprKind, type, arg1)
                : CreateBinop(exprKind, type, arg1, arg2);
        }

        public static ExprBinOp CreateUserDefinedBinop(ExpressionKind exprKind, CType type, Expr left, Expr right, Expr call, MethPropWithInst userMethod) =>
            new ExprBinOp(exprKind, type, left, right, call, userMethod);

        // The call may be lifted, but we do not mark the outer binop as lifted.
        public static ExprUnaryOp CreateUserDefinedUnaryOperator(ExpressionKind exprKind, CType type, Expr operand, ExprCall call, MethPropWithInst userMethod) =>
            new ExprUnaryOp(exprKind, type, operand, call, userMethod);

        public static ExprUnaryOp CreateNeg(EXPRFLAG flags, Expr operand)
        {
            Debug.Assert(operand != null);
            ExprUnaryOp unary = CreateUnaryOp(ExpressionKind.Negate, operand.Type, operand);
            unary.Flags |= flags;
            return unary;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the second.

        public static ExprBinOp CreateSequence(Expr first, Expr second) =>
            CreateBinop(ExpressionKind.Sequence, second.Type, first, second);

        ////////////////////////////////////////////////////////////////////////////////
        // Create a node that evaluates the first, evaluates the second, results in the first.

        public static ExprAssignment CreateAssignment(Expr left, Expr right) => new ExprAssignment(left, right);

        ////////////////////////////////////////////////////////////////////////////////

        public static ExprNamedArgumentSpecification CreateNamedArgumentSpecification(Name name, Expr value) =>
            new ExprNamedArgumentSpecification(name, value);

        public static ExprWrap CreateWrap(Expr expression) => new ExprWrap(expression);

        public static ExprBinOp CreateSave(ExprWrap wrap)
        {
            Debug.Assert(wrap != null);
            ExprBinOp expr = CreateBinop(ExpressionKind.Save, wrap.Type, wrap.OptionalExpression, wrap);
            expr.SetAssignment();
            return expr;
        }

        public static ExprConstant CreateNull() => CreateConstant(NullType.Instance, default);

        public static void AppendItemToList(Expr newItem, ref Expr first, ref Expr last)
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

        public static ExprList CreateList(Expr op1, Expr op2) => new ExprList(op1, op2);

        public static ExprList CreateList(Expr op1, Expr op2, Expr op3) => CreateList(op1, CreateList(op2, op3));

        public static ExprList CreateList(Expr op1, Expr op2, Expr op3, Expr op4) =>
            CreateList(op1, CreateList(op2, CreateList(op3, op4)));

        public static ExprClass CreateClass(CType type) => new ExprClass(type);
    }
}

