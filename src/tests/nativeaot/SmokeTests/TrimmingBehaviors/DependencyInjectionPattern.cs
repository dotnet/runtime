// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

public class DependencyInjectionPattern
{
    public static int Run()
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
}

public class DataObject
{
    public string Name { get; set; }
}

// Simplistic implementation of DI which is comparable in behavior to our DI
class Services
{
    private Dictionary<Type, Type> _services = new Dictionary<Type, Type>();

    public void RegisterService(Type interfaceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType)
    {
        _services.Add(interfaceType, implementationType);
    }

    public T GetService<T>()
    {
        return (T)GetService(typeof(T));
    }

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

    private bool DamAnnotationsMatch(Type argument, Type parameter)
    {
        // .... - not interesting for this test, it will be true in the cases we use in this test
        return true;
    }
}

interface INameProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    string? GetName(T instance);
}

class NameProviderService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    : INameProvider<T>
{
    public string? GetName(T instance)
    {
        return (string?)typeof(T).GetProperty("Name")?.GetValue(instance);
    }
}

interface IDataObjectPrinter
{
    int GetNameLength(DataObject instance);
}

class DataObjectPrinterService : IDataObjectPrinter
{
    // The data flow is not applied on the INameProvider<DataObject> here, or in the method parameter
    // or in the call to the GetName below inside Print.
    INameProvider<DataObject> _nameProvider;

    public DataObjectPrinterService(INameProvider<DataObject> nameProvider)
    {
        _nameProvider = nameProvider;
    }

    public int GetNameLength(DataObject instance)
    {
        // This throws because DataObject.Name is not preserved
        string? name = _nameProvider.GetName(instance);
        return name == null ? 0 : name.Length;
    }
}

interface ICustomFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
{
    T Create();
}

class CustomFactoryImpl<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : ICustomFactory<T>
{
    public T Create()
    {
        return Activator.CreateInstance<T>();
    }
}

class FactoryCreated
{
    int _value;

    public FactoryCreated()
    {
        _value = 42;
    }

    public int GetValue() => _value;
}

interface ICustomFactoryWithConstraintWrapper
{
    FactoryWithConstraintCreated Create();
}

class CustomFactoryWithConstraintWrapperImpl : ICustomFactoryWithConstraintWrapper
{
    private FactoryWithConstraintCreated _value;

    public CustomFactoryWithConstraintWrapperImpl(ICustomFactoryWithConstraint<FactoryWithConstraintCreated> factory)
    {
        _value = factory.Create();
    }

    public FactoryWithConstraintCreated Create() => _value;
}

interface ICustomFactoryWithConstraint<T> where T : new()
{
    T Create();
}

class CustomFactoryWithConstraintImpl<T> : ICustomFactoryWithConstraint<T> where T : new()
{
    public T Create()
    {
        return Activator.CreateInstance<T>();
    }
}

class FactoryWithConstraintCreated
{
    int _value;

    public FactoryWithConstraintCreated()
    {
        _value = 42;
    }

    public int GetValue() => _value;
}