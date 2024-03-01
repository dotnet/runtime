// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using System.Runtime.CompilerServices;

#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Microsoft.Extensions.DependencyInjection.Tests
{
    public class ActivatorUtilitiesTests
    {
        [Fact]
        public void CreateInstance_ClassWithABCS_UsesTheLongestAvailableConstructor()
        {
            var services = new ServiceCollection();
            services.AddScoped<B>();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, a, c);

            Assert.NotNull(instance.B);
            Assert.NotNull(instance.S);
            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
        }

        [Fact]
        public void CreateInstance_OneCtor_IsRegistered_CreatesInstanceSuccessfully()
        {
            var services = new ServiceCollection();
            services.AddScoped<A>();
            using var provider = services.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance<ClassWithA>(provider);
            Assert.NotNull(instance.A);
        }

        [Fact]
        public void CreateInstance_BadlyConfiguredIServiceProviderIsService_ProperlyCreatesUnambiguousInstance()
        {
            var serviceCollection = new ServiceCollection()
                .AddScoped<A>();
            var fakeServiceProvider = new FakeServiceProvider();
            fakeServiceProvider.Populate(serviceCollection);
            fakeServiceProvider.Build();

            var instance = ActivatorUtilities.CreateInstance<ClassWithA>(fakeServiceProvider);

            Assert.NotNull(instance);
            Assert.True(fakeServiceProvider.FakeServiceProviderIsService.IsServiceGotCalled);
        }

        [Theory]
        [InlineData(typeof(ABCS1))]
        [InlineData(typeof(ABCS2))]
        [InlineData(typeof(ABCS3))]
        public void CreateInstance_DifferentOrders_CreatesInstanceSuccessfully(Type type)
        {
            var services = new ServiceCollection();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            B b = new B();
            C c = new C();

            var instance = (ABCS)ActivatorUtilities.CreateInstance(provider, type, b, c);
            Assert.Null(instance.A);
            Assert.Same(b, instance.B);
            Assert.Same(c, instance.C);
            Assert.NotNull(instance.S);
        }

        [Fact]
        public void CreateInstance_NullInstance_HandlesBadInputWithInvalidOperationException()
        {
            var serviceCollection = new ServiceCollection()
                .AddScoped<S>();
            var serviceProvider = serviceCollection.BuildServiceProvider();
            B? nullB = null;

            Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateInstance<ABCS1>(serviceProvider, nullB!, new C()));
        }

        [Fact]
        public void TypeActivatorThrowsOnNullProvider()
        {
            Assert.Throws<ArgumentNullException>(() => ActivatorUtilities.CreateInstance<ClassWithABCS>(null, "hello"));
        }

        [Fact]
        public void FactoryActivatorThrowsOnNullProvider()
        {
            var f = ActivatorUtilities.CreateFactory(typeof(ClassWithA), new Type[0]);
            Exception ex = Assert.Throws<ArgumentNullException>(() => f(serviceProvider: null, null));
            Assert.Contains("serviceProvider", ex.ToString());
        }

        [Fact]
        public void CreateInstance_ClassWithABCS_UsesTheLongestAvailableConstructor_ParameterOrderDoesntMatter()
        {
            var services = new ServiceCollection();
            services.AddScoped<B>();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, c, a);

            Assert.NotNull(instance.B);
            Assert.NotNull(instance.S);
            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
        }

        [Theory]
        [InlineData(typeof(ClassWithABC_DefaultConstructorFirst))]
        [InlineData(typeof(ClassWithABC_DefaultConstructorLast))]
        public void CreateInstance_ClassWithABC_ChoosesDefaultConstructorNoMatterCtorOrder(Type instanceType)
        {
            var services = new ServiceCollection();
            using var provider = services.BuildServiceProvider();

            var instance = ActivatorUtilities.CreateInstance(provider, instanceType) as ClassWithABC;

            Assert.NotNull(instance);
            Assert.Null(instance.A);
            Assert.Null(instance.B);
        }

        [Fact]
        public void CreateInstance_ClassWithABCS_BNotRegistered_UsesLongestPossibleCtorTakingAllRegisteredAndPassedParameters()
        {
            var services = new ServiceCollection();
            services.AddScoped<S>();
            using var provider = services.BuildServiceProvider();
            var a = new A();
            var c = new C();

            var instance = ActivatorUtilities.CreateInstance<ClassWithABCS>(provider, c, a);

            Assert.Same(a, instance.A);
            Assert.Same(c, instance.C);
            Assert.NotNull(instance.S);
            Assert.Null(instance.B);
        }

        [Theory]
        [InlineData(typeof(ClassWithABC_FirstConstructorWithAttribute))]
        [InlineData(typeof(ClassWithABC_LastConstructorWithAttribute))]
        public void CreateInstance_ClassWithABC_ConstructorWithAttribute_PicksCtorWithAttr_NoMatterDefinitionOrder(Type instanceType)
        {
            var services = new ServiceCollection();
            var a = new A();
            services.AddSingleton(a);
            using var provider = services.BuildServiceProvider();

            var instance = (ClassWithABC)ActivatorUtilities.CreateInstance(provider, instanceType, new B(), new C());

            Assert.Same(a, instance.A);
        }

        [Fact]
        public void CreateInstanceFailsWithAmbiguousConstructor()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithA_And_B>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Neither ctor(A) nor ctor(B) have [ActivatorUtilitiesConstructor].
            Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance<ClassWithA_And_B>(serviceProvider));
        }

        [Fact]
        public void CreateInstanceFailsWithAmbiguousConstructor_ReversedOrder()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithB_And_A>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Neither ctor(A) nor ctor(B) have [ActivatorUtilitiesConstructor].
            Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance<ClassWithA_And_B>(serviceProvider));
        }

        [Fact]
        public void CreateInstancePassesWithAmbiguousConstructor()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithA_And_B_ActivatorUtilitiesConstructorAttribute>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var service = ActivatorUtilities.CreateInstance<ClassWithA_And_B_ActivatorUtilitiesConstructorAttribute>(serviceProvider);

            // Ensure ctor(A) was selected over ctor(B) since A has [ActivatorUtilitiesConstructor].
            Assert.NotNull(service.A);
        }

        [Fact]
        public void CreateInstancePassesWithAmbiguousConstructor_ReversedOrder()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithB_And_A_ActivatorUtilitiesConstructorAttribute>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var service = ActivatorUtilities.CreateInstance<ClassWithB_And_A_ActivatorUtilitiesConstructorAttribute>(serviceProvider);

            // Ensure ctor(A) was selected over ctor(B) since A has [ActivatorUtilitiesConstructor].
            Assert.NotNull(service.A);
        }

        [Fact]
        public void CreateInstanceIgnoresActivatorUtilitiesConstructorAttribute()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithA_And_AB_ActivatorUtilitiesConstructorAttribute>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var service = ActivatorUtilities.CreateInstance<ClassWithA_And_AB_ActivatorUtilitiesConstructorAttribute>(serviceProvider);

            // Ensure ctor(AB) was selected even though ctor(A) had [ActivatorUtilitiesConstructor].
            // Longer constructors are selected if they appear after the one with [ActivatorUtilitiesConstructor].
            Assert.NotNull(service.A);
            Assert.NotNull(service.B);
        }

        [Fact]
        public void CreateInstanceIgnoresActivatorUtilitiesConstructorAttribute_ReversedOrder()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddTransient<ClassWithAB_And_A_ActivatorUtilitiesConstructorAttribute>();
            serviceCollection.AddTransient<A>();
            serviceCollection.AddTransient<B>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var service = ActivatorUtilities.CreateInstance<ClassWithAB_And_A_ActivatorUtilitiesConstructorAttribute>(serviceProvider);

            // Ensure ctor(A) was selected since it has [ActivatorUtilitiesConstructor]. It exists after ctor(AB).
            Assert.NotNull(service.A);
            Assert.Null(service.B);
        }

        [Fact]
        public void CreateInstance_ClassWithABC_MultipleCtorsWithSameLength_ThrowsAmbiguous()
        {
            string message = $"Multiple constructors for type '{typeof(ClassWithABC_MultipleCtorsWithSameLength)}' were found with length 1.";
            var services = new ServiceCollection();
            var a = new A();
            var b = new B();
            services.AddSingleton(a);
            services.AddSingleton(b);
            using var provider = services.BuildServiceProvider();

            var exception = Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateInstance<ClassWithABC_MultipleCtorsWithSameLength>(provider));
            Assert.Equal(message, exception.Message);
        }

        [Fact]
        public void CreateFactory_CreatesFactoryMethod_4Types_3Injected()
        {
            var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithABCS), new Type[] { typeof(B) });
            var factory2 = ActivatorUtilities.CreateFactory<ClassWithABCS>(new Type[] { typeof(B) });

            var services = new ServiceCollection();
            services.AddSingleton(new A());
            services.AddSingleton(new C());
            services.AddSingleton(new S());
            using var provider = services.BuildServiceProvider();
            object item1 = factory1(provider, new[] { new B() });
            var item2 = factory2(provider, new[] { new B() });

            Assert.IsType<ObjectFactory>(factory1);
            Assert.IsType<ClassWithABCS>(item1);
            ClassWithABCS obj = (ClassWithABCS)item1;
            Assert.NotNull(obj.A);
            Assert.NotNull(obj.B);
            Assert.NotNull(obj.C);
            Assert.NotNull(obj.S);

            Assert.IsType<ObjectFactory<ClassWithABCS>>(factory2);
            Assert.IsType<ClassWithABCS>(item2);

            Assert.NotNull(item2.A);
            Assert.NotNull(item2.B);
            Assert.NotNull(item2.C);
            Assert.NotNull(item2.S);
        }

        [Fact]
        public void CreateFactory_CreatesFactoryMethod_5Types_5Injected()
        {
            // Inject 5 types which is a threshold for whether fixed or Span<> invoker args are used by reflection.
            var factory = ActivatorUtilities.CreateFactory<ClassWithABCSZ>(Type.EmptyTypes);

            var services = new ServiceCollection();
            services.AddSingleton(new A());
            services.AddSingleton(new B());
            services.AddSingleton(new C());
            services.AddSingleton(new S());
            services.AddSingleton(new Z());
            using var provider = services.BuildServiceProvider();
            ClassWithABCSZ item = factory(provider, null);

            Assert.IsType<ObjectFactory<ClassWithABCSZ>>(factory);
            Assert.NotNull(item.A);
            Assert.NotNull(item.B);
            Assert.NotNull(item.C);
            Assert.NotNull(item.S);
            Assert.NotNull(item.Z);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_CreatesFactoryMethod_KeyedParams(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory = ActivatorUtilities.CreateFactory<ClassWithAKeyedBKeyedC>(Type.EmptyTypes);

                var services = new ServiceCollection();
                services.AddSingleton(new A());
                services.AddKeyedSingleton("b", new B());
                services.AddKeyedSingleton("c", new C());
                using var provider = services.BuildServiceProvider();
                ClassWithAKeyedBKeyedC item = factory(provider, null);

                Assert.IsType<ObjectFactory<ClassWithAKeyedBKeyedC>>(factory);
                Assert.NotNull(item.A);
                Assert.NotNull(item.B);
                Assert.NotNull(item.C);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_CreatesFactoryMethod_KeyedParams_5Types(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory = ActivatorUtilities.CreateFactory<ClassWithAKeyedBKeyedCSZ>(Type.EmptyTypes);

                var services = new ServiceCollection();
                services.AddSingleton(new A());
                services.AddKeyedSingleton("b", new B());
                services.AddKeyedSingleton("c", new C());
                services.AddSingleton(new S());
                services.AddSingleton(new Z());
                using var provider = services.BuildServiceProvider();
                ClassWithAKeyedBKeyedCSZ item = factory(provider, null);

                Assert.IsType<ObjectFactory<ClassWithAKeyedBKeyedCSZ>>(factory);
                Assert.NotNull(item.A);
                Assert.NotNull(item.B);
                Assert.NotNull(item.C);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_CreatesFactoryMethod_KeyedParams_1Injected(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory = ActivatorUtilities.CreateFactory<ClassWithAKeyedBKeyedC>(new Type[] { typeof(A) });

                var services = new ServiceCollection();
                services.AddKeyedSingleton("b", new B());
                services.AddKeyedSingleton("c", new C());
                using var provider = services.BuildServiceProvider();
                ClassWithAKeyedBKeyedC item = factory(provider, new object?[] { new A() });

                Assert.IsType<ObjectFactory<ClassWithAKeyedBKeyedC>>(factory);
                Assert.NotNull(item.A);
                Assert.NotNull(item.B);
                Assert.NotNull(item.C);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_CreatesFactoryMethod(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithABCS), new Type[] { typeof(B) });
                var factory2 = ActivatorUtilities.CreateFactory<ClassWithABCS>(new Type[] { typeof(B) });

                var services = new ServiceCollection();
                services.AddSingleton(new A());
                services.AddSingleton(new C());
                services.AddSingleton(new S());
                using var provider = services.BuildServiceProvider();
                object item1 = factory1(provider, new[] { new B() });
                var item2 = factory2(provider, new[] { new B() });

                Assert.IsType<ObjectFactory>(factory1);
                Assert.IsType<ClassWithABCS>(item1);

                Assert.IsType<ObjectFactory<ClassWithABCS>>(factory2);
                Assert.IsType<ClassWithABCS>(item2);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_NullArguments_Throws(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithA), new Type[] { typeof(A) });

                var services = new ServiceCollection();
                using var provider = services.BuildServiceProvider();
                Assert.Throws<NullReferenceException>(() => factory1(provider, null));
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_NoArguments_UseNullDefaultValue(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithADefaultValue), new Type[0]);

                var services = new ServiceCollection();
                using var provider = services.BuildServiceProvider();
                var item = (ClassWithADefaultValue)factory1(provider, null);
                Assert.Null(item.A);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_NoArguments_ThrowRequiredValue(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithA), new Type[0]);

                var services = new ServiceCollection();
                using var provider = services.BuildServiceProvider();
                var ex = Assert.Throws<InvalidOperationException>(() => factory1(provider, null));
                Assert.Equal($"Unable to resolve service for type '{typeof(A).FullName}' while attempting to activate '{typeof(ClassWithA).FullName}'.", ex.Message);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_NullArgument_UseDefaultValue(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(ClassWithStringDefaultValue), new[] { typeof(string) });

                var services = new ServiceCollection();
                using var provider = services.BuildServiceProvider();
                var item = (ClassWithStringDefaultValue)factory1(provider, new object[] { null });
                Assert.Equal("DEFAULT", item.Text);
            }, options);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
