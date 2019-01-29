// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ExpressionResolverBuilder : CallSiteVisitor<object, Expression>
    {
        internal static readonly MethodInfo InvokeFactoryMethodInfo = GetMethodInfo<Action<Func<IServiceProvider, object>, IServiceProvider>>((a, b) => a.Invoke(b));
        internal static readonly MethodInfo CaptureDisposableMethodInfo = GetMethodInfo<Func<ServiceProviderEngineScope, object, object>>((a, b) => a.CaptureDisposable(b));
        internal static readonly MethodInfo TryGetValueMethodInfo = GetMethodInfo<Func<IDictionary<ServiceCacheKey, object>, ServiceCacheKey, object, bool>>((a, b, c) => a.TryGetValue(b, out c));
        internal static readonly MethodInfo AddMethodInfo = GetMethodInfo<Action<IDictionary<ServiceCacheKey, object>, ServiceCacheKey, object>>((a, b, c) => a.Add(b, c));
        internal static readonly MethodInfo MonitorEnterMethodInfo = GetMethodInfo<Action<object, bool>>((lockObj, lockTaken) => Monitor.Enter(lockObj, ref lockTaken));
        internal static readonly MethodInfo MonitorExitMethodInfo = GetMethodInfo<Action<object>>(lockObj => Monitor.Exit(lockObj));

        internal static readonly MethodInfo ArrayEmptyMethodInfo = typeof(Array).GetMethod(nameof(Array.Empty));

        private static readonly ParameterExpression ScopeParameter = Expression.Parameter(typeof(ServiceProviderEngineScope));

        private static readonly ParameterExpression ResolvedServices = Expression.Variable(typeof(IDictionary<ServiceCacheKey, object>), ScopeParameter.Name + "resolvedServices");
        private static readonly BinaryExpression ResolvedServicesVariableAssignment =
            Expression.Assign(ResolvedServices,
                Expression.Property(ScopeParameter, nameof(ServiceProviderEngineScope.ResolvedServices)));

        private static readonly ParameterExpression CaptureDisposableParameter = Expression.Parameter(typeof(object));
        private static readonly LambdaExpression CaptureDisposable = Expression.Lambda(
                    Expression.Call(ScopeParameter, CaptureDisposableMethodInfo, CaptureDisposableParameter),
                    CaptureDisposableParameter);

        private readonly CallSiteRuntimeResolver _runtimeResolver;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ServiceProviderEngineScope _rootScope;

        private readonly ConcurrentDictionary<ServiceCacheKey, Func<ServiceProviderEngineScope, object>> _scopeResolverCache;

        private readonly Func<ServiceCacheKey, ServiceCallSite, Func<ServiceProviderEngineScope, object>> _buildTypeDelegate;

        public ExpressionResolverBuilder(CallSiteRuntimeResolver runtimeResolver, IServiceScopeFactory serviceScopeFactory, ServiceProviderEngineScope rootScope)
        {
            if (runtimeResolver == null)
            {
                throw new ArgumentNullException(nameof(runtimeResolver));
            }

            _scopeResolverCache = new ConcurrentDictionary<ServiceCacheKey, Func<ServiceProviderEngineScope, object>>();
            _runtimeResolver = runtimeResolver;
            _serviceScopeFactory = serviceScopeFactory;
            _rootScope = rootScope;
            _buildTypeDelegate = (key, cs) => BuildNoCache(cs);
        }

        public Func<ServiceProviderEngineScope, object> Build(ServiceCallSite callSite)
        {
            // Optimize singleton case
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Root)
            {
                var value = _runtimeResolver.Resolve(callSite, _rootScope);
                return scope => value;
            }

            // Only scope methods are cached
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
#if NETCOREAPP
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, _buildTypeDelegate, callSite);
#else
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, key => _buildTypeDelegate(key, callSite));
#endif
            }

            return BuildNoCache(callSite);
        }

        public Func<ServiceProviderEngineScope, object> BuildNoCache(ServiceCallSite callSite)
        {
            var expression = BuildExpression(callSite);
            DependencyInjectionEventSource.Log.ExpressionTreeGenerated(callSite.ServiceType, expression);
            return expression.Compile();
        }

        private Expression<Func<ServiceProviderEngineScope, object>> BuildExpression(ServiceCallSite callSite)
        {
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                return Expression.Lambda<Func<ServiceProviderEngineScope, object>>(
                    Expression.Block(
                        new [] { ResolvedServices },
                        ResolvedServicesVariableAssignment,
                        BuildScopedExpression(callSite)),
                    ScopeParameter);
            }

            return Expression.Lambda<Func<ServiceProviderEngineScope, object>>(VisitCallSite(callSite, null), ScopeParameter);
        }

        protected override Expression VisitRootCache(ServiceCallSite singletonCallSite, object context)
        {
            return Expression.Constant(_runtimeResolver.Resolve(singletonCallSite, _rootScope));
        }

        protected override Expression VisitConstant(ConstantCallSite constantCallSite, object context)
        {
            return Expression.Constant(constantCallSite.DefaultValue);
        }

        protected override Expression VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, object context)
        {
            return ScopeParameter;
        }

        protected override Expression VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, object context)
        {
            return Expression.Constant(_serviceScopeFactory);
        }

        protected override Expression VisitFactory(FactoryCallSite factoryCallSite, object context)
        {
            return Expression.Invoke(Expression.Constant(factoryCallSite.Factory), ScopeParameter);
        }

        protected override Expression VisitIEnumerable(IEnumerableCallSite callSite, object context)
        {
            if (callSite.ServiceCallSites.Length == 0)
            {
                return Expression.Constant(ArrayEmptyMethodInfo
                    .MakeGenericMethod(callSite.ItemType)
                    .Invoke(obj: null, parameters: Array.Empty<object>()));
            }

            return Expression.NewArrayInit(
                callSite.ItemType,
                callSite.ServiceCallSites.Select(cs =>
                    Convert(
                        VisitCallSite(cs, context),
                        callSite.ItemType)));
        }

        protected override Expression VisitDisposeCache(ServiceCallSite callSite, object context)
        {
            // Elide calls to GetCaptureDisposable if the implementation type isn't disposable
            return TryCaptureDisposable(
                callSite,
                ScopeParameter,
                VisitCallSiteMain(callSite, context));
        }

        private Expression TryCaptureDisposable(ServiceCallSite callSite, ParameterExpression scope, Expression service)
        {
            if (!callSite.CaptureDisposable)
            {
                return service;
            }

            return Expression.Invoke(GetCaptureDisposable(scope), service);
        }

        protected override Expression VisitConstructor(ConstructorCallSite callSite, object context)
        {
            var parameters = callSite.ConstructorInfo.GetParameters();
            Expression[] parameterExpressions;
            if (callSite.ParameterCallSites.Length == 0)
            {
                parameterExpressions = Array.Empty<Expression>();
            }
            else
            {
                parameterExpressions = new Expression[callSite.ParameterCallSites.Length];
                for (int i = 0; i < parameterExpressions.Length; i++)
                {
                    parameterExpressions[i] = Convert(VisitCallSite(callSite.ParameterCallSites[i], context), parameters[i].ParameterType);
                }
            }
            return Expression.New(callSite.ConstructorInfo, parameterExpressions);
        }

        private static Expression Convert(Expression expression, Type type)
        {
            // Don't convert if the expression is already assignable
            if (type.GetTypeInfo().IsAssignableFrom(expression.Type.GetTypeInfo()))
            {
                return expression;
            }

            return Expression.Convert(expression, type);
        }

        protected override Expression VisitScopeCache(ServiceCallSite callSite, object context)
        {
            var lambda = Build(callSite);
            return Expression.Invoke(Expression.Constant(lambda), ScopeParameter);
        }

        // Move off the main stack
        private Expression BuildScopedExpression(ServiceCallSite callSite)
        {

            var keyExpression = Expression.Constant(
                callSite.Cache.Key,
                typeof(ServiceCacheKey));

            var resolvedVariable = Expression.Variable(typeof(object), "resolved");

            var resolvedServices = ResolvedServices;

            var tryGetValueExpression = Expression.Call(
                resolvedServices,
                TryGetValueMethodInfo,
                keyExpression,
                resolvedVariable);

            var captureDisposible = TryCaptureDisposable(callSite, ScopeParameter, VisitCallSiteMain(callSite, null));

            var assignExpression = Expression.Assign(
                resolvedVariable,
                captureDisposible);

            var addValueExpression = Expression.Call(
                resolvedServices,
                AddMethodInfo,
                keyExpression,
                resolvedVariable);

            var blockExpression = Expression.Block(
                typeof(object),
                new[]
                {
                    resolvedVariable
                },
                Expression.IfThen(
                    Expression.Not(tryGetValueExpression),
                    Expression.Block(
                        assignExpression,
                        addValueExpression)),
                resolvedVariable);


            // The C# compiler would copy the lock object to guard against mutation.
            // We don't, since we know the lock object is readonly.
            var lockWasTaken = Expression.Variable(typeof(bool), "lockWasTaken");

            var monitorEnter = Expression.Call(MonitorEnterMethodInfo, resolvedServices, lockWasTaken);
            var monitorExit = Expression.Call(MonitorExitMethodInfo, resolvedServices);

            var tryBody = Expression.Block(monitorEnter, blockExpression);
            var finallyBody = Expression.IfThen(lockWasTaken, monitorExit);

            return Expression.Block(
                typeof(object),
                new[] { lockWasTaken },
                Expression.TryFinally(tryBody, finallyBody));
        }

        private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method;
        }

        public Expression GetCaptureDisposable(ParameterExpression scope)
        {
            if (scope != ScopeParameter)
            {
                throw new NotSupportedException("GetCaptureDisposable call is supported only for main scope");
            }
            return CaptureDisposable;
        }
    }
}
