// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class CommandLineConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddCommandLine(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.CommandLine.CommandLineConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddCommandLine(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string[] args) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddCommandLine(this Microsoft.Extensions.Configuration.IConfigurationBuilder configurationBuilder, string[] args, System.Collections.Generic.IDictionary<string, string> switchMappings) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.CommandLine
{
    public partial class CommandLineConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
    {
        public CommandLineConfigurationProvider(System.Collections.Generic.IEnumerable<string> args, System.Collections.Generic.IDictionary<string, string> switchMappings = null) { }
        protected System.Collections.Generic.IEnumerable<string> Args { get { throw null; } }
        public override void Load() { }
    }
    public partial class CommandLineConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        public CommandLineConfigurationSource() { }
        public System.Collections.Generic.IEnumerable<string> Args { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, string> SwitchMappings { get { throw null; } set { } }
        public Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
