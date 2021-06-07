// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ILEmitResolverBuilder : CallSiteVisitor<ILEmitResolverBuilderContext, object>
    {
        private static readonly MethodInfo ResolvedServicesGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.ResolvedServices), BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;

        private static readonly MethodInfo ScopeLockGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.Sync), BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;

        private static readonly FieldInfo FactoriesField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Factories));
        private static readonly FieldInfo ConstantsField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Constants));
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        private static readonly ConstructorInfo CacheKeyCtor = typeof(ServiceCacheKey).GetConstructors()[0];

        private sealed class ILEmitResolverBuilderRuntimeContext
        {
            public object[] Constants;
            public Func<IServiceProvider, object>[] Factories;
        }

        private struct GeneratedMethod
        {
            public Func<ServiceProviderEngineScope, object> Lambda;

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

        public Func<ServiceProviderEngineScope, object> Build(ServiceCallSite callSite)
        {
            return BuildType(callSite).Lambda;
        }

        private GeneratedMethod BuildType(ServiceCallSite callSite)
        {
            // Only scope methods are cached
            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
#if NETSTANDARD2_1
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, _buildTypeDelegate, callSite);
#else
                return _scopeResolverCache.GetOrAdd(callSite.Cache.Key, key => _buildTypeDelegate(key, callSite));
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

            var fileName = "Test" + DateTime.Now.Ticks;
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.RunAndSave);
            var module = assembly.DefineDynamicModule(assemblyName, assemblyName+".dll");
            var type = module.DefineType("Resolver");

            var method = type.DefineMethod(
                "ResolveService", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(object),
                new[] { typeof(ILEmitResolverBuilderRuntimeContext), typeof(ServiceProviderEngineScope) });

            GenerateMethodBody(callSite, method.GetILGenerator(), info);
            type.CreateTypeInfo();
            assembly.Save(assemblyName + ".dll");
