// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public partial class DefaultServiceProviderFactory : Microsoft.Extensions.DependencyInjection.IServiceProviderFactory<Microsoft.Extensions.DependencyInjection.IServiceCollection>
    {
        public DefaultServiceProviderFactory() { }
        public DefaultServiceProviderFactory(Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) { }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection CreateBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public System.IServiceProvider CreateServiceProvider(Microsoft.Extensions.DependencyInjection.IServiceCollection containerBuilder) { throw null; }
    }
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
        public bool Remove(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { throw null; }
        public void RemoveAt(int index) { }
        void System.Collections.Generic.ICollection<Microsoft.Extensions.DependencyInjection.ServiceDescriptor>.Add(Microsoft.Extensions.DependencyInjection.ServiceDescriptor item) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class ServiceCollectionContainerBuilderExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceProviderOptions options) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.ServiceProvider BuildServiceProvider(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, bool validateScopes) { throw null; }
    }
    public sealed partial class ServiceProvider : System.IDisposable, System.IServiceProvider
    {
        internal ServiceProvider() { }
        public void Dispose() { }
        public object GetService(System.Type serviceType) { throw null; }
    }
    public partial class ServiceProviderOptions
    {
        public ServiceProviderOptions() { }
        public bool ValidateOnBuild { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool ValidateScopes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
