// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;
using Xunit;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
    public abstract partial class DependencyInjectionSpecificationTests
    {
        public delegate object CreateInstanceFunc(IServiceProvider provider, Type type, object[] args);

        private static object CreateInstanceDirectly(IServiceProvider provider, Type type, object[] args)
        {
            return ActivatorUtilities.CreateInstance(provider, type, args);
        }

        private static object CreateInstanceFromFactory(IServiceProvider provider, Type type, object[] args)
        {
            var factory = ActivatorUtilities.CreateFactory(type, args.Select(a => a.GetType()).ToArray());
            return factory(provider, args);
        }

        private static T CreateInstance<T>(CreateInstanceFunc func, IServiceProvider provider, params object[] args)
        {
            return (T)func(provider, typeof(T), args);
        }

        public static IEnumerable<object[]> CreateInstanceFuncs
        {
            get
            {
                yield return new[] { (CreateInstanceFunc)CreateInstanceDirectly };
                yield return new[] { (CreateInstanceFunc)CreateInstanceFromFactory };
            }
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void TypeActivatorEnablesYouToCreateAnyTypeWithServicesEvenWhenNotInIocContainer(CreateInstanceFunc createFunc)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            var anotherClass = CreateInstance<AnotherClass>(createFunc, serviceProvider);

            Assert.NotNull(anotherClass.FakeService);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void TypeActivatorAcceptsAnyNumberOfAdditionalConstructorParametersToProvide(CreateInstanceFunc createFunc)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act
            var anotherClass = CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, "1", "2");

            // Assert
            Assert.NotNull(anotherClass.FakeService);
            Assert.Equal("1", anotherClass.One);
            Assert.Equal("2", anotherClass.Two);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithStaticCtor(CreateInstanceFunc createFunc)
        {
            // Act
            var anotherClass = CreateInstance<ClassWithStaticCtor>(createFunc, provider: null);

            // Assert
            Assert.NotNull(anotherClass);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs(CreateInstanceFunc createFunc)
        {
            // Arrange
            var provider = new TestServiceCollection();
            var serviceProvider = CreateServiceProvider(provider);

            // Act
            var anotherClass = CreateInstance<ClassWithOptionalArgsCtor>(createFunc, serviceProvider);

            // Assert
            Assert.NotNull(anotherClass);
            Assert.Equal("BLARGH", anotherClass.Whatever);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorWorksWithCtorWithOptionalArgs_WithStructDefaults(CreateInstanceFunc createFunc)
        {
            // Arrange
            var provider = new TestServiceCollection();
            var serviceProvider = CreateServiceProvider(provider);

            // Act
            var anotherClass = CreateInstance<ClassWithOptionalArgsCtorWithStructs>(createFunc, serviceProvider);

            // Assert
            Assert.NotNull(anotherClass);
            Assert.Equal(ConsoleColor.DarkGreen, anotherClass.Color);
            Assert.Null(anotherClass.ColorNull);
            Assert.Equal(12, anotherClass.Integer);
            Assert.Null(anotherClass.IntegerNull);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void TypeActivatorCanDisambiguateConstructorsWithUniqueArguments(CreateInstanceFunc createFunc)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act
            var instance = CreateInstance<ClassWithAmbiguousCtors>(createFunc, serviceProvider, "1", 2);

            // Assert
            Assert.NotNull(instance);
            Assert.NotNull(instance.FakeService);
            Assert.Equal("1", instance.Data1);
            Assert.Equal(2, instance.Data2);
        }

        public static IEnumerable<object[]> TypesWithNonPublicConstructorData =>
            CreateInstanceFuncs.Zip(
                    new[] { typeof(ClassWithPrivateCtor), typeof(ClassWithInternalConstructor), typeof(ClassWithProtectedConstructor) },
                    (a, b) => new object[] { a[0], b });

        [Theory]
        [MemberData(nameof(TypesWithNonPublicConstructorData))]
        public void TypeActivatorRequiresPublicConstructor(CreateInstanceFunc createFunc, Type type)
        {
            // Arrange
            var expectedMessage = $"A suitable constructor for type '{type}' could not be located. " +
                "Ensure the type is concrete and services are registered for all parameters of a public constructor.";

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                createFunc(provider: null, type: type, args: new object[0]));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRequiresAllArgumentsCanBeAccepted(CreateInstanceFunc createFunc)
        {
            // Arrange
            var expectedMessage = $"A suitable constructor for type '{typeof(AnotherClassAcceptingData).FullName}' could not be located. " +
                "Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            var serviceCollection = new TestServiceCollection()
                .AddTransient<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            var ex1 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, "1", "2", "3"));
            var ex2 = Assert.Throws<InvalidOperationException>(() =>
                CreateInstance<AnotherClassAcceptingData>(createFunc, serviceProvider, 1, 2));

            Assert.Equal(expectedMessage, ex1.Message);
            Assert.Equal(expectedMessage, ex2.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorRethrowsOriginalExceptionFromConstructor(CreateInstanceFunc createFunc)
        {
            // Act
            var ex1 = Assert.Throws<Exception>(() =>
                CreateInstance<ClassWithThrowingEmptyCtor>(createFunc, provider: null));

            var ex2 = Assert.Throws<Exception>(() =>
                CreateInstance<ClassWithThrowingCtor>(createFunc, provider: null, args: new[] { new FakeService() }));

            // Assert
            Assert.Equal(nameof(ClassWithThrowingEmptyCtor), ex1.Message);
            Assert.Equal(nameof(ClassWithThrowingCtor), ex2.Message);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        public void TypeActivatorCreateFactoryDoesNotAllowForAmbiguousConstructorMatches(Type paramType)
        {
            // Arrange
            var type = typeof(ClassWithAmbiguousCtors);
            var expectedMessage = $"Multiple constructors accepting all given argument types have been found in type '{type}'. " +
                "There should only be one applicable constructor.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
                ActivatorUtilities.CreateFactory(type, new[] { paramType }));

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Theory]
        [InlineData("", "string")]
        [InlineData(5, "IFakeService, int")]
        public void TypeActivatorCreateInstanceUsesFirstMathchedConstructor(object value, string ctor)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);
            var type = typeof(ClassWithAmbiguousCtors);

            // Act
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, type, value);

            // Assert
            Assert.Equal(ctor, ((ClassWithAmbiguousCtors)instance).CtorUsed);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorUsesMarkedConstructor(CreateInstanceFunc createFunc)
        {
            // Arrange
            var serviceCollection = new TestServiceCollection();
            serviceCollection.AddSingleton<IFakeService, FakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            // Act
            var instance = CreateInstance<ClassWithAmbiguousCtorsAndAttribute>(createFunc, serviceProvider, "hello");

            // Assert
            Assert.Equal("IFakeService, string", instance.CtorUsed);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorThrowsOnMultipleMarkedCtors(CreateInstanceFunc createFunc)
        {
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => CreateInstance<ClassWithMultipleMarkedCtors>(createFunc, null, "hello"));

            // Assert
            Assert.Equal("Multiple constructors were marked with ActivatorUtilitiesConstructorAttribute.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void TypeActivatorThrowsWhenMarkedCtorDoesntAcceptArguments(CreateInstanceFunc createFunc)
        {
            // Act
            var exception = Assert.Throws<InvalidOperationException>(() => CreateInstance<ClassWithAmbiguousCtorsAndAttribute>(createFunc, null, 0, "hello"));

            // Assert
            Assert.Equal("Constructor marked with ActivatorUtilitiesConstructorAttribute does not accept all given argument types.", exception.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void GetServiceOrCreateInstanceRegisteredServiceTransient()
        {
            // Reset the count because test order is not guaranteed
            lock (CreationCountFakeService.InstanceLock)
            {
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>()
                    .AddTransient<CreationCountFakeService>();

                var serviceProvider = CreateServiceProvider(serviceCollection);

                var service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(2, service.InstanceId);
                Assert.Equal(2, CreationCountFakeService.InstanceCount);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void GetServiceOrCreateInstanceRegisteredServiceSingleton()
        {
            lock (CreationCountFakeService.InstanceLock)
            {
                // Arrange
                // Reset the count because test order is not guaranteed
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>()
                    .AddSingleton<CreationCountFakeService>();
                var serviceProvider = CreateServiceProvider(serviceCollection);

                // Act and Assert
                var service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/33894", TestRuntimes.Mono)]
        public void GetServiceOrCreateInstanceUnregisteredService()
        {
            lock (CreationCountFakeService.InstanceLock)
            {
                // Arrange
                // Reset the count because test order is not guaranteed
                CreationCountFakeService.InstanceCount = 0;

                var serviceCollection = new TestServiceCollection()
                    .AddTransient<IFakeService, FakeService>();
                var serviceProvider = CreateServiceProvider(serviceCollection);

                // Act and Assert
                var service = (CreationCountFakeService)ActivatorUtilities.GetServiceOrCreateInstance(
                    serviceProvider,
                    typeof(CreationCountFakeService));
                Assert.NotNull(service);
                Assert.Equal(1, service.InstanceId);
                Assert.Equal(1, CreationCountFakeService.InstanceCount);

                service = ActivatorUtilities.GetServiceOrCreateInstance<CreationCountFakeService>(serviceProvider);
                Assert.NotNull(service);
                Assert.Equal(2, service.InstanceId);
                Assert.Equal(2, CreationCountFakeService.InstanceCount);
            }
        }

        [Theory]
        [MemberData(nameof(CreateInstanceFuncs))]
        public void UnRegisteredServiceAsConstructorParameterThrowsException(CreateInstanceFunc createFunc)
        {
            var serviceCollection = new TestServiceCollection()
                .AddSingleton<CreationCountFakeService>();
            var serviceProvider = CreateServiceProvider(serviceCollection);

            var ex = Assert.Throws<InvalidOperationException>(() =>
            CreateInstance<CreationCountFakeService>(createFunc, serviceProvider));
            Assert.Equal($"Unable to resolve service for type '{typeof(IFakeService)}' while attempting" +
                $" to activate '{typeof(CreationCountFakeService)}'.",
                ex.Message);
        }

        [Fact]
        public void CreateInstance_WithAbstractTypeAndPublicConstructor_ThrowsCorrectException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance(default(IServiceProvider), typeof(AbstractFoo)));
            var msg = "A suitable constructor for type 'Microsoft.Extensions.DependencyInjection.Specification.DependencyInjectionSpecificationTests+AbstractFoo' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            Assert.Equal(msg, ex.Message);
        }

        [Fact]
        public void CreateInstance_CapturesInnerException_OfTargetInvocationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => ActivatorUtilities.CreateInstance(default(IServiceProvider), typeof(Bar)));
            var msg = "some error";
            Assert.Equal(msg, ex.Message);
        }

        abstract class AbstractFoo
        {
            // The constructor should be public, since that is checked as well.
            public AbstractFoo()
            {
            }
        }

        class Bar
        {
            public Bar()
            {
                throw new InvalidOperationException("some error");
            }
        }
    }
}
