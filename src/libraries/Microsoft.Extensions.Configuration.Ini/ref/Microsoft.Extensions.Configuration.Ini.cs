// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class IniConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.Ini.IniConfigurationSource> configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniStream(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.IO.Stream stream) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.Ini
{
    public partial class IniConfigurationProvider : Microsoft.Extensions.Configuration.FileConfigurationProvider
    {
        public IniConfigurationProvider(Microsoft.Extensions.Configuration.Ini.IniConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.FileConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
    }
    public partial class IniConfigurationSource : Microsoft.Extensions.Configuration.FileConfigurationSource
    {
        public IniConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
    public partial class IniStreamConfigurationProvider : Microsoft.Extensions.Configuration.StreamConfigurationProvider
    {
        public IniStreamConfigurationProvider(Microsoft.Extensions.Configuration.Ini.IniStreamConfigurationSource source) : base (default(Microsoft.Extensions.Configuration.StreamConfigurationSource)) { }
        public override void Load(System.IO.Stream stream) { }
        public static System.Collections.Generic.IDictionary<string, string> Read(System.IO.Stream stream) { throw null; }
    }
    public partial class IniStreamConfigurationSource : Microsoft.Extensions.Configuration.StreamConfigurationSource
    {
        public IniStreamConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
