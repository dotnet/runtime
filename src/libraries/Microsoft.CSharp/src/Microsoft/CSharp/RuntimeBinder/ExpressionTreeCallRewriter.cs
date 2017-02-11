// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class ExpressionTreeCallRewriter : ExprVisitorBase
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Members

        private sealed class ExpressionEXPR : EXPR
        {
            public readonly Expression Expression;
            public ExpressionEXPR(Expression e)
            {
                Expression = e;
            }
        }

        private readonly Dictionary<EXPRCALL, Expression> _DictionaryOfParameters;
        private readonly IEnumerable<Expression> _ListOfParameters;
        private readonly TypeManager _typeManager;
        // Counts how many EXPRSAVEs we've encountered so we know which index into the 
        // parameter list we should be taking.
        private int _currentParameterIndex;

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionTreeCallRewriter(TypeManager typeManager, IEnumerable<Expression> listOfParameters)
        {
            _typeManager = typeManager;
            _DictionaryOfParameters = new Dictionary<EXPRCALL, Expression>();
            _ListOfParameters = listOfParameters;
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static Expression Rewrite(TypeManager typeManager, EXPR pExpr, IEnumerable<Expression> listOfParameters)
        {
            ExpressionTreeCallRewriter rewriter = new ExpressionTreeCallRewriter(typeManager, listOfParameters);

            // We should have a EXPRBINOP thats an EK_SEQUENCE. The RHS of our sequence
            // should be a call to PM_EXPRESSION_LAMBDA. The LHS of our sequence is the 
            // set of declarations for the parameters that we'll need.
            // Assert all of these first, and then unwrap them.

            Debug.Assert(pExpr != null);
            Debug.Assert(pExpr.isBIN());
            Debug.Assert(pExpr.kind == ExpressionKind.EK_SEQUENCE);
            Debug.Assert(pExpr.asBIN().GetOptionalRightChild() != null);
            Debug.Assert(pExpr.asBIN().GetOptionalRightChild().isCALL());
            Debug.Assert(pExpr.asBIN().GetOptionalRightChild().asCALL().PredefinedMethod == PREDEFMETH.PM_EXPRESSION_LAMBDA);
            Debug.Assert(pExpr.asBIN().GetOptionalLeftChild() != null);

            // Visit the left to generate the parameter construction.
            rewriter.Visit(pExpr.asBIN().GetOptionalLeftChild());
            EXPRCALL call = pExpr.asBIN().GetOptionalRightChild().asCALL();

            ExpressionEXPR e = rewriter.Visit(call) as ExpressionEXPR;
            return e.Expression;
        }

        /////////////////////////////////////////////////////////////////////////////////

        protected override EXPR VisitSAVE(EXPRBINOP pExpr)
        {
            // Saves should have a LHS that is a CALL to PM_EXPRESSION_PARAMETER
            // and a RHS that is a WRAP of that call.
            Debug.Assert(pExpr.GetOptionalLeftChild() != null);
            Debug.Assert(pExpr.GetOptionalLeftChild().isCALL());
            Debug.Assert(pExpr.GetOptionalLeftChild().asCALL().PredefinedMethod == PREDEFMETH.PM_EXPRESSION_PARAMETER);
            Debug.Assert(pExpr.GetOptionalRightChild() != null);
            Debug.Assert(pExpr.GetOptionalRightChild().isWRAP());

            EXPRCALL call = pExpr.GetOptionalLeftChild().asCALL();
            EXPRTYPEOF TypeOf = call.GetOptionalArguments().asLIST().GetOptionalElement().asTYPEOF();
            Expression parameter = _ListOfParameters.ElementAt(_currentParameterIndex++);
            _DictionaryOfParameters.Add(call, parameter);

            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        protected override EXPR VisitCAST(EXPRCAST pExpr)
        {
            return base.VisitCAST(pExpr);
        }

        /////////////////////////////////////////////////////////////////////////////////

        protected override EXPR VisitCALL(EXPRCALL pExpr)
        {
            if (pExpr.PredefinedMethod != PREDEFMETH.PM_FIRST)
            {
                switch (pExpr.PredefinedMethod)
                {
                    case PREDEFMETH.PM_EXPRESSION_LAMBDA:
                        return GenerateLambda(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_CALL:
                        return GenerateCall(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
                        return GenerateArrayIndex(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_CONVERT:
                    case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
                        return GenerateConvert(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_PROPERTY:
                        return GenerateProperty(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_FIELD:
                        return GenerateField(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_INVOKE:
                        return GenerateInvoke(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_NEW:
                        return GenerateNew(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_ADD:
                    case PREDEFMETH.PM_EXPRESSION_AND:
                    case PREDEFMETH.PM_EXPRESSION_DIVIDE:
                    case PREDEFMETH.PM_EXPRESSION_EQUAL:
                    case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
                    case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
                    case PREDEFMETH.PM_EXPRESSION_MODULO:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
                    case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
                    case PREDEFMETH.PM_EXPRESSION_OR:
                    case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
                    case PREDEFMETH.PM_EXPRESSION_ORELSE:
                    case PREDEFMETH.PM_EXPRESSION_ANDALSO:
                    // Checked
                    case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
                        return GenerateBinaryOperator(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
                    // Checked
                    case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
                        return GenerateUserDefinedBinaryOperator(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_NEGATE:
                    case PREDEFMETH.PM_EXPRESSION_NOT:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                        return GenerateUnaryOperator(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                        return GenerateUserDefinedUnaryOperator(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
                        return GenerateConstantType(pExpr);

                    case PREDEFMETH.PM_EXPRESSION_ASSIGN:
                        return GenerateAssignment(pExpr);

                    default:
                        Debug.Assert(false, "Invalid Predefined Method in VisitCALL");
                        throw Error.InternalCompilerError();
                }
            }
            return pExpr;
        }

        #region Generators
        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateLambda(EXPRCALL pExpr)
        {
            // We always call Lambda(body, arrayinit) where the arrayinit 
            // is the initialization of the parameters.
            ExpressionEXPR body = Visit(pExpr.GetOptionalArguments().asLIST().GetOptionalElement()) as ExpressionEXPR;

            Expression e = body.Expression;

            /*
             * // Do we need to do this?
            if (e.Type.IsValueType)
            {
                // If we have a value type, convert it to object so that boxing
                // can happen.

                e = Expression.Convert(body.Expression, typeof(object));
            }
             * */
            return new ExpressionEXPR(e);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateCall(EXPRCALL pExpr)
        {
            // Our arguments are: object, methodinfo, parameters.
            // The object is either an EXPRWRAP of a CALL, or a CALL that is a PM_CONVERT, whose
            // argument is the WRAP of a CALL. Deal with that first.

            EXPRMETHODINFO methinfo;
            EXPRARRINIT arrinit;

            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();
            if (list.GetOptionalNextListNode().isLIST())
            {
                methinfo = list.GetOptionalNextListNode().asLIST().GetOptionalElement().asMETHODINFO();
                arrinit = list.GetOptionalNextListNode().asLIST().GetOptionalNextListNode().asARRINIT();
            }
            else
            {
                methinfo = list.GetOptionalNextListNode().asMETHODINFO();
                arrinit = null;
            }

            Expression obj = null;
            MethodInfo m = GetMethodInfoFromExpr(methinfo);
            Expression[] arguments = GetArgumentsFromArrayInit(arrinit);

            if (m == null)
            {
                Debug.Assert(false, "How did we get a call that doesn't have a methodinfo?");
                throw Error.InternalCompilerError();
            }

            // The DLR is expecting the instance for a static invocation to be null. If we have
            // an instance method, fetch the object.
            if (!m.IsStatic)
            {
                obj = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalElement());
            }

            return new ExpressionEXPR(Expression.Call(obj, m, arguments));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateArrayIndex(EXPRCALL pExpr)
        {
            // We have two possibilities here - we're either a single index array, in which 
            // case we'll be PM_EXPRESSION_ARRAYINDEX, or we have multiple dimensions,
            // in which case we are PM_EXPRESSION_ARRAYINDEX2. 
            //
            // Our arguments then, are: object, index or object, indices.
            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();
            Expression obj = GetExpression(list.GetOptionalElement());
            Expression[] indices;

            if (pExpr.PredefinedMethod == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX)
            {
                indices = new Expression[] { GetExpression(list.GetOptionalNextListNode()) };
            }
            else
            {
                Debug.Assert(pExpr.PredefinedMethod == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2);
                indices = GetArgumentsFromArrayInit(list.GetOptionalNextListNode().asARRINIT());
            }
            return new ExpressionEXPR(Expression.ArrayAccess(obj, indices));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateConvert(EXPRCALL pExpr)
        {
            PREDEFMETH pm = pExpr.PredefinedMethod;
            Expression e;
            Type t;

            if (pm == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED ||
                pm == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED)
            {
                // If we have a user defined conversion, then we'll have the object
                // as the first element, and another list as a second element. This list
                // contains a TYPEOF as the first element, and the METHODINFO for the call
                // as the second.

                EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();
                EXPRLIST list2 = list.GetOptionalNextListNode().asLIST();
                e = GetExpression(list.GetOptionalElement());
                t = list2.GetOptionalElement().asTYPEOF().SourceType.type.AssociatedSystemType;

                if (e.Type.MakeByRefType() == t)
                {
                    // We're trying to convert from a type to its by ref type. Don't do that.
                    return new ExpressionEXPR(e);
                }
                Debug.Assert((pExpr.flags & EXPRFLAG.EXF_UNBOXRUNTIME) == 0);

                MethodInfo m = GetMethodInfoFromExpr(list2.GetOptionalNextListNode().asMETHODINFO());

                if (pm == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED)
                {
                    return new ExpressionEXPR(Expression.Convert(e, t, m));
                }
                return new ExpressionEXPR(Expression.ConvertChecked(e, t, m));
            }
            else
            {
                Debug.Assert(pm == PREDEFMETH.PM_EXPRESSION_CONVERT ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED);

                // If we have a standard conversion, then we'll have some object as
                // the first list element (ie a WRAP or a CALL), and then a TYPEOF
                // as the second list element.
                EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();

                e = GetExpression(list.GetOptionalElement());
                t = list.GetOptionalNextListNode().asTYPEOF().SourceType.type.AssociatedSystemType;

                if (e.Type.MakeByRefType() == t)
                {
                    // We're trying to convert from a type to its by ref type. Don't do that.
                    return new ExpressionEXPR(e);
                }

                if ((pExpr.flags & EXPRFLAG.EXF_UNBOXRUNTIME) != 0)
                {
                    // If we want to unbox this thing, return that instead of the convert.
                    return new ExpressionEXPR(Expression.Unbox(e, t));
                }

                if (pm == PREDEFMETH.PM_EXPRESSION_CONVERT)
                {
                    return new ExpressionEXPR(Expression.Convert(e, t));
                }
                return new ExpressionEXPR(Expression.ConvertChecked(e, t));
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateProperty(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();

            EXPR instance = list.GetOptionalElement();
            EXPRPropertyInfo propinfo = list.GetOptionalNextListNode().isLIST() ?
                list.GetOptionalNextListNode().asLIST().GetOptionalElement().asPropertyInfo() :
                list.GetOptionalNextListNode().asPropertyInfo();
            EXPRARRINIT arguments = list.GetOptionalNextListNode().isLIST() ?
                list.GetOptionalNextListNode().asLIST().GetOptionalNextListNode().asARRINIT() : null;

            PropertyInfo p = GetPropertyInfoFromExpr(propinfo);

            if (p == null)
            {
                Debug.Assert(false, "How did we get a prop that doesn't have a propinfo?");
                throw Error.InternalCompilerError();
            }

            if (arguments == null)
            {
                return new ExpressionEXPR(Expression.Property(GetExpression(instance), p));
            }
            return new ExpressionEXPR(Expression.Property(GetExpression(instance), p, GetArgumentsFromArrayInit(arguments)));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateField(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();
            Type t = list.GetOptionalNextListNode().asFIELDINFO().FieldType().AssociatedSystemType;
            FieldInfo f = list.GetOptionalNextListNode().asFIELDINFO().Field().AssociatedFieldInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!t.GetTypeInfo().IsGenericType && !t.GetTypeInfo().IsNested)
            {
                t = f.DeclaringType;
            }

            // Now find the generic'ed one if we're generic.
            if (t.GetTypeInfo().IsGenericType)
            {
                f = t.GetField(f.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }

            return new ExpressionEXPR(Expression.Field(GetExpression(list.GetOptionalElement()), f));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateInvoke(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();

            return new ExpressionEXPR(Expression.Invoke(
                GetExpression(list.GetOptionalElement()),
                GetArgumentsFromArrayInit(list.GetOptionalNextListNode().asARRINIT())));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateNew(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.asCALL().GetOptionalArguments().asLIST();

            var constructor = GetConstructorInfoFromExpr(list.GetOptionalElement().asMETHODINFO());
            var arguments = GetArgumentsFromArrayInit(list.GetOptionalNextListNode().asARRINIT());
            return new ExpressionEXPR(Expression.New(constructor, arguments));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateConstantType(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();

            return new ExpressionEXPR(
                Expression.Constant(
                    GetObject(list.GetOptionalElement()),
                    list.GetOptionalNextListNode().asTYPEOF().SourceType.type.AssociatedSystemType));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateAssignment(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();

            return new ExpressionEXPR(Expression.Assign(
                GetExpression(list.GetOptionalElement()),
                GetExpression(list.GetOptionalNextListNode())));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateBinaryOperator(EXPRCALL pExpr)
        {
            Expression arg1 = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalElement());
            Expression arg2 = GetExpression(pExpr.GetOptionalArguments().asLIST().GetOptionalNextListNode());

            switch (pExpr.PredefinedMethod)
            {
                case PREDEFMETH.PM_EXPRESSION_ADD:
                    return new ExpressionEXPR(Expression.Add(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_AND:
                    return new ExpressionEXPR(Expression.And(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_DIVIDE:
                    return new ExpressionEXPR(Expression.Divide(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_EQUAL:
                    return new ExpressionEXPR(Expression.Equal(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
                    return new ExpressionEXPR(Expression.ExclusiveOr(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
                    return new ExpressionEXPR(Expression.GreaterThan(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
                    return new ExpressionEXPR(Expression.GreaterThanOrEqual(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
                    return new ExpressionEXPR(Expression.LeftShift(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
                    return new ExpressionEXPR(Expression.LessThan(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
                    return new ExpressionEXPR(Expression.LessThanOrEqual(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_MODULO:
                    return new ExpressionEXPR(Expression.Modulo(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
                    return new ExpressionEXPR(Expression.Multiply(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
                    return new ExpressionEXPR(Expression.NotEqual(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_OR:
                    return new ExpressionEXPR(Expression.Or(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
                    return new ExpressionEXPR(Expression.RightShift(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
                    return new ExpressionEXPR(Expression.Subtract(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_ORELSE:
                    return new ExpressionEXPR(Expression.OrElse(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_ANDALSO:
                    return new ExpressionEXPR(Expression.AndAlso(arg1, arg2));

                // Checked
                case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
                    return new ExpressionEXPR(Expression.AddChecked(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
                    return new ExpressionEXPR(Expression.MultiplyChecked(arg1, arg2));
                case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
                    return new ExpressionEXPR(Expression.SubtractChecked(arg1, arg2));

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateBinaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateUserDefinedBinaryOperator(EXPRCALL pExpr)
        {
            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();
            Expression arg1 = GetExpression(list.GetOptionalElement());
            Expression arg2 = GetExpression(list.GetOptionalNextListNode().asLIST().GetOptionalElement());

            list = list.GetOptionalNextListNode().asLIST();
            MethodInfo methodInfo;
            bool bIsLifted = false;
            if (list.GetOptionalNextListNode().isLIST())
            {
                EXPRCONSTANT isLifted = list.GetOptionalNextListNode().asLIST().GetOptionalElement().asCONSTANT();
                bIsLifted = isLifted.getVal().iVal == 1;
                methodInfo = GetMethodInfoFromExpr(list.GetOptionalNextListNode().asLIST().GetOptionalNextListNode().asMETHODINFO());
            }
            else
            {
                methodInfo = GetMethodInfoFromExpr(list.GetOptionalNextListNode().asMETHODINFO());
            }

            switch (pExpr.PredefinedMethod)
            {
                case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Add(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
                    return new ExpressionEXPR(Expression.And(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Divide(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Equal(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
                    return new ExpressionEXPR(Expression.ExclusiveOr(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
                    return new ExpressionEXPR(Expression.GreaterThan(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
                    return new ExpressionEXPR(Expression.GreaterThanOrEqual(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
                    return new ExpressionEXPR(Expression.LeftShift(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
                    return new ExpressionEXPR(Expression.LessThan(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
                    return new ExpressionEXPR(Expression.LessThanOrEqual(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Modulo(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Multiply(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
                    return new ExpressionEXPR(Expression.NotEqual(arg1, arg2, bIsLifted, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Or(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
                    return new ExpressionEXPR(Expression.RightShift(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Subtract(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
                    return new ExpressionEXPR(Expression.OrElse(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
                    return new ExpressionEXPR(Expression.AndAlso(arg1, arg2, methodInfo));

                // Checked
                case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
                    return new ExpressionEXPR(Expression.AddChecked(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
                    return new ExpressionEXPR(Expression.MultiplyChecked(arg1, arg2, methodInfo));
                case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
                    return new ExpressionEXPR(Expression.SubtractChecked(arg1, arg2, methodInfo));

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUserDefinedBinaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateUnaryOperator(EXPRCALL pExpr)
        {
            PREDEFMETH pm = pExpr.PredefinedMethod;
            Expression arg = GetExpression(pExpr.GetOptionalArguments());

            switch (pm)
            {
                case PREDEFMETH.PM_EXPRESSION_NOT:
                    return new ExpressionEXPR(Expression.Not(arg));

                case PREDEFMETH.PM_EXPRESSION_NEGATE:
                    return new ExpressionEXPR(Expression.Negate(arg));

                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                    return new ExpressionEXPR(Expression.NegateChecked(arg));

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUnaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionEXPR GenerateUserDefinedUnaryOperator(EXPRCALL pExpr)
        {
            PREDEFMETH pm = pExpr.PredefinedMethod;
            EXPRLIST list = pExpr.GetOptionalArguments().asLIST();
            Expression arg = GetExpression(list.GetOptionalElement());
            MethodInfo methodInfo = GetMethodInfoFromExpr(list.GetOptionalNextListNode().asMETHODINFO());

            switch (pm)
            {
                case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Not(arg, methodInfo));

                case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                    return new ExpressionEXPR(Expression.Negate(arg, methodInfo));

                case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                    return new ExpressionEXPR(Expression.UnaryPlus(arg, methodInfo));

                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                    return new ExpressionEXPR(Expression.NegateChecked(arg, methodInfo));

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUserDefinedUnaryOperator");
                    throw Error.InternalCompilerError();
            }
        }
        #endregion

        #region Helpers
        /////////////////////////////////////////////////////////////////////////////////

        private Expression GetExpression(EXPR pExpr)
        {
            if (pExpr.isWRAP())
            {
                return _DictionaryOfParameters[pExpr.asWRAP().GetOptionalExpression().asCALL()];
            }
            else if (pExpr.isCONSTANT())
            {
                Debug.Assert(pExpr.type.IsNullType());
                return null;
            }
            else
            {
                // We can have a convert node or a call of a user defined conversion.
                Debug.Assert(pExpr.isCALL());
                EXPRCALL call = pExpr.asCALL();
                PREDEFMETH pm = call.PredefinedMethod;
                Debug.Assert(pm == PREDEFMETH.PM_EXPRESSION_CONVERT ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT ||
                    pm == PREDEFMETH.PM_EXPRESSION_CALL ||
                    pm == PREDEFMETH.PM_EXPRESSION_PROPERTY ||
                    pm == PREDEFMETH.PM_EXPRESSION_FIELD ||
                    pm == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX ||
                    pm == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2 ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE ||
                    pm == PREDEFMETH.PM_EXPRESSION_NEW ||

                    // Binary operators.
                    pm == PREDEFMETH.PM_EXPRESSION_ASSIGN ||
                    pm == PREDEFMETH.PM_EXPRESSION_ADD ||
                    pm == PREDEFMETH.PM_EXPRESSION_AND ||
                    pm == PREDEFMETH.PM_EXPRESSION_DIVIDE ||
                    pm == PREDEFMETH.PM_EXPRESSION_EQUAL ||
                    pm == PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR ||
                    pm == PREDEFMETH.PM_EXPRESSION_GREATERTHAN ||
                    pm == PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL ||
                    pm == PREDEFMETH.PM_EXPRESSION_LEFTSHIFT ||
                    pm == PREDEFMETH.PM_EXPRESSION_LESSTHAN ||
                    pm == PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL ||
                    pm == PREDEFMETH.PM_EXPRESSION_MODULO ||
                    pm == PREDEFMETH.PM_EXPRESSION_MULTIPLY ||
                    pm == PREDEFMETH.PM_EXPRESSION_NOTEQUAL ||
                    pm == PREDEFMETH.PM_EXPRESSION_OR ||
                    pm == PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT ||
                    pm == PREDEFMETH.PM_EXPRESSION_SUBTRACT ||
                    pm == PREDEFMETH.PM_EXPRESSION_ORELSE ||
                    pm == PREDEFMETH.PM_EXPRESSION_ANDALSO ||
                    pm == PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED ||

                    // Checked binary
                    pm == PREDEFMETH.PM_EXPRESSION_ADDCHECKED ||
                    pm == PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED ||
                    pm == PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED ||
                    pm == PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED ||

                    // Unary operators.
                    pm == PREDEFMETH.PM_EXPRESSION_NOT ||
                    pm == PREDEFMETH.PM_EXPRESSION_NEGATE ||
                    pm == PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED ||

                    // Checked unary
                    pm == PREDEFMETH.PM_EXPRESSION_NEGATECHECKED ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED ||
                    pm == PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED
                    );

                switch (pm)
                {
                    case PREDEFMETH.PM_EXPRESSION_CALL:
                        return GenerateCall(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_CONVERT:
                    case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
                        return GenerateConvert(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT:
                        {
                            EXPRLIST list = call.GetOptionalArguments().asLIST();
                            return Expression.NewArrayInit(
                                list.GetOptionalElement().asTYPEOF().SourceType.type.AssociatedSystemType,
                                GetArgumentsFromArrayInit(list.GetOptionalNextListNode().asARRINIT()));
                        }

                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
                        return GenerateArrayIndex(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_NEW:
                        return GenerateNew(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_PROPERTY:
                        return GenerateProperty(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_FIELD:
                        return GenerateField(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
                        return GenerateConstantType(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_ASSIGN:
                        return GenerateAssignment(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_ADD:
                    case PREDEFMETH.PM_EXPRESSION_AND:
                    case PREDEFMETH.PM_EXPRESSION_DIVIDE:
                    case PREDEFMETH.PM_EXPRESSION_EQUAL:
                    case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
                    case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
                    case PREDEFMETH.PM_EXPRESSION_MODULO:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
                    case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
                    case PREDEFMETH.PM_EXPRESSION_OR:
                    case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
                    case PREDEFMETH.PM_EXPRESSION_ORELSE:
                    case PREDEFMETH.PM_EXPRESSION_ANDALSO:
                    // Checked
                    case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
                        return GenerateBinaryOperator(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
                    // Checked
                    case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
                        return GenerateUserDefinedBinaryOperator(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_NOT:
                    case PREDEFMETH.PM_EXPRESSION_NEGATE:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                        return GenerateUnaryOperator(call).Expression;

                    case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                        return GenerateUserDefinedUnaryOperator(call).Expression;

                    default:
                        Debug.Assert(false, "Invalid Predefined Method in GetExpression");
                        throw Error.InternalCompilerError();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private object GetObject(EXPR pExpr)
        {
            if (pExpr.isCAST())
            {
                return GetObject(pExpr.asCAST().GetArgument());
            }
            else if (pExpr.isTYPEOF())
            {
                return pExpr.asTYPEOF().SourceType.type.AssociatedSystemType;
            }
            else if (pExpr.isMETHODINFO())
            {
                return GetMethodInfoFromExpr(pExpr.asMETHODINFO());
            }
            else if (pExpr.isCONSTANT())
            {
                CONSTVAL val = pExpr.asCONSTANT().Val;
                CType underlyingType = pExpr.type;
                object objval;

                if (pExpr.type.IsNullType())
                {
                    return null;
                }

                if (pExpr.type.isEnumType())
                {
                    underlyingType = underlyingType.getAggregate().GetUnderlyingType();
                }

                switch (Type.GetTypeCode(underlyingType.AssociatedSystemType))
                {
                    case TypeCode.Boolean:
                        objval = val.boolVal;
                        break;
                    case TypeCode.SByte:
                        objval = val.sbyteVal;
                        break;
                    case TypeCode.Byte:
                        objval = val.byteVal;
                        break;
                    case TypeCode.Int16:
                        objval = val.shortVal;
                        break;
                    case TypeCode.UInt16:
                        objval = val.ushortVal;
                        break;
                    case TypeCode.Int32:
                        objval = val.iVal;
                        break;
                    case TypeCode.UInt32:
                        objval = val.uiVal;
                        break;
                    case TypeCode.Int64:
                        objval = val.longVal;
                        break;
                    case TypeCode.UInt64:
                        objval = val.ulongVal;
                        break;
                    case TypeCode.Single:
                        objval = val.floatVal;
                        break;
                    case TypeCode.Double:
                        objval = val.doubleVal;
                        break;
                    case TypeCode.Decimal:
                        objval = val.decVal;
                        break;
                    case TypeCode.Char:
                        objval = val.cVal;
                        break;
                    case TypeCode.String:
                        objval = val.strVal;
                        break;
                    default:
                        objval = val.objectVal;
                        break;
                }

                if (pExpr.type.isEnumType())
                {
                    objval = Enum.ToObject(pExpr.type.AssociatedSystemType, objval);
                }

                return objval;
            }
            else if (pExpr.isZEROINIT())
            {
                if (pExpr.asZEROINIT().OptionalArgument != null)
                {
                    return GetObject(pExpr.asZEROINIT().OptionalArgument);
                }
                return Activator.CreateInstance(pExpr.type.AssociatedSystemType);
            }

            Debug.Assert(false, "Invalid EXPR in GetObject");
            throw Error.InternalCompilerError();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression[] GetArgumentsFromArrayInit(EXPRARRINIT arrinit)
        {
            List<Expression> expressions = new List<Expression>();

            if (arrinit != null)
            {
                EXPR list = arrinit.GetOptionalArguments();
                EXPR p = list;
                while (list != null)
                {
                    if (list.isLIST())
                    {
                        p = list.asLIST().GetOptionalElement();
                        list = list.asLIST().GetOptionalNextListNode();
                    }
                    else
                    {
                        p = list;
                        list = null;
                    }
                    expressions.Add(GetExpression(p));
                }

                Debug.Assert(expressions.Count == arrinit.dimSizes[0]);
            }
            return expressions.ToArray();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private MethodInfo GetMethodInfoFromExpr(EXPRMETHODINFO methinfo)
        {
            // To do this, we need to construct a type array of the parameter types,
            // get the parent constructed type, and get the method from it.

            AggregateType aggType = methinfo.Method.Ats;
            MethodSymbol methSym = methinfo.Method.Meth();

            TypeArray genericParams = _typeManager.SubstTypeArray(methSym.Params, aggType, methSym.typeVars);
            CType genericReturn = _typeManager.SubstType(methSym.RetType, aggType, methSym.typeVars);

            Type type = aggType.AssociatedSystemType;
            MethodInfo methodInfo = methSym.AssociatedMemberInfo as MethodInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!type.GetTypeInfo().IsGenericType && !type.IsNested)
            {
                type = methodInfo.DeclaringType;
            }

            // We need to find the associated methodinfo on the instantiated type.
            foreach (MethodInfo m in type.GetRuntimeMethods())
            {
#if UNSUPPORTEDAPI
                if ((m.MetadataToken != methodInfo.MetadataToken) || (m.Module != methodInfo.Module))
#else
                if (!m.HasSameMetadataDefinitionAs(methodInfo))
#endif
                {
                    continue;
                }

                Debug.Assert((m.Name == methodInfo.Name) &&
                    (m.GetParameters().Length == genericParams.size) &&
                    (TypesAreEqual(m.ReturnType, genericReturn.AssociatedSystemType)));

                bool bMatch = true;
                ParameterInfo[] parameters = m.GetParameters();
                for (int i = 0; i < genericParams.size; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericParams.Item(i).AssociatedSystemType))
                    {
                        bMatch = false;
                        break;
                    }
                }
                if (bMatch)
                {
                    if (m.IsGenericMethod)
                    {
                        int size = methinfo.Method.TypeArgs != null ? methinfo.Method.TypeArgs.size : 0;
                        Type[] typeArgs = new Type[size];
                        if (size > 0)
                        {
                            for (int i = 0; i < methinfo.Method.TypeArgs.size; i++)
                            {
                                typeArgs[i] = methinfo.Method.TypeArgs[i].AssociatedSystemType;
                            }
                        }
                        return m.MakeGenericMethod(typeArgs);
                    }

                    return m;
                }
            }

            Debug.Assert(false, "Could not find matching method");
            throw Error.InternalCompilerError();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private ConstructorInfo GetConstructorInfoFromExpr(EXPRMETHODINFO methinfo)
        {
            // To do this, we need to construct a type array of the parameter types,
            // get the parent constructed type, and get the method from it.

            AggregateType aggType = methinfo.Method.Ats;
            MethodSymbol methSym = methinfo.Method.Meth();

            TypeArray genericInstanceParams = _typeManager.SubstTypeArray(methSym.Params, aggType);
            Type type = aggType.AssociatedSystemType;
            ConstructorInfo ctorInfo = (ConstructorInfo)methSym.AssociatedMemberInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!type.GetTypeInfo().IsGenericType && !type.IsNested)
            {
                type = ctorInfo.DeclaringType;
            }

            foreach (ConstructorInfo c in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
#if UNSUPPORTEDAPI
                if ((c.MetadataToken != ctorInfo.MetadataToken) || (c.Module != ctorInfo.Module))
#else
                if (!c.HasSameMetadataDefinitionAs(ctorInfo))
#endif
                {
                    continue;
                }
                Debug.Assert(c.GetParameters() == null || c.GetParameters().Length == genericInstanceParams.size);

                bool bMatch = true;
                ParameterInfo[] parameters = c.GetParameters();
                for (int i = 0; i < genericInstanceParams.size; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericInstanceParams.Item(i).AssociatedSystemType))
                    {
                        bMatch = false;
                        break;
                    }
                }
                if (bMatch)
                {
                    return c;
                }
            }

            Debug.Assert(false, "Could not find matching constructor");
            throw Error.InternalCompilerError();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private PropertyInfo GetPropertyInfoFromExpr(EXPRPropertyInfo propinfo)
        {
            // To do this, we need to construct a type array of the parameter types,
            // get the parent constructed type, and get the property from it.

            AggregateType aggType = propinfo.Property.Ats;
            PropertySymbol propSym = propinfo.Property.Prop();

            TypeArray genericInstanceParams = _typeManager.SubstTypeArray(propSym.Params, aggType, null);
            CType genericInstanceReturn = _typeManager.SubstType(propSym.RetType, aggType, null);

            Type type = aggType.AssociatedSystemType;
            PropertyInfo propertyInfo = propSym.AssociatedPropertyInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!type.GetTypeInfo().IsGenericType && !type.IsNested)
            {
                type = propertyInfo.DeclaringType;
            }

            foreach (PropertyInfo p in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
#if UNSUPPORTEDAPI
                if ((p.MetadataToken != propertyInfo.MetadataToken) || (p.Module != propertyInfo.Module))
#else
                if (!p.HasSameMetadataDefinitionAs(propertyInfo))
                {
#endif
                    continue;
                }
                Debug.Assert((p.Name == propertyInfo.Name) &&
                    (p.GetIndexParameters() == null || p.GetIndexParameters().Length == genericInstanceParams.size));

                bool bMatch = true;
                ParameterInfo[] parameters = p.GetSetMethod(true) != null ?
                    p.GetSetMethod(true).GetParameters() : p.GetGetMethod(true).GetParameters();
                for (int i = 0; i < genericInstanceParams.size; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericInstanceParams.Item(i).AssociatedSystemType))
                    {
                        bMatch = false;
                        break;
                    }
                }
                if (bMatch)
                {
                    return p;
                }
            }

            Debug.Assert(false, "Could not find matching property");
            throw Error.InternalCompilerError();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private bool TypesAreEqual(Type t1, Type t2)
        {
            if (t1 == t2)
            {
                return true;
            }

            return t1.IsEquivalentTo(t2);
        }
        #endregion
    }
}
