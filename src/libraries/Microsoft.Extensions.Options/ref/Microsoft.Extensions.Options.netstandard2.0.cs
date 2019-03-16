// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddOptions(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> AddOptions<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TOptions : class { throw null; }
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> AddOptions<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureAll<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureOptions(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, object configureInstance) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureOptions(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Type configureType) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection ConfigureOptions<TConfigureOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) where TConfigureOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection PostConfigureAll<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection PostConfigure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection PostConfigure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, System.Action<TOptions> configureOptions) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class ConfigureNamedOptions<TOptions> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class
    {
        public ConfigureNamedOptions(string name, System.Action<TOptions> action) { }
        public System.Action<TOptions> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureNamedOptions<TOptions, TDep> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class where TDep : class
    {
        public ConfigureNamedOptions(string name, TDep dependency, System.Action<TOptions, TDep> action) { }
        public System.Action<TOptions, TDep> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep Dependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureNamedOptions<TOptions, TDep1, TDep2> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class
    {
        public ConfigureNamedOptions(string name, TDep1 dependency, TDep2 dependency2, System.Action<TOptions, TDep1, TDep2> action) { }
        public System.Action<TOptions, TDep1, TDep2> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class
    {
        public ConfigureNamedOptions(string name, TDep1 dependency, TDep2 dependency2, TDep3 dependency3, System.Action<TOptions, TDep1, TDep2, TDep3> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class
    {
        public ConfigureNamedOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep4 Dependency4 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureNamedOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : Microsoft.Extensions.Options.IConfigureNamedOptions<TOptions>, Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class
    {
        public ConfigureNamedOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep4 Dependency4 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep5 Dependency5 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(string name, TOptions options) { }
        public void Configure(TOptions options) { }
    }
    public partial class ConfigureOptions<TOptions> : Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class
    {
        public ConfigureOptions(System.Action<TOptions> action) { }
        public System.Action<TOptions> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void Configure(TOptions options) { }
    }
    public partial interface IConfigureNamedOptions<in TOptions> : Microsoft.Extensions.Options.IConfigureOptions<TOptions> where TOptions : class
    {
        void Configure(string name, TOptions options);
    }
    public partial interface IConfigureOptions<in TOptions> where TOptions : class
    {
        void Configure(TOptions options);
    }
    public partial interface IOptionsChangeTokenSource<out TOptions>
    {
        string Name { get; }
        Microsoft.Extensions.Primitives.IChangeToken GetChangeToken();
    }
    public partial interface IOptionsFactory<TOptions> where TOptions : class, new()
    {
        TOptions Create(string name);
    }
    public partial interface IOptionsMonitorCache<TOptions> where TOptions : class
    {
        void Clear();
        TOptions GetOrAdd(string name, System.Func<TOptions> createOptions);
        bool TryAdd(string name, TOptions options);
        bool TryRemove(string name);
    }
    public partial interface IOptionsMonitor<out TOptions>
    {
        TOptions CurrentValue { get; }
        TOptions Get(string name);
        System.IDisposable OnChange(System.Action<TOptions, string> listener);
    }
    public partial interface IOptionsSnapshot<out TOptions> : Microsoft.Extensions.Options.IOptions<TOptions> where TOptions : class, new()
    {
        TOptions Get(string name);
    }
    public partial interface IOptions<out TOptions> where TOptions : class, new()
    {
        TOptions Value { get; }
    }
    public partial interface IPostConfigureOptions<in TOptions> where TOptions : class
    {
        void PostConfigure(string name, TOptions options);
    }
    public partial interface IValidateOptions<TOptions> where TOptions : class
    {
        Microsoft.Extensions.Options.ValidateOptionsResult Validate(string name, TOptions options);
    }
    public static partial class Options
    {
        public static readonly string DefaultName;
        public static Microsoft.Extensions.Options.IOptions<TOptions> Create<TOptions>(TOptions options) where TOptions : class, new() { throw null; }
    }
    public partial class OptionsBuilder<TOptions> where TOptions : class
    {
        public OptionsBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceCollection Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure(System.Action<TOptions> configureOptions) { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure<TDep>(System.Action<TOptions, TDep> configureOptions) where TDep : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure<TDep1, TDep2>(System.Action<TOptions, TDep1, TDep2> configureOptions) where TDep1 : class where TDep2 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3>(System.Action<TOptions, TDep1, TDep2, TDep3> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4>(System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Configure<TDep1, TDep2, TDep3, TDep4, TDep5>(System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure(System.Action<TOptions> configureOptions) { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure<TDep>(System.Action<TOptions, TDep> configureOptions) where TDep : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2>(System.Action<TOptions, TDep1, TDep2> configureOptions) where TDep1 : class where TDep2 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3>(System.Action<TOptions, TDep1, TDep2, TDep3> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4>(System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> PostConfigure<TDep1, TDep2, TDep3, TDep4, TDep5>(System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> configureOptions) where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Validate(System.Func<TOptions, bool> validation) { throw null; }
        public virtual Microsoft.Extensions.Options.OptionsBuilder<TOptions> Validate(System.Func<TOptions, bool> validation, string failureMessage) { throw null; }
    }
    public partial class OptionsCache<TOptions> : Microsoft.Extensions.Options.IOptionsMonitorCache<TOptions> where TOptions : class
    {
        public OptionsCache() { }
        public void Clear() { }
        public virtual TOptions GetOrAdd(string name, System.Func<TOptions> createOptions) { throw null; }
        public virtual bool TryAdd(string name, TOptions options) { throw null; }
        public virtual bool TryRemove(string name) { throw null; }
    }
    public partial class OptionsFactory<TOptions> : Microsoft.Extensions.Options.IOptionsFactory<TOptions> where TOptions : class, new()
    {
        public OptionsFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IConfigureOptions<TOptions>> setups, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IPostConfigureOptions<TOptions>> postConfigures) { }
        public OptionsFactory(System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IConfigureOptions<TOptions>> setups, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IPostConfigureOptions<TOptions>> postConfigures, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IValidateOptions<TOptions>> validations) { }
        public TOptions Create(string name) { throw null; }
    }
    public partial class OptionsManager<TOptions> : Microsoft.Extensions.Options.IOptions<TOptions>, Microsoft.Extensions.Options.IOptionsSnapshot<TOptions> where TOptions : class, new()
    {
        public OptionsManager(Microsoft.Extensions.Options.IOptionsFactory<TOptions> factory) { }
        public TOptions Value { get { throw null; } }
        public virtual TOptions Get(string name) { throw null; }
    }
    public static partial class OptionsMonitorExtensions
    {
        public static System.IDisposable OnChange<TOptions>(this Microsoft.Extensions.Options.IOptionsMonitor<TOptions> monitor, System.Action<TOptions> listener) { throw null; }
    }
    public partial class OptionsMonitor<TOptions> : Microsoft.Extensions.Options.IOptionsMonitor<TOptions>, System.IDisposable where TOptions : class, new()
    {
        public OptionsMonitor(Microsoft.Extensions.Options.IOptionsFactory<TOptions> factory, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Options.IOptionsChangeTokenSource<TOptions>> sources, Microsoft.Extensions.Options.IOptionsMonitorCache<TOptions> cache) { }
        public TOptions CurrentValue { get { throw null; } }
        public void Dispose() { }
        public virtual TOptions Get(string name) { throw null; }
        public System.IDisposable OnChange(System.Action<TOptions, string> listener) { throw null; }
    }
    public partial class OptionsValidationException : System.Exception
    {
        public OptionsValidationException(string optionsName, System.Type optionsType, System.Collections.Generic.IEnumerable<string> failureMessages) { }
        public System.Collections.Generic.IEnumerable<string> Failures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string OptionsName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Type OptionsType { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class OptionsWrapper<TOptions> : Microsoft.Extensions.Options.IOptions<TOptions> where TOptions : class, new()
    {
        public OptionsWrapper(TOptions options) { }
        public TOptions Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class PostConfigureOptions<TOptions> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class
    {
        public PostConfigureOptions(string name, System.Action<TOptions> action) { }
        public System.Action<TOptions> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
    }
    public partial class PostConfigureOptions<TOptions, TDep> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class where TDep : class
    {
        public PostConfigureOptions(string name, TDep dependency, System.Action<TOptions, TDep> action) { }
        public System.Action<TOptions, TDep> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep Dependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
        public void PostConfigure(TOptions options) { }
    }
    public partial class PostConfigureOptions<TOptions, TDep1, TDep2> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class
    {
        public PostConfigureOptions(string name, TDep1 dependency, TDep2 dependency2, System.Action<TOptions, TDep1, TDep2> action) { }
        public System.Action<TOptions, TDep1, TDep2> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
        public void PostConfigure(TOptions options) { }
    }
    public partial class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class
    {
        public PostConfigureOptions(string name, TDep1 dependency, TDep2 dependency2, TDep3 dependency3, System.Action<TOptions, TDep1, TDep2, TDep3> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
        public void PostConfigure(TOptions options) { }
    }
    public partial class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class
    {
        public PostConfigureOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3, TDep4> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep4 Dependency4 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
        public void PostConfigure(TOptions options) { }
    }
    public partial class PostConfigureOptions<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> : Microsoft.Extensions.Options.IPostConfigureOptions<TOptions> where TOptions : class where TDep1 : class where TDep2 : class where TDep3 : class where TDep4 : class where TDep5 : class
    {
        public PostConfigureOptions(string name, TDep1 dependency1, TDep2 dependency2, TDep3 dependency3, TDep4 dependency4, TDep5 dependency5, System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> action) { }
        public System.Action<TOptions, TDep1, TDep2, TDep3, TDep4, TDep5> Action { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep1 Dependency1 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep2 Dependency2 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep3 Dependency3 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep4 Dependency4 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TDep5 Dependency5 { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public virtual void PostConfigure(string name, TOptions options) { }
        public void PostConfigure(TOptions options) { }
    }
    public partial class ValidateOptionsResult
    {
        public static readonly Microsoft.Extensions.Options.ValidateOptionsResult Skip;
        public static readonly Microsoft.Extensions.Options.ValidateOptionsResult Success;
        public ValidateOptionsResult() { }
        public bool Failed { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public string FailureMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public bool Skipped { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public bool Succeeded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public static Microsoft.Extensions.Options.ValidateOptionsResult Fail(string failureMessage) { throw null; }
    }
    public partial class ValidateOptions<TOptions> : Microsoft.Extensions.Options.IValidateOptions<TOptions> where TOptions : class
    {
        public ValidateOptions(string name, System.Func<TOptions, bool> validation, string failureMessage) { }
        public string FailureMessage { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Func<TOptions, bool> Validation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Options.ValidateOptionsResult Validate(string name, TOptions options) { throw null; }
    }
}
