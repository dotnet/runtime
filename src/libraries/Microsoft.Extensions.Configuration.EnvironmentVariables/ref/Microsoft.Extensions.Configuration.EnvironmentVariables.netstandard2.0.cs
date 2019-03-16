// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Configuration
{
    public static partial class EnvironmentVariablesExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentVariables(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentVariables(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddEnvironmentVariables(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string prefix) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    public partial class EnvironmentVariablesConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
    {
        public EnvironmentVariablesConfigurationProvider() { }
        public EnvironmentVariablesConfigurationProvider(string prefix) { }
        public override void Load() { }
    }
    public partial class EnvironmentVariablesConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        public EnvironmentVariablesConfigurationSource() { }
        public string Prefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
