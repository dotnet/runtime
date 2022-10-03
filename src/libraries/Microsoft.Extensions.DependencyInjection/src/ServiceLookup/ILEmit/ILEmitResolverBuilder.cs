// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ILEmitResolverBuilder : CallSiteVisitor<ILEmitResolverBuilderContext, object?>
    {
        private static readonly MethodInfo ResolvedServicesGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.ResolvedServices), BindingFlags.Instance | BindingFlags.NonPublic)!.GetMethod!;

        private static readonly MethodInfo ScopeLockGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.Sync), BindingFlags.Instance | BindingFlags.NonPublic)!.GetMethod!;

        private static readonly MethodInfo ScopeIsRootScope = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.IsRootScope), BindingFlags.Instance | BindingFlags.Public)!.GetMethod!;

        private static readonly MethodInfo CallSiteRuntimeResolverResolveMethod = typeof(CallSiteRuntimeResolver).GetMethod(
            nameof(CallSiteRuntimeResolver.Resolve), BindingFlags.Public | BindingFlags.Instance)!;

        private static readonly MethodInfo CallSiteRuntimeResolverInstanceField = typeof(CallSiteRuntimeResolver).GetProperty(
            nameof(CallSiteRuntimeResolver.Instance), BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance)!.GetMethod!;

        private static readonly FieldInfo FactoriesField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Factories))!;
        private static readonly FieldInfo ConstantsField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Constants))!;
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!;

        private static readonly ConstructorInfo CacheKeyCtor = typeof(ServiceCacheKey).GetConstructors()[0];

        private sealed class ILEmitResolverBuilderRuntimeContext
        {
            public object?[]? Constants;
            public Func<IServiceProvider, object>[]? Factories;
        }

        private struct GeneratedMethod
        {
            public Func<ServiceProviderEngineScope, object?> Lambda;

            public ILEmitResolverBuilderRuntimeContext Context;
            public DynamicMethod DynamicMethod;
        }

        private readonly ServiceProviderEngineScope _rootScope;

        private readonly ConcurrentDictionary<ServiceCacheKey, GeneratedMethod> _scopeResolverCache;

        private readonly Func<ServiceCacheKey, ServiceCallSite, GeneratedMethod> _buildTypeDelegate;

        public ILEmitResolverBuilder(ServiceProvider serviceProvider)
        {
            _rootScope = serviceProvider.Root;
            _scopeResolverCache = new ConcurrentDictionary<ServiceCacheKey, GeneratedMethod>();
            _buildTypeDelegate = (key, cs) => BuildTypeNoCache(cs);
        }

        public Func<ServiceProviderEngineScope, object?> Build(ServiceCallSite callSite)
        {
            return BuildType(callSite).Lambda;
        }

        private GeneratedMethod BuildType(ServiceCallSite callSite)
        {
            // Only scope methods are cached
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
#if NETFRAMEWORK || NETSTANDARD2_0
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, key => _buildTypeDelegate(key, callSite));
#else
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, _buildTypeDelegate, callSite);
#endif
            }

            return BuildTypeNoCache(callSite);
        }

        private GeneratedMethod BuildTypeNoCache(ServiceCallSite callSite)
        {
            // We need to skip visibility checks because services/constructors might be private
            var dynamicMethod = new DynamicMethod("ResolveService",
                attributes: MethodAttributes.Public | MethodAttributes.Static,
                callingConvention: CallingConventions.Standard,
                returnType: typeof(object),
                parameterTypes: new[] { typeof(ILEmitResolverBuilderRuntimeContext), typeof(ServiceProviderEngineScope) },
                owner: GetType(),
                skipVisibility: true);

            // In traces we've seen methods range from 100B - 4K sized methods since we've
            // stop trying to inline everything into scoped methods. We'll pay for a couple of resizes
            // so there'll be allocations but we could potentially change ILGenerator to use the array pool
            ILGenerator ilGenerator = dynamicMethod.GetILGenerator(512);
            ILEmitResolverBuilderRuntimeContext runtimeContext = GenerateMethodBody(callSite, ilGenerator);

#if SAVE_ASSEMBLIES
            var assemblyName = "Test" + DateTime.Now.Ticks;
            var fileName = assemblyName + ".dll";

            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);
            var module = assembly.DefineDynamicModule(assemblyName, fileName);
            var type = module.DefineType(callSite.ServiceType.Name + "Resolver");

            var method = type.DefineMethod(
                "ResolveService", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(object),
                new[] { typeof(ILEmitResolverBuilderRuntimeContext), typeof(ServiceProviderEngineScope) });

            GenerateMethodBody(callSite, method.GetILGenerator());
            type.CreateTypeInfo();
            // Assembly.Save is only available in .NET Framework (https://github.com/dotnet/runtime/issues/15704)
            assembly.Save(fileName);
