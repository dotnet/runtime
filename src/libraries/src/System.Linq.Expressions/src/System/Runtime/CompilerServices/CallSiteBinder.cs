// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Linq.Expressions;
using System.Threading;
using System.Reflection;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Class responsible for runtime binding of the dynamic operations on the dynamic call site.
    /// </summary>
    public abstract class CallSiteBinder
    {
        /// <summary>
        /// The Level 2 cache - all rules produced for the same binder.
        /// </summary>
        internal Dictionary<Type, object> Cache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CallSiteBinder"/> class.
        /// </summary>
        protected CallSiteBinder()
        {
        }

        /// <summary>
        /// Gets a label that can be used to cause the binding to be updated. It
        /// indicates that the expression's binding is no longer valid.
        /// This is typically used when the "version" of a dynamic object has
        /// changed.
        /// </summary>
        public static LabelTarget UpdateLabel { get; } = Expression.Label("CallSiteBinder.UpdateLabel");

        private sealed class LambdaSignature<T> where T : class
        {
            private static LambdaSignature<T> s_instance;

            internal static LambdaSignature<T> Instance
            {
                get
                {
                    if (s_instance == null)
                    {
                        s_instance = new LambdaSignature<T>();
                    }
                    return s_instance;
                }
            }

            internal readonly ReadOnlyCollection<ParameterExpression> Parameters;
            internal readonly LabelTarget ReturnLabel;

            private LambdaSignature()
            {
                Type target = typeof(T);
                if (!target.IsSubclassOf(typeof(MulticastDelegate)))
                {
                    throw System.Linq.Expressions.Error.TypeParameterIsNotDelegate(target);
                }

                MethodInfo invoke = target.GetMethod("Invoke");
                ParameterInfo[] pis = invoke.GetParametersCached();
                if (pis[0].ParameterType != typeof(CallSite))
                {
                    throw System.Linq.Expressions.Error.FirstArgumentMustBeCallSite();
                }

                var @params = new ParameterExpression[pis.Length - 1];
                for (int i = 0; i < @params.Length; i++)
                {
                    @params[i] = Expression.Parameter(pis[i + 1].ParameterType, "$arg" + i);
                }

                Parameters = new TrueReadOnlyCollection<ParameterExpression>(@params);
                ReturnLabel = Expression.Label(invoke.GetReturnType());
            }
        }

        /// <summary>
        /// Performs the runtime binding of the dynamic operation on a set of arguments.
        /// </summary>
        /// <param name="args">An array of arguments to the dynamic operation.</param>
        /// <param name="parameters">The array of <see cref="ParameterExpression"/> instances that represent the parameters of the call site in the binding process.</param>
        /// <param name="returnLabel">A LabelTarget used to return the result of the dynamic binding.</param>
        /// <returns>
        /// An Expression that performs tests on the dynamic operation arguments, and
        /// performs the dynamic operation if the tests are valid. If the tests fail on
        /// subsequent occurrences of the dynamic operation, Bind will be called again
        /// to produce a new <see cref="Expression"/> for the new argument types.
        /// </returns>
        public abstract Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel);

        /// <summary>
        /// Provides low-level runtime binding support.  Classes can override this and provide a direct
        /// delegate for the implementation of rule.  This can enable saving rules to disk, having
        /// specialized rules available at runtime, or providing a different caching policy.
        /// </summary>
        /// <typeparam name="T">The target type of the CallSite.</typeparam>
        /// <param name="site">The CallSite the bind is being performed for.</param>
        /// <param name="args">The arguments for the binder.</param>
        /// <returns>A new delegate which replaces the CallSite Target.</returns>
        public virtual T BindDelegate<T>(CallSite<T> site, object[] args) where T : class
        {
            return null;
        }

        internal T BindCore<T>(CallSite<T> site, object[] args) where T : class
        {
            //
            // Try to find a precompiled delegate, and return it if found.
            //
            T result = BindDelegate(site, args);
            if (result != null)
            {
                return result;
            }

            //
            // Get the Expression for the binding
            //
            var signature = LambdaSignature<T>.Instance;
            Expression binding = Bind(args, signature.Parameters, signature.ReturnLabel);

            //
            // Check the produced rule
            //
            if (binding == null)
            {
                throw System.Linq.Expressions.Error.NoOrInvalidRuleProduced();
            }

            //
            // finally produce the new rule if we need to
            //

            Expression<T> e = Stitch(binding, signature);
            T newRule = e.Compile();

            CacheTarget(newRule);

            return newRule;
        }

        /// <summary>
        /// Adds a target to the cache of known targets.  The cached targets will
        /// be scanned before calling BindDelegate to produce the new rule.
        /// </summary>
        /// <typeparam name="T">The type of target being added.</typeparam>
        /// <param name="target">The target delegate to be added to the cache.</param>
        protected void CacheTarget<T>(T target) where T : class
        {
            GetRuleCache<T>().AddRule(target);
        }

        private static Expression<T> Stitch<T>(Expression binding, LambdaSignature<T> signature) where T : class
        {
            Type siteType = typeof(CallSite<T>);

            var body = new ReadOnlyCollectionBuilder<Expression>(3);
            body.Add(binding);

            var site = Expression.Parameter(typeof(CallSite), "$site");
            var @params = signature.Parameters.AddFirst(site);

            Expression updLabel = Expression.Label(UpdateLabel);

#if DEBUG
            // put the AST into the constant pool for debugging purposes
            updLabel = Expression.Block(
                Expression.Constant(binding, typeof(Expression)),
                updLabel
            );
#endif

            body.Add(updLabel);
            body.Add(
                Expression.Label(
                    signature.ReturnLabel,
                    Expression.Condition(
                        Expression.Call(
                            CallSiteOps_SetNotMatched,
                            @params.First()
                        ),
                        Expression.Default(signature.ReturnLabel.Type),
                        Expression.Invoke(
                            Expression.Property(
                                Expression.Convert(site, siteType),
                                typeof(CallSite<T>).GetProperty(nameof(CallSite<T>.Update))
                            ),
                            new TrueReadOnlyCollection<Expression>(@params)
                        )
                    )
                )
            );

            return Expression.Lambda<T>(
                Expression.Block(body),
                "CallSite.Target",
                true, // always compile the rules with tail call optimization
                new TrueReadOnlyCollection<ParameterExpression>(@params)
            );
        }

        internal RuleCache<T> GetRuleCache<T>() where T : class
        {
            // make sure we have cache.
            if (Cache == null)
            {
                Interlocked.CompareExchange(ref Cache, new Dictionary<Type, object>(), null);
            }

            object ruleCache;
            var cache = Cache;
            lock (cache)
            {
                if (!cache.TryGetValue(typeof(T), out ruleCache))
                {
                    cache[typeof(T)] = ruleCache = new RuleCache<T>();
                }
            }

            RuleCache<T> result = ruleCache as RuleCache<T>;
            Debug.Assert(result != null);
            return result;
        }
    }
}
