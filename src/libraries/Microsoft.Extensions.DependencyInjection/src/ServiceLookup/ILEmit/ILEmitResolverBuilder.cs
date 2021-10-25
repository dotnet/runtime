// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ILEmitResolverBuilder : CallSiteVisitor<ILEmitResolverBuilderContext, object>
    {
        private static readonly MethodInfo ResolvedServicesGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.ResolvedServices), BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;

        private static readonly MethodInfo ScopeLockGetter = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.Sync), BindingFlags.Instance | BindingFlags.NonPublic).GetMethod;

        private static readonly MethodInfo ScopeIsRootScope = typeof(ServiceProviderEngineScope).GetProperty(
            nameof(ServiceProviderEngineScope.IsRootScope), BindingFlags.Instance | BindingFlags.Public).GetMethod;

        private static readonly MethodInfo CallSiteRuntimeResolverResolveMethod = typeof(CallSiteRuntimeResolver).GetMethod(
            nameof(CallSiteRuntimeResolver.Resolve), BindingFlags.Public | BindingFlags.Instance);

        private static readonly MethodInfo CallSiteRuntimeResolverInstanceField = typeof(CallSiteRuntimeResolver).GetProperty(
            nameof(CallSiteRuntimeResolver.Instance), BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance).GetMethod;

        private static readonly FieldInfo FactoriesField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Factories));
        private static readonly FieldInfo ConstantsField = typeof(ILEmitResolverBuilderRuntimeContext).GetField(nameof(ILEmitResolverBuilderRuntimeContext.Constants));
        private static readonly MethodInfo GetTypeFromHandleMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle));

        private static readonly ConstructorInfo CacheKeyCtor = typeof(ServiceCacheKey).GetConstructors()[0];

        private sealed class ILEmitResolverBuilderRuntimeContext
        {
            public object[] Constants;
            public Func<IServiceProvider, object>[] Factories;
        }

        private struct GeneratedFactory
        {
            public ServiceFactory Factory;

            public MethodInfo CreateMethod;
        }

        private readonly ServiceProviderEngineScope _rootScope;

        private readonly ConcurrentDictionary<ServiceCacheKey, GeneratedFactory> _scopeResolverCache;

        private readonly Func<ServiceCacheKey, ServiceCallSite, GeneratedFactory> _buildTypeDelegate;

        public ILEmitResolverBuilder(ServiceProvider serviceProvider)
        {
            _rootScope = serviceProvider.Root;
            _scopeResolverCache = new ConcurrentDictionary<ServiceCacheKey, GeneratedFactory>();
            _buildTypeDelegate = (key, cs) => BuildTypeNoCache(cs);
        }

        public ServiceFactory Build(ServiceCallSite callSite)
        {
            return BuildType(callSite).Factory;
        }

        private GeneratedFactory BuildType(ServiceCallSite callSite)
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

        private GeneratedFactory BuildTypeNoCache(ServiceCallSite callSite)
        {
            // We need to skip visibility checks because services/constructors might be private
            var assemblyName = $"{callSite.ServiceType.Name}_{DateTime.UtcNow.Ticks}_ServiceFactoryAssembly";

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName),
#if SAVE_ASSEMBLIES
                AssemblyBuilderAccess.RunAndSave
#else
                AssemblyBuilderAccess.Run
