// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal class ExpressionResolverBuilder : CallSiteVisitor<CallSiteExpressionBuilderContext, Expression>
    {
        internal static readonly MethodInfo InvokeFactoryMethodInfo = GetMethodInfo<Action<Func<IServiceProvider, object>, IServiceProvider>>((a, b) => a.Invoke(b));
        internal static readonly MethodInfo CaptureDisposableMethodInfo = GetMethodInfo<Func<ServiceProviderEngineScope, object, object>>((a, b) => a.CaptureDisposable(b));
        internal static readonly MethodInfo TryGetValueMethodInfo = GetMethodInfo<Func<IDictionary<object, object>, object, object, bool>>((a, b, c) => a.TryGetValue(b, out c));
        internal static readonly MethodInfo AddMethodInfo = GetMethodInfo<Action<IDictionary<object, object>, object, object>>((a, b, c) => a.Add(b, c));
        internal static readonly MethodInfo MonitorEnterMethodInfo = GetMethodInfo<Action<object, bool>>((lockObj, lockTaken) => Monitor.Enter(lockObj, ref lockTaken));
        internal static readonly MethodInfo MonitorExitMethodInfo = GetMethodInfo<Action<object>>(lockObj => Monitor.Exit(lockObj));
        internal static readonly MethodInfo CallSiteRuntimeResolverResolve =
            GetMethodInfo<Func<CallSiteRuntimeResolver, IServiceCallSite, ServiceProviderEngineScope, object>>((r, c, p) => r.Resolve(c, p));

        internal static readonly MethodInfo ArrayEmptyMethodInfo = typeof(Array).GetMethod(nameof(Array.Empty));

        private static readonly ParameterExpression ScopeParameter = Expression.Parameter(typeof(ServiceProviderEngineScope));

        private static readonly ParameterExpression ResolvedServices = Expression.Variable(typeof(IDictionary<object, object>), ScopeParameter.Name + "resolvedServices");
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

        public ExpressionResolverBuilder(CallSiteRuntimeResolver runtimeResolver, IServiceScopeFactory serviceScopeFactory, ServiceProviderEngineScope rootScope)
        {
            if (runtimeResolver == null)
            {
                throw new ArgumentNullException(nameof(runtimeResolver));
            }
            _runtimeResolver = runtimeResolver;
            _serviceScopeFactory = serviceScopeFactory;
            _rootScope = rootScope;
        }

        public Func<ServiceProviderEngineScope, object> Build(IServiceCallSite callSite)
        {
            if (callSite is SingletonCallSite singletonCallSite)
            {
                // If root call site is singleton we can return Func calling
                // _runtimeResolver.Resolve directly and avoid Expression generation
                if (TryResolveSingletonValue(singletonCallSite, out var value))
                {
                    return scope => value;
                }

                return scope => _runtimeResolver.Resolve(callSite, scope);
            }

            return BuildExpression(callSite).Compile();
        }

        private bool TryResolveSingletonValue(SingletonCallSite singletonCallSite, out object value)
        {
            lock (_rootScope.ResolvedServices)
            {
                return _rootScope.ResolvedServices.TryGetValue(singletonCallSite.CacheKey, out value);
            }
        }

        private Expression<Func<ServiceProviderEngineScope, object>> BuildExpression(IServiceCallSite callSite)
        {
            var context = new CallSiteExpressionBuilderContext
            {
                ScopeParameter = ScopeParameter
            };

            var serviceExpression = VisitCallSite(callSite, context);

            if (context.RequiresResolvedServices)
            {
                return Expression.Lambda<Func<ServiceProviderEngineScope, object>>(
                    Expression.Block(
                        new [] { ResolvedServices },
                        ResolvedServicesVariableAssignment,
                        Lock(serviceExpression, ResolvedServices)),
                    ScopeParameter);
            }

            return Expression.Lambda<Func<ServiceProviderEngineScope, object>>(serviceExpression, ScopeParameter);
        }

        protected override Expression VisitSingleton(SingletonCallSite singletonCallSite, CallSiteExpressionBuilderContext context)
        {
            if (TryResolveSingletonValue(singletonCallSite, out var value))
            {
                return Expression.Constant(value);
            }

            return Expression.Call(
                Expression.Constant(_runtimeResolver),
                CallSiteRuntimeResolverResolve,
                Expression.Constant(singletonCallSite, typeof(IServiceCallSite)),
                context.ScopeParameter);
        }

        protected override Expression VisitConstant(ConstantCallSite constantCallSite, CallSiteExpressionBuilderContext context)
        {
            return Expression.Constant(constantCallSite.DefaultValue);
        }

        protected override Expression VisitCreateInstance(CreateInstanceCallSite createInstanceCallSite, CallSiteExpressionBuilderContext context)
        {
            return Expression.New(createInstanceCallSite.ImplementationType);
        }

        protected override Expression VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, CallSiteExpressionBuilderContext context)
        {
            return context.ScopeParameter;
        }

        protected override Expression VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, CallSiteExpressionBuilderContext context)
        {
            return Expression.Constant(_serviceScopeFactory);
        }

        protected override Expression VisitFactory(FactoryCallSite factoryCallSite, CallSiteExpressionBuilderContext context)
        {
            return Expression.Invoke(Expression.Constant(factoryCallSite.Factory), context.ScopeParameter);
        }

        protected override Expression VisitIEnumerable(IEnumerableCallSite callSite, CallSiteExpressionBuilderContext context)
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

        protected override Expression VisitTransient(TransientCallSite callSite, CallSiteExpressionBuilderContext context)
        {
            var implType = callSite.ServiceCallSite.ImplementationType;
            // Elide calls to GetCaptureDisposable if the implemenation type isn't disposable
            return TryCaptureDisposible(
                implType,
                context.ScopeParameter,
                VisitCallSite(callSite.ServiceCallSite, context));
        }

        private Expression TryCaptureDisposible(Type implType, ParameterExpression scope, Expression service)
        {
            if (implType != null &&
                !typeof(IDisposable).GetTypeInfo().IsAssignableFrom(implType.GetTypeInfo()))
            {
                return service;
            }

            return Expression.Invoke(GetCaptureDisposable(scope),
                service);
        }

        protected override Expression VisitConstructor(ConstructorCallSite callSite, CallSiteExpressionBuilderContext context)
        {
            var parameters = callSite.ConstructorInfo.GetParameters();
            var parameterExpressions = new Expression[callSite.ParameterCallSites.Length];
            for (int i = 0; i < parameterExpressions.Length; i++)
            {
                parameterExpressions[i] = Convert(VisitCallSite(callSite.ParameterCallSites[i], context), parameters[i].ParameterType);
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

        protected override Expression VisitScoped(ScopedCallSite callSite, CallSiteExpressionBuilderContext context)
        {
            return BuildScopedExpression(callSite, context, VisitCallSite(callSite.ServiceCallSite, context));
        }

        // Move off the main stack
        private Expression BuildScopedExpression(ScopedCallSite callSite, CallSiteExpressionBuilderContext context, Expression service)
        {
            var keyExpression = Expression.Constant(
                callSite.CacheKey,
                typeof(object));

            var resolvedVariable = Expression.Variable(typeof(object), "resolved");

            var resolvedServices = GetResolvedServices(context);

            var tryGetValueExpression = Expression.Call(
                resolvedServices,
                TryGetValueMethodInfo,
                keyExpression,
                resolvedVariable);

            var captureDisposible = TryCaptureDisposible(callSite.ImplementationType, context.ScopeParameter, service);

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

            return blockExpression;
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

        public Expression GetResolvedServices(CallSiteExpressionBuilderContext context)
        {
            if (context.ScopeParameter != ScopeParameter)
            {
                throw new NotSupportedException("GetResolvedServices call is supported only for main scope");
            }
            context.RequiresResolvedServices = true;
            return ResolvedServices;
        }

        private static Expression Lock(Expression body, Expression syncVariable)
        {
            // The C# compiler would copy the lock object to guard against mutation.
            // We don't, since we know the lock object is readonly.
            var lockWasTaken = Expression.Variable(typeof(bool), "lockWasTaken");

            var monitorEnter = Expression.Call(MonitorEnterMethodInfo, syncVariable, lockWasTaken);
            var monitorExit = Expression.Call(MonitorExitMethodInfo, syncVariable);

            var tryBody = Expression.Block(monitorEnter, body);
            var finallyBody = Expression.IfThen(lockWasTaken, monitorExit);

            return Expression.Block(
                typeof(object),
                new[] { lockWasTaken },
                Expression.TryFinally(tryBody, finallyBody));
        }
    }
}