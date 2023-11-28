// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ActivatorUtilities
    {
        public static Microsoft.Extensions.DependencyInjection.ObjectFactory CreateFactory([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type instanceType, System.Type[] argumentTypes) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ObjectFactory<T> CreateFactory<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>(System.Type[] argumentTypes) { throw null; }
        public static object CreateInstance(System.IServiceProvider provider, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type instanceType, params object[] parameters) { throw null; }
        public static T CreateInstance<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>(System.IServiceProvider provider, params object[] parameters) { throw null; }
        public static object GetServiceOrCreateInstance(System.IServiceProvider provider, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type) { throw null; }
        public static T GetServiceOrCreateInstance<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] T>(System.IServiceProvider provider) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class ActivatorUtilitiesConstructorAttribute : System.Attribute
    {
        public ActivatorUtilitiesConstructorAttribute() { }
    }
    public readonly partial struct AsyncServiceScope : Microsoft.Extensions.DependencyInjection.IServiceScope, System.IAsyncDisposable, System.IDisposable
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public AsyncServiceScope(Microsoft.Extensions.DependencyInjection.IServiceScope serviceScope) { throw null; }
        public System.IServiceProvider ServiceProvider { get { throw null; } }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter)]
    public partial class FromKeyedServicesAttribute : System.Attribute
    {
        public FromKeyedServicesAttribute(object key) { }
        public object Key { get { throw null; } }
    }
    public partial interface IServiceCollection : System.Collections.Generic.ICollection<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.Generic.IList<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.IEnumerable
    {
    }
    public partial interface IServiceProviderFactory<TContainerBuilder> where TContainerBuilder : notnull
    {
        TContainerBuilder CreateBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services);
        System.IServiceProvider CreateServiceProvider(TContainerBuilder containerBuilder);
    }
    public partial interface IServiceProviderIsService
    {
        bool IsService(System.Type serviceType);
    }
    public partial interface IServiceProviderIsKeyedService : IServiceProviderIsService
    {
        bool IsKeyedService(System.Type serviceType, object? serviceKey);
    }
    public partial interface IServiceScope : System.IDisposable
    {
        System.IServiceProvider ServiceProvider { get; }
    }
    public partial interface IServiceScopeFactory
    {
        Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope();
    }
    public partial interface IKeyedServiceProvider : System.IServiceProvider
    {
        object? GetKeyedService(System.Type serviceType, object? serviceKey);
        object GetRequiredKeyedService(System.Type serviceType, object? serviceKey);
    }
    public partial interface ISupportRequiredService
    {
        object GetRequiredService(System.Type serviceType);
    }
    public static partial class KeyedService
    {
        public static object AnyKey { get { throw null; } }
    }
    public delegate object ObjectFactory(System.IServiceProvider serviceProvider, object?[]? arguments);
    public delegate T ObjectFactory<T>(System.IServiceProvider serviceProvider, object?[]? arguments);
    public partial class ServiceCollection : Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Collections.Generic.ICollection<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.Generic.IList<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, System.Collections.IEnumerable
    {
        public ServiceCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.ServiceDescriptor this[int index] { get { throw null; } set { } }
        public void Clear() { }
        public bool Contains(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { throw null; }
        public void CopyTo(Microsoft.Extensions.DependencyInjection.ServiceDescriptor[] array, int arrayIndex) { }
        public System.Collections.Generic.IEnumerator<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> GetEnumerator() { throw null; }
        public int IndexOf(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { throw null; }
        public void Insert(int index, Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { }
        public void MakeReadOnly() { }
        public bool Remove(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.Generic.ICollection<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>.Add(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class ServiceCollectionServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType, object? serviceKey) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedScoped<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType, object? serviceKey) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, object implementationInstance) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, TService implementationInstance) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedSingleton<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType, object? serviceKey) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddKeyedTransient<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddScoped<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, object implementationInstance) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, TService implementationInstance) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddSingleton<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type serviceType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type serviceType, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTransient<TService, TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
    }
    public partial class ServiceDescriptor
    {
        public ServiceDescriptor(System.Type serviceType, System.Func<System.IServiceProvider, object> factory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { }
        public ServiceDescriptor(System.Type serviceType, object instance) { }
        public ServiceDescriptor(System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object?, object> factory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { }
        public ServiceDescriptor(System.Type serviceType, object? serviceKey, object instance) { }
        public ServiceDescriptor(System.Type serviceType, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { }
        public ServiceDescriptor(System.Type serviceType, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { }
        public System.Func<System.IServiceProvider, object>? ImplementationFactory { get { throw null; } }
        public object? ImplementationInstance { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public System.Type? ImplementationType { get { throw null; } }
        public bool IsKeyedService { get { throw null; } }
        public System.Func<System.IServiceProvider, object?, object>? KeyedImplementationFactory { get { throw null; } }
        public object? KeyedImplementationInstance { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public System.Type? KeyedImplementationType { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get { throw null; } }
        public object? ServiceKey { get { throw null; } }
        public System.Type ServiceType { get { throw null; } }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Describe(System.Type serviceType, System.Func<System.IServiceProvider, object> implementationFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Describe(System.Type serviceType, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor DescribeKeyed(System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor DescribeKeyed(System.Type serviceType, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedScoped(System.Type service, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedScoped(System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedScoped<TService>(object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedScoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedScoped<TService, TImplementation>(object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton(System.Type serviceType, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton(System.Type serviceType, object? serviceKey, object implementationInstance) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton(System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton<TService>(object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton<TService>(object? serviceKey, TService implementationInstance) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedSingleton<TService, TImplementation>(object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedTransient(System.Type service, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedTransient(System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedTransient<TService>(object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedTransient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(object? serviceKey) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor KeyedTransient<TService, TImplementation>(object? serviceKey, System.Func<System.IServiceProvider, object, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Scoped(System.Type service, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Scoped(System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Scoped<TService>(System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Scoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>() where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Scoped<TService, TImplementation>(System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton(System.Type serviceType, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton(System.Type serviceType, object implementationInstance) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton(System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton<TService>(System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton<TService>(TService implementationInstance) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>() where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Singleton<TService, TImplementation>(System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
        public override string ToString() { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Transient(System.Type service, System.Func<System.IServiceProvider, object> implementationFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Transient(System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Transient<TService>(System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Transient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>() where TService : class where TImplementation : class, TService { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceDescriptor Transient<TService, TImplementation>(System.Func<System.IServiceProvider, TImplementation> implementationFactory) where TService : class where TImplementation : class, TService { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Parameter)]
    public partial class ServiceKeyAttribute : System.Attribute
    {
        public ServiceKeyAttribute() { }
    }
    public enum ServiceLifetime
    {
        Singleton = 0,
        Scoped = 1,
        Transient = 2,
    }
    public static partial class ServiceProviderKeyedServiceExtensions
    {
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static System.Collections.Generic.IEnumerable<object?> GetKeyedServices(this System.IServiceProvider provider, System.Type serviceType, object? serviceKey) { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetKeyedServices<T>(this System.IServiceProvider provider, object? serviceKey) { throw null; }
        public static T? GetKeyedService<T>(this System.IServiceProvider provider, object? serviceKey) { throw null; }
        public static object GetRequiredKeyedService(this System.IServiceProvider provider, System.Type serviceType, object? serviceKey) { throw null; }
        public static T GetRequiredKeyedService<T>(this System.IServiceProvider provider, object? serviceKey) where T : notnull { throw null; }
    }
    public static partial class ServiceProviderServiceExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.AsyncServiceScope CreateAsyncScope(this Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.AsyncServiceScope CreateAsyncScope(this System.IServiceProvider provider) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceScope CreateScope(this System.IServiceProvider provider) { throw null; }
        public static object GetRequiredService(this System.IServiceProvider provider, System.Type serviceType) { throw null; }
        public static T GetRequiredService<T>(this System.IServiceProvider provider) where T : notnull { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute("The native code for an IEnumerable<serviceType> might not be available at runtime.")]
        public static System.Collections.Generic.IEnumerable<object?> GetServices(this System.IServiceProvider provider, System.Type serviceType) { throw null; }
        public static System.Collections.Generic.IEnumerable<T> GetServices<T>(this System.IServiceProvider provider) { throw null; }
        public static T? GetService<T>(this System.IServiceProvider provider) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    public static partial class ServiceCollectionDescriptorExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Add(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, Microsoft.Extensions.DependencyInjection.ServiceDescriptor descriptor) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Add(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> descriptors) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection RemoveAll(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type serviceType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection RemoveAll<T>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Replace(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, Microsoft.Extensions.DependencyInjection.ServiceDescriptor descriptor) { throw null; }
        public static void TryAdd(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, Microsoft.Extensions.DependencyInjection.ServiceDescriptor descriptor) { }
        public static void TryAdd(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> descriptors) { }
        public static void TryAddEnumerable(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceDescriptor descriptor) { }
        public static void TryAddEnumerable(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> descriptors) { }
        public static void TryAddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service) { }
        public static void TryAddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, System.Func<System.IServiceProvider, object> implementationFactory) { }
        public static void TryAddScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddScoped<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class { }
        public static void TryAddScoped<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { }
        public static void TryAddScoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class where TImplementation : class, TService { }
        public static void TryAddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service) { }
        public static void TryAddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, System.Func<System.IServiceProvider, object> implementationFactory) { }
        public static void TryAddSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddSingleton<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class { }
        public static void TryAddSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { }
        public static void TryAddSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, TService instance) where TService : class { }
        public static void TryAddSingleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class where TImplementation : class, TService { }
        public static void TryAddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service) { }
        public static void TryAddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, System.Func<System.IServiceProvider, object> implementationFactory) { }
        public static void TryAddTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddTransient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class { }
        public static void TryAddTransient<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, TService> implementationFactory) where TService : class { }
        public static void TryAddTransient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection) where TService : class where TImplementation : class, TService { }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection RemoveAllKeyed(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type serviceType, object? serviceKey) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection RemoveAllKeyed<T>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) { throw null; }
        public static void TryAddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service, object? serviceKey) { }
        public static void TryAddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { }
        public static void TryAddKeyedScoped(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddKeyedScoped<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class { }
        public static void TryAddKeyedScoped<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { }
        public static void TryAddKeyedScoped<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class where TImplementation : class, TService { }
        public static void TryAddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service, object? serviceKey) { }
        public static void TryAddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { }
        public static void TryAddKeyedSingleton(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddKeyedSingleton<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class { }
        public static void TryAddKeyedSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { }
        public static void TryAddKeyedSingleton<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey, TService instance) where TService : class { }
        public static void TryAddKeyedSingleton<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class where TImplementation : class, TService { }
        public static void TryAddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type service, object? serviceKey) { }
        public static void TryAddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, System.Func<System.IServiceProvider, object, object> implementationFactory) { }
        public static void TryAddKeyedTransient(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, System.Type service, object? serviceKey, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type implementationType) { }
        public static void TryAddKeyedTransient<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class { }
        public static void TryAddKeyedTransient<TService>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object? serviceKey, System.Func<System.IServiceProvider, object, TService> implementationFactory) where TService : class { }
        public static void TryAddKeyedTransient<TService, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(this Microsoft.Extensions.DependencyInjection.IServiceCollection collection, object? serviceKey) where TService : class where TImplementation : class, TService { }
    }
}
