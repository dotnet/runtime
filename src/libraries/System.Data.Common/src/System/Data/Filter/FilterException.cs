// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Data
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidExpressionException : DataException
    {
        protected InvalidExpressionException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        public InvalidExpressionException() : base() { }
        public InvalidExpressionException(string? s) : base(s) { }

        public InvalidExpressionException(string? message, Exception? innerException) : base(message, innerException) { }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class EvaluateException : InvalidExpressionException
    {
        protected EvaluateException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        public EvaluateException() : base() { }
        public EvaluateException(string? s) : base(s) { }

        public EvaluateException(string? message, Exception? innerException) : base(message, innerException) { }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SyntaxErrorException : InvalidExpressionException
    {
        protected SyntaxErrorException(SerializationInfo info, StreamingContext context)
        : base(info, context)
        {
        }

        public SyntaxErrorException() : base() { }
        public SyntaxErrorException(string? s) : base(s) { }

        public SyntaxErrorException(string? message, Exception? innerException) : base(message, innerException) { }
    }

    internal static class ExprException
    {
        private static OverflowException _Overflow(string error)
        {
            OverflowException e = new OverflowException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(e);
            return e;
        }
        private static InvalidExpressionException _Expr(string error)
        {
            InvalidExpressionException e = new InvalidExpressionException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(e);
            return e;
        }
        private static SyntaxErrorException _Syntax(string error)
        {
            SyntaxErrorException e = new SyntaxErrorException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(e);
            return e;
        }
        private static EvaluateException _Eval(string error)
        {
            EvaluateException e = new EvaluateException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(e);
            return e;
        }
        private static EvaluateException _Eval(string error, Exception? innerException)
        {
            EvaluateException e = new EvaluateException(error/*, innerException*/);
            ExceptionBuilder.TraceExceptionAsReturnValue(e);
            return e;
        }

        public static Exception InvokeArgument()
        {
            return ExceptionBuilder._Argument(SR.Expr_InvokeArgument);
        }

        public static Exception NYI(string moreinfo)
        {
            string err = SR.Format(SR.Expr_NYI, moreinfo);
            Debug.Fail(err);
            return _Expr(err);
        }

        public static Exception MissingOperand(OperatorInfo before)
        {
            return _Syntax(SR.Format(SR.Expr_MissingOperand, Operators.ToString(before._op)));
        }

        public static Exception MissingOperator(string token)
        {
            return _Syntax(SR.Format(SR.Expr_MissingOperand, token));
        }

        public static Exception TypeMismatch(string expr)
        {
            return _Eval(SR.Format(SR.Expr_TypeMismatch, expr));
        }

        public static Exception FunctionArgumentOutOfRange(string arg, string func)
        {
            return ExceptionBuilder._ArgumentOutOfRange(arg, SR.Format(SR.Expr_ArgumentOutofRange, func));
        }

        public static Exception ExpressionTooComplex()
        {
            return _Eval(SR.Expr_ExpressionTooComplex);
        }

        public static Exception UnboundName(string name)
        {
            return _Eval(SR.Format(SR.Expr_UnboundName, name));
        }

        public static Exception InvalidString(string str)
        {
            return _Syntax(SR.Format(SR.Expr_InvalidString, str));
        }

        public static Exception UndefinedFunction(string name)
        {
            return _Eval(SR.Format(SR.Expr_UndefinedFunction, name));
        }

        public static Exception SyntaxError()
        {
            return _Syntax(SR.Expr_Syntax);
        }

        public static Exception FunctionArgumentCount(string name)
        {
            return _Eval(SR.Format(SR.Expr_FunctionArgumentCount, name));
        }

        public static Exception MissingRightParen()
        {
            return _Syntax(SR.Expr_MissingRightParen);
        }

        public static Exception UnknownToken(string token, int position)
        {
            return _Syntax(SR.Format(SR.Expr_UnknownToken, token, position.ToString(CultureInfo.InvariantCulture)));
        }

        public static Exception UnknownToken(Tokens tokExpected, Tokens tokCurr, int position)
        {
            return _Syntax(SR.Format(SR.Expr_UnknownToken1, tokExpected.ToString(), tokCurr.ToString(), position.ToString(CultureInfo.InvariantCulture)));
        }

        public static Exception DatatypeConvertion(Type type1, Type type2)
        {
            return _Eval(SR.Format(SR.Expr_DatatypeConvertion, type1.ToString(), type2.ToString()));
        }

        public static Exception DatavalueConvertion(object value, Type type, Exception? innerException)
        {
            return _Eval(SR.Format(SR.Expr_DatavalueConvertion, value.ToString(), type.ToString()), innerException);
        }

        public static Exception InvalidName(string name)
        {
            return _Syntax(SR.Format(SR.Expr_InvalidName, name));
        }

        public static Exception InvalidDate(string date)
        {
            return _Syntax(SR.Format(SR.Expr_InvalidDate, date));
        }

        public static Exception NonConstantArgument()
        {
            return _Eval(SR.Expr_NonConstantArgument);
        }

        public static Exception InvalidPattern(string pat)
        {
            return _Eval(SR.Format(SR.Expr_InvalidPattern, pat));
        }

        public static Exception InWithoutParentheses()
        {
            return _Syntax(SR.Expr_InWithoutParentheses);
        }

        public static Exception InWithoutList()
        {
            return _Syntax(SR.Expr_InWithoutList);
        }

        public static Exception InvalidIsSyntax()
        {
            return _Syntax(SR.Expr_IsSyntax);
        }

        public static Exception Overflow(Type type)
        {
            return _Overflow(SR.Format(SR.Expr_Overflow, type.Name));
        }

        public static Exception ArgumentType(string function, int arg, Type type)
        {
            return _Eval(SR.Format(SR.Expr_ArgumentType, function, arg.ToString(CultureInfo.InvariantCulture), type));
        }

        public static Exception ArgumentTypeInteger(string function, int arg)
        {
            return _Eval(SR.Format(SR.Expr_ArgumentTypeInteger, function, arg.ToString(CultureInfo.InvariantCulture)));
        }

        public static Exception TypeMismatchInBinop(int op, Type type1, Type type2)
        {
            return _Eval(SR.Format(SR.Expr_TypeMismatchInBinop, Operators.ToString(op), type1, type2));
        }

        public static Exception AmbiguousBinop(int op, Type type1, Type type2)
        {
            return _Eval(SR.Format(SR.Expr_AmbiguousBinop, Operators.ToString(op), type1, type2));
        }

        public static Exception UnsupportedOperator(int op)
        {
            return _Eval(SR.Format(SR.Expr_UnsupportedOperator, Operators.ToString(op)));
        }

        public static Exception InvalidNameBracketing(string name)
        {
            return _Syntax(SR.Format(SR.Expr_InvalidNameBracketing, name));
        }

        public static Exception MissingOperandBefore(string op)
        {
            return _Syntax(SR.Format(SR.Expr_MissingOperandBefore, op));
        }

        public static Exception TooManyRightParentheses()
        {
            return _Syntax(SR.Expr_TooManyRightParentheses);
        }

        public static Exception UnresolvedRelation(string name, string expr)
        {
            return _Eval(SR.Format(SR.Expr_UnresolvedRelation, name, expr));
        }

        internal static EvaluateException BindFailure(string relationName)
        {
            return _Eval(SR.Format(SR.Expr_BindFailure, relationName));
        }

        public static Exception AggregateArgument()
        {
            return _Syntax(SR.Expr_AggregateArgument);
        }

        public static Exception AggregateUnbound(string expr)
        {
            return _Eval(SR.Format(SR.Expr_AggregateUnbound, expr));
        }

        public static Exception EvalNoContext()
        {
            return _Eval(SR.Expr_EvalNoContext);
        }

        public static Exception ExpressionUnbound(string expr)
        {
            return _Eval(SR.Format(SR.Expr_ExpressionUnbound, expr));
        }

        public static Exception ComputeNotAggregate(string expr)
        {
            return _Eval(SR.Format(SR.Expr_ComputeNotAggregate, expr));
        }

        public static Exception FilterConvertion(string expr)
        {
            return _Eval(SR.Format(SR.Expr_FilterConvertion, expr));
        }

        public static Exception LookupArgument()
        {
            return _Syntax(SR.Expr_LookupArgument);
        }

        public static Exception InvalidType(string typeName)
        {
            return _Eval(SR.Format(SR.Expr_InvalidType, typeName));
        }

        public static Exception InvalidHoursArgument()
        {
            return _Eval(SR.Expr_InvalidHoursArgument);
        }

        public static Exception InvalidMinutesArgument()
        {
            return _Eval(SR.Expr_InvalidMinutesArgument);
        }

        public static Exception InvalidTimeZoneRange()
        {
            return _Eval(SR.Expr_InvalidTimeZoneRange);
        }

        public static Exception MismatchKindandTimeSpan()
        {
            return _Eval(SR.Expr_MismatchKindandTimeSpan);
        }

        public static Exception UnsupportedDataType(Type type)
        {
            return ExceptionBuilder._Argument(SR.Format(SR.Expr_UnsupportedType, type.FullName));
        }
    }
}