#endif
            DependencyInjectionEventSource.Log.DynamicMethodBuilt(callSite.ServiceType, ilGenerator.ILOffset);

            return new GeneratedMethod()
            {
                Lambda = (Func<ServiceProviderEngineScope, object>)dynamicMethod.CreateDelegate(typeof(Func<ServiceProviderEngineScope, object>), runtimeContext),
                Context = runtimeContext,
                DynamicMethod = dynamicMethod
            };
        }


        protected override object VisitDisposeCache(ServiceCallSite transientCallSite, ILEmitResolverBuilderContext argument)
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

        protected override object VisitConstructor(ConstructorCallSite constructorCallSite, ILEmitResolverBuilderContext argument)
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
            return null;
        }

        protected override object VisitRootCache(ServiceCallSite callSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, CallSiteRuntimeResolver.Instance.Resolve(callSite, _rootScope));
            return null;
        }

        protected override object VisitScopeCache(ServiceCallSite scopedCallSite, ILEmitResolverBuilderContext argument)
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

        protected override object VisitConstant(ConstantCallSite constantCallSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, constantCallSite.DefaultValue);
            return null;
        }

        protected override object VisitServiceProvider(ServiceProviderCallSite serviceProviderCallSite, ILEmitResolverBuilderContext argument)
        {
            // [return] ProviderScope
            argument.Generator.Emit(OpCodes.Ldarg_1);
            return null;
        }

        protected override object VisitServiceScopeFactory(ServiceScopeFactoryCallSite serviceScopeFactoryCallSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, serviceScopeFactoryCallSite.Value);
            return null;
        }

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, ILEmitResolverBuilderContext argument)
        {
            if (enumerableCallSite.ServiceCallSites.Length == 0)
            {
                argument.Generator.Emit(OpCodes.Call, ExpressionResolverBuilder.GetArrayEmptyMethodInfo(enumerableCallSite.ItemType));
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

        protected override object VisitFactory(FactoryCallSite factoryCallSite, ILEmitResolverBuilderContext argument)
        {
            if (argument.Factories == null)
            {
                argument.Factories = new List<Func<IServiceProvider, object>>();
            }

            // this.Factories[i](ProviderScope)
            argument.Generator.Emit(OpCodes.Ldarg_0);
            argument.Generator.Emit(OpCodes.Ldfld, FactoriesField);

            argument.Generator.Emit(OpCodes.Ldc_I4, argument.Factories.Count);
            argument.Generator.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object>));

            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Call, ExpressionResolverBuilder.InvokeFactoryMethodInfo);

            argument.Factories.Add(factoryCallSite.Factory);
            return null;
        }

        private void AddConstant(ILEmitResolverBuilderContext argument, object value)
        {
            if (argument.Constants == null)
            {
                argument.Constants = new List<object>();
            }

            // this.Constants[i]
            argument.Generator.Emit(OpCodes.Ldarg_0);
            argument.Generator.Emit(OpCodes.Ldfld, ConstantsField);

            argument.Generator.Emit(OpCodes.Ldc_I4, argument.Constants.Count);
            argument.Generator.Emit(OpCodes.Ldelem, typeof(object));
            argument.Constants.Add(value);
        }

        private void AddCacheKey(ILEmitResolverBuilderContext argument, ServiceCacheKey key)
        {
            // new ServiceCacheKey(typeof(key.Type), key.Slot)
            argument.Generator.Emit(OpCodes.Ldtoken, key.Type);
            argument.Generator.Emit(OpCodes.Call, GetTypeFromHandleMethod);
            argument.Generator.Emit(OpCodes.Ldc_I4, key.Slot);
            argument.Generator.Emit(OpCodes.Newobj, CacheKeyCtor);
        }

        private ILEmitResolverBuilderRuntimeContext GenerateMethodBody(ServiceCallSite callSite, ILGenerator generator)
        {
            var context = new ILEmitResolverBuilderContext()
            {
                Generator = generator,
                Constants = null,
                Factories = null
            };

            //  var cacheKey = scopedCallSite.CacheKey;
            //  try
            //  {
            //    var resolvedServices = scope.ResolvedServices;
            //    Monitor.Enter(resolvedServices, out var lockTaken);
            //    if (!resolvedServices.TryGetValue(cacheKey, out value)
            //    {
            //       value = [createvalue];
            //       CaptureDisposable(value);
            //       resolvedServices.Add(cacheKey, value);
            //    }
            // }
            // finally
            // {
            //   if (lockTaken) Monitor.Exit(scope.ResolvedServices);
            // }
            // return value;

            if (callSite.Cache.Location == CallSiteResultCacheLocation.Scope)
            {
                LocalBuilder cacheKeyLocal = context.Generator.DeclareLocal(typeof(ServiceCacheKey));
                LocalBuilder resolvedServicesLocal = context.Generator.DeclareLocal(typeof(IDictionary<ServiceCacheKey, object>));
                LocalBuilder syncLocal = context.Generator.DeclareLocal(typeof(object));
                LocalBuilder lockTakenLocal = context.Generator.DeclareLocal(typeof(bool));
                LocalBuilder resultLocal = context.Generator.DeclareLocal(typeof(object));

                Label skipCreationLabel = context.Generator.DefineLabel();
                Label returnLabel = context.Generator.DefineLabel();

                // Generate cache key
                AddCacheKey(context, callSite.Cache.Key);
                // and store to local
                Stloc(context.Generator, cacheKeyLocal.LocalIndex);

                context.Generator.BeginExceptionBlock();

                // scope
                context.Generator.Emit(OpCodes.Ldarg_1);
                // .ResolvedServices
                context.Generator.Emit(OpCodes.Callvirt, ResolvedServicesGetter);
                // Store resolved services
                Stloc(context.Generator, resolvedServicesLocal.LocalIndex);

                // scope
                context.Generator.Emit(OpCodes.Ldarg_1);
                // .Sync
                context.Generator.Emit(OpCodes.Callvirt, ScopeLockGetter);
                // Store syncLocal
                Stloc(context.Generator, syncLocal.LocalIndex);

                // Load syncLocal
                Ldloc(context.Generator, syncLocal.LocalIndex);
                // Load address of lockTaken
                context.Generator.Emit(OpCodes.Ldloca_S, lockTakenLocal.LocalIndex);
                // Monitor.Enter
                context.Generator.Emit(OpCodes.Call, ExpressionResolverBuilder.MonitorEnterMethodInfo);

                // Load resolved services
                Ldloc(context.Generator, resolvedServicesLocal.LocalIndex);
                // Load cache key
                Ldloc(context.Generator, cacheKeyLocal.LocalIndex);
                // Load address of result local
                context.Generator.Emit(OpCodes.Ldloca_S, resultLocal.LocalIndex);
                // .TryGetValue
                context.Generator.Emit(OpCodes.Callvirt, ExpressionResolverBuilder.TryGetValueMethodInfo);

                // Jump to the end if already in cache
                context.Generator.Emit(OpCodes.Brtrue, skipCreationLabel);

                // Create value
                VisitCallSiteMain(callSite, context);
                Stloc(context.Generator, resultLocal.LocalIndex);

                if (callSite.CaptureDisposable)
                {
                    BeginCaptureDisposable(context);
                    Ldloc(context.Generator, resultLocal.LocalIndex);
                    EndCaptureDisposable(context);
                    // Pop value returned by CaptureDisposable off the stack
                    generator.Emit(OpCodes.Pop);
                }

                // load resolvedServices
                Ldloc(context.Generator, resolvedServicesLocal.LocalIndex);
                // load cache key
                Ldloc(context.Generator, cacheKeyLocal.LocalIndex);
                // load value
                Ldloc(context.Generator, resultLocal.LocalIndex);
                // .Add
                context.Generator.Emit(OpCodes.Callvirt, ExpressionResolverBuilder.AddMethodInfo);

                context.Generator.MarkLabel(skipCreationLabel);

                context.Generator.BeginFinallyBlock();

                // load lockTaken
                Ldloc(context.Generator, lockTakenLocal.LocalIndex);
                // return if not
                context.Generator.Emit(OpCodes.Brfalse, returnLabel);
                // Load syncLocal
                Ldloc(context.Generator, syncLocal.LocalIndex);
                // Monitor.Exit
                context.Generator.Emit(OpCodes.Call, ExpressionResolverBuilder.MonitorExitMethodInfo);

                context.Generator.MarkLabel(returnLabel);

                context.Generator.EndExceptionBlock();


                // load value
                Ldloc(context.Generator, resultLocal.LocalIndex);
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
            // Call CaptureDisposabl we expect calee and arguments to be on the stackcontext.Generator.BeginExceptionBlock
            argument.Generator.Emit(OpCodes.Callvirt, ExpressionResolverBuilder.CaptureDisposableMethodInfo);
        }

        private void Ldloc(ILGenerator generator, int index)
        {
            switch (index)
            {
                case 0: generator.Emit(OpCodes.Ldloc_0);
                    return;
                case 1: generator.Emit(OpCodes.Ldloc_1);
                    return;
                case 2: generator.Emit(OpCodes.Ldloc_2);
                    return;
                case 3: generator.Emit(OpCodes.Ldloc_3);
                    return;
            }

            if (index < byte.MaxValue)
            {
                generator.Emit(OpCodes.Ldloc_S, (byte)index);
                return;
            }

            generator.Emit(OpCodes.Ldloc, index);
        }

        private void Stloc(ILGenerator generator, int index)
        {
            switch (index)
            {
                case 0: generator.Emit(OpCodes.Stloc_0);
                    return;
                case 1: generator.Emit(OpCodes.Stloc_1);
                    return;
                case 2: generator.Emit(OpCodes.Stloc_2);
                    return;
                case 3: generator.Emit(OpCodes.Stloc_3);
                    return;
            }

            if (index < byte.MaxValue)
            {
                generator.Emit(OpCodes.Stloc_S, (byte)index);
                return;
            }

            generator.Emit(OpCodes.Stloc, index);
        }
    }
}
