// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class OptionsBuilderConfigurationExtensions
    {
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> Bind<TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder, Microsoft.Extensions.Configuration.IConfiguration config) where TOptions : class { throw null; }
        public static Microsoft.Extensions.Options.OptionsBuilder<TOptions> Bind<TOptions>(this Microsoft.Extensions.Options.OptionsBuilder<TOptions> optionsBuilder, Microsoft.Extensions.Configuration.IConfiguration config, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureBinder) where TOptions : class { throw null; }
    }
    public static partial class OptionsConfigurationServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration config) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration config, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureBinder) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, Microsoft.Extensions.Configuration.IConfiguration config) where TOptions : class { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection Configure<TOptions>(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, string name, Microsoft.Extensions.Configuration.IConfiguration config, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureBinder) where TOptions : class { throw null; }
    }
}
namespace Microsoft.Extensions.Options
{
    public partial class ConfigurationChangeTokenSource<TOptions> : Microsoft.Extensions.Options.IOptionsChangeTokenSource<TOptions>
    {
        public ConfigurationChangeTokenSource(Microsoft.Extensions.Configuration.IConfiguration config) { }
        public ConfigurationChangeTokenSource(string name, Microsoft.Extensions.Configuration.IConfiguration config) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.Extensions.Primitives.IChangeToken GetChangeToken() { throw null; }
    }
    public partial class ConfigureFromConfigurationOptions<TOptions> : Microsoft.Extensions.Options.ConfigureOptions<TOptions> where TOptions : class
    {
        public ConfigureFromConfigurationOptions(Microsoft.Extensions.Configuration.IConfiguration config) : base (default(System.Action<TOptions>)) { }
    }
    public partial class NamedConfigureFromConfigurationOptions<TOptions> : Microsoft.Extensions.Options.ConfigureNamedOptions<TOptions> where TOptions : class
    {
        public NamedConfigureFromConfigurationOptions(string name, Microsoft.Extensions.Configuration.IConfiguration config) : base (default(string), default(System.Action<TOptions>)) { }
        public NamedConfigureFromConfigurationOptions(string name, Microsoft.Extensions.Configuration.IConfiguration config, System.Action<Microsoft.Extensions.Configuration.BinderOptions> configureBinder) : base (default(string), default(System.Action<TOptions>)) { }
    }
}
