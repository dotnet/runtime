// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    public class DependencyInjectionPattern
    {
        public static int Main()
        {
            Services services = new();
            services.RegisterService(typeof(INameProvider<>), typeof(NameProviderService<>));
            services.RegisterService(typeof(IDataObjectPrinter), typeof(DataObjectPrinterService));

            var printer = services.GetService<IDataObjectPrinter>();
            var actual = printer.GetNameLength(new DataObject() { Name = "0123456789" });
            Assert.Equal(10, actual);

            services.RegisterService(typeof(ICustomFactory<>), typeof(CustomFactoryImpl<>));

            var customFactory = services.GetService<ICustomFactory<FactoryCreated>>();
            var created = customFactory.Create();
            Assert.Equal(42, created.GetValue());

            services.RegisterService(typeof(ICustomFactoryWithConstraint<>), typeof(CustomFactoryWithConstraintImpl<>));
            services.RegisterService(typeof(ICustomFactoryWithConstraintWrapper), typeof(CustomFactoryWithConstraintWrapperImpl));

            var customFactoryWithConstraintWrapper = services.GetService<ICustomFactoryWithConstraintWrapper>();
            var createdWithConstraint = customFactoryWithConstraintWrapper.Create();
            Assert.Equal(42, createdWithConstraint.GetValue());

            return 100;
        }

        [Kept]
        public class DataObject
        {
            [Kept]
            public string Name { [Kept] get; [Kept] set; }

            [Kept]
            public DataObject() { }
        }

        // Simplistic implementation of DI which is comparable in behavior to our DI
        [Kept]
        class Services
        {
            [Kept]
            private Dictionary<Type, Type> _services = new Dictionary<Type, Type>();

            [Kept]
            public void RegisterService(Type interfaceType, [KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
            {
                _services.Add(interfaceType, implementationType);
            }

            [Kept]
            public T GetService<T>()
            {
                return (T)GetService(typeof(T));
            }

            [Kept]
            public object GetService(Type interfaceType)
            {
                Type typeDef = interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType;
                Type implementationType = GetImplementationType(typeDef);

                if (implementationType.IsGenericTypeDefinition)
                {
                    for (int i = 0; i < implementationType.GetGenericArguments().Length; i++)
                    {
                        Type genericArgument = implementationType.GetGenericArguments()[i];
                        Type genericParameter = interfaceType.GetGenericArguments()[i];

                        // Validate that DAM annotations match
                        if (!DamAnnotationsMatch(genericArgument, genericParameter))
                            throw new InvalidOperationException();

                        if (genericParameter.IsValueType)
                            throw new InvalidOperationException();
                    }

                    implementationType = InstantiateServiceType(implementationType, interfaceType.GetGenericArguments());
                }

                ConstructorInfo constructor = implementationType.GetConstructors()[0]; // Simplification
                if (constructor.GetParameters().Length > 0)
                {
                    List<object> instances = new();
                    foreach (var parameter in constructor.GetParameters())
                    {
                        instances.Add(GetService(parameter.ParameterType));
                    }

                    return Activator.CreateInstance(implementationType, instances.ToArray())!;
                }
                else
                {
                    return Activator.CreateInstance(implementationType)!;
                }

                [UnconditionalSuppressMessage("", "IL2068", Justification = "We only add types with the right annotation to the dictionary")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
                Type GetImplementationType(Type interfaceType)
                {
                    if (!_services.TryGetValue(interfaceType, out Type? implementationType))
                        throw new NotImplementedException();

                    return implementationType;
                }

                [UnconditionalSuppressMessage("", "IL2055", Justification = "We validated that the type parameters match - THIS IS WRONG")]
                [UnconditionalSuppressMessage("", "IL3050", Justification = "We validated there are no value types")]
                [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
                Type InstantiateServiceType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type typeDef, Type[] typeParameters)
                {
                    return typeDef.MakeGenericType(typeParameters);
                }
            }

            [Kept]
            private bool DamAnnotationsMatch(Type argument, Type parameter)
            {
                // .... - not interesting for this test, it will be true in the cases we use in this test
                return true;
            }

            [Kept]
            public Services() { }
        }

        [Kept]
        interface INameProvider<[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
        {
            [Kept(By = Tool.Trimmer)]
            [return: KeptAttributeAttribute(typeof(System.Runtime.CompilerServices.NullableAttribute))]
            string? GetName(T instance);
        }

        [Kept]
        [KeptInterface(typeof(INameProvider<>))]
        class NameProviderService<[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
            : INameProvider<T>
        {
            [Kept]
            [return: KeptAttributeAttribute(typeof(System.Runtime.CompilerServices.NullableAttribute))]
            public string? GetName(T instance)
            {
                return (string?)typeof(T).GetProperty("Name")?.GetValue(instance);
            }

            [Kept]
            public NameProviderService() { }
        }

        [Kept]
        interface IDataObjectPrinter
        {
            [Kept(By = Tool.Trimmer)]
            int GetNameLength(DataObject instance);
        }

        [Kept]
        [KeptInterface(typeof(IDataObjectPrinter))]
        class DataObjectPrinterService : IDataObjectPrinter
        {
            // The data flow is not applied on the INameProvider<DataObject> here, or in the method parameter
            // or in the call to the GetName below inside Print.
            [Kept]
            INameProvider<DataObject> _nameProvider;

            [Kept]
            public DataObjectPrinterService(INameProvider<DataObject> nameProvider)
            {
                _nameProvider = nameProvider;
            }

            [Kept]
            public int GetNameLength(DataObject instance)
            {
                // This throws because DataObject.Name is not preserved
                string? name = _nameProvider.GetName(instance);
                return name == null ? 0 : name.Length;
            }
        }

        [Kept]
        interface ICustomFactory<[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        {
            [Kept(By = Tool.Trimmer)]
            T Create();
        }

        [Kept]
        [KeptInterface(typeof(ICustomFactory<>))]
        class CustomFactoryImpl<[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))][DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : ICustomFactory<T>
        {
            [Kept]
            public T Create()
            {
                return Activator.CreateInstance<T>();
            }

            [Kept]
            public CustomFactoryImpl() { }
        }

        [Kept]
        class FactoryCreated
        {
            [Kept]
            int _value;

            [Kept]
            public FactoryCreated()
            {
                _value = 42;
            }

            [Kept]
            public int GetValue() => _value;
        }

        [Kept]
        interface ICustomFactoryWithConstraintWrapper
        {
            [Kept(By = Tool.Trimmer)]
            FactoryWithConstraintCreated Create();
        }

        [Kept]
        [KeptInterface(typeof(ICustomFactoryWithConstraintWrapper))]
        class CustomFactoryWithConstraintWrapperImpl : ICustomFactoryWithConstraintWrapper
        {
            [Kept]
            private FactoryWithConstraintCreated _value;

            [Kept]
            public CustomFactoryWithConstraintWrapperImpl(ICustomFactoryWithConstraint<FactoryWithConstraintCreated> factory)
            {
                _value = factory.Create();
            }

            [Kept]
            public FactoryWithConstraintCreated Create() => _value;
        }

        [Kept]
        interface ICustomFactoryWithConstraint<[KeptGenericParamAttributes(GenericParameterAttributes.DefaultConstructorConstraint)] T> where T : new()
        {
            [Kept(By = Tool.Trimmer)]
            T Create();
        }

        [Kept]
        [KeptInterface(typeof(ICustomFactoryWithConstraint<>))]
        class CustomFactoryWithConstraintImpl<[KeptGenericParamAttributes(GenericParameterAttributes.DefaultConstructorConstraint)] T> : ICustomFactoryWithConstraint<T> where T : new()
        {
            [Kept]
            public T Create()
            {
                return Activator.CreateInstance<T>();
            }

            [Kept]
            public CustomFactoryWithConstraintImpl() { }
        }

        [Kept]
        class FactoryWithConstraintCreated
        {
            [Kept]
            int _value;

            [Kept]
            public FactoryWithConstraintCreated()
            {
                _value = 42;
            }

            [Kept]
            public int GetValue() => _value;
        }

        [Kept]
        static class Assert
        {
            [Kept]
            public static void Equal(int expected, int actual)
            {
                if (expected != actual)
                    throw new Exception($"{expected} != {actual}");
            }
        }
    }
}