#if NETCOREAPP
        [InlineData(false)]
#endif
        public void CreateFactory_RemoteExecutor_NoParameters_Success(bool useDynamicCode)
        {
            var options = new RemoteInvokeOptions();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                var factory1 = ActivatorUtilities.CreateFactory(typeof(A), new Type[0]);

                var services = new ServiceCollection();
                using var provider = services.BuildServiceProvider();
                var item = (A)factory1(provider, null);
                Assert.NotNull(item);
            }, options);
        }

#if NETCOREAPP
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34072", TestRuntimes.Mono)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateInstance_CollectibleAssembly(bool useDynamicCode)
        {
            if (PlatformDetection.IsNonBundledAssemblyLoadingSupported)
            {
                RemoteInvokeOptions options = new();
                if (!useDynamicCode)
                {
                    DisableDynamicCode(options);
                }

                using var remoteHandle = RemoteExecutor.Invoke(static () =>
                {
                    Assert.False(Collectible_IsAssemblyLoaded());
                    Collectible_LoadAndCreate(useCollectibleAssembly : true, out WeakReference asmWeakRef, out WeakReference typeWeakRef);

                    for (int i = 0; (typeWeakRef.IsAlive || asmWeakRef.IsAlive) && (i < 10); i++)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }

                    // These should be GC'd.
                    Assert.False(asmWeakRef.IsAlive, "asmWeakRef.IsAlive");
                    Assert.False(typeWeakRef.IsAlive, "typeWeakRef.IsAlive");
                    Assert.False(Collectible_IsAssemblyLoaded());
                }, options);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateInstance_NormalAssembly(bool useDynamicCode)
        {
            RemoteInvokeOptions options = new();
            if (!useDynamicCode)
            {
                DisableDynamicCode(options);
            }

            using var remoteHandle = RemoteExecutor.Invoke(static () =>
            {
                Assert.False(Collectible_IsAssemblyLoaded());
                Collectible_LoadAndCreate(useCollectibleAssembly: false, out WeakReference asmWeakRef, out WeakReference typeWeakRef);

                for (int i = 0; (typeWeakRef.IsAlive || asmWeakRef.IsAlive) && (i < 10); i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                // These will not be GC'd.
                Assert.True(asmWeakRef.IsAlive, "alcWeakRef.IsAlive");
                Assert.True(typeWeakRef.IsAlive, "typeWeakRef.IsAlive");
                Assert.True(Collectible_IsAssemblyLoaded());
            }, options);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Collectible_LoadAndCreate(bool useCollectibleAssembly, out WeakReference asmWeakRef, out WeakReference typeWeakRef)
        {
            Assembly asm;
            object obj;

            if (useCollectibleAssembly)
            {
                asm = MyLoadContext.LoadAsCollectable();
                obj = CreateWithActivator(asm);
                Assert.True(obj.GetType().Assembly.IsCollectible);
            }
            else
            {
                asm = MyLoadContext.LoadNormal();
                obj = CreateWithActivator(asm);
                Assert.False(obj.GetType().Assembly.IsCollectible);
            }

            Assert.True(Collectible_IsAssemblyLoaded());
            asmWeakRef = new WeakReference(asm);
            typeWeakRef = new WeakReference(obj.GetType());

            static object CreateWithActivator(Assembly asm)
            {
                Type t = asm.GetType("CollectibleAssembly.ClassToCreate");
                MethodInfo mi = t.GetMethod("Create", BindingFlags.Static | BindingFlags.Public, new Type[] { typeof(ServiceProvider) });

                object instance;
                ServiceCollection services = new();
                using (ServiceProvider provider = services.BuildServiceProvider())
                {
                    instance = mi.Invoke(null, new object[] { provider });
                }

                return instance;
            }
        }

        static bool Collectible_IsAssemblyLoaded()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Assembly asm = assemblies[i];
                string asmName = Path.GetFileName(asm.Location);
                if (asmName == "CollectibleAssembly.dll")
                {
                    return true;
                }
            }

            return false;
        }
#endif

        private static void DisableDynamicCode(RemoteInvokeOptions options)
        {
            // We probably only need to set 'IsDynamicCodeCompiled' since only that is checked,
            // but also set 'IsDynamicCodeSupported for correctness.
            options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", "false");
            options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeCompiled", "false");
        }
    }

    internal class A { }
    internal class B { }
    internal class C { }
    internal class S { }
    internal class Z { }

    internal class ClassWithAKeyedBKeyedC : ClassWithABC
    {
        public ClassWithAKeyedBKeyedC(A a, [FromKeyedServices("b")] B b, [FromKeyedServices("c")] C c)
            : base(a, b, c)
        { }
    }

    internal class ClassWithABCS : ClassWithABC
    {
        public S S { get; }
        public ClassWithABCS(A a, B b, C c, S s) : base(a, b, c) { S = s; }
        public ClassWithABCS(A a, C c, S s) : this(a, null, c, s) { }
    }

    internal class ClassWithABCSZ : ClassWithABCS
    {
        public Z Z { get; }
        public ClassWithABCSZ(A a, B b, C c, S s, Z z) : base(a, b, c, s) { Z = z; }
    }

    internal class ClassWithAKeyedBKeyedCSZ : ClassWithABCSZ
    {
        public ClassWithAKeyedBKeyedCSZ(A a, [FromKeyedServices("b")] B b, [FromKeyedServices("c")] C c, S s, Z z)
            : base(a, b, c, s, z)
        { }
    }

    internal class ClassWithABC_FirstConstructorWithAttribute : ClassWithABC
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithABC_FirstConstructorWithAttribute(A a, B b, C c) : base(a, b, c) { }
        public ClassWithABC_FirstConstructorWithAttribute(B b, C c) : this(null, b, c) { }
    }

    internal class ClassWithABC_LastConstructorWithAttribute : ClassWithABC
    {
        public ClassWithABC_LastConstructorWithAttribute(B b, C c) : this(null, b, c) { }
        [ActivatorUtilitiesConstructor]
        public ClassWithABC_LastConstructorWithAttribute(A a, B b, C c) : base(a, b, c) { }
    }

    internal class ClassWithA_And_B
    {
        public ClassWithA_And_B(A a)
        {
            A = a;
        }

        public ClassWithA_And_B(B b)
        {
            B = b;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class ClassWithB_And_A
    {
        public ClassWithB_And_A(A a)
        {
            A = a;
        }

        public ClassWithB_And_A(B b)
        {
            B = b;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class ClassWithA_And_B_ActivatorUtilitiesConstructorAttribute
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithA_And_B_ActivatorUtilitiesConstructorAttribute(A a)
        {
            A = a;
        }

        public ClassWithA_And_B_ActivatorUtilitiesConstructorAttribute(B b)
        {
            B = b;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class ClassWithB_And_A_ActivatorUtilitiesConstructorAttribute
    {
        public ClassWithB_And_A_ActivatorUtilitiesConstructorAttribute(B b)
        {
            B = b;
        }

        [ActivatorUtilitiesConstructor]
        public ClassWithB_And_A_ActivatorUtilitiesConstructorAttribute(A a)
        {
            A = a;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class ClassWithA_And_AB_ActivatorUtilitiesConstructorAttribute
    {
        [ActivatorUtilitiesConstructor]
        public ClassWithA_And_AB_ActivatorUtilitiesConstructorAttribute(A a)
        {
            A = a;
        }

        public ClassWithA_And_AB_ActivatorUtilitiesConstructorAttribute(A a, B b)
        {
            A = a;
            B = b;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class ClassWithAB_And_A_ActivatorUtilitiesConstructorAttribute
    {
        public ClassWithAB_And_A_ActivatorUtilitiesConstructorAttribute(A a, B b)
        {
            A = a;
            B = b;
        }

        [ActivatorUtilitiesConstructor]
        public ClassWithAB_And_A_ActivatorUtilitiesConstructorAttribute(A a)
        {
            A = a;
        }

        public A A { get; }
        public B B { get; }
    }

    internal class FakeServiceProvider : IServiceProvider
    {
        private IServiceProvider _inner;
        private IServiceCollection _services;
        public IServiceCollection Services => _services;
        public FakeIServiceProviderIsService FakeServiceProviderIsService { get; set; } = new FakeIServiceProviderIsService();

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProviderIsService))
            {
                return FakeServiceProviderIsService;
            }

            return _inner.GetService(serviceType);
        }

        public void Populate(IServiceCollection services)
        {
            _services = services;
            _services.AddSingleton<FakeServiceProvider>(this);
            _services.AddSingleton<IServiceProviderIsService>((p) => (IServiceProviderIsService)FakeServiceProviderIsService);
        }

        public void Build()
        {
            _inner = _services.BuildServiceProvider();
        }
    }

    internal class FakeIServiceProviderIsService : IServiceProviderIsService
    {
        public FakeIServiceProviderIsService() { }
        public bool IsServiceGotCalled { get; set; }
        public bool IsService(Type serviceType) { IsServiceGotCalled = true; return false; }
    }

    internal class ClassWithA
    {
        public A A { get; }
        public ClassWithA(A a)
        {
            A = a;
        }
    }

    internal class ClassWithADefaultValue
    {
        public A A { get; }
        public ClassWithADefaultValue(A a = null)
        {
            A = a;
        }
    }

    internal class ABCS
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }
        public S S { get; }

        public ABCS(A a, B b, C c)
        {
            A = a;
            B = b;
            C = c;
        }

        public ABCS(B b, C c, S s)
        {
            B = b;
            C = c;
            S = s;
        }
    }

    internal class ABCS1 : ABCS
    {
        public ABCS1(A a, B b, C c) : base(a, b, c) { }
        public ABCS1(B b, C c, S s) : base(b, c, s) { }
    }

    internal class ABCS2 : ABCS
    {
        public ABCS2(B b, A a, C c) : base(a, b, c) { }
        public ABCS2(B b, S s, C c) : base(b, c, s) { }
    }

    internal class ABCS3 : ABCS
    {
        public ABCS3(B b, S s, C c) : base(b, c, s) { }
        public ABCS3(A a, B b, C c) : base(a, b, c) { }
    }

    internal class ClassWithABC
    {
        public A A { get; }
        public B B { get; }
        public C C { get; }

        public ClassWithABC() { }

        public ClassWithABC(A a)
        {
            A = a;
        }

        public ClassWithABC(A a, B b)
        {
            A = a;
            B = b;
        }

        public ClassWithABC(A a, B b, C c)
        {
            A = a;
            B = b;
            C = c;
        }
    }

    internal class ClassWithABC_MultipleCtorsWithSameLength : ClassWithABC
    {
        public ClassWithABC_MultipleCtorsWithSameLength() : base() { }
        public ClassWithABC_MultipleCtorsWithSameLength(A a) : base(a, null) { }
        public ClassWithABC_MultipleCtorsWithSameLength(B b) : base(null, b) { }
    }

    internal class ClassWithABC_DefaultConstructorFirst : ClassWithABC
    {
        public ClassWithABC_DefaultConstructorFirst() : base() { }
        public ClassWithABC_DefaultConstructorFirst(A a) : base(a) { }
        public ClassWithABC_DefaultConstructorFirst(A a, B b) : base(a, b) { }
        public ClassWithABC_DefaultConstructorFirst(A a, B b, C c) : base(a, b, c) { }
    }

    internal class ClassWithABC_DefaultConstructorLast : ClassWithABC
    {
        public ClassWithABC_DefaultConstructorLast(A a, B b, C c) : base(a, b, c) { }
        public ClassWithABC_DefaultConstructorLast(A a, B b) : base(a, b) { }
        public ClassWithABC_DefaultConstructorLast(A a) : base(a) { }
        public ClassWithABC_DefaultConstructorLast() : base() { }
    }

    internal class ClassWithStringDefaultValue
    {
        public string Text { get; set; }
        public ClassWithStringDefaultValue(string text = "DEFAULT")
        {
            Text = text;
        }
    }

#if NETCOREAPP
    internal class MyLoadContext : AssemblyLoadContext
    {
        private MyLoadContext() : base(isCollectible: true)
        {
        }

        public Assembly LoadAssembly()
        {
            Assembly asm = LoadFromAssemblyPath(GetPath());
            Assert.Equal(GetLoadContext(asm), this);
            return asm;
        }

        public static Assembly LoadAsCollectable()
        {
            MyLoadContext alc = new MyLoadContext();
            return alc.LoadAssembly();
        }

        public static Assembly LoadNormal()
        {
            return Assembly.LoadFrom(GetPath());
        }

        private static string GetPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "CollectibleAssembly.dll");
        }
    }
#endif
}
