// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Dynamic
{
    /// <summary>
    /// Provides a simple class that can be inherited from to create an object with dynamic behavior
    /// at runtime.  Subclasses can override the various binder methods (GetMember, SetMember, Call, etc...)
    /// to provide custom behavior that will be invoked at runtime.  
    /// 
    /// If a method is not overridden then the DynamicObject does not directly support that behavior and 
    /// the call site will determine how the binding should be performed.
    /// </summary>
    [Serializable]
    public class DynamicObject : IDynamicMetaObjectProvider
    {
        /// <summary>
        /// Enables derived types to create a new instance of DynamicObject.  DynamicObject instances cannot be
        /// directly instantiated because they have no implementation of dynamic behavior.
        /// </summary>
        protected DynamicObject()
        {
        }

        #region Public Virtual APIs

        /// <summary>
        /// Provides the implementation of getting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the get operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of setting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TrySetMember(SetMemberBinder binder, object value)
        {
            return false;
        }

        /// <summary>
        /// Provides the implementation of deleting a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TryDeleteMember(DeleteMemberBinder binder)
        {
            return false;
        }

        /// <summary>
        /// Provides the implementation of calling a member.  Derived classes can override
        /// this method to customize behavior.  When not overridden the call site requesting the
        /// binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of converting the DynamicObject to another type.  Derived classes
        /// can override this method to customize behavior.  When not overridden the call site
        /// requesting the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the conversion.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryConvert(ConvertBinder binder, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of creating an instance of the DynamicObject.  Derived classes
        /// can override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments used for creation.</param>
        /// <param name="result">The created instance.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryCreateInstance(CreateInstanceBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of invoking the DynamicObject.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="args">The arguments to be used for the invocation.</param>
        /// <param name="result">The result of the invocation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryInvoke(InvokeBinder binder, object[] args, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a binary operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="arg">The right operand for the operation.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryBinaryOperation(BinaryOperationBinder binder, object arg, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a unary operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryUnaryOperation(UnaryOperationBinder binder, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a get index operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="result">The result of the operation.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a set index operation.  Derived classes can
        /// override this method to customize behavior.  When not overridden the call site requesting
        /// the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be used.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1007:UseGenericsWhereAppropriate")]
        public virtual bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return false;
        }

        /// <summary>
        /// Provides the implementation of performing a delete index operation.  Derived classes
        /// can override this method to customize behavior.  When not overridden the call site
        /// requesting the binder determines the behavior.
        /// </summary>
        /// <param name="binder">The binder provided by the call site.</param>
        /// <param name="indexes">The indexes to be deleted.</param>
        /// <returns>true if the operation is complete, false if the call site should determine behavior.</returns>
        public virtual bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            return false;
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        /// <returns>The list of dynamic member names.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        public virtual System.Collections.Generic.IEnumerable<string> GetDynamicMemberNames()
        {
            return Array.Empty<string>();
        }
        #endregion

        #region MetaDynamic

        private sealed class MetaDynamic : DynamicMetaObject
        {
            internal MetaDynamic(Expression expression, DynamicObject value)
                : base(expression, BindingRestrictions.Empty, value)
            {
            }

            public override System.Collections.Generic.IEnumerable<string> GetDynamicMemberNames()
            {
                return Value.GetDynamicMemberNames();
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                if (IsOverridden("TryGetMember"))
                {
                    return CallMethodWithResult("TryGetMember", binder, s_noArgs, (e) => binder.FallbackGetMember(this, e));
                }

                return base.BindGetMember(binder);
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                if (IsOverridden("TrySetMember"))
                {
                    return CallMethodReturnLast("TrySetMember", binder, s_noArgs, value.Expression, (e) => binder.FallbackSetMember(this, value, e));
                }

                return base.BindSetMember(binder, value);
            }

            public override DynamicMetaObject BindDeleteMember(DeleteMemberBinder binder)
            {
                if (IsOverridden("TryDeleteMember"))
                {
                    return CallMethodNoResult("TryDeleteMember", binder, s_noArgs, (e) => binder.FallbackDeleteMember(this, e));
                }

                return base.BindDeleteMember(binder);
            }

            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                if (IsOverridden("TryConvert"))
                {
                    return CallMethodWithResult("TryConvert", binder, s_noArgs, (e) => binder.FallbackConvert(this, e));
                }

                return base.BindConvert(binder);
            }

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                // Generate a tree like:
                //
                // {
                //   object result;
                //   TryInvokeMember(payload, out result)
                //      ? result
                //      : TryGetMember(payload, out result)
                //          ? FallbackInvoke(result)
                //          : fallbackResult
                // }
                //
                // Then it calls FallbackInvokeMember with this tree as the
                // "error", giving the language the option of using this
                // tree or doing .NET binding.
                //
                Fallback fallback = e => binder.FallbackInvokeMember(this, args, e);

                var call = BuildCallMethodWithResult(
                    "TryInvokeMember",
                    binder,
                    DynamicMetaObject.GetExpressions(args),
                    BuildCallMethodWithResult<GetMemberBinder>(
                        "TryGetMember",
                        new GetBinderAdapter(binder),
                        s_noArgs,
                        fallback(null),
                        (e) => binder.FallbackInvoke(e, args, null)
                    ),
                    null
                );

                return fallback(call);
            }


            public override DynamicMetaObject BindCreateInstance(CreateInstanceBinder binder, DynamicMetaObject[] args)
            {
                if (IsOverridden("TryCreateInstance"))
                {
                    return CallMethodWithResult("TryCreateInstance", binder, DynamicMetaObject.GetExpressions(args), (e) => binder.FallbackCreateInstance(this, args, e));
                }

                return base.BindCreateInstance(binder, args);
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                if (IsOverridden("TryInvoke"))
                {
                    return CallMethodWithResult("TryInvoke", binder, DynamicMetaObject.GetExpressions(args), (e) => binder.FallbackInvoke(this, args, e));
                }

                return base.BindInvoke(binder, args);
            }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (IsOverridden("TryBinaryOperation"))
                {
                    return CallMethodWithResult("TryBinaryOperation", binder, DynamicMetaObject.GetExpressions(new DynamicMetaObject[] { arg }), (e) => binder.FallbackBinaryOperation(this, arg, e));
                }

                return base.BindBinaryOperation(binder, arg);
            }

            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                if (IsOverridden("TryUnaryOperation"))
                {
                    return CallMethodWithResult("TryUnaryOperation", binder, s_noArgs, (e) => binder.FallbackUnaryOperation(this, e));
                }

                return base.BindUnaryOperation(binder);
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (IsOverridden("TryGetIndex"))
                {
                    return CallMethodWithResult("TryGetIndex", binder, DynamicMetaObject.GetExpressions(indexes), (e) => binder.FallbackGetIndex(this, indexes, e));
                }

                return base.BindGetIndex(binder, indexes);
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                if (IsOverridden("TrySetIndex"))
                {
                    return CallMethodReturnLast("TrySetIndex", binder, DynamicMetaObject.GetExpressions(indexes), value.Expression, (e) => binder.FallbackSetIndex(this, indexes, value, e));
                }

                return base.BindSetIndex(binder, indexes, value);
            }

            public override DynamicMetaObject BindDeleteIndex(DeleteIndexBinder binder, DynamicMetaObject[] indexes)
            {
                if (IsOverridden("TryDeleteIndex"))
                {
                    return CallMethodNoResult("TryDeleteIndex", binder, DynamicMetaObject.GetExpressions(indexes), (e) => binder.FallbackDeleteIndex(this, indexes, e));
                }

                return base.BindDeleteIndex(binder, indexes);
            }

            private delegate DynamicMetaObject Fallback(DynamicMetaObject errorSuggestion);

            private readonly static Expression[] s_noArgs = new Expression[0]; // used in reference comparison, requires unique object identity

            private static Expression[] GetConvertedArgs(params Expression[] args)
            {
                ReadOnlyCollectionBuilder<Expression> paramArgs = new ReadOnlyCollectionBuilder<Expression>(args.Length);

                for (int i = 0; i < args.Length; i++)
                {
                    paramArgs.Add(Expression.Convert(args[i], typeof(object)));
                }

                return paramArgs.ToArray();
            }

            /// <summary>
            /// Helper method for generating expressions that assign byRef call
            /// parameters back to their original variables
            /// </summary>
            private static Expression ReferenceArgAssign(Expression callArgs, Expression[] args)
            {
                ReadOnlyCollectionBuilder<Expression> block = null;

                for (int i = 0; i < args.Length; i++)
                {
                    ContractUtils.Requires(args[i] is ParameterExpression);
                    if (((ParameterExpression)args[i]).IsByRef)
                    {
                        if (block == null)
                            block = new ReadOnlyCollectionBuilder<Expression>();

                        block.Add(
                            Expression.Assign(
                                args[i],
                                Expression.Convert(
                                    Expression.ArrayIndex(
                                        callArgs,
                                        Expression.Constant(i)
                                    ),
                                    args[i].Type
                                )
                            )
                        );
                    }
                }

                if (block != null)
                    return Expression.Block(block);
                else
                    return Expression.Empty();
            }

            /// <summary>
            /// Helper method for generating arguments for calling methods
            /// on DynamicObject.  parameters is either a list of ParameterExpressions
            /// to be passed to the method as an object[], or NoArgs to signify that
            /// the target method takes no object[] parameter.
            /// </summary>
            private static Expression[] BuildCallArgs<TBinder>(TBinder binder, Expression[] parameters, Expression arg0, Expression arg1)
                where TBinder : DynamicMetaObjectBinder
            {
                if (!object.ReferenceEquals(parameters, s_noArgs))
                    return arg1 != null ? new Expression[] { Constant(binder), arg0, arg1 } : new Expression[] { Constant(binder), arg0 };
                else
                    return arg1 != null ? new Expression[] { Constant(binder), arg1 } : new Expression[] { Constant(binder) };
            }

            private static ConstantExpression Constant<TBinder>(TBinder binder)
            {
                return Expression.Constant(binder, typeof(TBinder));
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic that returns a result
            /// </summary>
            private DynamicMetaObject CallMethodWithResult<TBinder>(string methodName, TBinder binder, Expression[] args, Fallback fallback)
                where TBinder : DynamicMetaObjectBinder
            {
                return CallMethodWithResult(methodName, binder, args, fallback, null);
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic that returns a result
            /// </summary>
            private DynamicMetaObject CallMethodWithResult<TBinder>(string methodName, TBinder binder, Expression[] args, Fallback fallback, Fallback fallbackInvoke)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(null);

                var callDynamic = BuildCallMethodWithResult(methodName, binder, args, fallbackResult, fallbackInvoke);

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(callDynamic);
            }

            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on DynamicObject that returns a result.
            /// 
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject BuildCallMethodWithResult<TBinder>(string methodName, TBinder binder, Expression[] args, DynamicMetaObject fallbackResult, Fallback fallbackInvoke)
                where TBinder : DynamicMetaObjectBinder
            {
                if (!IsOverridden(methodName))
                {
                    return fallbackResult;
                }

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TryGetMember(payload, out result) ? fallbackInvoke(result) : fallbackResult
                // }
                //
                var result = Expression.Parameter(typeof(object), null);
                ParameterExpression callArgs = methodName != "TryBinaryOperation" ? Expression.Parameter(typeof(object[]), null) : Expression.Parameter(typeof(object), null);
                var callArgsValue = GetConvertedArgs(args);

                var resultMO = new DynamicMetaObject(result, BindingRestrictions.Empty);

                // Need to add a conversion if calling TryConvert
                if (binder.ReturnType != typeof(object))
                {
                    Debug.Assert(binder is ConvertBinder && fallbackInvoke == null);

                    var convert = Expression.Convert(resultMO.Expression, binder.ReturnType);
                    // will always be a cast or unbox
                    Debug.Assert(convert.Method == null);

                    // Prepare a good exception message in case the convert will fail
                    string convertFailed = Strings.DynamicObjectResultNotAssignable(
                        "{0}",
                        this.Value.GetType(),
                        binder.GetType(),
                        binder.ReturnType
                    );

                    Expression condition;
                    // If the return type can not be assigned null then just check for type assignablity otherwise allow null.
                    if (binder.ReturnType.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(binder.ReturnType) == null)
                    {
                        condition = Expression.TypeIs(resultMO.Expression, binder.ReturnType);
                    }
                    else
                    {
                        condition = Expression.OrElse(
                                        Expression.Equal(resultMO.Expression, Expression.Constant(null)),
                                        Expression.TypeIs(resultMO.Expression, binder.ReturnType));
                    }

                    var checkedConvert = Expression.Condition(
                        condition,
                        convert,
                        Expression.Throw(
                            Expression.New(typeof(InvalidCastException).GetConstructor(new Type[] { typeof(string) }),
                                Expression.Call(
                                    typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object[]) }),
                                    Expression.Constant(convertFailed),
                                    Expression.NewArrayInit(typeof(object),
                                        Expression.Condition(
                                            Expression.Equal(resultMO.Expression, Expression.Constant(null)),
                                            Expression.Constant("null"),
                                            Expression.Call(
                                                resultMO.Expression,
                                                typeof(object).GetMethod("GetType")
                                            ),
                                            typeof(object)
                                        )
                                    )
                                )
                            ),
                            binder.ReturnType
                        ),
                        binder.ReturnType
                    );

                    resultMO = new DynamicMetaObject(checkedConvert, resultMO.Restrictions);
                }

                if (fallbackInvoke != null)
                {
                    resultMO = fallbackInvoke(resultMO);
                }

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { result, callArgs },
                        methodName != "TryBinaryOperation" ? Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)) : Expression.Assign(callArgs, callArgsValue[0]),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                typeof(DynamicObject).GetMethod(methodName),
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    result
                                )
                            ),
                            Expression.Block(
                                methodName != "TryBinaryOperation" ? ReferenceArgAssign(callArgs, args) : Expression.Empty(),
                                resultMO.Expression
                            ),
                            fallbackResult.Expression,
                            binder.ReturnType
                        )
                    ),
                    GetRestrictions().Merge(resultMO.Restrictions).Merge(fallbackResult.Restrictions)
                );
                return callDynamic;
            }


            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            /// 
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodReturnLast<TBinder>(string methodName, TBinder binder, Expression[] args, Expression value, Fallback fallback)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(null);

                //
                // Build a new expression like:
                // {
                //   object result;
                //   TrySetMember(payload, result = value) ? result : fallbackResult
                // }
                //

                var result = Expression.Parameter(typeof(object), null);
                var callArgs = Expression.Parameter(typeof(object[]), null);
                var callArgsValue = GetConvertedArgs(args);

                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { result, callArgs },
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                typeof(DynamicObject).GetMethod(methodName),
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    Expression.Assign(result, Expression.Convert(value, typeof(object)))
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                result
                            ),
                            fallbackResult.Expression,
                            typeof(object)
                        )
                    ),
                    GetRestrictions().Merge(fallbackResult.Restrictions)
                );

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(callDynamic);
            }


            /// <summary>
            /// Helper method for generating a MetaObject which calls a
            /// specific method on Dynamic, but uses one of the arguments for
            /// the result.
            /// 
            /// args is either an array of arguments to be passed
            /// to the method as an object[] or NoArgs to signify that
            /// the target method takes no parameters.
            /// </summary>
            private DynamicMetaObject CallMethodNoResult<TBinder>(string methodName, TBinder binder, Expression[] args, Fallback fallback)
                where TBinder : DynamicMetaObjectBinder
            {
                //
                // First, call fallback to do default binding
                // This produces either an error or a call to a .NET member
                //
                DynamicMetaObject fallbackResult = fallback(null);
                var callArgs = Expression.Parameter(typeof(object[]), null);
                var callArgsValue = GetConvertedArgs(args);

                //
                // Build a new expression like:
                //   if (TryDeleteMember(payload)) { } else { fallbackResult }
                //
                var callDynamic = new DynamicMetaObject(
                    Expression.Block(
                        new[] { callArgs },
                        Expression.Assign(callArgs, Expression.NewArrayInit(typeof(object), callArgsValue)),
                        Expression.Condition(
                            Expression.Call(
                                GetLimitedSelf(),
                                typeof(DynamicObject).GetMethod(methodName),
                                BuildCallArgs(
                                    binder,
                                    args,
                                    callArgs,
                                    null
                                )
                            ),
                            Expression.Block(
                                ReferenceArgAssign(callArgs, args),
                                Expression.Empty()
                            ),
                            fallbackResult.Expression,
                            typeof(void)
                        )
                    ),
                    GetRestrictions().Merge(fallbackResult.Restrictions)
                );

                //
                // Now, call fallback again using our new MO as the error
                // When we do this, one of two things can happen:
                //   1. Binding will succeed, and it will ignore our call to
                //      the dynamic method, OR
                //   2. Binding will fail, and it will use the MO we created
                //      above.
                //
                return fallback(callDynamic);
            }

            /// <summary>
            /// Checks if the derived type has overridden the specified method.  If there is no
            /// implementation for the method provided then Dynamic falls back to the base class
            /// behavior which lets the call site determine how the binder is performed.
            /// </summary>
            private bool IsOverridden(string method)
            {
                var methods = Value.GetType().GetMember(method, BindingFlags.Public | BindingFlags.Instance).OfType<MethodInfo>();

                foreach (MethodInfo mi in methods)
                {
                    if (mi.DeclaringType != typeof(DynamicObject))
                    {
                        var baseMethods = typeof(DynamicObject).GetMember(method, BindingFlags.Public | BindingFlags.Instance).OfType<MethodInfo>();
                        foreach (MethodInfo baseMethod in baseMethods)
                        {
                            var baseParams = baseMethod.GetParameters();
                            var miParams = mi.GetParameters();

                            if (baseParams.Length == miParams.Length)
                            {
                                bool mismatch = false;
                                for (int i = 0; i < baseParams.Length; i++)
                                {
                                    if (baseParams[i].ParameterType != miParams[i].ParameterType)
                                    {
                                        mismatch = true;
                                    }
                                }
                                if (!mismatch)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }

            /// <summary>
            /// Returns a Restrictions object which includes our current restrictions merged
            /// with a restriction limiting our type
            /// </summary>
            private BindingRestrictions GetRestrictions()
            {
                Debug.Assert(Restrictions == BindingRestrictions.Empty, "We don't merge, restrictions are always empty");

                return BindingRestrictions.GetTypeRestriction(this);
            }

            /// <summary>
            /// Returns our Expression converted to DynamicObject
            /// </summary>
            private Expression GetLimitedSelf()
            {
                // Convert to DynamicObject rather than LimitType, because
                // the limit type might be non-public.
                if (TypeUtils.AreEquivalent(Expression.Type, typeof(DynamicObject)))
                {
                    return Expression;
                }
                return Expression.Convert(Expression, typeof(DynamicObject));
            }

            private new DynamicObject Value
            {
                get
                {
                    return (DynamicObject)base.Value;
                }
            }

            // It is okay to throw NotSupported from this binder. This object
            // is only used by DynamicObject.GetMember--it is not expected to
            // (and cannot) implement binding semantics. It is just so the DO
            // can use the Name and IgnoreCase properties.
            private sealed class GetBinderAdapter : GetMemberBinder
            {
                internal GetBinderAdapter(InvokeMemberBinder binder)
                    : base(binder.Name, binder.IgnoreCase)
                {
                }

                public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
                {
                    throw new NotSupportedException();
                }
            }
        }

        #endregion

        #region IDynamicMetaObjectProvider Members

        /// <summary>
        /// The provided MetaObject will dispatch to the Dynamic virtual methods.
        /// The object can be encapsulated inside of another MetaObject to
        /// provide custom behavior for individual actions.
        /// </summary>
        public virtual DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new MetaDynamic(parameter, this);
        }
        #endregion
    }
}