#endif
                );

            var createMethod = typeof(ServiceFactory).GetMethod(nameof(ServiceFactory.Create));

            // This is a bit insane but we need to walk the types to mark them all as skipping visibility
            void IgnoreAccessChecksToType(Type type, HashSet<Assembly> assemblies)
            {
                if (type is null)
                {
                    return;
                }

                if (assemblies.Add(type.Assembly))
                {
                    IgnoreAccessChecks(type.Assembly);
                }

                if (!type.IsGenericType)
                {
                    return;
                }

                foreach (var t in type.GetGenericArguments())
                {
                    IgnoreAccessChecksToType(t, assemblies);
                }
            }

            void IgnoreAccessChecks(Assembly ignoreAccessChecksAssembly)
            {
                if (ignoreAccessChecksAssembly is null)
                {
                    return;
                }

                var ignoresAccessChecksTo = new CustomAttributeBuilder
                (
                    typeof(IgnoresAccessChecksToAttribute).GetConstructor(new Type[] { typeof(string) }),
                    new object[] { ignoreAccessChecksAssembly.GetName().Name }
                );
                assemblyBuilder.SetCustomAttribute(ignoresAccessChecksTo);
            }

            var assemblies = new HashSet<Assembly>();
            IgnoreAccessChecksToType(typeof(ServiceFactory), assemblies);
            IgnoreAccessChecksToType(callSite.ServiceType, assemblies);
            IgnoreAccessChecksToType(callSite.ImplementationType, assemblies);

            var module = assemblyBuilder.DefineDynamicModule(assemblyName);
            var serviceTypeBuilder = module.DefineType(callSite.ServiceType.Name + "ServiceFactory", TypeAttributes.Public | TypeAttributes.Sealed, typeof(ServiceFactory));

            var f1 = serviceTypeBuilder.DefineField("_resolverContext", typeof(ILEmitResolverBuilderRuntimeContext), FieldAttributes.Private);
            var createParameters = createMethod.GetParameters();

            var parameterTypes = new Type[createParameters.Length];
            for (int i = 0; i < createParameters.Length; i++)
            {
                parameterTypes[i] = createParameters[i].ParameterType;
            }

            var createMethodOverride = serviceTypeBuilder.DefineMethod(
                createMethod.Name,
                MethodAttributes.Private | MethodAttributes.HideBySig |
                MethodAttributes.NewSlot | MethodAttributes.Virtual |
                MethodAttributes.Final, createMethod.ReturnType,
                parameterTypes);

            // In traces we've seen methods range from 100B - 4K sized methods since we've
            // stop trying to inline everything into scoped methods. We'll pay for a couple of resizes
            // so there'll be allocations but we could potentially change ILGenerator to use the array pool
            ILGenerator ilGenerator = createMethodOverride.GetILGenerator(512);

            ILEmitResolverBuilderRuntimeContext runtimeContext = GenerateMethodBody(callSite, ilGenerator, f1, out var dependentAssemblies);

            if (dependentAssemblies is not null)
            {
                foreach (var a in dependentAssemblies)
                {
                    IgnoreAccessChecks(a);
                }
            }

            serviceTypeBuilder.DefineMethodOverride(createMethodOverride, createMethod);

            var type = serviceTypeBuilder.CreateType();

            var factory = (ServiceFactory)Activator.CreateInstance(type);

            // Set the field
            type.GetField("_resolverContext", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(factory, runtimeContext);

#if SAVE_ASSEMBLIES
            // Assembly.Save is only available in .NET Framework (https://github.com/dotnet/runtime/issues/15704)
            assembly.Save(assemblyName + ".dll");
#endif
            DependencyInjectionEventSource.Log.DynamicMethodBuilt(_rootScope.RootProvider, callSite.ServiceType, ilGenerator.ILOffset);

            return new GeneratedFactory()
            {
                Factory = factory,
                CreateMethod = createMethodOverride
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
            if (constructorCallSite.ImplementationType.IsValueType)
            {
                argument.Generator.Emit(OpCodes.Box, constructorCallSite.ImplementationType);
            }

            return null;
        }

        protected override object VisitRootCache(ServiceCallSite callSite, ILEmitResolverBuilderContext argument)
        {
            AddConstant(argument, CallSiteRuntimeResolver.Instance.Resolve(callSite, _rootScope));
            return null;
        }

        protected override object VisitScopeCache(ServiceCallSite scopedCallSite, ILEmitResolverBuilderContext argument)
        {
            GeneratedFactory generatedFactory = BuildType(scopedCallSite);

            argument.Assemblies ??= new();
            argument.Assemblies.Add(generatedFactory.Factory.GetType().Assembly);

            AddConstant(argument, generatedFactory.Factory);
            // ProviderScope
            argument.Generator.Emit(OpCodes.Castclass, typeof(ServiceFactory));
            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Callvirt, generatedFactory.CreateMethod);

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

        protected override object VisitIEnumerable(IEnumerableCallSite enumerableCallSite, ILEmitResolverBuilderContext argument)
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

        protected override object VisitFactory(FactoryCallSite factoryCallSite, ILEmitResolverBuilderContext argument)
        {
            if (argument.Factories == null)
            {
                argument.Factories = new List<Func<IServiceProvider, object>>();
            }

            // this.Factories[i](ProviderScope)
            argument.Generator.Emit(OpCodes.Ldarg_0);
            argument.Generator.Emit(OpCodes.Ldfld, argument.RuntimeContextField);
            argument.Generator.Emit(OpCodes.Ldfld, FactoriesField);

            argument.Generator.Emit(OpCodes.Ldc_I4, argument.Factories.Count);
            argument.Generator.Emit(OpCodes.Ldelem, typeof(Func<IServiceProvider, object>));

            argument.Generator.Emit(OpCodes.Ldarg_1);
            argument.Generator.Emit(OpCodes.Call, ServiceLookupHelpers.InvokeFactoryMethodInfo);

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
            argument.Generator.Emit(OpCodes.Ldfld, argument.RuntimeContextField);
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

        private ILEmitResolverBuilderRuntimeContext GenerateMethodBody(ServiceCallSite callSite, ILGenerator generator, FieldInfo runtimeResolverContextField, out HashSet<Assembly> assemblies)
        {
            var context = new ILEmitResolverBuilderContext()
            {
                Generator = generator,
                Constants = null,
                Factories = null,
                RuntimeContextField = runtimeResolverContextField
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

            assemblies = context.Assemblies;

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

// The runtime understands this attribute, but it is not defined in the BCL so you have to define it yourself.
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName)
            => AssemblyName = assemblyName;

        public string AssemblyName { get; }
    }
}
