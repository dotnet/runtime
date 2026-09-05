// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class DefaultServiceProviderFactory : Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<Microsoft.Extensions.DependencyInjection.IServiceCollection>
    {
        public DefaultServiceProviderFactory() { }
        public DefaultServiceProviderFactory(Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) { }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection CreateBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public System.IServiceProvider CreateServiceProvider(Microsoft.Extensions.DependencyInjection.IServiceCollection containerBuilder) { throw null; }
    }
    public static partial class ServiceCollectionContainerBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, bool validateScopes) { throw null; }
    }
    public partial interface IServiceCollectionValidator
    {
        Microsoft.Extensions.DependencyInjection.ValidationResult Validate(System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> services);
    }
    public static partial class ServiceCollectionValidationExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidator<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] TValidator>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TValidator : class, Microsoft.Extensions.DependencyInjection.IServiceCollectionValidator { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidator(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.IServiceCollectionValidator validator) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddValidator(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Func<System.IServiceProvider, System.Collections.Generic.IReadOnlyList<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>, Microsoft.Extensions.DependencyInjection.ValidationResult> validator) { throw null; }
    }
    public sealed partial class ServiceProvider : Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider, System.IAsyncDisposable, System.IDisposable, System.IServiceProvider
    {
        internal ServiceProvider() { }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public object? GetKeyedService(System.Type serviceType, object? serviceKey) { throw null; }
        public object GetRequiredKeyedService(System.Type serviceType, object? serviceKey) { throw null; }
        public object? GetService(System.Type serviceType) { throw null; }
    }
    public partial class ServiceProviderOptions
    {
        public ServiceProviderOptions() { }
        public bool ValidateOnBuild { get { throw null; } set { } }
        public bool ValidateScopes { get { throw null; } set { } }
    }
    public readonly partial struct ValidationResult
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public ValidationResult(System.Collections.Generic.IReadOnlyList<string> errors) { }
        public static Microsoft.Extensions.DependencyInjection.ValidationResult Success { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<string> Errors { get { throw null; } }
        public bool IsSuccess { get { throw null; } }
        public static Microsoft.Extensions.DependencyInjection.ValidationResult Fail(string error) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ValidationResult Fail(System.Collections.Generic.IReadOnlyList<string> errors) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ValidationResult operator +(Microsoft.Extensions.DependencyInjection.ValidationResult left, Microsoft.Extensions.DependencyInjection.ValidationResult right) { throw null; }
    }
}
