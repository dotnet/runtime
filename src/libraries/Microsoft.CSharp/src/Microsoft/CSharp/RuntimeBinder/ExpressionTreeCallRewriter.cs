// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class ExpressionTreeCallRewriter : ExprVisitorBase
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Members

        private sealed class ExpressionExpr : Expr
        {
            public readonly Expression Expression;
            public ExpressionExpr(Expression e)
                : base(0)
            {
                Expression = e;
            }
        }

        private readonly Dictionary<ExprCall, Expression> _DictionaryOfParameters;
        private readonly Expression[] _ListOfParameters;
        private readonly TypeManager _typeManager;
        // Counts how many EXPRSAVEs we've encountered so we know which index into the 
        // parameter list we should be taking.
        private int _currentParameterIndex;

        /////////////////////////////////////////////////////////////////////////////////

        private ExpressionTreeCallRewriter(TypeManager typeManager, Expression[] listOfParameters)
        {
            _typeManager = typeManager;
            _DictionaryOfParameters = new Dictionary<ExprCall, Expression>();
            _ListOfParameters = listOfParameters;
        }

        /////////////////////////////////////////////////////////////////////////////////

        public static Expression Rewrite(TypeManager typeManager, ExprBinOp binOp, Expression[] listOfParameters)
        {
            ExpressionTreeCallRewriter rewriter = new ExpressionTreeCallRewriter(typeManager, listOfParameters);

            // We should have a ExprBinOp that's an EK_SEQUENCE. The RHS of our sequence
            // should be a call to PM_EXPRESSION_LAMBDA. The LHS of our sequence is the 
            // set of declarations for the parameters that we'll need.
            // Assert all of these first, and then unwrap them.

            Debug.Assert(binOp != null);
            Debug.Assert(binOp.Kind == ExpressionKind.Sequence);
            Debug.Assert(binOp.OptionalRightChild is ExprCall);
            Debug.Assert(((ExprCall)binOp.OptionalRightChild).PredefinedMethod == PREDEFMETH.PM_EXPRESSION_LAMBDA);
            Debug.Assert(binOp.OptionalLeftChild != null);

            // Visit the left to generate the parameter construction.
            rewriter.Visit(binOp.OptionalLeftChild);
            ExprCall call = (ExprCall)binOp.OptionalRightChild;

            ExpressionExpr e = rewriter.Visit(call) as ExpressionExpr;
            return e.Expression;
        }

        /////////////////////////////////////////////////////////////////////////////////

        protected override Expr VisitSAVE(ExprBinOp pExpr)
        {
            // Saves should have a LHS that is a CALL to PM_EXPRESSION_PARAMETER
            // and a RHS that is a WRAP of that call.
            ExprCall call = (ExprCall)pExpr.OptionalLeftChild;
            Debug.Assert(call?.PredefinedMethod == PREDEFMETH.PM_EXPRESSION_PARAMETER);
            Debug.Assert(pExpr.OptionalRightChild is ExprWrap);

            Expression parameter = _ListOfParameters[_currentParameterIndex++];
            _DictionaryOfParameters.Add(call, parameter);

            return null;
        }

        /////////////////////////////////////////////////////////////////////////////////

        protected override Expr VisitCALL(ExprCall pExpr)
        {
            if (pExpr.PredefinedMethod == PREDEFMETH.PM_COUNT)
            {
                return pExpr;
            }

            Expression exp;
            switch (pExpr.PredefinedMethod)
            {
                case PREDEFMETH.PM_EXPRESSION_LAMBDA:
                    return GenerateLambda(pExpr);

                case PREDEFMETH.PM_EXPRESSION_CALL:
                    exp = GenerateCall(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
                case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
                    exp = GenerateArrayIndex(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_CONVERT:
                case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
                case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
                case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
                    exp = GenerateConvert(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_PROPERTY:
                    exp = GenerateProperty(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_FIELD:
                    exp = GenerateField(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_INVOKE:
                    exp = GenerateInvoke(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_NEW:
                    exp = GenerateNew(pExpr);
                    break;

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
                    exp = GenerateBinaryOperator(pExpr);
                    break;

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
                    exp = GenerateUserDefinedBinaryOperator(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_NEGATE:
                case PREDEFMETH.PM_EXPRESSION_NOT:
                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                    exp = GenerateUnaryOperator(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                    exp = GenerateUserDefinedUnaryOperator(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
                    exp = GenerateConstantType(pExpr);
                    break;

                case PREDEFMETH.PM_EXPRESSION_ASSIGN:
                    exp = GenerateAssignment(pExpr);
                    break;

                default:
                    Debug.Assert(false, "Invalid Predefined Method in VisitCALL");
                    throw Error.InternalCompilerError();
            }

            return new ExpressionExpr(exp);
        }

        // ExpressionTreeRewriter has optimized away identity or up-cast conversions, leaving us with a bare parameter
        // access. Just get the expression for that parameter so the lambda produced can be p0 => p0
        protected override Expr VisitWRAP(ExprWrap pExpr) => new ExpressionExpr(GetExpression(pExpr));

        #region Generators
        /////////////////////////////////////////////////////////////////////////////////

        private Expr GenerateLambda(ExprCall pExpr)
        {
            // We always call Lambda(body, arrayinit) where the arrayinit 
            // is the initialization of the parameters.
            return Visit(((ExprList)pExpr.OptionalArguments).OptionalElement);

            /*
             * // Do we need to do this?
            Expression e = (body as ExpressionExpr).Expression;
            if (e.Type.IsValueType)
            {
                // If we have a value type, convert it to object so that boxing
                // can happen.

                e = Expression.Convert(body.Expression, typeof(object));
            }
             * */
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateCall(ExprCall pExpr)
        {
            // Our arguments are: object, methodinfo, parameters.
            // The object is either an EXPRWRAP of a CALL, or a CALL that is a PM_CONVERT, whose
            // argument is the WRAP of a CALL. Deal with that first.

            ExprMethodInfo methinfo;
            ExprArrayInit arrinit;

            ExprList list = (ExprList)pExpr.OptionalArguments;
            if (list.OptionalNextListNode is ExprList next)
            {
                methinfo = (ExprMethodInfo)next.OptionalElement;
                arrinit = (ExprArrayInit)next.OptionalNextListNode;
            }
            else
            {
                methinfo = (ExprMethodInfo)list.OptionalNextListNode;
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
                obj = GetExpression(((ExprList)pExpr.OptionalArguments).OptionalElement);
            }

            return Expression.Call(obj, m, arguments);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateArrayIndex(ExprCall pExpr)
        {
            // We have two possibilities here - we're either a single index array, in which 
            // case we'll be PM_EXPRESSION_ARRAYINDEX, or we have multiple dimensions,
            // in which case we are PM_EXPRESSION_ARRAYINDEX2. 
            //
            // Our arguments then, are: object, index or object, indices.
            ExprList list = (ExprList)pExpr.OptionalArguments;
            Debug.Assert(list != null);
            Expression obj = GetExpression(list.OptionalElement);
            Expression[] indices;

            if (pExpr.PredefinedMethod == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX)
            {
                indices = new[] { GetExpression(list.OptionalNextListNode) };
            }
            else
            {
                Debug.Assert(pExpr.PredefinedMethod == PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2);
                indices = GetArgumentsFromArrayInit((ExprArrayInit)list.OptionalNextListNode);
            }
            return Expression.ArrayAccess(obj, indices);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateConvert(ExprCall pExpr)
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

                ExprList list = (ExprList)pExpr.OptionalArguments;
                ExprList list2 = (ExprList)list.OptionalNextListNode;
                e = GetExpression(list.OptionalElement);
                t = ((ExprTypeOf)list2.OptionalElement).SourceType.AssociatedSystemType;

                if (e.Type.MakeByRefType() == t)
                {
                    // We're trying to convert from a type to its by ref type. Don't do that.
                    return e;
                }
                Debug.Assert((pExpr.Flags & EXPRFLAG.EXF_UNBOXRUNTIME) == 0);

                MethodInfo m = GetMethodInfoFromExpr((ExprMethodInfo)list2.OptionalNextListNode);

                if (pm == PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED)
                {
                    return Expression.Convert(e, t, m);
                }
                return Expression.ConvertChecked(e, t, m);
            }
            else
            {
                Debug.Assert(pm == PREDEFMETH.PM_EXPRESSION_CONVERT ||
                    pm == PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED);

                // If we have a standard conversion, then we'll have some object as
                // the first list element (ie a WRAP or a CALL), and then a TYPEOF
                // as the second list element.
                ExprList list = (ExprList)pExpr.OptionalArguments;

                e = GetExpression(list.OptionalElement);
                t = ((ExprTypeOf)list.OptionalNextListNode).SourceType.AssociatedSystemType;

                if (e.Type.MakeByRefType() == t)
                {
                    // We're trying to convert from a type to its by ref type. Don't do that.
                    return e;
                }

                if ((pExpr.Flags & EXPRFLAG.EXF_UNBOXRUNTIME) != 0)
                {
                    // If we want to unbox this thing, return that instead of the convert.
                    return Expression.Unbox(e, t);
                }

                if (pm == PREDEFMETH.PM_EXPRESSION_CONVERT)
                {
                    return Expression.Convert(e, t);
                }
                return Expression.ConvertChecked(e, t);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateProperty(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;

            Expr instance = list.OptionalElement;
            Expr nextNode = list.OptionalNextListNode;
            ExprPropertyInfo propinfo;
            ExprArrayInit arguments;
            if (nextNode is ExprList nextList)
            {
                propinfo = nextList.OptionalElement as ExprPropertyInfo;
                arguments = nextList.OptionalNextListNode as ExprArrayInit;
            }
            else
            {
                propinfo = nextNode as ExprPropertyInfo;
                arguments = null;
            }

            PropertyInfo p = GetPropertyInfoFromExpr(propinfo);

            if (p == null)
            {
                Debug.Assert(false, "How did we get a prop that doesn't have a propinfo?");
                throw Error.InternalCompilerError();
            }

            if (arguments == null)
            {
                return Expression.Property(GetExpression(instance), p);
            }

            return Expression.Property(GetExpression(instance), p, GetArgumentsFromArrayInit(arguments));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateField(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;
            ExprFieldInfo fieldInfo = (ExprFieldInfo)list.OptionalNextListNode;
            Debug.Assert(fieldInfo != null);
            Type t = fieldInfo.FieldType.AssociatedSystemType;
            FieldInfo f = fieldInfo.Field.AssociatedFieldInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!t.IsGenericType && !t.IsNested)
            {
                t = f.DeclaringType;
            }

            // Now find the generic'ed one if we're generic.
            if (t.IsGenericType)
            {
                f = t.GetField(f.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            }

            return Expression.Field(GetExpression(list.OptionalElement), f);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateInvoke(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;

            return Expression.Invoke(
                GetExpression(list.OptionalElement),
                GetArgumentsFromArrayInit(list.OptionalNextListNode as ExprArrayInit));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateNew(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;

            var constructor = GetConstructorInfoFromExpr(list.OptionalElement as ExprMethodInfo);
            var arguments = GetArgumentsFromArrayInit(list.OptionalNextListNode as ExprArrayInit);
            return Expression.New(constructor, arguments);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateConstantType(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;

            return Expression.Constant(
                GetObject(list.OptionalElement),
                ((ExprTypeOf)list.OptionalNextListNode).SourceType.AssociatedSystemType);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateAssignment(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;

            return Expression.Assign(
                GetExpression(list.OptionalElement),
                GetExpression(list.OptionalNextListNode));
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateBinaryOperator(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;
            Debug.Assert(list != null);
            Expression arg1 = GetExpression(list.OptionalElement);
            Expression arg2 = GetExpression(list.OptionalNextListNode);

            switch (pExpr.PredefinedMethod)
            {
                case PREDEFMETH.PM_EXPRESSION_ADD:
                    return Expression.Add(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_AND:
                    return Expression.And(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_DIVIDE:
                    return Expression.Divide(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_EQUAL:
                    return Expression.Equal(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR:
                    return Expression.ExclusiveOr(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_GREATERTHAN:
                    return Expression.GreaterThan(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL:
                    return Expression.GreaterThanOrEqual(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT:
                    return Expression.LeftShift(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_LESSTHAN:
                    return Expression.LessThan(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL:
                    return Expression.LessThanOrEqual(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_MODULO:
                    return Expression.Modulo(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_MULTIPLY:
                    return Expression.Multiply(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_NOTEQUAL:
                    return Expression.NotEqual(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_OR:
                    return Expression.Or(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT:
                    return Expression.RightShift(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_SUBTRACT:
                    return Expression.Subtract(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_ORELSE:
                    return Expression.OrElse(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_ANDALSO:
                    return Expression.AndAlso(arg1, arg2);

                // Checked
                case PREDEFMETH.PM_EXPRESSION_ADDCHECKED:
                    return Expression.AddChecked(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED:
                    return Expression.MultiplyChecked(arg1, arg2);
                case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED:
                    return Expression.SubtractChecked(arg1, arg2);

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateBinaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateUserDefinedBinaryOperator(ExprCall pExpr)
        {
            ExprList list = (ExprList)pExpr.OptionalArguments;
            Expression arg1 = GetExpression(list.OptionalElement);
            Expression arg2 = GetExpression(((ExprList)list.OptionalNextListNode).OptionalElement);

            list = (ExprList)list.OptionalNextListNode;
            MethodInfo methodInfo;
            bool bIsLifted = false;
            if (list.OptionalNextListNode is ExprList next)
            {
                ExprConstant isLifted = (ExprConstant)next.OptionalElement;
                Debug.Assert(isLifted != null);
                bIsLifted = isLifted.Val.Int32Val == 1;
                methodInfo = GetMethodInfoFromExpr((ExprMethodInfo)next.OptionalNextListNode);
            }
            else
            {
                methodInfo = GetMethodInfoFromExpr((ExprMethodInfo)list.OptionalNextListNode);
            }

            switch (pExpr.PredefinedMethod)
            {
                case PREDEFMETH.PM_EXPRESSION_ADD_USER_DEFINED:
                    return Expression.Add(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_AND_USER_DEFINED:
                    return Expression.And(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_DIVIDE_USER_DEFINED:
                    return Expression.Divide(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_EQUAL_USER_DEFINED:
                    return Expression.Equal(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_EXCLUSIVEOR_USER_DEFINED:
                    return Expression.ExclusiveOr(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_GREATERTHAN_USER_DEFINED:
                    return Expression.GreaterThan(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_GREATERTHANOREQUAL_USER_DEFINED:
                    return Expression.GreaterThanOrEqual(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_LEFTSHIFT_USER_DEFINED:
                    return Expression.LeftShift(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_LESSTHAN_USER_DEFINED:
                    return Expression.LessThan(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_LESSTHANOREQUAL_USER_DEFINED:
                    return Expression.LessThanOrEqual(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_MODULO_USER_DEFINED:
                    return Expression.Modulo(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_MULTIPLY_USER_DEFINED:
                    return Expression.Multiply(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_NOTEQUAL_USER_DEFINED:
                    return Expression.NotEqual(arg1, arg2, bIsLifted, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_OR_USER_DEFINED:
                    return Expression.Or(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_RIGHTSHIFT_USER_DEFINED:
                    return Expression.RightShift(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_SUBTRACT_USER_DEFINED:
                    return Expression.Subtract(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_ORELSE_USER_DEFINED:
                    return Expression.OrElse(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_ANDALSO_USER_DEFINED:
                    return Expression.AndAlso(arg1, arg2, methodInfo);

                // Checked
                case PREDEFMETH.PM_EXPRESSION_ADDCHECKED_USER_DEFINED:
                    return Expression.AddChecked(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_MULTIPLYCHECKED_USER_DEFINED:
                    return Expression.MultiplyChecked(arg1, arg2, methodInfo);
                case PREDEFMETH.PM_EXPRESSION_SUBTRACTCHECKED_USER_DEFINED:
                    return Expression.SubtractChecked(arg1, arg2, methodInfo);

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUserDefinedBinaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateUnaryOperator(ExprCall pExpr)
        {
            PREDEFMETH pm = pExpr.PredefinedMethod;
            Expression arg = GetExpression(pExpr.OptionalArguments);

            switch (pm)
            {
                case PREDEFMETH.PM_EXPRESSION_NOT:
                    return Expression.Not(arg);

                case PREDEFMETH.PM_EXPRESSION_NEGATE:
                    return Expression.Negate(arg);

                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                    return Expression.NegateChecked(arg);

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUnaryOperator");
                    throw Error.InternalCompilerError();
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression GenerateUserDefinedUnaryOperator(ExprCall pExpr)
        {
            PREDEFMETH pm = pExpr.PredefinedMethod;
            ExprList list = (ExprList)pExpr.OptionalArguments;
            Expression arg = GetExpression(list.OptionalElement);
            MethodInfo methodInfo = GetMethodInfoFromExpr((ExprMethodInfo)list.OptionalNextListNode);

            switch (pm)
            {
                case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                    return Expression.Not(arg, methodInfo);

                case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                    return Expression.Negate(arg, methodInfo);

                case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                    return Expression.UnaryPlus(arg, methodInfo);

                case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                    return Expression.NegateChecked(arg, methodInfo);

                default:
                    Debug.Assert(false, "Invalid Predefined Method in GenerateUserDefinedUnaryOperator");
                    throw Error.InternalCompilerError();
            }
        }
        #endregion

        #region Helpers
        /////////////////////////////////////////////////////////////////////////////////

        private Expression GetExpression(Expr pExpr)
        {
            if (pExpr is ExprWrap wrap)
            {
                return _DictionaryOfParameters[(ExprCall)wrap.OptionalExpression];
            }
            else if (pExpr is ExprConstant)
            {
                Debug.Assert(pExpr.Type is NullType);
                return null;
            }
            else
            {
                // We can have a convert node or a call of a user defined conversion.
                ExprCall call = (ExprCall)pExpr;
                Debug.Assert(call != null);
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
                        return GenerateCall(call);

                    case PREDEFMETH.PM_EXPRESSION_CONVERT:
                    case PREDEFMETH.PM_EXPRESSION_CONVERT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED:
                    case PREDEFMETH.PM_EXPRESSION_CONVERTCHECKED_USER_DEFINED:
                        return GenerateConvert(call);

                    case PREDEFMETH.PM_EXPRESSION_NEWARRAYINIT:
                    {
                        ExprList list = (ExprList)call.OptionalArguments;
                        return
                            Expression.NewArrayInit(
                                ((ExprTypeOf)list.OptionalElement).SourceType.AssociatedSystemType,
                                GetArgumentsFromArrayInit((ExprArrayInit)list.OptionalNextListNode));
                    }

                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX:
                    case PREDEFMETH.PM_EXPRESSION_ARRAYINDEX2:
                        return GenerateArrayIndex(call);

                    case PREDEFMETH.PM_EXPRESSION_NEW:
                        return GenerateNew(call);

                    case PREDEFMETH.PM_EXPRESSION_PROPERTY:
                        return GenerateProperty(call);

                    case PREDEFMETH.PM_EXPRESSION_FIELD:
                        return GenerateField(call);

                    case PREDEFMETH.PM_EXPRESSION_CONSTANT_OBJECT_TYPE:
                        return GenerateConstantType(call);

                    case PREDEFMETH.PM_EXPRESSION_ASSIGN:
                        return GenerateAssignment(call);

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
                        return GenerateBinaryOperator(call);

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
                        return GenerateUserDefinedBinaryOperator(call);

                    case PREDEFMETH.PM_EXPRESSION_NOT:
                    case PREDEFMETH.PM_EXPRESSION_NEGATE:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED:
                        return GenerateUnaryOperator(call);

                    case PREDEFMETH.PM_EXPRESSION_NOT_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATE_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_UNARYPLUS_USER_DEFINED:
                    case PREDEFMETH.PM_EXPRESSION_NEGATECHECKED_USER_DEFINED:
                        return GenerateUserDefinedUnaryOperator(call);

                    default:
                        Debug.Assert(false, "Invalid Predefined Method in GetExpression");
                        throw Error.InternalCompilerError();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private object GetObject(Expr pExpr)
        {
            for (;;)
            {
                if (pExpr is ExprCast cast)
                {
                    pExpr = cast.Argument;
                }
                else if (pExpr is ExprTypeOf typeOf)
                {
                    return typeOf.SourceType.AssociatedSystemType;
                }
                else if (pExpr is ExprMethodInfo methodInfo)
                {
                    return GetMethodInfoFromExpr(methodInfo);
                }
                else if (pExpr is ExprConstant constant)
                {
                    ConstVal val = constant.Val;
                    CType underlyingType = pExpr.Type;
                    object objval;

                    if (pExpr.Type is NullType)
                    {
                        return null;
                    }

                    if (pExpr.Type.isEnumType())
                    {
                        underlyingType = underlyingType.getAggregate().GetUnderlyingType();
                    }

                    switch (Type.GetTypeCode(underlyingType.AssociatedSystemType))
                    {
                        case TypeCode.Boolean:
                            objval = val.BooleanVal;
                            break;
                        case TypeCode.SByte:
                            objval = val.SByteVal;
                            break;
                        case TypeCode.Byte:
                            objval = val.ByteVal;
                            break;
                        case TypeCode.Int16:
                            objval = val.Int16Val;
                            break;
                        case TypeCode.UInt16:
                            objval = val.UInt16Val;
                            break;
                        case TypeCode.Int32:
                            objval = val.Int32Val;
                            break;
                        case TypeCode.UInt32:
                            objval = val.UInt32Val;
                            break;
                        case TypeCode.Int64:
                            objval = val.Int64Val;
                            break;
                        case TypeCode.UInt64:
                            objval = val.UInt64Val;
                            break;
                        case TypeCode.Single:
                            objval = val.SingleVal;
                            break;
                        case TypeCode.Double:
                            objval = val.DoubleVal;
                            break;
                        case TypeCode.Decimal:
                            objval = val.DecimalVal;
                            break;
                        case TypeCode.Char:
                            objval = val.CharVal;
                            break;
                        case TypeCode.String:
                            objval = val.StringVal;
                            break;
                        default:
                            objval = val.ObjectVal;
                            break;
                    }

                    return pExpr.Type.isEnumType() ? Enum.ToObject(pExpr.Type.AssociatedSystemType, objval) : objval;
                }
                else if (pExpr is ExprZeroInit zeroInit)
                {
                    return Activator.CreateInstance(zeroInit.Type.AssociatedSystemType);
                }
                else
                {
                    Debug.Assert(false, "Invalid Expr in GetObject");
                    throw Error.InternalCompilerError();
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private Expression[] GetArgumentsFromArrayInit(ExprArrayInit arrinit)
        {
            List<Expression> expressions = new List<Expression>();

            if (arrinit != null)
            {
                Expr list = arrinit.OptionalArguments;
                while (list != null)
                {
                    Expr p;
                    if (list is ExprList pList)
                    {
                        p = pList.OptionalElement;
                        list = pList.OptionalNextListNode;
                    }
                    else
                    {
                        p = list;
                        list = null;
                    }

                    expressions.Add(GetExpression(p));
                }

                Debug.Assert(expressions.Count == arrinit.DimensionSizes[0]);
            }

            return expressions.ToArray();
        }

        /////////////////////////////////////////////////////////////////////////////////

        private MethodInfo GetMethodInfoFromExpr(ExprMethodInfo methinfo)
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
            if (!type.IsGenericType && !type.IsNested)
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
                    (m.GetParameters().Length == genericParams.Count) &&
                    (TypesAreEqual(m.ReturnType, genericReturn.AssociatedSystemType)));

                bool bMatch = true;
                ParameterInfo[] parameters = m.GetParameters();
                for (int i = 0; i < genericParams.Count; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericParams[i].AssociatedSystemType))
                    {
                        bMatch = false;
                        break;
                    }
                }
                if (bMatch)
                {
                    if (m.IsGenericMethod)
                    {
                        int size = methinfo.Method.TypeArgs?.Count ?? 0;
                        Type[] typeArgs = new Type[size];
                        if (size > 0)
                        {
                            for (int i = 0; i < methinfo.Method.TypeArgs.Count; i++)
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

        private ConstructorInfo GetConstructorInfoFromExpr(ExprMethodInfo methinfo)
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
            if (!type.IsGenericType && !type.IsNested)
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
                Debug.Assert(c.GetParameters() == null || c.GetParameters().Length == genericInstanceParams.Count);

                bool bMatch = true;
                ParameterInfo[] parameters = c.GetParameters();
                for (int i = 0; i < genericInstanceParams.Count; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericInstanceParams[i].AssociatedSystemType))
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

        private PropertyInfo GetPropertyInfoFromExpr(ExprPropertyInfo propinfo)
        {
            // To do this, we need to construct a type array of the parameter types,
            // get the parent constructed type, and get the property from it.

            AggregateType aggType = propinfo.Property.Ats;
            PropertySymbol propSym = propinfo.Property.Prop();

            TypeArray genericInstanceParams = _typeManager.SubstTypeArray(propSym.Params, aggType, null);

            Type type = aggType.AssociatedSystemType;
            PropertyInfo propertyInfo = propSym.AssociatedPropertyInfo;

            // This is to ensure that for embedded nopia types, we have the
            // appropriate local type from the member itself; this is possible
            // because nopia types are not generic or nested.
            if (!type.IsGenericType && !type.IsNested)
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
                    (p.GetIndexParameters() == null || p.GetIndexParameters().Length == genericInstanceParams.Count));

                bool bMatch = true;
                ParameterInfo[] parameters = p.GetSetMethod(true) != null ?
                    p.GetSetMethod(true).GetParameters() : p.GetGetMethod(true).GetParameters();
                for (int i = 0; i < genericInstanceParams.Count; i++)
                {
                    if (!TypesAreEqual(parameters[i].ParameterType, genericInstanceParams[i].AssociatedSystemType))
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