#endif
            DependencyInjectionEventSource.Log.DynamicMethodBuilt(_rootScope.RootProvider, callSite.ServiceType, ilGenerator.ILOffset);

            return new GeneratedMethod()
            {
                Lambda = (Func<ServiceProviderEngineScope, object?>)dynamicMethod.CreateDelegate(typeof(Func<ServiceProviderEngineScope, object?>), runtimeContext),
                Context = runtimeContext,
                DynamicMethod = dynamicMethod
            };
        }


        protected override object? VisitDisposeCache(ServiceCallSite transientCallSite, ILEmitResolverBuilderContext argument)
        {
            if (transientCallSite.CaptureDisposable)
            {
                BeginCaptureDisposable(argument);
                VisitCallSiteMain(transientCallSite, argument);
                EndCaptureDisposable(argument);
            }
            else
            {
                VisitCallSiteMain(transientCallSite, argument);
            }
            return null;
        }

        protected override object? VisitConstructor(ConstructorCallSite constructorCallSite, ILEmitResolverBuilderContext argument)
        {
            // new T([create arguments])
            foreach (ServiceCallSite parameterCallSite in constructorCallSite.ParameterCallSites)
            {
                VisitCallSite(parameterCallSite, argument);
                if (parameterCallSite.ServiceType.IsValueType)
                {
                    argument.Generator.Emit(OpCodes.Unbox_Any, parameterCallSite.ServiceType);
                }
            }

            argument.Generator.Emit(OpCodes.Newobj, constructorCallSite.ConstructorInfo);
            if (constructorCallSite.ImplementationType!.IsValueType)
            {
                argument.Generator.Emit(OpCodes.Box, constructorCallSite.ImplementationType);
            }

            return null;
        }

        protected override object? VisitRootCache(ServiceCallSite callSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, CallSiteRuntimeResolver.Instance.Resolve(callSite, _rootScope));
            return null;
        }

        protected override object? VisitScopeCache(ServiceCallSite scopedCallSite, ILEmitResolverBuilderContext argument)
        {
            GeneratedMethod generatedMethod = BuildType(scopedCallSite);

            // Type builder doesn't support invoking dynamic methods, replace them with delegate.Invoke calls
#if SAVE_ASSEMBLIES
            AddConstant(argument, generatedMethod.Lambda);
            // ProviderScope
            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Call, generatedMethod.Lambda.GetType().GetMethod("Invoke"));
#else
            AddConstant(argument, generatedMethod.Context);
            // ProviderScope
            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Call, generatedMethod.DynamicMethod);
