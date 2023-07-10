// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Numerics.Hashing;
using Microsoft.CSharp.RuntimeBinder.Errors;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class BinderHelper
    {
        private static MethodInfo s_DoubleIsNaN;
        private static MethodInfo s_SingleIsNaN;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal static DynamicMetaObject Bind(
                ICSharpBinder action,
                RuntimeBinder binder,
                DynamicMetaObject[] args,
                IEnumerable<CSharpArgumentInfo> arginfos,
                DynamicMetaObject onBindingError)
        {
            Expression[] parameters = new Expression[args.Length];
            BindingRestrictions restrictions = BindingRestrictions.Empty;
            ICSharpInvokeOrInvokeMemberBinder callPayload = action as ICSharpInvokeOrInvokeMemberBinder;
            ParameterExpression tempForIncrement = null;
            IEnumerator<CSharpArgumentInfo> arginfosEnum = (arginfos ?? Array.Empty<CSharpArgumentInfo>()).GetEnumerator();

            for (int index = 0; index < args.Length; ++index)
            {
                DynamicMetaObject o = args[index];
                // Our contract with the DLR is such that we will not enter a bind unless we have
                // values for the meta-objects involved.

                Debug.Assert(o.HasValue);

                CSharpArgumentInfo info = arginfosEnum.MoveNext() ? arginfosEnum.Current : null;

                if (index == 0 && IsIncrementOrDecrementActionOnLocal(action))
                {
                    // We have an inc or a dec operation. Insert the temp local instead.
                    //
                    // We need to do this because for value types, the object will come
                    // in boxed, and we'd need to unbox it to get the original type in order
                    // to increment. The only way to do that is to create a new temporary.
                    object value = o.Value;
                    tempForIncrement = Expression.Variable(value != null ? value.GetType() : typeof(object), "t0");
                    parameters[0] = tempForIncrement;
                }
                else
                {
                    parameters[index] = o.Expression;
                }

                BindingRestrictions r = DeduceArgumentRestriction(index, callPayload, o, info);
                restrictions = restrictions.Merge(r);

                // Here we check the argument info. If the argument info shows that the current argument
                // is a literal constant, then we also add an instance restriction on the value of
                // the constant.
                if (info != null && info.LiteralConstant)
                {
                    if (o.Value is double && double.IsNaN((double)o.Value))
                    {
                        MethodInfo isNaN = s_DoubleIsNaN ??= typeof(double).GetMethod("IsNaN");
                        Expression e = Expression.Call(null, isNaN, o.Expression);
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(e));
                    }
                    else if (o.Value is float && float.IsNaN((float)o.Value))
                    {
                        MethodInfo isNaN = s_SingleIsNaN ??= typeof(float).GetMethod("IsNaN");
                        Expression e = Expression.Call(null, isNaN, o.Expression);
                        restrictions = restrictions.Merge(BindingRestrictions.GetExpressionRestriction(e));
                    }
                    else
                    {
                        Expression e = Expression.Equal(o.Expression, Expression.Constant(o.Value, o.Expression.Type));
                        r = BindingRestrictions.GetExpressionRestriction(e);
                        restrictions = restrictions.Merge(r);
                    }
                }
            }

            // Get the bound expression.
            try
            {
                Expression expression = binder.Bind(action, parameters, args, out DynamicMetaObject deferredBinding);

                if (deferredBinding != null)
                {
                    expression = ConvertResult(deferredBinding.Expression, action);
                    restrictions = deferredBinding.Restrictions.Merge(restrictions);
                    return new DynamicMetaObject(expression, restrictions);
                }

                if (tempForIncrement != null)
                {
                    // If we have a ++ or -- payload, we need to do some temp rewriting.
                    // We rewrite to the following:
                    //
                    // temp = (type)o;
                    // temp++;
                    // o = temp;
                    // return o;

                    DynamicMetaObject arg0 = args[0];

                    expression = Expression.Block(
                        new[] { tempForIncrement },
                        Expression.Assign(tempForIncrement, Expression.Convert(arg0.Expression, arg0.Value.GetType())),
                        expression,
                        Expression.Assign(arg0.Expression, Expression.Convert(tempForIncrement, arg0.Expression.Type)));
                }

                expression = ConvertResult(expression, action);

                return new DynamicMetaObject(expression, restrictions);
            }
            catch (RuntimeBinderException e)
            {
                if (onBindingError != null)
                {
                    return onBindingError;
                }

                return new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(
                            typeof(RuntimeBinderException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(e.Message)
                        ),
                        GetTypeForErrorMetaObject(action, args)
                    ),
                    restrictions
                );
            }
        }

        public static void ValidateBindArgument(DynamicMetaObject argument, string paramName)
        {
            ArgumentNullException.ThrowIfNull(argument, paramName);
            if (!argument.HasValue)
            {
                throw Error.DynamicArgumentNeedsValue(paramName);
            }
        }

        public static void ValidateBindArgument(DynamicMetaObject[] arguments, string paramName)
        {
            if (arguments != null) // null is treated as empty, so not invalid
            {
                for (int i = 0; i < arguments.Length; ++i)
                {
                    ValidateBindArgument(arguments[i], $"{paramName}[{i}]");
                }
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsTypeOfStaticCall(
            int parameterIndex,
            ICSharpInvokeOrInvokeMemberBinder callPayload)
        {
            return parameterIndex == 0 && callPayload != null && callPayload.StaticCall;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsComObject(object obj)
        {
            return obj != null && Marshal.IsComObject(obj);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsDynamicallyTypedRuntimeProxy(DynamicMetaObject argument, CSharpArgumentInfo info)
        {
            // This detects situations where, although the argument has a value with
            // a given type, that type is insufficient to determine, statically, the
            // set of reference conversions that are going to exist at bind time for
            // different values. For instance, one __ComObject may allow a conversion
            // to IFoo while another does not.

            bool isDynamicObject =
                info != null &&
                !info.UseCompileTimeType &&
                IsComObject(argument.Value);

            return isDynamicObject;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static BindingRestrictions DeduceArgumentRestriction(
            int parameterIndex,
            ICSharpInvokeOrInvokeMemberBinder callPayload,
            DynamicMetaObject argument,
            CSharpArgumentInfo info)
        {
            // Here we deduce what predicates the DLR can apply to future calls in order to
            // determine whether to use the previously-computed-and-cached delegate, or
            // whether we need to bind the site again. Ideally we would like the
            // predicate to be as broad as is possible; if we can re-use analysis based
            // solely on the type of the argument, that is preferable to re-using analysis
            // based on object identity with a previously-analyzed argument.

            // The times when we need to restrict re-use to a particular instance, rather
            // than its type, are:
            //
            // * if the argument is a null reference then we have no type information.
            //
            // * if we are making a static call then the first argument is
            //   going to be a Type object. In this scenario we should always check
            //   for a specific Type object rather than restricting to the Type type.
            //
            // * if the argument was dynamic at compile time and it is a dynamic proxy
            //   object that the runtime manages, such as COM RCWs and transparent
            //   proxies.
            //
            // ** there is also a case for constant values (such as literals) to use
            //    something like value restrictions, and that is accomplished in Bind().

            bool useValueRestriction =
                argument.Value == null ||
                IsTypeOfStaticCall(parameterIndex, callPayload) ||
                IsDynamicallyTypedRuntimeProxy(argument, info);

            return useValueRestriction ?
                    BindingRestrictions.GetInstanceRestriction(argument.Expression, argument.Value) :
                    BindingRestrictions.GetTypeRestriction(argument.Expression, argument.RuntimeType);
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static Expression ConvertResult(Expression binding, ICSharpBinder action)
        {
            // Need to handle the following cases:
            //   (1) Call to a constructor: no conversions.
            //   (2) Call to a void-returning method: return null iff result is discarded.
            //   (3) Call to a value-type returning method: box to object.
            //
            // In all other cases, binding.Type should be equivalent or
            // reference assignable to resultType.

            // No conversions needed for , the call site has the correct type.
            if (action is CSharpInvokeConstructorBinder)
            {
                return binding;
            }

            if (binding.Type == typeof(void))
            {
                if (action is ICSharpInvokeOrInvokeMemberBinder invoke && invoke.ResultDiscarded)
                {
                    Debug.Assert(action.ReturnType == typeof(object));
                    return Expression.Block(binding, Expression.Default(action.ReturnType));
                }

                throw Error.BindToVoidMethodButExpectResult();
            }

            if (binding.Type.IsValueType && !action.ReturnType.IsValueType)
            {
                Debug.Assert(action.ReturnType == typeof(object));
                return Expression.Convert(binding, action.ReturnType);
            }

            return binding;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static Type GetTypeForErrorMetaObject(ICSharpBinder action, DynamicMetaObject[] args)
        {
            // This is similar to ConvertResult but has fewer things to worry about.

            if (action is CSharpInvokeConstructorBinder)
            {
                Debug.Assert(args != null);
                Debug.Assert(args.Length != 0);
                Debug.Assert(args[0].Value is Type);
                return args[0].Value as Type;
            }

            return action.ReturnType;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsIncrementOrDecrementActionOnLocal(ICSharpBinder action) =>
            action is CSharpUnaryOperationBinder operatorPayload
            && (operatorPayload.Operation == ExpressionType.Increment || operatorPayload.Operation == ExpressionType.Decrement);

        /////////////////////////////////////////////////////////////////////////////////

        internal static T[] Cons<T>(T sourceHead, T[] sourceTail)
        {
            if (sourceTail?.Length != 0)
            {
                T[] array = new T[sourceTail.Length + 1];
                array[0] = sourceHead;
                sourceTail.CopyTo(array, 1);
                return array;
            }

            return new[] { sourceHead };
        }

        internal static T[] Cons<T>(T sourceHead, T[] sourceMiddle, T sourceLast)
        {
            if (sourceMiddle?.Length != 0)
            {
                T[] array = new T[sourceMiddle.Length + 2];
                array[0] = sourceHead;
                array[array.Length - 1] = sourceLast;
                sourceMiddle.CopyTo(array, 1);
                return array;
            }

            return new[] { sourceHead, sourceLast };
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static T[] ToArray<T>(IEnumerable<T> source) => source == null ? Array.Empty<T>() : source.ToArray();

        /////////////////////////////////////////////////////////////////////////////////

        internal static CallInfo CreateCallInfo(ref IEnumerable<CSharpArgumentInfo> argInfos, int discard)
        {
            // This function converts the C# Binder's notion of argument information to the
            // DLR's notion. The DLR counts arguments differently than C#. Here are some
            // examples:

            // Expression      Binder                     C# ArgInfos   DLR CallInfo
            //
            // d.M(1, 2, 3);   CSharpInvokeMemberBinder   4             3
            // d(1, 2, 3);     CSharpInvokeBinder         4             3
            // d[1, 2] = 3;    CSharpSetIndexBinder       4             2
            // d[1, 2, 3]      CSharpGetIndexBinder       4             3
            //
            // The "discard" parameter tells this function how many of the C# arg infos it
            // should not count as DLR arguments.

            int argCount = 0;
            List<string> argNames = new List<string>();
            CSharpArgumentInfo[] infoArray = ToArray(argInfos);
            argInfos = infoArray; // Write back the array to allow single enumeration.
            foreach (CSharpArgumentInfo info in infoArray)
            {
                if (info.NamedArgument)
                {
                    argNames.Add(info.Name);
                }
                ++argCount;
            }

            Debug.Assert(discard <= argCount);
            Debug.Assert(argNames.Count <= argCount - discard);

            return new CallInfo(argCount - discard, argNames);
        }

        internal static string GetCLROperatorName(this ExpressionType p) =>
            p switch
            {

                // Binary Operators
                ExpressionType.Add => SpecialNames.CLR_Add,
                ExpressionType.Subtract => SpecialNames.CLR_Subtract,
                ExpressionType.Multiply => SpecialNames.CLR_Multiply,
                ExpressionType.Divide => SpecialNames.CLR_Division,
                ExpressionType.Modulo => SpecialNames.CLR_Modulus,
                ExpressionType.LeftShift => SpecialNames.CLR_LShift,
                ExpressionType.RightShift => SpecialNames.CLR_RShift,
                ExpressionType.LessThan => SpecialNames.CLR_LT,
                ExpressionType.GreaterThan => SpecialNames.CLR_GT,
                ExpressionType.LessThanOrEqual => SpecialNames.CLR_LTE,
                ExpressionType.GreaterThanOrEqual => SpecialNames.CLR_GTE,
                ExpressionType.Equal => SpecialNames.CLR_Equality,
                ExpressionType.NotEqual => SpecialNames.CLR_Inequality,
                ExpressionType.And => SpecialNames.CLR_BitwiseAnd,
                ExpressionType.ExclusiveOr => SpecialNames.CLR_ExclusiveOr,
                ExpressionType.Or => SpecialNames.CLR_BitwiseOr,

                // "op_LogicalNot";
                ExpressionType.AddAssign => SpecialNames.CLR_InPlaceAdd,
                ExpressionType.SubtractAssign => SpecialNames.CLR_InPlaceSubtract,
                ExpressionType.MultiplyAssign => SpecialNames.CLR_InPlaceMultiply,
                ExpressionType.DivideAssign => SpecialNames.CLR_InPlaceDivide,
                ExpressionType.ModuloAssign => SpecialNames.CLR_InPlaceModulus,
                ExpressionType.AndAssign => SpecialNames.CLR_InPlaceBitwiseAnd,
                ExpressionType.ExclusiveOrAssign => SpecialNames.CLR_InPlaceExclusiveOr,
                ExpressionType.OrAssign => SpecialNames.CLR_InPlaceBitwiseOr,
                ExpressionType.LeftShiftAssign => SpecialNames.CLR_InPlaceLShift,
                ExpressionType.RightShiftAssign => SpecialNames.CLR_InPlaceRShift,

                // Unary Operators
                ExpressionType.Negate => SpecialNames.CLR_UnaryNegation,
                ExpressionType.UnaryPlus => SpecialNames.CLR_UnaryPlus,
                ExpressionType.Not => SpecialNames.CLR_LogicalNot,
                ExpressionType.OnesComplement => SpecialNames.CLR_OnesComplement,
                ExpressionType.IsTrue => SpecialNames.CLR_True,
                ExpressionType.IsFalse => SpecialNames.CLR_False,

                ExpressionType.Increment => SpecialNames.CLR_Increment,
                ExpressionType.Decrement => SpecialNames.CLR_Decrement,

                _ => null,
            };

        internal static int AddArgHashes(int hash, Type[] typeArguments, CSharpArgumentInfo[] argInfos)
        {
            foreach (var typeArg in typeArguments)
            {
                hash = HashHelpers.Combine(hash, typeArg.GetHashCode());
            }

            return AddArgHashes(hash, argInfos);
        }

        internal static int AddArgHashes(int hash, CSharpArgumentInfo[] argInfos)
        {
            foreach (var argInfo in argInfos)
            {
                hash = HashHelpers.Combine(hash, (int)argInfo.Flags);
                var argName = argInfo.Name;
                if (!string.IsNullOrEmpty(argName))
                {
                    hash = HashHelpers.Combine(hash, argName.GetHashCode());
                }
            }

            return hash;
        }

        internal static bool CompareArgInfos(Type[] typeArgs, Type[] otherTypeArgs, CSharpArgumentInfo[] argInfos, CSharpArgumentInfo[] otherArgInfos)
        {
            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (typeArgs[i] != otherTypeArgs[i])
                {
                    return false;
                }
            }

            return CompareArgInfos(argInfos, otherArgInfos);
        }

        internal static bool CompareArgInfos(CSharpArgumentInfo[] argInfos, CSharpArgumentInfo[] otherArgInfos)
        {
            for (int i = 0; i < argInfos.Length; i++)
            {
                var argInfo = argInfos[i];
                var otherArgInfo = otherArgInfos[i];

                if (argInfo.Flags != otherArgInfo.Flags ||
                    argInfo.Name != otherArgInfo.Name)
                {
                    return false;
                }
            }

            return true;
        }

#if !ENABLECOMBINDER
        internal static void ThrowIfUsingDynamicCom(DynamicMetaObject target)
        {
            if (target.LimitType.IsCOMObject)
            {
                throw ErrorHandling.Error(ErrorCode.ERR_DynamicBindingComUnsupported);
            }
        }
#endif
    }
}
