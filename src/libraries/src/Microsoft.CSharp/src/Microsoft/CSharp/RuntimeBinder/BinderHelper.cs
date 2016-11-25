// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal static class BinderHelper
    {
        internal static DynamicMetaObject Bind(
                DynamicMetaObjectBinder action,
                RuntimeBinder binder,
                IEnumerable<DynamicMetaObject> args,
                IEnumerable<CSharpArgumentInfo> arginfos,
                DynamicMetaObject onBindingError)
        {
            List<Expression> parameters = new List<Expression>();
            BindingRestrictions restrictions = BindingRestrictions.Empty;
            ICSharpInvokeOrInvokeMemberBinder callPayload = action as ICSharpInvokeOrInvokeMemberBinder;
            ParameterExpression tempForIncrement = null;
            IEnumerator<CSharpArgumentInfo> arginfosEnum = arginfos == null ? null : arginfos.GetEnumerator();

            int index = 0;
            foreach (DynamicMetaObject o in args)
            {
                // Our contract with the DLR is such that we will not enter a bind unless we have
                // values for the meta-objects involved.

                if (!o.HasValue)
                {
                    Debug.Assert(false, "The runtime binder is being asked to bind a metaobject without a value");
                    throw Error.InternalCompilerError();
                }
                CSharpArgumentInfo info = null;
                if (arginfosEnum != null && arginfosEnum.MoveNext())
                    info = arginfosEnum.Current;

                if (index == 0 && IsIncrementOrDecrementActionOnLocal(action))
                {
                    // We have an inc or a dec operation. Insert the temp local instead.
                    //
                    // We need to do this because for value types, the object will come 
                    // in boxed, and we'd need to unbox it to get the original type in order
                    // to increment. The only way to do that is to create a new temporary.
                    tempForIncrement = Expression.Variable(o.Value != null ? o.Value.GetType() : typeof(object), "t0");
                    parameters.Add(tempForIncrement);
                }
                else
                {
                    parameters.Add(o.Expression);
                }

                BindingRestrictions r = DeduceArgumentRestriction(index, callPayload, o, info);
                restrictions = restrictions.Merge(r);

                // Here we check the argument info. If the argument info shows that the current argument
                // is a literal constant, then we also add an instance restriction on the value of
                // the constant.
                if (info != null && info.LiteralConstant)
                {
                    if ((o.Value is float && float.IsNaN((float)o.Value))
                      || o.Value is double && double.IsNaN((double)o.Value))
                    {
                        // We cannot create an equality restriction for NaN, because equality is implemented
                        // in such a way that NaN != NaN and the rule we make would be unsatisfiable.
                    }
                    else
                    {
                        Expression e = Expression.Equal(o.Expression, Expression.Constant(o.Value, o.Expression.Type));
                        r = BindingRestrictions.GetExpressionRestriction(e);
                        restrictions = restrictions.Merge(r);
                    }
                }

                ++index;
            }

            // Get the bound expression.
            try
            {
                DynamicMetaObject deferredBinding;
                Expression expression = binder.Bind(action, parameters, args.ToArray(), out deferredBinding);

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

                    DynamicMetaObject arg0 = Enumerable.First(args);

                    Expression assignTemp = Expression.Assign(
                        tempForIncrement,
                        Expression.Convert(arg0.Expression, arg0.Value.GetType()));
                    Expression assignResult = Expression.Assign(
                        arg0.Expression,
                        Expression.Convert(tempForIncrement, arg0.Expression.Type));
                    List<Expression> expressions = new List<Expression>();

                    expressions.Add(assignTemp);
                    expressions.Add(expression);
                    expressions.Add(assignResult);

                    expression = Expression.Block(new ParameterExpression[] { tempForIncrement }, expressions);
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
                        GetTypeForErrorMetaObject(action, args.FirstOrDefault())
                    ),
                    restrictions
                );
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

        // Try to determine if this object represents a WindowsRuntime object - i.e. it either
        // is coming from a WinMD file or is derived from a class coming from a WinMD.
        // The logic here matches the CLR's logic of finding a WinRT object.
        internal static bool IsWindowsRuntimeObject(DynamicMetaObject obj)
        {
            if (obj != null && obj.RuntimeType != null)
            {
                Type curType = obj.RuntimeType;
                while (curType != null)
                {
                    if ((curType.GetTypeInfo().Attributes & TypeAttributes.WindowsRuntime) == TypeAttributes.WindowsRuntime)
                    {
                        // Found a WinRT COM object
                        return true;
                    }
                    if ((curType.GetTypeInfo().Attributes & TypeAttributes.Import) == TypeAttributes.Import)
                    {
                        // Found a class that is actually imported from COM but not WinRT
                        // this is definitely a non-WinRT COM object
                        return false;
                    }
                    curType = curType.GetTypeInfo().BaseType;
                }
            }
            return false;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsTransparentProxy(object obj)
        {
            // In the full framework, this checks:
            //     return obj != null && RemotingServices.IsTransparentProxy(obj);
            // but transparent proxies don't exist in .NET Core.
            return false;
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
                (IsComObject(argument.Value) || IsTransparentProxy(argument.Value));

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

        private static Expression ConvertResult(Expression binding, DynamicMetaObjectBinder action)
        {
            // Need to handle the following cases:
            //   (1) Call to a constructor: no conversions.
            //   (2) Call to a void-returning method: return null iff result is discarded.
            //   (3) Call to a value-type returning method: box to object.
            //
            // In all other cases, binding.Type should be equivalent or
            // reference assignable to resultType.

            var invokeConstructor = action as CSharpInvokeConstructorBinder;
            if (invokeConstructor != null)
            {
                // No conversions needed, the call site has the correct type.
                return binding;
            }

            if (binding.Type == typeof(void))
            {
                var invoke = action as ICSharpInvokeOrInvokeMemberBinder;
                if (invoke != null && invoke.ResultDiscarded)
                {
                    Debug.Assert(action.ReturnType == typeof(object));
                    return Expression.Block(binding, Expression.Default(action.ReturnType));
                }
                else
                {
                    throw Error.BindToVoidMethodButExpectResult();
                }
            }

            if (binding.Type.GetTypeInfo().IsValueType && !action.ReturnType.GetTypeInfo().IsValueType)
            {
                Debug.Assert(action.ReturnType == typeof(object));
                return Expression.Convert(binding, action.ReturnType);
            }

            return binding;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static Type GetTypeForErrorMetaObject(DynamicMetaObjectBinder action, DynamicMetaObject arg0)
        {
            // This is similar to ConvertResult but has fewer things to worry about.

            var invokeConstructor = action as CSharpInvokeConstructorBinder;
            if (invokeConstructor != null)
            {
                if (arg0 == null || !(arg0.Value is Type))
                {
                    Debug.Assert(false);
                    return typeof(object);
                }

                Type result = arg0.Value as Type;

                return result;
            }

            return action.ReturnType;
        }

        /////////////////////////////////////////////////////////////////////////////////

        private static bool IsIncrementOrDecrementActionOnLocal(DynamicMetaObjectBinder action)
        {
            CSharpUnaryOperationBinder operatorPayload = action as CSharpUnaryOperationBinder;

            return operatorPayload != null &&
                (operatorPayload.Operation == ExpressionType.Increment || operatorPayload.Operation == ExpressionType.Decrement);
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static IEnumerable<T> Cons<T>(T sourceHead, IEnumerable<T> sourceTail)
        {
            yield return sourceHead;

            if (sourceTail != null)
            {
                foreach (T x in sourceTail)
                {
                    yield return x;
                }
            }
        }

        internal static IEnumerable<T> Cons<T>(T sourceHead, IEnumerable<T> sourceMiddle, T sourceLast)
        {
            yield return sourceHead;

            if (sourceMiddle != null)
            {
                foreach (T x in sourceMiddle)
                {
                    yield return x;
                }
            }

            yield return sourceLast;
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static List<T> ToList<T>(IEnumerable<T> source)
        {
            if (source == null)
            {
                return new List<T>();
            }

            return source.ToList();
        }

        /////////////////////////////////////////////////////////////////////////////////

        internal static CallInfo CreateCallInfo(IEnumerable<CSharpArgumentInfo> argInfos, int discard)
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

            foreach (CSharpArgumentInfo info in argInfos)
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
    }
}
