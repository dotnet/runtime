// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Configuration
{
    public static partial class IniConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, Microsoft.Extensions.FileProviders.IFileProvider? provider, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.Action<Microsoft.Extensions.Configuration.Ini.IniConfigurationSource>? configureSource) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange) { throw null; }
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddIniStream(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, System.IO.Stream stream) { throw null; }
    }
    public static partial class WritableIniConfigurationExtensions
    {
        public static Microsoft.Extensions.Configuration.IConfigurationBuilder AddWritableIniFile(this Microsoft.Extensions.Configuration.IConfigurationBuilder builder, string path, bool optional = true) { throw null; }
    }
}
namespace Microsoft.Extensions.Configuration.Ini
{
    public sealed partial class IniDocument
    {
        public IniDocument() { }
        public System.Collections.Generic.IReadOnlyList<string> Sections { get { throw null; } }
        public System.Collections.Generic.IReadOnlyList<(string Key, string Value)> Entries(string section) { throw null; }
        public string? Get(string section, string key) { throw null; }
        public static Microsoft.Extensions.Configuration.Ini.IniDocument Parse(string text) { throw null; }
        public bool Remove(string section, string key) { throw null; }
        public void Set(string section, string key, string value, bool? quote = default(bool?)) { }
        public override string ToString() { throw null; }
        public string ToIniString() { throw null; }
    }
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
        public static System.Collections.Generic.IDictionary<string, string?> Read(System.IO.Stream stream) { throw null; }
    }
    public partial class IniStreamConfigurationSource : Microsoft.Extensions.Configuration.StreamConfigurationSource
    {
        public IniStreamConfigurationSource() { }
        public override Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
    public sealed partial class WritableIniConfigurationProvider : Microsoft.Extensions.Configuration.ConfigurationProvider
    {
        public WritableIniConfigurationProvider(Microsoft.Extensions.Configuration.Ini.WritableIniConfigurationSource source) { }
        public string Path { get { throw null; } }
        public override void Load() { }
        public void Save() { }
        public void SetValue(string section, string key, string value, bool? quote = default(bool?)) { }
    }
    public sealed partial class WritableIniConfigurationSource : Microsoft.Extensions.Configuration.IConfigurationSource
    {
        public WritableIniConfigurationSource() { }
        public bool Optional { get { throw null; } set { } }
        public required string Path { get { throw null; } set { } }
        public Microsoft.Extensions.Configuration.IConfigurationProvider Build(Microsoft.Extensions.Configuration.IConfigurationBuilder builder) { throw null; }
    }
}