#endif

            return null;
        }

        protected override object? VisitConstant(ConstantCallSite constantCallSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, constantCallSite.DefaultValue);
            return null;
        }

        protected override object? VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, ILEmitResolverBuilderContext argument)
        {
            // [return] ProviderScope
            argument.Generator.Emit(OpCodes.Ldarg_1);
            return null;
        }

        protected override object? VisitIEnumerable(IEnumerableCallSite enumerableCallSite, ILEmitResolverBuilderContext argument)
        {
            if (enumerableCallSite.ServiceCallSites.Length == 0)
            {
                argument.Generator.Emit(OpCodes.Call, ServiceLookupHelpers.GetArrayEmptyMethodInfo(enumerableCallSite.ItemType));
            }
            else
            {
                // var array = new ItemType[];
                // array[0] = [Create argument0];
                // array[1] = [Create argument1];
                // ...
                argument.Generator.Emit(OpCodes.Ldc_I4, enumerableCallSite.ServiceCallSites.Length);
                argument.Generator.Emit(OpCodes.Newarr, enumerableCallSite.ItemType);
                for (int i = 0; i < enumerableCallSite.ServiceCallSites.Length; i++)
                {
                    // duplicate array
                    argument.Generator.Emit(OpCodes.Dup);
                    // push index
                    argument.Generator.Emit(OpCodes.Ldc_I4, i);
                    // create parameter
                    ServiceCallSite parameterCallSite = enumerableCallSite.ServiceCallSites[i];
                    VisitCallSite(parameterCallSite, argument);
                    if (parameterCallSite.ServiceType.IsValueType)
                    {
                        argument.Generator.Emit(OpCodes.Unbox_Any, parameterCallSite.ServiceType);
                    }

                    // store
                    argument.Generator.Emit(OpCodes.Stelem, enumerableCallSite.ItemType);
                }
            }

            return null;
        }

        protected override object? VisitFactory(FactoryCallSite factoryCallSite, ILEmitResolverBuilderContext argument)
        {
            argument.Factories ??= new List<Func<IServiceProvider, object>>();

            // this.Factories[i](ProviderScope)
            argument.Generator.Emit(OpCodes.Ldarg_0);
            argument.Generator.Emit(OpCodes.Ldfld, FactoriesField);

            argument.Generator.Emit(OpCodes.Ldc_I4, argument.Factories.Count);
            argument.Generator.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object>));

            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Call, ServiceLookupHelpers.InvokeFactoryMethodInfo);

            argument.Factories.Add(factoryCallSite.Factory);
            return null;
        }

        private static void AddConstant(ILEmitResolverBuilderContext argument, object? value)
        {
            argument.Constants ??= new List<object?>();

            // this.Constants[i]
            argument.Generator.Emit(OpCodes.Ldarg_0);
            argument.Generator.Emit(OpCodes.Ldfld, ConstantsField);

            argument.Generator.Emit(OpCodes.Ldc_I4, argument.Constants.Count);
            argument.Generator.Emit(OpCodes.Ldelem, typeof(object));
            argument.Constants.Add(value);
        }

        private static void AddCacheKey(ILEmitResolverBuilderContext argument, ServiceCacheKey key)
        {
            Debug.Assert(key.Type != null);

            // new ServiceCacheKey(typeof(key.Type), key.Slot)
            argument.Generator.Emit(OpCodes.Ldtoken, key.Type);
            argument.Generator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            argument.Generator.Emit(OpCodes.Ldc_I4, key.Slot);
            argument.Generator.Emit(OpCodes.Newobj, CacheKeyCtor);
        }

        private ILEmitResolverBuilderRuntimeContext GenerateMethodBody(ServiceCallSite callSite, ILGenerator generator)
        {
            var context = new ILEmitResolverBuilderContext(generator)
            {
                Constants = null,
                Factories = null
            };

            // if (scope.IsRootScope)
            // {
            //    return CallSiteRuntimeResolver.Instance.Resolve(callSite, scope);
            // }
            // var cacheKey = scopedCallSite.CacheKey;
            // object sync;
            // bool lockTaken;
            // object result;
            // try
            // {
            //    var resolvedServices = scope.ResolvedServices;
            //    sync = scope.Sync;
            //    Monitor.Enter(sync, ref lockTaken);
            //    if (!resolvedServices.TryGetValue(cacheKey, out result)
            //    {
            //       result = [createvalue];
            //       CaptureDisposable(result);
            //       resolvedServices.Add(cacheKey, result);
            //    }
            // }
            // finally
            // {
            //   if (lockTaken)
            //   {
            //      Monitor.Exit(sync);
            //   }
            // }
            // return result;

            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                LocalBuilder cacheKeyLocal = context.Generator.DeclareLocal(typeof(ServiceCacheKey));
                LocalBuilder resolvedServicesLocal = context.Generator.DeclareLocal(typeof(IDictionary<ServiceCacheKey, object>));
                LocalBuilder syncLocal = context.Generator.DeclareLocal(typeof(object));
                LocalBuilder lockTakenLocal = context.Generator.DeclareLocal(typeof(bool));
                LocalBuilder resultLocal = context.Generator.DeclareLocal(typeof(object));

                Label skipCreationLabel = context.Generator.DefineLabel();
                Label returnLabel = context.Generator.DefineLabel();
                Label defaultLabel = context.Generator.DefineLabel();

                // Check if scope IsRootScope
                context.Generator.Emit(OpCodes.Ldarg_1);
                context.Generator.Emit(OpCodes.Callvirt, ScopeIsRootScope);
                context.Generator.Emit(OpCodes.Brfalse_S, defaultLabel);

                context.Generator.Emit(OpCodes.Call, CallSiteRuntimeResolverInstanceField);
                AddConstant(context, callSite);
                context.Generator.Emit(OpCodes.Ldarg_1);
                context.Generator.Emit(OpCodes.Callvirt, CallSiteRuntimeResolverResolveMethod);
                context.Generator.Emit(OpCodes.Ret);

                // Generate cache key
                context.Generator.MarkLabel(defaultLabel);
                AddCacheKey(context, callSite.Cache.Key);
                // and store to local
                context.Generator.Emit(OpCodes.Stloc, cacheKeyLocal);

                context.Generator.BeginExceptionBlock();

                // scope
                context.Generator.Emit(OpCodes.Ldarg_1);
                // .ResolvedServices
                context.Generator.Emit(OpCodes.Callvirt, ResolvedServicesGetter);
                // Store resolved services
                context.Generator.Emit(OpCodes.Stloc, resolvedServicesLocal);

                // scope
                context.Generator.Emit(OpCodes.Ldarg_1);
                // .Sync
                context.Generator.Emit(OpCodes.Callvirt, ScopeLockGetter);
                // Store syncLocal
                context.Generator.Emit(OpCodes.Stloc, syncLocal);

                // Load syncLocal
                context.Generator.Emit(OpCodes.Ldloc, syncLocal);
                // Load address of lockTaken
                context.Generator.Emit(OpCodes.Ldloca, lockTakenLocal);
                // Monitor.Enter
                context.Generator.Emit(OpCodes.Call, ServiceLookupHelpers.MonitorEnterMethodInfo);

                // Load resolved services
                context.Generator.Emit(OpCodes.Ldloc, resolvedServicesLocal);
                // Load cache key
                context.Generator.Emit(OpCodes.Ldloc, cacheKeyLocal);
                // Load address of result local
                context.Generator.Emit(OpCodes.Ldloca, resultLocal);
                // .TryGetValue
                context.Generator.Emit(OpCodes.Callvirt, ServiceLookupHelpers.TryGetValueMethodInfo);

                // Jump to the end if already in cache
                context.Generator.Emit(OpCodes.Brtrue, skipCreationLabel);

                // Create value
                VisitCallSiteMain(callSite, context);
                context.Generator.Emit(OpCodes.Stloc, resultLocal);

                if (callSite.CaptureDisposable)
                {
                    BeginCaptureDisposable(context);
                    context.Generator.Emit(OpCodes.Ldloc, resultLocal);
                    EndCaptureDisposable(context);
                    // Pop value returned by CaptureDisposable off the stack
                    generator.Emit(OpCodes.Pop);
                }

                // load resolvedServices
                context.Generator.Emit(OpCodes.Ldloc, resolvedServicesLocal);
                // load cache key
                context.Generator.Emit(OpCodes.Ldloc, cacheKeyLocal);
                // load value
                context.Generator.Emit(OpCodes.Ldloc, resultLocal);
                // .Add
                context.Generator.Emit(OpCodes.Callvirt, ServiceLookupHelpers.AddMethodInfo);

                context.Generator.MarkLabel(skipCreationLabel);

                context.Generator.BeginFinallyBlock();

                // load lockTaken
                context.Generator.Emit(OpCodes.Ldloc, lockTakenLocal);
                // return if not
                context.Generator.Emit(OpCodes.Brfalse, returnLabel);
                // Load syncLocal
                context.Generator.Emit(OpCodes.Ldloc, syncLocal);
                // Monitor.Exit
                context.Generator.Emit(OpCodes.Call, ServiceLookupHelpers.MonitorExitMethodInfo);

                context.Generator.MarkLabel(returnLabel);

                context.Generator.EndExceptionBlock();

                // load value
                context.Generator.Emit(OpCodes.Ldloc, resultLocal);
                // return
                context.Generator.Emit(OpCodes.Ret);
            }
            else
            {
                VisitCallSite(callSite, context);
                // return
                context.Generator.Emit(OpCodes.Ret);
            }

            return new ILEmitResolverBuilderRuntimeContext
            {
                Constants = context.Constants?.ToArray(),
                Factories = context.Factories?.ToArray()
            };
        }

        private static void BeginCaptureDisposable(ILEmitResolverBuilderContext argument)
        {
            argument.Generator.Emit(OpCodes.Ldarg_1);
        }

        private static void EndCaptureDisposable(ILEmitResolverBuilderContext argument)
        {
            // When calling CaptureDisposable we expect callee and arguments to be on the stackcontext.Generator.BeginExceptionBlock
            argument.Generator.Emit(OpCodes.Callvirt, ServiceLookupHelpers.CaptureDisposableMethodInfo);
        }
    }
}
